using System;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public partial class Program : MyGridProgram
    {
        readonly Scheduler _scheduler = new Scheduler();

        public void Main(string argument, UpdateType updateSource)
        {
            _scheduler.AddScheduledAction(() => { }, 0.1);
        }
    }

    public class Scheduler
    {
        public void AddScheduledAction(Action action, double updateFrequency, bool disposeAfterRun = false, double timeOffset = 0)
        {
            var scheduledAction = new QueuedAction(action, updateFrequency, disposeAfterRun);
        }

        public void AddScheduledAction(QueuedAction scheduledAction)
        {
        }
    }

    public class QueuedAction : ScheduledAction
    {
        public QueuedAction(Action action, double runInterval, bool removeAfterRun = false)
            : base(action, 1.0 / runInterval, removeAfterRun, 0)
        {
        }
    }

    public class ScheduledAction
    {
        public ScheduledAction(Action action, double runFrequency, bool removeAfterRun = false, double timeOffset = 0)
        {
        }
    }
}
