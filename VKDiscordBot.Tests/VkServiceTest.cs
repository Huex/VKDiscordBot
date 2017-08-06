using Microsoft.VisualStudio.TestTools.UnitTesting;
using VKDiscordBot.Services;
using System.Threading.Tasks;


namespace VKDiscordBot.Tests
{
    [TestClass]
    public class VkServiceTest
    {
        private VkService _vk;

        [TestInitialize]
        public void InitService()
        {
            _vk = new VkService("Configurations/Secrets/VkAuthParams.json");
        }

        [TestMethod]
        public void VkService_AuthorizeAsync()
        {
            var message = "";
            _vk.Log += (p) =>
            {
                message = p.Exception?.Message;
                return Task.CompletedTask;
            };
            _vk.AuthorizeAsync().GetAwaiter().GetResult();
            Assert.IsTrue(_vk.IsAuthorized, message);
        }
    }
}
