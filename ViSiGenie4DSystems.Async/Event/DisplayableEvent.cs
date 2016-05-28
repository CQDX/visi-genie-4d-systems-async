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
        }

        public event EventHandler<DeferrableDisplayEventArgs> DisplayEvent;

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
