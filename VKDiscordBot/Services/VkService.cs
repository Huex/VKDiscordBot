using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Model;
using VkNet.Model.RequestParams;

namespace VKDiscordBot.Services
{
    public class VkService : BotServiceBase
    {
        private readonly VkApi _vkApi;
        private readonly string _pathToAuthParams;
        public readonly string Domain = "https://vk.com/";

        public VkService(string pathToAuthParams)
        {
            _pathToAuthParams = pathToAuthParams;
            _vkApi = new VkApi
            {
                RequestsPerSecond = 10
            };
        }

        public bool IsAuthorized
        {
            get
            {
               return _vkApi.IsAuthorized;
            }
        }

        public List<Post> GetWallPosts(WallGetParams prms)
        {
            RaiseLog(LogSeverity.Debug, "GetWallPosts request");
            return new List<Post>(_vkApi.Wall.Get(prms).WallPosts);
        }

        public List<NewsSearchResult> NewsFeedSearch(NewsFeedSearchParams prms)
        {
            RaiseLog(LogSeverity.Debug, "NewsFeedSearch request");
            return new List<NewsSearchResult>(_vkApi.NewsFeed.Search(prms));
        }

        public List<Post> WallPostsSearch(WallSearchParams prms)
        {
            RaiseLog(LogSeverity.Debug, "WallPostsSearch request");
            return new List<Post>(_vkApi.Wall.Search(prms));
        }

        public User GetUser(long id, ProfileFields fields)
        {
            RaiseLog(LogSeverity.Debug, "GetUser request");
            return _vkApi.Users.Get(id, fields);
        }

        public Group GetGroup(long id, GroupsFields fields)
        {
            RaiseLog(LogSeverity.Debug, "GetGroup request");
            return _vkApi.Groups.GetById(new string[] { id.ToString() }, id.ToString(), fields)[0]; 
        }

        public VkObject ResolveScreeName(string screeName)
        {
            RaiseLog(LogSeverity.Debug, "ResolveScreeName request");
            return _vkApi.Utils.ResolveScreenName(screeName);
        }

        public async Task AuthorizeAsync()
        {
            try
            {
                await _vkApi.AuthorizeAsync((ApiAuthParams)JsonConvert.DeserializeObject(File.ReadAllText(_pathToAuthParams), typeof(ApiAuthParams)));
            }
            catch (Exception ex)
            {
                RaiseLog(Discord.LogSeverity.Error, "Error of authorization", ex);
                return;
            }
            RaiseLog(Discord.LogSeverity.Info, "Authorized");
        }

    }
}
