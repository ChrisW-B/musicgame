using Microsoft.Phone.Controls;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using Telerik.Windows.Controls;
using Windows.System;

namespace MusicGame
{
    public partial class MenuPage : PhoneApplicationPage
    {
        public MenuPage()
        {
            InitializeComponent();
        }

        private void myMusicGame_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/MyMusic.xaml", UriKind.Relative));
        }
        private void genreGame_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/GenreSelect.xaml", UriKind.Relative));
        }
        private void scores_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/HighScores.xaml", UriKind.Relative));
        }
    }
}