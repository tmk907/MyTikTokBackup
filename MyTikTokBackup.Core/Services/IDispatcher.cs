using System;
using System.Threading.Tasks;

namespace MyTikTokBackup.Core.Services
{
    public interface IDispatcher
    {
        Task RunAsync(Func<Task> task);
        Task RunAsync(Action action);
        void Run(Action action);
    }
}
