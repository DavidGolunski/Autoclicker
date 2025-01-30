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

        // shows if the autoclicker is currently running
        public bool IsRunning { get => cancellationTokenSource != null; }
        // keeps track of which action has instructed the autoclicker to start. Null value == -1
        private int activatedActionIndex;
        public int ActivatedActionIndex {
            get {
                return activatedActionIndex;
            }
            set {
                activatedActionIndex = value;
                OnStatusChanged(IsRunning, activatedActionIndex);
            }
        }



        // Delay in milliseconds between clicks
        private int delay;
        public int Delay { // this was made an Mutable Instance Variable on purpose. When used in the clicker task, it will be passed by reference and not by value, meaning if the Delay is changed here, it will also be changed inside the task
            get => delay;
            set => delay = value;
        }

        // Task and cancellation token for controlling the click loop
        private CancellationTokenSource cancellationTokenSource;


        // event handler 
        public delegate void StatusChangedHandler(bool isRunning, int delay);
        private event StatusChangedHandler StatusChanged;

        // Import mouse event functionality from the Windows API
        [DllImport("user32.dll", SetLastError = true)]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        public Autoclicker() {
            activatedActionIndex = -1;
            Delay = 1;
            cancellationTokenSource = null;
        }


        /*
         * Event Listener functions
         */
        public void AddStatusChangedListener(StatusChangedHandler listener) {
            StatusChanged += listener;
        }

        public void RemoveStatusChangedListener(StatusChangedHandler listener) {
            StatusChanged -= listener;
        }

        protected void OnStatusChanged(bool isRunning, int activatedActionIndex) {
            StatusChanged?.Invoke(isRunning, activatedActionIndex);
        }




        /*
         * Task Functions
         */
        public void Start(int activatedActionIndex) {
            if(IsRunning) {
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;

            Task.Run(async () =>
            {
                while(!token.IsCancellationRequested) {
                    Update();
                    Logger.Instance.LogMessage(TracingLevel.DEBUG, "Delay: " + Delay);
                    await Task.Delay(Delay, token);
                }
            }, token);

            this.ActivatedActionIndex = activatedActionIndex; // also calls the OnStatusChanged function

            Logger.Instance.LogMessage(TracingLevel.INFO, "Autoclicker Started.");
            
        }

        public void Stop() {
            if(!IsRunning) {
                return;
            }
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;

            this.ActivatedActionIndex = -1; // also calls the OnStatusChanged function

            Logger.Instance.LogMessage(TracingLevel.INFO, "Autoclicker stopped.");
        }

        public void Update() {
            //PerformClick();
        }

        private void PerformClick() {
            // Simulate mouse left click
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }
    }

}
