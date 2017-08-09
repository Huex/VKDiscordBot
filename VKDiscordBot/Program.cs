using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;
using VKDiscordBot.Models;
using VKDiscordBot.Modules;
using VKDiscordBot.Services;

namespace VKDiscordBot
{
    public class Program
    {
        static void Main(string[] args)
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        private async Task MainAsync()
        {
            var data = new DataManager();
            data.LoadBotSettings("Configurations/Settings.json");
            var logger = new Logger(data.BotSettings.LogLevel);
            data.Log += logger.Log;
            data.LoadGuildsSettings("Configurations/Guilds/");
            var client = new DiscordSocketClient(data.BotSettings.ToDiscordSocketConfig());
            client.Log += logger.Log;
            var tasker = new Tasker();
            tasker.Log += logger.Log;
            var commands = new CommandService();
            commands.Log += logger.Log;

            await commands.AddModuleAsync<SettingsModule>();
            // Команды добавить здесь

            var vk = new VkService("Configurations/Secrets/VkAuthParams.json");
            vk.Log += logger.Log;

            // Создать сервисы здесь

            var services = new ServiceCollection();
            services.AddSingleton(data);
            services.AddSingleton(commands);
            services.AddSingleton(vk);
            services.AddSingleton(tasker);

            // Сервисы добавить здесь

            var commandHandler = new CommandHandler(client, services.BuildServiceProvider());
            client.MessageReceived += commandHandler.HandleCommandAsync;
            client.GuildAvailable += data.CheckGuildSettings;
            await client.LoginAsync(Discord.TokenType.Bot, File.ReadAllText("Configurations/Secrets/Token.txt"));
            await client.StartAsync();

            await Task.Delay(-1);
        }
    }
}