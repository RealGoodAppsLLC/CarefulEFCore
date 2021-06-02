using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace RealGoodApps.CarefulEFCore.Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var t = new Test(null);
            t.HelloWorld(null);
        }
    }

    public class Test
    {
        private readonly SampleDbContext _context;

        public Test(SampleDbContext context)
        {
            _context = context;
        }

        private Expression<Func<User, bool>> expr;

        public void HelloWorld(Expression<Func<User, bool>> a)
        {
            var good = _context.Users.Where(u => true);
            var good2 = _context.Users.Where(u => u.SomeProperty);
            Expression<Func<User,bool>> expression = u => u.SomeProperty;
            var good3 = _context.Users.Where(expression);
            var good5 = _context.Users.Where(a);

            // these are all compile errors
            IEnumerable<User> b = new List<User>();

            // var m = b.AsQueryable().Where(expr);
            // var bad = _context.Users.Where(User.ComputedSomePropertyExpression);
            // var bad2 = _context.Users.Where(User.ComputedSomePropertyExpression.Or(u => true));
            // var bad3 = _context.Users.FirstOrDefault(User.ComputedSomePropertyExpression.Or(u => true));
            // var bad4 = _context.Users.Any(User.ComputedSomePropertyExpression.Or(u => true));
            // var bad5 = _context.Users.Where(expr);
            // var bad6 = _context.Users.FirstOrDefaultAsync(expr);
            // var bad7 = _context.Users.OrderByDescending(expr);
        }
    }

    public static class PredicateBuilder
    {
        public static Expression<Func<T, bool>> Or<T> (
            this Expression<Func<T, bool>> expr1,
            Expression<Func<T, bool>> expr2)
        {
            var invokedExpr = Expression.Invoke (expr2, expr1.Parameters.Cast<Expression> ());
            return Expression.Lambda<Func<T, bool>>
                (Expression.OrElse(expr1.Body, invokedExpr), expr1.Parameters);
        }

        public static Expression<Func<T, bool>> And<T> (
            this Expression<Func<T, bool>> expr1,
            Expression<Func<T, bool>> expr2)
        {
            var invokedExpr = Expression.Invoke (expr2, expr1.Parameters.Cast<Expression> ());
            return Expression.Lambda<Func<T, bool>>
                (Expression.AndAlso(expr1.Body, invokedExpr), expr1.Parameters);
        }

        public static Expression<Func<T, bool>> Not<T> (
            this Expression<Func<T, bool>> self)
        {
            var candidateExpr = self.Parameters[0];
            var body = Expression.Not(self.Body);

            return Expression.Lambda<Func<T, bool>>(body, candidateExpr);
        }

        public static Expression<Func<T, bool>> IsEqual<T>(
            this Expression<Func<T, bool>> self,
            bool comparison)
        {
            var candidateExpr = self.Parameters[0];
            var body = Expression.Equal(self.Body, Expression.Constant(comparison));

            return Expression.Lambda<Func<T, bool>>(body, candidateExpr);
        }
    }
}
