using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace SkyFloe
{
   public class FileSystemStore : Store.IStore
   {
      #region Connection Properties
      [Required]
      public String Path { get; set; }
      #endregion

      #region IStore Implementation
      public void Dispose ()
      {
      }
      public void Open ()
      {
      }
      public IEnumerable<String> ListArchives ()
      {
         return IO.FileSystem.Children(this.Path).Select(p => p.Name);
      }
      public Store.IArchive CreateArchive (String name, Backup.Header header)
      {
         FileSystemArchive archive = new FileSystemArchive((IO.Path)this.Path + name);
         archive.Create(header);
         return archive;
      }
      public Store.IArchive OpenArchive (String name)
      {
         FileSystemArchive archive = new FileSystemArchive((IO.Path)this.Path + name);
         archive.Open();
         return archive;
      }
      public void DeleteArchive (String name)
      {
         IO.FileSystem.Delete((IO.Path)this.Path + name);
      }
      #endregion
   }
}
