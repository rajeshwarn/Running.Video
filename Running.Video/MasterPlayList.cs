using System.Collections.Generic;

namespace Running.Video
{
  public class MasterPlayList
  {
    public string Path { get; set; }

    public List<PlayList> AvaiablePlayLists { get; set; } = new List<PlayList>();
  }
}