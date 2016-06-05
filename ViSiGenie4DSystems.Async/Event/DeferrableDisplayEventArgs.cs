using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace ViSiGenie4DSystems.Async.Event
{
    /// <summary>
    /// See Custom Async Events in C#
    /// By Tim Greenfield
    /// September 3, 2015
    /// https://programmerpayback.com/2015/09/03/custom-async-events-in-c/
    /// 
    /// </summary>
    public sealed class DeferrableDisplayEventArgs
    {
        readonly List<TaskCompletionSource<object>> taskCompletionSources;

        public DeferrableDisplayEventArgs()
        {
            taskCompletionSources = new List<TaskCompletionSource<object>>();
        }

        public Deferral GetDeferral()
        {
            var tcs = new TaskCompletionSource<object>();
            var deferral = new Deferral(() => tcs.SetResult(null));
            taskCompletionSources.Add(tcs);
            return deferral;
        }

        public IAsyncAction DeferAsync()
        {
            return Task.WhenAll(taskCompletionSources.Select(tcs => tcs.Task)).AsAsyncAction();
        }
    }
}
