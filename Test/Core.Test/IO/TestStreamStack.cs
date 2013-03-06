using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SkyFloe.IO;

namespace SkyFloe.Core.Test.IO
{
   [TestClass]
   public class TestStreamStack
   {
      [TestMethod]
      public void TestProperties ()
      {
         // invalid properties
         using (var stack = new StreamStack())
         {
            Assert.IsFalse(stack.CanSeek);
            Assert.IsFalse(stack.CanRead);
            Assert.IsFalse(stack.CanWrite);
            AssertException(() => { var x = stack.Position; });
            AssertException(() => stack.Position = 10);
            AssertException(() => { var x = stack.Length; });
            AssertException(() => stack.SetLength(10));
            Assert.IsNull(stack.Top);
         }
         // valid properties
         using (var stack = new StreamStack())
         {
            var top = new MemoryStream();
            stack.Push(top);
            Assert.IsTrue(stack.CanRead);
            Assert.IsTrue(stack.CanWrite);
            Assert.AreEqual(stack.Top, top);
         }
         using (var stack = new StreamStack())
         {
            stack.Push(new MemoryStream(new Byte[10], false));
            Assert.IsTrue(stack.CanRead);
            Assert.IsFalse(stack.CanWrite);
         }
      }

      [TestMethod]
      public void TestOperations ()
      {
         // invalid operations
         using (var stack = new StreamStack())
         {
            AssertException(() => stack.Push(null));
            Assert.IsNull(stack.GetStream<Stream>());
            stack.Push(new MemoryStream());
            AssertException(() => stack.Seek(0, SeekOrigin.Current));
         }
         // stream access
         using (var stack = new StreamStack())
         {
            var buffer = new MemoryStream();
            var test = new TestStream(buffer);
            stack.Push(buffer);
            Assert.AreEqual(stack.Top, buffer);
            stack.Push(test);
            Assert.AreEqual(stack.Top, test);
            Assert.AreEqual(stack.GetStream<MemoryStream>(), buffer);
            Assert.AreEqual(stack.GetStream<TestStream>(), test);
         }
         // stream flush
         using (var stack = new StreamStack())
         {
            var stream1 = new TestStream(new MemoryStream());
            var stream2 = new TestStream(stream1);
            stack.Push(stream1);
            stack.Push(stream2);
            stack.Flush();
            Assert.IsTrue(stream1.Flushed);
            Assert.IsTrue(stream2.Flushed);
         }
         // stream read
         using (var stack = new StreamStack())
         {
            var buffer = new MemoryStream();
            StreamWrite(buffer, "test");
            buffer.Position = 0;
            stack.Push(buffer);
            stack.Push(new TestStream(stack.Top));
            Assert.AreEqual(StreamRead(stack), "test");
         }
         // stream write
         using (var stack = new StreamStack())
         {
            var buffer = new MemoryStream();
            stack.Push(buffer);
            stack.Push(new TestStream(stack.Top));
            StreamWrite(stack, "test");
            buffer.Position = 0;
            Assert.AreEqual(StreamRead(buffer), "test");
         }
         // stream disposal
         using (var stack = new StreamStack())
         {
            var stream1 = new TestStream(new MemoryStream());
            var stream2 = new TestStream(stream1);
            stack.Push(stream1);
            stack.Push(stream2);
            stack.Dispose();
            Assert.IsTrue(stream1.Disposed);
            Assert.IsTrue(stream2.Disposed);
         }
      }

      private class TestStream : FilterStream
      {
         public Boolean Disposed { get; private set; }
         public Boolean Flushed { get; private set; }

         public TestStream (Stream baseStream)
            : base(baseStream)
         {
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
      }

      private String StreamRead (Stream stream)
      {
         var buffer = new Byte[65536];
         var read = stream.Read(buffer, 0, buffer.Length);
         return Encoding.UTF8.GetString(buffer, 0, read);
      }

      private void StreamWrite (Stream stream, String data)
      {
         var buffer = Encoding.UTF8.GetBytes(data);
         stream.Write(buffer, 0, buffer.Length);
      }

      private void AssertException (Action a)
      {
         try { a(); }
         catch { return; }
         Assert.Fail("Expected: exception");
      }
   }
}
