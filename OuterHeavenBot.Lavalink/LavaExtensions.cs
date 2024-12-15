using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Lavalink
{
    public static class LavaExtensions
    {
        public static async Task FireEventAsync<TEvent, TArgs>(this Func<TEvent, TArgs,Task>? func, TEvent sender, TArgs? args)
        {
            if (func == null || sender == null)
                return;

            var delegates = func.GetInvocationList();

            var tasks = new Task[delegates.Length];
            for (int i = 0; i < tasks.Length; i++)
            {
                var task = ((Func<TEvent, TArgs, Task>)delegates[i])(sender, args);
                tasks[i] = task;
            }

            await Task.WhenAll(tasks);
        }
    }
}
