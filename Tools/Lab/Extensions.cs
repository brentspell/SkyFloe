using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyFloe.Lab
{
   public static class Extensions
   {
      public static Boolean IsBlank (this String str)
      {
         return String.IsNullOrWhiteSpace(str);
      }
      public static Func<T> AsFunc<T> (this T t)
      {
         return () => t;
      }
      public static IEnumerable<T> AsEnumerable<T> (this T t)
      {
         return new[] { t };
      }
      public static void Visit<T> (this IEnumerable<T> e)
      {
         foreach (var t in e)
            ;
      }
      public static void Visit<T> (this IEnumerable<T> e, Action<T> v)
      {
         foreach (var t in e)
            v(t);
      }
      public static void Visit<T> (this IEnumerable<T> e, Action<Int32, T> v)
      {
         Int32 i = 0;
         foreach (var t in e)
            v(i++, t);
      }
      public static void Visit<T, R> (
         this IEnumerable<T> e,
         Func<T, IEnumerable<R>> s,
         Action<R> v)
      {
         foreach (var t in e)
            foreach (var r in s(t))
               v(r);
      }
      public static T Coalesce<T> (this IEnumerable<T> e)
      {
         return e.FirstOrDefault(t => !Object.Equals(t, default(T)));
      }
   }
}
