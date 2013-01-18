using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkyFloe.Restore
{
   public class Retrieval
   {
      public Int32 ID { get; set; }
      public String Name { get; set; }
      public Int32 BlobID { get; set; }
      public Int64 Offset { get; set; }
      public Int64 Length { get; set; }
   }
}
