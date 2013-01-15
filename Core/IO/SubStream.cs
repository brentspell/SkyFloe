using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkyFloe.IO
{
   public class SubStream : Stream
   {
      private Stream stream;
      private Int64 offset;
      private Int64 length;

      public SubStream (Stream stream, Int64 offset, Int64 length)
      {
         if (!stream.CanRead || !stream.CanSeek)
            throw new ArgumentException("stream");
         if (offset < 0)
            throw new ArgumentOutOfRangeException("offset");
         if (length < 0)
            throw new ArgumentOutOfRangeException("length");
         this.stream = stream;
         this.offset = offset;
         this.length = length;
         this.stream.Position = this.offset;
      }

      public override Boolean CanRead
      {
         get { return true; }
      }
      public override Boolean CanSeek
      {
         get { return true; }
      }
      public override Boolean CanWrite
      {
         get { return false; }
      }
      public override void Flush ()
      {
      }
      public override Int64 Length
      {
         get { return this.length; }
      }
      public override Int64 Position
      {
         get
         {
            return this.stream.Position - this.offset;
         }
         set
         {
            Seek(value, SeekOrigin.Begin);
         }
      }
      public override Int32 Read (Byte[] buffer, Int32 offset, Int32 count)
      {
         
         throw new NotImplementedException();
      }
      public override Int64 Seek (Int64 offset, SeekOrigin origin)
      {
         switch (origin)
         {
            case SeekOrigin.Begin:
               return this.stream.Seek(this.offset + offset, origin) - this.offset;
            case SeekOrigin.End:
               return this.stream.Seek(this.offset + this.length - offset, origin) - this.offset;
            case SeekOrigin.Current:
               return this.stream.Seek(offset, origin) - this.offset;
            default:
               throw new ArgumentException("origin");
         }
         throw new NotImplementedException();
      }
      public override void SetLength (Int64 value)
      {
         throw new NotSupportedException();
      }
      public override void Write (Byte[] buffer, Int32 offset, Int32 count)
      {
         throw new NotSupportedException();
      }
   }
}
