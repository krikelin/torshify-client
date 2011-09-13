using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Prism.ViewModel;

using Torshify.Client.Infrastructure;
using Torshify.Client.Infrastructure.Collections;
using Torshify.Client.Infrastructure.Commands;
using Torshify.Client.Infrastructure.Events;
using Torshify.Client.Infrastructure.Interfaces;

namespace Torshify.Client.Modules.Core.Views.Artist.Tabs
{
    public class OverviewTabItemViewModel : NotificationObject, ITabViewModel<IArtist>
    {
        #region Fields

        private SortedObservableCollection<IAlbum> _albums;
        private ICollectionView _albumsIcv;
        private IArtist _artist;
        private IEventAggregator _eventAggregator;
        private SubscriptionToken _trackMenuBarToken;
        private SubscriptionToken _tracksMenuBarToken;

        #endregion Fields

        #region Constructors

        public OverviewTabItemViewModel(IEventAggregator eventAggregator, IPlayerController player)
        {
            _eventAggregator = eventAggregator;

            Player = player;
            PlayArtistTrackCommand = new StaticCommand<ITrack>(ExecutePlayArtistTrack);
        }

        #endregion Constructors

        #region Properties

        public ICollectionView Albums
        {
            get
            {
                return _albumsIcv;
            }
            private set
            {
                _albumsIcv = value;
                RaisePropertyChanged("Albums");
            }
        }

        public IArtist Artist
        {
            get
            {
                return _artist;
            }
            private set
            {
                _artist = value;
                RaisePropertyChanged("Artist");
            }
        }

        public string Header
        {
            get
            {
                return Artist.Name;
            }
        }

        public ICommand PlayArtistTrackCommand
        {
            get;
            private set;
        }

        public IPlayerController Player
        {
            get; private set;
        }

        public Visibility Visibility
        {
            get { return Visibility.Visible; }
        }

        #endregion Properties

        #region Methods

        public void Deinitialize(NavigationContext navContext)
        {
            _eventAggregator.GetEvent<TrackCommandBarEvent>().Unsubscribe(_trackMenuBarToken);
            _eventAggregator.GetEvent<TracksCommandBarEvent>().Unsubscribe(_tracksMenuBarToken);

            Albums = null;

            if (Artist != null)
            {
                Artist.Info.FinishedLoading -= OnInfoFinishedLoading;
                Artist = null;
            }

            if (_albums != null)
            {
                _albums.Clear();
            }
        }

        public void Initialize(NavigationContext navContext)
        {
            _trackMenuBarToken = _eventAggregator.GetEvent<TrackCommandBarEvent>().Subscribe(OnTrackMenuBarEvent, true);
            _tracksMenuBarToken = _eventAggregator.GetEvent<TracksCommandBarEvent>().Subscribe(OnTracksMenuBarEvent, true);
        }

        public void SetModel(IArtist model)
        {
            Artist = model;

            if (model.Info.IsLoading)
            {
                model.Info.FinishedLoading += OnInfoFinishedLoading;
            }
            else
            {
                PrepareData();
            }

            RaisePropertyChanged("Header");
        }

        public void NavigatedTo()
        {

        }

        public void NavigatedFrom()
        {

        }

        private void ExecutePlayArtistTrack(ITrack track)
        {
            IEnumerable<ITrack> tracksToPlay = GetTracksToPlay(track);
            CoreCommands.PlayTrackCommand.Execute(tracksToPlay);
        }

        private IEnumerable<ITrack> GetTracksToPlay(ITrack track)
        {
            // This isn't working 100% because of the UI virtualization, and the caching/cleanup method i'm using for the album information.
            // This will only queue up what the view has created, which probably is just a fraction of certain artists' tracks.
            // TODO : Figure it out

            List<ITrack> tracksToPlay = new List<ITrack>();
            bool addRest = false;
            foreach (var album in _albums)
            {
                int index = album.Info.Tracks.IndexOf(track);

                if (index != -1 && addRest == false)
                {
                    tracksToPlay.AddRange(album.Info.Tracks.Skip(index));
                    addRest = true;
                    continue;
                }

                if (addRest)
                {
                    tracksToPlay.AddRange(album.Info.Tracks);
                }
            }
            return tracksToPlay;
        }

        private void OnInfoFinishedLoading(object sender, EventArgs e)
        {
            PrepareData();
        }

        private void OnTrackMenuBarEvent(TrackCommandBarModel model)
        {
            IEnumerable<ITrack> tracksToPlay = GetTracksToPlay(model.Track);

            model.CommandBar
                .AddCommand("Play", CoreCommands.PlayTrackCommand, tracksToPlay)
                .AddCommand("Queue", CoreCommands.QueueTrackCommand, model.Track);
        }

        private void OnTracksMenuBarEvent(TracksCommandBarModel model)
        {
            model.CommandBar
                .AddCommand("Play", CoreCommands.PlayTrackCommand, model.Tracks.LastOrDefault())
                .AddCommand("Queue", CoreCommands.QueueTrackCommand, model.Tracks);
        }

        private void PrepareData()
        {
            _albums = new SortedObservableCollection<IAlbum>(new AlbumComparer());

            Albums = CollectionViewSource.GetDefaultView(_albums);

            lock (_artist.Info.Albums.SyncRoot)
            {
                foreach (var album in _artist.Info.Albums)
                {
                    if (album.IsAvailable)
                    {
                        _albums.Add(album);
                    }
                }
            }
        }

        #endregion Methods

        #region Nested Types

        private class AlbumComparer : IComparer<IAlbum>
        {
            #region Methods

            public int Compare(IAlbum x, IAlbum y)
            {
                var xType = x.Type;
                var yType = y.Type;

                if (xType == yType)
                {
                    //return x.Year.CompareTo(y.Year) * -1;
                    return 0;
                }

                if (xType == AlbumType.Album)
                {
                    if (yType != AlbumType.Album)
                    {
                        return -1;
                    }
                }

                if (xType == AlbumType.Compilation)
                {
                    if (yType == AlbumType.Album || yType == AlbumType.Single)
                    {
                        return 1;
                    }

                    return -1;
                }

                if (xType == AlbumType.Single)
                {
                    if (yType != AlbumType.Album)
                    {
                        return -1;
                    }

                    return 1;
                }

                return 0;
            }

            #endregion Methods
        }

        #endregion Nested Types
    }
}