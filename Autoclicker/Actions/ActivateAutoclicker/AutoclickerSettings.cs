using Newtonsoft.Json;

namespace Autoclicker {

    internal class AutoclickerSettings {

        [JsonProperty(PropertyName = "delayString")]
        public string DelayString { get; set; }

        public int DelayInMilliseconds {
            get => int.TryParse(DelayString, out int result) ? result : 0;
        }

        [JsonProperty(PropertyName = "realDelayString")]
        public string RealDelayString {
            get => DelayInMilliseconds.ToString() + " ms"; 
        }

        [JsonProperty(PropertyName = "selectedButtonString")]
        public string SelectedButtonString { get; set; }

        public MouseButton SelectedButton {
            get => SelectedButtonString switch {
                "MMB" => MouseButton.MMB,
                "RMB" => MouseButton.RMB,
                _ => MouseButton.LMB,
            };
        }

        [JsonProperty(PropertyName = "allowMovementString")]
        public string AllowMovementString { get; set; } // can either be "allowMovement" or "dontAllowMovement"

        public bool AllowMovement {
            get => AllowMovementString == "allowMovement";
        }

        public AutoclickerSettings() {
            DelayString = "1";
            SelectedButtonString = "LMB";
            AllowMovementString = "allowMovement";
        }
    }
}
