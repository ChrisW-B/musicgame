using Microsoft.Phone.Controls;
using Nokia.Music;
using System;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Telerik.Windows.Controls;
using Windows.System;

namespace MusicGame
{
    public partial class ResultsPage : PhoneApplicationPage
    {
        IsolatedStorageSettings store;
        ObservableCollection<SongData> results;
        ObservableCollection<SongDataWithPicture> source;
        const string MUSIC_API_KEY = "987006b749496680a0af01edd5be6493";
        MusicClient client;
        public ResultsPage()
        {
            InitializeComponent();
            client = new MusicClient(MUSIC_API_KEY);
            getResults();
        }

        private void getResults()
        {
            store = IsolatedStorageSettings.ApplicationSettings;
            results = new ObservableCollection<SongData>();
            source = new ObservableCollection<SongDataWithPicture>();
            if (store.Contains("results"))
            {
                results = (ObservableCollection<SongData>)store["results"];
                setupResults();
            }
        }

        private void setupResults()
        {
            double score = 0;
            foreach (SongData song in results)
            {
                SongDataWithPicture data = new SongDataWithPicture()
                {
                    albumUri = song.albumUri,
                    albumCover = getAlbumArt(song.albumUri),
                    seconds = song.seconds + " seconds",
                    songName = song.songName,
                    correct = song.correct,
                    uri = song.uri,
                    points = song.points + " points"
                };
                score += song.points;
                source.Add(data);
            }
            resultsList.ItemsSource = source;
            totalScore.Text = score.ToString();
            double percent;
            if (score > 0)
            {
                percent = (score / 30) * 100;
                percent = Math.Round(percent, 1);
            }
            else
            {
               percent = 0;
            }

            this.percentage.Text = percent + "%";
            resultsList.SetValue(InteractionEffectManager.IsInteractionEnabledProperty, true);
            InteractionEffectManager.AllowedTypes.Add(typeof(SongDataWithPicture));
        }

        private BitmapImage getAlbumArt(Uri uri)
        {
            BitmapImage albumArt = new BitmapImage(uri);
            albumArt.DecodePixelHeight = 200;
            albumArt.DecodePixelWidth = 200;
            return albumArt;
        }

        private async void StackPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            await Launcher.LaunchUriAsync(((sender as StackPanel).DataContext as SongDataWithPicture).uri);
        }
        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            //override the back button to prevent it from going into the game again
            e.Cancel = true;
            while (NavigationService.BackStack.Count() > 1)
            {
                NavigationService.RemoveBackEntry();
            }
            NavigationService.GoBack();
        }

    }
}