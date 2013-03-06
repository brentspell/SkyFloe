//===========================================================================
// MODULE:  Difference.cs
// PURPOSE: file system/backup archive differencing task
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
// Project References

namespace SkyFloe.Tasks
{
   /// <summary>
   /// Difference archive task
   /// </summary>
   /// <remarks>
   /// This task calculates the difference between a file system directory
   /// tree and a backup index tree. It generates new/changed/deleted diff
   /// results that can be used to create a new archive or add entries
   /// to update an existing archive.
   /// </remarks>
   public class Difference : Task
   {
      /// <summary>
      /// The file system difference request
      /// </summary>
      public DiffRequest Request { get; set; }

      /// <summary>
      /// Task validation override
      /// </summary>
      protected override void DoValidate ()
      {
         if (this.Request == null)
            throw new ArgumentException("Request");
         foreach (var map in this.Request.RootPathMap)
         {
            if (map.Key.IsEmpty)
               throw new ArgumentException("Request.RootPathMap.Key");
            if (map.Value.IsEmpty)
               throw new ArgumentException("Request.RootPathMap.Value");
         }
         if (!this.Request.Filter.IsValid)
            throw new ArgumentException("Request.Filter");
         switch (this.Request.Method)
         {
            case DiffMethod.Timestamp: break;
            case DiffMethod.Digest: break;
            default:
               throw new ArgumentException("Request.Method");
         }
      }
      /// <summary>
      /// Task execution override
      /// </summary>
      protected override void DoExecute ()
      {
         foreach (var root in this.Archive.BackupIndex.ListNodes(null))
            foreach (var entry in Enumerate(root))
               ReportProgress(new ProgressEventArgs() { DiffEntry = entry });
      }
      /// <summary>
      /// Performs a differencing operation
      /// </summary>
      /// <param name="node">
      /// The root backup node to difference
      /// </param>
      /// <returns>
      /// The list of differences between the root node and the file system
      /// </returns>
      public IEnumerable<DiffResult> Enumerate (Backup.Node node)
      {
         var path = (IO.Path)node.Name;
         var mapPath = IO.Path.Empty;
         if (this.Request.RootPathMap.TryGetValue(path, out mapPath))
            path = mapPath;
         return DiffPathIndex(path, node).Concat(DiffIndexPath(node, path));
      }
      /// <summary>
      /// Differences a file system path against a backup node,
      /// for detecting new files/directories
      /// </summary>
      /// <param name="parentPath">
      /// The container path to compare
      /// </param>
      /// <param name="parentNode">
      /// The container backup node to compare
      /// </param>
      /// <returns>
      /// The list of differences between the two subtrees
      /// </returns>
      private IEnumerable<DiffResult> DiffPathIndex (IO.Path parentPath, Backup.Node parentNode)
      {
         // retrieve the list of child file system entries
         var files = WithRetry(
            "DiffPathIndex",
            () => IO.FileSystem.Children(parentPath).ToList()
         ) ?? Enumerable.Empty<IO.FileSystem.Metadata>();
         // retrieve the list of child nodes
         var nodes = this.Archive.BackupIndex.ListNodes(parentNode).ToList();
         // perform the difference
         foreach (var file in files)
         {
            this.Canceler.ThrowIfCancellationRequested();
            // always ignore system files/directories
            if (!file.IsSystem)
            {
               // ignore any files that do not match the filter
               // always include directories, though, since the filter
               // only applies to file paths
               if (file.IsDirectory || this.Request.Filter.Evaluate(file.Path))
               {
                  // if no matching node is found, return a new difference
                  var node = nodes.FirstOrDefault(n => n.NameEquals(file.Name));
                  if (node == null)
                     yield return new DiffResult()
                     {
                        Type = DiffType.New,
                        Node = node = new Backup.Node()
                        {
                           Parent = parentNode,
                           Type = file.IsDirectory ?
                              Backup.NodeType.Directory :
                              Backup.NodeType.File,
                           Name = file.Name
                        }
                     };
                  // recursively difference subdirectories
                  if (file.IsDirectory)
                     foreach (var diff in DiffPathIndex(file.Path, node))
                        yield return diff;
               }
            }
         }
      }
      /// <summary>
      /// Differences a backup node against a file system path,
      /// for detecting new (undeleted), changed, and deleted files
      /// </summary>
      /// <param name="parentNode">
      /// The container backup node to compare
      /// </param>
      /// <param name="parentPath">
      /// The container path to compare
      /// </param>
      /// <returns></returns>
      private IEnumerable<DiffResult> DiffIndexPath (Backup.Node parentNode, IO.Path parentPath)
      {
         foreach (var node in this.Archive.BackupIndex.ListNodes(parentNode))
         {
            this.Canceler.ThrowIfCancellationRequested();
            var path = parentPath + node.Name;
            // recursively difference subdirectory nodes
            if (node.Type == Backup.NodeType.Directory)
               foreach (var diff in DiffIndexPath(node, path))
                  yield return diff;
            else
            {
               // retrieve the file system entry for the node
               var file = WithRetry(
                  "DiffIndexPath",
                  () => IO.FileSystem.GetMetadata(path)
               ) ?? IO.FileSystem.Metadata.Empty;
               // fetch the latest backup entry for the node
               var entry = this.Archive.BackupIndex
                  .ListNodeEntries(node)
                  .OrderBy(e => e.Session.Created)
                  .LastOrDefault();
               // difference the file against the last backup entry
               var diff = DiffEntryFile(node, entry, file);
               if (diff != null)
                  yield return diff;
            }
         }
      }
      /// <summary>
      /// Differences a backup entry against a file system file
      /// </summary>
      /// <param name="node">
      /// The backup node to compare
      /// </param>
      /// <param name="entry">
      /// The last backup entry for the node (if any) to compare
      /// </param>
      /// <param name="file">
      /// The file system entry to compare
      /// </param>
      /// <returns>
      /// A new/changed/deleted difference result if a change was detected
      /// Null otherwise
      /// </returns>
      private DiffResult DiffEntryFile (
         Backup.Node node,
         Backup.Entry entry, 
         IO.FileSystem.Metadata file)
      {
         // if the file is not found, was changed to system, or
         // no longer matches the backup filter, AND
         // there was an existing non-delete backup entry,
         // then generate a deleted diff
         if (!file.Exists || file.IsSystem || !this.Request.Filter.Evaluate(file.Path))
         {
            if (entry != null && entry.State != Backup.EntryState.Deleted)
               return new DiffResult()
               {
                  Type = DiffType.Deleted,
                  Node = node
               };
         }
         // if the file was found, but the last backup entry
         // for it indicated a delete, then generate a new diff
         else if (entry == null || entry.State == Backup.EntryState.Deleted)
         {
            return new DiffResult()
            {
               Type = DiffType.New,
               Node = node
            };
         }
         // otherwise, detect a change diff only if there
         // is not a pending backup entry for the file
         else if (entry.State != Backup.EntryState.Pending)
         {
            var isChanged = false;
            switch (this.Request.Method)
            {
               case DiffMethod.Timestamp:
                  if (file.Updated > entry.Session.Created)
                     isChanged = true;
                  break;
               case DiffMethod.Digest:
                  isChanged = WithRetry(
                     "CalculateCrc",
                     () => IO.CrcFilter.Calculate(file.Path) != entry.Crc32
                  );
                  break;
            }
            if (isChanged)
               return new DiffResult()
               {
                  Type = DiffType.Changed,
                  Node = node
               };
         }
         return null;
      }
   }
}
