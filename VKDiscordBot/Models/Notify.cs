using System;
using System.Collections.Generic;
using System.Text;

namespace VKDiscordBot.Models
{
    public class Notify
    {
        public const ushort MaxPostsPerNotify = 100;
        public const ushort MinPostsPerNotify = 1;

        public const ushort MaxUpdatePeriod = 60 * 24;
        public const ushort MinUpdatePeriod = 1;

        public NotifyType Type { get; set; }
        public string SearchString { get; set; }
        public string Domain { get; set; }
        public ulong ChannelId { get; set; }
        public DateTime LastCheck { get; set; }
        public DateTime LastSent { get; set; }
        public string Comment { get; set; }

        private ushort _sendsPerNotify;
        public ushort SendsPerNotify
        {
            get
            {
                return _sendsPerNotify;
            }
            set
            {
                _sendsPerNotify = value;
                if (_sendsPerNotify > MaxPostsPerNotify)
                {
                    _sendsPerNotify = MaxPostsPerNotify;
                }
                if (_sendsPerNotify < MinPostsPerNotify)
                {
                    _sendsPerNotify = MinPostsPerNotify;
                }
            }
        }

        private ushort _updatePeriod;
        public ushort UpdatePeriod
        {
            get
            {
                return _updatePeriod;
            }
            set
            {
                _updatePeriod = value;
                if (_updatePeriod > MaxUpdatePeriod)
                {
                    _updatePeriod = MaxUpdatePeriod;
                }
                if (_updatePeriod < MinUpdatePeriod)
                {
                    _updatePeriod = MinUpdatePeriod;
                }
            }
        }
    }
}
