using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        [JsonProperty(PropertyName = "selectedButton")]
        public string SelectedButton { get; set; }

        public AutoclickerSettings() {
            DelayString = "1";
            SelectedButton = "LMB";
        }
    }
}
