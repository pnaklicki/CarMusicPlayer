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
    class Settings
    {
        private static Settings instance = null;
        private StorageFolder settingsFolder;
        private StorageFolder playlistsFolder;

        private const string settingsFolderName = "Settings";
        private const string playlistsFolderName = "Playlists";
        //XML
        //Tags
        private const string ROOT_TAG = "playlists";
        private const string PLAYLIST_TAG = "playlist";
        private const string MUSICFILE_TAG = "musicfile";
        private const string PLAYBACK_TAG = "playback";
        //Values
        private const string NAME_ATTR = "name";
        private const string ID_ATTR = "id";
        private const string SECONDSREPEAT_ATTR = "secondsbeforerepeat";

        private Settings()
        {
            SetSettingsFolders();
        }

        /// <summary>
        /// Getter for settings instance; ensures settings are a 
        /// singleton
        /// </summary>
        public static Settings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Settings();
                }
                return instance;
            }
        }

        public async void LoadSettings()
        {
            StorageFile settingsFile = await settingsFolder.TryGetItemAsync("Settings.xml") as StorageFile;
            if(settingsFile != null)
            {
                try
                {
                    var settingsStream = await settingsFile.OpenAsync(FileAccessMode.Read);

                    try
                    {
                        XDocument document = XDocument.Load(settingsStream.AsStreamForRead());
                        XElement rootNode = document.Root;
                        XElement playbackNode = rootNode.Element(PLAYBACK_TAG);

                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }

                    //Close the stream
                    settingsStream.Dispose();
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }

        /// <summary>
        /// Method which gets folders structure for settings and creates
        /// them is there are no folders
        /// </summary>
        private async void SetSettingsFolders()
        {
            try
            {
                settingsFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(settingsFolderName, CreationCollisionOption.OpenIfExists);
                playlistsFolder = await settingsFolder.CreateFolderAsync(playlistsFolderName, CreationCollisionOption.OpenIfExists);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        /// <summary>
        /// Method which saves given playlist to file
        /// </summary>
        /// <param name="playlist">Playlist to save</param>
        public async void SavePlaylists(List<MusicPlaylist> playlists)
        {
            StorageFile playlistFile = await playlistsFolder.CreateFileAsync("Playlists.xml", CreationCollisionOption.ReplaceExisting);
            var playlistsStream = await playlistFile.OpenAsync(FileAccessMode.ReadWrite);
            XDocument document = null;

            try
            {
                document = new XDocument();
                XElement rootNode = new XElement(ROOT_TAG);
                document.AddFirst(rootNode);

                foreach (var playlist in playlists)
                {
                    //Add playlist tag and name attribute
                    XElement newPlaylistTag = new XElement(PLAYLIST_TAG);
                    newPlaylistTag.SetAttributeValue(NAME_ATTR, playlist.Name);
                    foreach (var musicFile in playlist.musicFileList)
                    {
                        //Add every music file as a single tag
                        XElement musicFileTag = new XElement(MUSICFILE_TAG);
                        musicFileTag.SetAttributeValue(ID_ATTR, musicFile.Id);
                        newPlaylistTag.Add(musicFileTag);
                    }
                    rootNode.Add(newPlaylistTag);
                }

                document.Save(playlistsStream.AsStreamForWrite());
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            //Close the stream
            playlistsStream.Dispose();
        }

        /// <summary>
        /// Loads playlist xml file, reads it and returns
        /// dictionary containing playlist name as a key and
        /// music files id list as a value
        /// </summary>
        /// <returns></returns>
        public async Task<Dictionary<string, List<string>>> GetPlaylistsDataAsync()
        {
            Dictionary<string, List<string>> playlistsData = null;

            try
            {
                StorageFile playlistFile = await playlistsFolder.GetFileAsync("Playlists.xml");
                var playlistsStream = await playlistFile.OpenAsync(FileAccessMode.Read);
#if DEBUG
                var playlistFileText = await FileIO.ReadTextAsync(playlistFile);
#endif
                try
                {
                    if (playlistFile != null)
                    {

                        playlistsData = new Dictionary<string, List<string>>();
                        if (playlistsStream != null)
                        {
                            XDocument document = XDocument.Load(playlistsStream.AsStreamForRead());
                            XElement rootNode = document.Root;
                            List<XElement> playlistsNodes = rootNode.Elements(PLAYLIST_TAG).ToList();

                            foreach (var playlistNode in playlistsNodes)
                            {
                                List<string> musicFilesData = new List<string>();

                                foreach (var musicNode in playlistNode.Elements(MUSICFILE_TAG).ToList())
                                {
                                    musicFilesData.Add(musicNode.Attribute(ID_ATTR).Value);
                                }
                                playlistsData.Add(playlistNode.Attribute(NAME_ATTR).Value, musicFilesData);
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }

                //Close the stream
                playlistsStream.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            return playlistsData;
        }
    }
}
