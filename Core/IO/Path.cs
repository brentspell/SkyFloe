using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SysPath = System.IO.Path;

namespace SkyFloe.IO
{
   [Serializable]
   public struct Path : 
      IComparable,
      IEquatable<Path>,
      IComparable<Path>,
      IEquatable<String>,
      IComparable<String>,
      IEnumerable<String>
   {
      private static readonly StringComparer Comparer = 
         StringComparer.OrdinalIgnoreCase;
      private static readonly Char[] Separators = 
      { 
         SysPath.DirectorySeparatorChar, 
         SysPath.AltDirectorySeparatorChar
      };
      public static Char Separator = SysPath.DirectorySeparatorChar;
      public static readonly Path Empty = new Path();
      private String value;

      #region Construction/Disposal
      public Path (String path)
      {
         this.value = (!String.IsNullOrWhiteSpace(path)) ?
            SysPath.GetFullPath(path) :
            null;
      }
      public Path (params String[] elements) : this(SysPath.Combine(elements))
      {
      }
      #endregion

      #region Properties
      public Boolean IsEmpty
      {
         get { return String.IsNullOrEmpty(ToString()); }
      }
      public String FileName
      {
         get { return SysPath.GetFileName(ToString()) ?? String.Empty; }
      }
      public String BaseFileName
      {
         get { return SysPath.GetFileNameWithoutExtension(ToString()) ?? String.Empty; }
      }
      public String Extension
      {
         get { return SysPath.GetExtension(ToString()); }
      }
      public Path Parent
      {
         get { return Pop(); }
      }
      public Path Root
      {
         get { return (!IsEmpty) ? SysPath.GetPathRoot(ToString()) : String.Empty; }
      }
      public static Path Current
      {
         get
         {
            return Environment.CurrentDirectory;
         }
         set
         {
            if (value.IsEmpty)
               throw new ArgumentException("value");
            Environment.CurrentDirectory = value;
         }
      }
      #endregion

      #region Operations
      public String[] Split ()
      {
         return ToString().Split(Separators, StringSplitOptions.RemoveEmptyEntries);
      }
      public Path Push (String next)
      {
         return new Path(SysPath.Combine(ToString(), next));
      }
      public Path Pop ()
      {
         return (!IsEmpty) ? SysPath.GetDirectoryName(ToString()) : String.Empty;
      }
      public Boolean IsParent (Path path)
      {
         return this == path.Parent;
      }
      public Boolean IsChild (Path path)
      {
         return this.Parent == path;
      }
      public Boolean IsAncestor (Path path)
      {
         if (path.ToString().Length < ToString().Length)
            return false;
         return Comparer.Equals(
            ToString(),
            path.ToString().Substring(0, ToString().Length)
         );
      }
      public Boolean IsDescendant (Path path)
      {
         return path.IsAncestor(this);
      }
      #endregion

      #region Object Overrides
      public override String ToString ()
      {
         return this.value ?? String.Empty;
      }
      public override Boolean Equals (Object other)
      {
         String value = null;
         if (other is Path)
            value = ((Path)other).value;
         else if (other is String)
            value = (String)other;
         else if (other != null)
            return false;
         if (String.IsNullOrWhiteSpace(value))
            value = String.Empty;
         return Comparer.Equals(ToString(), value);
      }
      public override Int32 GetHashCode ()
      {
         return Comparer.GetHashCode(ToString());
      }
      #endregion

      #region IComparable Implementation
      public Int32 CompareTo (Object other)
      {
         String value = null;
         if (other is Path)
            value = ((Path)other).value;
         else if (other is String)
            value = (String)other;
         else if (other != null)
            throw new ArgumentException("other");
         if (String.IsNullOrWhiteSpace(value))
            value = String.Empty;
         return Comparer.Compare(ToString(), value);
      }
      #endregion

      #region IEquatable<Path> Implementation
      public Boolean Equals (Path other)
      {
         return Comparer.Equals(ToString(), other.ToString());
      }
      #endregion

      #region IComparable<Path> Implementation
      public Int32 CompareTo (Path other)
      {
         return Comparer.Compare(ToString(), other.ToString());
      }
      #endregion

      #region IEquatable<String> Implementation
      public Boolean Equals (String other)
      {
         if (String.IsNullOrWhiteSpace(other))
            other = String.Empty;
         return Comparer.Equals(ToString(), other ?? String.Empty);
      }
      #endregion

      #region IComparable<String> Implementation
      public Int32 CompareTo (String other)
      {
         if (String.IsNullOrWhiteSpace(other))
            other = String.Empty;
         return Comparer.Compare(ToString(), other ?? String.Empty);
      }
      #endregion

      #region IEnumerable Implementation
      public IEnumerator<String> GetEnumerator ()
      {
         return ((IEnumerable<String>)Split()).GetEnumerator();
      }
      IEnumerator IEnumerable.GetEnumerator ()
      {
         return Split().GetEnumerator();
      }
      #endregion

      #region Custom Operators
      public static implicit operator String (Path path)
      {
         return path.value ?? String.Empty;
      }
      public static implicit operator Path (String str)
      {
         return new Path(str);
      }
      public static Boolean operator == (Path path1, Path path2)
      {
         return path1.Equals(path2);
      }
      public static Boolean operator != (Path path1, Path path2)
      {
         return !path1.Equals(path2);
      }
      public static Boolean operator < (Path path1, Path path2)
      {
         return path1.CompareTo(path2) < 0;
      }
      public static Boolean operator <= (Path path1, Path path2)
      {
         return path1.CompareTo(path2) <= 0;
      }
      public static Boolean operator > (Path path1, Path path2)
      {
         return path1.CompareTo(path2) > 0;
      }
      public static Boolean operator >= (Path path1, Path path2)
      {
         return path1.CompareTo(path2) >= 0;
      }
      public static Boolean operator == (Path path, String str)
      {
         return path.Equals(str);
      }
      public static Boolean operator == (String str, Path path)
      {
         return path.Equals(str);
      }
      public static Boolean operator != (Path path, String str)
      {
         return !path.Equals(str);
      }
      public static Boolean operator != (String str, Path path)
      {
         return !path.Equals(str);
      }
      public static Boolean operator < (Path path, String str)
      {
         return path.CompareTo(str) < 0;
      }
      public static Boolean operator < (String str, Path path)
      {
         return path.CompareTo(str) > 0;
      }
      public static Boolean operator <= (Path path, String str)
      {
         return path.CompareTo(str) <= 0;
      }
      public static Boolean operator <= (String str, Path path)
      {
         return path.CompareTo(str) >= 0;
      }
      public static Boolean operator > (Path path, String str)
      {
         return path.CompareTo(str) > 0;
      }
      public static Boolean operator > (String str, Path path)
      {
         return path.CompareTo(str) < 0;
      }
      public static Boolean operator >= (Path path, String str)
      {
         return path.CompareTo(str) >= 0;
      }
      public static Boolean operator >= (String str, Path path)
      {
         return path.CompareTo(str) <= 0;
      }
      public static Path operator + (Path path, String str)
      {
         return path.Push(str);
      }
      #endregion
   }
}
