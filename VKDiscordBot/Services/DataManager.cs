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
                        RaiseLog(LogSeverity.Warning, $"Can not read guild settings. Path={filePath}");
                    }
                    else
                    {
                        guildsSettings.Add(serverSettings);
                    }
                }
                GuildsSettings = guildsSettings;
                RaiseLog(LogSeverity.Info, $"Guilds settings readed. Path={path}");
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

        public void SetServerPrefix(IGuild guild, string prefix)
        {
            var path = GUILDS_SETTINGS_PATH + guild.Id + ".json";
            var neededServer = GuildsSettings.Find(s => s.GuildId == guild.Id);
            if (neededServer != null)
            {
                neededServer.Prefix = prefix;
                UpdateGuildSettings(neededServer);
            }
            else
            {
                RaiseLog(LogSeverity.Warning, $"Guild settings not exist. GuildId={guild.Id}");
            }
        }

        internal void UpdateGuildSettings(GuildSettings serverSettings)
        {
            try
            {
                var neededServer = GuildsSettings.Find(s => s.GuildId == serverSettings.GuildId);
                if (neededServer != null)
                {
                    GuildsSettings[GuildsSettings.IndexOf(neededServer)] = serverSettings;
                    RaiseLog(LogSeverity.Verbose, $"Guild settings update. GuildId={serverSettings.GuildId}");
                }
                else
                {
                    RaiseLog(LogSeverity.Warning, $"Guild settings not exist. GuildId={serverSettings.GuildId}");
                    return;
                }
                WriteGuildSettings(serverSettings);
            }
            catch (Exception exp)
            {
                RaiseLog(LogSeverity.Error, $"Guild settings FAIL update. GuildId={serverSettings.GuildId}", exp);
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

        private void WriteGuildSettings(GuildSettings settings)
        {
            try
            {
                File.WriteAllText(GUILDS_SETTINGS_PATH + settings.GuildId + ".json", JsonConvert.SerializeObject(settings));
                RaiseLog(LogSeverity.Info, $"Guild settings successfully write. GuildId={settings.GuildId}");
            }
            catch (Exception exp)
            {
                RaiseLog(LogSeverity.Error, $"Guild settings FAIL write. GuildId={settings.GuildId}", exp);
            }
        }
    }
}
