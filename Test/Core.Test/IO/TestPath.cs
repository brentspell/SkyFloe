using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SysPath = System.IO.Path;

using SkyFloe.IO;

namespace Core.Test.IO
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
            Path p1 = Path.Empty;
            Path p2 = Path.Empty;
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
            Path p1 = Path.Empty;
            Path p2 = new Path("test");
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
            Path p1 = new Path("test", "testa");
            Path p2 = new Path("test", "testa");
            Path p3 = new Path("test", "testb");
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
            Path p1 = new Path("TEST", "TESTA");
            Path p2 = new Path("test", "testa");
            Path p3 = new Path("TeSt", "TeStB");
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
            Path p1 = new Path("test", "testa");
            Path p2 = Path.Empty;
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
            Path p1 = Path.Empty;
            String p2 = null;
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
            Path p1 = Path.Empty;
            String p2 = String.Empty;
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
            Path p1 = Path.Empty;
            String p2 = " ";
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
            Path p1 = Path.Empty;
            String p2 = SysPath.GetFullPath("test");
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
            Path p1 = new Path("test", "testa");
            String p2 = SysPath.GetFullPath(SysPath.Combine("test", "testa"));
            String p3 = SysPath.GetFullPath(SysPath.Combine("test", "testb"));
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
            Path p1 = new Path("TEST", "TESTA");
            String p2 = SysPath.GetFullPath(SysPath.Combine("test", "testa"));
            String p3 = SysPath.GetFullPath(SysPath.Combine("TeSt", "TeStB"));
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
            Path p1 = new Path("test", "testa");
            String p2 = " ";
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
            Path p1 = new Path("test", "testa");
            String p2 = String.Empty;
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
            Path p1 = new Path("test", "testa");
            String p2 = null;
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
            String p1 = null;
            Path p2 = Path.Empty;
            Assert.IsTrue(p1 == p2);
            Assert.IsFalse(p1 != p2);
            Assert.IsFalse(p1 < p2);
            Assert.IsTrue(p1 <= p2);
            Assert.IsFalse(p1 > p2);
            Assert.IsTrue(p1 >= p2);
         }
         // string-path comparison (empty-empty)
         {
            String p1 = String.Empty;
            Path p2 = Path.Empty;
            Assert.IsTrue(p1 == p2);
            Assert.IsFalse(p1 != p2);
            Assert.IsFalse(p1 < p2);
            Assert.IsTrue(p1 <= p2);
            Assert.IsFalse(p1 > p2);
            Assert.IsTrue(p1 >= p2);
         }
         // string-path comparison (space-empty)
         {
            String p1 = " ";
            Path p2 = Path.Empty;
            Assert.IsTrue(p1 == p2);
            Assert.IsFalse(p1 != p2);
            Assert.IsFalse(p1 < p2);
            Assert.IsTrue(p1 <= p2);
            Assert.IsFalse(p1 > p2);
            Assert.IsTrue(p1 >= p2);
         }
         // string-path comparison (null-valid)
         {
            String p1 = null;
            Path p2 = new Path("test");
            Assert.IsFalse(p1 == p2);
            Assert.IsTrue(p1 != p2);
            Assert.IsTrue(p1 < p2);
            Assert.IsTrue(p1 <= p2);
            Assert.IsFalse(p1 > p2);
            Assert.IsFalse(p1 >= p2);
         }
         // string-path comparison (empty-valid)
         {
            String p1 = String.Empty;
            Path p2 = new Path("test");
            Assert.IsFalse(p1 == p2);
            Assert.IsTrue(p1 != p2);
            Assert.IsTrue(p1 < p2);
            Assert.IsTrue(p1 <= p2);
            Assert.IsFalse(p1 > p2);
            Assert.IsFalse(p1 >= p2);
         }
         // string-path comparison (space-valid)
         {
            String p1 = " ";
            Path p2 = new Path("test");
            Assert.IsFalse(p1 == p2);
            Assert.IsTrue(p1 != p2);
            Assert.IsTrue(p1 < p2);
            Assert.IsTrue(p1 <= p2);
            Assert.IsFalse(p1 > p2);
            Assert.IsFalse(p1 >= p2);
         }
         // string-path comparison (valid-valid)
         {
            String p1 = SysPath.GetFullPath(SysPath.Combine("test", "testa"));
            Path p2 = new Path("test", "testa");
            Path p3 = new Path("test", "testb");
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
            String p1 = SysPath.GetFullPath(SysPath.Combine("TEST", "TESTA"));
            Path p2 = new Path("test", "testa");
            Path p3 = new Path("TeSt", "TeStB");
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
            String p1 = SysPath.GetFullPath(SysPath.Combine("test", "testa"));
            Path p2 = Path.Empty;
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
            Path p = Path.Empty;
            String s = p.ToString();
            Assert.IsTrue(s == p);
         }
         {
            Path p = Path.Empty;
            String s = p;
            Assert.IsTrue(s == p);
         }
         {
            Path p = new Path("test", "testa");
            String s = p.ToString();
            Assert.IsTrue(s == p);
         }
         {
            Path p = new Path("test", "testa");
            String s = p;
            Assert.IsTrue(s == p);
         }
         // string-path conversion
         {
            String s = null;
            Path p = s;
            Assert.IsTrue(p == s);
         }
         {
            String s = String.Empty;
            Path p = s;
            Assert.IsTrue(p == s);
         }
         {
            String s = " ";
            Path p = s;
            Assert.IsTrue(p == s);
         }
         {
            String s = SysPath.GetFullPath(SysPath.Combine("test", "testa"));
            Path p = s;
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
         Path prev = Path.Current;
         Path.Current = prev.Parent;
         try
         {
            Assert.AreNotEqual(Path.Current, prev);
            Assert.AreEqual(Path.Current, prev.Parent);
         }
         finally { Path.Current = prev; }
         Assert.AreEqual(Path.Current, prev);
      }

      [TestMethod]
      public void TestOperations ()
      {
         // TODO: test enumeration
         // TODO: test + operator
      }

      private void AssertException (Action a)
      {
         try
         { 
            a();
            Assert.Fail("Expected: exception");
         }
         catch { }
      }
   }
}
