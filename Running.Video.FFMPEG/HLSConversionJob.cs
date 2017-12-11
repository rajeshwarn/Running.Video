using EmergenceGuardian.FFmpeg;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Attributes;

namespace Running.Video.FFMPEG
{
  public class HLSConversionJob : IHLSConversionJob
  {
    const string FFMPEG_PATH_KEY = "ffmpeg_path";
    string _source = null;
    bool _sourceOK = false;
    ConversionStateEnum _state = ConversionStateEnum.NaoIniciado;
    FFmpegProcess fi;
    TimeLeftCalculator _timeLeftCalc = null;//= //new TimeLeftCalculator
    FFmpegProcess _mediaInfo = null;
    SourceInfo _sourceInfo;
    long _startTicks;
    public event EventHandler<ConversionProgressEventArgs> OnProgress;
    public event EventHandler<ConversionCompletedEventArgs> OnComplete;


    //[InjectionConstructor]
    public HLSConversionJob()
    {
      var FFMPEGPath = ConfigurationManager.AppSettings[FFMPEG_PATH_KEY];
      if (string.IsNullOrWhiteSpace(FFMPEGPath))
      {
        throw new ConfigurationErrorsException($"Você precisa definir a chave {FFMPEG_PATH_KEY} com o caminho do executável do ffmpeg.");
      }
      _Init(FFMPEGPath);
    }

    
    public HLSConversionJob(string ffmpegPath)
    {
      _Init(ffmpegPath);
    }


    protected void _Init(string ffmpegPath)
    {
      if (!File.Exists(ffmpegPath))
      {
        throw new FileNotFoundException($"FFMPEG não encontrado em: {ffmpegPath}") { };
      }
      FFmpegConfig.FFmpegPath = ffmpegPath;
    }

    public SourceInfo SourceInfo
    {
      get
      {
        if(!_sourceOK)
          throw new InvalidOperationException($@"A origem não parece correta: {_source}");
        return _sourceInfo;
      }
    }
    
    public ConversionStateEnum State
    {
      get { return _state; }
      private set { _state = value; }
    }

    public void SetSource(string arquivo)
    {
      _sourceOK = false;
      _source = string.Empty;
      if (!File.Exists(arquivo))
      {
        throw new FileNotFoundException($"Arquivo não encontrado: {arquivo}", arquivo);
      }

      _mediaInfo = MediaInfo.GetFileInfo(_source);

      _sourceInfo = new SourceInfo
      {
        Duration = _mediaInfo.FileDuration,
        Width = _mediaInfo.VideoStream.Width,
        Height = _mediaInfo.VideoStream.Height,
        FrameCount = _mediaInfo.FrameCount
      };

      _source = arquivo;
      _sourceOK = true;
    }
    
    public void StartConversion(string destinationFolder)
    {

      var geralFile = 
@"#EXTM3U
#EXT-X-STREAM-INF:PROGRAM-ID=1,BANDWIDTH=400000
hls/index1.m3u8
#EXT-X-STREAM-INF:PROGRAM-ID=1,BANDWIDTH=700000
hls/index2.m3u8";

      _timeLeftCalc = new TimeLeftCalculator(MediaInfo.GetFrameCount(_source),200);
      
      var dest320x180 = Path.Combine(destinationFolder, "320x180");
      if (!Directory.Exists(dest320x180))
        Directory.CreateDirectory(dest320x180); // deixa deisparar aqui se não for possível criar

      var dest480x270 = Path.Combine(destinationFolder, "480x270");
      if (!Directory.Exists(dest480x270))
        Directory.CreateDirectory(dest480x270);

      var dest640x360 = Path.Combine(destinationFolder, "640x360");
      if (!Directory.Exists(dest640x360))
        Directory.CreateDirectory(dest640x360);

      var dest1280x720 = Path.Combine(destinationFolder, "1280x720");
      if (!Directory.Exists(dest1280x720))
        Directory.CreateDirectory(dest1280x720);


      fi = new FFmpegProcess(
        //new ProcessStartOptions {  }  
      );
      fi.Completed += Fi_Completed;
      fi.StatusUpdated += Fi_StatusUpdated;
      fi.InfoUpdated += Fi_InfoUpdated;

      var cmd = $"-i \"{_source}\" " + // arquivo de entrada
        $" -c:a aac -strict experimental -ac 2 -b:a 64k -ar 44100 -c:v libx264 -pix_fmt yuv420p -profile:v baseline -level 1.3 -maxrate 192K -bufsize 1M -crf 18 -r 10 -g 30 -f hls -hls_time 9 -hls_list_size 0 -s 320x180 -hls_segment_filename \"{dest320x180}\\%03d.ts\" \"{dest320x180}\\index.m3u8\"" +
        $" -c:a aac -strict experimental -ac 2 -b:a 64k -ar 44100 -c:v libx264 -pix_fmt yuv420p -profile:v baseline -level 2.1 -maxrate 500K -bufsize 2M -crf 18 -r 10 -g 30  -f hls -hls_time 9 -hls_list_size 0 -s 480x270 -hls_segment_filename \"{dest480x270}\\%03d.ts\" \"{dest480x270}\\index.m3u8\"" +
        $" -c:a aac -strict experimental -ac 2 -b:a 96k -ar 44100 -c:v libx264 -pix_fmt yuv420p -profile:v baseline -level 3.1 -maxrate 1M -bufsize 3M -crf 18 -r 24 -g 72 -f hls -hls_time 9 -hls_list_size 0 -s 640x360 -hls_segment_filename \"{dest640x360}\\%03d.ts\" \"{dest640x360}\\index.m3u8\"" +
        $" -c:a aac -strict experimental -ac 2 -b:a 96k -ar 44100 -c:v libx264 -pix_fmt yuv420p -profile:v main -level 3.2 -maxrate 2M -bufsize 6M -crf 18 -r 24 -g 72 -f hls -hls_time 9 -hls_list_size 0 -s 1280x720  -hls_segment_filename \"{dest1280x720}\\%03d.ts\" \"{dest1280x720}\\index.m3u8\"";
      fi.RunFFmpeg(cmd);
      _startTicks = DateTime.Now.Ticks;
      State = ConversionStateEnum.Iniciado;
      //var.
      //throw new NotImplementedException();
    }

    private void Fi_InfoUpdated(object sender, EventArgs e)
    {
      //throw new NotImplementedException();
    }
    private TimeSpan ElapsedTime {
        get { return TimeSpan.FromTicks(DateTime.Now.Ticks - _startTicks); }
    }
    private void Fi_StatusUpdated(object sender, StatusUpdatedEventArgs e)
    {
      var tempoEstimado =  _timeLeftCalc.Calculate(e.Status.Frame);
      var porcentagemConclusao = e.Status.Frame / _mediaInfo.FrameCount;
      //throw new NotImplementedException();
      OnProgress?.Invoke(this, new ConversionProgressEventArgs(tempoEstimado,porcentagemConclusao, ElapsedTime) );
    }

    private void Fi_Completed(object sender, CompletedEventArgs e)
    {
      switch (e.Status)
      {


      } 
      _state = ConversionStateEnum.Completo;
      //throw new NotImplementedException();
    }
  }
}
