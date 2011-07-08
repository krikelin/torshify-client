using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using Moq;
using NUnit.Framework;
using Torshify.Client.Infrastructure.Interfaces;
using Torshify.Client.Infrastructure.Models;

namespace Torshify.Client.Tests
{
    [TestFixture]
    public class PlayerQueueTest
    {
        private PlayerQueue _queue;

        [SetUp]
        public void Setup()
        {
            _queue = new PlayerQueue(Dispatcher.CurrentDispatcher);
        }

        [Test]
        public void Current_NoSongsAdded_IsNull()
        {
            Assert.IsNull(_queue.Current);
        }

        [Test]
        public void Set_OneTrack_CanGoNext()
        {
            var track = GetTrack();
            _queue.Set(new[] { track });
            Assert.IsTrue(_queue.CanGoNext);
        }

        [Test]
        public void Set_OneTrack_CannotGoPrevious()
        {
            var track = GetTrack();
            _queue.Set(new[] { track });
            Assert.IsFalse(_queue.CanGoPrevious);
        }

        [Test]
        public void Set_PlayedAll_PlaylistEmpty()
        {
            var tracks = GetTracks(5);
            _queue.Set(tracks);
            Assert.AreEqual(tracks.Count(), _queue.Left.Count());

            for (int i = 0; i < tracks.Count(); i++)
            {
                _queue.Next();
                Assert.AreEqual(tracks.Count() - (i + 1), _queue.Left.Count());
            }

            Assert.IsFalse(_queue.CanGoPrevious);
        }

        [Test]
        public void Set_PlayedAllButOne_CanGoPreviousToStart()
        {
            var tracks = GetTracks(5);
            _queue.Set(tracks);
            Assert.AreEqual(tracks.Count(), _queue.Left.Count());

            for (int i = 0; i < tracks.Count() - 1; i++)
            {
                _queue.Next();
                Assert.AreEqual(tracks.Count() - (i + 1), _queue.Left.Count());
            }

            while (_queue.Left.Count() != tracks.Count())
            {
                bool result = _queue.Previous();
                Console.WriteLine(_queue.Left.Count());
                Assert.IsTrue(result);
            }

            Assert.IsFalse(_queue.CanGoPrevious);
        }

        [Test]
        public void Set_PlayedAllButOne_CanGoPrevious()
        {
            var tracks = GetTracks(5);
            _queue.Set(tracks);
            Assert.AreEqual(tracks.Count(), _queue.Left.Count());

            for (int i = 0; i < tracks.Count() - 1; i++)
            {
                _queue.Next();
                Assert.AreEqual(tracks.Count() - (i + 1), _queue.Left.Count());
            }

            Assert.IsTrue(_queue.CanGoPrevious);
        }

        [Test]
        public void Set_MultipleQueueMultiple_QueuedSongsShouldStartNext()
        {
            _queue.Set(GetTracks(5));
            _queue.Enqueue(new[] { GetTrack("Queue0"), GetTrack("Queue1"), GetTrack("Queue2") });
            _queue.Next();

            Assert.AreEqual("Queue0", _queue.Left.First().Track.Name);
            Assert.AreEqual("Queue0", _queue.Current.Track.Name);
        }

        [Test]
        public void Set_StartNewPlaylist_PlaylistShouldBeTheLastSet()
        {
            _queue.Set(GetTracks(5));
            _queue.Next();
            _queue.Next();
            _queue.Next();

            _queue.Set(new []{GetTrack("Song1"), GetTrack("Song2"), GetTrack("Song3")});

            Assert.AreEqual("Song1", _queue.Left.First().Track.Name);
            Assert.AreEqual("Song1", _queue.Current.Track.Name);

            _queue.Next();

            Assert.AreEqual("Song2", _queue.Left.First().Track.Name);
            Assert.AreEqual("Song2", _queue.Current.Track.Name);
        }

        [Test]
        public void Enqueue_OneTrack_CanGoNext()
        {
            var track = GetTrack();
            _queue.Enqueue(track);
            Assert.IsTrue(_queue.CanGoNext);
        }

        [Test]
        public void Enqueue_OneTrack_CannotGoPrevious()
        {
            var track = GetTrack();
            _queue.Enqueue(track);
            Assert.IsFalse(_queue.CanGoPrevious);
        }

        [Test]
        public void Enqueue_OneTrack_WillBeAddedAfterCurrent()
        {
            int numberOfTracks = 10;
            IEnumerable<ITrack> tracks = GetTracks(numberOfTracks);

            _queue.Set(tracks);
            Assert.AreEqual(numberOfTracks, _queue.Left.Count());

            for (int i = 0; i < numberOfTracks / 2; i++)
            {
                Assert.AreEqual("Track" + i, _queue.Current.Track.Name);
                bool result = _queue.Next();
                Assert.IsTrue(result);
            }

            _queue.Enqueue(GetTrack("Testing queue"));
            _queue.Next();
            Assert.AreEqual("Testing queue", _queue.Current.Track.Name);
        }

        [Test]
        public void Enqueue_OneTrackThenSetMultiple_NextSongShouldBeTheQueued()
        {
            _queue.Enqueue(GetTrack("The queued"));
            _queue.Set(GetTracks(3));
            _queue.Next();

            Assert.AreEqual("The queued", _queue.Left.First().Track.Name);
            Assert.AreEqual("The queued", _queue.Current.Track.Name);
        }

        [Test]
        public void Enqueue_OneTrackThenSetMultiple_PlayQueuedShouldBeAbleToGoPrevious()
        {
            _queue.Enqueue(GetTrack("The queued"));
            _queue.Set(GetTracks(3));
            _queue.Next();

            Assert.IsTrue(_queue.CanGoPrevious);

            var result = _queue.Previous();
            Assert.IsTrue(result);

            Assert.AreEqual("Track0", _queue.Current.Track.Name);
        }

        [Test]
        public void Enqueue_OneTrackThenSetMultiple_PlayQueuedShouldBeAbleToGoPrevious_AndNextAgainShouldBeNotTheQueued()
        {
            _queue.Enqueue(GetTrack("The queued"));
            _queue.Set(GetTracks(3));
            _queue.Next();

            Assert.IsTrue(_queue.CanGoPrevious);

            var result = _queue.Previous();
            Assert.IsTrue(result);

            Assert.AreEqual("Track0", _queue.Current.Track.Name);

            _queue.Next();

            Assert.AreEqual("Track1", _queue.Left.First().Track.Name);
            Assert.AreEqual("Track1", _queue.Current.Track.Name);
        }

        [Test]
        public void MoveCurrentTo_CurrentIsTheExpected()
        {
            _queue.Set(GetTracks(20));
            var expected = _queue.Left.Skip(10).FirstOrDefault();
            bool result = _queue.MoveCurrentTo(expected);

            Assert.IsTrue(result);
            Assert.AreEqual(expected, _queue.Current);
        }

        private ITrack GetTrack(string name = "TrackName")
        {
            Mock<ITrack> trackMock = new Mock<ITrack>();
            trackMock.SetupGet(t => t.Name).Returns(name);
            return trackMock.Object;
        }

        private IEnumerable<ITrack> GetTracks(int count = 10)
        {
            List<ITrack> tracks = new List<ITrack>();
            for (int i = 0; i < count; i++)
            {
                tracks.Add(GetTrack("Track" + i));
            }
            return tracks;
        }
    }
}