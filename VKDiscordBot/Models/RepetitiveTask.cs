using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VKDiscordBot.Models
{
    public class RepetitiveTask
    {
        public Task Action { get; private set; }
        public int DueTime { get; private set; }
        public int Period { get; private set; }

        private Timer _timer;

        public RepetitiveTask(Task task, int dueTime, int period)
        {
            DueTime = dueTime;
            Period = period;
            _timer = new Timer((sender) =>
            {
                Action.Start();
            }, null, Timeout.Infinite, Timeout.Infinite);
                      
        }

        public void Start()
        {
            _timer.Change(DueTime, Period);
        }

        public void Stop()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void Change(int dueTime, int period)
        {
            DueTime = dueTime;
            Period = period;
            _timer.Change(dueTime, period);
        }
    }
}
