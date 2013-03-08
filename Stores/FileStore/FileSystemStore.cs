//===========================================================================
// MODULE:  FileSystemStore.cs
// PURPOSE: file-based backup store
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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
// Project References
using SkyFloe.Store;

namespace SkyFloe
{
   /// <summary>
   /// File system backup store
   /// </summary>
   /// <remarks>
   /// This class is the entry point to the file-based backup store. The
   /// file store is represented by a root directory containing a set of
   /// subdirectories, one per archive. Each archive subdirectory contains
   /// a backup index file and a blob file.
   /// </remarks>
   public class FileSystemStore : IStore
   {
      /// <summary>
      /// Initializes a new store instance
      /// </summary>
      public FileSystemStore ()
      {
      }
      /// <summary>
      /// Releases resources associated with the store
      /// </summary>
      public void Dispose ()
      {
      }

      #region Connection Properties
      /// <summary>
      /// The path to the root directory of the store
      /// </summary>
      [Required]
      public String Path { get; set; }
      #endregion

      #region IStore Implementation
      /// <summary>
      /// A descriptive name for the store
      /// </summary>
      public String Caption
      {
         get { return this.Path; }
      }
      /// <summary>
      /// Connects to the backup store
      /// </summary>
      public void Open ()
      {
         IO.FileSystem.CreateDirectory((IO.Path)this.Path);
      }
      /// <summary>
      /// Enumerates the archive names in the store,
      /// which are the subdirectories within the root directory
      /// </summary>
      /// <returns>
      /// The store archive name enumeration
      /// </returns>
      public IEnumerable<String> ListArchives ()
      {
         return IO.FileSystem.Children((IO.Path)this.Path).Select(p => p.Name);
      }
      /// <summary>
      /// Creates and connects to a new backup archive
      /// </summary>
      /// <param name="name">
      /// The name of the new archive
      /// </param>
      /// <param name="header">
      /// The archive backup index header
      /// </param>
      /// <returns>
      /// The connected archive implementation
      /// </returns>
      public IArchive CreateArchive (String name, Backup.Header header)
      {
         var archive = new FileSystemArchive((IO.Path)this.Path + name);
         archive.Create(header);
         return archive;
      }
      /// <summary>
      /// Opens an existing backup archive
      /// </summary>
      /// <param name="name">
      /// The name of the archive to open
      /// </param>
      /// <returns>
      /// The connected archive implementation
      /// </returns>
      public IArchive OpenArchive (String name)
      {
         var archive = new FileSystemArchive((IO.Path)this.Path + name);
         archive.Open();
         return archive;
      }
      /// <summary>
      /// Permanently removes an archive from the store
      /// </summary>
      /// <param name="name">
      /// The name of the archive to delete
      /// </param>
      public void DeleteArchive (String name)
      {
         IO.FileSystem.Delete((IO.Path)this.Path + name);
      }
      #endregion
   }
}
