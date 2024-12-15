using Discord;

namespace OuterHeavenBot.Core.Models
{
    public class ClippieFileData
    {
        public required string Name { get; init; }
        public required string FullName { get; init; }
        public IVoiceChannel? RequestingChannel { get; set; }
        public byte[] Data { get; set; } = [];

        public override bool Equals(object? obj)
        {
            if (obj is ClippieFileData other)
            {
                return Name == other.Name && other.FullName == FullName;
            }

            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return (Name, FullName).GetHashCode();
        }
    }
}
