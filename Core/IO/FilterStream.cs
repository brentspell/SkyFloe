using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkyFloe.IO
{
   public class FilterStream : Stream
   {
      Stream baseStream;

      public FilterStream (Stream baseStream)
      {
         this.baseStream = baseStream;
      }
      protected override void Dispose (Boolean disposing)
      {
         if (this.baseStream != null)
            this.baseStream.Dispose();
         this.baseStream = null;
         base.Dispose(disposing);
      }

      #region Stream Overrides
      public override Boolean CanSeek
      {
         get { return false; }
      }
      public override Boolean CanRead
      {
         get { return this.baseStream.CanRead; }
      }
      public override Boolean CanWrite
      {
         get { return this.baseStream.CanWrite; }
      }
      public override Int64 Position
      {
         get { throw new NotSupportedException(); }
         set { throw new NotSupportedException(); }
      }
      public override Int64 Length
      {
         get { throw new NotSupportedException(); }
      }
      public override void SetLength (Int64 value)
      {
         throw new NotSupportedException();
      }
      public override Int64 Seek (Int64 offset, SeekOrigin origin)
      {
         throw new NotSupportedException();
      }
      public override Int32 Read (Byte[] buffer, Int32 offset, Int32 count)
      {
         if (!this.CanRead)
            throw new InvalidOperationException();
         Int32 read = this.baseStream.Read(buffer, offset, count);
         Filter(buffer, offset, read);
         return read;
      }
      public override void Write (Byte[] buffer, Int32 offset, Int32 count)
      {
         if (!this.CanWrite)
            throw new InvalidOperationException();
         Filter(buffer, offset, count);
         this.baseStream.Write(buffer, offset, count);
      }
      public override void Flush ()
      {
         this.baseStream.Flush();
      }
      #endregion

      #region FilterStream Overrides
      protected virtual void Filter (Byte[] buffer, Int32 offset, Int32 count)
      {
      }
      #endregion
   }
}
