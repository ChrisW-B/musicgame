using Microsoft.Phone.Controls;
using Microsoft.Xna.Framework.Media;
using MusicGame.ViewModels;
using Nokia.Music;
using Nokia.Music.Tasks;
using Nokia.Music.Types;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Telerik.Windows.Controls;
using Windows.System;

namespace MusicGame
{
    public partial class MyMusicAlbum : PhoneApplicationPage
    {
        #region global variables
        const string MUSIC_API_KEY = "987006b749496680a0af01edd5be6493";
        int numTimesWrong;
        int points;
        int timesPlayed;
        int numTicks;
        SongCollection allSongs;
        ObservableCollection<DataItemViewModel> albumArtList;
        ObservableCollection<Song> pickedSongs;
        Song winningSong;
        DispatcherTimer playTime;
        Random rand;
        MusicClient client;
        MediaLibrary songs;
        ProgressBar progBar;
        Grid grid;
        #endregion
        private enum TimerStatus
        {
            On,
            Off,
            Pause
        }
        // Constructor
        public MyMusicAlbum()
        {
            InitializeComponent();
            initialize();
            setUpSongList();
            pickSongList();  
        }
        private enum ProgBarStatus
        {
            On,
            Off
        }

        //Get library and other setup
        private void initialize()
        {
            pickedSongs = new ObservableCollection<Song>();
            albumArtList = new ObservableCollection<DataItemViewModel>();
            playTime = new DispatcherTimer();
            playTime.Interval = new TimeSpan(0, 0, 1);
            playTime.Tick += playTime_Tick;
            client = new MusicClient(MUSIC_API_KEY);
            rand = new Random();
            songs = new MediaLibrary();
            numTimesWrong = 0;
            timesPlayed = 0;
            points = 0;
        }
        private void setUpSongList()
        {
            allSongs = songs.Songs;
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
                text.Foreground =new SolidColorBrush(Colors.White);
                progBar.IsIndeterminate = true;
                progBar.IsEnabled = true;
                Thickness pad = new Thickness(0,0,0,40);
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

        //choose songs, and pick a winning song
        private void pickSongList()
        {
            //picks a list of 12 albums/songs to display
            int numAlbums = songs.Albums.Count;
            if (numAlbums > 12)
            {
                for (int i = 0; i < 12; i++)
                {
                    pickSong();
                }
                setAlbumArt();
                checkConnectionAndRun();
            }
            else
            {
                albumArtGrid.EmptyContent = "Not enough songs to play!" + "\n" + "Try adding some more albums to your phone," + "\n" + "or simply play the top music of" + "\n" + "Nokia MixRadio with Nokia MixRadio genres!";
            }
        }
        private void pickSong()
        {
            //picks an idividual song from the list of all songs in library
            Song song = allSongs[rand.Next(allSongs.Count)];
            BitmapImage albumArt = getBitmap(song);
            if (albumArt != null)
            {
                if (!prevSelected(song))
                {
                    pickedSongs.Add(song);
                    DataItemViewModel album = new DataItemViewModel();
                    album.ImageSource = albumArt;
                    album.Song = song;
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
        private void setAlbumArt()
        {
            //sets up the grid of album art
            albumArtGrid.ItemsSource = albumArtList;
            albumArtGrid.SetValue(InteractionEffectManager.IsInteractionEnabledProperty, true);
            InteractionEffectManager.AllowedTypes.Add(typeof(RadDataBoundListBoxItem));
        }
        private void pickWinner()
        {
            //picks a random song from the selected songs to be the winner
            winningSong = pickedSongs[rand.Next(pickedSongs.Count)];
            playSong();
        }
        private bool prevSelected(Song song)
        {
            //check whether a album has already been picked (so no repeats)
            foreach (Song pickedSong in pickedSongs)
            {
                if (song.Album == pickedSong.Album)
                {
                    return true;
                }
            }
            return false;
        }
        private BitmapImage getBitmap(Song song)
        {
            //get the album covers to add to the list
            Stream albumArtStream = song.Album.GetAlbumArt();
            if (albumArtStream == null)
            {
                return null;
            }
            else
            {
                BitmapImage albumArt = new BitmapImage();
                albumArt.SetSource(albumArtStream);
                albumArt.DecodePixelHeight = 200;
                albumArt.DecodePixelWidth = 200;

                return albumArt;
            }
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
                playTime.Start();
            }
            else if (stat == TimerStatus.Off)
            {
                numTicks = 25;
                timer.Content = numTicks;
                playTime.Stop();
            }
            else
            {
                playTime.Stop();
            }
        }

        void playTime_Tick(object sender, EventArgs e)
        {
            timer.Content = numTicks;
            if (numTicks % 5 == 0 && numTicks!=25)
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
        private void Image_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            //checks to see if the correct answer was selected
            DataItemViewModel item = ((sender as Image).DataContext as DataItemViewModel);
            if (item.Song == winningSong)
            {
                correctAns();
            }
            else
            {
                removeFromList(item);
                wrongAns();
            }
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
            toggleClock(TimerStatus.Off);
            player.Stop();
            player.Resources.Clear();
            albumArtList.Clear();
            pickedSongs.Clear();
            reInitialize();
            pickSongList();
        }
        private void reInitialize()
        {
            
            player.Source = null;
            albumArtList = null;
            pickedSongs = null;
            albumArtGrid.ItemsSource = null;

            albumArtList = new ObservableCollection<DataItemViewModel>();
            pickedSongs = new ObservableCollection<Song>();
        }
        private void removeFromList(DataItemViewModel selected)
        {
            int i = 0;
            foreach (DataItemViewModel item in albumArtList)
            {
                if (selected.Song.Name == item.Song.Name)
                {
                    break;
                }
                i++;
            }
            albumArtList.RemoveAt(i);
            pickedSongs.RemoveAt(i);
        }
    }
}