using System.ComponentModel;
using ATL;

namespace PlaylistEditing
{
    public class PlaylistItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        private bool IsStream => FileInformation is null;

        public PlaylistItem(FileInfo file)
        {
            FileInformation = file;
            var track = new Track(file.FullName);
            SongTrackNumber = track.TrackNumber ?? 0;
            SongTitle = track.Title;
            SongAlbum = track.Album;
            SongArtist = track.Artist;
            SongDurationSeconds = track.Duration;
        }

        public PlaylistItem(int trackNumber, string title, int numSeconds, Uri streamUri)
        {
            SongTrackNumber = trackNumber;
            SongTitle = title;
            SongDurationSeconds = numSeconds;
            StreamUri = streamUri;
        }

        public FileInfo? FileInformation
        {
            get => field;
            set
            {
                field = value;
                OnPropertyChanged(nameof(FileInformation));
            }
        }

        public Uri? StreamUri
        {
            get => field;
            set
            {
                field = value;
                OnPropertyChanged(nameof(StreamUri));
            }
        }

        public string SongTitle
        {
            get => field;
            set
            {
                if (field != value && !value.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    field = value;
                    OnPropertyChanged(nameof(SongTitle));
                }
                else if (value.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    field = "--";
                }
            }
        } = string.Empty;

        public string SongArtist
        {
            get => field;
            set
            {
                if (field != value)
                    if (FileInformation is not null)
                        field = value;
                    else
                        field = StreamUri!.OriginalString;

                OnPropertyChanged(nameof(SongArtist));
            }
        } = string.Empty;

        public string SongAlbum
        {
            get => field;
            set
            {
                if (field != value)
                {
                    field = value;
                    OnPropertyChanged(nameof(SongAlbum));
                }
            }
        } = string.Empty;

        public int SongDurationSeconds
        {
            get => field;
            set
            {
                field = value;
                OnPropertyChanged(nameof(SongDurationSeconds));
            }
        } = 0;

        public int SongTrackNumber
        {
            get => field;
            set
            {
                field = value;
                OnPropertyChanged(nameof(SongTrackNumber));
            }
        } = 0;

        public string SongTrack => $"{SongTrackNumber:0}. ";
        public string PlaylistItemDisplay => GetDisplayText();

        public string DurationTextMinutes => GetTrackTime();

        public string DurationTime
        {
            get
            {
                int hours = SongDurationSeconds / 3600;
                int minutes = (SongDurationSeconds % 3600) / 60;
                int seconds = SongDurationSeconds % 60;
                TimeOnly time = new TimeOnly(hours, minutes, seconds);
                if (hours > 0)
                    return time.ToString("H:mm:ss");

                return time.ToString("mm:ss");
            }
        }



        private string GetDisplayText()
        {
            if (IsStream)
                return $"{SongTrack}{SongTitle} - {StreamUri?.OriginalString}";
            else
                return $"{SongTrack}{SongTitle} - ({SongArtist})";
        }

        private string GetTrackTime()
        {
            if (IsStream)
                return string.Empty;

            int minutes = SongDurationSeconds / 60;
            int seconds = SongDurationSeconds % 60;

            return $"{minutes:D2}:{seconds:D2}";
        }
    }
}
