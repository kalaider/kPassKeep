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
using System.ComponentModel;
using kPassKeep.Model;

namespace kPassKeep.Controls
{
    /// <summary>
    /// Interaction logic for Accounts.xaml
    /// </summary>
    public partial class Accounts : UserControl
    {
        public AccountGroup AccountGroup
        {
            get { return (AccountGroup)GetValue(AccountGroupProperty); }
            set { SetValue(AccountGroupProperty, value); }
        }

        public static readonly DependencyProperty AccountGroupProperty =
            DependencyProperty.Register("AccountGroup", typeof(AccountGroup), typeof(Accounts), new FrameworkPropertyMetadata { DefaultValue = null, PropertyChangedCallback = (d, o) => ((Accounts)d).GroupChanged() });
        
        public Account Selected
        {
            get { return (Account)GetValue(SelectedProperty); }
            set { SetValue(SelectedProperty, value); }
        }

        public static readonly DependencyProperty SelectedProperty =
            DependencyProperty.Register("Selected", typeof(Account), typeof(Accounts), new FrameworkPropertyMetadata { DefaultValue = null, BindsTwoWayByDefault = true });
        
        public ICollectionView FilteredAccounts
        {
            get { return (ICollectionView)GetValue(FilteredAccountsProperty); }
            set { SetValue(FilteredAccountsProperty, value); }
        }

        public static readonly DependencyProperty FilteredAccountsProperty =
            DependencyProperty.Register("FilteredAccounts", typeof(ICollectionView), typeof(Accounts), new PropertyMetadata(null));
        
        public string FilterText
        {
            get { return (string)GetValue(FilterTextProperty); }
            set { SetValue(FilterTextProperty, value); }
        }
        
        public static readonly DependencyProperty FilterTextProperty =
            DependencyProperty.Register("FilterText", typeof(string), typeof(Accounts), new PropertyMetadata(null));
        
        public Accounts()
        {
            InitializeComponent();
        }

        private void GroupChanged()
        {
            /** 
             *  #13 Target of newly selected account clears when
             *      group selection changes
             *  
             *  Description:
             *  
             *      When switching groups, selected account of the last
             *      selected group loses its Target property.
             *  
             *  Solution:
             *  
             *      Clear selection on group change.
             */
            Selected = null;

            if (AccountGroup != null)
            {
                var itemSourceList = new CollectionViewSource() { Source = AccountGroup.Accounts };
                FilteredAccounts = itemSourceList.View;
                FilteredAccounts.Filter = new Predicate<object>(o => {
                    if (String.IsNullOrWhiteSpace(FilterText))
                    {
                        return true;
                    }
                    var a = o as Account;
                    if (a.Target != null)
                    {
                        var t = a.Target;
                        if (t.Title != null)
                        {
                            if (t.Title.ToLower().Contains(FilterText.ToLower()))
                            {
                                return true;
                            }
                        }
                        if (t.Description != null)
                        {
                            if (t.Description.ToLower().Contains(FilterText.ToLower()))
                            {
                                return true;
                            }
                        }
                        if (t.Uri != null)
                        {
                            if (t.Uri.ToLower().Contains(FilterText.ToLower()))
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                });
            }
            else
            {
                FilteredAccounts = null;
            }
        }

        private void AddMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (AccountGroup == null)
            {
                return;
            }
            Account a = new Account();
            AccountGroup.Accounts.Add(a);
            Selected = a;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (Selected == null)
            {
                return;
            }
            MessageBoxResult r = ModernDialog.ShowMessage(
               "Do you really want to remove account?",
               "Confirm delete", MessageBoxButton.YesNo, Window.GetWindow(this));
            if (r == MessageBoxResult.Yes)
            {
                AccountGroup.Accounts.Remove(Selected);
                Selected = null;
            }
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (FilteredAccounts != null)
            {
                FilteredAccounts.Refresh();
            }
        }

        private void GotoButton_Click(object sender, RoutedEventArgs e)
        {
            if (Selected != null && Selected.Target != null && Selected.Target.Uri != null)
            {
                var url = Selected.Target.Uri;
                if (!url.StartsWith("http://")
                    && !url.StartsWith("https://"))
                {
                    url = "http://" + url;
                }
                System.Diagnostics.Process.Start(url);
            }
        }

        private void TargetToClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            if (Selected != null && Selected.Target != null && Selected.Target.Uri != null)
            {
                System.Windows.Clipboard.SetDataObject(Selected.Target.Uri);
            }
        }

        private void LoginToClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            if (Selected != null && Selected.Login != null && Selected.Login.Username != null)
            {
                System.Windows.Clipboard.SetDataObject(Selected.Login.Username);
            }
        }

        private void PasswordToClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            if (Selected != null && Selected.Password != null)
            {
                System.Windows.Clipboard.SetDataObject(Selected.Password);
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                FilterText = ((TextBox)sender).Text;
                if (FilteredAccounts != null)
                {
                    FilteredAccounts.Refresh();
                }
            }
        }
    }
}
