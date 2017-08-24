using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Discord;
using Newtonsoft.Json;
using VKDiscordBot.Models;

namespace VKDiscordBot.Services
{
    public class DataManager : BotServiceBase
    {
        private static List<GuildSettings> _guildsSettings;

        public static BotSettings BotSettings { get; private set; }
        internal ReadOnlyCollection<GuildSettings> GuildsSettings
        {
            get
            {
                return new ReadOnlyCollection<GuildSettings>(_guildsSettings);
            }
        }

        public void LoadBotSettings(string path)
        {
            BotSettings = (BotSettings)JsonConvert.DeserializeObject(File.ReadAllText(path), typeof(BotSettings));         
        }

        public void LoadGuildsSettings()
        {
            var guildsSettings = new List<GuildSettings>();
            try
            {
                var filesPaths = Directory.GetFiles(BotSettings.GuildsSettingsDirectoryPath);
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
                _guildsSettings = guildsSettings;
                RaiseLog(LogSeverity.Info, $"Guilds settings readed. Path={BotSettings.GuildsSettingsDirectoryPath}");
            }
            catch (Exception exp)
            {
                RaiseLog(LogSeverity.Critical, "Error of reading guilds settings", exp);
            }
        }

        public void AddGuildSettings(GuildSettings guildSettings)
        {
            if(!GuildSettingsExist(guildSettings.GuildId))
            {
                _guildsSettings.Add(guildSettings);
                WriteGuildSettings(guildSettings);
                RaiseLog(LogSeverity.Verbose, $"Added new guild settings. GuildId={guildSettings.GuildId}");
            }
            else
            {
                RaiseLog(LogSeverity.Warning, $"Guild settings already exist. GuildId={guildSettings.GuildId}");
            }
        }

        internal void SetServerPrefix(ulong guildId, string prefix)
        {
            var neededServer = _guildsSettings.Find(s => s.GuildId == guildId);
            if (neededServer != null)
            {
                neededServer.Prefix = prefix;
                UpdateGuildSettings(neededServer.GuildId, neededServer);
            }
            else
            {
                RaiseLog(LogSeverity.Warning, $"Guild settings not exist. GuildId={guildId}");
            }
        }

        internal void UpdateGuildSettings(ulong GuildId, GuildSettings serverSettings)
        {
            try
            {
                var neededServer = _guildsSettings.Find(s => s.GuildId == serverSettings.GuildId);
                if (neededServer != null)
                {
                    _guildsSettings[_guildsSettings.IndexOf(neededServer)] = serverSettings;
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

        internal bool GuildSettingsExist(ulong guildId)
        {
            return _guildsSettings.Find(g => g.GuildId == guildId) != null;
        }

        internal GuildSettings GetGuildSettings(ulong guildId)
        {
            return _guildsSettings.Find(g => g.GuildId == guildId);
        }

        private void WriteGuildSettings(GuildSettings settings)
        {
            try
            {
                File.WriteAllText(BotSettings.GuildsSettingsDirectoryPath + settings.GuildId + ".json", JsonConvert.SerializeObject(settings));
                RaiseLog(LogSeverity.Info, $"Guild settings successfully write. GuildId={settings.GuildId}");
            }
            catch (Exception exp)
            {
                RaiseLog(LogSeverity.Error, $"Guild settings FAIL write. GuildId={settings.GuildId}", exp);
            }
        }
    }
}
