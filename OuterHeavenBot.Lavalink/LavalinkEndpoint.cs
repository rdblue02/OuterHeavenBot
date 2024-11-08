namespace OuterHeavenBot.Lavalink
{
    public class LavalinkEndpoint
    { 
        public string Host { get; } 
        public int Port { get; } 
        public string Route { get; } 
        public bool IsSecure { get; } 
        public string Password { get; }
         
        public LavalinkEndpoint(string host, int port, string password, string route = "/", bool isSecure = false)
        {
            if (string.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));
            if (port < 1 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentNullException(nameof(password));
            if (string.IsNullOrWhiteSpace(route)) throw new ArgumentNullException(nameof(route));

            Host = host;
            Port = port;
            Password = password;
            Route = route;
            IsSecure = isSecure;
        }

        public Uri ToUri() => new Uri($"http{(IsSecure ? "S" : string.Empty)}://{Host}:{Port}{Route}");
        public override string ToString() => $"http{(IsSecure ? "S" : string.Empty)}://{Host}:{Port}{Route}"; 
        public string ToWebSocketString() => $"ws{(IsSecure ? "s" : string.Empty)}://{Host}:{Port}{Route}";
    }
}