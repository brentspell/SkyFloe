using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyFloe.Backup
{
   public enum NodeType
   {
      Root = 1,
      Directory = 2,
      File = 3
   }

   public class Node
   {
      public Int32 ID { get; set; }
      public Node Parent { get; set; }
      public NodeType Type { get; set; }
      public String Name { get; set; }

      public Node GetRoot ()
      {
         Node node = this;
         while (node.Parent != null)
            node = node.Parent;
         return node;
      }
      public IO.Path GetAbsolutePath ()
      {
         return (this.Parent != null) ?
            this.Parent.GetAbsolutePath() + this.Name :
            (IO.Path)this.Name;
      }
      public IO.Path GetRelativePath ()
      {
         return (this.Parent != null) ?
            this.Parent.GetRelativePath() + this.Name :
            IO.Path.Empty;
      }
   }
}
