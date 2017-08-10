using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VKDiscordBot.Models
{
    public class RepetitiveTask
    {
        /// <summary>
        /// Возвращает id повторяющиеся задачи RepetitiveTask
        /// </summary>
        public event Action<int> TaskStarted;

        public event Action<TaskStatus> TaskEnded;

        public Action Action { get; private set; }

        public int DueTime { get; private set; }

        public int Period { get; private set; }

        public RepeririveTaskState State { get; private set; }

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
            State = RepeririveTaskState.TimerStopped;
            DueTime = dueTime;
            Period = period;
            Action = action;
            TaskStarted += (id) => State = RepeririveTaskState.TaskStarted;
            TaskEnded += (status) =>
            {
                if (State != RepeririveTaskState.TimerStopped)
                {
                    State = RepeririveTaskState.WaitingTimerTrip;
                }
            };
            _timer = new Timer((sender) =>
            {
                TaskStarted?.Invoke(Id);
                Task.Factory.StartNew(action).ContinueWith((s) => 
                {
                    TaskEnded?.Invoke(s.Status);
                });
            }, null, Timeout.Infinite, Timeout.Infinite);                      
        }

        public void Start()
        {
            _timer.Change(DueTime, Period);
            State = RepeririveTaskState.WaitingTimerTrip;
        }

        public void Stop()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            State = RepeririveTaskState.TimerStopped;
        }

        public void Change(int dueTime, int period)
        {
            DueTime = dueTime;
            Period = period;
            if(DueTime == Timeout.Infinite || Period == Timeout.Infinite)
            {
                State = RepeririveTaskState.TimerStopped;
            }
            _timer.Change(dueTime, period);
        }
    }
}
