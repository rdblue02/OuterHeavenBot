using Castle.Core.Logging;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using OuterHeavenLight.Music;

namespace OuterHeavenBot.Test
{
    [TestClass]
    public class CommandHandlerTest
    {
        [TestMethod]
        public void CanGetCommandInfoFromMessage()
        {
           // var cs = new Moq.Mock<CommandService>();
           // var sp = new Moq.Mock<IServiceProvider>();
           // var l = new Moq.Mock<ILogger<MusicCommandHandler>>();
           // var handler = new MusicCommandHandler(cs.Object,sp.Object,l.Object);
           // var client = new Moq.Mock<MusicDiscordClient>();
             
           // var messageMock = new Moq.Mock<SocketMessage>();
           //     messageMock.Setup(x=>x.Content).Returns("~");
           // var userMessage = messageMock.Object;
           //var canExecute = handler.ShouldExecuteCommand(client.Object, userMessage);

           // Assert.IsTrue(canExecute);
            
        }
    }
 
}