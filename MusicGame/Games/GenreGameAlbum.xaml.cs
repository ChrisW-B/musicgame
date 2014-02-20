using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Nokia.Music;
using Nokia.Music.Types;
using System.Collections.ObjectModel;
using MusicGame.ViewModels;
using Telerik.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows.Media;
using System.IO.IsolatedStorage;

namespace MusicGame
{
    public partial class GenreGameAlbum : PhoneApplicationPage
    {
        MusicClient client;
        Random rand;
        ObservableCollection<Product> topSongs;
        ObservableCollection<Product> pickedSongs;
        ObservableCollection<DataItemViewModel> albumArtList;
        ObservableCollection<SongData> winningSongList;
        bool gameOver;
        int roundPoints;
        IsolatedStorageSettings store;
        Product winningSong;
        int timesPlayed;
        int points;
        int numTimesWrong;
        int numTicks;
        DispatcherTimer playTime;
        Grid grid;
        ProgressBar progBar;
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
        const string MUSIC_API_KEY = "987006b749496680a0af01edd5be6493";

        public GenreGameAlbum()
        {
            InitializeComponent();
            initialize();
        }

        private void initialize()
        {
            client = new MusicClient(MUSIC_API_KEY);
            rand = new Random();
            pickedSongs = new ObservableCollection<Product>();
            albumArtList = new ObservableCollection<DataItemViewModel>();
            winningSongList = new ObservableCollection<SongData>();
            store = IsolatedStorageSettings.ApplicationSettings;
            topSongs = new ObservableCollection<Product>();
            playTime = new DispatcherTimer();
            playTime.Interval = new TimeSpan(0, 0, 1);   
            playTime.Tick += playTime_Tick;
            numTimesWrong = 0;
            timesPlayed = 0;
            points = 0;
            gameOver = true;
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
            pickSongs();
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

        //Pick and play the winning song
        private void pickWinner()
        {
            //picks a random song from the selected songs to be the winner
            winningSong = pickedSongs[rand.Next(pickedSongs.Count)];
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
        

        //Get a list of 12 songs with album art
        private void pickSongs()
        {

            for (int i = 0; i < 12; i++)
            {
                pickSong();
            }
            setAlbumArt();

        }
        private void pickSong()
        {
            //picks an idividual song from the list of all songs in library
            Product prod = topSongs[rand.Next(topSongs.Count)];
            BitmapImage albumArt = getBitmap(prod);
            if (albumArt != null)
            {
                if (!onList(prod))
                {
                    pickedSongs.Add(prod);
                    DataItemViewModel album = new DataItemViewModel();
                    album.ImageSource = albumArt;
                    album.Prod = prod;
                    albumArtList.Add(album);
                }
                else
                {
                    pickSong();
                }
            }
            else
            {
                pickSong();
            }
        }
        private BitmapImage getBitmap(Product prod)
        {
            return new BitmapImage(prod.Thumb320Uri);
        }
        private void setAlbumArt()
        {
            //sets up the grid of album art
            albumArtGrid.ItemsSource = albumArtList;
            albumArtGrid.SetValue(InteractionEffectManager.IsInteractionEnabledProperty, true);
            InteractionEffectManager.AllowedTypes.Add(typeof(RadDataBoundListBoxItem));
        }
        private bool onList(Product prod)
        {
            foreach (Product picked in pickedSongs)
            {
                if (picked.TakenFrom.Name == prod.TakenFrom.Name)
                {
                    return true;
                }
            }
            return false;
        }

        //check correctness
        private void Image_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            //checks to see if the correct answer was selected
            DataItemViewModel selected = ((sender as Image).DataContext as DataItemViewModel);
            albumArtGrid.SelectedItem = selected;
            if (selected.Prod == winningSong)
            {
                correctAns();
            }
            else
            {
                removeFromList(selected);
                wrongAns();
            }
        }

        private void removeFromList(DataItemViewModel selected)
        {
            int i = 0;
            foreach (DataItemViewModel item in albumArtList)
            {
                if (selected.Prod.Name == item.Prod.Name)
                {
                    break;
                }
                i++;
            }
            albumArtList.RemoveAt(i);
            pickedSongs.RemoveAt(i);
        }
        private void wrongAns()
        {
            //handles incorrect answers
            resultText.Text = "Wrong answer!";
            points--;
            numTimesWrong++;
            Points.Text = points.ToString();
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
            else
            {
                numTimesWrong = 0;
                timesPlayed = 0;
                Points.Text = points.ToString();
                player.Stop();
                player.Resources.Clear();
                albumArtList.Clear();
                pickedSongs.Clear();
                reInitialize();
                pickSongs();
                pickWinner();
            }
        }
        private void reInitialize()
        {
            albumArtGrid.ItemsSource = null;
            albumArtList = null;
            pickedSongs = null;
            player.Source = null;
            albumArtList = new ObservableCollection<DataItemViewModel>();
            pickedSongs = new ObservableCollection<Product>();
        }
    }

}