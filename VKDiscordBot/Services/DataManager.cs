using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VKDiscordBot.Models;
using Newtonsoft.Json;
using System.IO;
using Discord;
using Discord.WebSocket;

namespace VKDiscordBot.Services
{
    public class DataManager : BotServiceBase
    {
        private const string GUILDS_SETTINGS_PATH = "Configurations/Guilds/";

        public BotSettings BotSettings { get; private set; }
        public List<GuildSettings> GuildsSettings { get; private set; }

        public void LoadBotSettings(string path)
        {
            BotSettings = (BotSettings)JsonConvert.DeserializeObject(File.ReadAllText(path), typeof(BotSettings));         
        }

        public void LoadGuildsSettings(string path)
        {
            var guildsSettings = new List<GuildSettings>();
            try
            {
                var filesPaths = Directory.GetFiles(path);
                foreach (var filePath in filesPaths)
                {
                    var serverSettings = (GuildSettings)JsonConvert.DeserializeObject(File.ReadAllText(filePath), typeof(GuildSettings));
                    if (serverSettings == null)
                    {
                        RaiseLog(LogSeverity.Warning, $"Can not read guild settings: {filePath}");
                    }
                    else
                    {
                        guildsSettings.Add(serverSettings);
                    }
                }
                GuildsSettings = guildsSettings;
                RaiseLog(LogSeverity.Info, "Guilds settings readed");
            }
            catch (Exception exp)
            {
                RaiseLog(LogSeverity.Critical, "Error of reading guilds settings", exp);
            }
        }

        internal Task CheckGuildSettings(SocketGuild guild)
        {
            if(GuildsSettings.Find(g=>g.GuildId == guild.Id) == null)
            {
                var settings = new GuildSettings
                {
                    Prefix = BotSettings.DefaultPrefix,
                    GuildId = guild.Id,
                    Name = guild.Name,
                    Notifys = new List<Notify>()
                };
                GuildsSettings.Add(settings);
                WriteGuildSettings(settings);
            }
            return Task.CompletedTask;
        }

        public bool SetServerPrefix(IGuild guild, string prefix)
        {
            var path = GUILDS_SETTINGS_PATH + guild.Id + ".json";
            var neededServer = GuildsSettings.Find(s => s.GuildId == guild.Id);
            if (neededServer != null)
            {
                neededServer.Prefix = prefix;
                var res = UpdateServerSettings(neededServer);
                return res;
            }
            else
            {
                RaiseLog(LogSeverity.Warning, $"Guild settings not exist: {guild.Id}");
                return false;
            }
        }

        private bool UpdateServerSettings(GuildSettings serverSettings)
        {
            try
            {
                if (WriteGuildSettings(serverSettings))
                {
                    var neededServer = GuildsSettings.Find(s => s.GuildId == serverSettings.GuildId);
                    if (neededServer != null)
                    {
                        GuildsSettings[GuildsSettings.IndexOf(neededServer)] = serverSettings;
                        RaiseLog(LogSeverity.Verbose, $"Guild settings successfully update: {serverSettings.GuildId}");
                        return true;
                    }
                    else
                    {
                        RaiseLog(LogSeverity.Warning, $"Guild settings {serverSettings.GuildId} not exist");
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception exp)
            {
                RaiseLog(LogSeverity.Error, $"Guild settings FAIL update: {serverSettings.GuildId}", exp);
                return false;
            }
        }

        public string GetGuildPrefix(IGuild guild)
        {
            return GuildsSettings.Find(s => s.GuildId == guild.Id).Prefix;
        }

        public string GetGuildPrefix(ulong guildId)
        {
            return GuildsSettings.Find(s => s.GuildId == guildId).Prefix;
        }

        private bool WriteGuildSettings(GuildSettings settings)
        {
            try
            {
                File.WriteAllText(GUILDS_SETTINGS_PATH + settings.GuildId + ".json", JsonConvert.SerializeObject(settings));
                RaiseLog(LogSeverity.Info, $"Guild settings successfully write: {settings.GuildId}");
                return true;
            }
            catch (Exception exp)
            {
                RaiseLog(LogSeverity.Error, $"Guild settings FAIL write: {settings.GuildId}", exp);
                return false;
            }
        }
    }
}
