using Microsoft.Phone.Controls;
using System;
using System.Windows;
using System.Windows.Navigation;

namespace MusicGame
{
    public partial class CategorySelect : PhoneApplicationPage
    {
        private bool isVoiceGame;

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
                NavigationService.Navigate(new Uri("/Games/MyMusicVoice.xaml", UriKind.Relative));
            }
            else
            {
                NavigationService.Navigate(new Uri("/Games/MyMusicAlbum.xaml", UriKind.Relative));
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