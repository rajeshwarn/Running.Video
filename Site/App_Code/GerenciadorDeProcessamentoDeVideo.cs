using Running.Video.FFMPEG;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;


public static class GerenciadorDeProcessamentoDeVideo
{


  private static HLSConversionJob _conversionJob;

  private static Thread _thread = null;

  /// <summary>
  /// Joga o temp para a fila novamente, o temporário e preenche Processado
  /// </summary>
  public static void Inicializar()
  {
    // Tipo um static destructor
    AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

//    System.Diagnostics.Debugger.Break();

    CarregaProcessado();
    ReenfileraNaoTerminados();
    CarregaNaoProcessados();
    IniciaProcessamento();
    //_bgWorker.RunWorkerAsync();
    //_bgWorker.DoWork += _bgWorker_DoWork;

    //_bgWorker.DoWork += _bgWorker_DoWork;
    //_bgWorker.RunWorkerAsync();

  }


  private static string TmpDirFor(string nomeArquivo) {
    return Path.Combine(MapPath("~/_video_/tmp"), nomeArquivo);
  }

  private static string OutDirFor(string nomeArquivo)
  {
    return Path.Combine(MapPath("~/_video_/out"), nomeArquivo);
  }

  private static string InDirFor(string nomeArquivo)
  {
    return Path.Combine(MapPath("~/_video_/in"), nomeArquivo);
  }


  private static void IniciaProcessamento()
  {

    _thread = new Thread(new ThreadStart(()=> {
      if (null == ProcessamentoAtual && _fila?.Count > 0)
      {
        var processamento = _fila.FirstOrDefault();
        processamento.Situacao = SituacaoProcessamentoEnum.EmAndamento;
        _conversionJob = new HLSConversionJob();
        _conversionJob.SetSource(
          Path.Combine(MapPath("~/_video_/in"), processamento.NomeArquivo)
        );
        var dirTmp = TmpDirFor(processamento.NomeArquivo);
        try
        {
          Directory.Delete(dirTmp, true);
        }
        catch (Exception ex)
        {

        }
        Directory.CreateDirectory(dirTmp);

        _conversionJob.OnProgress += _conversionJob_OnProgress;
        _conversionJob.OnComplete += _conversionJob_OnComplete;

        _conversionJob.StartConversion(dirTmp);


      }
    }));
    _thread.Start(); 
  }

  private static void _conversionJob_OnProgress(object sender, Running.Video.ConversionProgressEventArgs e)
  {
    ProcessamentoAtual.Progresso = String.Format("{0:P0}", e.Progress);
    ProcessamentoAtual.TempoDecorrido = e.ElapsedTime;
  }

  private static void MoveTempParaOut(string nomeArquivo) {
    try
    {
      Directory.Delete(
        OutDirFor(nomeArquivo)
      );
    }
    catch (Exception) {

    }

    Directory.Move(
      TmpDirFor(nomeArquivo), 
      OutDirFor(nomeArquivo)
    );

    File.Move(InDirFor(nomeArquivo), OutDirFor(nomeArquivo) + "_" );
  }

  private static void _conversionJob_OnComplete(object sender, Running.Video.ConversionCompletedEventArgs e)
  {
    MoveTempParaOut(ProcessamentoAtual.NomeArquivo);
    ProcessamentoAtual.Situacao = SituacaoProcessamentoEnum.Concluido;
    _processado.Add( _fila?.Dequeue() );

    IniciaProcessamento();
    //throw new NotImplementedException();
    //_bgWorker.RunWorkerAsync();
  }




  private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
  {
    if(null!=_thread)
      _thread.Abort();
    
  }

  private static void CarregaNaoProcessados()
  {
    var dirTemp = MapPath("~/_video_/tmp");
    var dirEntrada = MapPath("~/_video_/in");
    var arquivosSomenteEntrada = Directory.GetFiles(dirEntrada).OrderBy(x => new FileInfo(x).LastWriteTime).Select(x => Path.GetFileName(x)).Except(Directory.GetDirectories(dirTemp).Select(x => Path.GetFileName(x)));
    foreach (var arquivo in arquivosSomenteEntrada)
    {
      _fila.Enqueue(new Processamento
      {
        NomeArquivo = arquivo,
        Situacao = SituacaoProcessamentoEnum.NaoIniciado
      });
    }
  }
  private static void _bgWorker_DoWork(object sender, DoWorkEventArgs e)
  {
    IniciaProcessamento();
  }

  private static List<Processamento> _processado;

  // System.IO.Path.GetInvalidFileNameChars
  private static Func<string, string> MapPath = System.Web.Hosting.HostingEnvironment.MapPath;
  private static void CarregaProcessado()
  {
    _processado = new List<Processamento>();
    var dirSaida = MapPath("~/_video_/out");
    foreach (var pasta in Directory.GetDirectories(dirSaida))
    {
      _processado.Add(new Processamento
      {
        Situacao = SituacaoProcessamentoEnum.Concluido,
        NomeArquivo = Path.GetFileName(pasta)
      });
    }
  }

  public static Processamento ProcessamentoAtual
  {
    get
    {
      return _fila.Where(x => x.Situacao == SituacaoProcessamentoEnum.EmAndamento).FirstOrDefault();
    }
  }

  private static void ReenfileraNaoTerminados()
  {
    _fila = new Queue<Processamento>();
    var dirTemp = MapPath("~/_video_/tmp");
    foreach (var pasta in Directory.GetDirectories(dirTemp))
    {
      _processado.Add(new Processamento
      {
        Situacao = SituacaoProcessamentoEnum.Interrompido,
        NomeArquivo = Path.GetFileName(pasta)
      });
      var arquivoEntrada = Path.Combine(MapPath("~/_video_/in"), Path.GetFileName(pasta));
      if (File.Exists(arquivoEntrada))
        ColocarNaFila(Path.GetFileName(arquivoEntrada), SituacaoProcessamentoEnum.Interrompido);

      Directory.Delete(pasta, true);
    }
  }


  public static void ColocarNaFila(string caminho, SituacaoProcessamentoEnum situacao = SituacaoProcessamentoEnum.NaoIniciado)
  {
    _fila.Enqueue(new Processamento
    {
      NomeArquivo = caminho,
      Situacao = situacao//SituacaoProcessamentoEnum.NaoIniciado
    });
    IniciaProcessamento();
  }

  public static Queue<Processamento> _fila;
  public static IReadOnlyList<Processamento> FilaDeProcessamento
  {
    get
    {
        return _fila?.ToList().AsReadOnly();
    }
  }
  public static IReadOnlyList<Processamento> Processado
  {
    get
    {
      return _processado;
    }
  }

  public static IReadOnlyList<Processamento> TodosProcessamentos
  {
    get
    {
      return _processado.Union(_fila).ToList();
    }
  }

  //
  // TODO: Add constructor logic here
  //
}

public class Processamento
{
  public string NomeArquivo { get; set; }
  public SituacaoProcessamentoEnum Situacao { get; set; }
  public string Progresso { get; set; }
  public TimeSpan TempoDecorrido { get; set; }
}

public enum SituacaoProcessamentoEnum
{
  NaoIniciado,
  Interrompido,
  EmAndamento,
  Concluido
}
