using PortableDeviceApiLib;
namespace SteeleTerm.FileBrowser
{
    internal class WPD
    {
        internal static List<(string deviceID, string deviceName)> GetDevices()
        {
            uint count = 0;
            string idsBuffer = "";
            var devicelist = new List<(string deviceID, string deviceName)>();
            PortableDeviceManager deviceManager = new();
            deviceManager.GetDevices(ref idsBuffer, ref count);
            if (count == 0) return devicelist;
            try { idsBuffer = new string('\0', (int)count); } catch { return devicelist; }
            deviceManager.GetDevices(ref idsBuffer, ref count);
            string[] deviceIDs = idsBuffer.Split('\0', StringSplitOptions.RemoveEmptyEntries);
            foreach (string deviceID in deviceIDs)
            {
                uint nameLength = 0;
                ushort dummy = 0;
                string deviceName;
                try { deviceManager.GetDeviceFriendlyName(deviceID, ref dummy, ref nameLength); } catch { continue; }
                if (nameLength <= 1)
                {
                    deviceName = "Unknown Device";
                    devicelist.Add((deviceID, deviceName));
                    continue;
                }
                ushort[] nameBuf = new ushort[nameLength];
                nameLength = (uint)nameBuf.Length;
                try { deviceManager.GetDeviceFriendlyName(deviceID, ref nameBuf[0], ref nameLength); } catch { continue; }
                int nullIndex = Array.IndexOf(nameBuf, (ushort)0);
                int length = nullIndex >= 0 ? nullIndex : nameBuf.Length;
                char[] chars = new char[length];
                for (int i = 0; i < length; i++) chars[i] = (char)nameBuf[i];
                deviceName = new string(chars);
                devicelist.Add((deviceID, deviceName));
            }
            return devicelist;
        }
    }
}