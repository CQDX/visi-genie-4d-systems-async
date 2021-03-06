﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;

namespace ViSiGenie4DSystems.Async.Event
{
    /// <summary>
    /// Based on Tim Greenfield's Blog, "Custom Async Events in C#", September 3, 2015
    /// Reference https://programmerpayback.com/2015/09/03/custom-async-events-in-c/
    /// </summary>
    public sealed class ReportEventArgs
    {
        private readonly List<TaskCompletionSource<object>> _taskCompletionSources;

        public ReportEventArgs()
        {
            _taskCompletionSources = new List<TaskCompletionSource<object>>();
        }

        public Deferral GetDeferral()
        {
            var tcs = new TaskCompletionSource<object>();
            var deferral = new Deferral(() => tcs.SetResult(null));
            _taskCompletionSources.Add(tcs);
            return deferral;
        }

        public IAsyncAction DeferAsync()
        {
            return Task.WhenAll(_taskCompletionSources.Select(tcs => tcs.Task)).AsAsyncAction();
        }
    }
}
