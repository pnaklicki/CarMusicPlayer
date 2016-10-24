using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using BackgroundMusicPlayer;

namespace CarMusicPlayer
{
    public sealed partial class SettingsView : Page
    {
        private static SettingsView instance = null;
        private static SettingsViewModel appSettings;


        public static SettingsView getInstance()
        {
            if(instance == null)
            {
                instance = new SettingsView();
            }
            return instance;
        }

        public SettingsView()
        {
            this.InitializeComponent();
            instance = this;
        }
    }
}
