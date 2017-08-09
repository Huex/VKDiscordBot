using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using VKDiscordBot.Models;

namespace VKDiscordBot.Services
{
    public class Tasker : BotServiceBase
    {
        private List<RepetitiveTask> _tasks;

        public Tasker()
        {
            _tasks = new List<RepetitiveTask>();
        }

        public void Add(RepetitiveTask task)
        {
            _tasks.Add(task);
            RaiseLog(Discord.LogSeverity.Verbose, $"Added new repetitive task: {task.Id}");
        }

        public void AddAndStart(RepetitiveTask task)
        {
            Add(task);
            task.Start();
        }

        public RepetitiveTask Find(Predicate<RepetitiveTask> match)
        {
            return _tasks.Find(match);
        }
    }
}
