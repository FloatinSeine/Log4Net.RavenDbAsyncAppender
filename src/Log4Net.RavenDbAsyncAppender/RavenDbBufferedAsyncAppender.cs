using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using log4net.Appender;
using log4net.Core;
using Log4Net.RavenDbAsyncAppender.Store;

namespace Log4Net.RavenDbAsyncAppender
{
    public class RavenDbBufferedAsyncAppender : BufferingAppenderSkeleton
    {
        private readonly List<Task> _activeTasks = new List<Task>();
        private LogEventStore _logEventStore;
        private bool _readyToUse;
        private bool _shuttingDown;
        private int _slidingFlush;
        private DateTime _lastSendBuffer = DateTime.MaxValue;
        private Timer _timer;

        public string ConnectionString { get; set; }
        public int MaxNumberOfRequestsPerSession { get; set; }

        public int SlidingFlush
        {
            get { return _slidingFlush; }
            set
            {
                _slidingFlush = value;
                SetTimerInterval();
            }

        }

        public bool IsAfterSlidingFlushDuration
        {
            get
            {
                var prev = _lastSendBuffer.AddMinutes(SlidingFlush);
                return (prev.CompareTo(DateTime.UtcNow) > 0);
            }
        }

        public override void ActivateOptions()
        {
            base.ActivateOptions();
            _logEventStore = new LogEventStore(ConnectionString) { ErrorHandler = ErrorHandler };

            if (BufferSize > 0) _logEventStore.MaxNumberOfRequestsPerSession = BufferSize;
            if (SlidingFlush > 0)
            {
                _timer = new Timer() { Enabled = true, AutoReset = true };
                SetTimerInterval();
                _timer.Elapsed += OnElapsedCheckSlidingExpiration;
                _timer.Start();
            }
            Fix = FixFlags.Partial;
            _readyToUse = true;
        }

        protected override void OnClose()
        {
            Flush();
            _shuttingDown = true;
            Task.WaitAll(_activeTasks.ToArray());
            _logEventStore.Dispose();
            if (_timer != null)
            {
                _timer.Enabled = false;
                _timer.AutoReset = false;
                _timer.Stop();
            }
            base.OnClose();
        }

        protected override void SendBuffer(LoggingEvent[] events)
        {
            if (events == null) return;
            if (!_readyToUse) HandleError("Sendbuffer", new Exception("RavenDbBufferedAsyncAppender is not ready to use"), ErrorCode.FlushFailure);
            if (_shuttingDown) HandleError("Sendbuffer", new Exception("RavenDbBufferedAsyncAppender is shutting down"), ErrorCode.FlushFailure);

            var task = new Task(() => _logEventStore.Store(events), TaskCreationOptions.LongRunning);
            task.ContinueWith(ContinuationAction);
            _activeTasks.Add(task);
            task.Start();
            _lastSendBuffer = DateTime.UtcNow;
        }

        public void HandleError(string message, Exception ex, ErrorCode code)
        {
            ErrorHandler.Error(message, ex, code);
        }

        private void SetTimerInterval()
        {
            if (_timer != null) _timer.Interval = (_slidingFlush * 1000);
        }

        private void ContinuationAction(Task task)
        {
            _activeTasks.Remove(task);
        }

        private void OnElapsedCheckSlidingExpiration(object sender, ElapsedEventArgs args)
        {
            if (!IsAfterSlidingFlushDuration) return;
            Flush();
        }
    }
}
