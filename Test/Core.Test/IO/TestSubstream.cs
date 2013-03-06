using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SkyFloe.IO;

namespace SkyFloe.Core.Test.IO
{
   [TestClass]
   public class TestSubstream
   {
      [TestMethod]
      public void TestConstruction ()
      {
         // invalid construction
         AssertException(() => new Substream(null, 0, 0));
         AssertException(() => new Substream(CreateStream(""), -1, 0));
         AssertException(() => new Substream(CreateStream(""), 0, -1));
         AssertException(() => new Substream(CreateStream(""), 1, 0));
         AssertException(() => new Substream(CreateStream(""), 0, 1));
         AssertException(() => new Substream(CreateStream("A"), 2, 0));
         AssertException(() => new Substream(CreateStream("A"), 0, 2));
         AssertException(() => new Substream(new FilterStream(CreateStream("")), 0, 0));
         // valid construction
         new Substream(CreateStream(""), 0, 0);
         new Substream(CreateStream("A"), 0, 0);
         new Substream(CreateStream("A"), 1, 0);
         new Substream(CreateStream("A"), 0, 1);
         new Substream(CreateStream("ABC"), 0, 0);
         new Substream(CreateStream("ABC"), 3, 0);
         new Substream(CreateStream("ABC"), 2, 0);
         new Substream(CreateStream("ABC"), 2, 1);
         new Substream(CreateStream("ABC"), 1, 0);
         new Substream(CreateStream("ABC"), 1, 1);
         new Substream(CreateStream("ABC"), 1, 2);
         new Substream(CreateStream("ABC"), 0, 0);
         new Substream(CreateStream("ABC"), 0, 1);
         new Substream(CreateStream("ABC"), 0, 2);
         new Substream(CreateStream("ABC"), 0, 3);
      }

      [TestMethod]
      public void TestProperties ()
      {
         // stream flags
         Assert.IsTrue(new Substream(CreateStream(""), 0, 0).CanSeek);
         Assert.IsTrue(new Substream(CreateStream(""), 0, 0).CanRead);
         Assert.IsFalse(new Substream(CreateStream(""), 0, 0).CanWrite);
         // stream position
         Assert.AreEqual(new Substream(CreateStream(""), 0, 0).Position, 0);
         Assert.AreEqual(new Substream(CreateStream("A"), 0, 1).Position, 0);
         Assert.AreEqual(new Substream(CreateStream("A"), 1, 0).Position, 0);
         Assert.AreEqual(new Substream(CreateStream("ABC"), 0, 3).Position, 0);
         Assert.AreEqual(new Substream(CreateStream("ABC"), 1, 2).Position, 0);
         Assert.AreEqual(new Substream(CreateStream("ABC"), 2, 1).Position, 0);
         Assert.AreEqual(new Substream(CreateStream("ABC"), 3, 0).Position, 0);
         using (var stream = CreateStream("ABCDE"))
         using (var sub = new Substream(stream, 1, 3))
         {
            for (var i = 0; i < 7; i++)
            {
               sub.Position = i;
               Assert.AreEqual(sub.Position, i);
               Assert.AreEqual(stream.Position, i + 1);
            }
         }
         // stream length
         AssertException(() => new Substream(CreateStream("ABC"), 0, 3).SetLength(2));
         Assert.AreEqual(new Substream(CreateStream(""), 0, 0).Length, 0);
         Assert.AreEqual(new Substream(CreateStream("A"), 0, 1).Length, 1);
         Assert.AreEqual(new Substream(CreateStream("A"), 1, 0).Length, 0);
         Assert.AreEqual(new Substream(CreateStream("ABC"), 0, 3).Length, 3);
         Assert.AreEqual(new Substream(CreateStream("ABC"), 0, 2).Length, 2);
         Assert.AreEqual(new Substream(CreateStream("ABC"), 0, 1).Length, 1);
         Assert.AreEqual(new Substream(CreateStream("ABC"), 1, 2).Length, 2);
         Assert.AreEqual(new Substream(CreateStream("ABC"), 1, 1).Length, 1);
         Assert.AreEqual(new Substream(CreateStream("ABC"), 2, 1).Length, 1);
         Assert.AreEqual(new Substream(CreateStream("ABC"), 3, 0).Length, 0);
      }

      [TestMethod]
      public void TestOperations ()
      {
         var stream = CreateStream("ABCDE");
         var sub = new Substream(stream, 1, 3);
         // invalid operations
         AssertException(() => sub.Seek(0, (SeekOrigin)(Int32)42));
         AssertException(() => sub.Write(new Byte[10], 0, 10));
         // seek
         sub.Position = 0;
         for (var i = 0; i < sub.Length; i++)
         {
            sub.Seek(i, SeekOrigin.Begin);
            Assert.AreEqual(stream.Position, 1 + i);
         }
         sub.Position = 0;
         for (var i = 0; i < sub.Length; i++)
         {
            sub.Seek(-i, SeekOrigin.End);
            Assert.AreEqual(stream.Position, 4 - i);
         }
         sub.Position = 0;
         for (var i = 0; i < sub.Length; i++)
         {
            sub.Seek(1, SeekOrigin.Current);
            Assert.AreEqual(stream.Position, 2 + i);
         }
         for (var i = 0; i < sub.Length; i++)
         {
            sub.Seek(-1, SeekOrigin.Current);
            Assert.AreEqual(stream.Position, 3 - i);
         }
         sub.Position = 0;
         sub.Seek(3, SeekOrigin.Current);
         Assert.AreEqual(stream.Position, 4);
         sub.Seek(-3, SeekOrigin.Current);
         Assert.AreEqual(stream.Position, 1);
         // read
         sub.Position = 0;
         Assert.AreEqual(ReadChar(sub), 'B');
         Assert.AreEqual(sub.Position, 1);
         Assert.AreEqual(ReadChar(sub), 'C');
         Assert.AreEqual(sub.Position, 2);
         Assert.AreEqual(ReadChar(sub), 'D');
         Assert.AreEqual(sub.Position, 3);
         Assert.AreEqual(ReadChar(sub), 0);
         sub.Position = 0;
         Assert.AreEqual(ReadString(sub), "BCD");
         Assert.AreEqual(ReadString(sub), "");
         sub.Position = 1;
         Assert.AreEqual(ReadString(sub), "CD");
         Assert.AreEqual(ReadString(sub), "");
         sub.Position = 2;
         Assert.AreEqual(ReadString(sub), "D");
         Assert.AreEqual(ReadString(sub), "");
         sub.Position = 3;
         Assert.AreEqual(ReadString(sub), "");
      }

      private Stream CreateStream (String data)
      {
         return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(data));
      }
      private Char ReadChar (Stream stream)
      {
         Byte[] buffer = new Byte[1];
         if (stream.Read(buffer, 0, 1) != 1)
            return (Char)0;
         return System.Text.Encoding.UTF8.GetChars(buffer)[0];
      }
      private String ReadString (Stream stream)
      {
         return new StreamReader(stream).ReadToEnd();
      }
      private void AssertException (Action a)
      {
         try { a(); }
         catch { return; }
         Assert.Fail("Expected: exception");
      }
   }
}
