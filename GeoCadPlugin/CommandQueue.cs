using System.Collections.Concurrent;

namespace GeoCadPlugin
{
    public static class CommandQueue
    {
        private static readonly ConcurrentQueue<Action> queue =
            new();


        public static void Enqueue(Action action)
        {
            queue.Enqueue(action);
        }

        public static void Execute()
        {
            while (queue.TryDequeue(out var action))
            {
                action();
            }
        }
    }
}
