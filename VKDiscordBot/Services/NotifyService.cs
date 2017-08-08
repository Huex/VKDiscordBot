using System;
using System.Collections.Generic;
using System.Text;

namespace VKDiscordBot.Services
{
    internal class NotifyService : BotServiceBase
    {
        private readonly IServiceProvider _services;
        

        public NotifyService(IServiceProvider services)
        {
            _services = services;
        }

        public void LoadNotifys()
        {

        }
    }
}
