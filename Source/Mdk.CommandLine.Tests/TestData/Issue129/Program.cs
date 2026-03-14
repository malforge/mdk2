using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public partial class Program : MyGridProgram
    {
        readonly Scheduler _scheduler;

        public Program()
        {
            Runtime.UpdateFrequency |= UpdateFrequency.Update100;
            _scheduler = new Scheduler(this);
            _scheduler.AddScheduledAction(() => { Echo($"Hello World! It is {DateTime.Now}"); }, 0.1);
        }

        public void Main(string argument, UpdateType updateSource)
        {
            _scheduler.Update();
        }
    }

    public class Scheduler
    {
        public double CurrentTimeSinceLastRun { get; private set; }
        public long CurrentTicksSinceLastRun { get; private set; }

        QueuedAction _currentlyQueuedAction;
        bool _firstRun = true;
        bool _inUpdate;

        readonly bool _ignoreFirstRun;
        readonly List<ScheduledAction> _actionsToAdd = new List<ScheduledAction>();
        readonly List<ScheduledAction> _scheduledActions = new List<ScheduledAction>();
        readonly List<ScheduledAction> _actionsToDispose = new List<ScheduledAction>();
        readonly Queue<QueuedAction> _queuedActions = new Queue<QueuedAction>();
        readonly Program _program;

        public const long TicksPerSecond = 60;
        public const double TickDurationSeconds = 1.0 / TicksPerSecond;
        const long ClockTicksPerGameTick = 166666L;

        public Scheduler(Program program, bool ignoreFirstRun = false)
        {
            _program = program;
            _ignoreFirstRun = ignoreFirstRun;
        }

        public void Update()
        {
            _inUpdate = true;
            var deltaTicks = Math.Max(0, _program.Runtime.TimeSinceLastRun.Ticks / ClockTicksPerGameTick);

            if (_firstRun)
            {
                if (_ignoreFirstRun)
                    deltaTicks = 0;

                _firstRun = false;
            }

            _actionsToDispose.Clear();
            foreach (var action in _scheduledActions)
            {
                CurrentTicksSinceLastRun = action.TicksSinceLastRun + deltaTicks;
                CurrentTimeSinceLastRun = action.TimeSinceLastRun + deltaTicks * TickDurationSeconds;
                action.Update(deltaTicks);
                if (action.JustRan && action.DisposeAfterRun)
                    _actionsToDispose.Add(action);
            }

            if (_actionsToDispose.Count > 0)
                _scheduledActions.RemoveAll(x => _actionsToDispose.Contains(x));

            if (_currentlyQueuedAction == null && _queuedActions.Count != 0)
                _currentlyQueuedAction = _queuedActions.Dequeue();

            if (_currentlyQueuedAction != null)
            {
                _currentlyQueuedAction.Update(deltaTicks);
                if (_currentlyQueuedAction.JustRan)
                {
                    if (!_currentlyQueuedAction.DisposeAfterRun)
                        _queuedActions.Enqueue(_currentlyQueuedAction);

                    _currentlyQueuedAction = null;
                }
            }

            _inUpdate = false;

            if (_actionsToAdd.Count > 0)
            {
                _scheduledActions.AddRange(_actionsToAdd);
                _actionsToAdd.Clear();
            }
        }

        public void AddScheduledAction(Action action, double updateFrequency, bool disposeAfterRun = false, double timeOffset = 0)
        {
            var scheduledAction = new ScheduledAction(action, updateFrequency, disposeAfterRun, timeOffset);
            if (!_inUpdate)
                _scheduledActions.Add(scheduledAction);
            else
                _actionsToAdd.Add(scheduledAction);
        }

        public void AddScheduledAction(ScheduledAction scheduledAction)
        {
            if (!_inUpdate)
                _scheduledActions.Add(scheduledAction);
            else
                _actionsToAdd.Add(scheduledAction);
        }

        public void AddQueuedAction(Action action, double updateInterval, bool removeAfterRun = false)
        {
            if (updateInterval <= 0)
                updateInterval = 0.001;

            var scheduledAction = new QueuedAction(action, updateInterval, removeAfterRun);
            _queuedActions.Enqueue(scheduledAction);
        }

        public void AddQueuedAction(QueuedAction scheduledAction)
        {
            _queuedActions.Enqueue(scheduledAction);
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
        public bool JustRan { get; private set; }
        public bool DisposeAfterRun { get; private set; }
        public double TimeSinceLastRun
        {
            get
            {
                return TicksSinceLastRun * Scheduler.TickDurationSeconds;
            }
        }
        public long TicksSinceLastRun { get; private set; }

        public double RunInterval
        {
            get
            {
                return RunIntervalTicks * Scheduler.TickDurationSeconds;
            }
            set
            {
                RunIntervalTicks = (long)Math.Round(value * Scheduler.TicksPerSecond);
            }
        }

        public long RunIntervalTicks
        {
            get
            {
                return _runIntervalTicks;
            }
            set
            {
                if (value == _runIntervalTicks)
                    return;

                _runIntervalTicks = value < 0 ? 0 : value;
                _runFrequency = value == 0 ? double.MaxValue : Scheduler.TicksPerSecond / _runIntervalTicks;
            }
        }

        public double RunFrequency
        {
            get
            {
                return _runFrequency;
            }
            set
            {
                if (value == _runFrequency)
                    return;

                if (value == 0)
                    RunIntervalTicks = long.MaxValue;
                else
                    RunIntervalTicks = (long)Math.Round(Scheduler.TicksPerSecond / value);
            }
        }

        long _runIntervalTicks;
        double _runFrequency;
        readonly Action _action;

        public ScheduledAction(Action action, double runFrequency, bool removeAfterRun = false, double timeOffset = 0)
        {
            _action = action;
            RunFrequency = runFrequency;
            DisposeAfterRun = removeAfterRun;
            TicksSinceLastRun = (long)Math.Round(timeOffset * Scheduler.TicksPerSecond);
        }

        public void Update(long deltaTicks)
        {
            TicksSinceLastRun += deltaTicks;

            if (TicksSinceLastRun >= RunIntervalTicks)
            {
                _action.Invoke();
                TicksSinceLastRun = 0;
                JustRan = true;
            }
            else
            {
                JustRan = false;
            }
        }
    }
}
