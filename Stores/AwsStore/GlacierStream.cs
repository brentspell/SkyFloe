using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkyFloe.Aws
{
   public class GlacierStream : Stream
   {
      private Amazon.Glacier.AmazonGlacierClient glacier;
      private String vault;
      private String jobID;
      private Stream stream;
      private Int64 offset;
      private Int64 length;
      private Byte[] seekBuffer;

      public GlacierStream (
         Amazon.Glacier.AmazonGlacierClient glacier,
         String vault,
         String jobID,
         Int64 length)
      {
         this.glacier = glacier;
         this.vault = vault;
         this.jobID = jobID;
         this.stream = null;
         this.offset = 0;
         this.length = length;
         this.seekBuffer = new Byte[65536];
      }

      protected override void Dispose (Boolean disposing)
      {
         base.Dispose(disposing);
         CloseJob();
      }

      public String JobID { get { return this.jobID; } }

      private void OpenJob ()
      {
         if (this.stream != null)
            CloseJob();
         this.stream = this.glacier.GetJobOutput(
            new Amazon.Glacier.Model.GetJobOutputRequest()
            {
               VaultName = this.vault,
               JobId = this.jobID,
               Range = String.Format("bytes={0}-{1}", this.offset, this.length - 1)
            }
         ).GetJobOutputResult.Body;
      }
      private void CloseJob ()
      {
         if (this.stream != null)
            this.stream.Dispose();
         this.stream = null;
      }

      #region Stream Overrides
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
      public override Int64 Length
      {
         get { return this.length; }
      }
      public override Int64 Position
      {
         get { return this.offset; }
         set { Seek(value, SeekOrigin.Begin); }
      }

      public override Int64 Seek (Int64 offset, SeekOrigin origin)
      {
         Int64 newOffset = 0;
         switch (origin)
         {
            case SeekOrigin.Begin:
               newOffset = offset;
               break;
            case SeekOrigin.End:
               newOffset = this.length + offset;
               break;
            case SeekOrigin.Current:
               newOffset = this.offset + offset;
               break;
         }
         newOffset = Math.Min(Math.Max(newOffset, 0), this.length);
         if (newOffset < this.offset)
            CloseJob();
         if (this.stream == null)
         {
            this.offset = newOffset;
            OpenJob();
         }
         else
         {
            try
            {
               while (this.offset < newOffset)
               {
                  Int32 read = this.stream.Read(
                     this.seekBuffer,
                     0,
                     Math.Min((Int32)(newOffset - this.offset), this.seekBuffer.Length)
                  );
                  if (read == 0)
                     break;
                  this.offset += read;
               }
            }
            catch
            {
               CloseJob();
               throw;
            }
         }
         return this.offset;
      }
      public override Int32 Read (Byte[] buffer, Int32 offset, Int32 count)
      {
         if (this.stream == null)
            OpenJob();
         try
         {
            Int32 read = this.stream.Read(buffer, offset, count);
            this.offset += read;
            return read;
         }
         catch
         {
            CloseJob();
            throw;
         }
      }
      public override void Write (Byte[] buffer, Int32 offset, Int32 count)
      {
         throw new NotSupportedException();
      }
      public override void Flush ()
      {
      }
      public override void SetLength (Int64 value)
      {
         throw new NotSupportedException();
      }
      #endregion
   }
}
