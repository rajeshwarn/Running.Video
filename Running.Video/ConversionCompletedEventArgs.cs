using System;
using System.Collections.Generic;

namespace Running.Video
{
  public class ConversionCompletedEventArgs
  {
    public ConversionCompletedEventArgs(ConversionStateEnum state)
    {
      Status = state;
    }
    public ConversionStateEnum Status { get; private set; }
    public MasterPlayList MasterPlayList { get; set; } = new MasterPlayList();
    public TimeSpan Duration { get; set; }
    
  }
}