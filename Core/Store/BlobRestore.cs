using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkyFloe.Store
{
   public class BlobRestore
   {
      public Model.Blob Blob { get; set; }
      public Int64 Offset { get; set; }
      public Int64 Length { get; set; }
   }
}
