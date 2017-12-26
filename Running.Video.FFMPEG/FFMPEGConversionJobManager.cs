using Running.Video.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Running.Video.FFMPEG
{
  public  class FFMPEGConversionJobManager<IRepo>: 
    VideoConversionManager<BasicVideoConverionJobInfo,HLSConversionJob, IRepo>
    where IRepo: class, IVideoConversionQueueRepository<BasicVideoConverionJobInfo>,  new()
  {
    protected FFMPEGConversionJobManager(string poolId):base(poolId)
    {
      
    }
  }
}
