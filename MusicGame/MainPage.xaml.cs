using Microsoft.Phone.Controls;
using Microsoft.Xna.Framework.Media;
using MusicGame.ViewModels;
using Nokia.Music;
using Nokia.Music.Tasks;
using Nokia.Music.Types;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Telerik.Windows.Controls;

namespace MusicGame
{
    public partial class MainPage : PhoneApplicationPage
    {
        static const string MUSIC_API_KEY = "987006b749496680a0af01edd5be6493";
        int numTimesWrong;

        SongCollection allSongs;
        ObservableCollection<DataItemViewModel> albumArtList;
        ObservableCollection<Song> pickedSongs;
        Song winningSong;
        DispatcherTimer playTimer;
        DispatcherTimer replayTimer;
        Random rand;
        MusicClient client;
        MediaLibrary songs;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            initialize();
            setUpSongList();
            pickSongList();
            pickWinner();
        }
        //Get library and other setup
        private void initialize()
        {
            pickedSongs = new ObservableCollection<Song>();
            albumArtList = new ObservableCollection<DataItemViewModel>();
            replayTimer = new DispatcherTimer();
            playTimer = new DispatcherTimer();
            client = new MusicClient(MUSIC_API_KEY);
            rand = new Random();
            songs = new MediaLibrary();
            numTimesWrong = 0;
        }
        private void setUpSongList()
        {
            allSongs = songs.Songs;
        }

        //choose songs, and pick a winning song
        private void pickSongList()
        {
            int numAlbums = songs.Albums.Count;
            if (numAlbums > 12)
            {
                for (int i = 0; i < 12; i++)
                {
                    pickSong();
                }
                setAlbumArt();
            }
            else
            {
                SongName.Text = "not enough albums to play!";
            }
        }
        private void pickSong()
        {
            int numSongs = allSongs.Count;
            int randNum = rand.Next(numSongs);
            Song song = allSongs[randNum];
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
            albumArtGrid.ItemsSource = albumArtList;
        }
        private void pickWinner()
        {
            winningSong = pickedSongs[rand.Next(pickedSongs.Count)];
            playSong();
        }
        //check whether a album has already been picked (so no repeats)
        private bool prevSelected(Song song)
        {
            foreach (Song pickedSong in pickedSongs)
            {
                if (song.Album == pickedSong.Album)
                {
                    return true;
                }
            }
            return false;
        }
        //get the album covers to add to the list
        private BitmapImage getBitmap(Song song)
        {
            Stream albumArtStream = song.Album.GetAlbumArt();

            if (albumArtStream == null)
            {
                return null;
            }
            else
            {
                BitmapImage albumArt = new BitmapImage();
                albumArt.SetSource(albumArtStream);
                return albumArt;
            }

        }

        //get and play a sample of a song
        //first ensuring that it is the right song
        //then limiting its time playing
        async private void playSong()
        {
            ListResponse<MusicItem> result = await client.SearchAsync(winningSong.Name + " " + winningSong.Artist.Name, Category.Track);
            //ListResponse<MusicItem> result = await client.SearchAsync("ode to chin switchfoot", Category.Track);
            if (result.Result != null && result.Count > 0)
            {
                Response<Product> prod = await client.GetProductAsync(result[0].Id);
                if (performersAreArtists(prod.Result.Performers, winningSong.Artist.Name))
                {
                    Uri songUri = client.GetTrackSampleUri(result[0].Id);
                    player.Source = songUri;
                    playForLimit(5);
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
        private void playForLimit(int secs)
        {
            playTimer.Interval = new TimeSpan(0, 0, secs);
            player.Play();
            playTimer.Tick += delegate(object s, EventArgs args)
            {
                playTimer.Stop();
                player.Stop();
                replaySong(secs);
            };
            playTimer.Start();
        }
        private void replaySong(int secs)
        {
            replayTimer.Interval = new TimeSpan(0, 0, 5);
            replayTimer.Tick += delegate(object s, EventArgs args)
            {
                replayTimer.Stop();
                playForLimit(secs + 5);
            };
            replayTimer.Start();
        }

        //handle answers
        private void Image_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (((sender as Image).DataContext as DataItemViewModel).Song == winningSong)
            {
                correctAns();
            }
            else
            {
                wrongAns();
            }
        }
        private void wrongAns()
        {
            SongName.Text = "Wrong answer!";
            numTimesWrong++;
            if (numTimesWrong > 2)
            {
                newBoard();
            }
        }
        private void correctAns()
        {
            SongName.Text = "Correct!";
            newBoard();
        }
        private void newBoard()
        {
            numTimesWrong = 0;
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
            albumArtList.Clear();
            pickedSongs.Clear();
            pickSongList();
            pickWinner();
        }
    }
}