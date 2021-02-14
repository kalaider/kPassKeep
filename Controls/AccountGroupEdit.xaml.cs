using FirstFloor.ModernUI.Windows.Controls;
using System;
using System.Collections.Generic;
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
            DependencyProperty.Register("AccountGroups", typeof(AccountGroups), typeof(AccountGroupEdit), new FrameworkPropertyMetadata { DefaultValue = null, PropertyChangedCallback = (o, d) => ((AccountGroupEdit)o).AccountGroupsChanged() });

        public AccountGroup Selected
        {
            get { return (AccountGroup)GetValue(SelectedProperty); }
            set { SetValue(SelectedProperty, value); }
        }

        public static readonly DependencyProperty SelectedProperty =
            DependencyProperty.Register("Selected", typeof(AccountGroup), typeof(AccountGroupEdit), new FrameworkPropertyMetadata { DefaultValue = null, BindsTwoWayByDefault = true });

        public ICollectionView SortedGroups
        {
            get { return (ICollectionView)GetValue(SortedGroupsProperty); }
            set { SetValue(SortedGroupsProperty, value); }
        }

        public static readonly DependencyProperty SortedGroupsProperty =
            DependencyProperty.Register("SortedGroups", typeof(ICollectionView), typeof(AccountGroupEdit), new FrameworkPropertyMetadata { DefaultValue = null });

        public AccountGroupEdit()
        {
            InitializeComponent();
        }

        private void AccountGroupsChanged()
        {
            if (AccountGroups == null)
            {
                SortedGroups = null;
                return;
            }
            else
            {
                var itemSourceList = new CollectionViewSource() { Source = AccountGroups.Groups };
                itemSourceList.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                var g = AccountGroups.Groups;
                SortedGroups = itemSourceList.View;
                foreach (var el in g)
                {
                    RegisterGroup(el);
                }
                g.CollectionChanged += (s, e) =>
                {
                    if (s != g || SortedGroups == null)
                    {
                        return;
                    }
                    if (e.NewItems != null)
                    {
                        foreach (var el in e.NewItems)
                        {
                            RegisterGroup((AccountGroup)el);
                        }
                    }
                    SortedGroups.Refresh();
                };
            }
        }

        private void RegisterGroup(AccountGroup ag)
        {
            ag.PropertyChanged += (s2, e2) =>
            {
                if (s2 != ag || !"Name".Equals(e2.PropertyName))
                {
                    return;
                }
                SortedGroups.Refresh();
            };
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

        private void EditPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (Selected == null || Selected.IsLocked)
            {
                return;
            }
            string pass;
            while (true)
            {
                var r = ShowPasswordPromptWithConfirmation("Enter passphrase", "Enter passphrase", out pass, Window.GetWindow(this));

                if (r == MessageBoxResult.OK)
                {
                    Selected.Password = pass;
                    break;
                }
                else if (r == MessageBoxResult.Cancel)
                {
                    break;
                }
            }
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
                var r = ShowPasswordPrompt("Enter passphrase", "Password required", out pass, Window.GetWindow(this));

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

        private static MessageBoxResult ShowPasswordPrompt(string text, string title, out string input, Window owner = null)
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
            var tb = new PasswordBox();
            dlg.Content = tb;
            tb.Focus();
            dlg.ShowDialog();
            if (dlg.MessageBoxResult == MessageBoxResult.OK)
            {
                input = tb.Password;
            }
            else
            {
                input = "";
            }
            return dlg.MessageBoxResult;
        }

        private static MessageBoxResult ShowPasswordPromptWithConfirmation(string text, string title, out string input, Window owner = null)
        {
            var dlg = new ModernDialog
            {
                Title = title,
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

            var grid = new Grid();

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var passwordLabel = new TextBlock(new Run("Password:"));
            var confirmationLabel = new TextBlock(new Run("Confirmation:"));

            var password = new PasswordBox();
            var confirmation = new PasswordBox();

            Grid.SetColumn(passwordLabel, 0); Grid.SetRow(passwordLabel, 0);
            Grid.SetColumn(confirmationLabel, 0); Grid.SetRow(confirmationLabel, 1);
            Grid.SetColumn(password, 2); Grid.SetRow(password, 0);
            Grid.SetColumn(confirmation, 2); Grid.SetRow(confirmation, 1);

            grid.Children.Add(passwordLabel);
            grid.Children.Add(password);
            grid.Children.Add(confirmationLabel);
            grid.Children.Add(confirmation);

            dlg.Content = grid;
            password.Focus();

            dlg.ShowDialog();

            if (dlg.MessageBoxResult == MessageBoxResult.Cancel)
            {
                input = "";
                return dlg.MessageBoxResult;
            }

            if (dlg.MessageBoxResult == MessageBoxResult.OK
                && String.Equals(password.Password, confirmation.Password))
            {
                input = password.Password;
            }
            else
            {
                input = "";
            }
            if (!String.Equals(password.Password, confirmation.Password))
            {
                return MessageBoxResult.None;
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
