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

        public void Remove(RepetitiveTask task)
        {
            if (_tasks.Contains(task))
            {
                _tasks.Remove(task);
                RaiseLog(Discord.LogSeverity.Verbose, $"Removed repetitive task: {task.Id}");
            }
        }

        public void Remove(int taskId)
        {
            var task = _tasks.Find(t => t.Id == taskId);
            if (task != null)
            {
                Remove(task);
            }
        }

        public RepetitiveTask Find(Predicate<RepetitiveTask> match)
        {
            return _tasks.Find(match);
        }
    }
}
