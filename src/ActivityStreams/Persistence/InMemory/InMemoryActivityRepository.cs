﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ActivityStreams.Helpers;

namespace ActivityStreams.Persistence.InMemory
{
    public class InMemoryActivityRepository : IActivityRepository
    {
        ConcurrentDictionary<byte[], SortedSet<Activity>> activityStreamStore = new ConcurrentDictionary<byte[], SortedSet<Activity>>(new ByteArrayEqualityComparer());

        public void Append(Activity activity)
        {
            SortedSet<Activity> stream;
            if (activityStreamStore.TryGetValue(activity.StreamId, out stream))
                stream.Add(activity);
            else
                activityStreamStore.TryAdd(activity.StreamId, new SortedSet<Activity>(Activity.Comparer) { activity });
        }

        /// <summary>
        /// FanIn
        /// </summary>
        public IEnumerable<Activity> Load(Feed feed)
        {
            return Load(feed, DateTime.UtcNow);
        }

        public IEnumerable<Activity> Load(Feed feed, DateTime timestamp)
        {
            var snapshot = new Dictionary<byte[], Queue<Activity>>(activityStreamStore.Count, new ByteArrayEqualityComparer());
            foreach (var item in activityStreamStore)
            {
                snapshot.Add(item.Key, new Queue<Activity>(item.Value));
            }

            SortedSet<Activity> buffer = new SortedSet<Activity>(Activity.Comparer);
            var streams = feed.Streams.ToList();
            var streamsCount = streams.Count;
            var snapshotCount = snapshot.Count;

            //  Init
            for (int streamIndexInsideSubsciption = 0; streamIndexInsideSubsciption < streamsCount; streamIndexInsideSubsciption++)
            {
                if (snapshotCount <= streamIndexInsideSubsciption)
                    break;
                var streamId = streams[streamIndexInsideSubsciption];
                var activity = snapshot[streamId].Dequeue();
                buffer.Add(activity);
            }

            while (buffer.Count > 0)
            {
                Activity nextActivity = buffer.FirstOrDefault();
                buffer.Remove(nextActivity);
                var streamQueue = snapshot[nextActivity.StreamId];
                if (streamQueue.Count > 0)
                {
                    var candidate = snapshot[nextActivity.StreamId].Dequeue();
                    buffer.Add(candidate);
                }
                yield return nextActivity;
            }
        }
    }
}
