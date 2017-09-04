using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using VKDiscordBot.Models;
using VkNet.Enums.Filters;
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

        private Dictionary<ulong, List<NotifyTask>> _notifys;

        public NotifyService(DiscordSocketClient client, VkService vk, DataManager data)
        {
            _vk = vk ?? throw new ArgumentNullException(nameof(vk));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _notifys = new Dictionary<ulong, List<NotifyTask>>();
        }

        public List<NotifyTask> GetNotifys(ulong guildId)
        {
            return _notifys.ContainsKey(guildId) ? _notifys[guildId] : new List<NotifyTask>();
        }

        public bool NotifyExist(int taskId)
        {
            foreach (var guildNotifys in _notifys)
            {
                if (guildNotifys.Value.Find(t => t.Id == taskId) != null)
                {
                    return true;
                }
            }
            return false;
        }

        public bool NotifyExist(ulong guildId, int taskId)
        {
            if (_notifys.ContainsKey(guildId))
            {
                    if (_notifys[guildId].Find(t => t.Id == taskId) != null)
                    {
                        return true;
                    }
            }
            return false;
        }

        public void RemoveNotify(int taskId)
        {
            foreach(var guildNotifys in _notifys)
            {
                var task = guildNotifys.Value.Find(t => t.Id == taskId);
                if(task != null)
                {
                    var index = guildNotifys.Value.IndexOf(task);
                    guildNotifys.Value[index].Stop();
                    guildNotifys.Value[index].Dispose();
                    guildNotifys.Value.RemoveAt(index);
                    var guild = _data.GetGuildSettings(guildNotifys.Key);
                    guild.Notifys.Remove(guild.Notifys.Find(n => n.Info == task.Info));
                    _data.UpdateGuildSettings(guildNotifys.Key, guild);
                    task.Dispose();
                    break;
                }               
            }
        }

        public void RemoveNotify(ulong guildId, int taskId)
        {
            if (_notifys.ContainsKey(guildId))
            {
                var task = _notifys[guildId].Find(t => t.Id == taskId);
                if (task != null)
                {
                    var index = _notifys[guildId].IndexOf(task);
                    _notifys[guildId][index].Stop();
                    _notifys[guildId][index].Dispose();
                    _notifys[guildId].RemoveAt(index);
                    var guild = _data.GetGuildSettings(guildId);
                    guild.Notifys.Remove(guild.Notifys.Find(n => n.Info == task.Info));
                    _data.UpdateGuildSettings(guildId, guild);
                    task.Dispose();
                }
            }             
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
                if(_client.ConnectionState != ConnectionState.Connected)
                {
                    RaiseLog(LogSeverity.Warning, $"No connection to perform notify task.");
                    return;                  
                }
                var newNotifyEntry = guild.Notifys.Find(n => n.GetHashCode() == notifyHashCode);
                if(newNotifyEntry.Info.SourceId == null)
                {
                    var obj = _vk.ResolveScreeName(newNotifyEntry.Info.SourceDomain);
                    newNotifyEntry.Info.SourceId = obj.Id;
                    if (obj.Type == VkNet.Enums.VkObjectType.Group)
                    {
                        newNotifyEntry.Info.SourceId *= -1;
                    }
                }
                var posts = CheckWallPosts(newNotifyEntry);
                if (posts.Count != 0)
                {
                    var typingState = ((SocketTextChannel)_client.GetChannel(notify.Info.ChannelId)).EnterTypingState();
                    DateTime? newLastSent = newNotifyEntry.LastSent;
                    posts.ForEach((post) => newLastSent = newLastSent < post.Date ? post.Date : newLastSent);
                    newNotifyEntry.LastCheck = DateTime.Now;
                    newNotifyEntry.LastSent = (DateTime)newLastSent;
                    guild.Notifys.Remove(guild.Notifys.Find(n => n.GetHashCode() == notifyHashCode));
                    guild.Notifys.Add(newNotifyEntry);
                    notifyHashCode = newNotifyEntry.GetHashCode();
                    _data.UpdateGuildSettings(guild.GuildId, guild);
                    SentNotify(posts, newNotifyEntry.Info);
                    typingState.Dispose();
                }
            }, notify.Info, DataManager.BotSettings.NotifyDueTime, notify.Info.UpdatePeriod * MILLISECONDS_PER_MINUTE);
            task.TaskStarted += (t) =>
            {
                RaiseLog(LogSeverity.Debug, $"Notify task started. TaskId={t.Id}");
            };
            task.TaskEnded += (t, result) =>
            {
                var severety = LogSeverity.Debug;
                if (result == TaskStatus.Canceled || result == TaskStatus.Faulted)
                {
                    severety = LogSeverity.Warning;
                }
                RaiseLog(severety, $"Notify task ended. TaskId={t.Id} Result={result.ToString()}");
            };
            if (!_notifys.ContainsKey(guild.GuildId))
            {
                _notifys.Add(guild.GuildId, new List<NotifyTask>());
            }
            _notifys[guild.GuildId].Add(task);
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
            foreach (var guildNotifys in _notifys)
            {
                foreach(var notify in guildNotifys.Value)
                {
                    notify.Start();
                    await Task.Delay(DataManager.BotSettings.StartNotifyDelay);
                }
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
                    OwnerId = notify.Info.SourceId
                });
            }
            else
            {
                posts = _vk.WallPostsSearch(new WallSearchParams
                {
                    Count = Convert.ToInt64(notify.Info.SendsPerNotify),
                    OwnerId = notify.Info.SourceId,
                    Query = notify.Info.SearchString
                });
            }
            
            posts.RemoveAll(p => p.Date <= notify.LastSent);
            posts.Reverse();
            RaiseLog(LogSeverity.Debug, $"WallPosts checked. Domain={notify.Info.SourceDomain} PostsCount={posts.Count}");
            return posts;
        }

        public string GetWallPostLink(Post post)
        {
            return $"{_vk.Domain}wall{post.OwnerId}_{post.Id}";
        }

        private string[] ToDivideText(string text, double maxTextLength)
        {
            var needMessageCount = (int)Math.Ceiling((double)text.Length / DataManager.BotSettings.MessageTextLimit);
            if(needMessageCount != 0)
            {
                var blockLength = text.Length / needMessageCount;
                string[] textBlocks = new string[needMessageCount];
                for (int i = 0, j = 0; i < text.Length && j < needMessageCount; i += blockLength, j++)
                {
                    textBlocks[j] = (text.Substring(i, text.Length - i > blockLength ? blockLength : text.Length - i));
                }
                return textBlocks;
            }
            else
            {
                return new string[0];
            }           
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

        private void SentPhotoUris(ISocketMessageChannel channel, List<object> photos)
        {
            string[] links = new string[photos.Count];
            int index = 0;
            foreach (Photo photo in photos)
            {
                links[index] = GetMaxImageUri(photo).ToString();
                index++;
            }
            if (links.Length <= 5)
            {
                channel.SendMessageAsync(String.Join(Environment.NewLine, links)).Wait();
            }
            else
            {
                string[] firstmsg = new string[5];
                string[] secondmsg = new string[links.Length - 5];
                for (int i = 0; i < links.Length; i++)
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

        private void SentText(ISocketMessageChannel channel, string text)
        {
            foreach (var textBlock in ToDivideText(text, DataManager.BotSettings.MessageTextLimit))
            {
                channel.SendMessageAsync(textBlock).Wait();
                Task.Delay(DataManager.BotSettings.BetweenSentTextDelay).Wait();
            }
            Task.Delay(DataManager.BotSettings.SentTextDelay).Wait();
        }

        private void SentAudioNames(ISocketMessageChannel channel, List<object> audios)
        {
            var sentText = "";
            foreach(Audio audio in audios)
            {
                sentText += $"🎵   {audio.Artist} - {audio.Title}" + Environment.NewLine;
            }
            if(sentText != "")
            {
                channel.SendMessageAsync(sentText).Wait();
                Task.Delay(DataManager.BotSettings.SentAudioDelay).Wait();
            }
        }

        private Embed GetHeader(Post post, NotifyInfo notify, User user)
        {
            var embed = new EmbedBuilder
            {
                ThumbnailUrl = user.Photo200.ToString(),
                Author = new EmbedAuthorBuilder
                {
                    Name = user.FirstName + " " + user.LastName,
                    Url = _vk.Domain + notify.SourceDomain
                },
                Timestamp = post.Date.Value,
                Title = "Link to post",
                Url = _vk.Domain + "wall" + post.OwnerId + "_" + post.Id
            };
            embed.AddField(new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "Likes",
                Value = post.Likes == null ? 0 : post.Likes.Count
            });
            embed.AddField(new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "Reposts",
                Value = post.Reposts == null ? 0 : post.Reposts.Count
            });
            embed.AddField(new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "Comments",
                Value = post.Comments == null ? 0 : post.Comments.Count
            });
            embed.AddField(new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "Views",
                Value = post.Views == null ? 0 : post.Views.Count
            });
            return embed.Build();
        }

        private Embed GetHeader(Post post, NotifyInfo notify, Group group)
        {
            var embed = new EmbedBuilder
            {
                ThumbnailUrl = group.Photo200.ToString(),
                Author = new EmbedAuthorBuilder
                {
                    Name = group.Name,
                    Url = _vk.Domain + notify.SourceDomain
                },
                Timestamp = post.Date.Value,
                Title = "Link to post",
                Url = _vk.Domain + "wall" + post.OwnerId + "_" + post.Id
            };
            embed.AddField(new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "Likes",
                Value = post.Likes == null ? 0 : post.Likes.Count
            });
            embed.AddField(new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "Reposts",
                Value = post.Reposts == null ? 0 : post.Reposts.Count
            });
            embed.AddField(new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "Comments",
                Value = post.Comments == null ? 0 : post.Comments.Count
            });
            embed.AddField(new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "Views",
                Value = post.Views == null ? 0 : post.Views.Count
            });
            return embed.Build();
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
                    if(notify.Type == NotifyType.GroupWall)
                    {
                        var group = _vk.GetGroup((long)posts[0].OwnerId, GroupsFields.AllUndocumented);
                        channel.SendMessageAsync(notify.Comment ?? "", false, GetHeader(post, notify, group)).Wait();
                    }
                    if (notify.Type == NotifyType.UserWall)
                    {
                        var user = _vk.GetUser((long)posts[0].OwnerId, ProfileFields.Photo200);
                        channel.SendMessageAsync(notify.Comment ?? "", false, GetHeader(post, notify, user)).Wait();
                    }

                    Task.Delay(DataManager.BotSettings.SentHeaderDelay).Wait();
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
                        SentText(channel, post.Text);
                    }

                    if (notify.WithAudio)
                    {
                        if (attachments.ContainsKey(typeof(Audio)))
                        {
                            SentAudioNames(channel, attachments[typeof(Audio)]);
                        }
                    }

                    if (notify.WithDocument)
                    {
                        if (attachments.ContainsKey(typeof(Document)))
                        {
                            SentDocuments(channel, attachments[typeof(Document)]);
                        }
                    }

                    if (notify.WithPhoto)
                    {
                        if (attachments.ContainsKey(typeof(Photo)))
                        {
                            SentPhotoUris(channel, attachments[typeof(Photo)]);
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
                }
                Task.Delay(DataManager.BotSettings.SentNotifyDelay).Wait();
            }
            RaiseLog(LogSeverity.Debug, $"Posts notifyed. Domain={notify.SourceDomain} ChannelId={notify.ChannelId} PostsCount={posts.Count}");
        }

        private void SentDocuments(ISocketMessageChannel channel, List<object> list)
        {
            throw new NotImplementedException();
        }
    }
}
