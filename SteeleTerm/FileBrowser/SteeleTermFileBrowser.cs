using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using static SteeleTerm.SteeleTerm;
namespace SteeleTerm.FileBrowser
{
    partial class SteeleTermFileBrowser
    {
        public static string? FileBrowser(string? startDir, bool allowOpen)
        {
            string promptFileBrowser = " 📂 > ";
            string cwd = startDir ?? "";
            bool inThisPc = false;
            if (cwd.Length == 0 || !Directory.Exists(cwd)) cwd = Directory.GetCurrentDirectory();
            while (true)
            {
                string currentDir = Directory.GetCurrentDirectory();
                string[] dirs = [];
                string[] files = [];
                var items = new List<(bool IsDir, string Name, string FullPath)>();
                if (inThisPc)
                {
                    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    bool redirected = Console.IsOutputRedirected;
                    int scanTop = Console.CursorTop;
                    int waitingScan = 1;
                    ConsoleSpinner? scanSpinner = null;
                    if (!redirected)
                    {
                        scanSpinner = new ConsoleSpinner(consoleLock);
                        if (Console.CursorLeft != 0) Console.WriteLine("");
                        scanSpinner.Start(promptFileBrowser, "Scanning drives ");
                    }
                    else Console.WriteLine($"{promptFileBrowser}Scanning drives...");
                    var drives = DriveInfo.GetDrives();
                    var uncCache = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                    var wpd = WPD.GetDevices();
                    for (int d = 0; d < drives.Length; d++)
                    {
                        var di = drives[d];
                        string root = di.Name;
                        string drive = root.TrimEnd('\\');
                        if (drive.Length >= 2 && drive[1] == ':') drive = char.ToUpperInvariant(drive[0]) + ":";
                        string label = "";
                        if (di.DriveType != DriveType.Network)
                        {
                            try { if (di.IsReady) label = (di.VolumeLabel ?? "").Trim(); } catch { }
                            string disp = label.Length == 0 ? $"({drive})" : $"({drive}) {label}";
                            items.Add((true, disp, root));
                            continue;
                        }
                        string disp2 = label.Length == 0 ? $"({drive})" : $"({drive}) {label}";
                        if (!uncCache.TryGetValue(drive, out string? unc))
                        {
                            string? uncResult = null;
                            var t = new Thread(() => { try { uncResult = TryGetUncForDrive(drive); } catch { } }) { IsBackground = true };
                            t.Start();
                            if (t.Join(250)) unc = uncResult;
                            else unc = null;
                            uncCache[drive] = unc;
                        }
                        if (!string.IsNullOrEmpty(unc)) disp2 += $" {unc}";
                        items.Add((true, disp2, root));
                    }
                    if (scanSpinner != null) StopSpinnerIfArmed(ref waitingScan, scanSpinner);
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
                                string? unc = dk?.GetValue("RemotePath") as string;
                                if (string.IsNullOrEmpty(unc)) continue;
                                string drive = letter.EndsWith(':') ? letter : letter + ":";
                                drive = letter.Length > 0 ? char.ToUpperInvariant(letter[0]) + ":" : "";
                                if (seen.Contains(drive)) continue;
                                string disp = $"({drive}) {unc}";
                                items.Add((true, disp, unc)); // FullPath is UNC so traversal works
                                seen.Add(drive);
                            }
                        }
                    }
                    catch { }
                }
                else
                {
                    try { dirs = Directory.GetDirectories(cwd); files = Directory.GetFiles(cwd); }
                    catch { Console.WriteLine($"{promptFileBrowser}Cannot access directory."); var parent = Directory.GetParent(cwd); if (parent != null) { cwd = parent.FullName; continue; } inThisPc = true; continue; }
                    SortNatural(dirs);
                    SortNatural(files);
                    for (int d = 0; d < dirs.Length; d++) items.Add((true, Path.GetFileName(dirs[d]), dirs[d]));
                    for (int f = 0; f < files.Length; f++) items.Add((false, Path.GetFileName(files[f]), files[f]));
                }
                int count = items.Count;
                var display = new List<string>(count);
                for (int k = 0; k < count; k++)
                {
                    string icon = inThisPc ? "💽" : (items[k].IsDir ? "📁" : GetFileIcon(items[k].Name));
                    string s = $"{k + 1:0000}{icon} {items[k].Name}";
                    display.Add(s);
                }
                int consoleWidth;
                try { consoleWidth = Console.WindowWidth; } catch { consoleWidth = 64; }
                consoleWidth = Math.Max(64, consoleWidth);
                int fileBrowserWidth = Math.Max(64, consoleWidth);
                int itemWidth = 31;
                int cols = Math.Max(2, fileBrowserWidth / 32);
                int total = cols * 32;
                string bar = new('-', total - 1);
                Console.WriteLine();
                bool redirectedRender = Console.IsOutputRedirected;
                int renderTop = Console.CursorTop;
                int waitingRender = 1;
                ConsoleSpinner? renderSpinner = null;
                if (!redirectedRender)
                {
                    if (Console.CursorLeft != 0) Console.WriteLine("");
                    renderTop = Console.CursorTop;
                    renderSpinner = new ConsoleSpinner(consoleLock);
                    renderSpinner.Start(promptFileBrowser, inThisPc ? "Building drive list " : "Building table ");
                }
                byte[] colour = new byte[count];
                if (!inThisPc)
                {
                    for (int k = 0; k < count; k++)
                    {
                        try
                        {
                            string p = items[k].FullPath;
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
                        if (!string.IsNullOrEmpty(root) && root.StartsWith(@"\\", StringComparison.Ordinal)) header = $"This PC\\[UNC] {cwd}\\";
                        else if (!string.IsNullOrEmpty(root))
                        {
                            var di = new DriveInfo(root);
                            string drive = di.Name.TrimEnd('\\'); // "C:"
                            string fmt = di.IsReady ? di.DriveFormat : "NOTREADY";
                            string label = di.IsReady ? (di.VolumeLabel ?? "") : "";
                            label = label.Trim();
                            header = label.Length == 0 ? $"This PC\\[{fmt}] {cwd}" : $"This PC\\[{fmt}] {label} {cwd}\\";
                        }
                        else header = $"This PC\\[PATH] {cwd}\\";
                    }
                    catch { header = $"This PC\\[FS] {cwd}\\"; }
                }
                string pathLine = header.Length > total ? string.Concat(header.AsSpan(0, Math.Max(0, total - 3)), "...") : header;
                int rows = Math.Max(((count + cols - 1) / cols), 16);
                var cellText = new string?[rows, cols];
                var cellIdx = new int[rows, cols];
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        int idx = c * rows + r;
                        if (idx >= count) break;
                        string s = display[idx];
                        if (s.Length > itemWidth) s = string.Concat(s.AsSpan(0, Math.Max(0, itemWidth - 3)), "...");
                        cellText[r, c] = s.PadRight(itemWidth);
                        cellIdx[r, c] = idx;
                    }
                }
                if (renderSpinner != null) StopSpinnerIfArmed(ref waitingRender, renderSpinner);
                if (!redirectedRender)
                {
                    ClearLine(renderTop);
                    Console.SetCursorPosition(0, renderTop);
                }
                Console.WriteLine(" " + pathLine.PadRight(total - 1));
                Console.WriteLine(" " + bar);
                for (int r = 0; r < rows; r++)
                {
                    Console.Write(" ");
                    for (int c = 0; c < cols; c++)
                    {
                        string? cell = cellText[r, c];
                        if (cell == null) break;
                        if (c != 0) Console.Write("|");
                        int idx = cellIdx[r, c];
                        var old = Console.ForegroundColor;
                        if (colour[idx] == 1) Console.ForegroundColor = ConsoleColor.Green;
                        else if (colour[idx] == 2) Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write(cell);
                        Console.ForegroundColor = old;
                    }
                    Console.WriteLine();
                }
                Console.WriteLine(" " + bar);
                Console.WriteLine(" Commands: ## = open/select | back = go up a directory | exit = close file browser | Exit = close SteeleTerm");
                Console.WriteLine();
                string? input = ReadToken(promptFileBrowser, "", true, true, true);
                if (input == null) continue;
                input = input.Trim();
                if (input.Length == 0) continue;
                if (string.Equals(input, "Exit", StringComparison.Ordinal)) return "Exit";
                if (string.Equals(input, "exit", StringComparison.Ordinal)) return "exit";
                if (string.Equals(input, "back", StringComparison.Ordinal))
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
                if (int.TryParse(input, out int n) && n >= 1 && n <= count)
                {
                    if (allowOpen && !items[n - 1].IsDir)
                    {
                        try { Process.Start(new ProcessStartInfo { FileName = items[n - 1].FullPath, UseShellExecute = true }); }
                        catch (Exception ex) { Console.WriteLine($"{promptFileBrowser}Cannot open file: {ex.Message}"); }
                        continue;
                    }
                    if (items[n - 1].IsDir) { cwd = items[n - 1].FullPath; inThisPc = false; continue; }
                    return items[n - 1].FullPath;
                }
            }
        }
        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        static extern int WNetGetConnection(string localName, StringBuilder remoteName, ref int length);
        static string? TryGetUncForDrive(string driveLetter)
        {
            try
            {
                int len = 1024;
                var sb = new StringBuilder(len);
                int rc = WNetGetConnection(driveLetter, sb, ref len);
                if (rc == 0) return sb.ToString();
            }
            catch { }
            return null;
        }
        static string[] SortNatural(string[] input, string[]? output = null)
        {
            if (output == null || ReferenceEquals(output, input)) output = input;
            else if (output.Length != input.Length) throw new ArgumentException("Output array is not the same length as input array.", nameof(output));
            Array.Copy(input, output, input.Length);
            Array.Sort(output, WinSort.CompareNatural);
            return output;
        }
        static partial class WinSort
        {
            [LibraryImport("shlwapi.dll", EntryPoint = "StrCmpLogicalW", StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int StrCmpLogicalW(string psz1, string psz2);
            public static int CompareNatural(string a, string b) { return StrCmpLogicalW(a, b); }
        }
        static readonly HashSet<string> compressedArchiveExts = new(StringComparer.Ordinal) { "7z", "apk", "arc", "arj", "bz2", "cab", "cpio", "gz", "iso", "jar", "lha", "lzh", "lz", "lzma", "lzo", "rar", "tar", "tbz2", "tgz", "txz", "xz", "zip", "zipx" };
        static readonly HashSet<string> executableExts = new(StringComparer.Ordinal) { "appx", "appxbundle", "com", "exe", "msi", "msix", "msixbundle", "msp" };
        static readonly HashSet<string> imageExts = new(StringComparer.Ordinal) { "avif", "bmp", "gif", "heic", "heif", "ico", "jpeg", "jpg", "png", "svg", "tif", "tiff", "webp" };
        static string GetFileIcon(string fileName)
        {
            var ext = Path.GetExtension(fileName);
            if (ext.Length != 0 && ext[0] == '.') ext = ext[1..];
            ext = ext.ToLowerInvariant();
            if (ext.Length != 0)
            {
                if (compressedArchiveExts.Contains(ext)) return "📦";
                if (executableExts.Contains(ext)) return "⚙️";
                if (imageExts.Contains(ext)) return "🌄";
            }
            return "📄";
        }
    }
}
