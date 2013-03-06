//===========================================================================
// MODULE:  Node.cs
// PURPOSE: backup index file node record type
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

namespace SkyFloe.Backup
{
   /// <summary>
   /// Backup node type
   /// </summary>
   public enum NodeType
   {
      Root = 1,         // backup source root directory (name = full path)
      Directory = 2,    // backup source subdirectory (internal node)
      File = 3          // backup source file (leaf node)
   }

   /// <summary>
   /// The file node record type
   /// </summary>
   /// <remarks>
   /// This class maintains the backup source directory tree in the backup
   /// index. The tree structure minimizes the storage of file path
   /// information in the backup and provides for efficient display of
   /// the file system hierarchy during restore. Nodes are stored using
   /// parent foreign keys (instead of nested sets, for example) to support
   /// efficient tree modifications when there are multiple backup sessions,
   /// since a single directory structure is shared across sessions.
   /// </remarks>
   public class Node
   {
      /// <summary>
      /// Record primary key
      /// </summary>
      public Int32 ID { get; set; }
      /// <summary>
      /// Parent node (null if root)
      /// </summary>
      public Node Parent { get; set; }
      /// <summary>
      /// Node type indicator
      /// </summary>
      public NodeType Type { get; set; }
      /// <summary>
      /// Node name
      /// - full source path for root nodes
      /// - directory/file name for non-roots
      /// </summary>
      public String Name { get; set; }

      /// <summary>
      /// Traverses the node tree up to the root of the current node
      /// </summary>
      /// <returns>
      /// The current node's root (or this if at the root)
      /// </returns>
      public Node GetRoot ()
      {
         var node = this;
         while (node.Parent != null)
            node = node.Parent;
         return node;
      }
      /// <summary>
      /// Constructs the full string path to the current node
      /// </summary>
      /// <returns>
      /// The path to the current node from the root
      /// </returns>
      public IO.Path GetAbsolutePath ()
      {
         return (this.Parent != null) ?
            this.Parent.GetAbsolutePath() + this.Name :
            (IO.Path)this.Name;
      }
      /// <summary>
      /// Constructs the string path to the current node, excluding the root
      /// </summary>
      /// <returns>
      /// The path to the current node from the root, excluding
      /// the root path
      /// </returns>
      public String GetRelativePath ()
      {
         if (this.Parent == null)
            return String.Empty;
         var parentPath = this.Parent.GetRelativePath();
         if (!String.IsNullOrEmpty(parentPath))
            parentPath += IO.Path.Separator;
         return parentPath + this.Name;
      }
      /// <summary>
      /// Compares the node name
      /// </summary>
      /// <param name="name">
      /// name to compare
      /// </param>
      /// <returns>
      /// True if the names match (case insensitive)
      /// False otherwise
      /// </returns>
      public Boolean NameEquals (String name)
      {
         return StringComparer.OrdinalIgnoreCase.Equals(this.Name, name);
      }
   }
}
