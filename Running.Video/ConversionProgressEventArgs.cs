using System;

namespace Running.Video
{
  public class ConversionProgressEventArgs
  {

    public ConversionProgressEventArgs(
      decimal progress, 
      TimeSpan elapsedTime
    )
    {
      Progress = progress;
      ElapsedTime = elapsedTime;
    }
    public TimeSpan ElapsedTime { get; private set; }
    public decimal Progress { get; private set; }
  }
}