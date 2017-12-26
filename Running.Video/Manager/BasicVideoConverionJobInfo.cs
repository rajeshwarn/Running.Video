using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Running.Video.Manager
{
  public class BasicVideoConverionJobInfo : IVideoConversionJobInfo
  {
    public BasicVideoConverionJobInfo(string videoFilePath,string id)
    {
      FilePath = videoFilePath;
      Id = id;
    }

    public TimeSpan Duration
    {
      get; set;
    }

    public string FilePath
    {
      get; set;
    }

    public DateTime FinishedAt
    {
      get; set;
    }

    public string Id
    {
      get; set;
    }

    public MasterPlayList MasterPlayList
    {
      get; set;
    }

    public decimal Progress
    {
      get; set;
    }

    public SourceInfo SourceInfo
    {
      get; set;
    }

    public DateTime StartedAt
    {
      get; set;
    }
    ConversionStatusEnum _status = ConversionStatusEnum.NotStarted;
    public ConversionStatusEnum Status
    {
      get {
        return _status;
      }
        
      set {
        _status = value;
      }
    }
  }
}
