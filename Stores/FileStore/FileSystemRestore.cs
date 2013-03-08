//===========================================================================
// MODULE:  FileSystemRestore.cs
// PURPOSE: file-based restore implementation
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
using System.Linq;
using Stream = System.IO.Stream;
// Project References
using SkyFloe.Store;

namespace SkyFloe
{
   /// <summary>
   /// File system restore
   /// </summary>
   /// <remarks>
   /// This class implements the IRestore interface for extracting backup
   /// entries from an archive. Entries are retrieved from the archive blob
   /// based on their recorded offset and length into the blob.
   /// </remarks>
   public class FileSystemRestore : IRestore
   {
      private Stream blobFile;

      /// <summary>
      /// Initializes a new restore instance
      /// </summary>
      /// <param name="archive">
      /// The file system archive for the restore
      /// </param>
      /// <param name="session">
      /// The restore session being processed
      /// </param>
      public FileSystemRestore (FileSystemArchive archive, Restore.Session session)
      {
         this.blobFile = IO.FileSystem.Open(archive.BlobPath);
      }
      /// <summary>
      /// Releases the resources associated with the restore
      /// </summary>
      public void Dispose ()
      {
         if (this.blobFile != null)
            this.blobFile.Dispose();
         this.blobFile = null;
      }

      #region IRestore Implementation
      /// <summary>
      /// Retrieves an entry from the archive
      /// </summary>
      /// <param name="entry">
      /// The restore entry to retrieve
      /// </param>
      /// <returns>
      /// A sub-stream of the blob file containing the entry's data
      /// </returns>
      public Stream Restore (Restore.Entry entry)
      {
         return new IO.Substream(this.blobFile, entry.Offset, entry.Length);
      }
      #endregion
   }
}
