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

            [JsonProperty(PropertyName = "delay")]
            public int Delay { get; set; }

            public AutoclickerSettings() {
                Delay = 1;
            }

        }


        private AutoclickerSettings localSettings;

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
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Dial Rotated: {payload.Ticks}");

            localSettings.Delay += payload.Ticks;
            if(localSettings.Delay < 0) {
                localSettings.Delay = 0;
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

        private Task SaveSettings() {
            return Connection.SetSettingsAsync(JObject.FromObject(localSettings));
        }

        #endregion
    }
}