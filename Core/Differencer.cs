using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyFloe
{
   public class Differencer
   {
      public DiffMethod Method { get; set; }
      public Store.IBackupIndex Index { get; set; }
      public Backup.Node Root { get; set; }
      public IO.Path Path { get; set; }

      public IEnumerable<Diff> Enumerate ()
      {
         return DiffPathIndex(this.Root, this.Path)
            .Concat(DiffIndexPath(this.Root, this.Path));
      }

      private IEnumerable<Diff> DiffPathIndex (Backup.Node parentNode, IO.Path parentPath)
      {
         IList<Backup.Node> nodes = this.Index.ListNodes(parentNode).ToList();
         foreach (IO.FileSystem.Metadata metadata in IO.FileSystem.Children(parentPath))
         {
            if (!metadata.IsSystem)
            {
               Backup.Node node = nodes.FirstOrDefault(
                  n => String.Compare(n.Name, metadata.Name, true) == 0
               );
               if (node == null)
                  yield return new Diff()
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
                  foreach (Diff diff in DiffPathIndex(node, metadata.Path))
                     yield return diff;
            }
         }
      }

      private IEnumerable<Diff> DiffIndexPath (Backup.Node parentNode, IO.Path parentPath)
      {
         foreach (Backup.Node node in this.Index.ListNodes(parentNode))
         {
            IO.Path path = parentPath + node.Name;
            IO.FileSystem.Metadata metadata = IO.FileSystem.GetMetadata(path);
            if (node.Type == Backup.NodeType.Directory)
            {
               if (!metadata.Exists || !metadata.IsSystem)
                  foreach (Diff diff in DiffIndexPath(node, path))
                     yield return diff;
            }
            else
            {
               Backup.Entry entry = this.Index
                  .ListNodeEntries(node)
                  .OrderBy(e => e.Session.Created)
                  .LastOrDefault();
               if (!metadata.Exists || metadata.IsSystem)
               {
                  if (entry != null && entry.State != Backup.EntryState.Deleted)
                     yield return new Diff()
                     {
                        Type = DiffType.Deleted,
                        Node = node
                     };
               }
               else if (entry == null || entry.State == Backup.EntryState.Deleted)
               {
                  yield return new Diff()
                  {
                     Type = DiffType.New,
                     Node = node
                  };
               }
               else if (entry != null && entry.State != Backup.EntryState.Pending)
               {
                  Boolean isChanged = true;
                  switch (this.Method)
                  {
                     case DiffMethod.Timestamp:
                        if (metadata.Updated < entry.Session.Created)
                           isChanged = false;
                        break;
                     case DiffMethod.Digest:
                        if (IO.Crc32Stream.Calculate(path) == entry.Crc32)
                           isChanged = false;
                        break;
                     default:
                        throw new InvalidOperationException("TODO");
                  }
                  if (isChanged)
                     yield return new Diff()
                     {
                        Type = DiffType.Changed,
                        Node = node
                     };
               }
            }
         }
      }

      public class Diff
      {
         public DiffType Type { get; set; }
         public Backup.Node Node { get; set; }
      }
   }
}
