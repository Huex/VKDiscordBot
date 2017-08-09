using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VKDiscordBot.Models
{
    public class RepetitiveTask
    {
        public Action Action { get; private set; }
        public int DueTime { get; private set; }
        public int Period { get; private set; }
        public int Id
        {
            get
            {
                return _timer.GetHashCode();
            }
        }

        private Timer _timer;

        public RepetitiveTask(Action action, int dueTime, int period)
        {
            DueTime = dueTime;
            Period = period;
            Action = action;
            _timer = new Timer((sender) =>
            {
                Task.Factory.StartNew(action);
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
