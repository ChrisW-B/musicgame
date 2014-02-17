using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace MusicGame
{
    public partial class CategorySelect : PhoneApplicationPage
    {
        bool isVoiceGame;
        public CategorySelect()
        {
            InitializeComponent();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            isVoiceGame = false;
            if (this.NavigationContext.QueryString.ContainsKey("voice"))
            {
                if (this.NavigationContext.QueryString["voice"] == "0")
                {
                    isVoiceGame = false;
                }
                else
                {
                    isVoiceGame = true;
                }
            }
        }

        private void myMusicGame_Click(object sender, RoutedEventArgs e)
        {
            if (isVoiceGame)
            {
                NavigationService.Navigate(new Uri("/MyMusicVoice.xaml", UriKind.Relative));
            }
            else
            {
                NavigationService.Navigate(new Uri("/MyMusicAlbum.xaml", UriKind.Relative));
            }
        }
        private void genreGame_Click(object sender, RoutedEventArgs e)
        {
            if (isVoiceGame)
            {
                NavigationService.Navigate(new Uri("/GenreSelect.xaml?voice=1", UriKind.Relative));
            }
            else
            {
                NavigationService.Navigate(new Uri("/GenreSelect.xaml?voice=0", UriKind.Relative));
            }
        }
    }
}