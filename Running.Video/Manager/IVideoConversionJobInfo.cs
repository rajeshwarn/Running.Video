using Running.Video;
using System;
namespace Running.Video.Manager
{
  public interface IVideoConversionJobInfo
  {
    string Id { get; set; }
    string FilePath { get; set; }
    SourceInfo SourceInfo { get; set; }
    ConversionStatusEnum Status { get; set; }
    DateTime StartedAt { get; set; }
    DateTime FinishedAt { get; set; }
    TimeSpan Duration { get; set; }
    MasterPlayList MasterPlayList { get; set; }
    decimal Progress { get; set; }

  }
}