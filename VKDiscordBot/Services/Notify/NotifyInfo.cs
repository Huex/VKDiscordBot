using System;
using System.Collections.Generic;
using System.Text;

namespace VKDiscordBot.Models
{
    public class NotifyInfo
    {
        public NotifyType Type { get; set; }

        public string Name { get; set; }
        public string SearchString { get; set; }
        public string Comment { get; set; }

        public string SourceDomain { get; set; }
        public long? SourceId { get; set; }
        public ulong ChannelId { get; set; }

        public bool WithHeader { get; set; }
        public bool WithText { get; set; }
        public bool WithPhoto { get; set; }
        public bool WithAudio { get; set; }
        public bool WithVideo { get; set; }
        public bool WithDocument { get; set; }
        public bool WithMap { get; set; }
        public bool WithPool { get; set; }

        public ushort SendsPerNotify { get; set; }
        public ushort UpdatePeriod { get; set; }

        public bool Hidden { get; set; }
    }
}
