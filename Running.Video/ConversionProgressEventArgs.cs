using System;

namespace Running.Video
{
  public class ConversionProgressEventArgs
  {

    public ConversionProgressEventArgs(TimeSpan timeLeft, double progress, TimeSpan elapsedTime)
    {
      TimeLeft = timeLeft;
      Progress = progress;
      ElapsedTime = elapsedTime;
    }
    public TimeSpan ElapsedTime { get; private set; }
    public TimeSpan TimeLeft { get; private set; }
    public double Progress { get; private set; }
    //public string Description { get; set; }
  }
}