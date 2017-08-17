using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
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
            if(period > Notify.MaxUpdatePeriod || period < Notify.MinUpdatePeriod)
            {
                await ReplyAsync("", false, new EmbedBuilder
                {
                    Color = Color.Red,
                    Description = $"Period can not be: `{period}`\nOnly between {Notify.MinUpdatePeriod} and {Notify.MaxUpdatePeriod}",
                });
                return;
            }
            if (countPosts > Notify.MaxPostsPerNotify || countPosts < Notify.MinPostsPerNotify)
            {
                await ReplyAsync("", false, new EmbedBuilder
                {
                    Color = Color.Red,
                    Description = $"Count posts can not be: `{countPosts}`\nOnly between {Notify.MinPostsPerNotify} and {Notify.MaxPostsPerNotify}",
                });
                return;
            }

            _notify.AddNotify(Context.Guild.Id, new Notify()
            {
                ChannelId = channelId,
                Domain = domain,
                LastSent = DateTime.Now,
                SendsPerNotify = Convert.ToUInt16(countPosts),
                UpdatePeriod = Convert.ToUInt16(period),
                LastCheck = DateTime.Now,
                Type = NotifyType.Wall
            });

            await ReplyAsync("", false, new EmbedBuilder
            {
                Color = Color.Green,
                Description = $"Okay, i am added",
            });
        }
    }
}
