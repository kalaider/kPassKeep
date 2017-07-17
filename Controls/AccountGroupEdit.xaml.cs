using FirstFloor.ModernUI.Windows.Controls;
using System;
using System.Collections.Generic;
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
using kPassKeep.Service;

namespace kPassKeep.Controls
{
    /// <summary>
    /// Interaction logic for AccountGroupEdit.xaml
    /// </summary>
    public partial class AccountGroupEdit : UserControl
    {

        public AccountGroups AccountGroups
        {
            get { return (AccountGroups)GetValue(AccountGroupsProperty); }
            set { SetValue(AccountGroupsProperty, value); }
        }

        public static readonly DependencyProperty AccountGroupsProperty =
            DependencyProperty.Register("AccountGroups", typeof(AccountGroups), typeof(AccountGroupEdit), new FrameworkPropertyMetadata { DefaultValue = null });

        public AccountGroup Selected
        {
            get { return (AccountGroup)GetValue(SelectedProperty); }
            set { SetValue(SelectedProperty, value); }
        }

        public static readonly DependencyProperty SelectedProperty =
            DependencyProperty.Register("Selected", typeof(AccountGroup), typeof(AccountGroupEdit), new FrameworkPropertyMetadata { DefaultValue = null, BindsTwoWayByDefault = true });

        public AccountGroupEdit()
        {
            InitializeComponent();
        }

        private void DeleteGroupButton_Click(object sender, RoutedEventArgs e)
        {
            if (Selected == null || Selected.IsLocked)
            {
                return;
            }
            MessageBoxResult r = ModernDialog.ShowMessage(
                "Do you really want to remove group?",
                "Confirm delete", MessageBoxButton.YesNo, Window.GetWindow(this));
            if (r == MessageBoxResult.Yes)
            {
                AccountGroups.Groups.Remove(Selected);
                Selected = null;
            }
        }

        private void AddGroupButton_Click(object sender, RoutedEventArgs e)
        {
            var group = new AccountGroup();
            group.Name = "<New Group>";
            AccountGroups.Groups.Add(group);
            Selected = group;
        }

        private void LockUnlockGroupButton_Click(object sender, RoutedEventArgs e)
        {
            if (Selected == null || !Selected.IsLocked)
            {
                return;
            }
            string pass;
            while (true)
            {
                var r = ShowInputDialog("Enter passphrase", "Password required", out pass, Window.GetWindow(this));

                if (r == MessageBoxResult.OK)
                {
                    if (SecurityProvider.Unlock(Selected, pass))
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        private static MessageBoxResult ShowInputDialog(string text, string title, out string input, Window owner = null)
        {
            var dlg = new ModernDialog
            {
                Title = title,
                Content = new BBCodeBlock { BBCode = text, Margin = new Thickness(0, 0, 0, 8) },
                MinHeight = 0,
                MinWidth = 0,
                MaxHeight = 480,
                MaxWidth = 640,
            };
            if (owner != null)
            {
                dlg.Owner = owner;
            }

            dlg.Buttons = GetButtons(dlg);
            var tb = new TextBox();
            dlg.Content = tb;
            tb.Focus();
            dlg.ShowDialog();
            if (dlg.MessageBoxResult == MessageBoxResult.OK)
            {
                input = tb.Text;
            }
            else
            {
                input = "";
            }
            return dlg.MessageBoxResult;
        }

        private static IEnumerable<Button> GetButtons(ModernDialog d)
        {
            yield return d.OkButton;
            yield return d.CancelButton;
        }
    }
}
