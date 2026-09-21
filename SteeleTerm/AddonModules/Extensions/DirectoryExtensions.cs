namespace SteeleTerm.AddonModules.Extensions
{
	static class DirectoryExtensions
	{
		internal enum SpecialDirectory
		{
			Desktop = 0,
			Programs = 2,
			MyDocuments = 5, // aka Personal
			Favorites = 6,
			Startup = 7,
			Recent = 8,
			SendTo = 9,
			StartMenu = 11,
			MyMusic = 13,
			MyVideos = 14,
			DesktopDirectory = 16,
			MyComputer = 17,
			NetworkShortcuts = 19,
			Fonts = 20,
			Templates = 21,
			CommonStartMenu = 22,
			CommonPrograms = 23,
			CommonStartup = 24,
			CommonDesktopDirectory = 25,
			ApplicationData = 26,
			PrinterShortcuts = 27,
			LocalApplicationData = 28,
			InternetCache = 32,
			Cookies = 33,
			History = 34,
			CommonApplicationData = 35,
			Windows = 36,
			System = 37,
			ProgramFiles = 38,
			MyPictures = 39,
			UserProfile = 40,
			SystemX86 = 41,
			ProgramFilesX86 = 42,
			CommonProgramFiles = 43,
			CommonProgramFilesX86 = 44,
			CommonTemplates = 45,
			CommonDocuments = 46,
			CommonAdminTools = 47,
			AdminTools = 48,
			CommonMusic = 53,
			CommonPictures = 54,
			CommonVideos = 55,
			Resources = 56,
			LocalizedResources = 57,
			CommonOemLinks = 58,
			CDBurning = 59
		}
		internal static string GetCurrentDirectoryName() => Path.GetFileName(Directory.GetCurrentDirectory());
		internal static string GetCurrentDirectoryPath() => Directory.GetCurrentDirectory();
		internal static string GetSpecialDirectoryPath(SpecialDirectory dir) => Environment.GetFolderPath((Environment.SpecialFolder)dir);
	}
}