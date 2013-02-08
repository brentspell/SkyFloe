using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyFloe.Restore
{
   public enum SessionState
   {
      Pending = 1,
      InProgress = 2,
      Completed = 3
   }

   [Flags]
   public enum SessionFlags
   {
      SkipExisting = 1,
      SkipReadOnly = 2,
      VerifyResults = 4,
      EnableDeletes = 8
   }

   public class Session
   {
      public Int32 ID { get; set; }
      public SessionState State { get; set; }
      public SessionFlags Flags { get; set; }
      public DateTime Created { get; set; }

      public Boolean SkipExisting
      { 
         get { return this.Flags.HasFlag(SessionFlags.SkipExisting); }
      }
      public Boolean SkipReadOnly
      {
         get { return this.Flags.HasFlag(SessionFlags.SkipReadOnly); }
      }
      public Boolean VerifyResults
      {
         get { return this.Flags.HasFlag(SessionFlags.VerifyResults); }
      }
      public Boolean EnableDeletes
      {
         get { return this.Flags.HasFlag(SessionFlags.EnableDeletes); }
      }
   }
}
