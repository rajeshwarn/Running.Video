using Running.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Running.Video.Manager
{
  public interface IVideoConversionQueueRepository<VideoJobClass> where VideoJobClass : IVideoConversionJobInfo
  {
    string TempDir { get; set; }
    string OutDir { get; set; }

    void OnJobEnqueued(IVideoConversionJobInfo job);
    void OnConversionStarted(IVideoConversionJobInfo job);
    void OnConversionUpdateProgress(IVideoConversionJobInfo job);
    void OnConversionFinished(IVideoConversionJobInfo job);

    IEnumerable<VideoJobClass> ListJobs();
  }
}