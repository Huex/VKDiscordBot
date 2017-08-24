using System;
using System.Collections.Generic;
using System.Text;

namespace VKDiscordBot.Models
{
	public class NotifyTask : RepetitiveTask
	{
		public readonly NotifyInfo Info;

		public NotifyTask(Action action, NotifyInfo info, int dueTime, int period) : base(action, dueTime, period)
		{
			Info = info;
		}
	}
}
