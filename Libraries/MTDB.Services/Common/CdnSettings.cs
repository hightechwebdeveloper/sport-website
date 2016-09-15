namespace MTDB.Core.Services.Common
{
    public class CdnSettings
    {
        public CdnSettings(string host, string username, string password, string subdir = "")
        {
            this.Subdir = subdir.TrimStart('/');
            this.Host = host;
            this.Username = username;
            this.Password = password;
        }

        public string Host { get; }

        public string Username { get; }

        public string Password { get; }

        public string Subdir { get; }
    }
}
