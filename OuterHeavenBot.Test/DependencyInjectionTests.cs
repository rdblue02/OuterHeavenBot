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
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddDevUtilities();
            serviceCollection.AddLava();
            serviceCollection.AddMusic();
            serviceCollection.AddClips();
            serviceCollection.AddSingleton(new AppSettings());
          //  serviceCollection.AddSingleton(new LavaFileCache());
            var provider = serviceCollection.BuildServiceProvider();

            var musicService = provider.GetService<MusicService>();
            var clippieService = provider.GetService<ClippieService>();

            Assert.IsNotNull(musicService);
            Assert.IsNotNull(clippieService);

        }
    }
}
