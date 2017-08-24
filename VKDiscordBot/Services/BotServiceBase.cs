using System;
using System.Threading.Tasks;
using Discord;

namespace VKDiscordBot.Services
{
	public abstract class BotServiceBase
    {
        public event Func<LogMessage, Task> Log;

        protected void RaiseLog(LogSeverity severity, string message, Exception exception = null)
        {
            Log?.Invoke(new LogMessage(severity, GetType().Name, message, exception));
        }
    }
}