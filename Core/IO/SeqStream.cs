using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkyFloe.IO
{
   public enum StreamMode
   {
      None = 0,
      Read = 1,
      Write = 2
   }

   public abstract class SeqStream : Stream
   {
      private StreamMode mode;

      public SeqStream (StreamMode mode)
      {
         switch (mode)
         {
            case StreamMode.Read:
               break;
            case StreamMode.Write:
               break;
            default:
               throw new ArgumentException("mode");
         }
         this.mode = mode;
      }

      #region Stream Overrides
      public override Boolean CanSeek
      {
         get { return false; }
      }
      public override Boolean CanRead
      {
         get { return this.mode == StreamMode.Read; }
      }
      public override Boolean CanWrite
      {
         get { return this.mode == StreamMode.Write; }
      }
      public override Int64 Position
      {
         get { throw new NotSupportedException(); }
         set { throw new NotSupportedException(); }
      }
      public override Int64 Seek (Int64 offset, SeekOrigin origin)
      {
         throw new NotSupportedException();
      }
      public override void SetLength (Int64 value)
      {
         throw new NotSupportedException();
      }
      #endregion
   }
}
