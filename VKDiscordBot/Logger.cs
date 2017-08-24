using System;
using System.Threading.Tasks;
using Discord;

namespace VKDiscordBot
{
	public class Logger
    {
        public LogSeverity LogLevel { get; private set; }

        public Logger(LogSeverity logLevel)
        {
            LogLevel = logLevel;
        }

        public Task Log(LogMessage logMessage)
        {
            if (LogLevel < logMessage.Severity)
            {
                return Task.CompletedTask;
            }
            var cc = Console.ForegroundColor;
            switch (logMessage.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
                default:
                    Console.ForegroundColor = cc;
                    break;
            }
            Console.WriteLine($"{DateTime.Now,-19} {logMessage.Source}: {logMessage.Message} {logMessage.Exception?.Message}");
            Console.ForegroundColor = cc;
            return Task.CompletedTask;
        }
    }
}
