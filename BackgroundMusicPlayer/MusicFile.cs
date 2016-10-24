using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace BackgroundMusicPlayer
{
    public sealed class MusicFile
    {
        public string Id { get; private set; }
        public StorageFile AudioFile { get; private set; }
        public TimeSpan Duration { get; private set; }
        public string Title { get; private set; }
        public string Artist { get; private set; }
        public MediaPlaybackItem PlaybackItem { get; private set; }

        private const string trackIdKey = "trackid";
        private const string titleKey = "title";
        private const string albumArtKey = "albumart";
        private const string artistKey = "artist";

        public MusicFile(StorageFile musicFile)
        {
            AudioFile = musicFile;
            LoadMusicData();
        }

        private void LoadMusicData()
        {
            MusicProperties fileProperties = AudioFile.Properties.GetMusicPropertiesAsync().AsTask().Result;
            //Path is the best ID for a file
            Id = AudioFile.Path;
            Title = string.IsNullOrWhiteSpace(fileProperties.Title) ? AudioFile.Name : fileProperties.Title;
            Artist = string.IsNullOrWhiteSpace(fileProperties.Artist) ? "Nieznany" : fileProperties.Artist;
            Duration = fileProperties.Duration;
            //var thumb = song.GetThumbnailAsync(ThumbnailMode.MusicView, 100, ThumbnailOptions.UseCurrentScale).AsTask().Result;
            var mediaSource = MediaSource.CreateFromStorageFile(AudioFile);
            mediaSource.CustomProperties[trackIdKey] = AudioFile.Path;
            mediaSource.CustomProperties[titleKey] = Title;
            mediaSource.CustomProperties[albumArtKey] = null;//TODO: add thumbnail
            mediaSource.CustomProperties[artistKey] = Artist;
            PlaybackItem = new MediaPlaybackItem(mediaSource);
        }
    }
}
