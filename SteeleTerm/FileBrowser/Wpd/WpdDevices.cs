namespace SteeleTerm.FileBrowser.Wpd
{
    internal class WpdDevices
    {
        internal static List<(string deviceID, string deviceName)> GetDevices()
        {
            var deviceList = new List<(string deviceID, string deviceName)>();
            IPortableDeviceManager deviceManager = PortableDeviceManagerFactory.Create();
            string[] deviceIDs = WpdHelpers.GetDeviceIDsList(deviceManager);
            if (deviceIDs.Length == 0) return deviceList;
            deviceList = [.. WpdHelpers.GetDeviceNamesList().Zip(deviceIDs, (name, id) => (id, name))];
            // code goes here
            return deviceList;
        }
    }
}