using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViSiGenie4DSystems.Async.Message;

namespace ViSiGenie4DSystems.Async.Event
{
    public class DisplayableEvent
    {
        public DisplayableEvent()
        {
            this.DisplayEvent = null;
            this.ActiveDelegates = new List<EventHandler<DeferrableDisplayEventArgs>>();
        }

        private List<EventHandler<DeferrableDisplayEventArgs>> ActiveDelegates { get; set; }

        private event EventHandler<DeferrableDisplayEventArgs> DisplayEvent;

        public void Add(EventHandler<DeferrableDisplayEventArgs> handler)
        {
            this.DisplayEvent += handler;
            ActiveDelegates.Add(handler);
        }

        public void Remove(EventHandler<DeferrableDisplayEventArgs> handler)
        {
            this.DisplayEvent -= handler;
            ActiveDelegates.Remove(handler);
        }

        public void RemoveAll()
        {
            foreach (var eventHandler in this.ActiveDelegates)
            {
                DisplayEvent -= eventHandler;
            }
            this.ActiveDelegates.Clear();
        }

        public async Task Raise(ReadMessage readMessage)
        {
            if (this.DisplayEvent != null)
            {
                var args = new DeferrableDisplayEventArgs();
                this.DisplayEvent(readMessage, args);
                await args.DeferAsync();
            }
        }
    }
}
