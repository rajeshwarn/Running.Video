using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Unity;
using Microsoft.Practices.Unity.Configuration;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Running.Video.FFMPEG;
using Running.Video.Manager;
using System.Collections.Generic;

namespace Running.Video.Tests
{

  public class StubRepository : IVideoConversionQueueRepository<BasicVideoConverionJobInfo>
  {
    public string OutDir
    {
      get; set;
    } = "d:\\out";

    public string TempDir
    {
      get; set;
    } = "d:\\temp";

    public IEnumerable<BasicVideoConverionJobInfo> ListJobs()
    {
      Console.WriteLine("ListJobs");
      //throw new NotImplementedException();
      return new[] { default(BasicVideoConverionJobInfo) };
    }

    public void OnConversionFinished(IVideoConversionJobInfo job)
    {
      //Console.WriteLine("OnConversionFinished");
    }

    public void OnConversionStarted(IVideoConversionJobInfo job)
    {
      Console.WriteLine("OnConversionStarted");
    }

    public void OnConversionUpdateProgress(IVideoConversionJobInfo job)
    {
      Console.WriteLine("OnConversionUpdateProgress");
      //throw new NotImplementedException();
    }

    public void OnJobEnqueued(IVideoConversionJobInfo job)
    {
      //throw new NotImplementedException();
    }
  }

  [TestClass]
  public class DITest
  {

    [TestMethod]
    public void Test_HLSConversionManager_ConversoesSimultaneas()
    {
      HLSConversionJob v = new HLSConversionJob();
      v.SetSource(@"D:\videos\fresenius\Volumat_Agilia.mp4");
      Thread t = new Thread(new ThreadStart(
        ()=> v.StartConversion(@"D:\temp\v")  
      ));
      HLSConversionJob v2 = new HLSConversionJob();
      v2.SetSource(@"D:\videos\fresenius\Applix.mp4");
      Thread t2 = new Thread(new ThreadStart(
        () => v2.StartConversion(@"D:\temp\v2")
      ));
      t.Start(); t2.Start();

      while (v.State != ConversionStatusEnum.Success 
        || v2.State != ConversionStatusEnum.Success)
      {

      }

    }


    [TestMethod]
    public void Test_FFMPEGConversionJobManager_ConversoesSimultaneas() {
      var conversor =  
        FFMPEGConversionJobManager<StubRepository>.Para("aaaaa");

      var conversor2 =
        FFMPEGConversionJobManager<StubRepository>.Para("ccccc");

      var job1 = new BasicVideoConverionJobInfo(
        @"D:\videos\fresenius\Volumat_Agilia.mp4",
        "1"
      );
      var job2 =
        new BasicVideoConverionJobInfo(
          @"D:\videos\fresenius\Applix.mp4",
          "2"
        );

      conversor.EnqueueJob(job1);
      conversor2.EnqueueJob(job2);

      while(
          (job1.Status != ConversionStatusEnum.Success) 
          || (job2.Status != ConversionStatusEnum.Success)
      )
      {
        Thread.Sleep(2000);
      }
    }

    [TestMethod,Description("Coloca 2 jobs, sendo que um é enfileirado")]
    public void Test_FFMPEGConversionJobManager_Fila()
    {
      var conversor =
        FFMPEGConversionJobManager<StubRepository>.Para("Empresa1");
      //var conversor2 =
      //  FFMPEGConversionJobManager<StubRepository>.Para("ccccc");

      var job1 = 
        new BasicVideoConverionJobInfo(
          @"D:\videos\fresenius\Volumat_Agilia.mp4",
          "1"
        );

      var job2 =
        new BasicVideoConverionJobInfo(
          @"D:\videos\fresenius\Applix.mp4",
          "2"
        );

      conversor.EnqueueJob(job1);
      conversor.EnqueueJob(job2);

      Assert.AreEqual(1, conversor.JobQueue.Count);

      while (
          (job1.Status != ConversionStatusEnum.Success)
          || (job2.Status != ConversionStatusEnum.Success)
      )
      {
        Thread.Sleep(2000);
      }
    }


    StreamWriter progressWriter = null;


    [TestMethod]
    public void Test4_VGR__ATROVERAN()
    {
      ProcessarVideo("4_VGR_-_ATROVERAN_-_MOBILE.mp4");
    }



    [TestMethod]
    public void Test15_Legislacao_e_tributacao_VSMobile()
    {
      ProcessarVideo("15_Legislacao_e_tributacao_VSMobile.mp4");
    }

    [TestMethod]  
    public void TestSpider_Man()
    {
      ProcessarVideo("Spider-Man.mkv");
    }
    

    protected void ProcessarVideo(string video) {

      var progressFile = $@"d:\GDrive\Running.Video\videos\saida\{video}.progress";
      var outDir = $@"d:\GDrive\Running.Video\videos\saida\{video}";
      try
      {
        Directory.Delete(outDir, true);
        File.Delete(progressFile);
      }
      catch
      {

      }

      IUnityContainer DIContainer = new UnityContainer().LoadConfiguration();
      var z = DIContainer.Resolve<IHLSConversionJob>();

      z.SetSource($@"D:\GDrive\Running.Video\videos\entrada\{video}");
      var mediaInfo = z.SourceInfo;
      progressWriter = File.AppendText(progressFile);
      z.OnComplete += Z_OnComplete;
      z.OnProgress += Z_OnProgress;

      Task.Run(() => z.StartConversion(outDir));

      while (z.State != ConversionStatusEnum.Success)
      {
        Thread.Sleep(2000);
      }

    }

    private void Z_OnProgress(object sender, ConversionProgressEventArgs e)
    {
      progressWriter.WriteLine($"Progress: {e.Progress} ||  ElapsedTime: {e.ElapsedTime}");
      progressWriter.Flush();
    }

    private void Z_OnComplete(object sender, ConversionCompletedEventArgs e)
    {
      progressWriter.WriteLine($"Completed in : {e.Duration}" );
      progressWriter.Close();
      progressWriter.Dispose();
    }
  }
}
