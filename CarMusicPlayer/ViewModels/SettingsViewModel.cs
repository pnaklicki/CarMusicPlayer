using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Windows.Storage;
using Windows.Storage.Search;

namespace BackgroundMusicPlayer
{
    class SettingsViewModel
    {
        private static SettingsViewModel instance = null;

        private SettingsViewModel()
        {
        }

        /// <summary>
        /// Getter for settings instance; ensures settings are a 
        /// singleton
        /// </summary>
        public static SettingsViewModel Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SettingsViewModel();
                }
                return instance;
            }
        }
    }
}
