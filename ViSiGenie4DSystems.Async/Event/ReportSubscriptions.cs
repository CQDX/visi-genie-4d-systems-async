using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ViSiGenie4DSystems.Async.Message;

namespace ViSiGenie4DSystems.Async.Event
{
    public class ReportSubscriptions
    {
        private List<EventHandler<ReportEventArgs>> ActiveDelegates { get; set; }

        private event EventHandler<ReportEventArgs> ReportEvent;

        public ReportSubscriptions()
        {
            this.ReportEvent = null;
            this.ActiveDelegates = new List<EventHandler<ReportEventArgs>>();
        }

        public void Add(EventHandler<ReportEventArgs> handler)
        {
            this.ReportEvent += handler;
            ActiveDelegates.Add(handler);
        }

        public void Remove(EventHandler<ReportEventArgs> handler)
        {
            this.ReportEvent -= handler;
            ActiveDelegates.Remove(handler);
        }

        public void RemoveAll()
        {
            foreach (var eventHandler in this.ActiveDelegates)
            {
                ReportEvent -= eventHandler;
            }
            this.ActiveDelegates.Clear();
        }

        public async Task Raise(ReadMessage readMessage)
        {
            if (this.ReportEvent != null)
            {
                var args = new ReportEventArgs();
                this.ReportEvent(readMessage, args);
                await args.DeferAsync();
            }
        }
    }
}
