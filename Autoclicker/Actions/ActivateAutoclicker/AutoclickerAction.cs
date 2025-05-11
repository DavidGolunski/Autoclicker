using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Payloads;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace Autoclicker {

    [PluginActionId("com.davidgolunski.autoclicker.autoclickeraction")]
    public class AutoclickerAction : KeyAndEncoderBase {

        // to distinguish between a dial press and a "rotate press"
        private bool dialWasRotated = false;

        private readonly AutoclickerSettings localSettings;


        public AutoclickerAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
            if(payload.Settings == null || payload.Settings.Count == 0) {
                this.localSettings = new AutoclickerSettings();
                SaveSettings();
            }
            else {
                this.localSettings = payload.Settings.ToObject<AutoclickerSettings>();
            }


            Autoclicker.Instance.OnStatusChanged += UpdateVisuals;
            Connection.OnPropertyInspectorDidAppear += OnPropertyInspectorOpened;
            UpdateVisuals();
        }

        public override void Dispose() {
            /*
            if(Autoclicker.Instance.IsRunning && Autoclicker.Instance.ActivatedActionIndex == index) {
                Autoclicker.Instance.Stop();
            }*/

            Autoclicker.Instance.OnStatusChanged -= UpdateVisuals;
            Connection.OnPropertyInspectorDidAppear -= OnPropertyInspectorOpened;
        }



        public override void KeyPressed(KeyPayload payload) {
            ToggleAutoclicker();
        }

        public override void KeyReleased(KeyPayload payload) { }


        public override void DialRotate(DialRotatePayload payload) {
            bool wasActive = IsActive();

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

            if(Autoclicker.Instance.IsRunning && wasActive) {
                Autoclicker.Instance.Delay = delay;
            }

            UpdateVisuals();
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
            bool wasActive = IsActive();

            Tools.AutoPopulateSettings(localSettings, payload.Settings);


            if(Autoclicker.Instance.IsRunning && wasActive) {
                Autoclicker.Instance.Delay = localSettings.DelayInMilliseconds;
                Autoclicker.Instance.SelectedButton = localSettings.SelectedButton;
                Autoclicker.Instance.AllowMovement = localSettings.AllowMovement;
            }

            UpdateVisuals();

            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        #region Private Methods

        // checks if this actions settings are the same as the current Autoclicker settings
        private bool IsActive() {
            return localSettings.DelayInMilliseconds == Autoclicker.Instance.Delay
                && localSettings.AllowMovement == Autoclicker.Instance.AllowMovement
                && localSettings.SelectedButton == Autoclicker.Instance.SelectedButton;
        }

        private void ToggleAutoclicker() {
            // if autoclicker is not running, start it
            if(!Autoclicker.Instance.IsRunning) {
                Autoclicker.Instance.Delay = localSettings.DelayInMilliseconds;
                Autoclicker.Instance.SelectedButton = localSettings.SelectedButton;
                Autoclicker.Instance.AllowMovement = localSettings.AllowMovement;
                Autoclicker.Instance.Start();
                return;
            }

            // if autoclicker is running and the autoclicker was activated by this action, then stop the autoclicker
            if(IsActive()) {
                Autoclicker.Instance.Stop();
                return;
            }

            // Autoclicker was already running, but it was started by a different action. Update the delay and activationIndex
            Autoclicker.Instance.Delay = localSettings.DelayInMilliseconds;
            Autoclicker.Instance.SelectedButton = localSettings.SelectedButton;
            Autoclicker.Instance.AllowMovement = localSettings.AllowMovement;

        }

        // this method is subsribed to the Autoclicker Status Changed event and gets called automatically
        private async void UpdateVisuals() {
            string imageFilePath;

            if(!Autoclicker.Instance.IsRunning) {
                Connection.SetStateAsync(0).GetAwaiter().GetResult();
                imageFilePath = "./Actions/ActivateAutoclicker/Click.png";
            }
            else if(IsActive()) {
                Connection.SetStateAsync(2).GetAwaiter().GetResult();
                imageFilePath = "./Actions/ActivateAutoclicker/Click Selected Active.png";
            }
            else {
                Connection.SetStateAsync(1).GetAwaiter().GetResult();
                imageFilePath = "./Actions/ActivateAutoclicker/Click Active.png";
            }

            Bitmap img = ImageTools.GetBitmapFromFilePath(imageFilePath);
            string imageString = Tools.ImageToBase64(img, true);
            img.Dispose();
            Dictionary<string, string> dkv = new Dictionary<string, string> {
                ["delay"] = localSettings.DelayInMilliseconds.ToString(),
                ["button"] = localSettings.SelectedButtonString,
                ["icon"] = imageString
            };
            await Connection.SetFeedbackAsync(dkv);
        }

        private Task SaveSettings() {
            return Connection.SetSettingsAsync(JObject.FromObject(localSettings));
        }

        private void OnPropertyInspectorOpened(object sender, SDEventReceivedEventArgs<PropertyInspectorDidAppear> e) {
            Connection.SetSettingsAsync(JObject.FromObject(localSettings));
        }

        #endregion
    }
}