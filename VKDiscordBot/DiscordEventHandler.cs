using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using VKDiscordBot.Models;
using VKDiscordBot.Services;

namespace VKDiscordBot
{
    public class DiscordEventHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commandService;
        private readonly IServiceProvider _services;
        private Random _random;

        private VkService _vkService => (VkService)_services.GetService(typeof(VkService));
        private NotifyService _notifyService => (NotifyService)_services.GetService(typeof(NotifyService));
        private DataManager _data => (DataManager)_services.GetService(typeof(DataManager));

        public DiscordEventHandler(DiscordSocketClient client, CommandService commandService, IServiceProvider services)
        {
            _random = new Random();
            _client = client;
            _commandService = commandService;
            _services = services;
        }

        internal Task GuildAvailable(SocketGuild guild)
        {
            if (!_data.GuildSettingsExist(guild.Id))
            {
                _data.AddGuildSettings(new GuildSettings
                {
                    Prefix = DataManager.BotSettings.DefaultPrefix,
                    GuildId = guild.Id,
                    Name = guild.Name,
                    Notifys = new List<Notify>()
                });
            }
            return Task.CompletedTask;
        }

        internal Task GuildUpdated(SocketGuild oldGuild, SocketGuild newGuild)
        {
            var settings = _data.GetGuildSettings(oldGuild.Id);
            settings.Name = newGuild.Name;
            _data.UpdateGuildSettings(oldGuild.Id, settings);
            return Task.CompletedTask;
        }

        internal Task ClientReady()
        {
            _vkService.AuthorizeAsync().ContinueWith((t) =>
            {
                if (_vkService.IsAuthorized)
                {
                    _notifyService.AddGuildsNotifys();
                    _notifyService.StartAsync().ConfigureAwait(false);
                }
            });
            
            return Task.CompletedTask;
        }

        internal Task HandleCommandAsync(SocketMessage arg)
        {
            var msg = arg as SocketUserMessage;
            if (msg == null || msg.Source != MessageSource.User)
            {
                return Task.CompletedTask;
            }
            var guild = (new SocketCommandContext(_client, msg)).Guild;
            if (guild != null)
            {
                var prefix = _data.GetGuildSettings(guild.Id).Prefix;
                int prefixInt = prefix.Length - 1;
                if (msg.HasStringPrefix(prefix, ref prefixInt) || msg.HasMentionPrefix(_client.CurrentUser, ref prefixInt))
                {
                    ProcessCommandAsync(prefixInt, msg).ConfigureAwait(false);
                }
            }
            return Task.CompletedTask;
        }

        private async Task ProcessCommandAsync(int prefixInt, SocketUserMessage message)
        {
            var context = new SocketCommandContext(_client, message);
            await message.Channel.TriggerTypingAsync();
            var result = await _commandService.ExecuteAsync(context, prefixInt, _services);
            if (!result.IsSuccess)
            {
                await message.AddReactionAsync(new Emoji("❔")).ContinueWith((s) =>
                {
                    List<GuildEmote> emotes = new List<GuildEmote>(context.Guild.Emotes);
                    if (emotes.Count != 0)
                    {
                        message.Channel.SendMessageAsync(emotes[_random.Next(0, emotes.Count - 1)].Url);
                    }
                });
            }
        }
    }
}
