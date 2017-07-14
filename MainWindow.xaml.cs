using FirstFloor.ModernUI.Windows.Controls;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interactivity;
using FirstFloor.ModernUI.Presentation;
using kPassKeep.Model;
using kPassKeep.Service;
using kPassKeep.Util;

namespace kPassKeep
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ModernWindow
    {
        private MainWindowModel model;

        public MainWindow()
        {
            AppearanceManager.Current.AccentColor = Colors.Green;
            InitializeComponent();
            this.model = new MainWindowModel();
            this.DataContext = this.model;
        }

        private void ModernWindow_Loaded(object sender, RoutedEventArgs e)
        {
            model.AccountGroups = PersistenceProvider.Load();
            foreach (var g in model.AccountGroups.Groups)
            {
                RegisterGroup(g);
            }
            model.AccountGroups.Groups.CollectionChanged += Groups_CollectionChanged;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            PersistenceProvider.Persist(model.AccountGroups);
        }

        private void NewAccountButton_Click(object sender, RoutedEventArgs e)
        {
            if (model.SelectedGroup == null)
            {
                return;
            }
            Account a = new Account();
            model.SelectedGroup.Accounts.Add(a);
            model.SelectedAccount = a;
        }

        private void Groups_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var i in e.NewItems)
                {
                    var g = ((AccountGroup)i);
                    g.ModificationTracker.ModifiedElements.Add(g.Guid);
                    RegisterGroup(g);
                }
            }
            if (e.OldItems != null)
            {
                foreach (var i in e.OldItems)
                {
                    var g = ((AccountGroup)i);
                    g.ModificationTracker.ModifiedElements.Add(g.Guid);
                }
            }
        }

        private void RegisterGroup(AccountGroup g)
        {
            g.PropertyChanged += (s, evt) => {
                g.ModificationTracker.ModifiedElements.Add(g.Guid);
                if ("IsLocked".Equals(evt.PropertyName))
                {
                    model.SelectedGroup = model.SelectedGroup;
                }
            };
            foreach (var l in g.Logins)
            {
                l.PropertyChanged += (s, evt) => {
                    g.ModificationTracker.ModifiedElements.Add(l.Guid);
                };
            }
            g.Logins.CollectionChanged += (s, evt) => {
                if (evt.NewItems != null)
                {
                    foreach (var l in evt.NewItems)
                    {
                        g.ModificationTracker.ModifiedElements.Add(((Entity)l).Guid);
                        ((Entity)l).PropertyChanged += (s2, evt2) => {
                            g.ModificationTracker.ModifiedElements.Add(((Entity)l).Guid);
                        };
                    }
                }
                if (evt.OldItems != null)
                {
                    foreach (var l in evt.OldItems)
                    {
                        g.ModificationTracker.ModifiedElements.Add(((Entity)l).Guid);
                    }
                }
            };
            foreach (var a in g.Accounts)
            {
                RegisterAccount(g, a);
            }
            g.Accounts.CollectionChanged += (s, evt) =>
            {
                if (evt.NewItems != null)
                {
                    foreach (var a in evt.NewItems)
                    {
                        g.ModificationTracker.ModifiedElements.Add(((Entity)a).Guid);
                        RegisterAccount(g, (Account)a);
                    };
                }
                if (evt.OldItems != null)
                {
                    foreach (var a in evt.OldItems)
                    {
                        g.ModificationTracker.ModifiedElements.Add(((Entity)a).Guid);
                    };
                }
            };
        }

        private void RegisterAccount(AccountGroup g, Account a)
        {
            a.PropertyChanged += (s, evt) => {
                g.ModificationTracker.ModifiedElements.Add(a.Guid);
                if (a.Target != null)
                {
                    a.Target.PropertyChanged += (s2, evt2) => {
                        g.ModificationTracker.ModifiedElements.Add(a.Target.Guid);
                    };
                }
            };
            if (a.Target != null)
            {
                a.Target.PropertyChanged += (s, evt) => {
                    g.ModificationTracker.ModifiedElements.Add(a.Target.Guid);
                };
            }
        }

        private void ModernWindow_Closing(object sender, CancelEventArgs e)
        {
            var hasModifications = model.AccountGroups.Groups
                .Select(g => g.ModificationTracker.ModifiedElements.Any())
                .Where(m => m)
                .Any();
            if (hasModifications)
            {
                MessageBoxResult r = ModernDialog.ShowMessage(
                   "Save unsaved changes?",
                   "Save or Ignore", MessageBoxButton.YesNoCancel, Window.GetWindow(this));
                if (r == MessageBoxResult.Yes)
                {
                    PersistenceProvider.Persist(model.AccountGroups);
                }
                else if (r == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }
    }




    public class MainWindowModel : NotifyPropertyChanged
    {
        private AccountGroups groups;
        private AccountGroup  selectedGroup;
        private Account       selectedAccount;
        private Target        selectedTarget;
        private Login         selectedLogin;
        private bool          isAccountSelected;

        public ObservableCollection<Target> UniqueTargets { get; private set; }

        public MainWindowModel()
        {
            UniqueTargets = new ObservableCollection<Target>();
        }

        public AccountGroups AccountGroups
        {
            get { return groups; }
            set { groups = value; OnPropertyChanged("AccountGroups"); }
        }

        public AccountGroup SelectedGroup
        {
            get { return selectedGroup; }
            set
            {
                selectedGroup = value;
                OnPropertyChanged("SelectedGroup");
                UniqueTargets.Clear();
                if (value != null)
                {
                    foreach (var a in value.Accounts)
                    {
                        if (a.Target != null)
                        {
                            UniqueTargets.Add(a.Target);
                        }
                    }
                }
            }
        }

        public Account SelectedAccount
        {
            get { return selectedAccount; }
            set { selectedAccount = value; OnPropertyChanged("SelectedAccount"); IsAccountSelected = (value != null); }
        }

        public Target SelectedTarget
        {
            get { return selectedTarget; }
            set { selectedTarget = value; OnPropertyChanged("SelectedTarget"); }
        }

        public Login SelectedLogin
        {
            get { return selectedLogin; }
            set { selectedLogin = value; OnPropertyChanged("SelectedLogin"); }
        }

        public bool IsAccountSelected
        {
            get { return isAccountSelected; }
            set { isAccountSelected = value; OnPropertyChanged("IsAccountSelected"); }
        }

    }

}
