using System;
using System.Reflection;
using NewLife.Reflection;

namespace NewLife.CubeUI.Models.Entity
{
    public class Entity<TEntity> : EntityBase where TEntity : Entity<TEntity>, new()
    {
        private static readonly BindingFlags bfic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.IgnoreCase;

        // TODO 增加Item缓存读过的设置过的属性
        public override Object this[String name]
        {
            get
            {
               var pi = GetType().GetProperty(name, bfic);
               Console.WriteLine(pi?.Name);
                if (pi != null && pi.CanRead) return pi.GetValue(this);

                return null;
            }
            set
            {
                var pi = GetType().GetProperty(name, bfic);
                Console.WriteLine(pi?.Name);
                if (pi != null && pi.CanWrite) pi.SetValue(this, value);
            }
        }
    }
}
