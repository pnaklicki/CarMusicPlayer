using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.Storage;

namespace CarMusicPlayer.ViewModels
{
    public class MusicFileViewModel
    {
        public string Id { get; set; }
        public TimeSpan Duration { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }

        public MusicFileViewModel()
        {
            Duration = new TimeSpan();
        }
    }
}
