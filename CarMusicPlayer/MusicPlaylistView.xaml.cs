using CarMusicPlayer.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class MusicPlaylistView : Page
    {
        private static MusicPlaylistView instance;
        private MusicPlaylistViewModel currentPlaylist;

        public static MusicPlaylistView Instance
        {
            get
            {
                if(instance == null)
                {
                    instance = new MusicPlaylistView();
                }
                return instance;
            }
        }

        public MusicPlaylistView()
        {
            this.InitializeComponent();
            instance = this;
        }

        public async void SetCurrentItem(MusicFileViewModel currentItem)
        {
            if (currentItem != null && App.musicPlayerData.CurrentPlaylist == currentPlaylist)
            {
                App.musicPlayerData.SetCurrentMusicFile(currentItem);
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    musicListview.SelectedItem = currentItem;
                    musicListview.ScrollIntoView(currentItem,ScrollIntoViewAlignment.Leading);
                });
            }
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                string currentPlaylistName = e.Parameter.ToString();
                currentPlaylist = App.musicPlayerData.MusicPlaylists.Where(m => m
                .Name == currentPlaylistName).Single();
                musicListview.ItemsSource = currentPlaylist.musicFileList;
                playlistNameTextbox.Text = currentPlaylist.Name;
                playlistQuantityTextbox.Text = "Ilość utworów: " +
                    currentPlaylist.musicFileList.Count();
                playlistDurationTextbox.Text = "Czas trwania: "
                    + currentPlaylist.Duration
                    .ToString(@"mm\:ss");
                await Task.Delay(300);
                SetCurrentItem(App.musicPlayerData.CurrentMusicFile);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void Refresh()
        {
            Frame.Navigate(typeof(MainPage));
            Frame.GoBack();
        }

        private async void musicListview_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (musicListview.SelectedItem != null)
            {
                if(App.musicPlayerData.CurrentPlaylist != currentPlaylist)
                {
                    App.musicPlayerData.SetCurrentPlaylist(currentPlaylist);
                    await Task.Delay(500);
                    App.musicPlayerData.SetCurrentMusicFile((MusicFileViewModel)musicListview.SelectedItem);
                }
                else if (App.musicPlayerData.CurrentPlaylist == currentPlaylist && musicListview.SelectedItem != App.musicPlayerData.CurrentMusicFile)
                {
                    App.musicPlayerData.SetCurrentMusicFile((MusicFileViewModel)musicListview.SelectedItem);
                }
            }
        }

        private async void RemoveFromPlaylistBtn_Tapped(object sender, TappedRoutedEventArgs e)
        {
            MusicFileViewModel musicToDelete = ((Grid)((AppBarButton)
                sender).Parent).DataContext as MusicFileViewModel;
            if(musicToDelete != null)
            {
                if (await App.AskForConfirmation("Czy na pewno chcesz "
                    + "usunąć wybrany utwór z listy odtwarzania?"))
                {

                    App.musicPlayerData.RemoveMusicFileFromPlaylist(
                        musicToDelete, currentPlaylist);
                    Refresh();
                }
            }
        }
    }
}
