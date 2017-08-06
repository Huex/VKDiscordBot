using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VKDiscordBot.Models;
using Newtonsoft.Json;
using System.IO;

namespace VKDiscordBot.Services
{
    internal class DataManager : BotServiceBase
    {
        public BotSettings BotSettings { get; private set; }

        public void LoadBotSettings(string path)
        {
            BotSettings = (BotSettings)JsonConvert.DeserializeObject(File.ReadAllText(path), typeof(BotSettings));         
        }
    }
}
