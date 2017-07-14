using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FirstFloor.ModernUI.Presentation;

namespace kPassKeep.Model
{
    public class Login : DescribableEntity
    {

        private string username;

        public Login()
        {
        }

        public string Username
        {
            get { return username; }
            set { username = value; OnPropertyChanged("Username"); }
        }
    }
}