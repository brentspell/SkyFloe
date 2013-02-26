using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SkyFloe.Lab
{
   public class Test
   {
      public Int32 ThreadID;
      public Int32 Iteration;
      
      static Test ()
      {
      }
      
      public Test ()
      {
      }

      public void Run ()
      {
         Boolean b = false;
         using (var input = IO.FileSystem.Open((IO.Path)@"c:\save\keeper.xml"))
         using (var encoder = new IO.CompressionStream(input, IO.CompressionMode.Compress))
         using (var output = IO.FileSystem.Truncate((IO.Path)@"c:\encoded.dat"))
            encoder.CopyTo(output);
         using (var input = IO.FileSystem.Open((IO.Path)@"c:\encoded.dat"))
         using (var decoder = new IO.CompressionStream(input, IO.CompressionMode.Decompress))
         using (var output = IO.FileSystem.Truncate((IO.Path)@"c:\decoded.dat"))
            decoder.CopyTo(output);
         b = new FileInfo(@"c:\decoded.dat").Length == new FileInfo(@"c:\save\keeper.xml").Length;
         b = Enumerable.SequenceEqual(File.ReadAllBytes(@"c:\decoded.dat"), File.ReadAllBytes(@"c:\save\keeper.xml"));

         /*
         using (var connect = new Connection("Store=AwsGlacier;AccessKey=AKIAJCSD57AVPC5OL4BQ;SecretKey=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"))
         using (var engine = new Engine() { Connection = connect })
            engine.DeleteArchive("Test");
         */
      }
   }
}
