using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SkyFloe.IO;

namespace SkyFloe.Core.Test.IO
{
   [TestClass]
   public class TestFilterStream
   {
      [TestMethod]
      public void TestConstruction ()
      {
         // invalid construction
         AssertException(() => new FilterStream(null));
         // valid construction
         new FilterStream(CreateStream("test"));
         new FilterStream(new FilterStream(CreateStream("test")));
      }

      [TestMethod]
      public void TestProperties ()
      {
         using (var filter = new TestFilter(CreateStream("test")))
         {
            // invalid properties
            Assert.IsFalse(filter.CanSeek);
            AssertException(() => { var x = filter.Position; });
            AssertException(() => filter.Position = 10);
            AssertException(() => { var x = filter.Length; });
            AssertException(() => filter.SetLength(10));
            // valid properties
            Assert.IsTrue(filter.CanRead);
            Assert.IsTrue(filter.CanWrite);
            Assert.IsFalse(filter.Disposed);
            Assert.IsFalse(filter.Flushed);
         }
         // read-only filter
         using (var filter = new TestFilter(CreateStream("test", false)))
         {
            Assert.IsTrue(filter.CanRead);
            Assert.IsFalse(filter.CanWrite);
            Assert.IsFalse(filter.Disposed);
            Assert.IsFalse(filter.Flushed);
         }
      }

      [TestMethod]
      public void TestOperations ()
      {
         // invalid operations
         using (var filter = new FilterStream(CreateStream("test", true)))
         {
            AssertException(() => filter.Seek(0, SeekOrigin.Current));
            AssertException(() => filter.Write(new Byte[10], 0, 10));
         }
         // filter read/write
         using (var reader = new TestFilter(CreateStream("test")))
         using (var writer = new TestFilter(new MemoryStream()))
         {
            reader.CopyTo(writer);
            Assert.IsTrue(AreEqual(reader.Log, writer.Log));
         }
         using (var reader = new TestFilter(CreateStream(String.Join("", Enumerable.Repeat("123", 1000)))))
         using (var writer = new TestFilter(new MemoryStream()))
         {
            reader.CopyTo(writer);
            Assert.IsTrue(AreEqual(reader.Log, writer.Log));
         }
         // filter flush/dispose
         using (var reader1 = new TestFilter(CreateStream("test")))
         using (var reader2 = new FilterStream(reader1))
         using (var writer1 = new TestFilter(new MemoryStream()))
         using (var writer2 = new FilterStream(writer1))
         {
            reader2.CopyTo(writer2);
            Assert.IsTrue(AreEqual(reader1.Log, writer1.Log));
            Assert.IsFalse(writer1.Flushed);
            writer2.Flush();
            Assert.IsTrue(writer1.Flushed);
            Assert.IsFalse(reader1.Disposed);
            Assert.IsFalse(writer1.Disposed);
            reader2.Dispose();
            writer2.Dispose();
            Assert.IsTrue(reader1.Disposed);
            Assert.IsTrue(writer1.Disposed);
         }
      }

      private Stream CreateStream (String data, Boolean writable = true)
      {
         return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(data), writable);
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

      private class TestFilter : FilterStream
      {
         public MemoryStream Log { get; private set; }
         public Boolean Disposed { get; private set; }
         public Boolean Flushed { get; private set; }

         public TestFilter (Stream baseStream) : base(baseStream)
         {
            this.Log = new MemoryStream();
         }

         protected override void Dispose (Boolean disposing)
         {
            base.Dispose(disposing);
            this.Disposed = true;
         }

         public override void Flush ()
         {
            base.Flush();
            this.Flushed = true;
         }

         protected override void Filter (Byte[] buffer, Int32 offset, Int32 count)
         {
            this.Log.Write(buffer, offset, count);
         }
      }
   }
}
