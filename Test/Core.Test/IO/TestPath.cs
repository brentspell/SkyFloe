//===========================================================================
// MODULE:  TestPath.cs
// PURPOSE: file system path wrapper unit test
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
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SysPath = System.IO.Path;
// Project References
using SkyFloe.IO;

namespace SkyFloe.Core.Test.IO
{
   [TestClass]
   public class TestPath
   {

      [TestMethod]
      public void TestConstruction ()
      {
         // empty construction
         Assert.IsTrue(default(Path).IsEmpty);
         Assert.IsTrue(Path.Empty.IsEmpty);
         Assert.IsTrue(new Path().IsEmpty);
         Assert.IsTrue(new Path((String)null).IsEmpty);
         Assert.IsTrue(new Path("").IsEmpty);
         Assert.IsTrue(new Path(" ").IsEmpty);
         Assert.IsTrue(new Path("  ").IsEmpty);
         // absolute path construction
         Assert.AreEqual(
            new Path(SysPath.GetFullPath("test")).ToString(), 
            SysPath.GetFullPath("test")
         );
         Assert.AreEqual(
            new Path(SysPath.GetFullPath(SysPath.Combine("test", "testa"))).ToString(), 
            SysPath.GetFullPath(SysPath.Combine("test", "testa"))
         );
         Assert.AreEqual(
            new Path(SysPath.GetFullPath(SysPath.Combine("test", "test.exe"))).ToString(), 
            SysPath.GetFullPath(SysPath.Combine("test", "test.exe"))
         );
         // relative path construction
         Assert.AreEqual(
            new Path("test").ToString(), 
            SysPath.GetFullPath("test")
         );
         Assert.AreEqual(
            new Path(SysPath.Combine("test", "testa")).ToString(), 
            SysPath.GetFullPath(SysPath.Combine("test", "testa"))
         );
         Assert.AreEqual(
            new Path(SysPath.Combine("test", "test.exe")).ToString(), 
            SysPath.GetFullPath(SysPath.Combine("test", "test.exe"))
         );
         // combined path construction
         Assert.AreEqual(
            new Path(new[] { "test" }).ToString(),
            SysPath.GetFullPath("test")
         );
         Assert.AreEqual(
            new Path("test", "testa").ToString(), 
            SysPath.GetFullPath(SysPath.Combine("test", "testa"))
         );
         Assert.AreEqual(
            new Path("test", "test.exe").ToString(),
            SysPath.GetFullPath(SysPath.Combine("test", "test.exe"))
         );
         Assert.AreEqual(
            new Path(null, "test", null).ToString(),
            SysPath.GetFullPath("test")
         );
         Assert.AreEqual(
            new Path("", "test", "").ToString(),
            SysPath.GetFullPath("test")
         );
         Assert.AreEqual(
            new Path(" ", "test", " ").ToString(),
            SysPath.GetFullPath("test")
         );
      }

      [TestMethod]
      public void TestComparison ()
      {
         // invalid comparison
         {
            Assert.IsFalse(Path.Empty.Equals((Object)42));
            Assert.IsFalse(new Path("test").Equals((Object)42));
            AssertException(() => Path.Empty.CompareTo((Object)42));
            AssertException(() => new Path("test").CompareTo((Object)42));
         }
         // path-path comparison (empty-empty)
         {
            var p1 = Path.Empty;
            var p2 = Path.Empty;
            Assert.IsTrue(p1.Equals((Object)p2));
            Assert.IsTrue(p1.Equals(p2));
            Assert.IsTrue(p1 == p2);
            Assert.IsFalse(p1 != p2);
            Assert.IsTrue(p1.CompareTo((Object)p2) == 0);
            Assert.IsTrue(p1.CompareTo(p2) == 0);
            Assert.IsFalse(p1 < p2);
            Assert.IsTrue(p1 <= p2);
            Assert.IsFalse(p1 > p2);
            Assert.IsTrue(p1 >= p2);
         }
         // path-path comparison (empty-valid)
         {
            var p1 = Path.Empty;
            var p2 = new Path("test");
            Assert.IsFalse(p1.Equals((Object)p2));
            Assert.IsFalse(p1.Equals(p2));
            Assert.IsFalse(p1 == p2);
            Assert.IsTrue(p1 != p2);
            Assert.IsTrue(p1.CompareTo((Object)p2) < 0);
            Assert.IsTrue(p1.CompareTo(p2) < 0);
            Assert.IsTrue(p1 < p2);
            Assert.IsTrue(p1 <= p2);
            Assert.IsFalse(p1 > p2);
            Assert.IsFalse(p1 >= p2);
         }
         // path-path comparison (valid-valid)
         {
            var p1 = new Path("test", "testa");
            var p2 = new Path("test", "testa");
            var p3 = new Path("test", "testb");
            Assert.IsTrue(p1.Equals((Object)p2));
            Assert.IsFalse(p1.Equals((Object)p3));
            Assert.IsTrue(p1.Equals(p2));
            Assert.IsFalse(p1.Equals(p3));
            Assert.IsTrue(p1 == p2);
            Assert.IsFalse(p1 != p2);
            Assert.IsFalse(p1 == p3);
            Assert.IsTrue(p1 != p3);
            Assert.IsTrue(p1.CompareTo((Object)p2) == 0);
            Assert.IsTrue(p1.CompareTo((Object)p3) < 0);
            Assert.IsTrue(p1.CompareTo(p2) == 0);
            Assert.IsTrue(p1.CompareTo(p3) < 0);
            Assert.IsFalse(p1 < p2);
            Assert.IsTrue(p1 < p3);
            Assert.IsTrue(p1 <= p2);
            Assert.IsTrue(p1 <= p3);
            Assert.IsFalse(p1 > p2);
            Assert.IsFalse(p1 > p3);
            Assert.IsTrue(p1 >= p2);
            Assert.IsFalse(p1 >= p3);
         }
         // path-path comparison (case insensitivity)
         {
            var p1 = new Path("TEST", "TESTA");
            var p2 = new Path("test", "testa");
            var p3 = new Path("TeSt", "TeStB");
            Assert.IsTrue(p1.Equals((Object)p2));
            Assert.IsFalse(p1.Equals((Object)p3));
            Assert.IsTrue(p1.Equals(p2));
            Assert.IsFalse(p1.Equals(p3));
            Assert.IsTrue(p1 == p2);
            Assert.IsFalse(p1 != p2);
            Assert.IsFalse(p1 == p3);
            Assert.IsTrue(p1 != p3);
            Assert.IsTrue(p1.CompareTo((Object)p2) == 0);
            Assert.IsTrue(p1.CompareTo((Object)p3) < 0);
            Assert.IsTrue(p1.CompareTo(p2) == 0);
            Assert.IsTrue(p1.CompareTo(p3) < 0);
            Assert.IsFalse(p1 < p2);
            Assert.IsTrue(p1 < p3);
            Assert.IsTrue(p1 <= p2);
            Assert.IsTrue(p1 <= p3);
            Assert.IsFalse(p1 > p2);
            Assert.IsFalse(p1 > p3);
            Assert.IsTrue(p1 >= p2);
            Assert.IsFalse(p1 >= p3);
         }
         // path-path comparison (valid-empty)
         {
            var p1 = new Path("test", "testa");
            var p2 = Path.Empty;
            Assert.IsFalse(p1.Equals((Object)p2));
            Assert.IsFalse(p1.Equals(p2));
            Assert.IsFalse(p1 == p2);
            Assert.IsTrue(p1 != p2);
            Assert.IsTrue(p1.CompareTo((Object)p2) > 0);
            Assert.IsTrue(p1.CompareTo(p2) > 0);
            Assert.IsFalse(p1 < p2);
            Assert.IsFalse(p1 <= p2);
            Assert.IsTrue(p1 > p2);
            Assert.IsTrue(p1 >= p2);
         }
         // path-string comparison (empty-null)
         {
            var p1 = Path.Empty;
            var p2 = (String)null;
            Assert.IsTrue(p1.Equals((Object)p2));
            Assert.IsTrue(p1.Equals(p2));
            Assert.IsTrue(p1 == p2);
            Assert.IsFalse(p1 != p2);
            Assert.IsTrue(p1.CompareTo((Object)p2) == 0);
            Assert.IsTrue(p1.CompareTo(p2) == 0);
            Assert.IsFalse(p1 < p2);
            Assert.IsTrue(p1 <= p2);
            Assert.IsFalse(p1 > p2);
            Assert.IsTrue(p1 >= p2);
         }
         // path-string comparison (empty-empty)
         {
            var p1 = Path.Empty;
            var p2 = String.Empty;
            Assert.IsTrue(p1.Equals((Object)p2));
            Assert.IsTrue(p1.Equals(p2));
            Assert.IsTrue(p1 == p2);
            Assert.IsFalse(p1 != p2);
            Assert.IsTrue(p1.CompareTo((Object)p2) == 0);
            Assert.IsTrue(p1.CompareTo(p2) == 0);
            Assert.IsFalse(p1 < p2);
            Assert.IsTrue(p1 <= p2);
            Assert.IsFalse(p1 > p2);
            Assert.IsTrue(p1 >= p2);
         }
         // path-string comparison (empty-space)
         {
            var p1 = Path.Empty;
            var p2 = " ";
            Assert.IsTrue(p1.Equals((Object)p2));
            Assert.IsTrue(p1.Equals(p2));
            Assert.IsTrue(p1 == p2);
            Assert.IsFalse(p1 != p2);
            Assert.IsTrue(p1.CompareTo((Object)p2) == 0);
            Assert.IsTrue(p1.CompareTo(p2) == 0);
            Assert.IsFalse(p1 < p2);
            Assert.IsTrue(p1 <= p2);
            Assert.IsFalse(p1 > p2);
            Assert.IsTrue(p1 >= p2);
         }
         // path-string comparison (empty-valid)
         {
            var p1 = Path.Empty;
            var p2 = SysPath.GetFullPath("test");
            Assert.IsFalse(p1.Equals((Object)p2));
            Assert.IsFalse(p1.Equals(p2));
            Assert.IsFalse(p1 == p2);
            Assert.IsTrue(p1 != p2);
            Assert.IsTrue(p1.CompareTo((Object)p2) < 0);
            Assert.IsTrue(p1.CompareTo(p2) < 0);
            Assert.IsTrue(p1 < p2);
            Assert.IsTrue(p1 <= p2);
            Assert.IsFalse(p1 > p2);
            Assert.IsFalse(p1 >= p2);
         }
         // path-string comparison (valid-valid)
         {
            var p1 = new Path("test", "testa");
            var p2 = SysPath.GetFullPath(SysPath.Combine("test", "testa"));
            var p3 = SysPath.GetFullPath(SysPath.Combine("test", "testb"));
            Assert.IsTrue(p1.Equals((Object)p2));
            Assert.IsFalse(p1.Equals((Object)p3));
            Assert.IsTrue(p1.Equals(p2));
            Assert.IsFalse(p1.Equals(p3));
            Assert.IsTrue(p1 == p2);
            Assert.IsFalse(p1 != p2);
            Assert.IsFalse(p1 == p3);
            Assert.IsTrue(p1 != p3);
            Assert.IsTrue(p1.CompareTo((Object)p2) == 0);
            Assert.IsTrue(p1.CompareTo((Object)p3) < 0);
            Assert.IsTrue(p1.CompareTo(p2) == 0);
            Assert.IsTrue(p1.CompareTo(p3) < 0);
            Assert.IsFalse(p1 < p2);
            Assert.IsTrue(p1 < p3);
            Assert.IsTrue(p1 <= p2);
            Assert.IsTrue(p1 <= p3);
            Assert.IsFalse(p1 > p2);
            Assert.IsFalse(p1 > p3);
            Assert.IsTrue(p1 >= p2);
            Assert.IsFalse(p1 >= p3);
         }
         // path-string comparison (case insensitivity)
         {
            var p1 = new Path("TEST", "TESTA");
            var p2 = SysPath.GetFullPath(SysPath.Combine("test", "testa"));
            var p3 = SysPath.GetFullPath(SysPath.Combine("TeSt", "TeStB"));
            Assert.IsTrue(p1.Equals((Object)p2));
            Assert.IsFalse(p1.Equals((Object)p3));
            Assert.IsTrue(p1.Equals(p2));
            Assert.IsFalse(p1.Equals(p3));
            Assert.IsTrue(p1 == p2);
            Assert.IsFalse(p1 != p2);
            Assert.IsFalse(p1 == p3);
            Assert.IsTrue(p1 != p3);
            Assert.IsTrue(p1.CompareTo((Object)p2) == 0);
            Assert.IsTrue(p1.CompareTo((Object)p3) < 0);
            Assert.IsTrue(p1.CompareTo(p2) == 0);
            Assert.IsTrue(p1.CompareTo(p3) < 0);
            Assert.IsFalse(p1 < p2);
            Assert.IsTrue(p1 < p3);
            Assert.IsTrue(p1 <= p2);
            Assert.IsTrue(p1 <= p3);
            Assert.IsFalse(p1 > p2);
            Assert.IsFalse(p1 > p3);
            Assert.IsTrue(p1 >= p2);
            Assert.IsFalse(p1 >= p3);
         }
         // path-string comparison (valid-space)
         {
            var p1 = new Path("test", "testa");
            var p2 = " ";
            Assert.IsFalse(p1.Equals((Object)p2));
            Assert.IsFalse(p1.Equals(p2));
            Assert.IsFalse(p1 == p2);
            Assert.IsTrue(p1 != p2);
            Assert.IsTrue(p1.CompareTo((Object)p2) > 0);
            Assert.IsTrue(p1.CompareTo(p2) > 0);
            Assert.IsFalse(p1 < p2);
            Assert.IsFalse(p1 <= p2);
            Assert.IsTrue(p1 > p2);
            Assert.IsTrue(p1 >= p2);
         }
         // path-string comparison (valid-empty)
         {
            var p1 = new Path("test", "testa");
            var p2 = String.Empty;
            Assert.IsFalse(p1.Equals((Object)p2));
            Assert.IsFalse(p1.Equals(p2));
            Assert.IsFalse(p1 == p2);
            Assert.IsTrue(p1 != p2);
            Assert.IsTrue(p1.CompareTo((Object)p2) > 0);
            Assert.IsTrue(p1.CompareTo(p2) > 0);
            Assert.IsFalse(p1 < p2);
            Assert.IsFalse(p1 <= p2);
            Assert.IsTrue(p1 > p2);
            Assert.IsTrue(p1 >= p2);
         }
         // path-string comparison (valid-null)
         {
            var p1 = new Path("test", "testa");
            var p2 = (String)null;
            Assert.IsFalse(p1.Equals((Object)p2));
            Assert.IsFalse(p1.Equals(p2));
            Assert.IsFalse(p1 == p2);
            Assert.IsTrue(p1 != p2);
            Assert.IsTrue(p1.CompareTo((Object)p2) > 0);
            Assert.IsTrue(p1.CompareTo(p2) > 0);
            Assert.IsFalse(p1 < p2);
            Assert.IsFalse(p1 <= p2);
            Assert.IsTrue(p1 > p2);
            Assert.IsTrue(p1 >= p2);
         }
         // string-path comparison (null-empty)
         {
            var p1 = (String)null;
            var p2 = Path.Empty;
            Assert.IsTrue(p1 == p2);
            Assert.IsFalse(p1 != p2);
            Assert.IsFalse(p1 < p2);
            Assert.IsTrue(p1 <= p2);
            Assert.IsFalse(p1 > p2);
            Assert.IsTrue(p1 >= p2);
         }
         // string-path comparison (empty-empty)
         {
            var p1 = String.Empty;
            var p2 = Path.Empty;
            Assert.IsTrue(p1 == p2);
            Assert.IsFalse(p1 != p2);
            Assert.IsFalse(p1 < p2);
            Assert.IsTrue(p1 <= p2);
            Assert.IsFalse(p1 > p2);
            Assert.IsTrue(p1 >= p2);
         }
         // string-path comparison (space-empty)
         {
            var p1 = " ";
            var p2 = Path.Empty;
            Assert.IsTrue(p1 == p2);
            Assert.IsFalse(p1 != p2);
            Assert.IsFalse(p1 < p2);
            Assert.IsTrue(p1 <= p2);
            Assert.IsFalse(p1 > p2);
            Assert.IsTrue(p1 >= p2);
         }
         // string-path comparison (null-valid)
         {
            var p1 = (String)null;
            var p2 = new Path("test");
            Assert.IsFalse(p1 == p2);
            Assert.IsTrue(p1 != p2);
            Assert.IsTrue(p1 < p2);
            Assert.IsTrue(p1 <= p2);
            Assert.IsFalse(p1 > p2);
            Assert.IsFalse(p1 >= p2);
         }
         // string-path comparison (empty-valid)
         {
            var p1 = String.Empty;
            var p2 = new Path("test");
            Assert.IsFalse(p1 == p2);
            Assert.IsTrue(p1 != p2);
            Assert.IsTrue(p1 < p2);
            Assert.IsTrue(p1 <= p2);
            Assert.IsFalse(p1 > p2);
            Assert.IsFalse(p1 >= p2);
         }
         // string-path comparison (space-valid)
         {
            var p1 = " ";
            var p2 = new Path("test");
            Assert.IsFalse(p1 == p2);
            Assert.IsTrue(p1 != p2);
            Assert.IsTrue(p1 < p2);
            Assert.IsTrue(p1 <= p2);
            Assert.IsFalse(p1 > p2);
            Assert.IsFalse(p1 >= p2);
         }
         // string-path comparison (valid-valid)
         {
            var p1 = SysPath.GetFullPath(SysPath.Combine("test", "testa"));
            var p2 = new Path("test", "testa");
            var p3 = new Path("test", "testb");
            Assert.IsTrue(p1 == p2);
            Assert.IsFalse(p1 != p2);
            Assert.IsFalse(p1 == p3);
            Assert.IsTrue(p1 != p3);
            Assert.IsFalse(p1 < p2);
            Assert.IsTrue(p1 < p3);
            Assert.IsTrue(p1 <= p2);
            Assert.IsTrue(p1 <= p3);
            Assert.IsFalse(p1 > p2);
            Assert.IsFalse(p1 > p3);
            Assert.IsTrue(p1 >= p2);
            Assert.IsFalse(p1 >= p3);
         }
         // string-path comparison (case insensitivity)
         {
            var p1 = SysPath.GetFullPath(SysPath.Combine("TEST", "TESTA"));
            var p2 = new Path("test", "testa");
            var p3 = new Path("TeSt", "TeStB");
            Assert.IsTrue(p1 == p2);
            Assert.IsFalse(p1 != p2);
            Assert.IsFalse(p1 == p3);
            Assert.IsTrue(p1 != p3);
            Assert.IsFalse(p1 < p2);
            Assert.IsTrue(p1 < p3);
            Assert.IsTrue(p1 <= p2);
            Assert.IsTrue(p1 <= p3);
            Assert.IsFalse(p1 > p2);
            Assert.IsFalse(p1 > p3);
            Assert.IsTrue(p1 >= p2);
            Assert.IsFalse(p1 >= p3);
         }
         // string-path comparison (valid-empty)
         {
            var p1 = SysPath.GetFullPath(SysPath.Combine("test", "testa"));
            var p2 = Path.Empty;
            Assert.IsFalse(p1 == p2);
            Assert.IsTrue(p1 != p2);
            Assert.IsFalse(p1 < p2);
            Assert.IsFalse(p1 <= p2);
            Assert.IsTrue(p1 > p2);
            Assert.IsTrue(p1 >= p2);
         }
         // hash code calculation
         Assert.AreEqual(Path.Empty.GetHashCode(), Path.Empty.GetHashCode());
         Assert.AreNotEqual(Path.Empty.GetHashCode(), 0);
         Assert.AreNotEqual(Path.Empty.GetHashCode(), new Path("test").GetHashCode());
         Assert.AreEqual(new Path("test").GetHashCode(), new Path("test").GetHashCode());
         Assert.AreEqual(new Path("TEST").GetHashCode(), new Path("test").GetHashCode());
         Assert.AreEqual(new Path("testa", "testb").GetHashCode(), new Path("testa", "testb").GetHashCode());
         Assert.AreEqual(new Path("TESTA", "TESTB").GetHashCode(), new Path("testa", "testb").GetHashCode());
         Assert.AreNotEqual(new Path("testa").GetHashCode(), new Path("testb").GetHashCode());
         Assert.AreNotEqual(new Path("testa", "testb").GetHashCode(), new Path("testa").GetHashCode());
      }

      [TestMethod]
      public void TestConversion ()
      {
         // path-string conversion
         {
            var p = Path.Empty;
            var s = p.ToString();
            Assert.IsTrue(s == p);
         }
         {
            var p = Path.Empty;
            String s = p;
            Assert.IsTrue(s == p);
         }
         {
            var p = new Path("test", "testa");
            var s = p.ToString();
            Assert.IsTrue(s == p);
         }
         {
            var p = new Path("test", "testa");
            String s = p;
            Assert.IsTrue(s == p);
         }
         // string-path conversion
         {
            var s = (String)null;
            var p = (Path)s;
            Assert.IsTrue(p == s);
         }
         {
            var s = String.Empty;
            var p = (Path)s;
            Assert.IsTrue(p == s);
         }
         {
            var s = " ";
            var p = (Path)s;
            Assert.IsTrue(p == s);
         }
         {
            var s = SysPath.GetFullPath(SysPath.Combine("test", "testa"));
            var p = (Path)s;
            Assert.IsTrue(p == s);
         }
      }

      [TestMethod]
      public void TestProperties ()
      {
         // IsEmpty
         Assert.IsTrue(default(Path).IsEmpty);
         Assert.IsTrue(Path.Empty.IsEmpty);
         Assert.IsTrue(new Path().IsEmpty);
         Assert.IsFalse(new Path("test").IsEmpty);
         Assert.IsFalse(new Path("testa", "testb").IsEmpty);
         // Top
         Assert.AreEqual(default(Path).Top, String.Empty);
         Assert.AreEqual(Path.Empty.Top, String.Empty);
         Assert.AreEqual(new Path().Top, String.Empty);
         Assert.AreEqual(new Path("test").Top, "test");
         Assert.AreEqual(new Path("testa", "testb").Top, "testb");
         Assert.AreEqual(new Path("testa", "testb", "testc.exe").Top, "testc.exe");
         // FileName
         Assert.AreEqual(default(Path).FileName, String.Empty);
         Assert.AreEqual(Path.Empty.FileName, String.Empty);
         Assert.AreEqual(new Path().FileName, String.Empty);
         Assert.AreEqual(new Path("test").FileName, "test");
         Assert.AreEqual(new Path("testa", "testb").FileName, "testb");
         Assert.AreEqual(new Path("testa", "testb", "testc.exe").FileName, "testc.exe");
         // BaseFileName
         Assert.AreEqual(default(Path).BaseFileName, String.Empty);
         Assert.AreEqual(Path.Empty.BaseFileName, String.Empty);
         Assert.AreEqual(new Path().BaseFileName, String.Empty);
         Assert.AreEqual(new Path("test").BaseFileName, "test");
         Assert.AreEqual(new Path("test.exe").BaseFileName, "test");
         Assert.AreEqual(new Path("testa", "testb").BaseFileName, "testb");
         Assert.AreEqual(new Path("testa", "testb.txt").BaseFileName, "testb");
         Assert.AreEqual(new Path("testa", "testb", "testc.exe").BaseFileName, "testc");
         // Extension
         Assert.AreEqual(default(Path).Extension, String.Empty);
         Assert.AreEqual(Path.Empty.Extension, String.Empty);
         Assert.AreEqual(new Path().Extension, String.Empty);
         Assert.AreEqual(new Path("test").Extension, String.Empty);
         Assert.AreEqual(new Path("test.exe").Extension, ".exe");
         Assert.AreEqual(new Path("testa", "testb").Extension, String.Empty);
         Assert.AreEqual(new Path("testa", "testb.txt").Extension, ".txt");
         Assert.AreEqual(new Path("testa", "testb", "testc.exe").Extension, ".exe");
         // Parent
         Assert.AreEqual(default(Path).Parent, String.Empty);
         Assert.AreEqual(Path.Empty.Parent, String.Empty);
         Assert.AreEqual(new Path().Parent, String.Empty);
         Assert.AreEqual(new Path("test").Parent, Path.Current);
         Assert.AreEqual(new Path("testa", "testb").Parent, new Path("testa"));
         Assert.AreEqual(new Path("testa", "testb", "testc.exe").Parent, new Path("testa", "testb"));
         // Root
         Assert.AreEqual(default(Path).Root, String.Empty);
         Assert.AreEqual(Path.Empty.Root, String.Empty);
         Assert.AreEqual(new Path().Root, String.Empty);
         Assert.AreEqual(new Path("test").Root, Path.Current.Root);
         Assert.AreEqual(new Path("testa", "testb").Root, new Path("testa").Root);
         Assert.AreEqual(new Path("testa", "testb", "testc.exe").Root, new Path("testa", "testb").Root);
         // Current
         Assert.IsFalse(Path.Current.IsEmpty);
         Assert.AreEqual(new Path(Path.Current, "test"), new Path("test"));
         AssertException(() => Path.Current = Path.Empty);
         var prev = Path.Current;
         Path.Current = prev.Parent;
         try
         {
            Assert.AreNotEqual(Path.Current, prev);
            Assert.AreEqual(Path.Current, prev.Parent);
         }
         finally { Path.Current = prev; }
         Assert.AreEqual(Path.Current, prev);
         // Temp
         Assert.IsFalse(Path.Temp.IsEmpty);
         Assert.AreEqual(Path.Temp, Path.Temp);
      }

      [TestMethod]
      public void TestOperations ()
      {
         // path split
         {
            var baseSplit = Path.Current.Split();
            Assert.IsTrue(baseSplit.Length > 0);
            AssertEqual(Path.Empty.Split(), new String[0]);
            AssertEqual(Path.Current.Split(), baseSplit);
            AssertEqual(new Path("test").Split(), baseSplit.Concat(new[] { "test" }));
            AssertEqual(new Path("testa", "testb").Split(), baseSplit.Concat(new[] { "testa", "testb" }));
         }
         // path push
         {
            Assert.AreEqual(Path.Empty.Push(null), Path.Empty);
            Assert.AreEqual(Path.Empty.Push(""), Path.Empty);
            Assert.AreEqual(Path.Empty.Push(" "), Path.Empty);
            Assert.AreEqual(Path.Empty.Push(Path.Current.Root), Path.Current.Root);
            Assert.AreEqual(Path.Current.Push("test"), new Path("test"));
            Assert.AreEqual(new Path("testa").Push("testb"), new Path("testa", "testb"));
            Assert.AreEqual(Path.Empty + null, Path.Empty);
            Assert.AreEqual(Path.Empty + "", Path.Empty);
            Assert.AreEqual(Path.Empty + " ", Path.Empty);
            Assert.AreEqual(Path.Current + "test", new Path("test"));
            Assert.AreEqual(new Path("testa") + "testb", new Path("testa", "testb"));
         }
         // path pop
         {
            Assert.AreEqual(Path.Empty.Pop(), Path.Empty);
            Assert.AreEqual(Path.Empty.Push(Path.Current.Root).Pop(), Path.Empty);
            Assert.AreEqual(new Path("test").Pop(), Path.Current);
            Assert.AreEqual(new Path("testa", "testb").Pop(), new Path("testa"));
            var testPath = new Path("test");
            var count = testPath.Count();
            for (var i = 0; i < count; i++)
            {
               Assert.IsFalse(testPath.IsEmpty);
               testPath = testPath.Pop();
            }
            Assert.IsTrue(testPath.IsEmpty);
         }
         // parent/child test
         {
            Assert.IsFalse(Path.Empty.IsParent(Path.Empty));
            Assert.IsFalse(Path.Empty.IsParent(new Path("test")));
            Assert.IsFalse(Path.Empty.IsParent(new Path("test").Root));
            Assert.IsFalse(new Path("test").IsParent(Path.Empty));
            Assert.IsFalse(new Path("test").Root.IsParent(Path.Empty));
            Assert.IsFalse(new Path("testa").IsParent(new Path("testb")));
            Assert.IsFalse(new Path("testa", "testb", "testc").IsParent(new Path("testa")));
            Assert.IsFalse(new Path("test").IsParent(Path.Current));
            Assert.IsTrue(Path.Current.IsParent(new Path("test")));
            Assert.IsTrue(new Path("testa").IsParent(new Path("testa", "testb")));
            Assert.IsFalse(Path.Empty.IsChild(Path.Empty));
            Assert.IsFalse(Path.Empty.IsChild(new Path("test")));
            Assert.IsFalse(Path.Empty.IsChild(new Path("test").Root));
            Assert.IsFalse(new Path("test").IsChild(Path.Empty));
            Assert.IsFalse(new Path("test").Root.IsChild(Path.Empty));
            Assert.IsFalse(new Path("testa").IsChild(new Path("testb")));
            Assert.IsFalse(new Path("testa").IsChild(new Path("testa", "testb", "testc")));
            Assert.IsFalse(Path.Current.IsChild(new Path("test")));
            Assert.IsTrue(new Path("test").IsChild(Path.Current));
            Assert.IsTrue(new Path("testa", "testb").IsChild(new Path("testa")));
         }
         // ancestor/descendant test
         {
            Assert.IsFalse(Path.Empty.IsAncestor(Path.Empty));
            Assert.IsFalse(Path.Empty.IsAncestor(new Path("test")));
            Assert.IsFalse(Path.Empty.IsAncestor(new Path("test").Root));
            Assert.IsFalse(new Path("test").IsAncestor(Path.Empty));
            Assert.IsFalse(new Path("test").Root.IsAncestor(Path.Empty));
            Assert.IsFalse(new Path("test").IsAncestor(Path.Current));
            Assert.IsTrue(Path.Current.IsAncestor(new Path("test")));
            Assert.IsTrue(Path.Current.Root.IsAncestor(new Path("test")));
            Assert.IsFalse(new Path("testa").IsAncestor(new Path("testb")));
            Assert.IsTrue(new Path("testa").IsAncestor(new Path("testa", "testb")));
            Assert.IsTrue(new Path("testa").IsAncestor(new Path("testa", "testb", "testc")));
            Assert.IsFalse(Path.Empty.IsDescendant(Path.Empty));
            Assert.IsFalse(Path.Empty.IsDescendant(new Path("test")));
            Assert.IsFalse(Path.Empty.IsDescendant(new Path("test").Root));
            Assert.IsFalse(new Path("test").IsDescendant(Path.Empty));
            Assert.IsFalse(new Path("test").Root.IsDescendant(Path.Empty));
            Assert.IsFalse(new Path("testa").IsDescendant(new Path("testb")));
            Assert.IsFalse(Path.Current.IsDescendant(new Path("test")));
            Assert.IsFalse(Path.Current.Root.IsDescendant(new Path("test")));
            Assert.IsTrue(new Path("test").IsDescendant(Path.Current));
            Assert.IsTrue(new Path("testa", "testb").IsDescendant(new Path("testa")));
            Assert.IsTrue(new Path("testa", "testb", "testc").IsDescendant(new Path("testa")));
         }
         // path enumeration
         {
            AssertEqual(Path.Empty.Split(), Path.Empty);
            AssertEqual(new Path("test").Split(), new Path("test"));
            AssertEqual(new Path("testa", "testb").Split(), new Path("testa", "testb"));
            var testPath = new Path("test").Split();
            var pathIdx = 0;
            foreach (Object e in (IEnumerable)new Path("test"))
               Assert.AreEqual(e, testPath[pathIdx++]);
         }
      }

      private void AssertEqual<T> (IEnumerable<T> e1, IEnumerable<T> e2)
      {
         Assert.IsTrue(Enumerable.SequenceEqual(e1, e2));
      }
      private void AssertException (Action a)
      {
         try { a(); }
         catch { return; }
         Assert.Fail("Expected: exception");
      }
   }
}
