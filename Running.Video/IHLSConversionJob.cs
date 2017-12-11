using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Running.Video
{
  public interface IHLSConversionJob
  {
    void SetSource(string arquivo);
    SourceInfo SourceInfo { get; }
    void StartConversion(string destinationFolder);
    ConversionStateEnum State { get; }
    event EventHandler<ConversionProgressEventArgs> OnProgress;
    event EventHandler<ConversionCompletedEventArgs> OnComplete;
  }
}
