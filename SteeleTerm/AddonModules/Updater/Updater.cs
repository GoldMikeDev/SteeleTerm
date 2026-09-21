using SteeleTerm.AddonModules.Extensions;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
[assembly: SupportedOSPlatform("windows")]
namespace SteeleTerm.AddonModules.Updater
{
	public static partial class Update
	{
		[GeneratedRegex("<Version>(.*?)</Version>", RegexOptions.Compiled | RegexOptions.CultureInvariant)] private static partial Regex VersionRegex();
		internal static bool TryHandleUpdateCommandTree(string[] args, string toolId, string csprojFileName, out int exitCode, ConsoleSpinner? spinner = null)
		{
			var hasUpdateMajor = args.Contains("--updateMajor", StringComparer.Ordinal);
			var hasUpdateMinor = args.Contains("--updateMinor", StringComparer.Ordinal);
			var hasUpdate = args.Contains("--update", StringComparer.Ordinal);
			if ((hasUpdateMajor && hasUpdateMinor) || (hasUpdateMajor && hasUpdate) || (hasUpdateMinor && hasUpdate)) { Console.WriteLine("Only one primary argument allowed."); exitCode = 2; return true; }
			var isUpdatePrimary = hasUpdateMajor || hasUpdateMinor || hasUpdate;
			if (!isUpdatePrimary) { exitCode = 2; return false; }
			var forceUpdate = args.Contains("--forceUpdate", StringComparer.Ordinal);
			var skipVersion = args.Contains("--skipVersion", StringComparer.Ordinal);
			if (skipVersion && !forceUpdate) { Console.WriteLine("❌ --skipVersion requires --forceUpdate as a secondary arg."); exitCode = 1; return true; }
			var allowed = new HashSet<string>(StringComparer.Ordinal) { "--updateMajor", "--updateMinor", "--update", "--forceUpdate", "--skipVersion" };
			foreach (var a in args)
			{
				if (!a.StartsWith("--", StringComparison.Ordinal) || allowed.Contains(a)) continue;
				Console.WriteLine($"❌ Unknown arg for update command: {a}"); exitCode = 1; return true;
			}
			try { UpdateTool(toolId, csprojFileName, hasUpdateMajor, hasUpdateMinor, forceUpdate, skipVersion, true, spinner); exitCode = 0; return true; }
			catch (Exception ex) { Console.WriteLine($"❌ Update failed: {ex.Message}"); exitCode = 1; return true; }
		}
		internal static void UpdateTool(string toolId, string csprojFileName, bool major, bool minor, bool forceUpdate, bool skipVersion, bool inheritConsole = true, ConsoleSpinner? spinner = null)
		{
			var projectDir = FindProjectDir(csprojFileName);
			var csprojPath = Path.Combine(projectDir, csprojFileName);
			var nupkgPath = Path.Combine(projectDir, "bin", "Release", "nupkg");
			var installedNupkg = FindInstalledNupkg(toolId) ?? throw new Exception($"❌ No installed {toolId} package found.");
			if (!forceUpdate)
			{
				Console.WriteLine("🔄 Hashing currently installed package...");
				var currentHash = ComputeFileHash(installedNupkg);
				Console.WriteLine($"🔒 Currently installed package hash: {currentHash}");
				Console.WriteLine("🏗️ Building and packing current version...");
				Executor.Launch("dotnet", "pack -c Release", projectDir, false, true, true, inheritConsole);
				var latestForCompare = FindLatestNupkg(nupkgPath);
				Console.WriteLine($"📁 Latest nupkg package found: {Path.GetFileName(latestForCompare)} (modified {File.GetLastWriteTime(latestForCompare):dd-MM-yyyy HH:mm:ss})");
				Console.WriteLine("🔄 Hashing new package...");
				var newHash = ComputeFileHash(latestForCompare);
				Console.WriteLine($"🔒 Newly built package hash: {newHash}");
				Console.WriteLine("⚖️ Comparing current hash to new build hash...");
				if (string.Equals(currentHash, newHash, StringComparison.Ordinal)) { Console.WriteLine($"🔁 {toolId} is up to date. Packages are identical."); return; }
				Console.WriteLine("🆕 Changes detected — proceeding with update...");
			}
			string? oldVersion = null;
			string? newVersion = null;
			try
			{
				if (!skipVersion)
				{
					var csprojText = File.ReadAllText(csprojPath);
					var match = VersionRegex().Match(csprojText);
					if (!match.Success) throw new Exception("⚠️ No <Version> tag found in .csproj.");
					oldVersion = match.Groups[1].Value.Trim();
					var parts = oldVersion.Split('.');
					if (parts.Length != 3 || !int.TryParse(parts[0], out var majorNum) || !int.TryParse(parts[1], out var minorNum) || !int.TryParse(parts[2], out var patchNum)) throw new Exception($"⚠️ Invalid version format: {oldVersion}");
					if (major) { majorNum++; minorNum = 0; patchNum = 0; }
					else if (minor) { minorNum++; patchNum = 0; }
					else patchNum++;
					newVersion = $"{majorNum}.{minorNum}.{patchNum}";
					csprojText = csprojText.Replace($"<Version>{oldVersion}</Version>", $"<Version>{newVersion}</Version>");
					File.WriteAllText(csprojPath, csprojText);
					Console.WriteLine($"⏫ Incremented version: {oldVersion} → {newVersion}");
				}
				else Console.WriteLine("⏭️ Skipping version increment");
				Console.WriteLine("🏗️ Building and packing...");
				Executor.Launch("dotnet", "pack -c Release", projectDir, false, true, true, inheritConsole);
			}
			catch (Exception ex) { Console.WriteLine($"❌ Update failed: {ex.Message}"); Cleanup(newVersion, oldVersion, csprojPath); return; }
			var nupkg = FindLatestNupkg(nupkgPath);
			var pkgDir = Path.GetDirectoryName(nupkg)!;
			var currentPid = Environment.ProcessId;
			var psExe = FindPowerShellExe();
			var updateScriptPath = Path.Combine(AppContext.BaseDirectory, "AddonModules", "Updater", "UpdateScript.ps1");
			var psArgs = $"-NoLogo -NoProfile -ExecutionPolicy Bypass -File \"{updateScriptPath}\" -toolId \"{toolId}\" {(skipVersion ? "-skipVersion " : "")}-pidToWait {currentPid} -pkgDir \"{pkgDir}\" -csprojPath \"{csprojPath}\" -oldVersion \"{oldVersion ?? ""}\" -newVersion \"{newVersion ?? ""}\"";
			var psi = new ProcessStartInfo(psExe, psArgs)
			{
				UseShellExecute = !inheritConsole,
				CreateNoWindow = false,
				RedirectStandardOutput = false,
				RedirectStandardError = false,
				WorkingDirectory = Environment.CurrentDirectory,
			};
			Console.WriteLine("🧠 Executing: UpdateScript.ps1");
			_ = Process.Start(psi) ?? throw new Exception("❌ Failed to start UpdateScript PowerShell process.");
			if (spinner != null) { spinner.Start("⏳ Closing ToolBox"); AppDomain.CurrentDomain.ProcessExit += (_, _) => spinner.StopAndFlush(); }
			else { Console.WriteLine("🚪 Closing ToolBox..."); }
			var timeoutThread = new Thread(() => {
				Thread.Sleep(3000);
				spinner?.StopAndFlush();
				Process.GetCurrentProcess().Kill();
			})
			{ IsBackground = true };
			timeoutThread.Start();
			Console.Out.Flush();
			Environment.Exit(0);
		}
		 static string FindProjectDir(string csprojFileName)
		{
			var dir = new DirectoryInfo(Environment.CurrentDirectory);
			while (dir != null) { if (File.Exists(Path.Combine(dir.FullName, csprojFileName))) return dir.FullName; dir = dir.Parent; }
			var home = DirectoryExtensions.GetSpecialDirectoryPath(DirectoryExtensions.SpecialDirectory.UserProfile);
			var repos = Path.Combine(home, "source", "repos");
			var found = TryFindFile(repos, csprojFileName) ?? TryFindFile(home, csprojFileName);
			if (found == null) throw new Exception($"❌ Could not locate {csprojFileName}.");
			var projDir = Path.GetDirectoryName(found)!;
			Console.WriteLine($"📁 Found project at: {projDir}");
			return projDir;
		}
		 static string? TryFindFile(string root, string fileName)
		{
			if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return null;
			var opts = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, ReturnSpecialDirectories = false };
			try { return Directory.EnumerateFiles(root, fileName, opts).FirstOrDefault(); } catch { return null; }
		}
		 static string? FindInstalledNupkg(string toolId)
		{
			var home = DirectoryExtensions.GetSpecialDirectoryPath(DirectoryExtensions.SpecialDirectory.UserProfile);
			var toolsRoot = Path.Combine(home, ".dotnet", "tools");
			if (!Directory.Exists(toolsRoot)) return null;
			var opts = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, ReturnSpecialDirectories = false };
			try { return Directory.EnumerateFiles(toolsRoot, $"{toolId}*.nupkg", opts).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault(); } catch { return null; }
		}
		 static string FindLatestNupkg(string nupkgDir)
		{
			if (!Directory.Exists(nupkgDir)) throw new Exception($"❌ .nupkg directory not found: {nupkgDir}");
			return Directory.EnumerateFiles(nupkgDir, "*.nupkg", SearchOption.TopDirectoryOnly).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault() ?? throw new Exception("❌ No .nupkg file found after packing.");
		}
		 static string FindPowerShellExe()
		{
			var psExe = Path.Combine(DirectoryExtensions.GetSpecialDirectoryPath(DirectoryExtensions.SpecialDirectory.LocalApplicationData), @"Microsoft\WindowsApps\Microsoft.PowerShellPreview_8wekyb3d8bbwe\pwsh.exe");
			var path = Environment.GetEnvironmentVariable("PATH") ?? "";
			var pwshOnPath = path.Split(';').Any(dir => File.Exists(Path.Combine(dir, "pwsh.exe")));
			if (!File.Exists(psExe) && pwshOnPath) { psExe = "pwsh"; }
			else if (!File.Exists(psExe) && !pwshOnPath) { psExe = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe"; }
			return psExe;
		}
		 static string ComputeFileHash(string filePath)
		{
			using var sha = SHA256.Create();
			using var stream = File.OpenRead(filePath);
			return Convert.ToHexString(sha.ComputeHash(stream));
		}
		 static void Cleanup(string? newVersion, string? oldVersion, string csprojPath)
		{
			Console.WriteLine("🧹 Performing cleanup...");
			try
			{
				if (!string.IsNullOrEmpty(oldVersion) && !string.IsNullOrEmpty(newVersion))
				{
					var rollbackText = File.ReadAllText(csprojPath);
					rollbackText = rollbackText.Replace($"<Version>{newVersion}</Version>", $"<Version>{oldVersion}</Version>");
					File.WriteAllText(csprojPath, rollbackText);
					Console.WriteLine($"↩️ Restored version number: {newVersion} → {oldVersion}");
				}
			}
			catch (Exception ex) { Console.WriteLine($"⚠️ Cleanup encountered an issue: {ex.Message}"); }
			Console.WriteLine("✅ Cleanup complete.");
		}
	}
}