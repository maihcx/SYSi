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

        public static async Task OptimizeAfterStartupAsync(
            int delayMilliseconds = 5000)
        {
            await Task.Delay(delayMilliseconds);

            await OptimizeAsync();
        }
    }
}
