using BackgroundMusicPlayer;
using CarMusicPlayer.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.ViewManagement;
using Windows.Storage;
using Windows.UI.Popups;
using System.Threading.Tasks;

namespace CarMusicPlayer
{
    sealed partial class App : Application
    {
        public static MusicPlayerData musicPlayerData;
        private static readonly object loadingSynchronizer = new object();
        private static readonly object retrivingSettingsSynchronizer = new object();

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Method which locks thread until method Notify() is triggered
        /// </summary>
        public static void Wait()
        {
            lock (loadingSynchronizer)
            {
                Monitor.Wait(loadingSynchronizer);
            }
        }

        /// <summary>
        /// Method which unlocks thread locked with Wait() method
        /// </summary>
        public static void Notify()
        {
            lock (loadingSynchronizer)
            {
                Monitor.Pulse(loadingSynchronizer);
            }
        }

        private void SetupApp()
        {
            SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;
            musicPlayerData = MusicPlayerData.Instance;
            App.Current.EnteredBackground += AppEnteringBackground;
            App.Current.LeavingBackground += AppLeavingBackground;
            BackgroundMediaPlayer.Current.AudioCategory = MediaPlayerAudioCategory.Media;
            BackgroundMediaPlayer.MessageReceivedFromBackground += MessageReceivedFromBackground;
        }

        private void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            if (((Frame)Window.Current.Content).CanGoBack)
            {
                ((Frame)Window.Current.Content).GoBack();
                e.Handled = true;
            }
            else if (((Frame)Window.Current.Content).CurrentSourcePageType == typeof(MainPage))
            {
                Quit();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Method for exiting application after getting
        /// confirmation from user
        /// </summary>
        public async static void Quit()
        {
            if(await AskForConfirmation("Czy na pewno chcesz wyjść?"))
            { 
                Current.Exit();
            }
        }

        public static void SendPlaylistDataToBackground(MusicPlaylistViewModel playlistToSend)
        {
            const string filesSeparator = "\n";

            //Data starts with playlist name
            string dataToSend = playlistToSend.Name + filesSeparator;

            foreach (var musicFile in playlistToSend.musicFileList)
            {
                  dataToSend += musicFile.Id + filesSeparator;
            }
            SendMessageToBackground(MessageType.Playlist, dataToSend);
        }

        public static void SendMessageToBackground(MessageType messageType, object messageData)
        {
            try
            {
                //Add values to message
                ValueSet valuesToSend = new ValueSet();
                valuesToSend.Add(messageType.ToString(), messageData.ToString());
                BackgroundMediaPlayer.SendMessageToBackground(valuesToSend);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(DateTime.Now.ToString() + ": {0}", ex.Message);
            }
        }

        /// <summary>
        /// Method which asks user for confirmation
        /// </summary>
        /// <param name="message">Message to show to user</param>
        /// <param name="ifConfirmOnBackRequest">Optional: Set it to 
        /// true if you want to confirm when user cancels 
        /// message dialog</param>
        /// <param name="confirmText">Text on confirm button</param>
        /// <param name="cancelText">Text on cancel button</param>
        /// <returns>Boolean value determining if user confirmed
        /// or not</returns>
        public async static Task<bool> AskForConfirmation(string message, bool ifConfirmOnBackRequest = false,
            string confirmText = "Tak", string cancelText = "Nie")
        {
            var dialog = new MessageDialog(message, "Potwierdzenie");
            dialog.Commands
                .Add(new UICommand(confirmText) { Id = 0 });
            dialog.Commands
                .Add(new UICommand(cancelText) { Id = 1 });
            if (ifConfirmOnBackRequest)
            {
                dialog.DefaultCommandIndex = 1;
                dialog.CancelCommandIndex = 0;
            }
            else
            {
                dialog.DefaultCommandIndex = 0;
                dialog.CancelCommandIndex = 1;
            }
            var result = await dialog.ShowAsync();

            return result.Id.Equals(0);
        }

        private async void MessageReceivedFromBackground(object sender, MediaPlayerDataReceivedEventArgs arguments)
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
                            case Command.Pause:

                                break;
                            case Command.Play:

                                break;
                        }
                    }
                    else
                    {
                        Debug.WriteLine("An error occured while processing send command");
                    }
                }
                else if (arguments.Data.TryGetValue(MessageType.MediaPlaybackItem.ToString(), out message))
                {
                    await Task.Delay(400);
                    MusicFileViewModel musicFile = musicPlayerData.CurrentPlaylist
                        .musicFileList.Where(m => m.Id == message.ToString()).Single();
                    if (musicPlayerData.CurrentPlaylist == musicPlayerData.MainPlaylist)
                    {
                        MainPage.Instance.SetCurrentItem(musicFile);
                    }
                    else
                    {
                        MusicPlaylistView.Instance.SetCurrentItem(musicFile);
                    }
                }
                else if (arguments.Data.TryGetValue(MessageType.Playlist.ToString(), out message))
                {
                    //There is a playlist to add, handle it
                    MusicPlaylistViewModel playlist = null;
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
                        if (musicPlayerData.MusicPlaylists.Where(m => m
                         .Name == playlistData[0]).Count() == 0)
                        {
                            playlist = new MusicPlaylistViewModel(playlistData[0]);
                            //Remove first element which contains playlist name
                            List<string> playlistList = playlistData.ToList();
                            playlistList.RemoveAt(0);
                            foreach (var musicData in playlistList)
                            {
                                if (musicData != "")
                                {
                                    playlist.Add(musicPlayerData.MainPlaylist.musicFileList
                                .Where(m => m.Id == musicData).Single());
                                }
                            }
                            //Playlist is ready, add it to list
                            musicPlayerData.AddPlaylist(playlist);
                        }
                        else
                        {
                            throw new Exception("Playlist already exists");
                        }
                    }
                }
                else if (arguments.Data.TryGetValue(MessageType.CurrentPlaylist.ToString(), out message))
                {
                    if (musicPlayerData.MainPlaylist.Name == message.ToString())
                    {
                        musicPlayerData.SetCurrentPlaylist(musicPlayerData.MainPlaylist);
                    }
                    else if (musicPlayerData.MusicPlaylists.Where(m => m.Name == message.ToString()).Count() == 1)
                    {
                        musicPlayerData.SetCurrentPlaylist(musicPlayerData.MusicPlaylists.Where(m => m.Name == message.ToString()).Single());
                    }
                }
                else if (arguments.Data.TryGetValue(MessageType.MainPlaylist.ToString(), out message))
                {
                    //Main playlist have changed, update it
                    MusicPlaylistViewModel mainPlaylist = null;
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
                        mainPlaylist = new MusicPlaylistViewModel(playlistData[0]);
                        //Remove first element which contains playlist name
                        List<string> playlistList = playlistData.ToList();
                        playlistList.RemoveAt(0);
                        foreach (var musicData in playlistList)
                        {
                            if (musicData != "")
                            {
                                string[] musicDetails = musicData.Split(dataSeparator.ToCharArray().Single());
                                if (musicDetails.Length != 4)
                                {
                                    //Data is incomplete or corrupted, abort
                                    throw new Exception("Data is either corrupted or incomplete");
                                }
                                else
                                {
                                    MusicFileViewModel musicFile = new MusicFileViewModel();
                                    musicFile.Id = musicDetails[0];
                                    musicFile.Title = musicDetails[1];
                                    musicFile.Artist = musicDetails[2];
                                    musicFile.Duration = TimeSpan.Parse(musicDetails[3]);
                                    mainPlaylist.musicFileList.Add(musicFile);
                                }
                            }
                        }
                        //Playlist is ready, set is as current
                        musicPlayerData.SetMainPlaylist(mainPlaylist);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void AppEnteringBackground(object sender, EnteredBackgroundEventArgs arguments)
        {
            SendMessageToBackground(MessageType.Message, Command.EnterBackground);
        }

        private async void AppLeavingBackground(object sender, LeavingBackgroundEventArgs arguments)
        {
            SendMessageToBackground(MessageType.Message, Command.LeaveBackground);
            await Task.Delay(200);
            if (!((Frame)Window.Current.Content).CurrentSourcePageType
                .Equals(typeof(MusicPlaylistView)) && musicPlayerData
                .CurrentPlaylist != null && musicPlayerData
                .CurrentPlaylist != musicPlayerData.MainPlaylist)
            {
                ((Frame)Window.Current.Content).Navigate(typeof(
                    MusicPlaylistView), musicPlayerData.CurrentPlaylist
                    .Name);
            }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            SetupApp();

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }

                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            deferral.Complete();
        }
    }
}