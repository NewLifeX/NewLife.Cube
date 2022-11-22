using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Reflection;
using XCode;
using XCode.Membership;
using Xunit;

namespace XUnitTest
{
    public class EntityExtensionTests
    {
        [Fact]
        public void ToDictionary()
        {
            var list = new List<User>
            {
                new User { Name = "Stone", DisplayName = "大石头" },
                new User { Name = "NewLife", DisplayName = "新生命" }
            };

            var type = list.GetType();
            var value = (Object)list;

            var rs1 = type.As(typeof(IEnumerable<>));
            Assert.True(rs1);

            var elmType = type.GetElementTypeEx();
            Assert.Equal(typeof(User), elmType);

            var rs2 = elmType.As(typeof(IEntity));
            Assert.True(rs2);

            var mtype = typeof(EntityExtension);
            var method = mtype.GetMethod("ToDictionary");
            Assert.NotNull(method);

            //var vs = mtype.Invoke(method, value);
            //Assert.NotNull(vs as IEnumerable);

            IEntityFactory factory = null;
            var dic = new Dictionary<String, String>();
            foreach (IEntity entity in value as IEnumerable)
            {
                factory ??= entity.GetType().AsFactory();

                var key = entity[factory.Unique.Name] + "";
                dic[key] = entity + "";
            }
            Assert.True(dic.Count > 0);
        }
    }
}