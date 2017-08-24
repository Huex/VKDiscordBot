using System;

namespace VKDiscordBot.Models
{
    public class Notify
    {
        public NotifyInfo Info { get; set; }
        public DateTime LastCheck { get; set; }
        public DateTime LastSent { get; set; }
    }
}
