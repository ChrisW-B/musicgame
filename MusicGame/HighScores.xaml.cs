using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;

namespace MusicGame
{
    public partial class HighScores : PhoneApplicationPage
    {
        
        public HighScores()
        {
            InitializeComponent();
        }
        

        private void album_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/HighScorePivot.xaml?style=album", UriKind.Relative));
        }

        private void voice_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/HighScorePivot.xaml?style=voice", UriKind.Relative));
        }

        private void clear_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            IsolatedStorageSettings store = IsolatedStorageSettings.ApplicationSettings;
            if (store.Contains("HighScoreList"))
            {
                store.Remove("HighScoreList");
            }
        }
    }
}