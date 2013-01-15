using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkyFloe
{
   public class FileStore : Store.IStore
   {
      #region Connection Properties
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
         foreach (var dir in Directory.GetDirectories(this.Path))
            yield return System.IO.Path.GetFileName(dir);
      }
      public Store.IArchive CreateArchive (String name, Model.Header header)
      {
         var archive = new FileArchive()
         {
            Path = System.IO.Path.Combine(this.Path, name)
         };
         archive.Create(header);
         return archive;
      }
      public Store.IArchive OpenArchive (String name)
      {
         var archive = new FileArchive()
         {
            Path = System.IO.Path.Combine(this.Path, name)
         };
         archive.Open();
         return archive;
      }
      public void DeleteArchive (String name)
      {
         var path = System.IO.Path.Combine(this.Path, name);
         if (Directory.Exists(path))
            Directory.Delete(path, true);
      }
      #endregion
   }
}
