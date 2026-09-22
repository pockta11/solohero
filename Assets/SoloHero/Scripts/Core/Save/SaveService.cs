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

        private readonly ISaveStore _localV2;
        private readonly ISaveSerializer _serializer;
        private readonly ISaveStore _remoteV2;
        private readonly ISaveStore _remoteV1;
        private readonly ISaveStore _localV1;
        private readonly IReadOnlyCollection<string> _knownEquipmentIds;
        private readonly int _debounceMs;
        private readonly object _gate = new object();

        private CancellationTokenSource _debounceCts;
        private bool _running;
        private bool _hasPending;
        private string _pending;
        private Task _run = Task.CompletedTask;

        public SaveService(
            ISaveStore localV2,
            ISaveSerializer serializer,
            ISaveStore remoteV2 = null,
            ISaveStore remoteV1 = null,
            ISaveStore localV1 = null,
            IReadOnlyCollection<string> knownEquipmentIds = null,
            int debounceMs = DebounceMs)
        {
            _localV2 = localV2 ?? throw new ArgumentNullException(nameof(localV2));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _remoteV2 = remoteV2;
            _remoteV1 = remoteV1;
            _localV1 = localV1;
            _knownEquipmentIds = knownEquipmentIds;
            _debounceMs = debounceMs;
        }

        public void RequestSave(SaveDataV2 data)
        {
            if (data == null) return;
            Queue(data);
            RestartDebounce();
        }

        public Task FlushAsync(SaveDataV2 data)
        {
            if (data != null) Queue(data);
            CancelDebounce();
            StartRun();
            return _run;
        }

        public async Task<SaveDataV2> LoadAsync()
        {
            if (_remoteV2 == null) return await LoadOfflineAsync();

            try
            {
                string json = await _remoteV2.LoadJsonAsync();
                if (!string.IsNullOrEmpty(json))
                {
                    SaveDataV2 data = _serializer.FromV2Json(json) ?? SaveDataV2.CreateNew();
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
            string json = await source.LoadJsonAsync();
            if (string.IsNullOrEmpty(json)) return null;

            PlayerDataV1 v1 = _serializer.FromV1Json(json);
            if (v1 == null) return null;

            SaveDataV2 data = MigrationV1ToV2.Convert(v1, _knownEquipmentIds);
            await _localV2.SaveJsonAsync(_serializer.ToJson(data));
            if (writeRemote && _remoteV2 != null)
            {
                try
                {
                    await _remoteV2.SaveJsonAsync(_serializer.ToJson(data));
                }
                catch (Exception e)
                {
                    Log.Warn(LogTag.Save, "remote save of migration failed, local backup kept: " + e.Message);
                }
            }

            return data;
        }

        private void Queue(SaveDataV2 data)
        {
            string json = _serializer.ToJson(data);
            lock (_gate)
            {
                _pending = json;
                _hasPending = true;
            }
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
                        if (!_hasPending)
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
