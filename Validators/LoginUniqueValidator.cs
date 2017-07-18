using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Globalization;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using kPassKeep.Model;

namespace kPassKeep.Validators
{
    public class LoginUniqueValidator : ValidationRule
    {

        public CollectionViewSource Logins { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value != null && Logins != null && Logins.Source != null)
            {
                if (((ObservableCollection<Login>)Logins.Source)
                    .Select(l => l.Username)
                    .Where(l => String.Equals(l, value))
                    .Any())
                {
                    return new ValidationResult(false, "Username should be unique");
                }
            }

            return new ValidationResult(true, null);
        }
    }
}
