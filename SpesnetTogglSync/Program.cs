namespace SpesnetTogglSync
{
    internal static class Program
    {
        private const string SingleInstanceMutexName = @"Local\SpesnetTogglSync";
        internal const string ShowWindowEventName = @"Local\SpesnetTogglSync.Show";

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            ApplicationConfiguration.Initialize();

            using var mutex = new Mutex(false, SingleInstanceMutexName);
            var acquired = false;
            try
            {
                try
                {
                    acquired = mutex.WaitOne(TimeSpan.Zero);
                }
                catch (AbandonedMutexException)
                {
                    acquired = true;
                }

                if (!acquired)
                {
                    SignalRunningInstance();
                    return;
                }

                var startInTray = args.Any(arg =>
                    string.Equals(arg, "--tray", StringComparison.OrdinalIgnoreCase));

                using var showWindowEvent = new EventWaitHandle(
                    false,
                    EventResetMode.AutoReset,
                    ShowWindowEventName);

                Application.Run(new TrayApplicationContext(startInTray, showWindowEvent));
            }
            finally
            {
                if (acquired)
                {
                    mutex.ReleaseMutex();
                }
            }
        }

        private static void SignalRunningInstance()
        {
            try
            {
                using var showWindowEvent = EventWaitHandle.OpenExisting(ShowWindowEventName);
                showWindowEvent.Set();
            }
            catch (WaitHandleCannotBeOpenedException)
            {
            }
        }
    }
}
