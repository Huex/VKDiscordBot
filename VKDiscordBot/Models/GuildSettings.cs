using System;
using System.Collections.Generic;
using System.Text;

namespace VKDiscordBot.Models
{
    public class GuildSettings
    {
        public string Name { get; set; }
        public ulong Id { get; set; }
        public string Prefix { get; set; }
    }
}
