using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace kPassKeep.Model
{
    public class AccountGroups
    {

        private ObservableCollection<AccountGroup> accountGroups = new ObservableCollection<AccountGroup>();

        public ObservableCollection<AccountGroup> Groups
        {
            get { return accountGroups; }
        }
    }
}
