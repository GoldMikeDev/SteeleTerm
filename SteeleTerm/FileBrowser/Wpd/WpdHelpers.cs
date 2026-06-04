using System.Runtime.InteropServices;
namespace SteeleTerm.FileBrowser.Wpd
{
	internal class WpdHelpers
	{
		internal static string[] GetDeviceNamesList(IPortableDeviceManager deviceManager, string[] deviceIDs)
		{
			List<string> deviceNames = [];
			foreach (string deviceID in deviceIDs)
			{
				uint requiredChars = 0;
				try { deviceManager.GetDeviceFriendlyName(deviceID, 0, ref requiredChars); } catch { }
				if (requiredChars == 0) { deviceNames.Add("Unknown Device"); continue; }
				nint bufferPtr = Marshal.AllocHGlobal((int)requiredChars * 2);
				uint capacityChars = requiredChars;
				try
				{
					deviceManager.GetDeviceFriendlyName(deviceID, bufferPtr, ref capacityChars);
					string deviceName = Marshal.PtrToStringUni(bufferPtr) ?? "Unknown Device";
					deviceNames.Add(deviceName);
				}
				catch { deviceNames.Add("Unknown Device"); }
				finally { Marshal.FreeHGlobal(bufferPtr); }
			}
			return [.. deviceNames];
		}
		internal static string[] PointerArrayToList(nint basePtr, uint count)
		{
			List<string> results = new((int)count);
			int limit = (int)count;
			for (int i = 0; i < limit; i++)
			{
				int offsetBytes = i * IntPtr.Size;
				nint currentPtr = Marshal.ReadIntPtr(basePtr, offsetBytes);
				if (currentPtr == 0) continue;
				try
				{
					string? s = Marshal.PtrToStringUni(currentPtr);
					if (s == null || s.Length == 0) continue;
					results.Add(s);
				}
				finally { Marshal.FreeCoTaskMem(currentPtr); }
			}
			return [.. results];
		}
	}
}