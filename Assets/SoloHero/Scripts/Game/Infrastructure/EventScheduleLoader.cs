using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Firebase.Database;
using Newtonsoft.Json.Linq;
using SoloHero.Core.Common;
using SoloHero.Core.Events;
using UnityEngine;

namespace SoloHero.Game.Infrastructure
{
    /// <summary>
    /// D-156 the event schedule: RTDB <c>config/events</c> (players read, only the console writes), shaped
    /// <c>{"launch_2026": {"template": "launch", "start": 1792000000, "end": 1793209600}}</c> (UTC seconds). The last list
    /// read is kept on the device, so a running event stays open without a network; a failed read keeps it (external I/O
    /// falls back). Development builds use files/event_test.txt instead when it exists (QA without the server).
    /// </summary>
    public static class EventScheduleLoader
    {
        private const string CacheKey = "event_schedule_v1";
        private const string RemotePath = "config/events";
        private const int TimeoutMs = 6000;

        public static void Start(EventService events, bool firebaseReady)
        {
            if (events == null) return;
            string cached = PlayerPrefs.GetString(CacheKey, "");
            if (!string.IsNullOrEmpty(cached)) events.SetSchedule(Parse(cached));
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            try
            {
                string test = Path.Combine(Application.persistentDataPath, "event_test.txt");
                if (File.Exists(test))
                {
                    Log.Warn(LogTag.Boot, "QA: event schedule from event_test.txt");
                    events.SetSchedule(Parse(File.ReadAllText(test)));
                    return;
                }
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Boot, "QA: event_test.txt not used: " + e.Message);
            }
#endif
            if (firebaseReady) _ = FetchAsync(events);
        }

        private static async Task FetchAsync(EventService events)
        {
            try
            {
                Task<DataSnapshot> get = GameDatabase.Instance.GetReference(RemotePath).GetValueAsync();
                Task first = await Task.WhenAny(get, Task.Delay(TimeoutMs));
                if (first != get || get.IsFaulted || get.IsCanceled)
                {
                    Log.Warn(LogTag.Boot, "event schedule not read, keeping the saved one");
                    return;
                }

                DataSnapshot snapshot = get.Result;
                string json = snapshot != null && snapshot.Exists ? snapshot.GetRawJsonValue() : "{}";
                MainThreadDispatcher.Post(() =>
                {
                    events.SetSchedule(Parse(json));
                    PlayerPrefs.SetString(CacheKey, json);
                    PlayerPrefs.Save();
                });
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Boot, "event schedule unavailable: " + e.Message);
            }
        }

        /// <summary>Schedule entries from the node's JSON; anything malformed is skipped.</summary>
        public static List<ScheduleEntry> Parse(string json)
        {
            var list = new List<ScheduleEntry>();
            if (string.IsNullOrEmpty(json)) return list;
            try
            {
                JObject root = JObject.Parse(json);
                foreach (JProperty p in root.Properties())
                {
                    if (!(p.Value is JObject o)) continue;
                    list.Add(new ScheduleEntry
                    {
                        Id = p.Name,
                        Template = (string)o["template"] ?? "",
                        StartUtc = (long?)o["start"] ?? 0L,
                        EndUtc = (long?)o["end"] ?? 0L
                    });
                }
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Boot, "event schedule unreadable: " + e.Message);
            }

            return list;
        }
    }
}
