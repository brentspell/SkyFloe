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
         foreach (String dir in Directory.GetDirectories(this.Path))
            yield return System.IO.Path.GetFileName(dir);
      }
      public Store.IArchive CreateArchive (String name, Backup.Header header)
      {
         FileSystemArchive archive = new FileSystemArchive(
            System.IO.Path.Combine(this.Path, name)
         );
         archive.Create(header);
         return archive;
      }
      public Store.IArchive OpenArchive (String name)
      {
         FileSystemArchive archive = new FileSystemArchive(
            System.IO.Path.Combine(this.Path, name)
         );
         archive.Open();
         return archive;
      }
      public void DeleteArchive (String name)
      {
         String path = System.IO.Path.Combine(this.Path, name);
         if (Directory.Exists(path))
            Directory.Delete(path, true);
      }
      #endregion
   }
}
