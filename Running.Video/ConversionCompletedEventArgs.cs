namespace Running.Video
{
  public class ConversionCompletedEventArgs
  {
    public ConversionCompletedEventArgs(ConversionStateEnum state)
    {
      Status = state;
    }
    public ConversionStateEnum Status { get; private set; }
  }
}