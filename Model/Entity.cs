using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FirstFloor.ModernUI.Presentation;

namespace kPassKeep.Model
{
    public class RawSimpleEntity : Entity
    {

        public byte[] Data { get; set; }
    }

    public class Entity : NotifyPropertyChanged, IEquatable<Entity>
    {

        private Guid guid = Guid.NewGuid();

        public Entity()
        {
        }

        public Guid Guid { get { return guid; } set { guid = value; } }

        public override bool Equals(object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            Entity p = obj as Entity;
            if ((object) p == null)
            {
                return false;
            }

            // Return true if the fields match:
            return guid.Equals(p.Guid);
        }

        public bool Equals(Entity p)
        {
            // If parameter is null return false:
            if ((object)p == null)
            {
                return false;
            }

            // Return true if the fields match:
            return guid.Equals(p.Guid); ;
        }

        public override int GetHashCode()
        {
            return guid.GetHashCode();
        }
    }
}