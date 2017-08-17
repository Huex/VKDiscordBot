using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using VKDiscordBot.Models;
using VkNet.Model;
using VkNet.Model.RequestParams;

namespace VKDiscordBot.Services
{
    public class NotifyService : BotServiceBase
    {
        public const int TaskDueTime = 3000;
        public const int NotifyDelay = 5000;
        public const int StartNotifyDelay = 5000;
        private const int MillicesondsInMinute = 60 * 1000;

        private readonly VkService _vk;
        private readonly TaskManager _tasker;
        private readonly DiscordSocketClient _client;
        private readonly DataManager _data;

        private Collection<int> TasksIds;

        public NotifyService(DiscordSocketClient client, VkService vk, TaskManager tasker, DataManager data)
        {
            _vk = vk;
            _tasker = tasker;
            _client = client;
            _data = data;
            TasksIds = new Collection<int>();
        }

        internal void AddNotify(ulong GuildId, Notify notify)
        {
            var guild = _data.GuildsSettings.Find(g => g.GuildId == GuildId);
            if(guild != null)
            {
                guild.Notifys.Add(notify);
                var notifyIndex = guild.Notifys.IndexOf(notify);
                _tasker.AddAndStart(new RepetitiveTask(() =>
                {
                    var posts = CheckWallPosts(guild.Notifys[notifyIndex]);
                    if (posts.Count != 0)
                    {
                        NotifyPosts(posts, guild.Notifys[notifyIndex]);
                        DateTime? newLastSent = guild.Notifys[notifyIndex].LastSent;
                        posts.ForEach((post) => newLastSent = newLastSent < post.Date ? post.Date : newLastSent);
                        guild.Notifys[notifyIndex].LastCheck = DateTime.Now;
                        guild.Notifys[notifyIndex].LastSent = (DateTime)newLastSent;
                        _data.UpdateGuildSettings(guild);
                    }
                }, TaskDueTime, guild.Notifys[notifyIndex].UpdatePeriod * MillicesondsInMinute));
                RaiseLog(LogSeverity.Debug, $"Added notify task. GuildId={guild.GuildId}");
                _data.UpdateGuildSettings(guild);
            }
            else
            {
                RaiseLog(LogSeverity.Error, $"Guild not found. GuildId={GuildId}");
            }
        }

        public void AddGuildsNotifys()
        {
            foreach (var guild in _data.GuildsSettings)
            {
                foreach (var notify in guild.Notifys)
                {
                    if (notify.Type == NotifyType.Wall)
                    {
                        var task = new RepetitiveTask(() =>
                        {
                            var posts = CheckWallPosts(notify);
                            if (posts.Count != 0)
                            {
                                NotifyPosts(posts, notify);
                                DateTime? newLastSent = notify.LastSent;
                                posts.ForEach((post) => newLastSent = newLastSent < post.Date ? post.Date : newLastSent);
                                notify.LastCheck = DateTime.Now;
                                notify.LastSent = (DateTime)newLastSent;
                                _data.UpdateGuildSettings(guild);
                            }
                        }, TaskDueTime, notify.UpdatePeriod * MillicesondsInMinute);
                        TasksIds.Add(task.Id);
                        _tasker.Add(task);
                        RaiseLog(LogSeverity.Debug, $"Added notify task. GuildId={guild.GuildId}");
                    }
                }
            }           
        }

        public async Task StartAsync()
        {
            foreach (var task in TasksIds)
            {
                _tasker.Find(t => t.Id == task).Start();
                await Task.Delay(StartNotifyDelay);
            }
        }

        private void NotifyPosts(List<Post> posts, Notify notify)
        {
            if (posts.Count == 0)
            {
                return;
            }

            var channel = _client.GetChannel(notify.ChannelId) as ISocketMessageChannel;
            foreach (var post in posts)
            {
                channel?.SendMessageAsync($"{notify.Comment}\nNew post -> {GetWallPostLink(post)}").Wait();
                Task.Delay(NotifyDelay).Wait();
            }
            RaiseLog(LogSeverity.Debug, $"Notifyed posts. Domain={notify.Domain} ChannelId={notify.ChannelId}");
        }

        private List<Post> CheckWallPosts(Notify notify)
        {
            List<Post> posts = new List<Post>();
            if (notify.SearchString == null)
            {
                posts = _vk.GetWallPosts(new WallGetParams
                {
                    Count = Convert.ToUInt64(notify.SendsPerNotify),
                    Domain = notify.Domain
                });
            }
            else
            {
                posts = _vk.WallPostsSearch(new WallSearchParams
                {
                    Count = Convert.ToInt64(notify.SendsPerNotify),
                    Domain = notify.Domain,
                    Query = notify.SearchString
                });
            }
            posts.RemoveAll(p => p.Date <= notify.LastSent);
            posts.Reverse();
            RaiseLog(LogSeverity.Debug, $"WallPosts checked. Domain={notify.Domain}");
            return posts;
        }

        public string GetWallPostLink(Post post)
        {
            return $"{_vk.Domain}wall{post.OwnerId}_{post.Id}";
        }

        //private void NotifyPosts(List<Post> posts, Notify notify)
        //{
        //    if(posts.Count == 0)
        //    {
        //        return;
        //    }

        //    var channel = _client.GetChannel(notify.ChannelId) as ISocketMessageChannel;
        //    var sourceName = "";

        //    var source = _vk.ResolveScreeName(notify.Domain);
        //    if (source.Type == VkObjectType.User)
        //    {
        //        var user = _vk.GetUser((long)source.Id, ProfileFields.Photo200);
        //        sourceName = $"{user.FirstName} {user.LastName}";
        //    }
        //    if (source.Type == VkObjectType.Group)
        //    {
        //        var group = _vk.GetGroup((long)source.Id, GroupsFields.All);
        //        sourceName = group.Name;
        //    }

        //    foreach (var post in posts)
        //    {
        //        var intro = $"Community: {sourceName}\nDate: {post.Date?.AddHours(-3).ToString()}";
        //        var footer = $"Like {post.Likes?.Count}    Comment {post.Comments?.Count}    Share {post.Reposts?.Count}    View {post.Views?.Count}";
        //        var postLink = $"{_vk.Domain}wall{post.OwnerId}_{post.Id}";

        //        var textAndIntro = $"```{intro}```\n{postLink}\n{post.Text}";

        //        var needMessageCount = (int)Math.Ceiling((double)textAndIntro.Length / 2000);
        //        var blockLength = textAndIntro.Length / needMessageCount;
        //        List<string> textBlocks = new List<string>(textAndIntro.Length / blockLength + 1);
        //        for (int i = 0; i < textAndIntro.Length; i += blockLength)
        //        {
        //            textBlocks.Add(textAndIntro.Substring(i, textAndIntro.Length - i > blockLength ? blockLength : textAndIntro.Length - i));
        //        }
        //        foreach(var textBlock in textBlocks)
        //        {
        //            channel.SendMessageAsync(textBlock).Wait();
        //            Task.Delay(2000).Wait();
        //        }
        //        var links = GetLinks(post.Attachments);
        //        if(links.Count <= 5)
        //        {
        //            channel.SendMessageAsync($"```{footer}```\n{String.Join("\n", links)}").Wait();
        //        }
        //        else
        //        {
        //            var secondLinks = new Collection<string>();
        //            for(int i=5; i < links.Count; i++)
        //            {
        //                secondLinks.Add(links[i]);
        //                links.RemoveAt(i);
        //            }
        //            channel.SendMessageAsync($"```{footer}```\n{String.Join("\n", links)}").Wait();
        //            Task.Delay(2000);
        //            channel.SendMessageAsync($"{String.Join("\n", secondLinks)}").Wait();
        //        }
        //        Task.Delay(5000).Wait();
        //    }
        //}

        //private Collection<string> GetLinks(ReadOnlyCollection<VkNet.Model.Attachments.Attachment> attachments)
        //{
        //    var links = new Collection<string>();
        //    foreach(var attach in attachments)
        //    {
        //        if(attach.Type == typeof(Photo))
        //        {
        //            links.Add(GetMaxImageUri((Photo)attach.Instance).ToString());
        //        }
        //        if(attach.Type == typeof(Video))
        //        {
        //            links.Add(GetUri((Video)attach.Instance).ToString());
        //        }
        //    }
        //    return links;
        //}

        //public Uri GetMaxImageUri(Photo photo)
        //{
        //    Uri uri = new Uri("http://investformat.ru/wp-content/uploads/2015/11/Kak-dobavit-video-v-VK-v-Kontakt-700x350.jpg");
        //    uri = photo.Photo75 ?? uri;
        //    uri = photo.Photo130 ?? uri;
        //    uri = photo.Photo604 ?? uri;
        //    uri = photo.Photo807 ?? uri;
        //    uri = photo.Photo1280 ?? uri;
        //    uri = photo.Photo2560 ?? uri;
        //    return uri;
        //}

        //public Uri GetMaxImageUri(Video video)
        //{
        //    Uri uri = new Uri("http://investformat.ru/wp-content/uploads/2015/11/Kak-dobavit-video-v-VK-v-Kontakt-700x350.jpg");
        //    uri = video.Photo130 ?? uri;
        //    uri = video.Photo320 ?? uri;
        //    uri = video.Photo640 ?? uri;
        //    uri = video.Photo800 ?? uri;
        //    return uri;
        //}

        //public Uri GetUri(Video video)
        //{
        //    return new Uri($"{_vk.Domain}video{video.OwnerId}_{video.Id}");
        //}
    }
}
