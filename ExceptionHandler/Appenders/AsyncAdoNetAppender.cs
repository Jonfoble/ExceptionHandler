using log4net.Appender;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ExceptionHandler.Appenders
{
    public class AsyncAdoNetAppender : AdoNetAppender
    {
        private Queue<LoggingEvent> pendingTasks;
        private readonly object lockObject = new object();
        private readonly ManualResetEvent manualResetEvent;
        private bool onClosing;

        public AsyncAdoNetAppender()
        {
            try
            {
                pendingTasks = new Queue<LoggingEvent>();
                manualResetEvent = new ManualResetEvent(false);
                Start();
            }
            catch (Exception)
            { }
        }

        protected override void Append(LoggingEvent[] loggingEvents)
        {
            try
            {
                foreach (LoggingEvent loggingEvent in loggingEvents)
                    Append(loggingEvent);
            }
            catch (Exception)
            { }
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            try
            {
                if (FilterEvent(loggingEvent))
                    Enqueue(loggingEvent);
            }
            catch (Exception)
            { }
        }

        private void Start()
        {
            try
            {
                if (!onClosing)
                {
                    Thread thread = new Thread(LogMessages);
                    thread.Start();
                }
            }
            catch (Exception)
            { }

        }

        private void LogMessages()
        {
            try
            {
                LoggingEvent loggingEvent;
                while (!onClosing)
                {
                    while (!DeQueue(out loggingEvent))
                    {
                        Thread.Sleep(10);

                        if (onClosing)
                            break;
                    }
                    if (loggingEvent != null)
                    {
                        base.Append(loggingEvent);
                    }
                }

                manualResetEvent.Set();
            }
            catch (Exception)
            { }
        }
        
        private void Enqueue(LoggingEvent loggingEvent)
        {
            try
            {
                lock (lockObject)
                {
                    pendingTasks.Enqueue(loggingEvent);
                }
            }
            catch (Exception)
            { }
        }

        private bool DeQueue(out LoggingEvent loggingEvent)
        {
            lock (lockObject)
            {
                if (pendingTasks.Count > 0)
                {
                    loggingEvent = pendingTasks.Dequeue();
                    return true;
                }
                else
                {
                    loggingEvent = null;
                    return false;
                }
            }
        }

        protected override void OnClose()
        {
            try
            {
                onClosing = true;
                manualResetEvent.WaitOne(TimeSpan.FromSeconds(10));
                base.OnClose();
            }
            catch (Exception)
            { }
        }
    }
}
