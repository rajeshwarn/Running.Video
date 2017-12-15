using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Unity;
using Microsoft.Practices.Unity.Configuration;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace Running.Video.Tests
{
  [TestClass]
  public class DITest
  {

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

      while (z.State != ConversionStateEnum.Success)
      {
        Thread.Sleep(2000);
      }

    }

    private void Z_OnProgress(object sender, ConversionProgressEventArgs e)
    {
      progressWriter.WriteLine($"Progress: {e.Progress} ||  TimeLeft: {e.TimeLeft}  ||  ElapsedTime: {e.ElapsedTime}");
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
