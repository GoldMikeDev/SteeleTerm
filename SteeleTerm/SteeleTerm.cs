using SteeleTerm.FileBrowser;
using SteeleTerm.Serial;
using SteeleTerm.SSH;
using SteeleTerm.ToolBox;
using SteeleTerm.Updater;
using System.Runtime.Versioning;
using System.Text;
[assembly: SupportedOSPlatform("windows")]
namespace SteeleTerm
{
    partial class SteeleTerm
    {
        internal static readonly object consoleLock = new();
        static readonly string[] availableCommands = ["--fileBrowser", "--help", "--serial", "--ssh", "--update", "--updateMajor", "--updateMinor"];
        static int Main(string[] args)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            bool hasFileBrowser = args.Contains("--fileBrowser", StringComparer.Ordinal);
            bool hasHelp = args.Contains("--help", StringComparer.Ordinal);
            bool hasSerial = args.Contains("--serial", StringComparer.Ordinal);
            bool hasSSH = args.Contains("--ssh", StringComparer.Ordinal);
            if (args.Length == 0 || !args.Any(arg => availableCommands.Contains(arg))) { Console.WriteLine("Use --help for arg list."); return 1; }
            if (hasHelp)
            {
                Console.WriteLine("Commands:");
                Console.WriteLine("  \'SteeleTerm\'                          Print \'Use --help for arg list\' message.");
                Console.WriteLine("Primary args:");
                Console.WriteLine("  \'--fileBrowser\'                       Open SteeleTerm file browser.");
                Console.WriteLine("  \'--help\'                              Print help list to console.");
                Console.WriteLine("  \'--serial\'                            Open SteeleTerm in serial mode.");
                Console.WriteLine("  \'--ssh\'                               Open SteeleTerm in ssh mode.");
                Console.WriteLine("  \'--update [secondary] [tertiary]\'     Increment patch version.");
                Console.WriteLine("  \'--updateMajor [secondary] [tertiary]\'Increment major version.");
                Console.WriteLine("  \'--updateMinor [secondary] [teriary]\' Increment minor version.");
                Console.WriteLine("Secondary args:");
                Console.WriteLine("  \'<primary> --forceUpdate [tertiary]\'  Force rebuild/reinstall even if nothing changed. Requires an update primary arg.");
                Console.WriteLine("Tertiary args:");
                Console.WriteLine("  \'<primary> <secondary> --skipVersion\' Do not update version number. Requires --forceUpdate arg.");
                return 0;
            }
            if (!ToolBoxHandshake.VerifyToolBoxHost()) return 1;
            if (Update.TryHandleUpdateCommandTree(args, "SteeleTerm", "SteeleTerm.csproj", out var updateExitCode)) return updateExitCode;
            if ((hasHelp && hasSerial) || (hasHelp && hasSSH) || (hasSerial && hasSSH) || (hasHelp && hasFileBrowser) || (hasSerial && hasFileBrowser) || (hasSSH && hasFileBrowser)) { Console.WriteLine("Only one primary argument allowed."); return 1; }
            if (hasFileBrowser) { SteeleTermFileBrowser.FileBrowser(Directory.GetCurrentDirectory(), true); return 0; }
            if (hasSerial) { SteeleTermSerial.Serial(); return 0; }
            if (hasSSH) { SteeleTermSSH.SSH(); return 0; }
            else return 0;
        }
        public static void Say(string prompt, string message) { Console.WriteLine($"{prompt}{message}"); }
        public static void ClearLine(int top)
        {
            int height;
            try { height = Console.BufferHeight; }
            catch { return; }
            if (height <= 0) return;
            if (top < 0) top = 0;
            if (top >= height) top = height - 1;
            int width;
            try { width = Console.BufferWidth; }
            catch { return; }
            width = Math.Max(1, width);
            try { Console.SetCursorPosition(0, top); Console.Write(new string(' ', Math.Max(0, width - 1))); Console.SetCursorPosition(0, top); }
            catch (ArgumentOutOfRangeException) { }
        }
        public static string? ReadToken(string prompt, string promptText, bool echo = true, bool printPrompt = true, bool commitNewlineOnEnter = false, Func<ConsoleKeyInfo, bool>? immediateKey = null, Action<ConsoleKeyInfo>? onImmediateKey = null, Func<bool>? echoEnabled = null)
        {
            int startTop;
            int startLeft;
            lock (consoleLock)
            {
                if (printPrompt) Console.Write($"{prompt}{promptText}");
                startTop = Console.CursorTop;
                startLeft = Console.CursorLeft;
            }
            var buf = new StringBuilder();
            int echoedCount = 0;
            bool lastEcho = (echoEnabled?.Invoke() ?? echo);
            while (true)
            {
                var k = Console.ReadKey(true);
                bool echoNow = (echoEnabled?.Invoke() ?? echo);
                if (lastEcho && !echoNow && echoedCount != 0)
                {
                    lock (consoleLock)
                    {
                        for (int i = 0; i < echoedCount; i++)
                        {
                            int top = Console.CursorTop;
                            int left = Console.CursorLeft;
                            if (top < startTop || (top == startTop && left <= startLeft)) break;
                            Console.Write("\b \b");
                        }
                    }
                    echoedCount = 0;
                }
                lastEcho = echoNow;
                if (buf.Length == 0 && immediateKey != null && immediateKey(k)) { onImmediateKey?.Invoke(k); continue; }
                if (k.Key == ConsoleKey.Enter)
                {
                    if (commitNewlineOnEnter) { lock (consoleLock) { Console.WriteLine(""); } }
                    if (buf.Length == 0) return null;
                    return buf.ToString();
                }
                if (k.Key == ConsoleKey.Backspace)
                {
                    if (buf.Length == 0) continue;
                    buf.Length--;
                    if (echoNow && echoedCount != 0)
                    {
                        lock (consoleLock)
                        {
                            int top = Console.CursorTop;
                            int left = Console.CursorLeft;
                            if (top > startTop || (top == startTop && left > startLeft)) Console.Write("\b \b");
                        }
                        echoedCount--;
                    }
                    continue;
                }
                if (k.KeyChar == '\0') continue;
                if (char.IsControl(k.KeyChar)) continue;
                buf.Append(k.KeyChar);
                if (echoNow) { lock (consoleLock) { Console.Write(k.KeyChar); } echoedCount++; }
            }
        }
        public sealed class ConsoleSpinner(object outputLock, int intervalMs = 100) : IDisposable
        {
            readonly string[] frames = ["|", "/", "-", "\\"];
            readonly object outputLock = outputLock;
            readonly int intervalMs = intervalMs;
            Thread? t;
            volatile bool running;
            string text = "";
            bool oldCursorVisible = true;
            public void Start(string prompt, string text)
            {
                if (Console.IsOutputRedirected) return;
                if (running) return;
                this.text = text;
                running = true;
                try { oldCursorVisible = Console.CursorVisible; Console.CursorVisible = false; } catch { }
                t = new Thread(() =>
                {
                    int i = 0;
                    while (running)
                    {
                        lock (outputLock) { try { Console.Write("\r" + prompt + this.text + " " + frames[i++ % frames.Length]); } catch { } }
                        Thread.Sleep(intervalMs);
                    }
                })
                { IsBackground = true };
                t.Start();
            }
            public void Stop()
            {
                if (!running) return;
                running = false;
                try { t?.Join(); } catch { }
                lock (outputLock)
                {
                    try
                    {
                        int w = Math.Max(1, Console.BufferWidth);
                        Console.Write("\r" + new string(' ', Math.Max(0, w - 1)) + "\r");
                    }
                    catch { try { Console.Write("\r"); } catch { } }
                    try { Console.CursorVisible = oldCursorVisible; } catch { }
                }
            }
            public void Dispose() { Stop(); }
        }
        public static void StopSpinnerIfArmed(ref int waitingRx, ConsoleSpinner rxSpinner)
        {
            if (Interlocked.Exchange(ref waitingRx, 0) == 1) rxSpinner.Stop();
        }
    }
}