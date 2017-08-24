using Discord;
using Discord.WebSocket;

namespace VKDiscordBot.Models
{
	public class BotSettings
    {
        public string DefaultPrefix { get; set; }
        public int WaitingBeforeDeleteMessage { get; set; }
        public LogSeverity LogLevel { get; set; }
        public int MessageCacheSize { get; set; }
        public RetryMode DefaultRetryMode { get; set; }
        public int ConnectionTimeout { get; set; }
        public int? HandlerTimeout { get; set; }
        public bool AlwaysDownloadUsers { get; set; }

		public string GuildsSettingsDirectoryPath { get; set; }
		public string VkAuthFilePath { get; set; }
		public string TokenFilePath { get; set; }

		public int NotifyDueTime { get; set; }
		public int SentNotifyDelay { get; set; }
		public int StartNotifyDelay { get; set; }
		public int SentTextDelay { get; set; }
		public int BetweenSentPhotosDelay { get; internal set; }
		public int BeforePhotoDelay { get; internal set; }
		public int MessageTextLimit { get; set; }

		public int DefaultUpdatePeriod { get; set; }
		public int DefaultSentPostsCount { get; set; }


		public DiscordSocketConfig DiscordSocketConfig()
        {
            return new DiscordSocketConfig
            {
                LogLevel = LogLevel,
                MessageCacheSize = MessageCacheSize,
                DefaultRetryMode = DefaultRetryMode,
                ConnectionTimeout = ConnectionTimeout,
                HandlerTimeout = HandlerTimeout,
                AlwaysDownloadUsers = AlwaysDownloadUsers
            };
        }
    }
}
