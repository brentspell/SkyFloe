using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyFloe.Backup
{
   public class Blob
   {
      public Int32 ID { get; set; }
      public String Name { get; set; }
      public Int64 Length { get; set; }
      public DateTime Created { get; set; }
      public DateTime Updated { get; set; }
   }
}
