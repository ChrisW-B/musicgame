using System;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;
using Windows.Phone.Speech.Recognition;
using Microsoft.Xna.Framework.Media;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using Nokia.Music;
using System.Threading.Tasks;
using Telerik.Windows.Controls;
using Windows.System;
using System.Windows.Media;
using Nokia.Music.Types;
using System.Windows.Input;
using System.IO.IsolatedStorage;

namespace MusicGame
{
    public partial class MyMusicVoice : PhoneApplicationPage
    {

        #region global variables
        const string MUSIC_API_KEY = "987006b749496680a0af01edd5be6493";
        int numTimesWrong;
        int points;
        int roundPoints;
        int timesPlayed;
        int numTicks;
        bool gameOver;
        bool isRight;
        Song winningSong;
        Random rand;
        MusicClient client;
        MediaLibrary songs;
        ProgressBar progBar;
        DispatcherTimer playTime;
        Grid grid;
        Uri prodUri;
        Uri albumUri;
        bool speaking;
        ObservableCollection<SongData> winningSongList;
        IsolatedStorageSettings store;
        #endregion

        public MyMusicVoice()
        {
            InitializeComponent();
            initialize();
            checkConnectionAndRun();
        }


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

        //Get library and other setup
        private void initialize()
        {
            gameOver = true;
            store = IsolatedStorageSettings.ApplicationSettings;
            winningSongList = new ObservableCollection<SongData>();
            client = new MusicClient(MUSIC_API_KEY);
            rand = new Random();
            songs = new MediaLibrary();
            playTime = new DispatcherTimer();
            playTime.Interval = new TimeSpan(0, 0, 1);
            playTime.Tick += playTime_Tick;
            numTimesWrong = 0;
            timesPlayed = 0;
            points = 0;
            roundPoints = 0;
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
        async private void checkConnectionAndRun()
        {
            toggleProgBar(ProgBarStatus.On);

            bool connected = await isConnected();
            if (!connected)
            {
                MessageBoxClosedEventArgs res = await RadMessageBox.ShowAsync("This app needs data to work, please make sure you are connected to wifi or a network. Would you like to check now?", "Cannot get song", MessageBoxButtons.YesNo);
                if (res.Result == DialogResult.OK)
                {
                    await Launcher.LaunchUriAsync(new Uri("ms-settings-wifi:"));
                }
            }
            else
            {
                pickWinner();
            }
            toggleProgBar(ProgBarStatus.Off);
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
        private void pickWinner()
        {
            //picks a random song from the selected songs to be the winner
            int numSongs = songs.Songs.Count;
            if (numSongs > 10)
            {
                winningSong = songs.Songs[rand.Next(songs.Songs.Count)];
                if (alreadyPicked(winningSong))
                {
                    pickWinner();
                }
                else
                {
                    playSong();
                }
            }
            else
            {
                resultText.Text = "Not enough songs!";
            }
        }
        private bool alreadyPicked(Song winningSong)
        {
            foreach (SongData song in winningSongList)
            {
                if (song.songName == winningSong.Name)
                {
                    return true;
                }
            }
            return false;
        }


        //get and play a sample of a song
        //first ensuring that it is the right song
        //then limiting its time playing
        async private void playSong()
        {
            //plays the winning song, unless there is a problem, in which it picks a new winner
            player.Resources.Clear();
            ListResponse<MusicItem> result = await getPossibleSong();
            if (result.Result != null && result.Count > 0)
            {
                Response<Product> prod = await getSongData(result);
                if (performersAreArtists(prod.Result.Performers, winningSong.Artist.Name))
                {
                    prodUri = prod.Result.WebUri;
                    albumUri = prod.Result.Thumb320Uri;
                    toggleProgBar(ProgBarStatus.On);
                    Uri songUri = getSongUri(prod);
                    player.Source = songUri;
                    player.MediaOpened += player_MediaOpened;
                    player.MediaFailed += player_MediaFailed;
                }
                else
                {
                    pickWinner();
                }
            }
            else
            {
                pickWinner();
            }
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
        //breakup nokia music requests
        private Uri getSongUri(Response<Product> prod)
        {
            return client.GetTrackSampleUri(prod.Result.Id);
        }
        async private Task<Response<Product>> getSongData(ListResponse<MusicItem> result)
        {
            return await client.GetProductAsync(result[0].Id);
        }
        async private Task<ListResponse<MusicItem>> getPossibleSong()
        {
            return await client.SearchAsync(winningSong.Name + " " + winningSong.Artist.Name, Category.Track, null, null, null, 0, 1);
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
            roundPoints--;
            points--;
            numTimesWrong++;
            Points.Text = points.ToString();
            player.Play();
            if (numTimesWrong > 2)
            {
                isRight = false;
                newBoard();
            }

        }
        private void correctAns()
        {
            //handles correct answers
            isRight = true;
            resultText.Text = "Correct!";
            roundPoints += (5 - timesPlayed);
            points += (5 - timesPlayed);
            newBoard();
        }


        private void timeOut()
        {
            isRight = false;
            resultText.Text = "Too long!";
            newBoard();
        }
        private void newBoard()
        {
            //clears the current board and creates a new one
            winningSongList.Add(new SongData() { albumUri = albumUri, points = roundPoints, correct = isRight, seconds = 25 - numTicks, songName = winningSong.Name, uri = prodUri });
            toggleClock(TimerStatus.Off);
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
            else
            {
                yourAnswer.Text = "";
                roundPoints = 0;
                numTimesWrong = 0;
                timesPlayed = 0;
                Points.Text = points.ToString();
                player.Resources.Clear();
                reInitialize();
                pickWinner();
            }
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