using Microsoft.Phone.Controls;
using Microsoft.Xna.Framework.Media;
using MusicGame.ViewModels;
using Nokia.Music;
using Nokia.Music.Types;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
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
        int roundPoints;
        bool isRight;
        SongCollection allSongs;
        ObservableCollection<DataItemViewModel> albumArtList;
        ObservableCollection<Song> pickedSongs;
        Song winningSong;
        Song lastWinningSong;
        DispatcherTimer playTime;
        Random rand;
        MusicClient client;
        MediaLibrary songs;
        ProgressBar progBar;
        Grid grid;
        Grid correctAnsGrid;
        ObservableCollection<SongData> winningSongList;
        IsolatedStorageSettings store;
        Uri prodUri;
        Uri nokiaMusicUri;
        Uri albumUri;
        bool gameOver;

        bool openInNokiaMusic = true;
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
        private enum AnsVisibility
        {
            On,
            Off
        }
        //Get library and other setup
        private void initialize()
        {
            gameOver = true;
            pickedSongs = new ObservableCollection<Song>();
            winningSongList = new ObservableCollection<SongData>();
            store = IsolatedStorageSettings.ApplicationSettings;
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
            roundPoints = 0;
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
            await toggleProgBar(ProgBarStatus.On);

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
            await toggleProgBar(ProgBarStatus.Off);
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
        async private Task displayData(bool win, AnsVisibility vis, Song song)
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
                text.Text = winStat + "was " + song.Name + " by " + song.Artist;
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
                BitmapImage btmp = new BitmapImage();
                btmp.SetSource(song.Album.GetAlbumArt());
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
                pickWinner();
            }
            else
            {
                Grid grid = new Grid();
                grid.Background = new SolidColorBrush(Colors.Black);
                grid.Opacity = .7;
                TextBlock text = new TextBlock() { Text = "Not enough songs! Try adding more songs to your phone, or just play with Nokia MixRadio!" };
                text.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                text.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                text.FontSize = 20;
                text.TextAlignment = TextAlignment.Center;
                text.TextWrapping = TextWrapping.Wrap;
                grid.Children.Add(text);
                ContentPanel.Children.Add(grid);
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
            if (alreadyPicked(winningSong))
            {
                pickWinner();
            }
            else
            {
                playSong();
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
                    setAlbumArt();
                    prodUri = prod.Result.WebUri;
                    nokiaMusicUri = prod.Result.AppToAppUri;
                    albumUri = prod.Result.Thumb320Uri;
                    await toggleProgBar(ProgBarStatus.On);
                    Uri songUri = getSongUri(prod);
                    player.AutoPlay = false;
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
        async void player_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            await toggleProgBar(ProgBarStatus.Off);
        }
        async void player_MediaOpened(object sender, RoutedEventArgs e)
        {
            await playForLimit();
        }
        async private Task playForLimit()
        {
            numTimesWrong = 0;
            timesPlayed = 0;
            numTicks = 25;
            toggleClock(TimerStatus.On);
            await toggleProgBar(ProgBarStatus.Off);
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
        private void Image_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            //checks to see if the correct answer was selected
            DataItemViewModel item = ((sender as Image).DataContext as DataItemViewModel);
            if (item.Title != MUSIC_API_KEY)
            {
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
            else
            {
                return;
            }
        }
        private void wrongAns()
        {
            //handles incorrect answers
            roundPoints--;
            points--;
            numTimesWrong++;
            Points.Text = points.ToString() + "/30 Points";
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
            roundPoints += (5 - timesPlayed);
            points += (5 - timesPlayed);
            newBoard();
        }
        private void timeOut()
        {
            isRight = false;
            roundPoints = 0;
            newBoard();
        }
        private void newBoard()
        {
            //clears the current board and creates a new one
            if (openInNokiaMusic)
            {
                winningSongList.Add(new SongData() { albumUri = albumUri, points = roundPoints, correct = isRight, seconds = 25 - numTicks, songName = winningSong.Name, uri = nokiaMusicUri });
            }
            else
            {
                winningSongList.Add(new SongData() { albumUri = albumUri, points = roundPoints, correct = isRight, seconds = 25 - numTicks, songName = winningSong.Name, uri = prodUri });
            }
            toggleClock(TimerStatus.Off);
            lastWinningSong = winningSong;
            if (winningSongList.Count > 5)
            {
                store["results"] = winningSongList;
                store.Save();
                if (gameOver)
                {
                    NavigationService.Navigate(new Uri("/ResultsPage.xaml?style=album&genre=MyMusic", UriKind.Relative));
                    gameOver = false;
                }
            }
            else
            {
                numTimesWrong = 0;
                timesPlayed = 0;
                roundPoints = 0;
                Points.Text = points.ToString() + "/30 Points";
                roundNum.Text = "Round " + (winningSongList.Count + 1) + "/6";
                toggleClock(TimerStatus.Off);
                player.Stop();
                player.Resources.Clear();
                albumArtList.Clear();
                pickedSongs.Clear();
                reInitialize();
                pickSongList();
            }
        }
        private System.Windows.Media.Imaging.BitmapImage getAlbumArt(Stream stream)
        {
            if (stream == null)
            {
                return null;
            }
            else
            {
                BitmapImage albumArt = new BitmapImage();
                albumArt.SetSource(stream);
                albumArt.DecodePixelHeight = 200;
                albumArt.DecodePixelWidth = 200;
                return albumArt;
            }
        }
        private void reInitialize()
        {
            player.Source = null;
            albumArtList = null;
            pickedSongs = null;
            albumArtList = new ObservableCollection<DataItemViewModel>();
            pickedSongs = new ObservableCollection<Song>();
        }
        private void removeFromList(DataItemViewModel selected)
        {
            int i = 0;
            foreach (DataItemViewModel item in albumArtList)
            {
                if (item.Title != MUSIC_API_KEY)
                {
                    if (selected.Song.Name == item.Song.Name && selected.Song.Artist == item.Song.Artist)
                    {
                        break;
                    }
                }
                i++;
            }
            albumArtList.RemoveAt(i);
            //creates blank tile to prevent shifts
            DataItemViewModel wrong = new DataItemViewModel();
            wrong.ImageSource = new BitmapImage(new Uri("/Assets/x.png", UriKind.Relative));
            //API key unlikely to be song title
            wrong.Title = MUSIC_API_KEY;
            albumArtList.Insert(i, wrong);
        }
    }
}