using Microsoft.Win32;
using SteeleTerm.AddonModules;
using SteeleTerm.FileBrowser.Wpd;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.UI.Shell;
using static SteeleTerm.SteeleTerm;
namespace SteeleTerm.FileBrowser
{
	 class SteeleTermFileBrowser
	{
		static bool sortUnicode;
		[SupportedOSPlatformGuard("windows5.0")] public static bool Win5 => OperatingSystem.IsWindowsVersionAtLeast(5);
		[SupportedOSPlatformGuard("windows5.1.2600")] public static bool Win512600 => OperatingSystem.IsWindowsVersionAtLeast(5, 1, 2600);
		public static string? FileBrowser(string? startDir, bool allowOpen)
        {
			const string promptFileBrowser = " 📂 > ";
			var cwd = startDir ?? "";
			var inThisPc = false;
            Span<PWSTR> ids = stackalloc PWSTR[1];
            if (cwd.Length == 0 || !Directory.Exists(cwd)) cwd = Directory.GetCurrentDirectory();
			while (true)
			{
				var currentDir = Directory.GetCurrentDirectory();
				string[] dirs = [];
				string[] files = [];
				var items = new List<(bool IsDir, string Name, string FullPath)>();
				if (inThisPc)
				{
					var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					var redirected = Console.IsOutputRedirected;
					var scanTop = Console.CursorTop;
					ConsoleSpinner? scanSpinner = null;
					if (!redirected)
					{
						scanSpinner = new ConsoleSpinner(ConsoleLock, promptFileBrowser, 100, 150);
						if (Console.CursorLeft != 0) Console.WriteLine("");
						scanSpinner.Start("Scanning drives ");
					}
					else Console.WriteLine($"{promptFileBrowser}Scanning drives...");
					DriveInfo[] drives = DriveInfo.GetDrives();
					var uncCache = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
					List<(string deviceID, string deviceName)> wpd = WpdDevices.GetAllDevices();
					foreach (var di in drives)
					{
						var root = di.Name;
						var drive = root.TrimEnd('\\');
						if (drive is [_, ':', ..]) drive = char.ToUpperInvariant(drive[0]) + ":";
						var label = "";
						try { label = GetVolumeLabel(di); }
						catch
						{
							if (!Win512600) { break; }
							unsafe
							{
								SHFILEINFOW shfi = default;
								fixed (char* rootChar = root) { _ = PInvoke.SHGetFileInfo(rootChar, (FILE_FLAGS_AND_ATTRIBUTES)0, &shfi, (uint)Marshal.SizeOf<SHFILEINFOW>(), SHGFI_FLAGS.SHGFI_DISPLAYNAME); }
								label = new string(shfi.szDisplayName.AsSpan()).Trim();
							}
						}
						if (di.DriveType != DriveType.Network)
						{
							var disp = label.Length == 0 ? $"({drive})" : $"({drive}) {label}";
							items.Add((true, disp, root));
							continue;
						}
						var disp2 = label.Length == 0 ? $"({drive})" : $"({drive}) {label}";
						if (!uncCache.TryGetValue(drive, out var unc))
						{
							string? uncResult = null;
							var t = new Thread(() => { try { uncResult = TryGetUncForDrive(drive); } catch { } }) { IsBackground = true };
							t.Start();
							unc = t.Join(250) ? uncResult : null;
							uncCache[drive] = unc;
						}
						if (!string.IsNullOrEmpty(unc)) disp2 += $" {unc}";
						items.Add((true, disp2, root));
						seen.Add(drive);
					}
					foreach (var (deviceID, deviceName) in wpd)
					{
						var disp = $"(WPD) {deviceName}";
						items.Add((true, disp, "wpd:" + deviceID));
					}
					scanSpinner?.RequestStopAndFlush();
					if (!redirected)
					{
						ClearLine(scanTop);
						Console.SetCursorPosition(0, scanTop);
					}
					try
					{
						using var net = Registry.CurrentUser.OpenSubKey(@"Network");
						if (net != null)
						{
							foreach (var letter in net.GetSubKeyNames())
							{
								using var dk = net.OpenSubKey(letter);
								var unc = dk?.GetValue("RemotePath") as string;
								if (string.IsNullOrEmpty(unc)) continue;
								var drive = letter.Length > 0 ? char.ToUpperInvariant(letter[0]) + ":" : "";
								if (seen.Contains(drive)) continue;
								var disp = $"({drive}) {unc}";
								items.Add((true, disp, unc)); // FullPath is UNC so traversal works
								seen.Add(drive);
							}
						}
					}
					catch { }
				}
				else
				{
					sortUnicode = false;
					if (cwd.StartsWith("wpd:", StringComparison.Ordinal))
					{
						try
						{
							var deviceID = cwd[4..];
							List<(string name, string objID)> dirsWPD = [];
							List<(string name, string objID)> filesWPD = [];
							var device = PortableDeviceFactory.Create();
							device.Open(deviceID, null);
							device.Content(out var content);
							content.EnumObjects(0, "DEVICE", null, out var enumerator);
							content.Properties(out var properties);
                            PROPERTYKEY WPD_OBJECT_NAME = new() { fmtid = new Guid("EF6B490D-5CD8-437A-AFFC-DA8B60EE4A3C"), pid = 4 };
                            PROPERTYKEY WPD_OBJECT_CONTENT_TYPE = new() { fmtid = new Guid("EF6B490D-5CD8-437A-AFFC-DA8B60EE4A3C"), pid = 7 };
                            while (true)
							{
								uint fetched = 0;
                                var hr = enumerator.Next(ids, ref fetched);
								if (hr != 0 || fetched == 0) break;
								var objID = ids[0].ToString();
                                properties.GetValues(objID, null, out var values);
                                values.GetStringValue(WPD_OBJECT_NAME, out var namePwstr);
                                var name = namePwstr.ToString();
                                values.GetGuidValue(WPD_OBJECT_CONTENT_TYPE, out var contentType);
                                var isDir = contentType == new Guid("27E2E392-A111-48E0-AB0C-E17705A05F85");
								if (isDir) { dirsWPD.Add((name, objID)); }
								else { filesWPD.Add((name, objID)); }
							}
							dirs = [.. dirsWPD.Select(d => d.name)];
							files = [.. filesWPD.Select(f => f.name)];
							SortNatural(dirs);
							SortNatural(files);
							List<(string name, string objID)> dirsWPDsort = [];
							List<(string name, string objID)> filesWPDsort = [];
							dirsWPDsort.AddRange(from directory in dirs let objID = dirsWPD[dirsWPD.FindIndex(d => d.name == directory)].objID select (directory, objID));
							filesWPDsort.AddRange(from file in files let objID = filesWPD[filesWPD.FindIndex(f => f.name == file)].objID select (file, objID));
							items.AddRange(dirs.Select((t, d) => (true, t, $"wpd:{deviceID}:{dirsWPDsort[d].objID}")));
							items.AddRange(files.Select((t, f) => (false, t, $"wpd:{deviceID}:{filesWPDsort[f].objID}")));
						}
						catch { Console.WriteLine($"{promptFileBrowser}WPD device error."); var parent = Directory.GetParent(cwd); if (parent != null) { cwd = parent.FullName; continue; } inThisPc = true; continue; }
					}
					else
					{
						try { dirs = Directory.GetDirectories(cwd); files = Directory.GetFiles(cwd); }
						catch { Console.WriteLine($"{promptFileBrowser}Cannot access directory."); var parent = Directory.GetParent(cwd); if (parent != null) { cwd = parent.FullName; continue; } inThisPc = true; continue; }
						SortNatural(dirs);
						SortNatural(files);
						items.AddRange(dirs.Select(t => (true, Path.GetFileName(t), t)));
						items.AddRange(files.Select(t => (false, Path.GetFileName(t), t)));
					}
				}
				var count = items.Count;
				var display = new List<string>(count);
				for (var k = 0; k < count; k++)
				{
					var icon = inThisPc ? (items[k].FullPath.StartsWith("wpd:", StringComparison.Ordinal) ? "📱" : "💽") : (items[k].IsDir ? "📁" : GetFileIcon(items[k].Name));
					var s = $"{k + 1:0000}{icon} {items[k].Name}";
					display.Add(s);
				}
				int consoleWidth;
				try { consoleWidth = Console.WindowWidth; } catch { consoleWidth = 64; }
				consoleWidth = Math.Max(64, consoleWidth);
				var fileBrowserWidth = Math.Max(64, consoleWidth);
				const int itemWidth = 31;
				var cols = Math.Max(2, fileBrowserWidth / 32);
				var total = cols * 32;
				string bar = new('-', total - 1);
				Console.WriteLine();
				var redirectedRender = Console.IsOutputRedirected;
				var renderTop = Console.CursorTop;
				ConsoleSpinner? renderSpinner = null;
				if (!redirectedRender)
				{
					if (Console.CursorLeft != 0) Console.WriteLine("");
					renderTop = Console.CursorTop;
					renderSpinner = new ConsoleSpinner(ConsoleLock, promptFileBrowser, 100, 150);
					renderSpinner.Start(inThisPc ? "Building drive list " : "Building table ");
				}
				var colour = new byte[count];
				if (!inThisPc)
				{
					for (var k = 0; k < count; k++)
					{
						try
						{
							var p = items[k].FullPath;
							if (p.StartsWith(@"\\", StringComparison.Ordinal)) continue;
							var attr = File.GetAttributes(p);
							if ((attr & FileAttributes.Encrypted) != 0) colour[k] = 1;
							else if ((attr & FileAttributes.Compressed) != 0) colour[k] = 2;
						}
						catch { }
					}
				}
				string header;
				if (inThisPc) header = "This PC\\";
				else
				{
					try
					{
						var root = Path.GetPathRoot(cwd);
						switch (string.IsNullOrEmpty(root))
						{
							case false when root!.StartsWith(@"\\", StringComparison.Ordinal):
								header = $@"This PC\[UNC] {cwd}\";
								break;
							case false:
							{
								var di = new DriveInfo(root);
								var drive = di.Name.TrimEnd('\\'); // "C:"
								var fmt = di.IsReady ? di.DriveFormat : "NOTREADY";
								var label = di.IsReady ? (di.VolumeLabel ?? "") : "";
								label = label.Trim();
								header = label.Length == 0 ? $"This PC\\[{fmt}] {cwd}" :  $@"This PC\[{fmt}] {label} {cwd}\";
								break;
							}
							default:
								header = $@"This PC\[PATH] {cwd}\";
								break;
						}
					}
					catch { header = $@"This PC\[FS] {cwd}\"; }
				}
				var pathLine = header.Length > total ? string.Concat(header.AsSpan(0, Math.Max(0, total - 3)), "...") : header;
				var rows = Math.Max(((count + cols - 1) / cols), 16);
				var cellText = new string?[rows, cols];
				var cellIdx = new int[rows, cols];
				for (var r = 0; r < rows; r++)
				{
					for (var c = 0; c < cols; c++)
					{
						var idx = c * rows + r;
						if (idx >= count) break;
						var s = display[idx];
						if (s.Length > itemWidth) s = string.Concat(s.AsSpan(0, Math.Max(0, itemWidth - 3)), "...");
						cellText[r, c] = s.PadRight(itemWidth);
						cellIdx[r, c] = idx;
					}
				}
				renderSpinner?.RequestStopAndFlush();
				if (!redirectedRender)
				{
					ClearLine(renderTop);
					Console.SetCursorPosition(0, renderTop);
				}
				Console.WriteLine(" " + pathLine.PadRight(total - 1));
				Console.WriteLine(" " + bar);
				for (var r = 0; r < rows; r++)
				{
					Console.Write(" ");
					for (var c = 0; c < cols; c++)
					{
						var cell = cellText[r, c];
						if (cell == null) break;
						if (c != 0) Console.Write("|");
						var idx = cellIdx[r, c];
						var old = Console.ForegroundColor;
						Console.ForegroundColor = colour[idx] switch
						{
							1 => ConsoleColor.Green,
							2 => ConsoleColor.Blue,
							_ => Console.ForegroundColor
						};
						Console.Write(cell);
						Console.ForegroundColor = old;
					}
					Console.WriteLine();
				}
				Console.WriteLine(" " + bar);
				Console.WriteLine(" Commands: #### = open/select | b = go up a directory | exit = close file browser | Exit = close SteeleTerm");
				Console.WriteLine();
				if (sortUnicode) { Console.WriteLine("⚠️ Natural sort failed. Falling back to Unicode sort"); Console.WriteLine(); }
                Console.Write(promptFileBrowser);
                var input = ReadToken(promptFileBrowser, "", true, true, true);
				if (input == null) continue;
				input = input.Trim();
				if (input.Length == 0) continue;
				if (string.Equals(input, "Exit", StringComparison.Ordinal)) return "Exit";
				if (string.Equals(input, "exit", StringComparison.Ordinal)) return "exit";
				if (string.Equals(input, "b", StringComparison.Ordinal))
				{
					if (inThisPc) continue;
					var parent = Directory.GetParent(cwd);
					if (parent != null) { cwd = parent.FullName; continue; }
					inThisPc = true;
					continue;
				}
				if (input.Length >= 2 && ((input[0] == '"' && input[^1] == '"') || (input[0] == '\'' && input[^1] == '\''))) input = input[1..^1];
				if (File.Exists(input)) return Path.GetFullPath(input);
				if (Directory.Exists(input)) { cwd = Path.GetFullPath(input); continue; }
				if (!int.TryParse(input, out var n) || n < 1 || n > count) continue;
				if (allowOpen && !items[n - 1].IsDir)
				{
					try { Process.Start(new ProcessStartInfo { FileName = items[n - 1].FullPath, UseShellExecute = true }); }
					catch (Exception ex) { Console.WriteLine($"{promptFileBrowser}Cannot open file: {ex.Message}"); }
					continue;
				}
				if (!items[n - 1].IsDir) return items[n - 1].FullPath;
				cwd = items[n - 1].FullPath; inThisPc = false; continue;
			}
		}
		static string GetVolumeLabel(DriveInfo drive)
		{
			var label = drive.VolumeLabel.Trim();
			return string.IsNullOrEmpty(label) ? throw new NoLabelException() : label;
		}
		public class NoLabelException : Exception { public override string? StackTrace => null; }
		static string? TryGetUncForDrive(string driveLetter)
		{
			if (!Win5) return null;
			try
			{
				Span<char> buffer = stackalloc char[1024];
				var len = (uint)buffer.Length;
				var rc = PInvoke.WNetGetConnection(driveLetter, buffer, ref len);
				if (rc == WIN32_ERROR.NO_ERROR) return new string(buffer[..(int)len]).TrimEnd('\0');
			}
			catch { return null; }
			return null;
		}
        static void SortNatural(string[] input, string[]? output = null)
		{
			if (output == null || ReferenceEquals(output, input)) output = input;
			else if (output.Length != input.Length) throw new ArgumentException("Output array is not the same length as input array.", nameof(output));
			Array.Copy(input, output, input.Length);
			Array.Sort(output, CompareNatural);
		}
		public static int CompareNatural(string a, string b)
		{
			if (!Win512600) return 1;
			try { return PInvoke.StrCmpLogical(a, b); }
			catch
			{
				sortUnicode = true;
				return string.CompareOrdinal(a, b);
			}
		}
		static readonly HashSet<string> compressedArchiveExts = new(StringComparer.Ordinal) { "7z", "apk", "arc", "arj", "bz2", "cab", "cpio", "gz", "iso", "jar", "lha", "lzh", "lz", "lzma", "lzo", "rar", "tar", "tbz2", "tgz", "txz", "xz", "zip", "zipx" };
		static readonly HashSet<string> executableExts = new(StringComparer.Ordinal) { "appx", "appxbundle", "com", "exe", "msi", "msix", "msixbundle", "msp" };
		static readonly HashSet<string> imageExts = new(StringComparer.Ordinal) { "avif", "bmp", "gif", "heic", "heif", "ico", "jpeg", "jpg", "png", "svg", "tif", "tiff", "webp" };
		static readonly HashSet<string> shortcutExts = new(StringComparer.Ordinal) { "lnk" };
		static string GetFileIcon(string fileName)
		{
			var ext = Path.GetExtension(fileName);
			if (ext.Length != 0 && ext[0] == '.') ext = ext[1..];
			ext = ext.ToLowerInvariant();
			if (ext.Length == 0) return "📄";
			if (compressedArchiveExts.Contains(ext)) return "📦";
			if (executableExts.Contains(ext)) return "⚙️";
			if (imageExts.Contains(ext)) return "🌄";
			return shortcutExts.Contains(ext) ? "↗️" : "📄";
		}
	}
}