using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkyFloe.IO
{
   public static class FileSystem
   {
      public static TempStream Temp ()
      {
         FileInfo info = new FileInfo(Path.GetTempFileName());
         info.Attributes |= FileAttributes.Temporary;
         return new TempStream(info.FullName);
      }

      public static Stream Open (String path, FileShare share = FileShare.Read)
      {
         return new FileStream(
            path, 
            FileMode.Open, 
            FileAccess.Read, 
            share
         );
      }
      public static Stream Create (String path)
      {
         return new FileStream(
            path, 
            FileMode.CreateNew, 
            FileAccess.ReadWrite, 
            FileShare.Read
         );
      }
      public static Stream Truncate (String path)
      {
         return new FileStream(
            path, 
            FileMode.Create, 
            FileAccess.ReadWrite, 
            FileShare.Read
         );
      }
      public static Stream Append (String path)
      {
         Stream stream = new FileStream(
            path, 
            FileMode.OpenOrCreate, 
            FileAccess.ReadWrite, 
            FileShare.Read
         );
         stream.Seek(0, SeekOrigin.End);
         return stream;
      }

      public class TempStream : FileStream
      {
         internal TempStream (String path)
            : base(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)
         {
            this.Path = path;
         }
         public String Path
         {
            get;
            private set;
         }
         protected override void Dispose (Boolean disposing)
         {
            base.Dispose(disposing);
            if (this.Path != null)
               try { File.Delete(this.Path); }
               catch { }
            this.Path = null;
         }
      }
   }
}
