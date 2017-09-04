using System;
using System.Threading;
using System.Threading.Tasks;

namespace VKDiscordBot.Models
{
    public class RepetitiveTask : IDisposable
    {
        private Action _action;
        private Timer _timer;

        public event Action<RepetitiveTask> TaskStarted;
        public event Action<RepetitiveTask, TaskStatus> TaskEnded;
        public RepeririveTaskState State { get; private set; }
        public int Id => _timer.GetHashCode();

        private int _dueTime;
        public int DueTime
        {
            get
            {
                return _dueTime;
            }
            private set
            {
                _dueTime = value;
                State = value == Timeout.Infinite ? RepeririveTaskState.Stopped : State;
            }
        }

        private int _period;
        public int Period
        {
            get
            {
                return _period;
            }
            private set
            {
                _period = value;
                State = value == Timeout.Infinite ? RepeririveTaskState.Stopped : State;
            }
        }

        public RepetitiveTask(Action action, int dueTime, int period)
        {
            State = RepeririveTaskState.Stopped;
            DueTime = dueTime;
            Period = period;
            _action = action;
            TaskStarted += (task) => State = RepeririveTaskState.TaskStarted;
            TaskEnded += (task, status) => State = State != RepeririveTaskState.Stopped ? RepeririveTaskState.Waiting : State;
            _timer = new Timer((sender) =>              
            {
                TaskStarted?.Invoke(this);
                Task.Factory.StartNew(action).ContinueWith((task) => 
                {
                    TaskEnded?.Invoke(this,task.Status);
                });             
            }, null, Timeout.Infinite, Timeout.Infinite);                      
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        public void Start()
        {
            _timer.Change(DueTime, Period);
            State = RepeririveTaskState.Waiting;
        }

        public void Stop()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            State = RepeririveTaskState.Stopped;
        }

        public void Change(int dueTime, int period)
        {
            DueTime = dueTime;
            Period = period;
            _timer.Change(DueTime, Period);
        }
    }
}
