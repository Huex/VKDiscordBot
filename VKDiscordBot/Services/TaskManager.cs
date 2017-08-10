using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VKDiscordBot.Models;

namespace VKDiscordBot.Services
{
    public class TaskManager : BotServiceBase
    {
        private List<RepetitiveTask> _tasks;

        public TaskManager()
        {
            _tasks = new List<RepetitiveTask>();
        }

        public void Add(RepetitiveTask task)
        {
            task.TaskStarted += (id) => RaiseLog(LogSeverity.Verbose, $"Repetitive task started. Id={id}");
            task.TaskEnded += (status) =>
            {
                LogSeverity severity = LogSeverity.Verbose;
                if (status == TaskStatus.Faulted)
                {
                    severity = LogSeverity.Warning;
                }
                RaiseLog(severity, $"Repetitive task ended. Id={task.Id} Status={status}");
            };
            _tasks.Add(task);
            RaiseLog(LogSeverity.Verbose, $"Added new repetitive task. Id={task.Id}");
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
                RaiseLog(LogSeverity.Verbose, $"Removed repetitive task. Id={task.Id}");
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
