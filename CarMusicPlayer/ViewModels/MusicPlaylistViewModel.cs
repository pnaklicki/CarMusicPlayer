using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarMusicPlayer.ViewModels
{
    public class MusicPlaylistViewModel
    {
        public string Name { get; private set; }
        public TimeSpan Duration { get; set; }
        public IList<MusicFileViewModel> musicFileList { get; private set; }
        public bool isPlaylistShuffled { get;  set; }

        public MusicPlaylistViewModel(string name)
        {
            Name = name;
            musicFileList = new List<MusicFileViewModel>();
        }

        public void Add(MusicFileViewModel musicFile)
        {
            musicFileList.Add(musicFile);
            Duration = Duration.Add(musicFile.Duration);
        }

        /// <summary>
        /// Method which removes element from playlist and calculates playlist duration
        /// </summary>
        /// <param name="musicFile">Element to remove</param>
        public void Remove(MusicFileViewModel musicFile)
        {
            musicFileList.Remove(musicFile);
            Duration = Duration.Subtract(musicFile.Duration);
        }
    }
}
