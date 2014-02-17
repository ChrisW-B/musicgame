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

       
        private void scores_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/HighScores.xaml", UriKind.Relative));
        }

        private void sayIt_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/CategorySelect.xaml?voice=1", UriKind.Relative));
        }

        private void albumPick_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/CategorySelect.xaml?voice=0", UriKind.Relative));
        }
    }
}