using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
namespace SteeleTerm.FileBrowser.Wpd
{
	[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)] [Guid("625e2df8-6392-4cf0-9ad1-3cfa5f17775c")] public partial interface IPortableDevice
	{
		[PreserveSig] int Open(string pszPnPDeviceID, IPortableDeviceValues? pClientInfo);
		[PreserveSig] int SendCommand(uint dwFlags, IPortableDeviceValues pParameters, out IPortableDeviceValues ppResults);
		[PreserveSig] int Content(out IPortableDeviceContent ppContent);
		[PreserveSig] int Capabilities(out IPortableDeviceCapabilities ppCapabilities);
		[PreserveSig] int Cancel();
		[PreserveSig] int Close();
		[PreserveSig] int Advise(uint dwFlags, IPortableDeviceEventCallback pCallback, IPortableDeviceValues pParameters, out string ppszCookie);
		[PreserveSig] int Unadvise(string pszCookie);
		[PreserveSig] int GetPnPDeviceID(out string ppszPnPDeviceID);
	}
	[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)] [Guid("2c8c6dbf-e3dc-4061-becc-8542e810d126")] public partial interface IPortableDeviceCapabilities
	{
		[PreserveSig] int GetSupportedCommands(out IPortableDeviceKeyCollection ppCommands);
		[PreserveSig] int GetCommandOptions(ref Guid Command, out IPortableDeviceValues ppOptions);
		[PreserveSig] int GetFunctionalCategories(out IPortableDevicePropVariantCollection ppCategories);
		[PreserveSig] int GetFunctionalObjects(ref Guid Category, out IPortableDevicePropVariantCollection ppObjectIDs);
		[PreserveSig] int GetSupportedContentTypes(ref Guid Category, out IPortableDevicePropVariantCollection ppContentTypes);
		[PreserveSig] int GetSupportedFormats(ref Guid ContentType, out IPortableDevicePropVariantCollection ppFormats);
		[PreserveSig] int GetSupportedFormatProperties(ref Guid Format, out IPortableDeviceKeyCollection ppKeys);
		[PreserveSig] int GetFixedPropertyAttributes(ref Guid Format, ref Guid Key, out IPortableDeviceValues ppAttributes);
		[PreserveSig] int Cancel();
		[PreserveSig] int GetSupportedEvents(out IPortableDevicePropVariantCollection ppEvents);
		[PreserveSig] int GetEventOptions(ref Guid Event, out IPortableDeviceValues ppOptions);
	}
	[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)] [Guid("6a96ed84-7c73-4480-9938-bf5af477d426")] public partial interface IPortableDeviceContent
	{
		[PreserveSig] int EnumObjects(uint dwFlags, string pszParentObjectID, IPortableDeviceValues? pFilter, out IEnumPortableDeviceObjectIDs ppEnum);
		[PreserveSig] int Properties(out IPortableDeviceProperties ppProperties);
		[PreserveSig] int Transfer(out IPortableDeviceResources ppResources);
		[PreserveSig] int CreateObjectWithPropertiesOnly(IPortableDeviceValues pValues, out string ppszObjectID);
		[PreserveSig] int CreateObjectWithPropertiesAndData(IPortableDeviceValues pValues, out nint ppData, ref uint pdwOptimalWriteBufferSize, out string ppszCookie);
		[PreserveSig] int Delete(uint dwOptions, IPortableDevicePropVariantCollection pObjectIDs, ref IPortableDevicePropVariantCollection ppResults);
		[PreserveSig] int GetObjectIDsFromPersistentUniqueIDs(IPortableDevicePropVariantCollection pPersistentUniqueIDs, out IPortableDevicePropVariantCollection ppObjectIDs);
		[PreserveSig] int Cancel();
		[PreserveSig] int Move(IPortableDevicePropVariantCollection pObjectIDs, string pszDestinationFolderObjectID, ref IPortableDevicePropVariantCollection ppResults);
		[PreserveSig] int Copy(IPortableDevicePropVariantCollection pObjectIDs, string pszDestinationFolderObjectID, ref IPortableDevicePropVariantCollection ppResults);
	}
	[GeneratedComInterface] [Guid("A8792A31-F385-493C-A893-40F64EB45F6E")] public partial interface IPortableDeviceEventCallback { }             // Stub interface referenced but not needed
	[GeneratedComInterface] [Guid("DADA2357-E0AD-492E-98DB-DD61C53BA353")] public partial interface IPortableDeviceKeyCollection { }             // Stub interface referenced but not needed
	[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)] [Guid("A1567595-4C2F-4574-A6FA-ECEF917B9A40")] public partial interface IPortableDeviceManager
	{
		[PreserveSig] int GetDevices(nint deviceIDsBuffer, ref uint deviceIDsCount);
		void RefreshDeviceList();
		[PreserveSig] int GetDeviceFriendlyName(string deviceID, nint nameBuffer, ref uint nameCharCount);
		[PreserveSig] int GetDeviceDescription(string deviceID, nint descriptionBuffer, ref uint descriptionCharCount);
		[PreserveSig] int GetDeviceManufacturer(string deviceID, nint manufacturerBuffer, ref uint manufacturerCharCount);
		[PreserveSig] int GetDeviceProperty(string deviceID, string propertyName, nint dataBuffer, ref uint byteCount, ref uint dataType);
		[PreserveSig] int GetPrivateDevices(nint deviceIDsBuffer, ref uint deviceIDsCharCount);
	}
	[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)] [Guid("10ece955-cf41-4728-bfa0-41eedf1bbf19")] public partial interface IEnumPortableDeviceObjectIDs
	{
		[PreserveSig] int Next(uint cObjects, out string pObjIDs, ref uint pcFetched);
		[PreserveSig] int Skip(uint cObjects);
		[PreserveSig] int Reset();
		[PreserveSig] int Clone(out IEnumPortableDeviceObjectIDs ppEnum);
		[PreserveSig] int Cancel();
	}
	[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)] [Guid("7f6d695c-03df-4439-a809-59266beee3a6")] public partial interface IPortableDeviceProperties
	{
		[PreserveSig] int GetSupportedProperties(string pszObjectID, out IPortableDeviceKeyCollection ppKeys);
		[PreserveSig] int GetPropertyAttributes(string pszObjectID, ref Guid Key, out IPortableDeviceValues ppAttributes);
		[PreserveSig] int GetValues(string pszObjectID, IPortableDeviceKeyCollection? pKeys, out IPortableDeviceValues ppValues);
		[PreserveSig] int SetValues(string pszObjectID, IPortableDeviceValues pValues, out IPortableDeviceValues ppResults);
		[PreserveSig] int Delete(string pszObjectID, IPortableDeviceKeyCollection pKeys);
		[PreserveSig] int Cancel();
	}
	[GeneratedComInterface] [Guid("89B2E422-4F1B-4316-BCEF-A44AFEA83EB3")] public partial interface IPortableDevicePropVariantCollection { }	// Stub interface referenced but not needed
	[GeneratedComInterface] [Guid("FD8878AC-D841-4D17-891C-E6829CDB6934")] public partial interface IPortableDeviceResources { }				// Stub interface referenced but not needed
	[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)] [Guid("6848F6F2-3155-4F86-B6F5-263EEEAB3143")] public partial interface IPortableDeviceValues
	{
		[PreserveSig] int GetCount(ref uint pcelt);
		[PreserveSig] int GetAt(uint index, ref PropertyKey pKey, nint pValue);
		[PreserveSig] int SetValue(ref PropertyKey key, nint pValue);
		[PreserveSig] int GetValue(ref PropertyKey key, nint pValue);
		[PreserveSig] int SetStringValue(ref PropertyKey key, string Value);
		[PreserveSig] int GetStringValue(ref PropertyKey key, out string pValue);
		[PreserveSig] int SetUnsignedIntegerValue(ref PropertyKey key, uint Value);
		[PreserveSig] int GetUnsignedIntegerValue(ref PropertyKey key, out uint pValue);
		[PreserveSig] int SetSignedIntegerValue(ref PropertyKey key, int Value);
		[PreserveSig] int GetSignedIntegerValue(ref PropertyKey key, out int pValue);
		[PreserveSig] int SetUnsignedLargeIntegerValue(ref PropertyKey key, ulong Value);
		[PreserveSig] int GetUnsignedLargeIntegerValue(ref PropertyKey key, out ulong pValue);
		[PreserveSig] int SetSignedLargeIntegerValue(ref PropertyKey key, long Value);
		[PreserveSig] int GetSignedLargeIntegerValue(ref PropertyKey key, out long pValue);
		[PreserveSig] int SetFloatValue(ref PropertyKey key, float Value);
		[PreserveSig] int GetFloatValue(ref PropertyKey key, out float pValue);
		[PreserveSig] int SetErrorValue(ref PropertyKey key, int Value);
		[PreserveSig] int GetErrorValue(ref PropertyKey key, out int pValue);
		[PreserveSig] int SetKeyValue(ref PropertyKey key, ref PropertyKey Value);
		[PreserveSig] int GetKeyValue(ref PropertyKey key, out PropertyKey pValue);
		[PreserveSig] int SetBoolValue(ref PropertyKey key, int Value);
		[PreserveSig] int GetBoolValue(ref PropertyKey key, out int pValue);
		[PreserveSig] int SetIUnknownValue(ref PropertyKey key, nint pValue);
		[PreserveSig] int GetIUnknownValue(ref PropertyKey key, out nint ppValue);
		[PreserveSig] int SetGuidValue(ref PropertyKey key, ref Guid Value);
		[PreserveSig] int GetGuidValue(ref PropertyKey key, out Guid pValue);
		[PreserveSig] int SetBufferValue(ref PropertyKey key, nint pValue, uint cbValue);
		[PreserveSig] int GetBufferValue(ref PropertyKey key, out nint ppValue, out uint pcbValue);
		[PreserveSig] int SetIPortableDeviceValuesValue(ref PropertyKey key, IPortableDeviceValues pValue);
		[PreserveSig] int GetIPortableDeviceValuesValue(ref PropertyKey key, out IPortableDeviceValues ppValue);
		[PreserveSig] int SetIPortableDeviceKeyCollectionValue(ref PropertyKey key, IPortableDeviceKeyCollection pValue);
		[PreserveSig] int GetIPortableDeviceKeyCollectionValue(ref PropertyKey key, out IPortableDeviceKeyCollection ppValue);
		[PreserveSig] int SetIPortableDevicePropVariantCollectionValue(ref PropertyKey key, IPortableDevicePropVariantCollection pValue);
		[PreserveSig] int GetIPortableDevicePropVariantCollectionValue(ref PropertyKey key, out IPortableDevicePropVariantCollection ppValue);
		[PreserveSig] int SetIPortableDeviceValuesCollectionValue(ref PropertyKey key, nint pValue);
		[PreserveSig] int GetIPortableDeviceValuesCollectionValue(ref PropertyKey key, out nint ppValue);
		[PreserveSig] int RemoveValue(ref PropertyKey key);
		[PreserveSig] int CopyValuesFromPropertyStore(nint pStore);
		[PreserveSig] int CopyValuesToPropertyStore(nint pStore);
		[PreserveSig] int Clear();
	}
	[StructLayout(LayoutKind.Sequential)] public struct PropertyKey
	{
		public Guid fmtid;
		public uint pid;
	}
	internal static class PortableDeviceFactory
	{
		private static readonly Guid CLSID_PortableDevice = new("728A21C5-3D9E-48D7-9810-864D719376A4");
		internal static IPortableDevice Create()
		{
			var type = Type.GetTypeFromCLSID(CLSID_PortableDevice);
			return type == null ? throw new COMException("Failed to get type from CLSID for PortableDevice.") : (IPortableDevice)Activator.CreateInstance(type)!;
		}
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