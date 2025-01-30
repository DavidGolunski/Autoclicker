using BarRaider.SdTools;
using BarRaider.SdTools.Payloads;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Autoclicker {

    [PluginActionId("com.davidgolunski.autoclicker.autoclickeraction")]
    public class AutoclickerAction : KeyAndEncoderBase {

        // this int is used to identify the autoclicker action.
        // everytime this class gets instantated it takes the current number and increments the counter by 1
        private static int indexCounter = 0;

        private readonly int index;


        // to distinguish between a dial press and a "rotate press"
        private bool dialWasRotated = false;

        private readonly AutoclickerSettings localSettings;

        


        public AutoclickerAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
            this.index = indexCounter++;

            if(payload.Settings == null || payload.Settings.Count == 0) {
                this.localSettings = new AutoclickerSettings();
                SaveSettings();
            }
            else {
                this.localSettings = payload.Settings.ToObject<AutoclickerSettings>();
            }

            Autoclicker.Instance.AddStatusChangedListener(UpdateVisuals);
            UpdateVisuals(Autoclicker.Instance.IsRunning, Autoclicker.Instance.Delay);
        }

        public override void Dispose() {
            if(Autoclicker.Instance.IsRunning && Autoclicker.Instance.ActivatedActionIndex == index) {
                Autoclicker.Instance.Stop();
            }

            Autoclicker.Instance.RemoveStatusChangedListener(UpdateVisuals);
        }



        public override void KeyPressed(KeyPayload payload) {
            ToggleAutoclicker();
        }

        public override void KeyReleased(KeyPayload payload) { }


        public override void DialRotate(DialRotatePayload payload) {
            dialWasRotated = true;

            int stepSize = payload.IsDialPressed ? 10 : 1;

            int delay = localSettings.DelayInMilliseconds;
            delay += payload.Ticks * stepSize;

            if(delay < 0) {
                delay = 0;
            }
            if(delay > 9999) {
                delay = 9999;
            }

            localSettings.DelayString = delay.ToString();

            if(Autoclicker.Instance.IsRunning && Autoclicker.Instance.ActivatedActionIndex == index) {
                Autoclicker.Instance.Delay = delay;
            }

            UpdateVisuals(Autoclicker.Instance.IsRunning, Autoclicker.Instance.ActivatedActionIndex);

            SaveSettings();
        }

        public override void DialDown(DialPayload payload) {
            dialWasRotated = false;
        }

        public override void DialUp(DialPayload payload) {
            if(dialWasRotated)
                return;
            ToggleAutoclicker();
        }

        public override void TouchPress(TouchpadPressPayload payload) {
            ToggleAutoclicker();
        }

        public override void OnTick() { }




        public override void ReceivedSettings(ReceivedSettingsPayload payload) {
            Tools.AutoPopulateSettings(localSettings, payload.Settings);

            if(Autoclicker.Instance.IsRunning && Autoclicker.Instance.ActivatedActionIndex == index) {
                Autoclicker.Instance.Delay = localSettings.DelayInMilliseconds;
            }
            UpdateVisuals(Autoclicker.Instance.IsRunning, Autoclicker.Instance.ActivatedActionIndex);

            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        #region Private Methods

        private void ToggleAutoclicker() {

            // if autoclicker is not running, start it
            if(!Autoclicker.Instance.IsRunning) {
                Autoclicker.Instance.Delay = localSettings.DelayInMilliseconds;
                Autoclicker.Instance.Start(this.index);
                return;
            }

            // if autoclicker is running and the autoclicker was activated by this action, then stop the autoclicker
            if(Autoclicker.Instance.ActivatedActionIndex == this.index) {
                Autoclicker.Instance.Stop();
                return;
            }

            // Autoclicker was already running, but it was started by a different action. Update the delay and activationIndex
            Autoclicker.Instance.Delay = localSettings.DelayInMilliseconds;
            Autoclicker.Instance.ActivatedActionIndex = this.index;
            
        }

        // this method is subsribed to the Autoclicker Status Changed event and gets called automatically
        private async void UpdateVisuals(bool isRunning, int activationIndex) {
            if(!isRunning) {
                Connection.SetStateAsync(0).GetAwaiter().GetResult();

                Bitmap img = ImageTools.GetBitmapFromFilePath("./Actions/ActivateAutoclicker/Click.png");
                string imageString = Tools.ImageToBase64(img, true);
                img.Dispose();

                Dictionary<string, string> dkv = new Dictionary<string, string> {
                    ["delay"] = localSettings.DelayInMilliseconds.ToString(),
                    ["button"] = localSettings.SelectedButton,
                    ["icon"] = imageString
                };
                await Connection.SetFeedbackAsync(dkv);

                return;
            }

            if(this.index == activationIndex) {
                Connection.SetStateAsync(2).GetAwaiter().GetResult();

                Bitmap img = ImageTools.GetBitmapFromFilePath("./Actions/ActivateAutoclicker/Click Selected Active.png");
                string imageString = Tools.ImageToBase64(img, true);
                img.Dispose();

                Dictionary<string, string> dkv = new Dictionary<string, string> {
                    ["delay"] = localSettings.DelayInMilliseconds.ToString(),
                    ["button"] = localSettings.SelectedButton,
                    ["icon"] = imageString
                };
                await Connection.SetFeedbackAsync(dkv);
                return;
            }
            else {
                Connection.SetStateAsync(1).GetAwaiter().GetResult();

                Bitmap img = ImageTools.GetBitmapFromFilePath("./Actions/ActivateAutoclicker/Click Active.png");
                string imageString = Tools.ImageToBase64(img, true);
                img.Dispose();

                Dictionary<string, string> dkv = new Dictionary<string, string> {
                    ["delay"] = localSettings.DelayInMilliseconds.ToString(),
                    ["button"] = localSettings.SelectedButton,
                    ["icon"] = imageString
                };
                await Connection.SetFeedbackAsync(dkv);
                return;
            }

            
        }

        private Task SaveSettings() {
            return Connection.SetSettingsAsync(JObject.FromObject(localSettings));
        }

        #endregion
    }
}