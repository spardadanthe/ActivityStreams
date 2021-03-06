﻿using System.Linq;
using System.Text;
using ActivityStreams.Persistence;
using ActivityStreams.Persistence.InMemory;
using Machine.Specifications;

namespace ActivityStreams.Tests.Feeds
{
    [Subject("Feeds")]
    public class When_a_feed_stream_is_added_twice_to_a_feed
    {
        Establish context = () =>
            {
                var feedId = Encoding.UTF8.GetBytes("ownerId");
                var feedFactory = new FeedFactory(new FeedStreamRepository(new InMemoryFeedStreamStore()));
                feed = feedFactory.GG(feedId);
                var firstStreamId = Encoding.UTF8.GetBytes("streamId");
                feed.AttachStream(new FeedStream(feed.Id, firstStreamId));

                secondStreamId = Encoding.UTF8.GetBytes("streamId");
            };

        Because of = () => feed.AttachStream(new FeedStream(feed.Id, secondStreamId));

        It should_be_threated_as_a_single_subscribtion = () => feed.Streams.Count().ShouldEqual(1);

        static Feed feed;
        static byte[] secondStreamId;
    }
}
