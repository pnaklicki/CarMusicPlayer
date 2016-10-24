using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.UI.Xaml;

namespace BackgroundMusicPlayer
{
    public enum MessageType
    {
        Message,
        MediaPlaybackItem,
        CurrentPlaylist,
        MainPlaylist,
        Playlist,
        NewPlaylist,
        RemovePlaylist,
        AddToPlaylist,
        RemoveFromPlaylist
    };

    public enum Command
    {
        Play,
        Pause,
        EnterBackground,
        LeaveBackground
    };

    public sealed class MusicPlayer : IBackgroundTask
    {
        private enum Song
        {
            Next = 1,
            Specified = 0,
            Previous = -1
        };

        private MusicPlaylist mainPlaylist;
        private bool isPlayerInBackground;
        private SystemMediaTransportControls systemMediaTransportControl;
        private BackgroundTaskDeferral taskDeferral;
        private MusicPlaylist currentPlaylist;
        private List<MusicPlaylist> musicPlaylists;
        private Settings settings;
        private readonly object loadingSynchronizer = new object();
        private ManualResetEvent backgroundTaskStarted = new ManualResetEvent(false);
        private MusicFile currentItem;
        private bool isInitializationComplete = false;
        private bool isCommandFromForeground = false;

        //Constants
        private const string trackIdKey = "trackid";
        private const string titleKey = "title";
        private const string albumArtKey = "albumart";
        private const string artistKey = "artist";
        private const string dataSeparator = "\t";
        private const string filesSeparator = "\n";

        /// <summary>
        /// Method which runs music player for foreground and background
        /// </summary>
        /// <param name="taskInstance">Instance of background task</param>
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            SetupMediaPlayer();
            currentPlaylist = new MusicPlaylist("");
            musicPlaylists = new List<MusicPlaylist>();
            settings = Settings.Instance;
            SetupMediaTransportControls();
            taskDeferral = taskInstance.GetDeferral();
            taskInstance.Task.Completed += TaskCompleted;
            LoadSettings();
            backgroundTaskStarted.Set();
            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceled); // event may raise immediately before continuing thread excecution so must be at the end
        }

        

        /// <summary>
        /// Method which sets current playlist shuffle state
        /// depending on the argument
        /// </summary>
        /// <param name="isShuffle">Boolean value which decides
        /// is playlist is shuffled or not</param>
        private void SetCurrentPlaylistShuffled(bool isShuffled)
        {
            currentPlaylist.SetPlaylistShufled(isShuffled);
        }

        /// <summary>
        /// Method which loads media player settings
        /// </summary>
        private void LoadSettings()
        {
            //Lock thread before every method to ensure that data will load 
            LoadMainAudioList();
            Wait();
            LoadAllPlaylists();
            //If initialization is complete that means data was loaded fast
            //enough, there is no need to wait
            if (!isInitializationComplete)
            {
                Wait();
            }
            SendMainPlaylistDataToForeground();
            SendAllPlaylistsDataToForeground();
            SetCurrentPlaylist(mainPlaylist);
            SendCurrentPlaylistToForeground();
        }
        
        /// <summary>
        /// Method which locks thread until method Notify() is triggered
        /// </summary>
        private void Wait()
        {
            lock (loadingSynchronizer)
            {
                Monitor.Wait(loadingSynchronizer);
            }
        }

        /// <summary>
        /// Method which unlocks thread locked with Wait() method
        /// </summary>
        private void Notify()
        {
            lock (loadingSynchronizer)
            {
                Monitor.Pulse(loadingSynchronizer);
            }
        }
        
        /// <summary>
        /// Gets playlists data from settings and creates playlists
        /// object
        /// </summary>
        private async void LoadAllPlaylists()
        {
            //Initialize this list from settings file with
            //id's of music files

            //Dictionary contains playlists names as keys and lists of 
            //music files id as values
            Dictionary<string, List<string>> musicPlaylistsData = await settings.GetPlaylistsDataAsync();
            if (musicPlaylists != null)
            {
                try
                {
                    //Create playlist with given name and select
                    //items from main playlist by id
                    foreach (var playlistData in musicPlaylistsData)
                    {
                        MusicPlaylist playlist = new MusicPlaylist(playlistData.Key);

                        foreach (var musicData in playlistData.Value)
                        {
                            if (musicData != "")
                            {
                                playlist.Add(mainPlaylist.musicFileList
                                    .Where(m => m.Id == musicData).Single());
                            }
                        }
                        musicPlaylists.Add(playlist);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
            isInitializationComplete = true;
            Notify();
        }     

        /// <summary>
        /// Method which loads all music files from device
        /// </summary>
        private async void LoadMainAudioList()
        {
            mainPlaylist = new MusicPlaylist("Wszystkie utwory");
            //Create a query which will get all music files from device
            QueryOptions musicFilesQuery = new QueryOptions(CommonFileQuery.OrderByTitle, new string[] { ".mp3", ".vma" });
            musicFilesQuery.FolderDepth = FolderDepth.Deep;

            var musicFiles = await KnownFolders.MusicLibrary.CreateFileQueryWithOptions(musicFilesQuery).GetFilesAsync();

            foreach (var musicFile in musicFiles)
            {
                mainPlaylist.Add(new MusicFile(musicFile));
            }

            //Assign the list to the player
            SetCurrentPlaylist(mainPlaylist);
            Notify();
        }

        /// <summary>
        /// Method which sends main playlist to foreground
        /// </summary>
        private void SendMainPlaylistDataToForeground()
        {
            //Data starts with playlist name
            string dataToSend = mainPlaylist.Name + filesSeparator;

            foreach (var musicFile in mainPlaylist.musicFileList)
            {
                //Write song data to string and finish it with files
                //separator - data contains id, title, artist and duration
                dataToSend += musicFile.Id + dataSeparator + musicFile.Title
                    + dataSeparator + musicFile.Artist + dataSeparator
                    + musicFile.Duration.ToString(@"hh\:mm\:ss") + filesSeparator;
            }

            SendMessageToForeground(MessageType.MainPlaylist, dataToSend);
        }

        /// <summary>
        /// Method which sends all playlists data to foreground
        /// </summary>
        private void SendAllPlaylistsDataToForeground()
        {
            if (musicPlaylists != null)
            {
                foreach (var playlist in musicPlaylists)
                {
                    SendPlaylistDataToForeground(playlist);
                }
            }
        }

        /// <summary>
        /// Method which sends currently played playlist to foreground
        /// </summary>
        private void SendCurrentPlaylistToForeground()
        {
            SendMessageToForeground(MessageType.CurrentPlaylist, currentPlaylist.Name);
        }

        /// <summary>
        /// Method which prepares data of all playlists for sending
        /// to foreground
        /// </summary>
        private void SendPlaylistDataToForeground(MusicPlaylist playlistToSend)
        {
            //Data starts with playlist name
            string dataToSend = playlistToSend.Name + filesSeparator;

            foreach (var musicFile in playlistToSend.musicFileList)
            {
                //Write song data to string and finish it with files
                //separator - data contains id, title, artist and duration
                dataToSend += musicFile.Id + filesSeparator;
            }
            SendMessageToForeground(MessageType.Playlist, dataToSend);
        }

        /// <summary>
        /// Method which sets currently used music playlist
        /// </summary>
        /// <param name="playlist">Current playlist</param>
        private void SetCurrentPlaylist(MusicPlaylist playlist)
        {
            if (currentPlaylist != null)
            {
                currentPlaylist.PlaybackList.CurrentItemChanged -= PlaylistItemChanged;
            }
            currentPlaylist = playlist;
            BackgroundMediaPlayer.Current.AutoPlay = false;
            BackgroundMediaPlayer.Current.Source = currentPlaylist.PlaybackList;
            currentPlaylist.PlaybackList.CurrentItemChanged += PlaylistItemChanged;

        }

        /// <summary>
        /// Method which sends given data to foreground.
        /// Lanuches only if app is in background
        /// </summary>
        /// <param name="messageData">Data object. It can be a string 
        /// representing notification message for foreground or a
        /// MediaPlaybackItem representing an item which is currently 
        /// played</param>
        private void SendMessageToForeground(MessageType messageType, object messageData)
        {
            if (!isPlayerInBackground)
            {
                try
                {
                    //Add values to message
                    ValueSet valuesToSend = new ValueSet();
                    valuesToSend.Add(messageType.ToString(), messageData);
                    BackgroundMediaPlayer.SendMessageToForeground(valuesToSend);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(DateTime.Now.ToString() + ": {0}", ex.Message);
                }
            }
        }

        private void MessageReceivedFromForeground(object sender, MediaPlayerDataReceivedEventArgs arguments)
        {
            object message;
            try
            {
                if (arguments.Data.TryGetValue(MessageType.Message.ToString(), out message))
                {
                    //It is a command, handle it
                    Command command;
                    if (Enum.TryParse(message.ToString(), out command))
                    {
                        switch (command)
                        {
                            case Command.Play:
                                Play();
                                break;
                            case Command.Pause:
                                Pause();
                                break;
                            case Command.EnterBackground:
                                isPlayerInBackground = true;
                                break;
                            case Command.LeaveBackground:
                                isPlayerInBackground = false;
                                isCommandFromForeground = false;
                                SendMainPlaylistDataToForeground();
                                SendAllPlaylistsDataToForeground();
                                SendCurrentPlaylistToForeground();
                                SendCurrentItemToForeground();
                                break;
                        }
                    }
                }
                else if(arguments.Data.TryGetValue(MessageType.NewPlaylist.ToString(), out message))
                {
                    //New playlist created, handle it
                    MusicPlaylist newPlaylist = new MusicPlaylist(message.ToString());
                    musicPlaylists.Add(newPlaylist);
                    settings.SavePlaylists(musicPlaylists);
                }
                else if(arguments.Data.TryGetValue(MessageType.RemoveFromPlaylist.ToString(), out message))
                {
                    //Music file needs to be removed from playlist, handle it
                    string[] dataToProcess = message.ToString().Split(dataSeparator.ToCharArray().Single());
                    if (dataToProcess.Count() != 2)
                    {
                        throw new Exception("Data frame incorrect");
                    }
                    else
                    {
                        musicPlaylists.Where(m => m.Name == dataToProcess[0])
                            .Single().Remove(mainPlaylist.musicFileList.Where(m => m
                            .Id == dataToProcess[1]).Single());
                        settings.SavePlaylists(musicPlaylists);
                    }
                }
                else if(arguments.Data.TryGetValue(MessageType.AddToPlaylist.ToString(), out message))
                {
                    //Music file needs to be added to playlist, handle it
                    string[] dataToProcess = message.ToString().Split(dataSeparator.ToCharArray().Single());
                    if(dataToProcess.Count() != 2)
                    {
                        throw new Exception("Data frame incorrect");
                    }
                    else
                    {
                        musicPlaylists.Where(m => m.Name == dataToProcess[0])
                            .Single().Add(mainPlaylist.musicFileList.Where(m => m
                            .Id == dataToProcess[1]).Single());
                        settings.SavePlaylists(musicPlaylists);
                    }
                }
                else if(arguments.Data.TryGetValue(MessageType.RemovePlaylist.ToString(), out message))
                {
                    if (message.ToString().Equals("0"))
                    {
                        musicPlaylists.Clear();
                    }
                    else
                    {
                        musicPlaylists.Remove(musicPlaylists.Where(m => m.Name == message.ToString()).Single());
                    }
                    settings.SavePlaylists(musicPlaylists);
                }
                else if(arguments.Data.TryGetValue(MessageType.Playlist.ToString(),out message))
                {
                    //There is a playlist to add, handle it
                    MusicPlaylist playlist = null;
                    string dataSeparator = "\t";
                    string filesSeparator = "\n";

                    string[] playlistData = message.ToString().Split(filesSeparator.ToCharArray().Single());

                    //Validate that first element in data is name of playlist
                    if (playlistData[0].Split(dataSeparator.ToCharArray().Single()).Length != 1)
                    {
                        throw new Exception("Incorrect data frame");
                    }
                    else
                    {
                        if (musicPlaylists.Where(m => m
                         .Name == playlistData[0]).Count() == 0)
                        {
                            playlist = new MusicPlaylist(playlistData[0]);
                            //Remove first element which contains playlist name
                            List<string> playlistList = playlistData.ToList();
                            playlistList.RemoveAt(0);
                            foreach (var musicData in playlistList)
                            {
                                if (musicData != "")
                                {
                                    playlist.Add(mainPlaylist.musicFileList
                                .Where(m => m.Id == musicData).Single());
                                }
                            }
                            //Playlist is ready, add it to list
                            musicPlaylists.Add(playlist);
                        }
                        else
                        {
                            throw new Exception("Playlist already exists");
                        }
                    }
                }
                else if (arguments.Data.TryGetValue(MessageType.CurrentPlaylist.ToString(), out message))
                {
                    if (currentPlaylist.Name != message.ToString())
                    {
                        if (mainPlaylist.Name == message.ToString())
                        {
                            SetCurrentPlaylist(mainPlaylist);
                        }
                        else if (musicPlaylists.Where(m => m.Name == message.ToString()).Count() == 1)
                        {
                            SetCurrentPlaylist(musicPlaylists.Where(m => m.Name == message.ToString()).Single());
                        }
                    }
                }
                else if (arguments.Data.TryGetValue(MessageType.MediaPlaybackItem.ToString(), out message))
                {
                    //User selected music item to play, handle it
                    isCommandFromForeground = true;
                    MusicFile musicFile = currentPlaylist.musicFileList
                        .Where(m => m.Id == message.ToString()).Single();
                    if (musicFile != currentItem)
                    {
                        Play(musicFile);
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void SendCurrentItemToForeground()
        {
            if (!isCommandFromForeground)
            {
                SendMessageToForeground(MessageType.MediaPlaybackItem,
                    currentItem.Id);
                isCommandFromForeground = false;
            }
        }


        /// <summary>
        /// Method which is executen on change of currently played item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arguments"></param>
        private void PlaylistItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs arguments)
        {
            try
            {
                if (currentItem != null)
                {
                    if (arguments.NewItem != currentItem.PlaybackItem)
                    {
                        var newItem = arguments.NewItem;
                        currentItem = currentPlaylist.musicFileList
                        .Where(m => m.PlaybackItem == newItem).Single();

                        // Update the system view
                        UpdateTrackViewInfo(newItem);
                        //Send change of currently played item to foreground
                        SendCurrentItemToForeground();
                    }
                }
                else
                {
                    var newItem = arguments.NewItem;
                    currentItem = currentPlaylist.musicFileList
                        .Where(m => m.PlaybackItem == newItem).Single();

                    // Update the system view
                    UpdateTrackViewInfo(newItem);
                    //Send change of currently played item to foreground
                    SendCurrentItemToForeground();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        /// <summary>
        /// Method which refreshs media transport controls display data
        /// with data from given track
        /// </summary>
        /// <param name="musicItem">Music item to display data of</param>
        private void UpdateTrackViewInfo(MediaPlaybackItem musicItem)
        {
            try
            {
                if (musicItem == null)
                {
                    //Inform media transport controls that playback is stopped
                    systemMediaTransportControl.PlaybackStatus = MediaPlaybackStatus.Stopped;
                    systemMediaTransportControl.DisplayUpdater.Update();
                }
                else
                {
                    //Update media transport controls with data from new track
                    systemMediaTransportControl.PlaybackStatus = MediaPlaybackStatus.Playing;
                    systemMediaTransportControl.DisplayUpdater.Type = MediaPlaybackType.Music;
                    systemMediaTransportControl.DisplayUpdater.MusicProperties.Title = musicItem.Source.CustomProperties[titleKey] as string;
                    systemMediaTransportControl.DisplayUpdater.MusicProperties.Artist = musicItem.Source.CustomProperties[artistKey] as string;

                    var albumArtThumbnail = musicItem.Source.CustomProperties[albumArtKey] as StorageItemThumbnail;

                    if (albumArtThumbnail != null)
                        systemMediaTransportControl.DisplayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromStream(albumArtThumbnail.CloneStream());
                    else
                        systemMediaTransportControl.DisplayUpdater.Thumbnail = null;
                    //Apply all assigned data
                    systemMediaTransportControl.DisplayUpdater.Update();
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Method executed on media player exit
        /// </summary>
        /// <param name="sender">Background task</param>
        /// <param name="args">Task completion arguments</param>
        private void TaskCompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            taskDeferral.Complete();
        }

        /// <summary>
        /// Method executed on sudden background task cancellation i.e. launching another music player
        /// </summary>
        /// <param name="sender">Background task instance</param>
        /// <param name="reason">Reason for cancellation</param>
        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            try
            {
                backgroundTaskStarted.Reset();

                if (currentPlaylist != null)
                {
                    currentPlaylist = null;
                }

                systemMediaTransportControl.ButtonPressed -= MediaTransportControlButtonPressed;

                BackgroundMediaPlayer.Shutdown(); // shutdown media pipeline
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            taskDeferral.Complete(); // signals task completion.
        }

        /// <summary>
        /// Method which starts music playback
        /// </summary>
        /// <param name="musicToPlay">Optional: music to play</param>
        private void Play()
        {
            BackgroundMediaPlayer.Current.Play();
        }

        /// <summary>
        /// Method which plays given music file
        /// </summary>
        /// <param name="musicFile"Music file to play></param>
        private void Play(MusicFile musicFile)
        {
            uint songIndex = Convert.ToUInt32(currentPlaylist.musicFileList
                .IndexOf(musicFile));

            currentPlaylist.PlaybackList.MoveTo(songIndex);
            if(BackgroundMediaPlayer.Current.PlaybackSession.PlaybackState != MediaPlaybackState.Playing)
            {
                Play();
            }
        }

        /// <summary>
        /// Method which moves current playlist position to 
        /// last played song
        /// </summary>
        private void PreviousTrack()
        {
            currentPlaylist.PlaybackList.MovePrevious();
            if (BackgroundMediaPlayer.Current.PlaybackSession.PlaybackState != MediaPlaybackState.Playing)
            {
                Play();
            }
        }

        /// <summary>
        /// Method which moves current playlist position to
        /// next song
        /// </summary>
        private void NextTrack()
        {
            currentPlaylist.PlaybackList.MoveNext();
            if (BackgroundMediaPlayer.Current.PlaybackSession.PlaybackState != MediaPlaybackState.Playing)
            {
                Play();
            }
        }

        /// <summary>
        /// Method which pauses media playback
        /// </summary>
        private void Pause()
        {
            BackgroundMediaPlayer.Current.Pause();
        }

        /// <summary>
        /// Method which is executed on pressing a button from media transport control
        /// </summary>
        /// <param name="sender">Pressed button object</param>
        /// <param name="e">Pressed button data</param>
        private void MediaTransportControlButtonPressed(object sender, SystemMediaTransportControlsButtonPressedEventArgs e)
        {
            isCommandFromForeground = false;
            switch (e.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    Play();
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    Pause();
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    HandlePreviousButtonClick();
                    break;
                case SystemMediaTransportControlsButton.Next:
                    NextTrack();
                    break;
            }
        }

        /// <summary>
        /// Method which handles previous button click. If song is played 
        /// less than 2 seconds, song is changed to previous, otherwise song is
        /// played from begining
        /// </summary>
        private void HandlePreviousButtonClick()
        {
            if(BackgroundMediaPlayer.Current.PlaybackSession.Position > TimeSpan.FromSeconds(2))
            {
                BackgroundMediaPlayer.Current.PlaybackSession.Position = TimeSpan.FromSeconds(0);
            }
            else
            {
                PreviousTrack();
            }
        }

        /// <summary>
        /// Method which handles media player state change and updates
        /// media tranposrt controls status
        /// </summary>
        /// <param name="sender">Object which changed state</param>
        /// <param name="arguments">Parameters</param>
        void MusicPlayerStateChanged(object sender, EventArgs arguments)
        {
            switch (BackgroundMediaPlayer.Current.PlaybackSession.PlaybackState)
            {
                case MediaPlaybackState.Playing:
                    systemMediaTransportControl.PlaybackStatus = MediaPlaybackStatus.Playing;
                    break;
                case MediaPlaybackState.Paused:
                    systemMediaTransportControl.PlaybackStatus = MediaPlaybackStatus.Paused;
                    break;
                case MediaPlaybackState.None:
                    systemMediaTransportControl.PlaybackStatus = MediaPlaybackStatus.Stopped;
                    break;
                case MediaPlaybackState.Buffering:
                    systemMediaTransportControl.PlaybackStatus = MediaPlaybackStatus.Changing;
                    break;
                default:
                    systemMediaTransportControl.PlaybackStatus = MediaPlaybackStatus.Closed;
                    break;
            }
        }

        /// <summary>
        /// Method which initializes media player
        /// </summary>
        private void SetupMediaPlayer()
        {
            BackgroundMediaPlayer.Current.AutoPlay = false;
            BackgroundMediaPlayer.MessageReceivedFromForeground += MessageReceivedFromForeground;
        }

        /// <summary>
        /// Method which initializes media transport controls
        /// </summary>
        private void SetupMediaTransportControls()
        {
            systemMediaTransportControl = BackgroundMediaPlayer.Current.SystemMediaTransportControls;
            systemMediaTransportControl.ButtonPressed += MediaTransportControlButtonPressed;
            systemMediaTransportControl.IsEnabled = true;
            systemMediaTransportControl.IsPauseEnabled = true;
            systemMediaTransportControl.IsPlayEnabled = true;
            systemMediaTransportControl.IsNextEnabled = true;
            systemMediaTransportControl.IsPreviousEnabled = true;
        }
    }
}
