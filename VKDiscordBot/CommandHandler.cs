using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VKDiscordBot.Services;

namespace VKDiscordBot
{
    internal class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;
        private readonly Random _random;

        public CommandHandler(DiscordSocketClient client, IServiceProvider services)
        {
            _services = services;
            _client = client;
            _random = new Random();
        }

        public async Task HandleCommandAsync(SocketMessage arg)
        {
            var msg = arg as SocketUserMessage;
            if (msg == null || msg?.Source != MessageSource.User)
            {
                return;
            }
            var guild = (new SocketCommandContext(_client, msg)).Guild;
            if (guild != null)
            {
                var data = (DataManager)_services.GetService(typeof(DataManager));
                await ProcessCommandAsync(data.BotSettings.DefaultPrefix, msg);
            }
        }

        public async Task ProcessCommandAsync(string prefix, SocketUserMessage message)
        {
            int prefixInt = prefix.Length - 1;
            if (message.HasStringPrefix(prefix, ref prefixInt) || message.HasMentionPrefix(_client.CurrentUser, ref prefixInt))
            {
                await message.Channel.TriggerTypingAsync();
                var context = new SocketCommandContext(_client, message);
                var commands = (CommandService)_services.GetService(typeof(CommandService));
                var result = await commands.ExecuteAsync(context, prefixInt, _services);
                if (!result.IsSuccess)
                {
                    List<GuildEmote> emotes = new List<GuildEmote>(context.Guild.Emotes);
                    if (emotes.Count != 0)
                    {
                        await message.AddReactionAsync(emotes[_random.Next(0, emotes.Count - 1)]);
                    }
                    await message.AddReactionAsync(new Emoji("❔"));
                }
            }
        }
    }
}
