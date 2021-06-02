using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;

namespace RealGoodApps.CarefulEFCore.Example
{
    public class User
    {
        public long Id { get; set; }

        public bool SomeProperty { get; set; }

        public bool SomePropertyNew { get; set; }

        public static Expression<Func<User, bool>> ComputedSomePropertyExpression => u => u.SomeProperty || u.SomePropertyNew;

        [NotMapped]
        public bool ComputedSomeProperty
        {
            get
            {
                var func = ComputedSomePropertyExpression.Compile();
                return func.Invoke(this);
            }
        }
    }
}
