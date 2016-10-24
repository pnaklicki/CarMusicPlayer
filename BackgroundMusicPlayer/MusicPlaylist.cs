using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Playback;

namespace BackgroundMusicPlayer
{
    public sealed class MusicPlaylist
    {
        public string Name { get; private set; }
        public TimeSpan Duration { get; private set; }
        public MediaPlaybackList PlaybackList { get; private set; }
        public IList<MusicFile> musicFileList { get; private set; }
        public bool isPlaylistShuffled { get; private set; }

        /// <summary>
        /// Initializes MusicPlaylist object
        /// </summary>
        /// <param name="playlistName">Name of the playlist</param>
        public MusicPlaylist(string playlistName)
        {
            Name = playlistName;
            Duration = new TimeSpan(0, 0, 0, 0);
            PlaybackList = new MediaPlaybackList();
            musicFileList = new List<MusicFile>();
            isPlaylistShuffled = false;
        }

        /// <summary>
        /// Method which sets playlist shuffle state depending on 
        /// the argument
        /// </summary>
        /// <param name="isShuffled">Boolean value which determinates
        /// if playlist is shuffled</param>
        public void SetPlaylistShufled(bool isShuffled)
        {
            isPlaylistShuffled = isShuffled;
            PlaybackList.ShuffleEnabled = isShuffled;
        }

        /// <summary>
        /// Method which adds element to playlist and calculates playlist duration
        /// </summary>
        /// <param name="musicFile">Element to add</param>
        public void Add(MusicFile musicFile)
        {
            musicFileList.Add(musicFile);
            Duration = Duration.Add(musicFile.Duration);
            PlaybackList.Items.Add(musicFile.PlaybackItem);
        }

        /// <summary>
        /// Method which removes element from playlist and calculates playlist duration
        /// </summary>
        /// <param name="musicFile">Element to remove</param>
        public void Remove(MusicFile musicFile)
        {
            musicFileList.Remove(musicFile);
            Duration = Duration.Subtract(musicFile.Duration);
            PlaybackList.Items.Remove(musicFile.PlaybackItem);
        }
    }
}
