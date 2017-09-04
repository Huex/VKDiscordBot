using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using VKDiscordBot.Services;

namespace VKDiscordBot.Modules
{
    [Name("Settings")]
    public class SettingsModule : ModuleBase
    {
        private DataManager _data;

        public SettingsModule(DataManager data)
        {
            _data = data;
        }

        [Name("Prefix"), Command("prefix"), Alias("pref")]
        [Summary("Sets the prefix for the bot commands on the current server")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task PrefixAsync(string prefix)
        {
            _data.SetServerPrefix(Context.Guild.Id, prefix);
            var message = await ReplyAsync($"Prefix `{prefix}` was set.");
            new Timer((s) => message.DeleteAsync(), null, DataManager.BotSettings.WaitingBeforeDeleteMessage, Timeout.Infinite);
        }

        [Name("Prefix"), Command("prefix"), Alias("pref")]
        [Summary("Displays the current prefix on the server")]
        public async Task PrefixAsync()
        {
            await ReplyAsync($"{Context.User.Mention} current prefix: `{_data.GetGuildSettings(Context.Guild.Id).Prefix}`");
        }
    }
}
