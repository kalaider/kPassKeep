using System;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FirstFloor.ModernUI.Windows.Controls;

namespace kPassKeep.Util
{
    public static class UIHelper
    {
        public static readonly string LONG_ENCRYPTION_DECRYPTION_ACTION_TEXT =
            "Encryption/decryption is in progress. It can take some time because " +
            "we need to take actions in order to make brute force attack on " +
            "the provided password much more difficult. An attacker will have to " +
            "repeat the same actions for each password he/she tries.\n\n" +
            "The window may become unresponsive for some time.";

        public static T WithLongActionWarning<T>(Window owner, string title, string text, Func<T> action)
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
            dlg.Buttons = new Button[0];

            T[] result = new T[1];
            
            dlg.ContentRendered += (sender, args) =>
            {
                result[0] = action.Invoke();
                dlg.Close();
            };
            
            dlg.ShowDialog();

            return result[0];
        }

        public static T WithLongEncryptionDecryptionWarning<T>(Window owner, string title, Func<T> action)
            => WithLongActionWarning(owner, title, LONG_ENCRYPTION_DECRYPTION_ACTION_TEXT, action);

        public static void WithLongEncryptionDecryptionWarning(Window owner, string title, Action action)
        {
            WithLongEncryptionDecryptionWarning<object>(owner, title, () =>
            {
                action.Invoke();
                return null;
            });
        }
    }
}
