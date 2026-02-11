using System.Runtime.InteropServices;
namespace SteeleTerm.FileBrowser.Wpd
{
    internal class WpdHelpers
    {
        internal static string[] GetDeviceIDsList(IPortableDeviceManager deviceManager)
        {
            uint charCount = 0;
            try { deviceManager.GetDevices(0, ref charCount); } catch { }
            if (charCount == 0) return [];
            nint bufferPtr = Marshal.AllocHGlobal((int)charCount * 2);
            try
            {
                uint capacityChars = charCount;
                try { deviceManager.GetDevices(bufferPtr, ref capacityChars); } catch { }
                return MULTI_SZtoString(bufferPtr, capacityChars);
            }
            finally
            {
                Marshal.FreeHGlobal(bufferPtr);
            }
        }
        internal static string[] GetDeviceNamesList()
        {
            return [];
        }
        internal static string[] MULTI_SZtoString(nint bufferPtr, uint charCount)
        {
            List<string> strings = [];
            int offsetChars = 0;
            int limitChars = (int)charCount;
            while (offsetChars < limitChars)
            {
                string s = Marshal.PtrToStringUni(bufferPtr + (offsetChars * 2)) ?? "";
                if (s.Length == 0) break;
                strings.Add(s);
                offsetChars += s.Length + 1;
            }
            return [.. strings];
        }
    }
}