using Microsoft.Xna.Framework.Media;
using Nokia.Music.Types;
using System.Windows.Media.Imaging;
using Telerik.Windows.Controls;

namespace MusicGame.ViewModels
{
    public class DataItemViewModel : ViewModelBase
    {
        private BitmapImage imageSource;
        private Song song;
        private Product prod;
        private string title;
        private string information;

        /// <summary>
        /// Gets or sets the image source.
        /// </summary>
        public BitmapImage ImageSource
        {
            get
            {
                return this.imageSource;
            }
            set
            {
                if (this.imageSource != value)
                {
                    this.imageSource = value;
                    this.OnPropertyChanged("ImageSource");
                }
            }
        }

        public Song Song
        {
            get
            {
                return this.song;
            }
            set
            {
                if (this.song != value)
                {
                    this.song = value;
                    this.OnPropertyChanged("Song");
                }
            }
        }

        public Product Prod
        {
            get { return this.prod; }
            set
            {
                if (this.prod != value)
                {
                    this.prod = value;
                    this.OnPropertyChanged("Prod");
                }
            }
        }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string Title
        {
            get
            {
                return this.title;
            }
            set
            {
                if (this.title != value)
                {
                    this.title = value;
                    this.OnPropertyChanged("Title");
                }
            }
        }

        /// <summary>
        /// Gets or sets the information.
        /// </summary>
        public string Information
        {
            get
            {
                return this.information;
            }
            set
            {
                if (this.information != value)
                {
                    this.information = value;
                    this.OnPropertyChanged("Information");
                }
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.title;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.

        /// </returns>
        public override bool Equals(object obj)
        {
            DataItemViewModel typedObject = obj as DataItemViewModel;
            if (typedObject == null)
            {
                return false;
            }
            return this.Title == typedObject.Title && this.Information == typedObject.Information;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.

        /// </returns>
        public override int GetHashCode()
        {
            return this.Title.GetHashCode() ^ this.Information.GetHashCode();
        }
    }
}