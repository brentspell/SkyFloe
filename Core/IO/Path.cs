//===========================================================================
// MODULE:  Path.cs
// PURPOSE: file system path shim struct
// 
// Copyright © 2013
// Brent M. Spell. All rights reserved.
//
// This library is free software; you can redistribute it and/or modify it 
// under the terms of the GNU Lesser General Public License as published 
// by the Free Software Foundation; either version 3 of the License, or 
// (at your option) any later version. This library is distributed in the 
// hope that it will be useful, but WITHOUT ANY WARRANTY; without even the 
// implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
// See the GNU Lesser General Public License for more details. You should 
// have received a copy of the GNU Lesser General Public License along with 
// this library; if not, write to 
//    Free Software Foundation, Inc. 
//    51 Franklin Street, Fifth Floor 
//    Boston, MA 02110-1301 USA
//===========================================================================
// System References
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SysPath = System.IO.Path;
// Project References

namespace SkyFloe.IO
{
   /// <summary>
   /// Path structure
   /// </summary>
   /// <remarks>
   /// This class encapsulates an absolute path within the file system,
   /// providing an easy to use wrapper over the tedious System.IO.Path 
   /// facade.
   /// </remarks>
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
      /// <summary>
      /// Initializes a new path instance, canonicalizing a set
      /// of path elements into an absolute file system path
      /// </summary>
      /// <param name="elements">
      /// The path elements to concatenate to form the path
      /// </param>
      public Path (params String[] elements)
      {
         var path = SysPath.Combine(
            elements
               .Select(p => (!String.IsNullOrWhiteSpace(p)) ? p : String.Empty)
               .ToArray()
         );
         this.value = (!String.IsNullOrWhiteSpace(path)) ?
            SysPath.GetFullPath(path) :
            String.Empty;
      }
      #endregion

      #region Properties
      /// <summary>
      /// Indicates whether the path is empty (phew!)
      /// </summary>
      public Boolean IsEmpty
      {
         get { return String.IsNullOrEmpty(ToString()); }
      }
      /// <summary>
      /// Returns the path element at the end of the path
      /// </summary>
      public String Top
      {
         get { return SysPath.GetFileName(ToString()) ?? String.Empty; }
      }
      /// <summary>
      /// Retrieves the file name portion of the path
      /// </summary>
      public String FileName
      {
         get { return this.Top; }
      }
      /// <summary>
      /// Retrieves the file name portion of the path, 
      /// excluding the extension, if any
      /// </summary>
      public String BaseFileName
      {
         get { return SysPath.GetFileNameWithoutExtension(ToString()) ?? String.Empty; }
      }
      /// <summary>
      /// Retrieves the extension of the file path, if any
      /// </summary>
      public String Extension
      {
         get { return SysPath.GetExtension(ToString()); }
      }
      /// <summary>
      /// Retrieves the path to the parent directory
      /// </summary>
      public Path Parent
      {
         get { return Pop(); }
      }
      /// <summary>
      /// Retrieves the path to the root directory
      /// </summary>
      public Path Root
      {
         get { return (!IsEmpty) ? (Path)SysPath.GetPathRoot(ToString()) : Path.Empty; }
      }
      /// <summary>
      /// Retrieves the application's current working directory
      /// </summary>
      public static Path Current
      {
         get
         {
            return (Path)Environment.CurrentDirectory;
         }
         set
         {
            if (value.IsEmpty)
               throw new ArgumentException("value");
            Environment.CurrentDirectory = value;
         }
      }
      /// <summary>
      /// Retrieves the path to the application's temporary directory
      /// </summary>
      public static Path Temp
      {
         get { return (Path)SysPath.GetTempPath(); }
      }
      #endregion

      #region Operations
      /// <summary>
      /// Splits the path into its elements
      /// </summary>
      /// <returns>
      /// The array of path elements
      /// </returns>
      public String[] Split ()
      {
         return ToString().Split(Separators, StringSplitOptions.RemoveEmptyEntries);
      }
      /// <summary>
      /// Creates a new path with an additional element
      /// </summary>
      /// <param name="next">
      /// The element to add to the path
      /// </param>
      /// <returns>
      /// The concatenated path
      /// </returns>
      public Path Push (String next)
      {
         return (!String.IsNullOrWhiteSpace(next)) ?
            new Path(SysPath.Combine(ToString(), next)) :
            this;
      }
      /// <summary>
      /// Creates a new path to the parent directory
      /// </summary>
      /// <returns>
      /// The truncated path
      /// </returns>
      public Path Pop ()
      {
         return (!IsEmpty) ? (Path)SysPath.GetDirectoryName(ToString()) : Path.Empty;
      }
      /// <summary>
      /// Determines whether the current path is the parent of another
      /// </summary>
      /// <param name="path">
      /// The path to test
      /// </param>
      /// <returns>
      /// True if this is the parent of the specified path
      /// False otherwise
      /// </returns>
      public Boolean IsParent (Path path)
      {
         if (IsEmpty || path.IsEmpty)
            return false;
         return this == path.Parent;
      }
      /// <summary>
      /// Determines whether the current path is a child of another
      /// </summary>
      /// <param name="path">
      /// The path to test
      /// </param>
      /// <returns>
      /// True if this is a child of the specified path
      /// False otherwise
      /// </returns>
      public Boolean IsChild (Path path)
      {
         return path.IsParent(this);
      }
      /// <summary>
      /// Determines whether the current path is an ancestor of another
      /// </summary>
      /// <param name="path">
      /// The path to test
      /// </param>
      /// <returns>
      /// True if this is an ancestor of the specified path
      /// False otherwise
      /// </returns>
      public Boolean IsAncestor (Path path)
      {
         if (IsEmpty || path.IsEmpty)
            return false;
         if (path.ToString().Length < ToString().Length)
            return false;
         return Comparer.Equals(
            ToString(),
            path.ToString().Substring(0, ToString().Length)
         );
      }
      /// <summary>
      /// Determines whether the current path is a descendant of another
      /// </summary>
      /// <param name="path">
      /// The path to test
      /// </param>
      /// <returns>
      /// True if this is a descendant of the specified path
      /// False otherwise
      /// </returns>
      public Boolean IsDescendant (Path path)
      {
         return path.IsAncestor(this);
      }
      #endregion

      #region Object Overrides
      /// <summary>
      /// Converts the path to string
      /// </summary>
      /// <returns>
      /// The path string
      /// </returns>
      public override String ToString ()
      {
         return this.value ?? String.Empty;
      }
      /// <summary>
      /// Object comparison
      /// </summary>
      /// <param name="other">
      /// The object to compare
      /// </param>
      /// <returns>
      /// True if the objects are semantically equal
      /// False otherwise
      /// </returns>
      public override Boolean Equals (Object other)
      {
         var value = (String)null;
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
      /// <summary>
      /// Computes a hash code for the path
      /// </summary>
      /// <returns>
      /// The path hash code
      /// </returns>
      public override Int32 GetHashCode ()
      {
         return Comparer.GetHashCode(ToString());
      }
      #endregion

      #region IComparable Implementation
      /// <summary>
      /// Object ordering
      /// </summary>
      /// <param name="other">
      /// The object to compare
      /// </param>
      /// <returns>
      /// &lt; 0 if this &lt; other
      /// = 0 if the this == other
      /// &gt; 0 if this &gt; other
      /// </returns>
      public Int32 CompareTo (Object other)
      {
         var value = (String)null;
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
      /// <summary>
      /// Path comparison
      /// </summary>
      /// <param name="other">
      /// The path to compare
      /// </param>
      /// <returns>
      /// True if the paths are equivalent
      /// False otherwise
      /// </returns>
      public Boolean Equals (Path other)
      {
         return Comparer.Equals(ToString(), other.ToString());
      }
      #endregion

      #region IComparable<Path> Implementation
      /// <summary>
      /// Path ordering
      /// </summary>
      /// <param name="other">
      /// The path to compare
      /// </param>
      /// <returns>
      /// &lt; 0 if this &lt; other
      /// = 0 if the this == other
      /// &gt; 0 if this &gt; other
      /// </returns>
      public Int32 CompareTo (Path other)
      {
         return Comparer.Compare(ToString(), other.ToString());
      }
      #endregion

      #region IEquatable<String> Implementation
      /// <summary>
      /// Path-string comparison
      /// </summary>
      /// <param name="other">
      /// The path string to compare
      /// </param>
      /// <returns>
      /// True if the path and string are equivalent
      /// False otherwise
      /// </returns>
      public Boolean Equals (String other)
      {
         if (String.IsNullOrWhiteSpace(other))
            other = String.Empty;
         return Comparer.Equals(ToString(), other ?? String.Empty);
      }
      #endregion

      #region IComparable<String> Implementation
      /// <summary>
      /// Path-string ordering
      /// </summary>
      /// <param name="other">
      /// The path string to compare
      /// </param>
      /// <returns>
      /// &lt; 0 if this &lt; other
      /// = 0 if the this == other
      /// &gt; 0 if this &gt; other
      /// </returns>
      public Int32 CompareTo (String other)
      {
         if (String.IsNullOrWhiteSpace(other))
            other = String.Empty;
         return Comparer.Compare(ToString(), other ?? String.Empty);
      }
      #endregion

      #region IEnumerable Implementation
      /// <summary>
      /// Enumerates the elements of the path
      /// </summary>
      /// <returns>
      /// An enumerator for the path elements
      /// </returns>
      public IEnumerator<String> GetEnumerator ()
      {
         return ((IEnumerable<String>)Split()).GetEnumerator();
      }
      /// <summary>
      /// Enumerates the elements of the path
      /// </summary>
      /// <returns>
      /// An enumerator for the path elements
      /// </returns>
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
      public static explicit operator Path (String str)
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
