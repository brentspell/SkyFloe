using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyFloe.Tasks
{
   public class Difference : Task
   {
      public DiffRequest Request { get; set; }

      protected override void DoValidate ()
      {
         if (this.Request == null)
            throw new ArgumentException("Request");
         foreach (KeyValuePair<IO.Path, IO.Path> map in this.Request.RootPathMap)
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
      protected override void DoExecute ()
      {
         foreach (Backup.Node root in this.Archive.BackupIndex.ListNodes(null))
            foreach (DiffResult entry in Enumerate(root))
               ReportProgress(
                  new Engine.ProgressEventArgs()
                  {
                     DiffEntry = entry
                  }
               );
      }
      public IEnumerable<DiffResult> Enumerate (Backup.Node root)
      {
         IO.Path path = root.Name;
         IO.Path mapPath = null;
         if (this.Request.RootPathMap.TryGetValue(path, out mapPath))
            path = mapPath;
         return DiffPathIndex(root, path)
            .Concat(DiffIndexPath(root, path));
      }

      private IEnumerable<DiffResult> DiffPathIndex (Backup.Node parentNode, IO.Path parentPath)
      {
         IList<Backup.Node> nodes = this.Archive.BackupIndex.ListNodes(parentNode).ToList();
         IList<IO.FileSystem.Metadata> files = TryExecute(
            "DiffPathIndex", 
            () => IO.FileSystem.Children(parentPath).ToList()
         );
         if (files != null)
         {
            foreach (IO.FileSystem.Metadata metadata in files)
            {
               this.Canceler.ThrowIfCancellationRequested();
               if (!metadata.IsSystem)
               {
                  if (metadata.IsDirectory || this.Request.Filter.Evaluate(metadata.Path))
                  {
                     Backup.Node node = nodes.FirstOrDefault(
                        n => StringComparer.OrdinalIgnoreCase.Equals(n.Name, metadata.Name)
                     );
                     if (node == null)
                        yield return new DiffResult()
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
                        foreach (DiffResult diff in DiffPathIndex(node, metadata.Path))
                           yield return diff;
                  }
               }
            }
         }
      }

      private IEnumerable<DiffResult> DiffIndexPath (Backup.Node parentNode, IO.Path parentPath)
      {
         foreach (Backup.Node node in this.Archive.BackupIndex.ListNodes(parentNode))
         {
            this.Canceler.ThrowIfCancellationRequested();
            IO.Path path = parentPath + node.Name;
            if (node.Type == Backup.NodeType.Directory)
            {
               foreach (DiffResult diff in DiffIndexPath(node, path))
                  yield return diff;
            }
            else
            {
               IO.FileSystem.Metadata metadata = TryExecute(
                  "DiffIndexPath",
                  () => IO.FileSystem.GetMetadata(path),
                  IO.FileSystem.Metadata.Empty
               );
               Backup.Entry entry = this.Archive.BackupIndex
                  .ListNodeEntries(node)
                  .OrderBy(e => e.Session.Created)
                  .LastOrDefault();
               if (!metadata.Exists || metadata.IsSystem || !this.Request.Filter.Evaluate(path))
               {
                  if (entry != null && entry.State != Backup.EntryState.Deleted)
                     yield return new DiffResult()
                     {
                        Type = DiffType.Deleted,
                        Node = node
                     };
               }
               else if (entry == null || entry.State == Backup.EntryState.Deleted)
               {
                  yield return new DiffResult()
                  {
                     Type = DiffType.New,
                     Node = node
                  };
               }
               else if (entry != null && entry.State != Backup.EntryState.Pending)
               {
                  Boolean isChanged = false;
                  switch (this.Request.Method)
                  {
                     case DiffMethod.Timestamp:
                        if (metadata.Updated > entry.Session.Created)
                           isChanged = true;
                        break;
                     case DiffMethod.Digest:
                        isChanged = TryExecute(
                           "CalculateCrc",
                           () => IO.Crc32Filter.Calculate(path.ToString()) != entry.Crc32
                        );
                        break;
                  }
                  if (isChanged)
                     yield return new DiffResult()
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
