//===========================================================================
// MODULE:  GlacierDownloader.cs
// PURPOSE: AWS glacier download utility
// 
// Copyright © 2013
// Brent M. Spell. All rights reserved.
//
// This library is free software; you can redistribute it and/or modify it 
// under the terms of the GNU Lesser General Public License as published 
// by the Free Software Foundation; either version 3 of the License, or 
// (at your option) any later version. This library is distributed in the 
// hope that it will be useful, but WITHOUT ANY WARRANTY; without even the 
// implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
// See the GNU Lesser General Public License for more details. You should 
// have received a copy of the GNU Lesser General Public License along with 
// this library; if not, write to 
//    Free Software Foundation, Inc. 
//    51 Franklin Street, Fifth Floor 
//    Boston, MA 02110-1301 USA
//===========================================================================
// System References
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Amazon.Glacier;
using Amazon.Glacier.Model;
// Project References

namespace SkyFloe.Aws
{
   /// <summary>
   /// Glacier job status enum, returned from QueryJob()
   /// </summary>
   public enum JobStatus
   {
      InProgress,       // job is still in progress
      Completed,        // job is complete and ready for download
      Failed            // job was not found or has failed at AWS somehow
   }

   /// <summary>
   /// Glacier vault archive downloader
   /// </summary>
   /// <remarks>
   /// This class manages a list of retrieval jobs for downloading vault 
   /// archives from Glacier. Glacier jobs take a long time to execute 
   /// (currently 4+ hours), so this class provides the ability to poll the 
   /// status of a retrieval job periodically to determine when it can be 
   /// downloaded.
   /// </remarks>
   public class GlacierDownloader : IDisposable
   {
      private AmazonGlacierClient glacier;
      private String vault;
      private Dictionary<String, Stream> jobStreams;
      
      /// <summary>
      /// Initializes a new downloader instance
      /// </summary>
      /// <param name="glacier">
      /// The Glacier client interface
      /// </param>
      /// <param name="vault">
      /// The current Glacier vault name
      /// </param>
      public GlacierDownloader (
         AmazonGlacierClient glacier,
         String vault)
      {
         this.glacier = glacier;
         this.vault = vault;
         this.jobStreams = new Dictionary<String, Stream>();
      }
      /// <summary>
      /// Releases the resources associated with the downloader
      /// </summary>
      public void Dispose ()
      {
         var streams = new List<Stream>(this.jobStreams.Values);
         this.jobStreams.Clear();
         foreach (var stream in streams)
            stream.Dispose();
      }
      /// <summary>
      /// Initiates a new Glacier retrieval job
      /// </summary>
      /// <param name="archiveID">
      /// The Glacier vault archive identifier
      /// </param>
      /// <param name="offset">
      /// The offset, in bytes, of the vault archive to retrieve
      /// </param>
      /// <param name="length">
      /// The number of bytes to retrieve from the vault archive
      /// </param>
      /// <returns>
      /// The new retrieval job identifier
      /// </returns>
      public String StartJob (String archiveID, Int64 offset, Int64 length)
      {
         return this.glacier.InitiateJob(
            new InitiateJobRequest()
            {
               VaultName = this.vault,
               JobParameters = new JobParameters()
               {
                  Type = "archive-retrieval",
                  ArchiveId = archiveID,
                  RetrievalByteRange = String.Format(
                     "{0}-{1}",
                     offset,
                     offset + length - 1
                  )
               }
            }
         ).InitiateJobResult.JobId;
      }
      /// <summary>
      /// Polls the status of a Glacier retrieval job
      /// </summary>
      /// <param name="jobID">
      /// The retrieval job identifier
      /// </param>
      /// <returns>
      /// The current status of the job
      /// </returns>
      public JobStatus QueryJob (String jobID)
      {
         // if we have already started downloading the job, it is completed
         if (this.jobStreams.ContainsKey(jobID))
            return JobStatus.Completed;
         // request the job status from AWS
         try
         {
            var jobInfo = this.glacier.DescribeJob(
               new DescribeJobRequest()
               {
                  VaultName = this.vault,
                  JobId = jobID
               }
            ).DescribeJobResult;
            if (jobInfo.Completed)
            {
               // if the job has completed, create a new GlacierStream
               // and add it to the completed job mapping
               // experiments show that AWS streams are chatty and return 
               // data as soon as it reaches the socket, so buffer it for
               // more efficient stream processing
               var range = jobInfo.RetrievalByteRange;
               var start = range.Substring(0, range.IndexOf('-'));
               var stop = range.Substring(range.IndexOf('-') + 1);
               var length = Convert.ToInt64(stop) - Convert.ToInt64(start) + 1;
               this.jobStreams.Add(
                  jobID,
                  new GlacierStream(this.glacier, this.vault, jobID, length)
               );
               return JobStatus.Completed;
            }
            if (StringComparer.OrdinalIgnoreCase.Equals(jobInfo.StatusCode, "InProgress"))
               return JobStatus.InProgress;
            return JobStatus.Failed;
         }
         catch (ResourceNotFoundException)
         {
            // if the job was not found, assume AWS expired it,
            // so that it can be resubmitted
            return JobStatus.Failed;
         }
      }
      /// <summary>
      /// Releases a job stream that is no longer needed
      /// </summary>
      /// <param name="jobID">
      /// The retrieval job identifier
      /// </param>
      public void DeleteJob (String jobID)
      {
         var jobStream = (Stream)null;
         if (this.jobStreams.TryGetValue(jobID, out jobStream))
         {
            this.jobStreams.Remove(jobID);
            jobStream.Dispose();
         }
      }
      /// <summary>
      /// Gets a job stream subset from the mapping
      /// </summary>
      /// <param name="jobID">
      /// The retrieval job identifier
      /// </param>
      /// <param name="offset">
      /// The offset of the substream
      /// </param>
      /// <param name="length">
      /// The number of bytes of the substream
      /// </param>
      /// <returns>
      /// The new substream
      /// </returns>
      public Stream GetJobStream (String jobID, Int64 offset, Int64 length)
      {
         var stream = (Stream)null;
         if (!this.jobStreams.TryGetValue(jobID, out stream))
            throw new InvalidOperationException();
         try
         {
            return new BufferedStream(new IO.Substream(stream, offset, length));
         }
         catch
         {
            this.jobStreams.Remove(jobID);
            stream.Dispose();
            throw;
         }
      }
   }
}
