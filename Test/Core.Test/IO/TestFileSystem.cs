//===========================================================================
// MODULE:  TestFileSystem.cs
// PURPOSE: file system facade unit test
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
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Stream = System.IO.Stream;
// Project References
using SkyFloe.IO;

namespace SkyFloe.Core.Test.IO
{
   [TestClass]
   public class TestFileSystem
   {
      private static readonly Path BasePath = Path.Temp + "SkyFloe-TestFileSystem";

      [TestInitialize]
      public void Initialize ()
      {
         Cleanup();
         FileSystem.CreateDirectory(BasePath);
      }

      [TestCleanup]
      public void Cleanup ()
      {
         if (FileSystem.Exists(BasePath))
            FileSystem.SetReadOnly(BasePath, false);
         FileSystem.Delete(BasePath);
      }

      [TestMethod]
      public void TestDirectory ()
      {
         // invalid directory
         AssertException(() => FileSystem.CreateDirectory(Path.Empty));
         AssertException(() => FileSystem.Delete(Path.Empty));
         // new directory
         FileSystem.CreateDirectory(BasePath + "test1");
         Assert.IsTrue(FileSystem.Exists(BasePath + "test1"));
         FileSystem.CreateDirectory(BasePath + "test2" + "test3");
         Assert.IsTrue(FileSystem.Exists(BasePath + "test2"));
         Assert.IsTrue(FileSystem.Exists(BasePath + "test2" + "test3"));
         // existing directory
         FileSystem.CreateDirectory(BasePath + "test1");
         Assert.IsTrue(FileSystem.Exists(BasePath + "test1"));
         FileSystem.CreateDirectory(BasePath + "test2");
         Assert.IsTrue(FileSystem.Exists(BasePath + "test2"));
         Assert.IsTrue(FileSystem.Exists(BasePath + "test2" + "test3"));
         FileSystem.CreateDirectory(BasePath + "test2" + "test3");
         Assert.IsTrue(FileSystem.Exists(BasePath + "test2"));
         Assert.IsTrue(FileSystem.Exists(BasePath + "test2" + "test3"));
         // delete empty directory
         FileSystem.Delete(BasePath + "test1");
         Assert.IsFalse(FileSystem.Exists(BasePath + "test1"));
         FileSystem.Delete(BasePath + "test1");
         Assert.IsFalse(FileSystem.Exists(BasePath + "test1"));
         FileSystem.Delete(BasePath + "test2" + "test3");
         Assert.IsTrue(FileSystem.Exists(BasePath + "test2"));
         Assert.IsFalse(FileSystem.Exists(BasePath + "test2" + "test3"));
         FileSystem.Delete(BasePath + "test2");
         Assert.IsFalse(FileSystem.Exists(BasePath + "test2"));
         Assert.IsFalse(FileSystem.Exists(BasePath + "test2" + "test3"));
         FileSystem.Delete(BasePath + "test2" + "test3");
         Assert.IsFalse(FileSystem.Exists(BasePath + "test2"));
         Assert.IsFalse(FileSystem.Exists(BasePath + "test2" + "test3"));
         FileSystem.Delete(BasePath + "test2");
         Assert.IsFalse(FileSystem.Exists(BasePath + "test2"));
         Assert.IsFalse(FileSystem.Exists(BasePath + "test2" + "test3"));
         // delete non-empty directory
         FileSystem.CreateDirectory(BasePath + "test1");
         FileSystem.Create(BasePath + "test1" + "test.txt").Dispose();
         FileSystem.Delete(BasePath + "test1");
         Assert.IsFalse(FileSystem.Exists(BasePath + "test1"));
         Assert.IsFalse(FileSystem.Exists(BasePath + "test1" + "test.txt"));
         FileSystem.CreateDirectory(BasePath + "test2" + "test3");
         Assert.IsTrue(FileSystem.Exists(BasePath + "test2" + "test3"));
         FileSystem.Delete(BasePath + "test2");
         Assert.IsFalse(FileSystem.Exists(BasePath + "test2"));
         Assert.IsFalse(FileSystem.Exists(BasePath + "test2" + "test3"));
      }

      [TestMethod]
      public void TestFile ()
      {
         var testDir = BasePath + "test";
         var testFile = testDir + "test.txt";
         // create
         AssertException(() => FileSystem.Create(Path.Empty));
         using (var stream = FileSystem.Create(testFile))
         {
            Assert.IsTrue(stream.CanSeek);
            Assert.IsTrue(stream.CanRead);
            Assert.IsTrue(stream.CanWrite);
            Assert.AreEqual(stream.Position, 0);
            stream.Write(new Byte[] { 42 }, 0, 1);
            stream.Position = 0;
            var buffer = new Byte[1];
            Assert.AreEqual(stream.Read(buffer, 0, 1), 1);
            Assert.AreEqual(buffer[0], 42);
         }
         AssertException(() => FileSystem.Create(testFile));
         // open
         AssertException(() => FileSystem.Open(Path.Empty));
         AssertException(() => FileSystem.Open(BasePath + "missing.txt"));
         using (var stream1 = FileSystem.Open(testFile))
         {
            AssertException(() => FileSystem.Open(testFile, System.IO.FileShare.None));
            Assert.IsTrue(stream1.CanSeek);
            Assert.IsTrue(stream1.CanRead);
            Assert.IsFalse(stream1.CanWrite);
            Assert.AreEqual(stream1.Position, 0);
            var buffer = new Byte[1];
            Assert.AreEqual(stream1.Read(buffer, 0, 1), 1);
            Assert.AreEqual(buffer[0], 42);
            using (var stream2 = FileSystem.Open(testFile))
            {
               Assert.IsTrue(stream2.CanSeek);
               Assert.IsTrue(stream2.CanRead);
               Assert.IsFalse(stream2.CanWrite);
               Assert.AreEqual(stream2.Position, 0);
               buffer[0] = 0;
               Assert.AreEqual(stream2.Read(buffer, 0, 1), 1);
               Assert.AreEqual(buffer[0], 42);
            }
         }
         // truncate
         AssertException(() => FileSystem.Truncate(Path.Empty));
         using (var stream = FileSystem.Truncate(testFile))
         {
            Assert.IsTrue(stream.CanSeek);
            Assert.IsTrue(stream.CanRead);
            Assert.IsTrue(stream.CanWrite);
            Assert.AreEqual(stream.Position, 0);
            Assert.AreEqual(stream.Length, 0);
         }
         FileSystem.Delete(testDir);
         using (var stream = FileSystem.Truncate(testFile))
         {
            Assert.IsTrue(stream.CanSeek);
            Assert.IsTrue(stream.CanRead);
            Assert.IsTrue(stream.CanWrite);
            Assert.AreEqual(stream.Position, 0);
            Assert.AreEqual(stream.Length, 0);
            stream.Write(new Byte[] { 42 }, 0, 1);
            stream.Position = 0;
            var buffer = new Byte[1];
            Assert.AreEqual(stream.Read(buffer, 0, 1), 1);
            Assert.AreEqual(buffer[0], 42);
         }
         // append
         AssertException(() => FileSystem.Append(Path.Empty));
         using (var stream = FileSystem.Append(testFile))
         {
            Assert.IsTrue(stream.CanSeek);
            Assert.IsTrue(stream.CanRead);
            Assert.IsTrue(stream.CanWrite);
            Assert.AreEqual(stream.Position, 1);
            Assert.AreEqual(stream.Length, 1);
            stream.Write(new Byte[] { 42 }, 0, 1);
            stream.Position = 0;
            var buffer = new Byte[1];
            Assert.AreEqual(stream.Read(buffer, 0, 1), 1);
            Assert.AreEqual(buffer[0], 42);
            Assert.AreEqual(stream.Read(buffer, 0, 1), 1);
            Assert.AreEqual(buffer[0], 42);
         }
         FileSystem.Delete(testDir);
         using (var stream = FileSystem.Append(testFile))
         {
            Assert.IsTrue(stream.CanSeek);
            Assert.IsTrue(stream.CanRead);
            Assert.IsTrue(stream.CanWrite);
            Assert.AreEqual(stream.Position, 0);
            Assert.AreEqual(stream.Length, 0);
            stream.Write(new Byte[] { 42 }, 0, 1);
            stream.Position = 0;
            var buffer = new Byte[1];
            Assert.AreEqual(stream.Read(buffer, 0, 1), 1);
            Assert.AreEqual(buffer[0], 42);
         }
         // delete
         AssertException(() => FileSystem.Delete(Path.Empty));
         FileSystem.Delete(testFile);
         Assert.IsTrue(FileSystem.Exists(testDir));
         Assert.IsFalse(FileSystem.Exists(testFile));
         FileSystem.Delete(testFile);
         Assert.IsTrue(FileSystem.Exists(testDir));
         Assert.IsFalse(FileSystem.Exists(testFile));
         // copy
         var copyDir1 = BasePath + "test1";
         var copyDir2 = BasePath + "test2";
         var copyFile1 = copyDir1 + "test1.txt";
         var copyFile2 = copyDir2 + "test2.txt";
         AssertException(() => FileSystem.Copy(Path.Empty, Path.Empty));
         AssertException(() => FileSystem.Copy(copyFile1, Path.Empty));
         AssertException(() => FileSystem.Copy(Path.Empty, copyFile2));
         AssertException(() => FileSystem.Copy(copyFile1, copyFile2));
         FileSystem.CreateDirectory(copyDir1);
         AssertException(() => FileSystem.Copy(copyDir1, copyDir2));
         using (var stream = FileSystem.Create(copyFile1))
            stream.Write(new Byte[] { 42 }, 0, 1);
         FileSystem.Copy(copyFile1, copyFile2);
         using (var stream = FileSystem.Open(copyFile2))
         {
            var buffer = new Byte[1];
            Assert.AreEqual(stream.Read(buffer, 0, 1), 1);
            Assert.AreEqual(buffer[0], 42);
         }
         FileSystem.Truncate(copyFile1).Dispose();
         FileSystem.Copy(copyFile2, copyFile1);
         using (var stream = FileSystem.Open(copyFile1))
         {
            var buffer = new Byte[1];
            Assert.AreEqual(stream.Read(buffer, 0, 1), 1);
            Assert.AreEqual(buffer[0], 42);
         }
      }

      [TestMethod]
      public void TestTemp ()
      {
         var tempPath = Path.Empty;
         using (var stream1 = FileSystem.Temp())
         {
            tempPath = stream1.Path;
            Assert.IsTrue(stream1.CanSeek);
            Assert.IsTrue(stream1.CanRead);
            Assert.IsTrue(stream1.CanWrite);
            Assert.AreEqual(stream1.Position, 0);
            Assert.AreEqual(stream1.Length, 0);
            Assert.IsFalse(stream1.Path.IsEmpty);
            stream1.Write(new Byte[] { 42 }, 0, 1);
            stream1.Flush();
            using (var stream2 = FileSystem.Open(stream1.Path, System.IO.FileShare.ReadWrite))
            {
               var buffer = new Byte[1];
               Assert.AreEqual(stream2.Read(buffer, 0, 1), 1);
               Assert.AreEqual(buffer[0], 42);
            }
         }
         Assert.IsFalse(FileSystem.Exists(tempPath));
      }

      [TestMethod]
      public void TestMetadata ()
      {
         var meta = FileSystem.Metadata.Empty;
         // invalid metadata
         AssertException(() => FileSystem.GetMetadata(Path.Empty));
         // empty metadata
         Assert.IsTrue(meta.Path.IsEmpty);
         Assert.IsNull(meta.Name);
         Assert.IsFalse(meta.Exists);
         Assert.AreEqual(meta.Length, 0);
         Assert.IsFalse(meta.IsDirectory);
         Assert.IsFalse(meta.IsSystem);
         Assert.IsFalse(meta.IsHidden);
         Assert.IsFalse(meta.IsReadOnly);
         // missing metadata
         meta = FileSystem.GetMetadata(BasePath + "missing.txt");
         Assert.AreEqual(meta.Path, BasePath + "missing.txt");
         Assert.AreEqual(meta.Name, "missing.txt");
         Assert.IsFalse(meta.Exists);
         Assert.AreEqual(meta.Length, 0);
         Assert.IsFalse(meta.IsDirectory);
         Assert.IsFalse(meta.IsSystem);
         Assert.IsFalse(meta.IsHidden);
         Assert.IsFalse(meta.IsReadOnly);
         // directory metadata
         meta = FileSystem.GetMetadata(BasePath);
         Assert.AreEqual(meta.Path, BasePath);
         Assert.AreEqual(meta.Name, BasePath.FileName);
         Assert.IsTrue(meta.Exists);
         Assert.AreEqual(meta.Length, 0);
         Assert.IsTrue(meta.IsDirectory);
         Assert.IsFalse(meta.IsSystem);
         Assert.IsFalse(meta.IsHidden);
         Assert.IsFalse(meta.IsReadOnly);
         // file metadata
         using (var stream = FileSystem.Truncate(BasePath + "test.txt"))
            stream.Write(new Byte[] { 42 }, 0, 1);
         meta = FileSystem.GetMetadata(BasePath + "test.txt");
         Assert.AreEqual(meta.Path, BasePath + "test.txt");
         Assert.AreEqual(meta.Name, "test.txt");
         Assert.IsTrue(meta.Exists);
         Assert.AreEqual(meta.Length, 1);
         Assert.IsFalse(meta.IsDirectory);
         Assert.IsFalse(meta.IsSystem);
         Assert.IsFalse(meta.IsHidden);
         Assert.IsFalse(meta.IsReadOnly);
         // readonly attribute
         AssertException(() => FileSystem.SetReadOnly(Path.Empty, true));
         AssertException(() => FileSystem.SetReadOnly(Path.Empty, false));
         AssertException(() => FileSystem.SetReadOnly(BasePath + "missing.txt", true));
         AssertException(() => FileSystem.SetReadOnly(BasePath + "missing.txt", false));
         var readOnlyDir = BasePath + "test";
         var readOnlyFile = BasePath + "test" + "test.txt";
         FileSystem.Create(readOnlyFile).Dispose();
         FileSystem.SetReadOnly(readOnlyFile);
         Assert.IsTrue(FileSystem.GetMetadata(readOnlyFile).IsReadOnly);
         FileSystem.SetReadOnly(readOnlyFile, true);
         Assert.IsTrue(FileSystem.GetMetadata(readOnlyFile).IsReadOnly);
         FileSystem.SetReadOnly(readOnlyFile, false);
         Assert.IsFalse(FileSystem.GetMetadata(readOnlyFile).IsReadOnly);
         FileSystem.SetReadOnly(readOnlyDir);
         Assert.IsTrue(FileSystem.GetMetadata(readOnlyFile).IsReadOnly);
         FileSystem.SetReadOnly(readOnlyDir, true);
         Assert.IsTrue(FileSystem.GetMetadata(readOnlyFile).IsReadOnly);
         FileSystem.SetReadOnly(readOnlyDir, false);
         Assert.IsFalse(FileSystem.GetMetadata(readOnlyFile).IsReadOnly);
         FileSystem.SetReadOnly(readOnlyDir, false);
         FileSystem.Delete(readOnlyDir);
      }

      [TestMethod]
      public void TestEnumeration ()
      {
         var testDir = BasePath + "test";
         var children = new[]
         {
            testDir + "test1",
            testDir + "test2",
            testDir + "test3",
            testDir + "test.txt"
         };
         var descendants = children.Concat(
            new[]
            {
               testDir + "test1" + "test1.txt",
               testDir + "test2" + "test20.txt",
               testDir + "test2" + "test21.txt",
               testDir + "test3" + "test30",
               testDir + "test3" + "test30" + "test3.txt"
            }
         ).ToArray();
         var fse = (IEnumerable<Path>)null;
         FileSystem.CreateDirectory(testDir + "test1");
         FileSystem.CreateDirectory(testDir + "test2");
         FileSystem.CreateDirectory(testDir + "test3");
         FileSystem.CreateDirectory(testDir + "test3" + "test30");
         foreach (var path in descendants)
            if (!FileSystem.Exists(path))
               FileSystem.Create(path).Dispose();
         // child enumeration
         AssertException(() => FileSystem.Children(Path.Empty));
         foreach (var child in FileSystem.Children(testDir))
         {
            Assert.IsTrue(child.Exists);
            Assert.IsTrue(testDir.IsParent(child.Path));
         }
         fse = FileSystem.Children(testDir).Select(c => c.Path).ToArray();
         Assert.IsTrue(
            Enumerable.SequenceEqual(
               children.OrderBy(c => c),
               fse.OrderBy(c => c)
            )
         );
         // descendant enumeration
         AssertException(() => FileSystem.Descendants(Path.Empty));
         foreach (var child in FileSystem.Descendants(testDir))
         {
            Assert.IsTrue(child.Exists);
            Assert.IsTrue(testDir.IsAncestor(child.Path));
         }
         fse = FileSystem.Descendants(testDir).Select(c => c.Path).ToArray();
         Assert.IsTrue(
            Enumerable.SequenceEqual(
               descendants.OrderBy(c => c),
               fse.OrderBy(c => c)
            )
         );
      }

      private void AssertException (Action a)
      {
         try { a(); }
         catch { return; }
         Assert.Fail("Expected: exception");
      }
   }
}
