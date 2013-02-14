using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyFloe.Restore
{
   public enum EntryState
   {
      Pending = 1,
      Completed = 2,
      Failed = 3
   }
   public class Entry
   {
      public Int32 ID { get; set; }
      public Int32 BackupEntryID { get; set; }
      public Session Session { get; set; }
      public Retrieval Retrieval { get; set; }
      public EntryState State { get; set; }
      public Int64 Offset { get; set; }
      public Int64 Length { get; set; }
   }
}
