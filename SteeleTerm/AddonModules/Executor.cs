using System.Diagnostics;
using System.Text;
namespace SteeleTerm.AddonModules
{
	public static class Executor
	{
		public static (int ExitCode, string Output, string Error) Launch(string exe, string args, string? workingDir, bool silent, bool streamToConsole, bool exitOnFail, bool inheritConsole)
		{
			try
			{
				if (!silent)
				{
					Console.WriteLine($"🧠 Executing: {exe} {args}");
					Console.WriteLine();
				}
				if (exe.Equals("dotnet")) args += " --tl:on";
				var psi = new ProcessStartInfo(exe, args) { WorkingDirectory = workingDir ?? Environment.CurrentDirectory, UseShellExecute = false, CreateNoWindow = !inheritConsole, RedirectStandardOutput = !inheritConsole, RedirectStandardError = !inheritConsole };
				if (!inheritConsole) { psi.StandardOutputEncoding = Encoding.UTF8; psi.StandardErrorEncoding = Encoding.UTF8; }
				using var p = new Process();
				p.StartInfo = psi;
				if (inheritConsole)
				{
					p.Start();
					p.WaitForExit();
					if (silent) return (p.ExitCode, string.Empty, string.Empty);
					Console.WriteLine($"🚪 Exit Code {p.ExitCode}: {ExitMessage(p.ExitCode)}");
					if (p.ExitCode != 0 && exitOnFail) Environment.Exit(p.ExitCode);
					return (p.ExitCode, string.Empty, string.Empty);
				}
				var sbOut = new StringBuilder();
				var sbErr = new StringBuilder();
				p.OutputDataReceived += (_, e) => { if (e.Data == null) return; sbOut.AppendLine(e.Data); if (streamToConsole) Console.WriteLine(e.Data); };
				p.ErrorDataReceived += (_, e) => { if (e.Data == null) return; sbErr.AppendLine(e.Data); if (streamToConsole) Console.Error.WriteLine(e.Data); };
				p.Start();
				p.BeginOutputReadLine();
				p.BeginErrorReadLine();
				p.WaitForExit();
				var output = sbOut.ToString().Trim();
				var error = sbErr.ToString().Trim();
				if (silent) return (p.ExitCode, output, error);
				if (p.ExitCode != 0 && !streamToConsole)
				{
					Console.WriteLine($"❌ Command failed to execute: {exe} {args}");
					Console.WriteLine("----------------------------------------------------------------");
					if (!string.IsNullOrWhiteSpace(output)) Console.WriteLine($"STDOUT:\n{output}");
					if (!string.IsNullOrWhiteSpace(error)) Console.WriteLine($"STDERR:\n{error}");
					Console.WriteLine("----------------------------------------------------------------");
				}
				Console.WriteLine($"🚪 Exit Code {p.ExitCode}: {ExitMessage(p.ExitCode)}");
				if (p.ExitCode == 0 || !exitOnFail) return (p.ExitCode, output, error);
				Console.Out.Flush(); Console.Error.Flush(); Environment.Exit(p.ExitCode);
				return (p.ExitCode, output, error);
			}
			catch (Exception ex) { Console.WriteLine($"❌ failed to execute '{exe} {args}': {ex.Message}"); return (-1, string.Empty, ex.Message); }
		}
		private static string ExitMessage(long code)
		{
			return code switch
			{
				-1 => "❌ Failed to start or was forcibly terminated.",
				0 => "✅ Success — operation completed successfully.",
				1 => "⚠️ General error — check command syntax or output for details.",
				2 => "❌ Invalid arguments or syntax.",
				3 => "🚫 Access denied or insufficient permissions.",
				4 => "📦 Target file or package not found.",
				5 => "🧱 I/O or path-related error.",
				126 => "🔒 Not executable — check file permissions.",
				127 => "❓ Command not found or missing from PATH.",
				128 => "📶 Terminated by external signal.",
				130 => "⛔ Terminated by Ctrl+C.",
				3221225786 => "⛔ Terminated by Ctrl+C (Windows NTSTATUS).",
				_ => $"🌀 Tool-specific exit code ({code})."
			};
		}
	}
}