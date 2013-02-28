using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SkyFloe.IO;

namespace Core.Test.IO
{
   [TestClass]
   public class TestCompressionStream
   {
      static Random rand = new Random();

      [TestMethod]
      public void TestConstruction ()
      {
         // invalid construction
         AssertException(() => new CompressionStream(null, CompressionMode.Compress));
         AssertException(() => new CompressionStream(Create("test"), (CompressionMode)(Int32)42));
         AssertException(() => new CompressionStream(Create("test"), CompressionMode.Decompress));
         // valid construction
         new CompressionStream(Create("test"), CompressionMode.Compress);
         new CompressionStream(
            new CompressionStream(Create("test"), CompressionMode.Compress), 
            CompressionMode.Decompress
         );
      }

      [TestMethod]
      public void TestProperties ()
      {
         using (var stream = new CompressionStream(Create("test"), CompressionMode.Compress))
         {
            // invalid properties
            Assert.IsFalse(stream.CanSeek);
            Assert.IsFalse(stream.CanWrite);
            AssertException(() => { var x = stream.Position; });
            AssertException(() => stream.Position = 10);
            AssertException(() => { var x = stream.Length; });
            AssertException(() => stream.SetLength(10));
            // valid properties
            Assert.IsTrue(stream.CanRead);
         }
         using (var stream = new CompressionStream(Encode("test"), CompressionMode.Decompress))
         {
            // invalid properties
            Assert.IsFalse(stream.CanSeek);
            Assert.IsFalse(stream.CanWrite);
            AssertException(() => { var x = stream.Position; });
            AssertException(() => stream.Position = 10);
            AssertException(() => { var x = stream.Length; });
            AssertException(() => stream.SetLength(10));
            // valid properties
            Assert.IsTrue(stream.CanRead);
         }
      }

      [TestMethod]
      public void TestOperations ()
      {
         // invalid operations
         using (var stream = new CompressionStream(Create("test"), CompressionMode.Compress))
         {
            AssertException(() => stream.Seek(0, SeekOrigin.Current));
            AssertException(() => stream.Write(new Byte[10], 0, 10));
         }
         using (var stream = new CompressionStream(Encode("test"), CompressionMode.Decompress))
         {
            AssertException(() => stream.Seek(0, SeekOrigin.Current));
            AssertException(() => stream.Write(new Byte[10], 0, 10));
         }
      }

      [TestMethod]
      public void TestCompression ()
      {
         // degenerate streams
         Assert.IsTrue(AreEqual(Create(""), RoundTrip("")));
         Assert.IsTrue(AreEqual(Create("1"), RoundTrip("1")));
         // embarassingly compressible streams
         using (var stream = Create(new String('A', 1048576)))
         using (var encoded = Encode(stream))
         using (var decoded = Decode(encoded))
         {
            Assert.IsTrue(encoded.Length < 65536);
            Assert.IsTrue(AreEqual(stream, decoded));
         }
         using (var stream = Create(String.Join("", Enumerable.Repeat("ABC", 300000))))
         using (var encoded = Encode(stream))
         using (var decoded = Decode(encoded))
         {
            Assert.IsTrue(encoded.Length < 65536);
            Assert.IsTrue(AreEqual(stream, decoded));
         }
         // incompressible streams
         var random = new Byte[1048576];
         rand.NextBytes(random);
         using (var stream = new MemoryStream(random))
         using (var encoded = Encode(stream))
         using (var decoded = Decode(encoded))
         {
            Assert.IsTrue(encoded.Length > decoded.Length);
            Assert.IsTrue(encoded.Length < decoded.Length + 65536);
            Assert.IsTrue(AreEqual(stream, decoded));
         }
      }

      private Stream Create (String data)
      {
         return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(data));
      }
      private Stream Encode (String data)
      {
         return Encode(Create(data));
      }
      private Stream Encode (Stream stream)
      {
         MemoryStream copy = new MemoryStream();
         new CompressionStream(stream, CompressionMode.Compress)
            .CopyTo(copy);
         copy.Position = 0;
         return copy;
      }
      private Stream Decode (Stream stream)
      {
         MemoryStream copy = new MemoryStream();
         new CompressionStream(stream, CompressionMode.Decompress)
            .CopyTo(copy);
         copy.Position = 0;
         return copy;
      }
      private Stream RoundTrip (String data)
      {
         return Decode(Encode(data));
      }

      private Boolean AreEqual (Stream stream1, Stream stream2)
      {
         stream1.Position = stream2.Position = 0;
         var buffer1 = new Byte[8192];
         var buffer2 = new Byte[8192];
         for (; ; )
         {
            var read1 = stream1.Read(buffer1, 0, buffer1.Length);
            var read2 = stream2.Read(buffer2, 0, buffer2.Length);
            if (read1 != read2)
               return false;
            if (read1 == 0)
               break;
            if (!Enumerable.SequenceEqual(buffer1.Take(read1), buffer2.Take(read2)))
               return false;
         }
         return true;
      }

      private void AssertException (Action a)
      {
         try { a(); }
         catch { return; }
         Assert.Fail("Expected: exception");
      }
   }
}
