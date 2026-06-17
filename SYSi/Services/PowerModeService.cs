namespace SYSi.Services
{
    public static class PowerModeService
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetProcessInformation(
            IntPtr hProcess,
            PROCESS_INFORMATION_CLASS processInformationClass,
            ref PROCESS_POWER_THROTTLING_STATE processInformation,
            uint processInformationSize);

        private enum PROCESS_INFORMATION_CLASS
        {
            ProcessPowerThrottling = 4
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_POWER_THROTTLING_STATE
        {
            public uint Version;
            public uint ControlMask;
            public uint StateMask;
        }

        private const uint PROCESS_POWER_THROTTLING_CURRENT_VERSION = 1;
        private const uint PROCESS_POWER_THROTTLING_EXECUTION_SPEED = 0x1;
        private const uint PROCESS_POWER_THROTTLING_IGNORE_TIMER_RESOLUTION = 0x4;

        public enum PowerModeState
        {
            /// <summary>
            /// Full refresh rate, no throttling. App is in foreground and active.
            /// </summary>
            Normal,

            /// <summary>
            /// Reduced refresh rate, EcoQoS enabled. App is minimized or in background.
            /// </summary>
            Efficiency,

            /// <summary>
            /// Minimal refresh rate, EcoQoS + lower process priority.
            /// App has been idle/background for an extended period, or system is on battery saver.
            /// </summary>
            EfficiencyAdvanced
        }

        public delegate void PowerModeChangedEventHandler(PowerModeState oldMode, PowerModeState newMode);

        public static event PowerModeChangedEventHandler? PowerModeChanged;

        public static PowerModeState CurrentPowerModeState = PowerModeState.Normal;

        public static void SetPowerMode(PowerModeState mode)
        {
            if (CurrentPowerModeState == mode)
            {
                return;
            }

            var oldMode = CurrentPowerModeState;
            CurrentPowerModeState = mode;

            var throttlingFlags = PROCESS_POWER_THROTTLING_EXECUTION_SPEED | PROCESS_POWER_THROTTLING_IGNORE_TIMER_RESOLUTION;

            var state = new PROCESS_POWER_THROTTLING_STATE
            {
                Version = PROCESS_POWER_THROTTLING_CURRENT_VERSION,
                ControlMask = throttlingFlags,
                StateMask = mode != PowerModeState.Normal ? throttlingFlags : 0
            };

            using var process = Process.GetCurrentProcess();
            process.PriorityClass = mode switch
            {
                PowerModeState.Normal => ProcessPriorityClass.Normal,
                PowerModeState.Efficiency => ProcessPriorityClass.BelowNormal,
                PowerModeState.EfficiencyAdvanced => ProcessPriorityClass.Idle,
                _ => ProcessPriorityClass.Normal
            };

            SetProcessInformation(
                process.Handle,
                PROCESS_INFORMATION_CLASS.ProcessPowerThrottling,
                ref state,
                (uint)Marshal.SizeOf(state));

            PowerModeChanged?.Invoke(oldMode, mode);
        }
    }
}