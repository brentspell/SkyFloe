using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkyFloe.Restore
{
   public class PathMap
   {
      public Int32 ID { get; set; }
      public Session Session { get; set; }
      public Int32 NodeID { get; set; }
      public IO.Path Path { get; set; }
   }
}
