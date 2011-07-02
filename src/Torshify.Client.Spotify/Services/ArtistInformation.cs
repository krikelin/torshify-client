using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using Microsoft.Practices.Prism.ViewModel;

using Torshify.Client.Infrastructure.Interfaces;

using ITorshifyTrack = Torshify.Client.Infrastructure.Interfaces.ITrack;

using ITorshifyAlbum = Torshify.Client.Infrastructure.Interfaces.IAlbum;

using ITorshifyArtist = Torshify.Client.Infrastructure.Interfaces.IArtist;

namespace Torshify.Client.Spotify.Services
{
    public class ArtistInformation : NotificationObject, IArtistInformation
    {
        #region Fields

        private readonly IArtist _artist;
        private readonly Dispatcher _dispatcher;
        private bool _isLoading;
        private string _biography;
        private ObservableCollection<BitmapSource> _portraits;
        private ObservableCollection<Track> _tracks;
        private ObservableCollection<Album> _albums;
        private ObservableCollection<Artist> _similarArtists;
        private IArtistBrowse _browse;

        #endregion Fields

        #region Constructors

        public ArtistInformation(IArtist artist, Dispatcher dispatcher)
        {
            _portraits = new ObservableCollection<BitmapSource>();
            _tracks = new ObservableCollection<Track>();
            _albums = new ObservableCollection<Album>();
            _similarArtists = new ObservableCollection<Artist>();
            _dispatcher = dispatcher;
            _artist = artist;
            _browse = _artist.Browse();
            _isLoading = !_browse.IsComplete;
            _browse.Completed += ArtistBrowseCompleted;
        }

        #endregion Constructors

        #region Properties

        public IEnumerable<ITorshifyAlbum> Albums
        {
            get
            {
                return _albums;
            }
        }

        public string Biography
        {
            get
            {
                return _biography;
            }
            private set
            {
                if (_biography != value)
                {
                    _biography = value;
                    RaisePropertyChanged("Biography");
                }
            }
        }

        public IEnumerable<BitmapSource> Portraits
        {
            get
            {
                return _portraits;
            }
        }

        public bool IsLoading
        {
            get
            {
                return _isLoading;
            }
            private set
            {
                _isLoading = value;
                RaisePropertyChanged("IsLoading");
            }
        }

        public IEnumerable<ITorshifyArtist> SimilarArtists
        {
            get
            {
                return _similarArtists;
            }
        }

        public IEnumerable<ITorshifyTrack> Tracks
        {
            get
            {
                return _tracks;
            }
        }

        #endregion Properties

        #region Methods

        private void ArtistBrowseCompleted(object sender, EventArgs e)
        {
            IArtistBrowse browse = (IArtistBrowse)sender;
            browse.Completed -= ArtistBrowseCompleted;
            Biography = browse.Biography;
            _dispatcher.BeginInvoke(new Action<IArtistBrowse>(LoadBrowseData), DispatcherPriority.Background, browse);
        }

        private void LoadBrowseData(IArtistBrowse browse)
        {
            using (browse)
            {
                foreach (var spotifyAlbum in browse.Albums)
                {
                    _albums.Add(new Album(spotifyAlbum, _dispatcher));
                }

                foreach (var spotifyTrack in browse.Tracks)
                {
                    _tracks.Add(new Track(spotifyTrack, _dispatcher));
                }

                foreach (var spotifyArtist in browse.SimilarArtists)
                {
                    _similarArtists.Add(new Artist(spotifyArtist, _dispatcher));
                }
            }

            IsLoading = false;
        }
        #endregion Methods
    }
}