using FirstFloor.ModernUI.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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
            DependencyProperty.Register("AccountGroup", typeof(AccountGroup), typeof(LoginEdit), new FrameworkPropertyMetadata { DefaultValue = null });
        
        public LoginEdit()
        {
            InitializeComponent();
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
