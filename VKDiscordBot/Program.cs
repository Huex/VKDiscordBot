using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;
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
            var client = new DiscordSocketClient(data.BotSettings.ToDiscordSocketConfig());
            client.Log += logger.Log;
            var commands = new CommandService();
            commands.Log += logger.Log;

            // Вставить команды 

            // Вставить сервисы

            var services = new ServiceCollection();

            services.AddSingleton(data);
            services.AddSingleton(commands);
            // Добавить сервисы в коллекцию
            var serviceProvider = services.BuildServiceProvider();

            await client.LoginAsync(Discord.TokenType.Bot, File.ReadAllText("Configurations/Secrets/Token.txt"));
            await client.StartAsync();

            await Task.Delay(-1);
        }
    }
}