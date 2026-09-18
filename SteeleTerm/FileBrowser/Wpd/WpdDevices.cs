using System.Runtime.InteropServices;
using Windows.Win32.Devices.PortableDevices;
using Windows.Win32.Foundation;
namespace SteeleTerm.FileBrowser.Wpd
{
	 class WpdDevices
	{
        unsafe delegate void GetDevicesDelegate(PWSTR* buffer, ref uint count);
        internal static unsafe List<(string deviceID, string deviceName)> GetAllDevices()
		{
			var deviceList = new List<(string deviceID, string deviceName)>();
			IPortableDeviceManager deviceManager;
			try { deviceManager = PortableDeviceManagerFactory.Create(); } catch (COMException ex) { Console.WriteLine($"WPD: PortableDeviceManagerFactory.Create failed (0x{ex.HResult:X8})"); return deviceList; }
			try { deviceManager.RefreshDeviceList(); } catch (COMException ex) { Console.WriteLine($"WPD: RefreshDeviceList failed (0x{ex.HResult:X8})"); }
			List<string> deviceTypes = ["GetDevices", "GetPrivateDevices"];
			string[] publicIDs = [];
			string[] privateIDs = [];
			foreach (var deviceType in deviceTypes)
			{
				var getDevices = deviceType == "GetDevices" ? (GetDevicesDelegate)getPublic : getPrivate;
				uint requiredDeviceCount = 0;
				try { getDevices(null, ref requiredDeviceCount); } catch (COMException ex) { Console.WriteLine($"WPD: {deviceType} probe failed (0x{ex.HResult:X8})"); }
                if (requiredDeviceCount == 0 || requiredDeviceCount > int.MaxValue / IntPtr.Size) return [];
                var bytesToAllocate = checked((int)requiredDeviceCount * IntPtr.Size);
                var bufferPtr = Marshal.AllocHGlobal(bytesToAllocate);
				try
				{
					var deviceCount = requiredDeviceCount;
					try { getDevices((PWSTR*)bufferPtr, ref deviceCount); } catch { return []; }
					if (deviceType == "GetDevices") publicIDs = WpdHelpers.PointerArrayToList(bufferPtr, deviceCount);
					else { privateIDs = WpdHelpers.PointerArrayToList(bufferPtr, deviceCount); }
				}
				finally { Marshal.FreeHGlobal(bufferPtr); }
			}
			string[] allIDs = [.. publicIDs, .. privateIDs];
			if (allIDs.Length == 0) return deviceList;
			var allNames = WpdHelpers.GetDeviceNamesList(deviceManager, allIDs);
			var minCount = Math.Min(allIDs.Length, allNames.Length);
			for (var i = 0; i < minCount; i++) deviceList.Add((allIDs[i], allNames[i]));
			return deviceList;
			void getPrivate(PWSTR* b, ref uint c) => deviceManager.GetPrivateDevices(b, ref c);
			void getPublic(PWSTR* b, ref uint c) => deviceManager.GetDevices(b, ref c);
		}
	}
}