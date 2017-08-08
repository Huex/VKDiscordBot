using System;
using System.Collections.Generic;
using System.Text;

namespace VKDiscordBot.Models
{
    public class GuildSettings
    {
        public string Name { get; set; }
        public ulong GuildId { get; set; }
        public string Prefix { get; set; }
        public List<Notify> Notifys { get; set; }
    }
}
