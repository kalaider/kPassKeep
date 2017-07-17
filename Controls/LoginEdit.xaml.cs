using FirstFloor.ModernUI.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using kPassKeep.Model;

namespace kPassKeep.Controls
{
    /// <summary>
    /// Interaction logic for LoginEdit.xaml
    /// </summary>
    public partial class LoginEdit : UserControl
    {

        public Login Selected
        {
            get { return (Login)GetValue(SelectedProperty); }
            set { SetValue(SelectedProperty, value); }
        }

        public static readonly DependencyProperty SelectedProperty =
            DependencyProperty.Register("Selected", typeof(Login), typeof(LoginEdit), new FrameworkPropertyMetadata { DefaultValue = null, BindsTwoWayByDefault = true });
        
        public AccountGroup AccountGroup
        {
            get { return (AccountGroup)GetValue(AccountGroupProperty); }
            set { SetValue(AccountGroupProperty, value); }
        }

        public static readonly DependencyProperty AccountGroupProperty =
            DependencyProperty.Register("AccountGroup", typeof(AccountGroup), typeof(LoginEdit), new FrameworkPropertyMetadata { DefaultValue = null, PropertyChangedCallback = (d, o) => ((LoginEdit)d).AccountGroupChanged() });

        public ICollectionView SortedLogins
        {
            get { return (ICollectionView)GetValue(SortedLoginsProperty); }
            set { SetValue(SortedLoginsProperty, value); }
        }

        public static readonly DependencyProperty SortedLoginsProperty =
            DependencyProperty.Register("SortedLogins", typeof(ICollectionView), typeof(LoginEdit), new FrameworkPropertyMetadata { DefaultValue = null });

        public LoginEdit()
        {
            InitializeComponent();
        }

        private void AccountGroupChanged()
        {
            if (AccountGroup == null)
            {
                SortedLogins = null;
                return;
            }
            else
            {
                var itemSourceList = new CollectionViewSource() { Source = AccountGroup.Logins };
                itemSourceList.SortDescriptions.Add(new SortDescription("Username", ListSortDirection.Ascending));
                var l = AccountGroup.Logins;
                SortedLogins = itemSourceList.View;
                foreach (var el in l)
                {
                    RegisterLogin(el);
                }
                l.CollectionChanged += (s, e) =>
                {
                    if (s != l || SortedLogins == null)
                    {
                        return;
                    }
                    if (e.NewItems != null)
                    {
                        foreach (var el in e.NewItems)
                        {
                            RegisterLogin((Login)el);
                        }
                    }
                    SortedLogins.Refresh();
                };
            }
        }

        private void RegisterLogin(Login ag)
        {
            ag.PropertyChanged += (s2, e2) =>
            {
                if (s2 != ag || !"Username".Equals(e2.PropertyName))
                {
                    return;
                }
                SortedLogins.Refresh();
            };
        }

        private void AddLoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (AccountGroup == null)
            {
                return;
            }
            Login l = new Login();
            l.Username = "<New Login>";
            AccountGroup.Logins.Add(l);
            Selected = l;
        }

        private void RemoveLoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (Selected == null || AccountGroup == null)
            {
                return;
            }
            var referenceCount = AccountGroup.Accounts
                .Where(a => Selected.Equals(a.Login))
                .Count();
            if (referenceCount > 1) // references to selected account
            {
                var message = String.Format("The login has another {0} reference(s), thus cannot be deleted.", referenceCount - 1);
                ModernDialog.ShowMessage(
                    message,
                    "Cannot delete login", MessageBoxButton.OK, Window.GetWindow(this));
                return;
            }
            MessageBoxResult r = ModernDialog.ShowMessage(
                "Do you really want to remove login?",
                "Confirm delete", MessageBoxButton.YesNo, Window.GetWindow(this));
            if (r == MessageBoxResult.Yes)
            {
                AccountGroup.Logins.Remove(Selected);
            }
        }
    }
}
