using Running.Video;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Running.Video.Manager
{
  public class VideoConversionManager<IVideoConversionJobInfoType, IHLSConversionJobType,IVideoConversionQueueRepositoryType>
  where IVideoConversionJobInfoType : class, IVideoConversionJobInfo
  where IHLSConversionJobType : class, IHLSConversionJob, new()
  where IVideoConversionQueueRepositoryType: IVideoConversionQueueRepository<IVideoConversionJobInfoType>, new()
  {
    private string _id;
    /// <summary>
    /// Construtor privado, deve ser construido utilizando o método para
    /// </summary>
    protected VideoConversionManager(string poolId)
    {
      _id = poolId;
      //_repository.
    }

    IVideoConversionQueueRepository<IVideoConversionJobInfoType> _repository = 
      new IVideoConversionQueueRepositoryType();
    
    private static Dictionary<string, VideoConversionManager<IVideoConversionJobInfoType, IHLSConversionJobType, IVideoConversionQueueRepositoryType>> _poolCliente =
      new Dictionary<string, VideoConversionManager<IVideoConversionJobInfoType, IHLSConversionJobType, IVideoConversionQueueRepositoryType>>();

    public static VideoConversionManager<IVideoConversionJobInfoType, IHLSConversionJobType, IVideoConversionQueueRepositoryType> Para(
      string identificador
    )
    {
      return _poolCliente.ContainsKey(identificador) ?
        _poolCliente[identificador] :
        (_poolCliente["identificador"] = new VideoConversionManager<IVideoConversionJobInfoType, IHLSConversionJobType, IVideoConversionQueueRepositoryType>(identificador));
    }


    public IReadOnlyList<Tuple<string, Task>> 
      JobQueue { get {
        return _jobQueue.ToList().AsReadOnly();
      } }


    //private IHLSConversionJob _conversionJob;

    //private Thread _thread = null;

    /// <summary>
    /// Joga o temp para a fila novamente, o temporário e preenche Processado
    /// </summary>
    public static void Inicializar()
    {
      AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
    }


    private string TmpDirFor(string nomeArquivo)
    {
      return Path.Combine(_repository.TempDir, Path.GetFileName(nomeArquivo));
    }

    private string OutDirFor(string nomeArquivo)
    {
      return Path.Combine(_repository.OutDir, Path.GetFileName(nomeArquivo));
    }


    Tuple<string, Task> _runningJob = null;

    Queue<Tuple<string, Task>> _jobQueue = new Queue<Tuple<string, Task>>();



    public JobQueueResultEnum EnqueueJob(IVideoConversionJobInfoType job)
    {

      if ( _runningJob!=null && job.Id.Equals( _runningJob.Item1 ))
        throw new InvalidOperationException($"O job: {job.Id} já está em andamento.");

      //_runningJob = new Tuple<string, Task>(job.Id,null);


      Task task =
        new Task(() =>
        {

          var thJob = job;
          thJob.Status = ConversionStatusEnum.InProgress;
          var _conversionJob = new IHLSConversionJobType();
          _conversionJob.OnProgress += _conversionJob_OnProgress;
          _conversionJob.OnComplete += _conversionJob_OnComplete;

          _conversionJob.Tag = thJob; // Vai ser recuperado através do sender nos eventos;
          _conversionJob.SetSource(thJob.FilePath);
          thJob.SourceInfo = _conversionJob.SourceInfo;
          var dirTmp = TmpDirFor(thJob.FilePath);
          try
          {
            Directory.Delete(dirTmp, true);
          }
          catch (Exception ex) { }
          Directory.CreateDirectory(dirTmp);
          _conversionJob.StartConversion(dirTmp);
          

        });
        


      if (_runningJob != null && _runningJob.Item2 != null &&  !_runningJob.Item2.IsCompleted)
      { // Bota na fila
        _jobQueue.Enqueue(new Tuple<string, Task>(job.Id, task));
        _repository?.OnJobEnqueued(job);
        return JobQueueResultEnum.Queued;
      }
      else
      {
        _runningJob = new Tuple<string, Task>(job.Id, task);
        task.Start();
        _repository?.OnConversionStarted(job);
        return JobQueueResultEnum.Started;
      }
    }

    private void _conversionJob_OnProgress(object sender, Running.Video.ConversionProgressEventArgs e)
    {
      IVideoConversionJobInfoType vJob = ((sender as IHLSConversionJob).Tag as IVideoConversionJobInfoType);
      vJob.Progress = e.Progress;
      _repository?.OnConversionUpdateProgress(vJob);
    }

    private void _conversionJob_OnComplete(object sender, Running.Video.ConversionCompletedEventArgs e)
    {
      IVideoConversionJobInfoType vJob = ((sender as IHLSConversionJob).Tag as IVideoConversionJobInfoType);

      vJob.FinishedAt = DateTime.Now;
      vJob.Status = e.Status;

      if (e.Status == ConversionStatusEnum.Success)
      {
        vJob.Progress = 1;
      }

      vJob.MasterPlayList = e.MasterPlayList;

      _repository?.OnConversionFinished(vJob);
      if (_jobQueue.Any())
      {
        _runningJob = _jobQueue.Dequeue();
        _runningJob.Item2.Start();
      }
    }

    private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
    {
      foreach (var task in _poolCliente.Select(x => x.Value._runningJob.Item2))
      {
        task.Dispose();
        //th.Value.Abort();
      }
    }

  }
}