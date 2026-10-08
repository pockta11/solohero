using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SoloHero.Core.Common;

namespace SoloHero.Core.Save
{
    public sealed class SaveService
    {
        public const int DebounceMs = 250;

        /// <summary>
        /// D-136: how long boot waits for the server copy. Without a network the database never answers (the old build
        /// sat on the loading screen forever); the local backup is never older than the server's on this device
        /// (D-073 writes it first), so after the wait the game starts from it and uploads it later.
        /// </summary>
        public const int RemoteLoadTimeoutMs = 6000;

        private readonly ISaveStore _localV2;
        private readonly ISaveSerializer _serializer;
        private readonly ISaveStore _remoteV2;
        private readonly ISaveStore _remoteV1;
        private readonly ISaveStore _localV1;
        private readonly IReadOnlyCollection<string> _knownEquipmentIds;
        private readonly int _debounceMs;
        private readonly int _remoteLoadTimeoutMs;
        private readonly object _gate = new object();

        private CancellationTokenSource _debounceCts;
        private bool _running;
        private bool _hasPending;
        private bool _suspended;
        private string _pending;
        private Task _run = Task.CompletedTask;

        public SaveService(
            ISaveStore localV2,
            ISaveSerializer serializer,
            ISaveStore remoteV2 = null,
            ISaveStore remoteV1 = null,
            ISaveStore localV1 = null,
            IReadOnlyCollection<string> knownEquipmentIds = null,
            int debounceMs = DebounceMs,
            int remoteLoadTimeoutMs = RemoteLoadTimeoutMs)
        {
            _localV2 = localV2 ?? throw new ArgumentNullException(nameof(localV2));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _remoteV2 = remoteV2;
            _remoteV1 = remoteV1;
            _localV1 = localV1;
            _knownEquipmentIds = knownEquipmentIds;
            _debounceMs = debounceMs;
            _remoteLoadTimeoutMs = remoteLoadTimeoutMs;
        }

        /// <summary>
        /// Writes the local backup at once (a kill right after a purchase keeps it, E9-08) and debounces the upload.
        /// </summary>
        public void RequestSave(SaveDataV2 data)
        {
            if (data == null || IsSuspended) return;
            string json = Queue(data);
            WriteLocalNow(json);
            RestartDebounce();
        }

        public Task FlushAsync(SaveDataV2 data)
        {
            if (IsSuspended) return _run;
            if (data != null) Queue(data);
            CancelDebounce();
            StartRun();
            return _run;
        }

        /// <summary>True while <see cref="Suspend"/> holds every write back.</summary>
        public bool IsSuspended
        {
            get
            {
                lock (_gate) return _suspended;
            }
        }

        /// <summary>
        /// D-134: no more writes - for the account tools (deleting the data, taking a transferred save), whose result
        /// must not be overwritten by the game's own next save. Unsent changes are dropped; a write already running
        /// finishes first (the database applies one client's writes in order).
        /// </summary>
        public void Suspend()
        {
            CancelDebounce();
            lock (_gate)
            {
                _suspended = true;
                _hasPending = false;
                _pending = null;
            }
        }

        /// <summary>Writes again after <see cref="Suspend"/> (the account tool gave up).</summary>
        public void Resume()
        {
            lock (_gate) _suspended = false;
        }

        /// <summary>
        /// D-134: writes <paramref name="data"/> to every store at once, also while suspended (a transferred save that the
        /// restart will load). The local copy is written first; a failed upload is fine because the higher revision
        /// uploads it on the next start (D-073).
        /// </summary>
        public async Task WriteThroughAsync(SaveDataV2 data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            string json = _serializer.ToJson(data);
            await _localV2.SaveJsonAsync(json);
            if (_remoteV2 == null) return;

            try
            {
                await _remoteV2.SaveJsonAsync(json);
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Save, "remote write of the adopted save failed, uploaded on the next start: " + e.Message);
            }
        }

        public async Task<SaveDataV2> LoadAsync()
        {
            if (_remoteV2 == null) return await LoadOfflineAsync();

            try
            {
                string json = await LoadRemoteAsync(_remoteV2);
                if (!string.IsNullOrEmpty(json))
                {
                    SaveDataV2 data = _serializer.FromV2Json(json) ?? SaveDataV2.CreateNew();
                    SaveDataV2 local = await TryLoadLocalV2Async();
                    if (local != null && local.saveRevision > data.saveRevision)
                    {
                        // The app stopped before the last upload: the local backup is newer. Keep it and upload it
                        // (not awaited: a slow upload must not hold the loading screen).
                        Log.Info(LogTag.Save, "local save newer than remote (" + local.saveRevision + " > " + data.saveRevision + "), uploading");
                        _ = TryUploadAsync(local);
                        return local;
                    }

                    await _localV2.SaveJsonAsync(_serializer.ToJson(data));
                    return data;
                }

                SaveDataV2 migrated = await TryMigrateAsync(_remoteV1, writeRemote: true);
                if (migrated != null) return migrated;
                return await LoadOfflineAsync();
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Save, "remote load failed, using local backup: " + e.Message);
                return await LoadOfflineAsync();
            }
        }

        /// <summary>The server copy, or a TimeoutException after <see cref="RemoteLoadTimeoutMs"/> (no network).</summary>
        private async Task<string> LoadRemoteAsync(ISaveStore store)
        {
            Task<string> load = store.LoadJsonAsync();
            if (await Task.WhenAny(load, Task.Delay(_remoteLoadTimeoutMs)) != load)
            {
                // Observe the late result so an eventual fault is not an unobserved task exception.
                _ = load.ContinueWith(t => t.Exception, TaskContinuationOptions.OnlyOnFaulted);
                throw new TimeoutException("no answer from the server in " + _remoteLoadTimeoutMs + " ms");
            }

            return await load;
        }

        private async Task<SaveDataV2> TryLoadLocalV2Async()
        {
            try
            {
                string json = await _localV2.LoadJsonAsync();
                return string.IsNullOrEmpty(json) ? null : _serializer.FromV2Json(json);
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Save, "local backup unreadable: " + e.Message);
                return null;
            }
        }

        private async Task TryUploadAsync(SaveDataV2 data)
        {
            try
            {
                await _remoteV2.SaveJsonAsync(_serializer.ToJson(data));
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Save, "upload of newer local save failed, retried on next save: " + e.Message);
            }
        }

        private void WriteLocalNow(string json)
        {
            try
            {
                _ = _localV2.SaveJsonAsync(json);
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Save, "local backup write failed: " + e.Message);
            }
        }

        private async Task<SaveDataV2> LoadOfflineAsync()
        {
            string json = await _localV2.LoadJsonAsync();
            if (!string.IsNullOrEmpty(json))
            {
                SaveDataV2 data = _serializer.FromV2Json(json);
                if (data != null) return data;
            }

            SaveDataV2 migrated = await TryMigrateAsync(_localV1, writeRemote: false);
            return migrated ?? SaveDataV2.CreateNew();
        }

        private async Task<SaveDataV2> TryMigrateAsync(ISaveStore source, bool writeRemote)
        {
            if (source == null) return null;
            string json = source == _localV1 ? await source.LoadJsonAsync() : await LoadRemoteAsync(source);
            if (string.IsNullOrEmpty(json)) return null;

            PlayerDataV1 v1 = _serializer.FromV1Json(json);
            if (v1 == null) return null;

            SaveDataV2 data = MigrationV1ToV2.Convert(v1, _knownEquipmentIds);
            await _localV2.SaveJsonAsync(_serializer.ToJson(data));
            if (writeRemote && _remoteV2 != null) _ = TryUploadAsync(data);

            return data;
        }

        private string Queue(SaveDataV2 data)
        {
            data.saveRevision++;
            string json = _serializer.ToJson(data);
            lock (_gate)
            {
                _pending = json;
                _hasPending = true;
            }

            return json;
        }

        private void RestartDebounce()
        {
            CancelDebounce();
            _debounceCts = new CancellationTokenSource();
            CancellationToken token = _debounceCts.Token;
            _ = DebounceAsync(token);
        }

        private void CancelDebounce()
        {
            if (_debounceCts == null) return;
            _debounceCts.Cancel();
            _debounceCts.Dispose();
            _debounceCts = null;
        }

        private async Task DebounceAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(_debounceMs, token);
                StartRun();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void StartRun()
        {
            lock (_gate)
            {
                if (_running || !_hasPending) return;
                _running = true;
                _run = RunAsync();
            }
        }

        private async Task RunAsync()
        {
            try
            {
                while (true)
                {
                    string json;
                    lock (_gate)
                    {
                        if (!_hasPending || _suspended)
                        {
                            _running = false;
                            return;
                        }

                        _hasPending = false;
                        json = _pending;
                    }

                    if (string.IsNullOrEmpty(json)) continue;

                    try
                    {
                        await PersistAsync(json);
                    }
                    catch (Exception e)
                    {
                        Log.Warn(LogTag.Save, "save failed, local backup kept: " + e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Save, "save queue stopped: " + e.Message);
                lock (_gate)
                {
                    _running = false;
                }
            }
        }

        private async Task PersistAsync(string json)
        {
            await _localV2.SaveJsonAsync(json);
            if (_remoteV2 == null) return;

            try
            {
                await _remoteV2.SaveJsonAsync(json);
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Save, "remote save failed, local backup kept: " + e.Message);
            }
        }
    }
}
