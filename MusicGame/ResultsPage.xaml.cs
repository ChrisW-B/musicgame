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
        private IsolatedStorageSettings store;
        private ObservableCollection<SongData> results;
        private ObservableCollection<SongDataWithPicture> source;
        private const string MUSIC_API_KEY = "987006b749496680a0af01edd5be6493";
        private MusicClient client;
        private string gameStyle;
        private string gameGenre;

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (this.NavigationContext.QueryString.ContainsKey("style") && this.NavigationContext.QueryString.ContainsKey("genre"))
            {
                gameStyle = this.NavigationContext.QueryString["style"];
                gameGenre = this.NavigationContext.QueryString["genre"];
            }
            client = new MusicClient(MUSIC_API_KEY);
            getResults();
        }

        public ResultsPage()
        {
            InitializeComponent();
        }

        private void getResults()
        {
            //gets the results from isolated storage from last game play
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
            //organizes the results
            double score = 0;
            int time = 0;
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
                time += song.seconds;
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
            saveResults(score, time);
        }

        private void saveResults(double score, int time)
        {
            if (store.Contains("HighScoreList"))
            {
                HighScoreResults highScoreList = (HighScoreResults)store["HighScoreList"];
                if (gameStyle == "album")
                {
                    if (highScoreList.albumList != null)
                    {
                        bool foundGenre = false;
                        foreach (ScoreGenre sg in highScoreList.albumList)
                        {
                            if (sg.genre == gameGenre)
                            {
                                int i = 0;
                                while (score < sg.scoreList[i].points)
                                {
                                    i++;
                                    if (i == sg.scoreList.Count)
                                    {
                                        break;
                                    }
                                }
                                if (i == sg.scoreList.Count)
                                {
                                    sg.scoreList.Add(new Scores() { points = score, totalTime = time });
                                }
                                else
                                {
                                    sg.scoreList.Insert(i, new Scores() { points = score, totalTime = time });
                                }
                                foundGenre = true;
                            }
                        }
                        if (!foundGenre)
                        {
                            ScoreGenre sg = new ScoreGenre();
                            sg.genre = gameGenre;
                            sg.scoreList = new ObservableCollection<Scores>();
                            sg.scoreList.Add(new Scores() { points = score, totalTime = time });
                            highScoreList.albumList.Add(sg);
                        }
                    }
                    else
                    {
                        highScoreList.albumList = new ObservableCollection<ScoreGenre>();
                        ScoreGenre sg = new ScoreGenre();
                        sg.genre = gameGenre;
                        sg.scoreList = new ObservableCollection<Scores>();
                        sg.scoreList.Add(new Scores() { points = score, totalTime = time });
                        highScoreList.albumList.Add(sg);
                    }
                }
                else
                {
                    if (highScoreList.voiceList != null)
                    {
                        bool foundGenre = false;
                        foreach (ScoreGenre sg in highScoreList.voiceList)
                        {
                            if (sg.genre == gameGenre)
                            {
                                int i = 0;
                                while (score < sg.scoreList[i].points)
                                {
                                    i++;
                                    if (i == sg.scoreList.Count)
                                    {
                                        break;
                                    }
                                }
                                if (i == sg.scoreList.Count)
                                {
                                    sg.scoreList.Add(new Scores() { points = score, totalTime = time });
                                }
                                else
                                {
                                    sg.scoreList.Insert(i, new Scores() { points = score, totalTime = time });
                                }
                                foundGenre = true;
                            }
                        }
                        if (!foundGenre)
                        {
                            ScoreGenre sg = new ScoreGenre();
                            sg.genre = gameGenre;
                            sg.scoreList = new ObservableCollection<Scores>();
                            sg.scoreList.Add(new Scores() { points = score, totalTime = time });
                            highScoreList.albumList.Add(sg);
                        }
                    }
                    else
                    {
                        highScoreList.albumList = new ObservableCollection<ScoreGenre>();
                        ScoreGenre sg = new ScoreGenre();
                        sg.genre = gameGenre;
                        sg.scoreList = new ObservableCollection<Scores>();
                        sg.scoreList.Add(new Scores() { points = score, totalTime = time });
                        highScoreList.albumList.Add(sg);
                    }
                }
                store["HighScoreList"] = highScoreList;
            }
            else
            {
                store["HighScoreList"] = new HighScoreResults();
                saveResults(score, time);
            }
        }

        private BitmapImage getAlbumArt(Uri uri)
        {
            //downloads album art for results
            BitmapImage albumArt = new BitmapImage(uri);
            albumArt.DecodePixelHeight = 200;
            albumArt.DecodePixelWidth = 200;
            return albumArt;
        }

        private async void StackPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            //opens result in browser
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