using System.Runtime.InteropServices;
namespace SteeleTerm.FileBrowser.Wpd
{
	internal class WpdDevices
	{
		delegate int GetDevicesDelegate(nint buffer, ref uint count);
		internal static List<(string deviceID, string deviceName)> GetAllDevices()
		{
			var deviceList = new List<(string deviceID, string deviceName)>();
			IPortableDeviceManager deviceManager;
			try { deviceManager = PortableDeviceManagerFactory.Create(); } catch { return deviceList; }
			try { deviceManager.RefreshDeviceList(); } catch { }
			if (deviceManager == null) { return deviceList; }
			List<string> deviceTypes = ["GetDevices", "GetPrivateDevices"];
			int getPublic(nint b, ref uint c) => deviceManager.GetDevices(b, ref c);
			int getPrivate(nint b, ref uint c) => deviceManager.GetPrivateDevices(b, ref c);
			string[] publicIDs = [];
			string[] privateIDs = [];
			foreach (string deviceType in deviceTypes)
			{
				var getDevices = deviceType == "GetDevices" ? (GetDevicesDelegate)getPublic : getPrivate;
				uint requiredDeviceCount = 0;
				try { getDevices(0, ref requiredDeviceCount); } catch { }
				if (requiredDeviceCount == 0 || requiredDeviceCount > int.MaxValue / IntPtr.Size) return [];
				int bytesToAllocate = checked((int)requiredDeviceCount * IntPtr.Size);
				nint bufferPtr = Marshal.AllocHGlobal(bytesToAllocate);
				try
				{
					uint deviceCount = requiredDeviceCount;
					try { getDevices(bufferPtr, ref deviceCount); } catch { return []; }
					if (deviceType == "GetDevices") publicIDs = WpdHelpers.PointerArrayToList(bufferPtr, deviceCount);
					else { privateIDs = WpdHelpers.PointerArrayToList(bufferPtr, deviceCount); }
				}
				finally { Marshal.FreeHGlobal(bufferPtr); }
			}
			string[] allIDs = [.. publicIDs, .. privateIDs];
			if (allIDs.Length == 0) return deviceList;
			string[] allNames = WpdHelpers.GetDeviceNamesList(deviceManager, allIDs);
			int minCount = Math.Min(allIDs.Length, allNames.Length);
			for (int i = 0; i < minCount; i++) deviceList.Add((allIDs[i], allNames[i]));
			return deviceList;
		}
	}
}