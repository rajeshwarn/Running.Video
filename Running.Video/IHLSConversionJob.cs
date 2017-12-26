using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Running.Video
{
  public interface IHLSConversionJob
  {
    SourceInfo SetSource(string arquivo);
    SourceInfo SourceInfo { get; }
    string StartConversion(string destinationFolder);
    ConversionStatusEnum State { get; }
    object Tag { get; set; }
    event EventHandler<ConversionProgressEventArgs> OnProgress;
    event EventHandler<ConversionCompletedEventArgs> OnComplete;

  }
}
