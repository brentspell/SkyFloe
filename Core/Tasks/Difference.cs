using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyFloe.Tasks
{
   public class Difference : Task
   {
      public DiffRequest Request { get; set; }
      
      public override void Execute ()
      {
         foreach (Backup.Node root in this.Archive.BackupIndex.ListNodes(null))
            foreach (DiffEntry entry in Enumerate(root))
               ReportProgress(
                  new Engine.ProgressEventArgs()
                  {
                     DiffEntry = entry
                  }
               );
      }
      public IEnumerable<DiffEntry> Enumerate (Backup.Node root)
      {
         IO.Path path = root.Name;
         IO.Path mapPath = null;
         if (this.Request.RootPathMap.TryGetValue(path, out mapPath))
            path = mapPath;
         return DiffPathIndex(root, path)
            .Concat(DiffIndexPath(root, path));
      }

      private IEnumerable<DiffEntry> DiffPathIndex (Backup.Node parentNode, IO.Path parentPath)
      {
         IList<Backup.Node> nodes = this.Archive.BackupIndex.ListNodes(parentNode).ToList();
         foreach (IO.FileSystem.Metadata metadata in IO.FileSystem.Children(parentPath))
         {
            this.Canceler.ThrowIfCancellationRequested();
            if (!metadata.IsSystem)
            {
               Backup.Node node = nodes.FirstOrDefault(
                  n => String.Compare(n.Name, metadata.Name, true) == 0
               );
               if (node == null)
                  yield return new DiffEntry()
                  {
                     Type = DiffType.New,
                     Node = node = new Backup.Node()
                     {
                        Parent = parentNode,
                        Type = metadata.IsDirectory ?
                           Backup.NodeType.Directory :
                           Backup.NodeType.File,
                        Name = metadata.Name
                     }
                  };
               if (metadata.IsDirectory)
                  foreach (DiffEntry diff in DiffPathIndex(node, metadata.Path))
                     yield return diff;
            }
         }
      }

      private IEnumerable<DiffEntry> DiffIndexPath (Backup.Node parentNode, IO.Path parentPath)
      {
         foreach (Backup.Node node in this.Archive.BackupIndex.ListNodes(parentNode))
         {
            this.Canceler.ThrowIfCancellationRequested();
            IO.Path path = parentPath + node.Name;
            IO.FileSystem.Metadata metadata = IO.FileSystem.GetMetadata(path);
            if (node.Type == Backup.NodeType.Directory)
            {
               if (!metadata.Exists || !metadata.IsSystem)
                  foreach (DiffEntry diff in DiffIndexPath(node, path))
                     yield return diff;
            }
            else
            {
               Backup.Entry entry = this.Archive.BackupIndex
                  .ListNodeEntries(node)
                  .OrderBy(e => e.Session.Created)
                  .LastOrDefault();
               if (!metadata.Exists || metadata.IsSystem)
               {
                  if (entry != null && entry.State != Backup.EntryState.Deleted)
                     yield return new DiffEntry()
                     {
                        Type = DiffType.Deleted,
                        Node = node
                     };
               }
               else if (entry == null || entry.State == Backup.EntryState.Deleted)
               {
                  yield return new DiffEntry()
                  {
                     Type = DiffType.New,
                     Node = node
                  };
               }
               else if (entry != null && entry.State != Backup.EntryState.Pending)
               {
                  Boolean isChanged = true;
                  switch (this.Request.Method)
                  {
                     case DiffMethod.Timestamp:
                        if (metadata.Updated < entry.Session.Created)
                           isChanged = false;
                        break;
                     case DiffMethod.Digest:
                        if (IO.Crc32Filter.Calculate(path.ToString()) == entry.Crc32)
                           isChanged = false;
                        break;
                     default:
                        throw new InvalidOperationException("TODO");
                  }
                  if (isChanged)
                     yield return new DiffEntry()
                     {
                        Type = DiffType.Changed,
                        Node = node
                     };
               }
            }
         }
      }
   }
}
