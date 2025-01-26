using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autoclicker {

    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Runtime.InteropServices;
    using BarRaider.SdTools;

    public class Autoclicker {

        private static Autoclicker instance;
        public static Autoclicker Instance {
            get => instance ??= new Autoclicker();
            private set => instance = value;
        }


        public bool IsRunning { get => cancellationTokenSource != null; }

        // Delay in milliseconds between clicks
        public int Delay { get; set; }

        // Task and cancellation token for controlling the click loop
        private Task clickTask;
        private CancellationTokenSource cancellationTokenSource;

        // Import mouse event functionality from the Windows API
        [DllImport("user32.dll", SetLastError = true)]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        public Autoclicker() {
            Delay = 1;
            clickTask = null;
            cancellationTokenSource = null;
        }

        public void Start() {
            if(clickTask != null && !clickTask.IsCompleted) {
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;

            clickTask = Task.Run(async () =>
            {
                while(!token.IsCancellationRequested) {
                    Update();
                    await Task.Delay(Delay, token);
                }
            }, token);

            Logger.Instance.LogMessage(TracingLevel.INFO, "Autoclicker Started with a Delay of " + Delay);
        }

        public void Stop() {
            if(cancellationTokenSource != null) {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
                clickTask = null;
                Logger.Instance.LogMessage(TracingLevel.INFO, "Autoclicker stopped.");
            }
        }

        public void Update() {
            PerformClick();
        }

        private void PerformClick() {
            // Simulate mouse left click
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }
    }

}
