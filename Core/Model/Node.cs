using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyFloe.Model
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
         var node = this;
         while (node.Parent != null)
            node = node.Parent;
         return node;
      }
      public String GetAbsolutePath ()
      {
         return (this.Parent != null) ? 
            System.IO.Path.Combine(this.Parent.GetAbsolutePath(), this.Name) :
            this.Name;
      }
      public String GetRelativePath ()
      {
         return (this.Parent != null) ?
            System.IO.Path.Combine(this.Parent.GetRelativePath(), this.Name) :
            "";
      }
   }
}
