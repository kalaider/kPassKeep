using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace kPassKeep.Model
{
    public class RawAccountGroup : RawSimpleEntity
    {
        public RawAccountGroup()
        {
            RawMembers = new Dictionary<Guid, RawSimpleEntity>();
        }
        public IDictionary<Guid, RawSimpleEntity> RawMembers { get; private set; }
        public Version FormatVersion { get; set; }
    }

    public class ModificationTracker
    {
        public ModificationTracker()
        {
            ModifiedElements = new HashSet<Guid>();
        }

        public ISet<Guid> ModifiedElements { get; private set; }
    }

    public class AccountGroup : DescribableEntity
    {

        private ObservableCollection<Account> accounts = new ObservableCollection<Account>();
        private ObservableCollection<Login>   logins   = new ObservableCollection<Login>();
        private string name;
        private string password;
        private bool   isLocked;

        public AccountGroup()
        {
            ModificationTracker = new ModificationTracker();
            RawAccountGroup = new RawAccountGroup();
            RawAccountGroup.FormatVersion = kPassKeep.Service.PersistenceProvider.LatestFormat;
            RawAccountGroup.Guid = Guid;
        }

        public ObservableCollection<Account> Accounts
        {
            get { return accounts; }
        }

        public ObservableCollection<Login> Logins
        {
            get { return logins; }
        }

        public string Password
        {
            get { return password; }
            set { password = value; OnPropertyChanged("Password"); }
        }

        public string Name
        {
            get { return name; }
            set { name = value; OnPropertyChanged("Name"); }
        }

        public bool IsLocked
        {
            get { return isLocked; }
            set { isLocked = value; OnPropertyChanged("IsLocked"); }
        }

        public RawAccountGroup RawAccountGroup { get; private set; }

        public ModificationTracker ModificationTracker { get; private set; }
    }
}
