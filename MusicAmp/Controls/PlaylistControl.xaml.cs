using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using PlaylistEditing;

namespace MusicAmp.Controls
{
    /// <summary>
    /// Interaction logic for PlaylistControl.xaml
    /// </summary>
    public partial class PlaylistControl : UserControl
    {
        public static readonly DependencyProperty SongPlaylistProperty = DependencyProperty.Register("SongPlaylist", typeof(Playlist), typeof(PlaylistControl), new PropertyMetadata(null));
        public event EventHandler<PlaylistItem>? PlaylistItemDoubleClicked;
        public event EventHandler<PlaylistItem?>? NewSelection;

        public Playlist SongPlaylist
        {
            get { return (Playlist)GetValue(SongPlaylistProperty); }
            set { SetValue(SongPlaylistProperty, value); }
        }

        public PlaylistControl()
        {
            InitializeComponent();
            DataContext = this;
            SongPlaylist = new Playlist();
        }

        public PlaylistItem? SelectNext()
        {
            var item = PlayListView.SelectedItem as PlaylistItem;
            if (item is not null)
            {
                int index = PlayListView.SelectedIndex;
                if (index < PlayListView.Items.Count - 1)
                {
                    PlayListView.SelectedIndex = index + 1;
                    item = PlayListView.SelectedItem as PlaylistItem;
                }
            }

            return item;
        }

        public PlaylistItem? SelectPrevious()
        {
            var item = PlayListView.SelectedItem as PlaylistItem;
            if (item is not null)
            {
                int index = PlayListView.SelectedIndex;
                if (index > 0)
                {
                    PlayListView.SelectedIndex = index - 1;
                    item = PlayListView.SelectedItem as PlaylistItem;
                }
            }

            return item;
        }

        public bool IsFirst()
        {  
            return PlayListView.SelectedIndex == 0; 
        }

        public bool IsLast()
        {
            return PlayListView.SelectedIndex == PlayListView.Items.Count - 1 && PlayListView.Items.Count > 0;
        }

        /* Private fields and methods */
        private FileInfo? PlaylistFile;

        private async void NewPlaylistClicked(object sender, RoutedEventArgs e)
        {
            SongPlaylist.Clear();
            PlaylistFile = null;
        }

        private async void OpenFileClicked(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Playlist files (*.m3u;*.m3u8)|*.m3u;*.m3u8|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                PlaylistFile = new FileInfo(dialog.FileName);
                SongPlaylist = Playlist.LoadPlaylist(PlaylistFile) ?? new();
            }
        }

        private async void SavePlaylistClicked(object sender, RoutedEventArgs e)
        {
            if (PlaylistFile == null)
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Playlist files (*.m3u;*.m3u8)|*.m3u;*.m3u8|All files (*.*)|*.*"
                };

                if (dialog.ShowDialog() == true)
                    PlaylistFile = new FileInfo(dialog.FileName);
                else
                    return;
            }
            //SongPlaylist.SavePlaylist(PlaylistFile.FullName);
        }

        private async void SavePlaylistAsClicked(object sender, RoutedEventArgs e)
        {
            FileInfo? oldFile = PlaylistFile;
            PlaylistFile = null;
            SavePlaylistClicked(sender, e);
            if (PlaylistFile == null)
                PlaylistFile = oldFile;
        }

        private void AddFilesClicked(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Audio files (*.mp3;*.wav;*.flac)|*.mp3;*.wav;*.flac|All files (*.*)|*.*",
                Multiselect = true
            };
            if (dialog.ShowDialog() == true)
            {
                foreach (var file in dialog.FileNames)
                    SongPlaylist.AddItem(new PlaylistItem(new FileInfo(file)));
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "It was the authors specific choice to only support Windows for now.")]
        private void AddFolderClicked(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Select a folder containing audio files"
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var folderPath = dialog.FileName;
                var supportedExtensions = new[] { ".mp3", ".wav", ".flac" };
                var files = Directory.GetFiles(folderPath ?? throw new FileNotFoundException("Folder Path was null"), "*.*", SearchOption.AllDirectories)
                    .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLower()));
                foreach (var file in files)
                    SongPlaylist.AddItem(new PlaylistItem(new FileInfo(file)));
            }
        }

        private void ClearPlaylistClicked(object sender, RoutedEventArgs e)
        {
            SongPlaylist.Clear();
        }

        private void RemoveSelectedClicked(object sender, RoutedEventArgs e)
        {
            //var selectedItems = PlaylistListBox.SelectedItems.Cast</* TODO: define the type to be removed!! */>().ToList();
            //foreach (var item in selectedItems)
            //    SongPlaylist.Remove(item);
        }

        private void MoveUpClicked(object sender, RoutedEventArgs e)
        {
            // TODO: Move selected item up in the playlist
        }

        private void MoveDownClicked(object sender, RoutedEventArgs e)
        {
            // TODO: Move selected item down in the playlist
        }

        private void RandomizeClicked(object sender, RoutedEventArgs e)
        {
            // TODO: Randomize the order of items in the playlist
        }

        private void PlaylistItem_DoubleClicked(object sender, RoutedEventArgs e)
        {
            if (sender is ListViewItem lvi && lvi.DataContext is PlaylistItem item)
            {
                PlaylistItemDoubleClicked?.Invoke(this, item);
            }
        }

        private void PlayListViewScrolling(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            double newOffset = PlaylistScroller.VerticalOffset - e.Delta / 3.0;
            PlaylistScroller.ScrollToVerticalOffset(newOffset);
            e.Handled = true;
        }

        private void PlaylistSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = PlayListView.SelectedItem as PlaylistItem;
            if (item is not null)
                NewSelection?.Invoke(this, item);
        }
    }
}
