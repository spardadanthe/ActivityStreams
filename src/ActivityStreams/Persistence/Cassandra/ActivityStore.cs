﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cassandra;

namespace ActivityStreams.Persistence.Cassandra
{
    /// <summary>
    /// Type reference: http://docs.datastax.com/en/developer/csharp-driver/2.7/csharp-driver/reference/csharpType2Cql3Datatypes_r.html
    /// </summary>
    public class ActivityStore : IActivityStore
    {
        const string AppendActivityStreamQueryTemplate = @"INSERT INTO activities_desc (sid,ts,data) VALUES (?,?,?);";

        const string LoadActivityStreamQueryTemplate = @"SELECT data FROM ""activities_desc"" where sid=? AND ts<=?;";

        readonly ISerializer serializer;

        readonly ISession session;

        public ActivityStore(ISession session, ISerializer serializer)
        {
            this.session = session;
            this.serializer = serializer;
        }

        public void Save(Activity activity)
        {
            var prepared = session.Prepare(AppendActivityStreamQueryTemplate);

            byte[] data = SerializeActivity(activity);
            session
                .Execute(prepared
                .Bind(Convert.ToBase64String(activity.StreamId), activity.Timestamp, data));
        }

        public IEnumerable<Activity> Get(Feed feed, Paging paging)
        {
            SortedSet<Activity> activities = new SortedSet<Activity>(Activity.Comparer);

            foreach (var streamId in feed.Streams)
            {
                var streamIdQuery = Convert.ToBase64String(streamId);

                var prepared = session
                        .Prepare(LoadActivityStreamQueryTemplate)
                        .Bind(streamIdQuery, paging.Timestamp)
                        .SetAutoPage(false)
                        .SetPageSize(paging.Take);

                var rowSet = session.Execute(prepared);
                foreach (var row in rowSet.GetRows())
                {
                    using (var stream = new MemoryStream(row.GetValue<byte[]>("data")))
                    {
                        var storedActivity = (Activity)serializer.Deserialize(stream);
                        activities.Add(storedActivity);
                    }
                }
            }
            return activities.Take(paging.Take);
        }

        byte[] SerializeActivity(Activity activity)
        {
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, activity);
                return stream.ToArray();
            }
        }
    }
}
