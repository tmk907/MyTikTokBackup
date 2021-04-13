using System;
using System.Threading.Tasks;
using Microsoft.System;
using MyTikTokBackup.Core.Services;
using Microsoft.Toolkit.Uwp.Extensions;
using Serilog;

namespace MyTikTokBackup.Desktop.Services
{
    public partial class DispatcherHelper : IDispatcher
    {
        //private readonly Func<DispatcherQueue> _func;
        private readonly DispatcherQueue _dispatcherQueue;

        public DispatcherHelper()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }

        private DispatcherQueue GetDispatcherQueue()
        {
            if (_dispatcherQueue is null)
            {
                Log.Error("dispatcherQueue is null");
            }
            return _dispatcherQueue;
        }

        public void Run(Action action)
        {
            var dispatcherQueue = GetDispatcherQueue();
            if (dispatcherQueue is not null)
            {
                dispatcherQueue.TryEnqueue(() => action());
            }
        }

        public async Task RunAsync(Func<Task> function)
        {
            var dispatcherQueue = GetDispatcherQueue();
            if (dispatcherQueue is not null)
            {
                await dispatcherQueue.EnqueueAsync(function);
            }
        }

        public async Task RunAsync(Action action)
        {
            var dispatcherQueue = GetDispatcherQueue();
            if (dispatcherQueue is not null)
            {
                await dispatcherQueue.EnqueueAsync(action);
            }
        }
    }
}
