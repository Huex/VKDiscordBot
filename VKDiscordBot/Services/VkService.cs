using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using VkNet;
using VkNet.Utils.AntiCaptcha;

namespace VKDiscordBot.Services
{
    public class VkService : BotServiceBase
    {
        private readonly VkApi _vkApi;
        private readonly string _pathToAuthParams;

        public VkService(string pathToAuthParams)
        {
            _pathToAuthParams = pathToAuthParams;
            _vkApi = new VkApi();
            RaiseLog(Discord.LogSeverity.Verbose, "Ready");
        }

        public bool IsAuthorized
        {
            get
            {
               return _vkApi.IsAuthorized;
            }
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
