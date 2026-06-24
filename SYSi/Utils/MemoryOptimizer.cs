namespace SYSi.Utils
{
    public static class MemoryOptimizer
    {
        private static readonly SemaphoreSlim _lock = new(1, 1);

        [DllImport("psapi.dll")]
        private static extern bool EmptyWorkingSet(IntPtr hProcess);

        public static async Task OptimizeAsync()
        {
            await _lock.WaitAsync();

            try
            {
                await Task.Run(() =>
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    EmptyWorkingSet(Process.GetCurrentProcess().Handle);
                });
            }
            finally
            {
                _lock.Release();
            }
        }

        internal static async Task OptimizeAfterAsync(TimeSpan? delay = null)
        {
            if (!delay.HasValue) {
                delay = TimeSpan.FromSeconds(5);
            }
            await Task.Delay((int)delay.Value.TotalMilliseconds);

            await OptimizeAsync();
        }
    }
}
