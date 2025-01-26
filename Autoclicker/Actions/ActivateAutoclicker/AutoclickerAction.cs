using BarRaider.SdTools;
using BarRaider.SdTools.Payloads;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Autoclicker {

    [PluginActionId("Autoclicker.autoclickeraction")]
    public class AutoclickerAction : KeyAndEncoderBase {

        private class AutoclickerSettings {

            [JsonProperty(PropertyName = "delayString")]
            public string DelayString { get; set; }
            public int DelayInMilliseconds { get; set; }

            [JsonProperty(PropertyName = "selectedButton")]
            public string SelectedButton { get; set; }

            public AutoclickerSettings() {
                DelayString = "1";
            }

        }


        private readonly AutoclickerSettings localSettings;

        public AutoclickerAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
            if(payload.Settings == null || payload.Settings.Count == 0) {
                this.localSettings = new AutoclickerSettings();
                SaveSettings();
            }
            else {
                this.localSettings = payload.Settings.ToObject<AutoclickerSettings>();
            }
        }

        public override void Dispose() {}

        public override void KeyPressed(KeyPayload payload) {
            ToggleAutoclicker();
        }

        public override void KeyReleased(KeyPayload payload) { }


        public override void DialRotate(DialRotatePayload payload) {

            localSettings.DelayInMilliseconds += payload.Ticks;
            if(localSettings.DelayInMilliseconds < 0) {
                localSettings.DelayInMilliseconds = 0;
            }

            SaveSettings();
        }

        public override void DialDown(DialPayload payload) {
            ToggleAutoclicker();
        }

        public override void DialUp(DialPayload payload) { }

        public override void TouchPress(TouchpadPressPayload payload) {
            ToggleAutoclicker();
        }

        public override void OnTick() { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload) {
            Tools.AutoPopulateSettings(localSettings, payload.Settings);
            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        #region Private Methods

        private void ToggleAutoclicker() {
            if(Autoclicker.Instance.IsRunning) {
                Autoclicker.Instance.Stop();
            }
            else {
                Autoclicker.Instance.Start();
            }
        }

        private void UpdateVisuals() {

        }

        private Task SaveSettings() {
            return Connection.SetSettingsAsync(JObject.FromObject(localSettings));
        }

        #endregion
    }
}