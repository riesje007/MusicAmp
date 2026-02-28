using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;

namespace PlaylistEditing
{
    public class Playlist : INotifyCollectionChanged, IEnumerable<PlaylistItem>, INotifyPropertyChanged
    {
        private SortedList<int, PlaylistItem> _items = new SortedList<int, PlaylistItem>();
        public const string FileNotFoundTitle = "!!! File not found !!!";

        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public static Playlist? LoadPlaylist(FileInfo playlistFile)
        {
            Playlist? playlist = null;

            string fileContents = File.ReadAllText(playlistFile.FullName);
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(fileContents));
            using (StreamReader sr = new StreamReader(ms))
            {
                try
                {
                    if (sr.ReadLine() != "#EXTM3U")
                        return null;

                    playlist = new Playlist();
                    string? tag = string.Empty;
                    string? location = string.Empty;

                    while (!sr.EndOfStream && tag is not null && location is not null)
                    {
                        tag = sr.ReadLine();
                        if (sr.EndOfStream)
                            break;
                        location = sr.ReadLine();
                        PlaylistItem? item = GetItem(tag, location, playlistFile);
                        if (item is not null)
                            playlist.AddItem(item);
                    }
                }
                catch (IOException) { }
            }

            return playlist;
        }

        private static PlaylistItem? GetItem(string? tag, string? location, FileInfo playlistFile)
        {
            PlaylistItem? item = null;
            if (string.IsNullOrEmpty(tag) || !tag.StartsWith("#EXTINF:") || !tag.Contains(',') || string.IsNullOrEmpty(location))
                return null;

            var tags = tag.Split(',');
            if (tags is null || tags.Length != 2)
                return null;

            var tagParts = tags[0].Split(':');
            if (tagParts is null || tagParts.Length != 2 || string.IsNullOrEmpty(tags[1]))
                return null;

            if (Uri.TryCreate(uriString: location, uriKind: UriKind.Absolute, result: out Uri? uri))
            {
                if (uri is not null && uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    item = new PlaylistItem(0, tags[1].Trim(), 0, uri);
                else
                    item = CreateItem(location, playlistFile);
            }
            else
                item = CreateItem(location, playlistFile);

            return item;
        }

        private static PlaylistItem CreateItem(string location, FileInfo playlistLocation)
        {
            FileInfo locationInfo = new FileInfo(location);
            if (locationInfo.Exists)
                return new PlaylistItem(locationInfo);

            var newLocation = playlistLocation.Directory is null ? locationInfo : new FileInfo(Path.Combine(playlistLocation.Directory.FullName, location.ToString()));
            if (newLocation.Exists)
                return new PlaylistItem(newLocation);

            return new PlaylistItem()
            {
                SongTitle = FileNotFoundTitle,
                SongArtist = Path.IsPathRooted(location) ? locationInfo.Name : newLocation.Name,
                SongTrackNumber = 0
            };
        }

        public void SavePlaylist(FileInfo fileInformation)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(ms))
                {
                    sw.WriteLine("#EXTM3U");
                    foreach (var item in _items)
                    {
                        PlaylistItem pi = item.Value;
                        if (pi.SongTitle == FileNotFoundTitle)
                            continue;
                        if (pi.IsStream)
                        {
                            sw.WriteLine($"#EXTINF:-1,{pi.SongTitle}");
                            sw.WriteLine(pi.StreamUri!.OriginalString);
                        }
                        else
                        {
                            sw.WriteLine($"#EXTINF:{pi.SongDurationSeconds:0},{pi.SongArtist} - {pi.SongTitle}");
                            string relativePath = Path.GetRelativePath(fileInformation.DirectoryName ?? string.Empty, pi.FileInformation!.FullName);
                            sw.WriteLine(relativePath);
                        }
                    }
                    sw.Flush();
                }
                File.WriteAllText(fileInformation.FullName, Encoding.UTF8.GetString(ms.ToArray()));
            }
        }

        public IList<int> Keys => _items.Keys;
        public IList<PlaylistItem> Values => _items.Values;

        public int Count
        {
            get => field;
            private set
            {
                field = value;
                OnPropertyChanged(nameof(Count));
            }
        }

        public int AddItem(PlaylistItem item)
        {
            int key = _items.Count > 0 ? _items.Keys[^1] + 1 : 1;
            item.SongTrackNumber = key;
            _items.Add(key, item);
            int index = _items.IndexOfKey(key);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
            Count = _items.Count;
            return key;
        }

        public bool RemoveItem(int key)
        {
            bool removed = false;

            if (_items.TryGetValue(key, out var item))
            {
                int index = _items.IndexOfKey(key);
                removed = _items.Remove(key);
                if (removed)
                {
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
                    Count = _items.Count;
                }
            }
            if (removed)
            {
                SortedList<int, PlaylistItem> newItems = new SortedList<int, PlaylistItem>();
                int counter = 1;
                foreach (var it in _items)
                {
                    PlaylistItem newItem = it.Value;
                    newItem.SongTrackNumber = counter;
                    newItems.Add(counter, newItem);
                    counter++;
                }
                _items = newItems;
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
            return removed;
        }

        public PlaylistItem? GetItem(int key)
        {
            _items.TryGetValue(key, out var item);
            return item;
        }

        public void Clear()
        {
            _items.Clear();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            Count = 0;
        }

        public bool UpdateItem(int key, PlaylistItem newItem)
        {
            bool updated = false;
            if (_items.ContainsKey(key))
            {
                int index = _items.IndexOfKey(key);
                var oldItem = _items[key];
                _items[key] = newItem;
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItem, oldItem, index));
                updated = true;
            }

            return updated;
        }

        private bool SwapItems(int key1, int key2)
        {
            bool success = false;
            if (_items.ContainsKey(key1) && _items.ContainsKey(key2))
            {
                int index1 = _items.IndexOfKey(key1);
                int index2 = _items.IndexOfKey(key2);

                var old1 = _items[key1];
                var old2 = _items[key2];

                _items[key1].SongTrackNumber = key2;
                _items[key2].SongTrackNumber = key1;
                _items[key1] = old2;
                _items[key2] = old1;

                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItem: old2, oldItem: old1, index: index1));
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItem: old1, oldItem: old2, index: index2));
                success = true;
            }

            return success;
        }

        public bool MoveItem(int oldKey, int newKey)
        {
            if (!_items.ContainsKey(0))
            {
                oldKey++;
                newKey++;
            }
            bool success = _items.ContainsKey(oldKey) && _items.ContainsKey(newKey);

            if (success)
            {
                for (int key = Math.Min(oldKey, newKey); key <= Math.Max(oldKey, newKey); key++)
                {
                    if (key == oldKey)
                        continue;
                    if (!SwapItems(key, key + 1))
                        return false;
                }
            }

            return success;
        }

        public void Randomize()
        {
            SortedList<int, PlaylistItem> newItems = new SortedList<int, PlaylistItem>();
            int rnd = 0;
            foreach (var item in _items)
            {
                do
                {
                    rnd = Random.Shared.Next(1, _items.Count + 1);
                } while (newItems.ContainsKey(rnd));
                PlaylistItem newItem = item.Value;
                newItem.SongTrackNumber = rnd;
                newItems.Add(rnd, newItem);
            }
            _items = newItems;
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public IEnumerator<PlaylistItem> GetEnumerator()
        {
            return _items.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
