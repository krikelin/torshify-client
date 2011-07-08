using System;
using System.Windows.Threading;
using Torshify.Client.Infrastructure.Interfaces;
using Torshify.Client.Infrastructure.Models;

namespace Torshify.Client.Mocks
{
    public class Player : IPlayerController
    {
        #region Fields

        private bool _isPlaying;
        private PlayerQueue _playerQueue;

        #endregion Fields

        #region Constructors

        public Player(Dispatcher dispatcher)
        {
            _playerQueue = new PlayerQueue(dispatcher);
            _playerQueue.CurrentChanged += OnCurrentChanged;
        }

        #endregion Constructors

        #region Events

        public event EventHandler IsPlayingChanged;

        #endregion Events

        #region Properties

        public TimeSpan DurationPlayed
        {
            get
            {
                return TimeSpan.Zero;
            }
        }

        public bool IsPlaying
        {
            get
            {
                return _isPlaying;
            }
            private set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    OnIsPlayingChanged();
                }
            }
        }

        public IPlayerQueue Playlist
        {
            get { return _playerQueue; }
        }

        public float Volume
        {
            get; 
            set;
        }

        #endregion Properties

        #region Methods

        public void Pause()
        {
        }

        public void Play()
        {
        }

        public void Seek(TimeSpan timeSpan)
        {
        }

        public void Stop()
        {
        }

        private void OnCurrentChanged(object sender, EventArgs e)
        {
        }

        private void OnIsPlayingChanged()
        {
            var handler = IsPlayingChanged;

            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        #endregion Methods
    }
}