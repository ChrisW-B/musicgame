using System;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Windows.Phone.Speech.Recognition;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using Nokia.Music;
using System.Threading.Tasks;
using System.Windows.Media;
using Nokia.Music.Types;
using System.Windows.Input;
using System.IO.IsolatedStorage;
using Telerik.Windows.Controls;

namespace MusicGame
{
    public partial class GenreGameVoice : PhoneApplicationPage
    {

        #region global variables
        const string MUSIC_API_KEY = "987006b749496680a0af01edd5be6493";
        MusicClient client;
        Random rand;
        ObservableCollection<Product> topSongs;
        Product winningSong;
        int timesPlayed;
        int points;
        int numTimesWrong;
        int numTicks;
        bool speaking;
        DispatcherTimer playTime;
        Grid grid;
        ProgressBar progBar;
        int roundPoints;
        bool gameOver;
        IsolatedStorageSettings store;
        ObservableCollection<SongData> winningSongList;

        #endregion
        private enum ProgBarStatus
        {
            On,
            Off
        }
        private enum TimerStatus
        {
            On,
            Off,
            Pause
        }
        public GenreGameVoice()
        {
            InitializeComponent();
            initialize();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (this.NavigationContext.QueryString.ContainsKey("genre") && this.NavigationContext.QueryString.ContainsKey("name"))
            {
                String genre = this.NavigationContext.QueryString["genre"];
                String name = this.NavigationContext.QueryString["name"];
                Genre nokGenre = pickGenre(genre, name);
                setupGenre(nokGenre);
            }
        }

        //creates the genre
        private Genre pickGenre(string genre, string name)
        {
            Genre nokGenre = new Genre();
            nokGenre.Id = genre;
            nokGenre.Name = name;
            return nokGenre;
        }
        //gets the list of top songs from a genre
        async private void setupGenre(Genre nokGenre)
        {
            await getTopMusic(nokGenre);
            pickWinner();
        }

        async private Task getTopMusic(Genre nokGenre)
        {
            ListResponse<Product> songPage = await client.GetTopProductsForGenreAsync(nokGenre, Category.Track, 0, 100);

            foreach (Product prod in songPage)
            {
                topSongs.Add(prod);
            }
        }



        //Get library and other setup
        private void initialize()
        {
            store = IsolatedStorageSettings.ApplicationSettings;
            winningSongList = new ObservableCollection<SongData>();
            gameOver = true;
            client = new MusicClient(MUSIC_API_KEY);
            rand = new Random();
            playTime = new DispatcherTimer();
            playTime.Interval = new TimeSpan(0, 0, 1);
            topSongs = new ObservableCollection<Product>();
            playTime.Tick += playTime_Tick;
            numTimesWrong = 0;
            timesPlayed = 0;
            points = 0;
        }
        //check to make sure we have data
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

        private void toggleProgBar(ProgBarStatus stat)
        {
            if (stat == ProgBarStatus.On)
            {
                progBar = new ProgressBar();
                grid = new Grid();
                TextBlock text = new TextBlock();
                text.Text = "loading song";
                text.TextAlignment = TextAlignment.Center;
                text.HorizontalAlignment = HorizontalAlignment.Center;
                text.VerticalAlignment = VerticalAlignment.Center;
                text.Foreground = new SolidColorBrush(Colors.White);
                progBar.IsIndeterminate = true;
                progBar.IsEnabled = true;
                Thickness pad = new Thickness(0, 0, 0, 40);
                progBar.Padding = pad;
                SolidColorBrush brush = new SolidColorBrush(Colors.Black);
                brush.Opacity = .7;
                grid.Background = brush;
                ContentPanel.Children.Add(grid);
                grid.Children.Add(progBar);
                grid.Children.Add(text);
            }
            else if (ContentPanel.Children.Contains(grid))
            {
                progBar.IsIndeterminate = false;
                grid.Children.Remove(progBar);
                ContentPanel.Children.Remove(grid);
            }
        }



        //get and play a sample of a song
        //first ensuring that it is the right song
        //then limiting its time playing
        private void pickWinner()
        {
            //picks a random song from the selected songs to be the winner
            winningSong = topSongs[rand.Next(topSongs.Count)];
            if (alreadyPicked(winningSong))
            {
                pickWinner();
            }
            else
            {
                playWinner();
            }
        }
        private bool alreadyPicked(Product winningSong)
        {
            foreach (SongData song in winningSongList)
            {
                if (song.uri == winningSong.AppToAppUri)
                {
                    return true;
                }
            }
            return false;
        }
        private void playWinner()
        {
            toggleProgBar(ProgBarStatus.On);
            player.Resources.Clear();
            Uri songUri = client.GetTrackSampleUri(winningSong.Id);
            player.Source = songUri;
            player.MediaOpened += player_MediaOpened;
            player.MediaFailed += player_MediaFailed;
        }
        void player_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            toggleProgBar(ProgBarStatus.Off);
            resultText.Text = "Opening failed!";
        }
        void player_MediaOpened(object sender, RoutedEventArgs e)
        {
            toggleProgBar(ProgBarStatus.Off);
            playForLimit();
        }
        private bool performersAreArtists(Nokia.Music.Types.Artist[] artists, string p)
        {
            //checks whether the performer from NokMixRadio is the same as the artist from XboxMusicLib
            p = p.ToLowerInvariant();
            foreach (Nokia.Music.Types.Artist nok in artists)
            {
                if ((nok.Name.ToLowerInvariant() == p) || (("the " + nok.Name.ToLowerInvariant()) == p))
                {
                    return true;
                }

            }
            return false;
        }
        private void playForLimit()
        {
            numTimesWrong = 0;
            timesPlayed = 0;
            numTicks = 25;
            toggleClock(TimerStatus.On);
            player.Play();
        }

        private void toggleClock(TimerStatus stat)
        {
            if (stat == TimerStatus.On)
            {
                timer.IsRunning = true;
                playTime.Start();
            }
            else if (stat == TimerStatus.Off)
            {
                timer.IsRunning = false;
                numTicks = 25;
                timer.Content = numTicks;
                playTime.Stop();
            }
            else
            {
                timer.IsRunning = false;
                playTime.Stop();
            }
        }

        void playTime_Tick(object sender, EventArgs e)
        {
            timer.Content = numTicks;
            if (numTicks % 5 == 0 && numTicks != 25)
            {
                timesPlayed++;
            }
            if (numTicks < 0)
            {
                toggleClock(TimerStatus.Off);
                timeOut();
            }
            numTicks--;
        }

        //handle answers
        private void checkAnswer(string songName)
        {
            songName = removePunctuation(songName).ToUpperInvariant();
            String winningName = removePunctuation(winningSong.Name).ToUpperInvariant();
            if (songName == winningName)
            {
                correctAns();
            }
            else
            {
                player.Play();
                wrongAns();
            }
        }
        private string removePunctuation(string songName)
        {
            while (songName.Contains('.'))
            {
                songName = songName.Replace('.', ' ');
            }
            while (songName.Contains(','))
            {
                songName = songName.Replace(',', ' ');
            }
            while (songName.Contains('!'))
            {
                songName = songName.Replace('!', ' ');
            }
            while (songName.Contains('?'))
            {
                songName = songName.Replace('?', ' ');
            }
            while (songName.Contains('/'))
            {
                songName = songName.Replace('/', ' ');
            }
            while (songName.Contains(')'))
            {
                songName = songName.Replace(')', ' ');
            }
            while (songName.Contains('('))
            {
                songName = songName.Replace('(', ' ');
            }
            while (songName.EndsWith(" "))
            {
                songName = songName.Remove(songName.Length - 1);
            }
            while (songName.Contains("  "))
            {
                songName = songName.Replace("  ", " ");
            }
            return songName;
        }
        private void wrongAns()
        {
            //handles incorrect answers
            resultText.Text = "Wrong answer!";
            points--;
            numTimesWrong++;
            Points.Text = points.ToString();
            player.Play();
            if (numTimesWrong > 2)
            {
                roundPoints = 0;
                newBoard();
            }
        }
        private void correctAns()
        {
            //handles correct answers
            resultText.Text = "Correct!";
            roundPoints = (5 - timesPlayed);
            points += roundPoints;
            newBoard();
        }
        private void timeOut()
        {
            roundPoints = 0;
            resultText.Text = "Too long!";
            newBoard();
        }
        private void newBoard()
        {
            //clears the current board and creates a new one
            toggleClock(TimerStatus.Off);
            bool isRight = false;
            if (roundPoints > 0)
            {
                isRight = true;
            }
            winningSongList.Add(new SongData() { albumUri = winningSong.Thumb200Uri, points = roundPoints, correct = isRight, seconds = 25 - numTicks, songName = winningSong.Name, uri = winningSong.AppToAppUri });
            if (winningSongList.Count > 5)
            {
                store["results"] = winningSongList;
                store.Save();
                if (gameOver)
                {
                    NavigationService.Navigate(new Uri("/ResultsPage.xaml", UriKind.Relative));
                    gameOver = false;
                }
            }
            yourAnswer.Text = "";
            numTimesWrong = 0;
            timesPlayed = 0;
            Points.Text = points.ToString();
            player.Resources.Clear();
            reInitialize();
            pickWinner();
        }
        private void reInitialize()
        {
            player.Source = null;
        }
        private void giveUp_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            newBoard();
        }
        private void go_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            checkAnswer(yourAnswer.Text);
        }
        private void yourAnswer_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            player.Pause();
            toggleClock(TimerStatus.Pause);
        }
        private void yourAnswer_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!speaking)
            {
                player.Play();
                toggleClock(TimerStatus.On);
            }
        }
        private void yourAnswer_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.Focus();
                checkAnswer(yourAnswer.Text);
            }
        }
        private async void RadTextBox_ActionButtonTap(object sender, EventArgs e)
        {
            speaking = true;
            player.Pause();
            toggleClock(TimerStatus.Pause);
            SpeechRecognizerUI recognizer = new SpeechRecognizerUI();
            SpeechRecognitionUIResult result = await recognizer.RecognizeWithUIAsync();
            speaking = false;
            if (result.ResultStatus == SpeechRecognitionUIStatus.Succeeded)
            {
                yourAnswer.Text = result.RecognitionResult.Text;
            }
            else if (result.ResultStatus == SpeechRecognitionUIStatus.Cancelled)
            {
                toggleClock(TimerStatus.On);
                player.Play();
            }
        }
    }
}