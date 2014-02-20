using System;

namespace MusicGame
{
    public class SongData
    {
        public Uri albumUri { get; set; }
        public int seconds { get; set; }
        public string songName { get; set; }
        public bool correct { get; set; }
        public Uri uri { get; set; }
        public int points { get; set; }
    }
}
