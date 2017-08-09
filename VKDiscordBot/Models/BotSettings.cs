using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

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

        public DiscordSocketConfig ToDiscordSocketConfig()
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
