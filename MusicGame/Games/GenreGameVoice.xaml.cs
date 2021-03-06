﻿using Microsoft.Phone.Controls;
using Nokia.Music;
using Nokia.Music.Types;
using System;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using Windows.Phone.Speech.Recognition;

namespace MusicGame
{
    public partial class GenreGameVoice : PhoneApplicationPage
    {
        #region global variables

        private const string MUSIC_API_KEY = "987006b749496680a0af01edd5be6493";
        private MusicClient client;
        private Random rand;
        private ObservableCollection<Product> topSongs;
        private Product winningSong;
        private Product lastWinningSong;
        private int timesPlayed;
        private int points;
        private int numTimesWrong;
        private int numTicks;
        private bool speaking;
        private bool isRight;
        private DispatcherTimer playTime;
        private Grid grid;
        private Grid correctAnsGrid;
        private Grid wrongAnsGrid;
        private ProgressBar progBar;
        private int roundPoints;
        private bool gameOver;
        private IsolatedStorageSettings store;
        private ObservableCollection<SongData> winningSongList;
        private String genre;

        private bool openInNokiaMusic = true;

        #endregion global variables

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

        private enum AnsVisibility
        {
            On,
            Off
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
                genre = this.NavigationContext.QueryString["genre"];
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
            roundPoints = 0;
        }

        async private Task toggleProgBar(ProgBarStatus stat)
        {
            if (winningSongList.Count == 0)
            {
                if (stat == ProgBarStatus.On)
                {
                    progBar = new ProgressBar();
                    grid = new Grid();
                    TextBlock text = new TextBlock();
                    text.Text = "loading the next song";
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
            else
            {
                if (stat == ProgBarStatus.On)
                {
                    await displayData(isRight, AnsVisibility.On, lastWinningSong);
                }
                else
                {
                    await displayData(isRight, AnsVisibility.Off, lastWinningSong);
                }
            }
        }

        async private Task displayData(bool win, AnsVisibility vis, Product song)
        {
            if (vis == AnsVisibility.On)
            {
                string winStat;
                if (win)
                {
                    winStat = "Congratulations! It ";
                }
                else
                {
                    winStat = "Nope, it ";
                }
                correctAnsGrid = new Grid();
                StackPanel panel = new StackPanel();

                //add background
                SolidColorBrush backgroundColor = new SolidColorBrush(Colors.Black);
                backgroundColor.Opacity = .7;
                correctAnsGrid.Background = backgroundColor;

                //show song name
                TextBlock text = new TextBlock();
                text.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                text.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                text.TextWrapping = TextWrapping.Wrap;
                text.TextAlignment = TextAlignment.Center;
                text.Foreground = new SolidColorBrush(Colors.White);
                text.Text = winStat + "was " + song.Name + " by " + song.Performers[0].Name;
                text.Padding = new Thickness(20, 20, 20, 20);
                panel.Children.Add(text);

                //show "loading" text
                TextBlock loadingText = new TextBlock();
                loadingText.Text = "loading the next song";
                loadingText.TextAlignment = TextAlignment.Center;
                loadingText.HorizontalAlignment = HorizontalAlignment.Center;
                loadingText.VerticalAlignment = VerticalAlignment.Center;
                loadingText.Foreground = new SolidColorBrush(Colors.White);

                //show progress bar
                progBar = new ProgressBar();
                progBar.IsIndeterminate = true;
                progBar.IsEnabled = true;
                Thickness pad = new Thickness(0, 0, 0, 40);
                progBar.Padding = pad;

                //show album art
                Image img = new Image();
                img.Height = 200;
                img.Width = 200;
                BitmapImage btmp = getBitmap(song);

                if (btmp != null)
                {
                    img.Source = btmp;
                    correctAnsGrid.Children.Add(img);
                }

                panel.Children.Add(loadingText);
                panel.Children.Add(progBar);
                panel.UpdateLayout();
                correctAnsGrid.Children.Add(panel);
                ContentPanel.Children.Add(correctAnsGrid);
                this.ContentPanel.UpdateLayout();
            }
            else if (ContentPanel.Children.Contains(correctAnsGrid))
            {
                await Task.Delay(new TimeSpan(0, 0, 2));
                correctAnsGrid.Children.Clear();
                ContentPanel.Children.Remove(correctAnsGrid);
                progBar.IsIndeterminate = false;
            }
        }

        private BitmapImage getBitmap(Product prod)
        {
            return new BitmapImage(prod.Thumb320Uri);
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
                if (song.uri == winningSong.WebUri)
                {
                    return true;
                }
            }
            return false;
        }

        async private void playWinner()
        {
            await toggleProgBar(ProgBarStatus.On);
            player.Resources.Clear();
            Uri songUri = client.GetTrackSampleUri(winningSong.Id);
            player.AutoPlay = false;
            player.Source = songUri;
            player.MediaOpened += player_MediaOpened;
            player.MediaFailed += player_MediaFailed;
        }

        private async void player_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            await toggleProgBar(ProgBarStatus.Off);
        }

        private async void player_MediaOpened(object sender, RoutedEventArgs e)
        {
            await toggleProgBar(ProgBarStatus.Off);
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

        private void playTime_Tick(object sender, EventArgs e)
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

        async private void wrongAns()
        {
            //handles incorrect answers

            points--;
            roundPoints--;
            numTimesWrong++;
            Points.Text = points.ToString() + "/30 Points";
            player.Play();
            if (numTimesWrong > 2)
            {
                isRight = false;
                newBoard();
            }
            else
            {
                await displayWrongAns(AnsVisibility.On);
                await displayWrongAns(AnsVisibility.Off);
            }
        }

        async private Task displayWrongAns(AnsVisibility vis)
        {
            if (vis == AnsVisibility.On)
            {
                wrongAnsGrid = new Grid();

                /*
                //add background
                SolidColorBrush backgroundColor = new SolidColorBrush(Colors.Black);
                backgroundColor.Opacity = 0;
                correctAnsGrid.Background = backgroundColor;
                */
                //show 'x'
                Image img = new Image();
                img.Height = 200;
                img.Width = 200;
                BitmapImage btmp = new BitmapImage(new Uri("/Assets/x.png", UriKind.Relative));
                if (btmp != null)
                {
                    img.Source = btmp;
                    wrongAnsGrid.Children.Add(img);
                }
                ContentPanel.Children.Add(wrongAnsGrid);
                this.ContentPanel.UpdateLayout();
            }
            else if (ContentPanel.Children.Contains(wrongAnsGrid))
            {
                await Task.Delay(500);
                wrongAnsGrid.Children.Clear();
                ContentPanel.Children.Remove(wrongAnsGrid);
            }
        }

        private void correctAns()
        {
            //handles correct answers
            isRight = true;
            roundPoints += (5 - timesPlayed);
            points += (5 - timesPlayed);
            newBoard();
        }

        private void timeOut()
        {
            isRight = false;
            numTicks = 0;
            newBoard();
        }

        private void newBoard()
        {
            //clears the current board and creates a new one
            if (openInNokiaMusic)
            {
                winningSongList.Add(new SongData() { albumUri = winningSong.Thumb200Uri, points = roundPoints, correct = isRight, seconds = (25 - numTicks), songName = winningSong.Name, uri = winningSong.AppToAppUri });
            }
            else
            {
                winningSongList.Add(new SongData() { albumUri = winningSong.Thumb200Uri, points = roundPoints, correct = isRight, seconds = (25 - numTicks), songName = winningSong.Name, uri = winningSong.WebUri });
            }
            toggleClock(TimerStatus.Off);
            lastWinningSong = winningSong;
            if (winningSongList.Count > 5)
            {
                store["results"] = winningSongList;
                store.Save();
                if (gameOver)
                {
                    NavigationService.Navigate(new Uri("/ResultsPage.xaml?style=voice&genre=" + genre, UriKind.Relative));
                    gameOver = false;
                }
            }
            else
            {
                yourAnswer.Text = "";
                numTimesWrong = 0;
                timesPlayed = 0;
                roundPoints = 0;
                Points.Text = points.ToString() + "/30 Points";
                roundNum.Text = "Round " + (winningSongList.Count + 1) + "/6";
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
                string antiCensorship = removeProfanityMarks(result.RecognitionResult.Text);
                yourAnswer.Text = antiCensorship;
            }
            else if (result.ResultStatus == SpeechRecognitionUIStatus.Cancelled)
            {
                toggleClock(TimerStatus.On);
                player.Play();
            }
        }

        private string removeProfanityMarks(string p)
        {
            p = p.Replace("<profanity>", "");
            p = p.Replace("</profanity>", "");
            return p;
        }
    }
}