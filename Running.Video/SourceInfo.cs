using System;

namespace Running.Video
{
  public class SourceInfo
  {
    public TimeSpan Duration { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public long FrameCount { get; set; }
  }
}