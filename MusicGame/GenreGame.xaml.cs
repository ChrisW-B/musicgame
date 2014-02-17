using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Nokia.Music;
using Nokia.Music.Types;
using System.Collections.ObjectModel;
using MusicGame.ViewModels;
using Telerik.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows.Media;

namespace MusicGame
{
    public partial class GenreGame : PhoneApplicationPage
    {
        MusicClient client;
        Random rand;
        ObservableCollection<Product> topSongs;
        ObservableCollection<Product> pickedSongs;
        ObservableCollection<DataItemViewModel> albumArtList;
        Product winningSong;
        int timesPlayed;
        int points;
        int numTimesWrong;
        DispatcherTimer playTimer;
        DispatcherTimer replayTimer;
        Grid grid;
        ProgressBar progBar;
        private enum ProgBarStatus
        {
            On,
            Off
        }
        const string MUSIC_API_KEY = "987006b749496680a0af01edd5be6493";

        public GenreGame()
        {
            InitializeComponent();
            setup();
        }

        private void setup()
        {
            client = new MusicClient(MUSIC_API_KEY);
            rand = new Random();
            pickedSongs = new ObservableCollection<Product>();
            albumArtList = new ObservableCollection<DataItemViewModel>();
            topSongs = new ObservableCollection<Product>();
            replayTimer = new DispatcherTimer();
            playTimer = new DispatcherTimer();
            numTimesWrong = 0;
            timesPlayed = 0;
            points = 0;
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
            startGame();
        }

        async private Task getTopMusic(Genre nokGenre)
        {

            ListResponse<Product> songPage = await client.GetTopProductsForGenreAsync(nokGenre, Category.Track, 0, 100);

            foreach (Product prod in songPage)
            {
                topSongs.Add(prod);
            }

        }

        //general game starter
        private void startGame()
        {
            pickSongs();
            pickWinner();
        }

        //Pick and play the winning song
        private void pickWinner()
        {
            //picks a random song from the selected songs to be the winner
            winningSong = pickedSongs[rand.Next(pickedSongs.Count)];
            playWinner();
        }
        private void playWinner()
        {
            toggleProgBar(ProgBarStatus.On);
            player.Resources.Clear();
            Uri songUri = client.GetTrackSampleUri(winningSong.Id);
            player.Source = songUri;
            player.MediaOpened += player_Loaded;
        }
        void player_Loaded(object sender, RoutedEventArgs e)
        {
            toggleProgBar(ProgBarStatus.Off);
            playForLimit(5);
        }
        private void playForLimit(int secs)
        {
            //limits the play time of a song to the seconds provided
            if (secs > 25)
            {
                timeOut();
            }
            else
            {
                timesPlayed = (secs / 5) - 1;
                playTimer.Interval = new TimeSpan(0, 0, secs);
                player.Play();
                playTimer.Tick += delegate(object s, EventArgs args)
                {
                    playTimer.Stop();
                    player.Stop();
                    player.Position = new TimeSpan(0, 0, 0);
                    replaySong(secs);
                };
                playTimer.Start();
            }
        }
        private void replaySong(int secs)
        {
            //replays the song after a set amount of time, and ups the play time for next time
            replayTimer.Interval = new TimeSpan(0, 0, 5);
            replayTimer.Tick += delegate(object s, EventArgs args)
            {
                replayTimer.Stop();
                playForLimit(secs + 5);
            };
            replayTimer.Start();
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
                newBoard();
            }
        }
        private void correctAns()
        {
            //handles correct answers
            resultText.Text = "Correct!";
            points = points + (5 - timesPlayed);
            newBoard();
        }
        private void timeOut()
        {
            resultText.Text = "Too long!";
            newBoard();
        }
        private void newBoard()
        {
            //clears the current board and creates a new one
            numTimesWrong = 0;
            timesPlayed = 0;
            Points.Text = points.ToString();
            if (playTimer.IsEnabled)
            {
                playTimer.Stop();
                playTimer.Interval = new TimeSpan(0, 0, 5);
            }
            if (replayTimer.IsEnabled)
            {
                replayTimer.Stop();
                replayTimer.Interval = new TimeSpan(0, 0, 5);
            }
            player.Stop();
            player.Resources.Clear();
            albumArtList.Clear();
            pickedSongs.Clear();
            reInitialize();
            pickSongs();
            pickWinner();
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