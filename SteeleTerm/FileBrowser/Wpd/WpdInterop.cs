using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
namespace SteeleTerm.FileBrowser
{
    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
    [Guid("A1567595-4C2F-4574-A6FA-ECEF917B9A40")]
    public partial interface IPortableDeviceManager
    {
        void GetDevices(nint deviceIDsBuffer, ref uint deviceIDsCharCount);
        void RefreshDeviceList();
        void GetDeviceFriendlyName(string deviceID, nint nameBuffer, ref uint nameCharCount);
        void GetDeviceDescription(string deviceID, nint descriptionBuffer, ref uint descriptionCharCount);
        void GetDeviceManufacturer(string deviceID, nint manufacturerBuffer, ref uint manufacturerCharCount);
        void GetDeviceProperty(string deviceID, string propertyName, nint dataBuffer, ref uint byteCount, ref uint dataType);
        void GetPrivateDevices(nint deviceIDsBuffer, ref uint deviceIDsCharCount);
    }
    internal static class PortableDeviceManagerFactory
    {
        private static readonly Guid CLSID_PortableDeviceManager = new("0AF10CEC-2ECD-4B92-9581-34F6AE0637F3");
        internal static IPortableDeviceManager Create()
        {
            var type = Type.GetTypeFromCLSID(CLSID_PortableDeviceManager);
            return type == null ? throw new COMException("Failed to get type from CLSID for PortableDeviceManager.") : (IPortableDeviceManager)Activator.CreateInstance(type)!;
        }
    }
}