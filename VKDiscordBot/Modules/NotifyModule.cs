using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using VKDiscordBot.Models;
using VKDiscordBot.Services;
using VkNet.Enums;
using VkNet.Model;

namespace VKDiscordBot.Modules
{
    [Name("Notify")]
    public class NotifyModule : ModuleBase
    {
        private readonly NotifyService _notify;
        private readonly VkService _vk;

        public NotifyModule(NotifyService notify, VkService vk)
        {
            _notify = notify;
            _vk = vk;
        }

        [Name("List"), Command("list"), Alias("лист", "список")]
        public async Task ShowNotifysList()
        {
            string output = "";
            var notifys = _notify.GetNotifys(Context.Guild.Id).FindAll(n => !n.Info.Hidden);
            foreach (var notify in notifys)
            {
                if (!notify.Info.Hidden)
                {
                    output += $"**ID:** `{notify.Id}`\n**Name:** {notify.Info.Name}\n**Domain:** {notify.Info.SourceDomain}\n**Channel:** {((await Context.Client.GetChannelAsync(notify.Info.ChannelId)) as ITextChannel).Mention}\n**Type:** {notify.Info.Type.ToString()}\n\n";
                }
            }
            output = String.IsNullOrEmpty(output) ? $"{Context.User.Mention} notifys list is empty" : output;
            await ReplyAsync(output);
        }

        [Name("Delete"), Command("del"), Alias("уд", "удалить")]
        public async Task DeleteNotifyModule(int notifyID)
        {
            if (_notify.NotifyExist(Context.Guild.Id, notifyID))
            {
                _notify.RemoveNotify(notifyID);
                var mes = await ReplyAsync("Notify was successfull removed");
                new Timer((s) => mes.DeleteAsync(), null, DataManager.BotSettings.WaitingBeforeDeleteMessage, Timeout.Infinite);
            }
            else
            {
                await ReplyAsync($"{Context.User.Mention} can not find notify with this id: `{notifyID}`");
            }
        }

        [Name("Add"), Command("add"), Alias("доб", "добавить")]
        public async Task AddNotify(string domain, string channel, params string[] name)
        {
            if (!MentionUtils.TryParseChannel(channel, out ulong channelId))
            {
                await ReplyAsync($"{Context.User.Mention} can not parse this as discord channel: `{channel}`");
                return;
            }
            if (!(Context.Client.GetChannelAsync(channelId).Result is ITextChannel))
            {
                await ReplyAsync($"{Context.User.Mention} this is not text channel: `{channel}`");
                return;
            }
            var vkObj = _vk.ResolveScreeName(domain);
            if (vkObj == null)
            {
                await ReplyAsync($"{Context.User.Mention} can not find this: `{domain}`");
                return;
            }
            if (vkObj.Type != VkObjectType.Group && vkObj.Type != VkObjectType.User)
            {
                await ReplyAsync($"{Context.User.Mention} this is not group or user: `{domain}`");
                return;
            }
            var notify = new Notify()
            {
                LastSent = DateTime.Now,
                LastCheck = DateTime.Now,
                Info = new NotifyInfo
                {
                    Hidden = false,
                    Type = NotifyType.UserWall,
                    Name = String.Join(" ", name),
                    SourceDomain = domain,
                    SourceId = vkObj.Id,
                    ChannelId = channelId,
                    SendsPerNotify = Convert.ToUInt16(DataManager.BotSettings.DefaultSentPostsCount),
                    UpdatePeriod = Convert.ToUInt16(DataManager.BotSettings.DefaultUpdatePeriod),
                    WithHeader = true,
                    WithAudio = true,
                    WithDocument = true,
                    WithMap = true,
                    WithPhoto = true,
                    WithPool = true,
                    WithText = true,
                    WithVideo = true,
                    SearchString = null,
                    Comment = null,
                }
            };
            if (vkObj.Type != VkObjectType.User)
            {
                notify.Info.SourceId *= -1;
                notify.Info.Type = NotifyType.GroupWall;
            }
            _notify.AddNotifyAndStart(Context.Guild.Id, notify);
            var message = await ReplyAsync("Okay, i am added");
            new Timer((s) => message.DeleteAsync(), null, DataManager.BotSettings.WaitingBeforeDeleteMessage, Timeout.Infinite);
        }
    }

    //[Name("Post"), Command("post")]
    //public async Task PostDialogAsync(string domain, string channel)
    //{
    //    var group = _vk.ResolveScreeName(domain);
    //    if (group == null)
    //    {
    //        await ReplyAsync("", false, new EmbedBuilder
    //        {
    //            Color = Color.Red,
    //            Description = $"{Context.User.Mention}\nCan not find this: `{domain}`",
    //        });
    //        return;
    //    }
    //    if (group.Type != VkObjectType.Group)
    //    {
    //        await ReplyAsync("", false, new EmbedBuilder
    //        {
    //            Color = Color.Red,
    //            Description = $"{Context.User.Mention}\nThis is not group: `{domain}`",
    //        });
    //        return;
    //    }
    //    var notify = new Notify()
    //    {
    //        LastSent = DateTime.Now,
    //        LastCheck = DateTime.Now,
    //        Info = new NotifyInfo
    //        {
    //            Comment = null,
    //            ChannelId = Context.Channel.Id,
    //            Domain = domain,
    //            SendsPerNotify = Convert.ToUInt16(DataManager.BotSettings.DefaultSentPostsCount),
    //            UpdatePeriod = Convert.ToUInt16(DataManager.BotSettings.DefaultUpdatePeriod),
    //            Type = NotifyType.Wall,
    //            WithHeader = true,
    //            Hidden = false,
    //            WithAudio = true,
    //            WithDocument = true,
    //            WithMap = true,
    //            WithPhoto = true,
    //            WithPool = true,
    //            WithText = true,
    //            WithVideo = true
    //        }
    //    };
    //    var message = await ReplyAsync("", false, new EmbedBuilder
    //    {
    //        Color = Color.LightOrange,
    //        Description = $"{Context.User.Mention}\nDo you want to configure the notification in more detail?\nWrite 'yes' to set up or something else to go out. ",
    //    });

    //}

    //[Name("Post"), Command("post")]
    //public async Task PostAsync(string domain, params string[] parames)
    //{
    //    var group = _vk.ResolveScreeName(domain);
    //    if (group == null)
    //    {
    //        await ReplyAsync("", false, new EmbedBuilder
    //        {
    //            Color = Color.Red,
    //            Description = $"Can not find this: `{domain}`",
    //        });
    //        return;
    //    }

    //    if (group.Type != VkObjectType.Group)
    //    {
    //        await ReplyAsync("", false, new EmbedBuilder
    //        {
    //            Color = Color.Red,
    //            Description = $"This is not group: `{domain}`",
    //        });
    //        return;
    //    }

    //    var notify = new Notify()
    //    {
    //        LastSent = DateTime.Now,
    //        LastCheck = DateTime.Now,
    //        Info = new NotifyInfo
    //        {
    //            Comment = null,
    //            ChannelId = Context.Channel.Id,
    //            Domain = domain,
    //            SendsPerNotify = Convert.ToUInt16(DataManager.BotSettings.DefaultSentPostsCount),
    //            UpdatePeriod = Convert.ToUInt16(DataManager.BotSettings.DefaultUpdatePeriod),
    //            Type = NotifyType.Wall,
    //            WithHeader = true,
    //            Hidden = false,
    //            WithAudio = true,
    //            WithDocument = true,
    //            WithMap = true,
    //            WithPhoto = true,
    //            WithPool = true,
    //            WithText = true,
    //            WithVideo = true
    //        }
    //    };

    //    var prms = String.Concat(parames).Split(-;


    //}

    //[Name("Post"), Command("post")]
    //public async Task PostAsync(string domain, string channel, string comment)
    //{
    //    await PostAsync(domain, channel, DataManager.BotSettings.DefaultUpdatePeriod, DataManager.BotSettings.DefaultSentPostsCount, comment);
    //}

    //[Name("Post"), Command("post")]
    //public async Task PostAsync(string domain, string channel, int period, int countPosts, string comment)
    //{


    //    if (!MentionUtils.TryParseChannel(channel, out ulong channelId))
    //    {
    //        await ReplyAsync("", false, new EmbedBuilder
    //        {
    //            Color = Color.Red,
    //            Description = $"Can not parse this as discord channel: `{channel}`",
    //        });
    //        return;
    //    }

    //    if (!(Context.Client.GetChannelAsync(channelId).Result is ITextChannel))
    //    {
    //        await ReplyAsync("", false, new EmbedBuilder
    //        {
    //            Color = Color.Red,
    //            Description = $"This is not text channel: `{channel}`",
    //        });
    //        return;
    //    }
    //    if (period > NotifyInfo.MaxUpdatePeriod || period < NotifyInfo.MinUpdatePeriod)
    //    {
    //        await ReplyAsync("", false, new EmbedBuilder
    //        {
    //            Color = Color.Red,
    //            Description = $"Period can not be: `{period}`\nOnly between {NotifyInfo.MinUpdatePeriod} and {NotifyInfo.MaxUpdatePeriod}",
    //        });
    //        return;
    //    }
    //    if (countPosts > NotifyInfo.MaxPostsPerNotify || countPosts < NotifyInfo.MinPostsPerNotify)
    //    {
    //        await ReplyAsync("", false, new EmbedBuilder
    //        {
    //            Color = Color.Red,
    //            Description = $"Count posts can not be: `{countPosts}`\nOnly between {NotifyInfo.MinPostsPerNotify} and {NotifyInfo.MaxPostsPerNotify}",
    //        });
    //        return;
    //    }

    //    _notify.AddNotifyAndStart(Context.Guild.Id, 
    //    });

    //    var message = await ReplyAsync("", false, new EmbedBuilder
    //    {
    //        Color = Color.Green,
    //        Description = $"Okay, i am added",
    //    });
    //    new Timer((s) => message.DeleteAsync(), null, DataManager.BotSettings.WaitingBeforeDeleteMessage, Timeout.Infinite);
    //}

    //[Name("Post"), Command("post")]
    //public async Task PostAsync(string domain, string channel, int period, int countPosts)
    //{


    //}
}

