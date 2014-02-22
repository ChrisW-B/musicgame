using Microsoft.Phone.Controls;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Telerik.Windows.Controls;
using Windows.System;

namespace MusicGame
{
    public partial class MenuPage : PhoneApplicationPage
    {
        AlbumCollection albums;
        public MenuPage()
        {
            InitializeComponent();
            initalizeSongList();
            randomizeLiveTile();
        }

        private void initalizeSongList()
        {
            MediaLibrary songs = new MediaLibrary();
            albums = songs.Albums;
        }

        private void randomizeLiveTile()
        {
            RadFlipTileData tileData = new RadFlipTileData()
            {
                VisualElement = getAlbumArt(),
                Title = "What's Spinning?",
                BackVisualElement = getAlbumArt(),
                MeasureMode = Telerik.Windows.Controls.MeasureMode.Element,
                BackTitle = "What's Spinning?",
                WideBackVisualElement = getAlbumArt(),
                WideVisualElement = getAlbumArt(),
                SmallVisualElement = getAlbumArt()
            };
            LiveTileHelper.CreateOrUpdateTile(tileData, new Uri("/", UriKind.Relative), true);
        }

        private Image getAlbumArt()
        {
            WriteableBitmap album = new WriteableBitmap(500,500);
            album.SetSource(getRandomAlbumArt());
            album.Invalidate();
            Image finished = new Image() { Source = album };
            return finished;
        }

        private Stream getLogo()
        {
            return App.GetResourceStream(new Uri("Assets/tileLarge.png", UriKind.Relative)).Stream;
        }

        private Stream getRandomAlbumArt()
        {
            Random rand = new Random();
            Stream art = albums[rand.Next(albums.Count)].GetAlbumArt();
            while (art == null)
            {
                art = albums[rand.Next(albums.Count)].GetAlbumArt();
            }
            return art;
        }


        private void scores_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/HighScores.xaml", UriKind.Relative));
        }

        private void sayIt_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/CategorySelect.xaml?voice=1", UriKind.Relative));
        }

        private void albumPick_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/CategorySelect.xaml?voice=0", UriKind.Relative));
        }
    }
}