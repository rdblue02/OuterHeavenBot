using Discord.WebSocket;

namespace OuterHeavenLight.Dev
{
    public interface IDiscordFileSender
    { 
        FileInfo? StageZipFile(List<FileInfo> filesToSend, string zipFileName);
        Task SendZipFileAsync(SocketUser user, FileInfo zipFileInfo);
    }
}