using Microsoft.Phone.Controls;
using Nokia.Music;
using Nokia.Music.Types;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using Telerik.Windows.Controls;
using Windows.System;

namespace MusicGame
{
    public partial class GenreSelect : PhoneApplicationPage
    {
        private const string MUSIC_API_KEY = "987006b749496680a0af01edd5be6493";
        private bool isVoiceGame;

        public GenreSelect()
        {
            InitializeComponent();
            checkConnectionAndRun();
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

        async private void checkConnectionAndRun()
        {
            ProgressBar progBar = new ProgressBar();
            progBar.IsEnabled = true;
            progBar.IsIndeterminate = true;
            ContentPanel.Children.Add(progBar);
            bool connected = await isConnected();
            if (!connected)
            {
                MessageBoxClosedEventArgs res = await RadMessageBox.ShowAsync("This app needs data to work, please make sure you are connected to wifi or a network. Would you like to check now?", "Cannot reach servers", MessageBoxButtons.YesNo);
                if (res.Result == DialogResult.OK)
                {
                    await Launcher.LaunchUriAsync(new Uri("ms-settings-wifi:"));
                }
            }
            else
            {
                getGenres();
            }
            ContentPanel.Children.Remove(progBar);
        }

        private Task<bool> isConnected()
        {
            var completed = new TaskCompletionSource<bool>();
            WebClient client = new WebClient();
            client.DownloadStringCompleted += (s, e) =>
            {
                if (e.Error == null && !e.Cancelled)
                {
                    completed.TrySetResult(true);
                }
                else
                {
                    completed.TrySetResult(false);
                }
            };
            client.DownloadStringAsync(new Uri("http://www.google.com/"));
            return completed.Task;
        }

        async private void getGenres()
        {
            MusicClient client = new MusicClient(MUSIC_API_KEY);
            ListResponse<Genre> genres = await client.GetGenresAsync();
            foreach (Genre genre in genres)
            {
                Button button = new Button();
                button.Click += button_Click;
                button.Content = genre.Name;
                button.Tag = genre.Id;
                button.BorderBrush = new SolidColorBrush(Colors.White);
                button.Foreground = new SolidColorBrush(Colors.White);
                sPanel.Children.Add(button);
            }
        }

        private void button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Button button = (sender as Button);
            String buttonName = button.Content.ToString();
            String buttonTag = button.Tag.ToString();
            if (isVoiceGame)
            {
                NavigationService.Navigate(new Uri("/Games/GenreGameVoice.xaml?genre=" + buttonTag + "&name=" + buttonName, UriKind.Relative));
            }
            else
            {
                NavigationService.Navigate(new Uri("/Games/GenreGameAlbum.xaml?genre=" + buttonTag + "&name=" + buttonName, UriKind.Relative));
            }
        }
    }
}