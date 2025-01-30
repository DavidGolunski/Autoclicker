using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;
using BarRaider.SdTools;


namespace Autoclicker {

    public enum MouseButton {
        LMB,
        MMB,
        RMB
    }

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

        // Bool to help keep track wether the button is currently being hold down
        // we have to keep the variable outside of the task, since when the task is requested to cancel it will not perform the "release" action
        private bool isHolding = false;
        private bool IsHolding {
            get => isHolding;
            set => isHolding = value;
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
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;

        private MouseButton selectedMouseButton;
        public MouseButton SelectedButton {
            get => selectedMouseButton;
            set {
                if(selectedMouseButton == value)
                    return;

                // restart the task if the selected mouse button has changed. This ensures any button that was currently used was correclty unselected
                bool wasRunning = IsRunning;
                int currentActivatedActionIndex = ActivatedActionIndex;
                if(wasRunning) {
                    Stop();
                }
                selectedMouseButton = value;
                if(wasRunning) {
                    Start(currentActivatedActionIndex);
                }

            }
        }


        public Autoclicker() {
            activatedActionIndex = -1;
            Delay = 1;
            SelectedButton = MouseButton.LMB;
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
                    if(Delay > 0) {
                        // If we were previously holding, release the button
                        if(IsHolding) {
                            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                            IsHolding = false;
                        }

                        PerformClick(); // Normal clicking behavior
                        await Task.Delay(Delay, token);
                    }
                    else if(Delay == 0 && !IsHolding) {
                        // If delay is 0 and we're not already holding, press the button down
                        PressButton();
                        IsHolding = true;
                    }
                    // wait for 100ms if the button is being hold down, before checking if the settings have changed
                    else {
                        await Task.Delay(100, token);
                    }
                }

            }, token);

            this.ActivatedActionIndex = activatedActionIndex; // also calls the OnStatusChanged function
            Logger.Instance.LogMessage(TracingLevel.INFO, "Autoclicker Started. Button: " + SelectedButton.ToString());
            
        }

        public void Stop() {
            if(!IsRunning) {
                return;
            }
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;


            // Ensure button is released when stopping and the button was being held down before
            if(IsHolding) {
                ReleaseButton();
            }

            this.ActivatedActionIndex = -1; // also calls the OnStatusChanged function

            Logger.Instance.LogMessage(TracingLevel.INFO, "Autoclicker stopped. Button: " + SelectedButton.ToString());
        }

        /*
         * Click functions
         */
        private void PressButton() {
            mouse_event(GetButtonDownFlag(), 0, 0, 0, UIntPtr.Zero);
        }

        private void ReleaseButton() {
            mouse_event(GetButtonUpFlag(), 0, 0, 0, UIntPtr.Zero);
        }

        private uint GetButtonDownFlag() {
            return SelectedButton switch {
                MouseButton.LMB => MOUSEEVENTF_LEFTDOWN,
                MouseButton.MMB => MOUSEEVENTF_MIDDLEDOWN,
                MouseButton.RMB => MOUSEEVENTF_RIGHTDOWN,
                _ => MOUSEEVENTF_LEFTDOWN
            };
        }

        private uint GetButtonUpFlag() {
            return SelectedButton switch {
                MouseButton.LMB => MOUSEEVENTF_LEFTUP,
                MouseButton.MMB => MOUSEEVENTF_MIDDLEUP,
                MouseButton.RMB => MOUSEEVENTF_RIGHTUP,
                _ => MOUSEEVENTF_LEFTUP
            };
        }

        private void PerformClick() {
            // Simulate mouse left click
            PressButton();
            Task.Delay(1).Wait();
            ReleaseButton();
        }
    }

}
