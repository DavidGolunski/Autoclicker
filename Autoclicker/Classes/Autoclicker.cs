using System;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;
using BarRaider.SdTools;
using System.Drawing;


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

        // Task and cancellation token for controlling the click loop
        private CancellationTokenSource cancellationTokenSource;

        // shows if the autoclicker is currently running
        public bool IsRunning { get => cancellationTokenSource != null; }


        // event handler, actions subscribe to this to get notified if something changes. They use the information to change icons
        public event Action OnStatusChanged; // parameters: isRunning, activeActionIndex



        // Delay in milliseconds between clicks
        private int delay;
        public int Delay { // this was made an Mutable Instance Variable on purpose. When used in the clicker task, it will be passed by reference and not by value, meaning if the Delay is changed here, it will also be changed inside the task
            get {
                DelayMutex.WaitOne();
                int delay = this.delay;
                DelayMutex.ReleaseMutex();
                return delay;
            }
            set {
                DelayMutex.WaitOne();
                if(delay == value) {
                    DelayMutex.ReleaseMutex();
                    return;
                }
                delay = value;
                DelayMutex.ReleaseMutex();

                OnStatusChanged?.Invoke();
            }
        }
        private readonly Mutex DelayMutex = new Mutex();

        // Bool to help keep track wether the button is currently being hold down
        // we have to keep the variable outside of the task, since when the task is requested to cancel it will not perform the "release" action
        private bool isHolding = false;
        private bool IsHolding {
            get => isHolding;
            set => isHolding = value;
        }

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
                if(wasRunning) {
                    Stop();
                }
                selectedMouseButton = value;
                if(wasRunning) {
                    Start();
                }

                OnStatusChanged?.Invoke();
            }
        }

        /*
         * The option of following the mouse or having a fixed positon of the clicks. 
         */
        private bool allowMovement;
        public bool AllowMovement {
            get {
                AllowMovementMutex.WaitOne();
                bool allowMovement = this.allowMovement;
                AllowMovementMutex.ReleaseMutex();
                return allowMovement;
            }
            set {
                AllowMovementMutex.WaitOne();
                if(allowMovement == value) {
                    AllowMovementMutex.ReleaseMutex();
                    return;
                }

                if(allowMovement && !value) {
                    // We're switching from dynamic to fixed position; save current position
                    if(GetCursorPos(out POINT p)) {
                        fixedClickPosition = new Point(p.X, p.Y);
                    }
                }
                allowMovement = value;

                AllowMovementMutex.ReleaseMutex();
                OnStatusChanged?.Invoke();
            }
        }
        private readonly Mutex AllowMovementMutex = new Mutex();


        private Point fixedClickPosition;

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT {
            public int X;
            public int Y;
        }


        public Autoclicker() {
            delay = 1;
            SelectedButton = MouseButton.LMB;
            allowMovement = true;
            cancellationTokenSource = null;
        }

        /*
         * Task Functions
         */
        public void Start() {
            if(IsRunning) {
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;

            if(!AllowMovement) {
                if(GetCursorPos(out POINT p)) {
                    fixedClickPosition = new Point(p.X, p.Y);
                }
            }

            Task.Run(async () => {
                
                while(!token.IsCancellationRequested) {
                    int delay = Delay;
                    if(delay > 0) {
                        // If we were previously holding, release the button
                        if(IsHolding) {
                            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                            IsHolding = false;
                        }

                        PerformClick(); // Normal clicking behavior
                        await Task.Delay(delay, token);
                    }
                    else if(delay == 0 && !IsHolding) {
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

            OnStatusChanged?.Invoke();
            Logger.Instance.LogMessage(TracingLevel.INFO, "Autoclicker started. Button: " + SelectedButton.ToString() + " Delay: " + Delay.ToString() + " AllowMovement: " + AllowMovement.ToString());
        }

        public void Stop() {
            if(!IsRunning) {
                return;
            }

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;


            // Ensure button is released when stopping and the button was being held down before
            if(IsHolding) 
                ReleaseButton();

            OnStatusChanged?.Invoke();
            Logger.Instance.LogMessage(TracingLevel.INFO, "Autoclicker stopped. Button: " + SelectedButton.ToString() + " Delay: " + Delay.ToString() + " AllowMovement: " + AllowMovement.ToString());
        }

        /*
         * Click functions
         */
        private void PressButton() { mouse_event(GetButtonDownFlag(), 0, 0, 0, UIntPtr.Zero); }
        private void ReleaseButton() { mouse_event(GetButtonUpFlag(), 0, 0, 0, UIntPtr.Zero); }

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
            if(!AllowMovement)
                SetCursorPos(fixedClickPosition.X, fixedClickPosition.Y);

            // Simulate mouse click
            PressButton();
            Task.Delay(1).Wait();
            ReleaseButton();
        }
    }
}
