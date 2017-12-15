using System.Collections.Generic;

namespace Running.Video
{
  public class PlayList
  {
    public string Path { get; set; }
    public List<MediaSegment> Segments { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
  }
}