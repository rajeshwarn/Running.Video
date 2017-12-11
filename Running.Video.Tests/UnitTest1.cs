using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Unity;
using Microsoft.Practices.Unity.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace Running.Video.Tests
{
  [TestClass]
  public class DITest
  {
    [TestMethod]
    public void TestMethod1()
    {
      //var x = AppDomain.CurrentDomain.Load("Running.Video.FFMPEG");

      //Microsoft.Practices.Unity.Configuration.UnityConfigurationSection
      //Microsoft.Practices.Untity.Configuration.UnityConfigurationSection
      IUnityContainer DIContainer = new UnityContainer().LoadConfiguration();
      var z = DIContainer.Resolve<IHLSConversionJob>();
      //z.SetSource(@"D:\Filmes\Homem-Aranha de Volta ao Lar\Spider-Man.mkv");
      z.SetSource(@"D:\AULA_MEDICA_-_OFOLATO.mp4");
      var mediaInfo = z.SourceInfo;
       Task.Run(() => z.StartConversion(@"d:\saida2"));
      //z.StartConversion("d:\saida2")
      while (z.State != ConversionStateEnum.Completo)
      {
        //Thread.Sleep(2000);
      }
      //var i = DIContainer.Resolve<IHLSConversionJob>();

      //z.OnProgress
       }
  }
}
