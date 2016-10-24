using BackgroundMusicPlayer;
using CarMusicPlayer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace CarMusicPlayer
{
    public class MusicPlayerData
    {
        public MusicPlaylistViewModel CurrentPlaylist { get; private set; }
        public List<MusicPlaylistViewModel> MusicPlaylists { get; private set; }
        public MusicPlaylistViewModel MainPlaylist { get; private set; }
        public MusicFileViewModel CurrentMusicFile { get; private set; }

        private static MusicPlayerData instance = null;

        private MusicPlayerData()
        {
            MusicPlaylists = new List<MusicPlaylistViewModel>();
        }

        /// <summary>
        /// Method for getting instance of class; ensures class is a singleton
        /// </summary>
        public static MusicPlayerData Instance
        {
            get
            {
                if(instance == null)
                {
                    instance = new MusicPlayerData();
                }
                return instance;
            }
        }

        /// <summary>
        /// Method for adding given music file to given playlist
        /// and notyfing background player about made changes
        /// </summary>
        /// <param name="musicFile">Music file to add to playlist</param>
        /// <param name="playlist">Playlist for music file to be added</param>
        public async void AddMusicFileToPlaylist(MusicFileViewModel musicFile, MusicPlaylistViewModel playlist)
        {
            if (!playlist.musicFileList.ToList().Exists(m => m
             == musicFile))
            {
                playlist.Add(musicFile);
                App.SendMessageToBackground(MessageType.AddToPlaylist, playlist.Name + "\t" + musicFile.Id);
            }
            else
            {
                await new MessageDialog("Wybrany utwór już znajduje się w liście odtwarzania!", "Błąd").ShowAsync();
            }
        }

        public void RemoveMusicFileFromPlaylist(MusicFileViewModel musicFile,
            MusicPlaylistViewModel playlist)
        {
            if (playlist.musicFileList.ToList().Exists(m => m
             == musicFile))
            {
                playlist.Remove(musicFile);
                App.SendMessageToBackground(MessageType.RemoveFromPlaylist, playlist.Name + "\t" + musicFile.Id);
            }
        }

        /// <summary>
        /// Method for removing playlist and notyfing background
        /// music player about made changes
        /// </summary>
        /// <param name="playlist"></param>
        public void RemovePlaylist(MusicPlaylistViewModel playlist)
        {
            if(playlist != null)
            {
                MusicPlaylists.Remove(playlist);
                App.SendMessageToBackground(MessageType.RemovePlaylist, playlist.Name);
                MainPage.Instance.Refresh();
            }
        }

        /// <summary>
        /// Method which creates new playlist with given name and 
        /// notyfies background about new playlist
        /// </summary>
        /// <param name="playlistName">Playlist name</param>
        /// <returns>True if playlist was succesfully created</returns>
        public bool CreatePlaylist(string playlistName)
        {
            bool ifSuccess = false;
            if (!MusicPlaylists.Exists(m => m.Name.ToLower() == playlistName.ToLower()))
            {
                ifSuccess = true;
                MusicPlaylists.Add(new MusicPlaylistViewModel(playlistName));
                App.SendMessageToBackground(MessageType.NewPlaylist, playlistName);
                MainPage.Instance.Refresh();
            }
            return ifSuccess;
        }

        /// <summary>
        /// Method which adds given playlist to playlists list;
        /// Used mainly for initialization
        /// </summary>
        /// <param name="playlist">Playlist to add</param>
        public void AddPlaylist(MusicPlaylistViewModel playlist)
        {
            if (!MusicPlaylists.Exists(m => m.Name == playlist.Name))
            {
                MusicPlaylists.Add(playlist);
                App.SendPlaylistDataToBackground(playlist);
            }
            MainPage.Instance.SetPlaylists(MusicPlaylists);
        }

        /// <summary>
        /// Method which removes all playlists and notyfies background
        /// player about made changes
        /// </summary>
        public void RemoveAllPlaylists()
        {
            MusicPlaylists.Clear();
            App.SendMessageToBackground(MessageType.RemovePlaylist, 0.ToString());
            MainPage.Instance.Refresh();
        }

        /// <summary>
        /// Method which sets currently played music file to given music file
        /// and notifies background player
        /// </summary>
        /// <param name="musicFile">Music file to set as current</param>
        public void SetCurrentMusicFile(MusicFileViewModel musicFile)
        {
            CurrentMusicFile = musicFile;
            App.SendMessageToBackground(MessageType.MediaPlaybackItem, CurrentMusicFile.Id);
         }

        /// <summary>
        /// Method which sets main playlist from given playlist
        /// </summary>
        /// <param name="playlist">Playlist to set as main</param>
        public void SetMainPlaylist(MusicPlaylistViewModel playlist)
        {
            MainPlaylist = playlist;
            MainPage.Instance.SetMainPlaylistItem(MainPlaylist);
        }

        /// <summary>
        /// Method which sets currently played playlist and notyfies
        /// background player
        /// </summary>
        /// <param name="playlist">Playlist to set as current</param>
        public void SetCurrentPlaylist(MusicPlaylistViewModel playlist)
        {
            CurrentPlaylist = playlist;
            App.SendMessageToBackground(MessageType.CurrentPlaylist, CurrentPlaylist.Name);
        }
    }
}
