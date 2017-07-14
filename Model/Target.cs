using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace kPassKeep.Model
{
    public class Target : DescribableEntity
    {

        private string      uri;
        private string      title;
        private BitmapImage icon;

        public Target()
        {
        }

        public string Uri
        {
            get { return uri; }
            set { uri = value; OnPropertyChanged("Uri"); }
        }

        public string Title
        {
            get { return title; }
            set { title = value; OnPropertyChanged("Title"); }
        }

        public BitmapImage Icon
        {
            get { return icon; }
            set { icon = value; OnPropertyChanged("Icon"); }
        }
    }
}
