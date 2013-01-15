using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkyFloe
{
   public class Differencer
   {
      public DiffMethod Method { get; set; }
      public Store.IIndex Index { get; set; }
      public Model.Node Root { get; set; }
      public String Path { get; set; }

      public IEnumerable<Diff> Enumerate ()
      {
         return DiffPathIndex(this.Root, this.Path)
            .Concat(DiffIndexPath(this.Root, this.Path));
      }

      private IEnumerable<Diff> DiffPathIndex (Model.Node parentNode, String parentPath)
      {
         var nodes = this.Index.ListNodes(parentNode).ToList();
         foreach (var path in Directory.EnumerateFileSystemEntries(parentPath))
         {
            if (!File.GetAttributes(path).HasFlag(FileAttributes.System))
            {
               var name = System.IO.Path.GetFileName(path);
               var node = nodes.FirstOrDefault(
                  n => String.Compare(n.Name, name, true) == 0
               );
               if (node == null)
                  yield return new Diff()
                  {
                     Type = DiffType.New,
                     Node = node = new Model.Node()
                     {
                        Parent = parentNode,
                        Type = (Directory.Exists(path)) ?
                           Model.NodeType.Directory :
                           Model.NodeType.File,
                        Name = name
                     }
                  };
               if (node.Type == Model.NodeType.Directory)
                  foreach (var diff in DiffPathIndex(node, path))
                     yield return diff;
            }
         }
      }

      private IEnumerable<Diff> DiffIndexPath (Model.Node parentNode, String parentPath)
      {
         foreach (var node in this.Index.ListNodes(parentNode))
         {
            var path = System.IO.Path.Combine(parentPath, node.Name);
            if (!File.GetAttributes(path).HasFlag(FileAttributes.System))
            {
               if (node.Type == Model.NodeType.Directory)
                  foreach (var diff in DiffIndexPath(node, path))
                     yield return diff;
               else
               {
                  var entry = this.Index
                     .ListNodeEntries(node)
                     .OrderBy(e => e.Session.Created)
                     .LastOrDefault();
                  if (!File.Exists(path))
                  {
                     if (entry != null && entry.State != Model.EntryState.Deleted)
                        yield return new Diff()
                        {
                           Type = DiffType.Deleted,
                           Node = node
                        };
                  }
                  else if (entry == null || entry.State == Model.EntryState.Deleted)
                  {
                     yield return new Diff()
                     {
                        Type = DiffType.New,
                        Node = node
                     };
                  }
                  else if (entry != null && entry.State != Model.EntryState.Pending)
                  {
                     var isChanged = true;
                     switch (this.Method)
                     {
                        case DiffMethod.Timestamp:
                           if (File.GetLastWriteTimeUtc(path) < entry.Session.Created)
                              isChanged = false;
                           break;
                        case DiffMethod.Digest:
                           if (IO.Crc32Stream.Calculate(new FileInfo(path)) == entry.Crc32)
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
      }

      public class Diff
      {
         public DiffType Type { get; set; }
         public Model.Node Node { get; set; }
      }
   }
}
