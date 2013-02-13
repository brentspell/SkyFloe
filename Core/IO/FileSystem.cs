using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkyFloe.IO
{
   public static class FileSystem
   {
      public static Metadata GetMetadata (Path path)
      {
         FileInfo info = new FileInfo(path);
         if ((Int32)info.Attributes == -1)
            return new Metadata(path);
         if (info.Attributes.HasFlag(FileAttributes.Directory))
            return new Metadata(new DirectoryInfo(path));
         return new Metadata(info);
      }
      public static void MakeWritable (Path path)
      {
         FileInfo info = new FileInfo(path);
         if (info.Attributes.HasFlag(FileAttributes.Directory))
            foreach (IO.Path child in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
               MakeWritable(child);
         else
            info.Attributes &= ~FileAttributes.ReadOnly;
      }
      public static void CreateDirectory (Path path)
      {
         Directory.CreateDirectory(path);
      }
      public static TempStream Temp ()
      {
         FileInfo info = new FileInfo(System.IO.Path.GetTempFileName());
         info.Attributes |= FileAttributes.Temporary;
         return new TempStream(info.FullName);
      }

      public static Stream Open (Path path, FileShare share = FileShare.Read)
      {
         return new FileStream(
            path, 
            FileMode.Open, 
            FileAccess.Read, 
            share
         );
      }
      public static Stream Create (Path path)
      {
         return new FileStream(
            path, 
            FileMode.CreateNew, 
            FileAccess.ReadWrite, 
            FileShare.Read
         );
      }
      public static Stream Truncate (Path path)
      {
         return new FileStream(
            path, 
            FileMode.Create, 
            FileAccess.ReadWrite, 
            FileShare.Read
         );
      }
      public static Stream Append (Path path)
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
      public static void Delete (Path path)
      {
         FileSystemInfo info = new FileInfo(path);
         if ((Int32)info.Attributes != -1)
            if (info.Attributes.HasFlag(FileAttributes.Directory))
               Directory.Delete(path, true);
            else
               File.Delete(path);
      }
      public static void Copy (IO.Path source, IO.Path target)
      {
         File.Copy(source, target, true);
      }
      public static IEnumerable<Metadata> Children (IO.Path parent)
      {
         return Directory.EnumerateFileSystemEntries(parent)
            .Select(p => GetMetadata(p));
      }
      public static IEnumerable<Metadata> Descendants (IO.Path parent)
      {
         return Directory
            .EnumerateFileSystemEntries(parent, "*", SearchOption.AllDirectories)
            .Select(p => GetMetadata(p));
      }

      public class TempStream : FileStream
      {
         internal TempStream (Path path)
            : base(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)
         {
            this.Path = path;
         }
         public Path Path { get; private set; }
         protected override void Dispose (Boolean disposing)
         {
            base.Dispose(disposing);
            if (!this.Path.IsEmpty)
               try { File.Delete(this.Path); }
               catch { }
            this.Path = Path.Empty;
         }
      }

      public class Metadata
      {
         public Metadata (IO.Path path)
         {
            this.Path = path;
            this.Name = path.FileName;
         }
         public Metadata (FileSystemInfo info)
         {
            this.Path = info.FullName;
            this.Name = info.Name;
            this.Exists = info.Exists;
            this.IsDirectory = info.Attributes.HasFlag(FileAttributes.Directory);
            this.IsSystem = info.Attributes.HasFlag(FileAttributes.System);
            this.IsHidden = info.Attributes.HasFlag(FileAttributes.Hidden);
            this.IsReadOnly = info.Attributes.HasFlag(FileAttributes.ReadOnly);
            this.Created = info.CreationTimeUtc;
            this.Updated = info.LastWriteTimeUtc;
         }
         public Metadata (FileInfo info) : this((FileSystemInfo)info)
         {
            this.Length = info.Length;
         }

         public IO.Path Path { get; private set; }
         public String Name { get; private set; }
         public Boolean Exists { get; private set; }
         public Int64 Length { get; private set; }
         public Boolean IsDirectory { get; private set; }
         public Boolean IsSystem { get; private set; }
         public Boolean IsHidden { get; private set; }
         public Boolean IsReadOnly { get; private set; }
         public DateTime Created { get; private set; }
         public DateTime Updated { get; private set; }
      }
   }
}
