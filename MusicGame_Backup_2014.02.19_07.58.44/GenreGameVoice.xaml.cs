using Microsoft.Phone.Controls;
using System.Windows;
using Windows.Phone.Speech.Recognition;
using System;
using Windows.Phone.Speech.Synthesis;

namespace MusicGame
{
    public partial class GenreGameVoice : PhoneApplicationPage
    {
        public GenreGameVoice()
        {
            InitializeComponent();
        }

        private async void recognizeThis_Click(object sender, RoutedEventArgs e)
        {
            SpeechRecognizerUI recognizer = new SpeechRecognizerUI();
            SpeechRecognitionUIResult result = await recognizer.RecognizeWithUIAsync();
            if (result.ResultStatus == SpeechRecognitionUIStatus.Succeeded)
            {
                youSaid.Text = result.RecognitionResult.Text;
            }
        }
    }
}