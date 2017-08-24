using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using VKDiscordBot.Models;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;

namespace VKDiscordBot.Services
{
    public class NotifyService : BotServiceBase
    {
        private int MILLISECONDS_PER_MINUTE = 60 * 1000;
        private readonly VkService _vk;
        private readonly DiscordSocketClient _client;
        private readonly DataManager _data;

        private List<NotifyTask> _notifys;

        public NotifyService(DiscordSocketClient client, VkService vk, DataManager data)
        {
            _vk = vk ?? throw new ArgumentNullException(nameof(vk));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _notifys = new List<NotifyTask>();
        }

        public void AddNotifyAndStart(ulong guildId, Notify notify)
        {
            if(_data.GuildSettingsExist(guildId))
            {
                var guild = _data.GetGuildSettings(guildId);
                guild.Notifys.Add(notify);
                AddNotifyTask(guild, notify);                
                _data.UpdateGuildSettings(guild.GuildId, guild);
            }
            else
            {
                RaiseLog(LogSeverity.Error, $"Guild not found. GuildId={guildId}");
            }
        }

        private void AddNotifyTask(GuildSettings guild, Notify notify)
        {
            var notifyHashCode = guild.Notifys[guild.Notifys.IndexOf(notify)].GetHashCode();
            var task = new NotifyTask(() =>
            {
                var newNotifyEntry = guild.Notifys.Find(n => n.GetHashCode() == notifyHashCode);
                var posts = CheckWallPosts(newNotifyEntry);
                if (posts.Count != 0)
                {
                    SentNotify(posts, newNotifyEntry.Info);
                    DateTime? newLastSent = newNotifyEntry.LastSent;
                    posts.ForEach((post) => newLastSent = newLastSent < post.Date ? post.Date : newLastSent);
                    newNotifyEntry.LastCheck = DateTime.Now;
                    newNotifyEntry.LastSent = (DateTime)newLastSent;
                    guild.Notifys.Remove(guild.Notifys.Find(n => n.GetHashCode() == notifyHashCode));
                    guild.Notifys.Add(newNotifyEntry);
                    notifyHashCode = newNotifyEntry.GetHashCode();
                    _data.UpdateGuildSettings(guild.GuildId, guild);
                }
            }, notify.Info, DataManager.BotSettings.NotifyDueTime, notify.Info.UpdatePeriod * MILLISECONDS_PER_MINUTE);
            task.TaskStarted += (id) =>
            {
                RaiseLog(LogSeverity.Debug, $"Notify task started. TaskId={id}");
            };
            task.TaskEnded += (id, result) =>
            {
                var severety = LogSeverity.Debug;
                if (result == TaskStatus.Canceled || result == TaskStatus.Faulted)
                {
                    severety = LogSeverity.Warning;
                }
                RaiseLog(severety, $"Notify task ended. TaslId={id} Resulr={result.ToString()}");
            };
            _notifys.Add(task);
            RaiseLog(LogSeverity.Debug, $"Added notify task. GuildId={guild.GuildId}");
        }

        public void AddGuildsNotifys()
        {
            foreach (var guild in _data.GuildsSettings)
            {
                foreach (var notify in guild.Notifys)
                {
                    AddNotifyTask(guild, notify);
                }
            }
        }

        public async Task StartAsync()
        {
            foreach (var task in _notifys)
            {
                _notifys.Find(t => t.Id == task.Id).Start();
                await Task.Delay(DataManager.BotSettings.StartNotifyDelay);
            }
        }

        private List<Post> CheckWallPosts(Notify notify)
        {
            List<Post> posts = new List<Post>();
            if (notify.Info.SearchString == null)
            {
                posts = _vk.GetWallPosts(new WallGetParams
                {
                    Count = Convert.ToUInt64(notify.Info.SendsPerNotify),
                    Domain = notify.Info.Domain
                });
            }
            else
            {
                posts = _vk.WallPostsSearch(new WallSearchParams
                {
                    Count = Convert.ToInt64(notify.Info.SendsPerNotify),
                    Domain = notify.Info.Domain,
                    Query = notify.Info.SearchString
                });
            }
            posts.RemoveAll(p => p.Date <= notify.LastSent);
            posts.Reverse();
            RaiseLog(LogSeverity.Debug, $"WallPosts checked. Domain={notify.Info.Domain} PostsCount={posts.Count}");
            return posts;
        }

        public string GetWallPostLink(Post post)
        {
            return $"{_vk.Domain}wall{post.OwnerId}_{post.Id}";
        }

        private string[] ToDivide(string text, double maxTextLength)
        {
            var needMessageCount = (int)Math.Ceiling((double)text.Length / DataManager.BotSettings.MessageTextLimit);
            var blockLength = text.Length / needMessageCount;
            string[] textBlocks = new string[needMessageCount];
            for (int i = 0,j=0; i < text.Length && j < needMessageCount; i += blockLength,j++)
            {
                textBlocks[j] = (text.Substring(i, text.Length - i > blockLength ? blockLength : text.Length - i));
            }
            return textBlocks;
        }

        private Uri GetMaxImageUri(Photo photo)
        {
            var uri = new Uri("Error: Can not get photo uri :c");
            uri = photo.Photo75 ?? uri;
            uri = photo.Photo130 ?? uri;
            uri = photo.Photo604 ?? uri;
            uri = photo.Photo807 ?? uri;
            uri = photo.Photo1280 ?? uri;
            uri = photo.Photo2560 ?? uri;
            return uri;
        }

        //TODO: доделать мг
        private void SentNotify(List<Post> posts, NotifyInfo notify)
        {
            if (posts.Count == 0)
            {
                return;
            }

            var channel = _client.GetChannel(notify.ChannelId) as ISocketMessageChannel;
            foreach (var post in posts)
            {
                if (notify.WithHeader)
                {
                    // здесь типо шапку оотправлять надо, данные о группе или посте
                }

                if (post.PostType == PostType.Post)
                {
                    var attachments = new Dictionary<Type, List<object>>();

                    foreach (var attach in post.Attachments)
                    {
                        if (!attachments.ContainsKey(attach.Type))
                        {
                            attachments.Add(attach.Type, new List<object>());
                        }
                        attachments[attach.Type].Add(attach.Instance);
                    }

                    if (notify.WithText)
                    {
                        foreach (var textBlock in ToDivide(post.Text, DataManager.BotSettings.MessageTextLimit))
                        {
                            channel.SendMessageAsync(textBlock).Wait();
                            Task.Delay(DataManager.BotSettings.SentTextDelay).Wait();
                        }
                    }

                    if (notify.WithPhoto)
                    {
                        if (attachments.ContainsKey(typeof(Photo)))
                        {
                            string[] links = new string[attachments[typeof(Photo)].Count];
                            int index = 0;
                            foreach(Photo photo in attachments[typeof(Photo)])
                            {
                                links[index] = GetMaxImageUri(photo).ToString();
                                index++;
                            }
                            if(links.Length <= 5)
                            {
                                channel.SendMessageAsync(String.Join(Environment.NewLine, links)).Wait();
                            }
                            else
                            {
                                string[] firstmsg = new string[5];
                                string[] secondmsg = new string[links.Length - 5];
                                for(int i = 0; i< links.Length; i++)
                                {
                                    if (i < 5)
                                    {
                                        firstmsg[i] = links[i];
                                    }
                                    else
                                    {
                                        secondmsg[i - 5] = links[i];
                                    }
                                }
                                channel.SendMessageAsync(String.Join(Environment.NewLine, firstmsg)).Wait();
                                Task.Delay(DataManager.BotSettings.BetweenSentPhotosDelay).Wait();
                                channel.SendMessageAsync(String.Join(Environment.NewLine, secondmsg)).Wait();
                            }
                            Task.Delay(DataManager.BotSettings.BeforePhotoDelay).Wait();
                        }
                    }


                    //var links = GetLinks(post.Attachments);
                    //if (links.Count <= 5)
                    //{
                    //    channel.SendMessageAsync($"```{footer}```\n{String.Join("\n", links)}").Wait();
                    //}
                    //else
                    //{
                    //    var secondLinks = new Collection<string>();
                    //    for (int i = 5; i < links.Count; i++)
                    //    {
                    //        secondLinks.Add(links[i]);
                    //        links.RemoveAt(i);
                    //    }
                    //    channel.SendMessageAsync($"```{footer}```\n{String.Join("\n", links)}").Wait();
                    //    Task.Delay(2000);
                    //    channel.SendMessageAsync($"{String.Join("\n", secondLinks)}").Wait();
                    //}
                    //Task.Delay(5000).Wait();

                    Task.Delay(DataManager.BotSettings.SentNotifyDelay).Wait();
                }                
            }
            RaiseLog(LogSeverity.Debug, $"Posts notifyed. Domain={notify.Domain} ChannelId={notify.ChannelId} PostsCount={posts.Count}");
        }
    }
}
