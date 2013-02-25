//===========================================================================
// MODULE:  FileSystem.cs
// PURPOSE: SkyFloe file system facade
// 
// Copyright © 2013
// Brent M. Spell. All rights reserved.
//
// This library is free software; you can redistribute it and/or modify it 
// under the terms of the GNU Lesser General Public License as published 
// by the Free Software Foundation; either version 3 of the License, or 
// (at your option) any later version. This library is distributed in the 
// hope that it will be useful, but WITHOUT ANY WARRANTY; without even the 
// implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
// See the GNU Lesser General Public License for more details. You should 
// have received a copy of the GNU Lesser General Public License along with 
// this library; if not, write to 
//    Free Software Foundation, Inc. 
//    51 Franklin Street, Fifth Floor 
//    Boston, MA 02110-1301 USA
//===========================================================================
// System References
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
// Project References

namespace SkyFloe.IO
{
   /// <summary>
   /// The file system facade
   /// </summary>
   /// <remarks>
   /// This class encapsulates all file system operations within the SkyFloe
   /// system, simplifying creating/opening files, traversing paths, and 
   /// access to metadata.
   /// </remarks>
   public static class FileSystem
   {
      /// <summary>
      /// Retrieves the file system metadata for a file/directory
      /// </summary>
      /// <param name="path">
      /// Path to retrieve
      /// </param>
      /// <returns>
      /// The file system metadata for the path 
      /// </returns>
      public static Metadata GetMetadata (Path path)
      {
         var info = new FileInfo(path);
         if ((Int32)info.Attributes == -1)
            return new Metadata(path);
         if (info.Attributes.HasFlag(FileAttributes.Directory))
            return new Metadata(new DirectoryInfo(path));
         return new Metadata(info);
      }
      /// <summary>
      /// Removes the read-only flag from a file/directory
      /// </summary>
      /// <param name="path">
      /// The path to modify
      /// </param>
      public static void MakeWritable (Path path)
      {
         var info = new FileInfo(path);
         if (info.Attributes.HasFlag(FileAttributes.Directory))
            foreach (var child in Directory.EnumerateFiles(
                  path, 
                  "*", 
                  SearchOption.AllDirectories
               )
            )
               MakeWritable((Path)child);
         else
            info.Attributes &= ~FileAttributes.ReadOnly;
      }
      /// <summary>
      /// Creates a new file system directory, and its ancestors
      /// </summary>
      /// <param name="path">
      /// Path to the directory to create
      /// </param>
      public static void CreateDirectory (Path path)
      {
         Directory.CreateDirectory(path);
      }
      /// <summary>
      /// Creates and opens a new temporary file
      /// </summary>
      /// <returns>
      /// The stream open on the temp file
      /// </returns>
      public static TempStream Temp ()
      {
         var info = new FileInfo(System.IO.Path.GetTempFileName());
         info.Attributes |= FileAttributes.Temporary;
         return new TempStream((Path)info.FullName);
      }
      /// <summary>
      /// Opens an existing file for reading
      /// Fails if the file does not exist
      /// </summary>
      /// <param name="path">
      /// Path to the file to open
      /// </param>
      /// <param name="share">
      /// File sharing flag
      /// </param>
      /// <returns>
      /// A stream for the opened file
      /// </returns>
      public static Stream Open (Path path, FileShare share = FileShare.Read)
      {
         return new FileStream(
            path, 
            FileMode.Open, 
            FileAccess.Read, 
            share
         );
      }
      /// <summary>
      /// Creates a new file for reading/writing
      /// Fails if the file exists
      /// </summary>
      /// <param name="path">
      /// Path to the file to create
      /// </param>
      /// <returns>
      /// A stream for the new file
      /// </returns>
      public static Stream Create (Path path)
      {
         return new FileStream(
            path, 
            FileMode.CreateNew, 
            FileAccess.ReadWrite, 
            FileShare.Read
         );
      }
      /// <summary>
      /// Creates a new file or opens and truncates an existing file
      /// The file is opened for reading/writing
      /// </summary>
      /// <param name="path">
      /// Path to the file to truncate
      /// </param>
      /// <returns>
      /// A stream for the new file
      /// </returns>
      public static Stream Truncate (Path path)
      {
         return new FileStream(
            path, 
            FileMode.Create, 
            FileAccess.ReadWrite, 
            FileShare.Read
         );
      }
      /// <summary>
      /// Creates a new file or opens an existing file for reading/writing
      /// Seeks to the end of the file
      /// </summary>
      /// <param name="path">
      /// Path to the file to append
      /// </param>
      /// <returns>
      /// A stream for the new file
      /// </returns>
      public static Stream Append (Path path)
      {
         var stream = new FileStream(
            path, 
            FileMode.OpenOrCreate, 
            FileAccess.ReadWrite, 
            FileShare.Read
         );
         stream.Seek(0, SeekOrigin.End);
         return stream;
      }
      /// <summary>
      /// Deletes an existing file/directory
      /// </summary>
      /// <param name="path">
      /// Path to the file/directory to delete
      /// </param>
      public static void Delete (Path path)
      {
         var info = new FileInfo(path);
         if ((Int32)info.Attributes != -1)
            if (info.Attributes.HasFlag(FileAttributes.Directory))
               Directory.Delete(path, true);
            else
               File.Delete(path);
      }
      /// <summary>
      /// Copies a file from a source to a target,
      /// overwriting the destination if it exists
      /// Fails if the source file does not exist
      /// </summary>
      /// <param name="source">
      /// The source file path
      /// </param>
      /// <param name="target">
      /// The target file path
      /// </param>
      public static void Copy (Path source, Path target)
      {
         File.Copy(source, target, true);
      }
      /// <summary>
      /// Lists the immediate child files/directories in a path
      /// </summary>
      /// <param name="parent">
      /// The path to enumerate
      /// </param>
      /// <returns>
      /// The list of metadata for the children of the specified path
      /// </returns>
      public static IEnumerable<Metadata> Children (Path parent)
      {
         return Directory
            .EnumerateFileSystemEntries(parent)
            .Select(p => GetMetadata((Path)p));
      }
      /// <summary>
      /// Lists the descendant child files/directories in a path
      /// </summary>
      /// <param name="parent">
      /// The path to enumerate
      /// </param>
      /// <returns>
      /// The list of metadata for the descendants of the specified path
      /// </returns>
      public static IEnumerable<Metadata> Descendants (Path parent)
      {
         return Directory
            .EnumerateFileSystemEntries(parent, "*", SearchOption.AllDirectories)
            .Select(p => GetMetadata((Path)p));
      }

      /// <summary>
      /// Temporary file stream class
      /// </summary>
      /// <remarks>
      /// This class extends the file stream to provide the ability to
      /// retrieve the path to the temporary file that was opened and to
      /// automatically delete the file during disposal.
      /// </remarks>
      public class TempStream : FileStream
      {
         /// <summary>
         /// Initializes a new temporary stream instance
         /// </summary>
         /// <param name="path">
         /// Path to the file to open
         /// </param>
         internal TempStream (Path path)
            : base(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)
         {
            this.Path = path;
         }
         
         /// <summary>
         /// The path to the open temporary file
         /// </summary>
         public Path Path { get; private set; }
         /// <summary>
         /// Deletes the temporary file
         /// </summary>
         /// <param name="disposing">
         /// True to release both managed and unmanaged resources
         /// False to release only unmanaged resources
         /// </param>
         protected override void Dispose (Boolean disposing)
         {
            base.Dispose(disposing);
            if (!this.Path.IsEmpty)
               try { File.Delete(this.Path); }
               catch { }
            this.Path = Path.Empty;
         }
      }

      /// <summary>
      /// File system metadata
      /// </summary>
      /// <remarks>
      /// This class encapsulates file system metadata, combining the
      /// capabilities of the BCL FileInfo and DirectoryInfo classes,
      /// with additional support for paths that do not exist.
      /// </remarks>
      public class Metadata
      {
         public static readonly Metadata Empty = new Metadata();

         /// <summary>
         /// Initializes an empty metadata instance
         /// </summary>
         public Metadata ()
         {
         }
         /// <summary>
         /// Initializes a new metadata instance for a path
         /// not necessarily in the file system
         /// </summary>
         /// <param name="path">
         /// The file system path to attach
         /// </param>
         public Metadata (Path path)
         {
            this.Path = path;
            this.Name = path.FileName;
         }
         /// <summary>
         /// Initializes a new metadata instance for a path
         /// in the file system
         /// </summary>
         /// <param name="info">
         /// File system metadata to attach
         /// </param>
         public Metadata (FileSystemInfo info)
         {
            this.Path = (Path)info.FullName;
            this.Name = info.Name;
            this.Exists = info.Exists;
            this.IsDirectory = info.Attributes.HasFlag(FileAttributes.Directory);
            this.IsSystem = info.Attributes.HasFlag(FileAttributes.System);
            this.IsHidden = info.Attributes.HasFlag(FileAttributes.Hidden);
            this.IsReadOnly = info.Attributes.HasFlag(FileAttributes.ReadOnly);
            this.Created = info.CreationTimeUtc;
            this.Updated = info.LastWriteTimeUtc;
         }
         /// <summary>
         /// Initializes a new metadata instance for an
         /// existing file in the file system
         /// </summary>
         /// <param name="info"></param>
         public Metadata (FileInfo info) : this((FileSystemInfo)info)
         {
            this.Length = info.Length;
         }

         /// <summary>
         /// The full path to the file/directory
         /// </summary>
         public Path Path { get; private set; }
         /// <summary>
         /// The name of the file/directory
         /// </summary>
         public String Name { get; private set; }
         /// <summary>
         /// Indicates whether the file/directory exists
         /// </summary>
         public Boolean Exists { get; private set; }
         /// <summary>
         /// The length of the file (0 for directories)
         /// </summary>
         public Int64 Length { get; private set; }
         /// <summary>
         /// Indicates whether the metadata represents a directory
         /// </summary>
         public Boolean IsDirectory { get; private set; }
         /// <summary>
         /// Indicates whether the file/directory has the system attribute
         /// </summary>
         public Boolean IsSystem { get; private set; }
         /// <summary>
         /// Indicates whether the file/directory has the hidden attribute
         /// </summary>
         public Boolean IsHidden { get; private set; }
         /// <summary>
         /// Indicates whether the file/directory has the read-only attribute
         /// </summary>
         public Boolean IsReadOnly { get; private set; }
         /// <summary>
         /// The file/directory creation time, in UTC
         /// </summary>
         public DateTime Created { get; private set; }
         /// <summary>
         /// The file/directory last write time, in UTC
         /// </summary>
         public DateTime Updated { get; private set; }
      }
   }
}
