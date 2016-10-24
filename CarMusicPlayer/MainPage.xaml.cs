using BackgroundMusicPlayer;
using CarMusicPlayer.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace CarMusicPlayer
{
    public sealed partial class MainPage : Page
    {
        private static MainPage instance = null;
        private MusicPlaylistViewModel selectedPlaylistToRemove = null;
        private MusicFileViewModel selectedMusicFileToAddToPlaylist = null;

        public static MainPage Instance
        {
            get
            {
                if(instance == null)
                {
                    instance = new MainPage();
                }
                return instance;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if(e.NavigationMode == NavigationMode.Back)
            {
                SetPlaylists(App.musicPlayerData.MusicPlaylists);
                SetMainPlaylistItem(App.musicPlayerData.MainPlaylist);
                SetCurrentItem(App.musicPlayerData.CurrentMusicFile);
            }            
        }

        /// <summary>
        /// Constructor that is launched by navigation to this page
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();          
            //If navigation to this page occurs, object created by navigation
            //should be set as an instance to allow global access to this class
            instance = this;
        }

        public void Refresh()
        {
            Frame.Navigate(typeof(MainPage));
            Frame.GoBack();
        }

        public async void SetPlaylists(List<MusicPlaylistViewModel> playlists)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                playlistListview.ItemsSource = playlists;
            });
        }

        /// <summary>
        /// Method which sets current playlist to given playlist
        /// </summary>
        /// <param name="playlist">Playlist to set as current</param>
        public async void SetMainPlaylistItem(MusicPlaylistViewModel playlist)
        {
            if (playlist != null)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    mainMusicListview.ItemsSource = playlist.musicFileList;
                    SetCurrentItem(MusicPlayerData.Instance.CurrentMusicFile);
                });
            }
        }

        public async void SetCurrentItem(MusicFileViewModel currentItem)
        {
            if (currentItem != null && currentItem != App.musicPlayerData.CurrentMusicFile)
            {
                App.musicPlayerData.SetCurrentMusicFile(currentItem);
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    mainMusicListview.SelectedItem = currentItem;
                    mainMusicListview.ScrollIntoView(currentItem, ScrollIntoViewAlignment.Leading);
                });
            }
        }

        /// <summary>
        /// Method which is invoked when user changes selection in list view
        /// </summary>
        /// <param name="sender">Object that raised this event</param>
        /// <param name="e">Arguments</param>
        private async void musicListview_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mainMusicListview.SelectedItem != null)
            {
                if (App.musicPlayerData.CurrentPlaylist != App.musicPlayerData.MainPlaylist)
                {
                    App.musicPlayerData.SetCurrentPlaylist(App.musicPlayerData.MainPlaylist);
                    await Task.Delay(500);
                    App.musicPlayerData.SetCurrentMusicFile((MusicFileViewModel)mainMusicListview.SelectedItem);
                }
                else if (App.musicPlayerData.CurrentPlaylist == App.musicPlayerData.MainPlaylist 
                    && mainMusicListview.SelectedItem != App.musicPlayerData.CurrentMusicFile)
                {
                    App.musicPlayerData.SetCurrentMusicFile((MusicFileViewModel)mainMusicListview.SelectedItem);
                }
            }
        }

        /// <summary>
        /// Method which handles clicking add playlist appbar button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            addPlaylistGrid.Visibility = Visibility.Visible;
        }

        private async void savePlaylistBtn_Click(object sender, RoutedEventArgs e)
        {
            if (playlistNameTextbox.Text.Length > 2)
            {
                if (!App.musicPlayerData.CreatePlaylist(playlistNameTextbox.Text))
                {
                    await new MessageDialog("Istnieje już lista odtwarzania o podanej nazwie!", "Błąd").ShowAsync();
                }
            }
            else
            {
                await new MessageDialog("Nazwa musi zawierać minimum trzy znaki!", "Błąd").ShowAsync();
            }
        }

        private void cancelBtn_Click(object sender, RoutedEventArgs e)
        {
            playlistNameTextbox.Text = "";
            addPlaylistGrid.Visibility = Visibility.Collapsed;
        }

        private void PlaylistGrid_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            selectedPlaylistToRemove = ((Grid)sender).DataContext as MusicPlaylistViewModel;
            playlistRightTappedMenu.ShowAt(sender as Grid);
        }

        private void MenuFlyoutItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            App.musicPlayerData.RemovePlaylist(selectedPlaylistToRemove);
        }

        private void RemovePlaylistsBtn_Clicked(object sender, RoutedEventArgs e)
        {
            App.musicPlayerData.RemoveAllPlaylists();
        }

        private void ShowAddToPlaylistGrid(MusicFileViewModel musicFileChoosen)
        {
            selectedMusicFileToAddToPlaylist = musicFileChoosen;
            playlistsToAddTo.ItemsSource = App.musicPlayerData.MusicPlaylists;
            addToPlaylistGrid.Visibility = Visibility.Visible;
        }

        private void addToPlaylistBtn_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ShowAddToPlaylistGrid(((Grid)((AppBarButton)sender)
                .Parent).DataContext as MusicFileViewModel);
        }

        private void cancelAddingBtn_Click(object sender, RoutedEventArgs e)
        {
            addToPlaylistGrid.Visibility = Visibility.Collapsed;
        }

        private void playlistListview_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (playlistListview.SelectedItem != null)
            {
                Frame.Navigate(typeof(MusicPlaylistView), ((MusicPlaylistViewModel)playlistListview.SelectedItem).Name);
                playlistListview.SelectedItem = null;
            }
        }

        private void playlistsToAddTo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (playlistsToAddTo.SelectedItem != null)
            {
                App.musicPlayerData.AddMusicFileToPlaylist(selectedMusicFileToAddToPlaylist, ((MusicPlaylistViewModel)playlistsToAddTo.SelectedItem));
                playlistsToAddTo.SelectedItem = null;
                addToPlaylistGrid.Visibility = Visibility.Collapsed;
            }
        }
    }
}
