using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using VKDiscordBot.Models;
using VKDiscordBot.Services;
using VkNet.Enums;
using VkNet.Model;

namespace VKDiscordBot.Modules
{
    [Name("Notify"), Group("notify")]
    public class NotifyModule : ModuleBase
    {
        private readonly NotifyService _notify;
        private readonly VkService _vk;

        public NotifyModule(NotifyService notify, VkService vk)
        {
            _notify = notify;
            _vk = vk;
        }

        [Name("Post"), Command("post")]
        public async Task PostAsync(string domain)
        {
            await PostAsync(domain, $"<#{Context.Channel.Id}>", DataManager.BotSettings.DefaultUpdatePeriod, DataManager.BotSettings.DefaultSentPostsCount);
        }

        [Name("Post"), Command("post")]
        public async Task PostAsync(string domain, int period, int countPosts)
        {
            await PostAsync(domain, $"<#{Context.Channel.Id}>", period, countPosts);
        }

        [Name("Post"), Command("post")]
        public async Task PostAsync(string domain, string channel)
        {
            await PostAsync(domain, channel, DataManager.BotSettings.DefaultUpdatePeriod, DataManager.BotSettings.DefaultSentPostsCount);
        }

        [Name("Post"), Command("post")]
        public async Task PostAsync(string domain, string channel, int period, int countPosts)
        {
            var group = _vk.ResolveScreeName(domain);
            if(group == null)
            {
                await ReplyAsync("", false, new EmbedBuilder
                {
                    Color = Color.Red,
                    Description = $"Can not find this: `{domain}`",
                });
                return;
            }
            if(group.Type != VkObjectType.Group)
            {
                await ReplyAsync("", false, new EmbedBuilder
                {
                    Color = Color.Red,
                    Description = $"This is not group: `{domain}`",
                });
                return;
            }
            if (!MentionUtils.TryParseChannel(channel, out ulong channelId))
            {
                await ReplyAsync("", false, new EmbedBuilder
                {
                    Color = Color.Red,
                    Description = $"Can not parse this as discord channel: `{channel}`",
                });
                return;
            }

            if (!(Context.Client.GetChannelAsync(channelId).Result is ITextChannel))
            {
                await ReplyAsync("", false, new EmbedBuilder
                {
                    Color = Color.Red,
                    Description = $"This is not text channel: `{channel}`",
                });
                return;
            }
            if(period > NotifyInfo.MaxUpdatePeriod || period < NotifyInfo.MinUpdatePeriod)
            {
                await ReplyAsync("", false, new EmbedBuilder
                {
                    Color = Color.Red,
                    Description = $"Period can not be: `{period}`\nOnly between {NotifyInfo.MinUpdatePeriod} and {NotifyInfo.MaxUpdatePeriod}",
                });
                return;
            }
            if (countPosts > NotifyInfo.MaxPostsPerNotify || countPosts < NotifyInfo.MinPostsPerNotify)
            {
                await ReplyAsync("", false, new EmbedBuilder
                {
                    Color = Color.Red,
                    Description = $"Count posts can not be: `{countPosts}`\nOnly between {NotifyInfo.MinPostsPerNotify} and {NotifyInfo.MaxPostsPerNotify}",
                });
                return;
            }

            _notify.AddNotifyAndStart(Context.Guild.Id, new Notify()
            {
                LastSent = DateTime.Now,
                LastCheck = DateTime.Now,
                Info = new NotifyInfo
                {
                    ChannelId = channelId,
                    Domain = domain,
                    SendsPerNotify = Convert.ToUInt16(countPosts),
                    UpdatePeriod = Convert.ToUInt16(period),
                    Type = NotifyType.Wall,
                    WithHeader = true,
                    Hidden = false,
                    WithAudio = true,
                    WithDocument = true,
                    WithMap = true,
                    WithPhoto = true,
                    WithPool = true,
                    WithText = true,
                    WithVideo = true
                }
            });

            await ReplyAsync("", false, new EmbedBuilder
            {
                Color = Color.Green,
                Description = $"Okay, i am added",
            });
        }
    }
}
