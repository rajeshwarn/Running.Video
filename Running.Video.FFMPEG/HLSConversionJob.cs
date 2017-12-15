using EmergenceGuardian.FFmpeg;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
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
    const string SEGMENT_MANIFEST_FILE_NAME = "index.m3u8";
    const string MASTER_MANIFEST_FILE_NAME = "master.m3u8";

    string _source = null;
    bool _sourceOK = false;
    ConversionStateEnum _state = ConversionStateEnum.NotStarted;
    FFmpegProcess fi;
    TimeLeftCalculator _timeLeftCalc = null;//= //new TimeLeftCalculator
    FFmpegProcess _mediaInfo = null;
    SourceInfo _sourceInfo;
    long _startTicks;
    long? _endTicks;

    public event EventHandler<ConversionProgressEventArgs> OnProgress;
    public event EventHandler<ConversionCompletedEventArgs> OnComplete;
    string _destinationFolder = string.Empty;

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

    private bool vaiFazer320;
    private bool vaiFazer480;
    private bool vaiFazer640;
    private bool vaiFazer1280;

    public void SetSource(string arquivo)
    {
      _sourceOK = false;
      _source = string.Empty;
      if (!File.Exists(arquivo))
      {
        throw new FileNotFoundException($"Arquivo não encontrado: {arquivo}", arquivo);
      }
      _source = arquivo;
      _mediaInfo = MediaInfo.GetFileInfo(_source);

      _sourceInfo = new SourceInfo
      {
        Duration = _mediaInfo.FileDuration,
        Width = _mediaInfo.VideoStream.Width,
        Height = _mediaInfo.VideoStream.Height,
        FrameCount = _mediaInfo.FrameCount
      };

      vaiFazer320 = _sourceInfo.Width >= 320;
      vaiFazer480 = _sourceInfo.Width >= 480;
      vaiFazer640 = _sourceInfo.Width >= 640;
      vaiFazer1280 = _sourceInfo.Width >= 1280;

      _source = arquivo;
      _sourceOK = true;
    }


    private string GeraManifestoMaster()
    {
      var manifesto =
"#EXTM3U\n" +
(!vaiFazer320 ? string.Empty : $"#EXT-X-STREAM-INF:PROGRAM-ID=1,BANDWIDTH=192000\n320x180/{SEGMENT_MANIFEST_FILE_NAME}") +
(!vaiFazer480 ? string.Empty : $"\n#EXT-X-STREAM-INF:PROGRAM-ID=1,BANDWIDTH=500000\n480x270/{SEGMENT_MANIFEST_FILE_NAME}") +
(!vaiFazer640 ? string.Empty : $"\n#EXT-X-STREAM-INF:PROGRAM-ID=1,BANDWIDTH=1000000\n640x360/{SEGMENT_MANIFEST_FILE_NAME}") +
(!vaiFazer1280 ? string.Empty : $"\n#EXT-X-STREAM-INF:PROGRAM-ID=1,BANDWIDTH=2000000\n1280x720/{SEGMENT_MANIFEST_FILE_NAME}");
      var caminhoArquivo = Path.Combine(_destinationFolder, MASTER_MANIFEST_FILE_NAME);
      File.WriteAllText(caminhoArquivo, manifesto);

      return caminhoArquivo;
    }

    public void StartConversion(string destinationFolder)
    {

      if (State == ConversionStateEnum.InProgress)
        return;

      _startTicks = DateTime.Now.Ticks;
      State = ConversionStateEnum.InProgress;

      _destinationFolder = destinationFolder;
      _timeLeftCalc = new TimeLeftCalculator(SourceInfo.FrameCount,400);
      
      var dest320x180 = Path.Combine(destinationFolder, "320x180");
      if (vaiFazer320 && !Directory.Exists(dest320x180))
        Directory.CreateDirectory(dest320x180); // deixa deisparar aqui se não for possível criar

      var dest480x270 = Path.Combine(destinationFolder, "480x270");
      if (vaiFazer480 && !Directory.Exists(dest480x270))
        Directory.CreateDirectory(dest480x270);

      var dest640x360 = Path.Combine(destinationFolder, "640x360");
      if (vaiFazer640 && !Directory.Exists(dest640x360))
        Directory.CreateDirectory(dest640x360);

      var dest1280x720 = Path.Combine(destinationFolder, "1280x720");
      if (vaiFazer1280 && !Directory.Exists(dest1280x720))
        Directory.CreateDirectory(dest1280x720);


      fi = new FFmpegProcess(
        new ProcessStartOptions { Priority = ProcessPriorityClass.BelowNormal, DisplayMode = FFmpegDisplayMode.None }
      );

      fi.Completed += Fi_Completed;
      fi.StatusUpdated += Fi_StatusUpdated;

      var cmd = $"-i \"{_source}\" " + // arquivo de entrada
        (!vaiFazer320 ? string.Empty : $" -c:a aac -strict experimental -ac 2 -b:a 64k -ar 44100 -c:v libx264 -pix_fmt yuv420p -profile:v baseline -level 1.3 -maxrate 192K -bufsize 1M -crf 18 -r 10 -g 30 -f hls -hls_time 9 -hls_list_size 0 -s 320x180 -hls_segment_filename \"{dest320x180}\\%03d.ts\" \"{dest320x180}\\{SEGMENT_MANIFEST_FILE_NAME}\"") +
        (!vaiFazer480 ? string.Empty : $" -c:a aac -strict experimental -ac 2 -b:a 64k -ar 44100 -c:v libx264 -pix_fmt yuv420p -profile:v baseline -level 2.1 -maxrate 500K -bufsize 2M -crf 18 -r 10 -g 30  -f hls -hls_time 9 -hls_list_size 0 -s 480x270 -hls_segment_filename \"{dest480x270}\\%03d.ts\" \"{dest480x270}\\{SEGMENT_MANIFEST_FILE_NAME}\"") +
        (!vaiFazer640 ? string.Empty : $" -c:a aac -strict experimental -ac 2 -b:a 96k -ar 44100 -c:v libx264 -pix_fmt yuv420p -profile:v baseline -level 3.1 -maxrate 1M -bufsize 3M -crf 18 -r 24 -g 72 -f hls -hls_time 9 -hls_list_size 0 -s 640x360 -hls_segment_filename \"{dest640x360}\\%03d.ts\" \"{dest640x360}\\{SEGMENT_MANIFEST_FILE_NAME}\"" ) +
        (!vaiFazer1280 ? string.Empty : $" -c:a aac -strict experimental -ac 2 -b:a 96k -ar 44100 -c:v libx264 -pix_fmt yuv420p -profile:v main -level 3.2 -maxrate 2M -bufsize 6M -crf 18 -r 24 -g 72 -f hls -hls_time 9 -hls_list_size 0 -s 1280x720  -hls_segment_filename \"{dest1280x720}\\%03d.ts\" \"{dest1280x720}\\{SEGMENT_MANIFEST_FILE_NAME}\"");

      fi.RunFFmpeg(cmd);

    }

    private TimeSpan ElapsedTime {
        get { return TimeSpan.FromTicks((_endTicks??DateTime.Now.Ticks) - _startTicks); }
    }
    private void Fi_StatusUpdated(object sender, StatusUpdatedEventArgs e)
    {
      var tempoEstimado =  _timeLeftCalc.Calculate(e.Status.Frame);
      var porcentagemConclusao = //e.Status.Frame / (decimal)_mediaInfo.FrameCount;
         e.Status.Time.Ticks / (decimal)SourceInfo.Duration.Ticks;
      lastFrame = e.Status.Frame;
      OnProgress?.Invoke(this, new ConversionProgressEventArgs(tempoEstimado,porcentagemConclusao, ElapsedTime) );
    }
    long lastFrame = 0;
    private void Fi_Completed(object sender, CompletedEventArgs e)
    {
      _endTicks = DateTime.Now.Ticks;
      switch (e.Status)
      {
        case CompletionStatus.Cancelled:
          State = ConversionStateEnum.Cancelled;
          break;
        case CompletionStatus.Error:
          State = ConversionStateEnum.Error;
          break;
        case CompletionStatus.Success:
          State = ConversionStateEnum.Success;
          break;
        case CompletionStatus.Timeout:
          State = ConversionStateEnum.Timeout;
          break;
      }
      var evArgs =  new ConversionCompletedEventArgs(State);
      if (State == ConversionStateEnum.Success) {
        var caminho = GeraManifestoMaster();
        evArgs.MasterPlayList.Path = caminho;
      }

      evArgs.Duration = ElapsedTime;
      
      if (vaiFazer320)
        evArgs.MasterPlayList.AvaiablePlayLists.Add(ObtemPlayListPara("320x180"));

      if(vaiFazer480)
        evArgs.MasterPlayList.AvaiablePlayLists.Add(ObtemPlayListPara("480x270"));

      if (vaiFazer640)
        evArgs.MasterPlayList.AvaiablePlayLists.Add(ObtemPlayListPara("640x360"));

      if (vaiFazer1280)
        evArgs.MasterPlayList.AvaiablePlayLists.Add(ObtemPlayListPara("1280x720"));

      //var oQueFez = new[] {  }


      OnComplete?.Invoke(this, evArgs);
    }

    private PlayList ObtemPlayListPara(string WxHFormatedParam)
    {
      var parts = WxHFormatedParam.Split('x');
      if (parts.Length != 2) throw new ArgumentException("O formato passado deve ser WxH.",nameof(WxHFormatedParam));
      return new PlayList {
        Width = int.Parse(parts[0]),
        Height = int.Parse(parts[1]),
        Path = Path.Combine(_destinationFolder,WxHFormatedParam,SEGMENT_MANIFEST_FILE_NAME),
        Segments = 
          Directory.GetFiles(Path.Combine(_destinationFolder, WxHFormatedParam)).Where(x=>x.EndsWith(".ts") )
          .Select(x=> new MediaSegment { Path = x, Size = new FileInfo(x).Length }).ToList()
      };
    }
  }
}
