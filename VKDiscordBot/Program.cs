using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
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
            data.LoadBotSettings("Settings.json");
            var logger = new Logger(DataManager.BotSettings.LogLevel);
            data.Log += logger.Log;
            data.LoadGuildsSettings();
            var client = new DiscordSocketClient(DataManager.BotSettings.DiscordSocketConfig);
            client.Log += logger.Log;
            var commandService = new CommandService();
            commandService.Log += logger.Log;

            await commandService.AddModuleAsync<SettingsModule>();
            await commandService.AddModuleAsync<NotifyModule>();

            var vkService = new VkService(DataManager.BotSettings.VkAuthFilePath);
            vkService.Log += logger.Log;
            var notifyService = new NotifyService(client, vkService, data);
            notifyService.Log += logger.Log;

            var services = new ServiceCollection();
            services.AddSingleton(data);
            services.AddSingleton(commandService);
            services.AddSingleton(vkService);
            services.AddSingleton(notifyService);

            var eventHandler = new DiscordEventHandler(client, commandService, services.BuildServiceProvider());
            client.MessageReceived += eventHandler.HandleCommandAsync;
            client.GuildAvailable += eventHandler.GuildAvailable;
            client.GuildUpdated += eventHandler.GuildUpdated;
            client.Ready += eventHandler.ClientReady;

            await client.LoginAsync(TokenType.Bot, File.ReadAllText(DataManager.BotSettings.TokenFilePath));
            await client.StartAsync();
            await Task.Delay(-1);
        }
    }
}