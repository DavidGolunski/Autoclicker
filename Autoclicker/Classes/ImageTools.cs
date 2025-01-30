using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autoclicker {
    internal static class ImageTools {

        public static Bitmap GetBitmapFromFilePath(string filePath) {
            if(!File.Exists(filePath)) {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"File not found: {filePath}");
                return null;
            }

            return new Bitmap(filePath);
        }

    }
}
