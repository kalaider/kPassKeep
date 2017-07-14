using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kPassKeep.Model
{
    public class Account : DescribableEntity
    {

        private string password;
        private Target target;
        private Login  login;

        public Account()
        {
        }

        public string Password
        {
            get { return password; }
            set { password = value; OnPropertyChanged("Password"); }
        }

        public Target Target
        {
            get { return target; }
            set { target = value; OnPropertyChanged("Target"); }
        }

        public Login Login
        {
            get { return login; }
            set { login = value; OnPropertyChanged("Login"); }
        }
    }
}
