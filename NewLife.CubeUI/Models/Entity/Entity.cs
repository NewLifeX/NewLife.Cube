using System;
using System.Collections.Generic;
using System.Reflection;
using NewLife.Reflection;

namespace NewLife.CubeUI.Models.Entity
{
    public class Entity<TEntity> : EntityBase where TEntity : class, new()
    {
        private static readonly BindingFlags bfic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.IgnoreCase;

        private Dictionary<String, Object> _item = new Dictionary<String, Object>();

        public override Object this[String name]
        {
            get
            {
                if (_item.TryGetValue(name, out var v))
                {
                    return v;
                }

                var pi = GetType().GetProperty(name, bfic);
                //Console.WriteLine(pi?.Name);
                if (pi != null && pi.CanRead) return pi.GetValue(this);

                return null;
            }
            set
            {
                if (typeof(TEntity) == typeof(Dictionary<String, Object>))
                {
                    _item[name] = value;
                    return;
                }

                var pi = GetType().GetProperty(name, bfic);
                //Console.WriteLine(pi?.Name);
                if (pi != null && pi.CanWrite) pi.SetValue(this, value);

                _item[name] = value;
            }
        }

        public virtual Entity<TEntity> SetValues(Dictionary<String, Object> items)
        {
            _item = items;
            return this;
        }
    }
    public class Entity : Entity<Dictionary<String, Object>>
    {
    }
}
