using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using OuterHeaven.LavalinkLight;
using OuterHeavenLight.Clippies;
using OuterHeavenLight.Dev;
using OuterHeavenLight.Extensions;
using OuterHeavenLight.LavaConnection;
using OuterHeavenLight.Music;

namespace OuterHeavenBot.Test
{
    [TestClass]
    public class DependencyInjectionTests
    {
        [TestMethod]
        public void Can_Resolve_Service_Collection()
        {
            var testAppSettings =  new AppSettings();
            var serviceCollection = new ServiceCollection();
           
            //add services
            serviceCollection.AddLogging();
            serviceCollection.AddDevUtilities();
            serviceCollection.AddLava();
            serviceCollection.AddMusic();
            serviceCollection.AddClips();
            serviceCollection.AddSingleton(testAppSettings); 

            //build provider
            var provider = serviceCollection.BuildServiceProvider();
            var musicService = provider.GetService<MusicService>();
            var clippieService = provider.GetService<ClippieService>();
            var devService = provider.GetService<DevService>();
        
            //ensure services are built
            Assert.IsNotNull(devService);
            Assert.IsNotNull(musicService);
            Assert.IsNotNull(clippieService); 
        }
    }
}
