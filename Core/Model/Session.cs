using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyFloe.Model
{
   public enum SessionState
   {
      Pending = 1,
      InProgress = 2,
      Completed = 3
   }

   public class Session
   {
      public Int32 ID { get; set; }
      public SessionState State { get; set; }
      public DateTime Created { get; set; }
      public Int64 EstimatedLength { get; set; }
      public Int64 ActualLength { get; set; }
   }
}
