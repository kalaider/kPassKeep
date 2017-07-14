using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kPassKeep.Model
{
    public class DescribableEntity : Entity
    {

        private string description;

        public string Description
        {
            get { return description; }
            set { description = value; OnPropertyChanged("Description"); }
        }
    }
}
