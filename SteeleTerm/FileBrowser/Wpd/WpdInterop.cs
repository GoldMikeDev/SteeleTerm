using System.Runtime.InteropServices;
using Windows.Win32.Devices.PortableDevices;
namespace SteeleTerm.FileBrowser.Wpd
{
	 static class PortableDeviceFactory
	{
		internal static readonly Guid CLSID_PortableDevice = new("728A21C5-3D9E-48D7-9810-864D719376A4");
		internal static IPortableDevice Create()
		{
			var type = Type.GetTypeFromCLSID(CLSID_PortableDevice);
			return type == null ? throw new COMException("Failed to get type from CLSID for PortableDevice.") : (IPortableDevice)Activator.CreateInstance(type)!;
		}
	}
	 static class PortableDeviceManagerFactory
	{
		internal static readonly Guid CLSID_PortableDeviceManager = new("0AF10CEC-2ECD-4B92-9581-34F6AE0637F3");
		internal static IPortableDeviceManager Create()
		{
			var type = Type.GetTypeFromCLSID(CLSID_PortableDeviceManager);
			return type == null ? throw new COMException("Failed to get type from CLSID for PortableDeviceManager.") : (IPortableDeviceManager)Activator.CreateInstance(type)!;
		}
	}
}