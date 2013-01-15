using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyFloe.Model
{
   public enum EntryState
   {
      Pending = 1,
      Completed = 2,
      Deleted = 3,
      Failed = 4
   }

   public class Entry
   {
      public Int32 ID { get; set; }
      public Session Session { get; set; }
      public Node Node { get; set; }
      public Blob Blob { get; set; }
      public EntryState State { get; set; }
      public Int64 Offset { get; set; }
      public Int64 Length { get; set; }
      [CLSCompliant(false)]
      public UInt32 Crc32 { get; set; }
   }
}
