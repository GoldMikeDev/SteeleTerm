using SteeleTerm.AddonModules;
using System.IO.Ports;
using System.Management;
using System.Text.RegularExpressions;
namespace SteeleTerm.Serial
{
	partial class SteeleTermSerial
	{
		private sealed record PortInfo(string Port, string FriendlyName, string PnpDeviceId, string VidPid);
		private const int DefaultBaud = 115200;
		private const int DefaultDataBits = 8;
		private const Parity DefaultParity = Parity.None;
		private const StopBits DefaultStopBits = StopBits.One;
		private static string prompt = " 🔌 > ";
		public static int Serial()
		{
		Reset:
			SetPromptDisconnected();
			var ports = GetPorts();
			if (ports.Count == 0) { SteeleTerm.Say(prompt, "❌ No COM ports found."); return 1; }
			var validPortNumbers = ports.Select(p => GetPortNumber(p.Port)).Where(n => n > 0).Distinct().Select(n => n.ToString()).ToHashSet(StringComparer.Ordinal);
			SteeleTerm.Say(prompt, "Available COM ports:");
			PrintTable(ports);
		SelectPort:
			int portTop = Console.CursorTop;
			var selected = SteeleTerm.ReadToken(prompt, "Select COM port: ");
			if (selected == null) { SteeleTerm.ClearLine(portTop); goto SelectPort; }
			selected = selected.Trim();
			if (selected.Length == 0) { SteeleTerm.ClearLine(portTop); goto SelectPort; }
			if (string.Equals(selected, "Exit", StringComparison.Ordinal)) { Console.WriteLine(""); return 0; }
			if (!validPortNumbers.Contains(selected)) { SteeleTerm.ClearLine(portTop); goto SelectPort; }
			Console.WriteLine("");
			var portNum = int.Parse(selected);
			var selectedPort = ports.First(p => GetPortNumber(p.Port) == portNum);
			SetPromptCOM(selectedPort.Port);
		EnterBaud:
			int baudTop = Console.CursorTop;
			var baud = SteeleTerm.ReadToken(prompt, "Enter baud rate (Default 115200): ");
			int baudRate;
			if (string.Equals(baud, "Exit", StringComparison.Ordinal)) { Console.WriteLine(""); return 0; }
			if (baud == null || baud.Trim().Length == 0) { Console.WriteLine(""); baudRate = DefaultBaud; }
			else { try { baudRate = int.Parse(baud.Trim()); Console.WriteLine(""); } catch { SteeleTerm.ClearLine(baudTop); goto EnterBaud; } }
			SetPromptBaud(selectedPort.Port, baudRate);
		EnterBitNotation:
			int bitsTop = Console.CursorTop;
			var bitNotation = SteeleTerm.ReadToken(prompt, "Enter bit notation (Default 8N1): ");
			int dataBits = DefaultDataBits;
			Parity parity = DefaultParity;
			StopBits stopBits = DefaultStopBits;
			if (string.Equals(bitNotation, "Exit", StringComparison.Ordinal)) { Console.WriteLine(""); return 0; }
			if (bitNotation == null || bitNotation.Trim().Length == 0) { Console.WriteLine(""); SetPromptBits(selectedPort.Port, baudRate, DefaultDataBits, DefaultParity, DefaultStopBits); }
			else
			{
				if (bitNotation.Trim().Length < 3 || bitNotation.Trim().Length > 5) { SteeleTerm.ClearLine(bitsTop); goto EnterBitNotation; }
				bitNotation = bitNotation.Trim().ToUpperInvariant();
				dataBits = bitNotation[0] - '0';
				if (dataBits < 5 || dataBits > 9) { SteeleTerm.ClearLine(bitsTop); goto EnterBitNotation; }
				char pChar = bitNotation[1];
				switch (pChar)
				{
					case 'N': parity = Parity.None; break;
					case 'E': parity = Parity.Even; break;
					case 'O': parity = Parity.Odd; break;
					case 'M': parity = Parity.Mark; break;
					case 'S': parity = Parity.Space; break;
					default: SteeleTerm.ClearLine(bitsTop); goto EnterBitNotation;
				}
				string sbText = bitNotation[2..];
				switch (sbText)
				{
					case "1": stopBits = StopBits.One; break;
					case "1.5": stopBits = StopBits.OnePointFive; break;
					case "2": stopBits = StopBits.Two; break;
					default: SteeleTerm.ClearLine(bitsTop); goto EnterBitNotation;
				}
				Console.WriteLine("");
				SetPromptBits(selectedPort.Port, baudRate, dataBits, parity, stopBits);
			}
		Connect:
			int connectTop = Console.CursorTop;
			var Connect = SteeleTerm.ReadToken(prompt, "Are these settings correct? (Y/N): ");
			if (Connect == null) { SteeleTerm.ClearLine(connectTop); goto Connect; }
			if (string.Equals(Connect.Trim(), "Exit", StringComparison.Ordinal)) { Console.WriteLine(""); return 0; }
			Connect = Connect.Trim().ToUpperInvariant();
			if (Connect == "N") { Console.WriteLine(""); goto Reset; }
			if (Connect == "Y")
			{
				Console.WriteLine("");
				try
				{
					using var serialPort = new SerialPort(selectedPort.Port, baudRate, parity, dataBits, stopBits);
					serialPort.NewLine = "\r";
					serialPort.ReadTimeout = 250;
					serialPort.WriteTimeout = 250;
					serialPort.Open();
					Console.WriteLine("");
					SteeleTerm.Say(prompt, $"✅ Connection to {selectedPort.Port} opened.");
					var stop = false;
					int forceLineStart = 0;
					string? suppressEchoLine = null;
					var rxSpinner = new ConsoleSpinner(SteeleTerm.consoleLock, prompt, 100, 150);
					int secretMode = 0;
					int suppressSecretEchoState = 0;
					bool echoEnabled() => Volatile.Read(ref secretMode) == 0;
					var rxThread = new Thread(() => {
						var buf = new char[4096];
						var echoBuf = new System.Text.StringBuilder();
						bool atLineStart = true;
						bool suppressing = false;
						string? expected = null;
						int echoPos = 0;
						int rxMinTop = 0;
						int rxMinLeft = 0;
						var rxTail = new System.Text.StringBuilder(64);
						bool pendingCr = false;
						while (!Volatile.Read(ref stop))
						{
							try
							{
								int n = serialPort.Read(buf, 0, buf.Length);
								if (n <= 0) continue;
								lock (SteeleTerm.consoleLock)
								{
									if (Interlocked.Exchange(ref forceLineStart, 0) != 0) atLineStart = true;
									for (int i = 0; i < n; i++)
									{
										char c = buf[i];
										if (pendingCr)
										{
											pendingCr = false;
											if (c != '\n')
											{
												if (suppressing)
												{
													if (expected != null && echoPos == expected.Length) { Volatile.Write(ref suppressEchoLine, null); }
													else
													{
														rxSpinner.RequestStopAndFlush();
														if (Console.CursorLeft != 0) Console.WriteLine("");
														Console.Write(prompt);
														rxMinTop = Console.CursorTop;
														rxMinLeft = Console.CursorLeft;
														Console.WriteLine(echoBuf.ToString());
													}
													suppressing = false;
													atLineStart = true;
												}
												else if (!atLineStart)
												{
													rxSpinner.RequestStopAndFlush();
													bool seek = true;
													try
													{
														int winTop = Console.WindowTop;
														int winBottom = winTop + Console.WindowHeight - 1;
														if (rxMinTop < winTop || rxMinTop > winBottom) seek = false;
													}
													catch { }
													if (seek)
													{
														try
														{
															Console.SetCursorPosition(rxMinLeft, rxMinTop);
															int clear = Math.Max(0, Console.BufferWidth - rxMinLeft);
															if (clear > 1) Console.Write(new string(' ', clear - 1));
															Console.SetCursorPosition(rxMinLeft, rxMinTop);
														}
														catch { seek = false; }
													}
													if (!seek)
													{
														if (Console.CursorLeft != 0) Console.WriteLine("");
														Console.Write(prompt);
														rxMinTop = Console.CursorTop;
														rxMinLeft = Console.CursorLeft;
													}
													rxTail.Clear();
												}
											}
										}
										if (c == '\r') { pendingCr = true; continue; }
										if (c == '\u007F') c = '\b';
										if (c == '\b')
										{
											if (Console.CursorTop > rxMinTop || Console.CursorLeft > rxMinLeft) Console.Write('\b');
											continue;
										}
										if (c != '\n' && char.IsControl(c)) continue;
										if (atLineStart && c != '\n') rxTail.Clear();
										if (c != '\n')
										{
											if (rxTail.Length == 64) rxTail.Remove(0, 1);
											rxTail.Append(c);
											if (c == ':')
											{
												var t = rxTail.ToString();
												if (t.EndsWith("Passcode:", StringComparison.OrdinalIgnoreCase) || t.EndsWith("Passkey:", StringComparison.OrdinalIgnoreCase) || t.EndsWith("Passphrase:", StringComparison.OrdinalIgnoreCase) || t.EndsWith("Password:", StringComparison.OrdinalIgnoreCase) || t.EndsWith("PIN:", StringComparison.OrdinalIgnoreCase) || t.EndsWith("Secret:", StringComparison.OrdinalIgnoreCase))
												{
													Volatile.Write(ref secretMode, 1);
													Volatile.Write(ref suppressSecretEchoState, 1);
												}
											}
										}
										int s = Volatile.Read(ref suppressSecretEchoState);
										if (s != 0)
										{
											if (c == '\n') { Volatile.Write(ref suppressSecretEchoState, 0); }
											else if (s == 1)
											{
												if (c == ':' || c == ' ') { }
												else { Volatile.Write(ref suppressSecretEchoState, 2); continue; }
											}
											else { continue; }
										}
										if (atLineStart)
										{
											if (c == '\n') continue;
											expected = Volatile.Read(ref suppressEchoLine);
											suppressing = expected != null;
											echoPos = 0;
											echoBuf.Clear();
											if (!suppressing)
											{
												rxSpinner.RequestStopAndFlush();
												if (Console.CursorLeft != 0) Console.WriteLine("");
												Console.Write(prompt);
												rxMinTop = Console.CursorTop;
												rxMinLeft = Console.CursorLeft;
											}
											atLineStart = false;
										}
										if (suppressing)
										{
											if (c == '\n')
											{
												if (expected != null && echoPos == expected.Length) { Volatile.Write(ref suppressEchoLine, null); }
												else
												{
													rxSpinner.RequestStopAndFlush();
													if (Console.CursorLeft != 0) Console.WriteLine("");
													Console.Write(prompt);
													rxMinTop = Console.CursorTop;
													rxMinLeft = Console.CursorLeft;
													Console.WriteLine(echoBuf.ToString());
												}
												suppressing = false;
												atLineStart = true;
												continue;
											}
											if (expected != null && echoPos < expected.Length && c == expected[echoPos])
											{
												echoBuf.Append(c);
												echoPos++;
												continue;
											}
											rxSpinner.RequestStopAndFlush();
											if (Console.CursorLeft != 0) Console.WriteLine("");
											Console.Write(prompt);
											rxMinTop = Console.CursorTop;
											rxMinLeft = Console.CursorLeft;
											Console.Write(echoBuf.ToString());
											Console.Write(c);
											suppressing = false;
											continue;
										}
										if (c == '\n') { Console.WriteLine(""); atLineStart = true; continue; }
										Console.Write(c);
									}
								}
							}
							catch (TimeoutException) { }
							catch { break; }
						}
					}) { IsBackground = true };
					rxThread.Start();
					serialPort.Write("\r");
					while (true)
					{
						bool secret = Volatile.Read(ref secretMode) != 0;
						var line = SteeleTerm.ReadToken(prompt, "", true, false, false, k => k.Key == ConsoleKey.Spacebar || k.Key == ConsoleKey.Backspace || k.Key == ConsoleKey.Delete, k =>
						{
							if (k.Key == ConsoleKey.Spacebar) { serialPort.Write(" "); return; }
							if (k.Key == ConsoleKey.Backspace) { serialPort.Write("\b"); return; }
							if (k.Key == ConsoleKey.Delete) { serialPort.Write("\u007F"); }
						}, echoEnabled);
						lock (SteeleTerm.consoleLock) { Console.WriteLine(""); Interlocked.Exchange(ref forceLineStart, 1); }
						if (line == null) { Volatile.Write(ref secretMode, 0); serialPort.Write("\r"); continue; }
						Volatile.Write(ref secretMode, 0);
						if (string.Equals(line.Trim(), "Exit", StringComparison.Ordinal))
						{
							Volatile.Write(ref stop, true);
							try { serialPort.Close(); } catch { }
							try { rxThread.Join(250); } catch { }
							SteeleTerm.Say(prompt, $"🚪 Connection to {selectedPort.Port} closed.");
							break;
						}
						rxSpinner.Start("Waiting for RX");
						Volatile.Write(ref suppressEchoLine, line.TrimEnd());
						serialPort.WriteLine(line);
					}
				}
				catch (Exception ex)
				{
					SteeleTerm.Say(prompt, $"❌ Error: {ex.Message}");
					return 1;
				}
			}
			else goto Connect;
			return 0;
		}
		private static void SetPromptDisconnected() { prompt = " 🔌 > "; }
		private static void SetPromptCOM(string port) { prompt = $" 🔌 {port} > "; }
		private static void SetPromptBaud(string port, int baud) { prompt = $" 🔌 {port} {baud} > "; }
		private static void SetPromptBits(string port, int baud, int dataBits, Parity parity, StopBits stopBits) { prompt = $" 🔌 {port} {baud} {dataBits}{GetParityChar(parity)}{GetStopBitsText(stopBits)} > "; }
		private static string GetStopBitsText(StopBits stopBits)
		{
			if (stopBits == StopBits.One) return "1";
			if (stopBits == StopBits.OnePointFive) return "1.5";
			if (stopBits == StopBits.Two) return "2";
			return "1";
		}
		private static char GetParityChar(Parity parity)
		{
			if (parity == Parity.None) return 'N';
			if (parity == Parity.Even) return 'E';
			if (parity == Parity.Odd) return 'O';
			if (parity == Parity.Mark) return 'M';
			if (parity == Parity.Space) return 'S';
			return 'N';
		}
		private static List<PortInfo> GetPorts()
		{
			var basePorts = new HashSet<string>(SerialPort.GetPortNames(), StringComparer.OrdinalIgnoreCase);
			var list = new List<PortInfo>();
			try
			{
				using var searcher = new ManagementObjectSearcher("SELECT Name, PNPDeviceID FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'");
				foreach (ManagementObject o in searcher.Get().Cast<ManagementObject>())
				{
					var name = (o["Name"] as string) ?? "";
					var pnp = (o["PNPDeviceID"] as string) ?? "";
					var m = COM().Match(name);
					if (!m.Success) continue;
					var com = m.Groups[1].Value.ToUpperInvariant();
					if (!basePorts.Contains(com)) continue;
					var vidpid = TryExtractVidPid(pnp);
					list.Add(new PortInfo(com, name, pnp, vidpid));
				}
			}
			catch { }
			if (list.Count == 0)
			{
				foreach (var p in basePorts.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
				{
					list.Add(new PortInfo(p.ToUpperInvariant(), p.ToUpperInvariant(), "", ""));
				}
			}
			return [.. list.OrderBy(x => x.Port, StringComparer.OrdinalIgnoreCase)];
		}
		private static string TryExtractVidPid(string pnpDeviceId)
		{
			if (string.IsNullOrWhiteSpace(pnpDeviceId)) return "";
			var m = VIDPID().Match(pnpDeviceId);
			if (!m.Success) return "";
			return $"{m.Groups[1].Value.ToUpperInvariant()}:{m.Groups[2].Value.ToUpperInvariant()}";
		}
		private static void PrintTable(List<PortInfo> ports)
		{
			int numW = Math.Max(5, ports.Max(p => GetPortNumber(p.Port).ToString().Length));
			int portW = Math.Max(4, ports.Max(p => p.Port.Length));
			int nameW = Math.Max(12, ports.Max(p => p.FriendlyName.Length));
			int vidW = Math.Max(7, ports.Max(p => p.VidPid.Length));
			static string H(string s, int w) => s.PadRight(w);
			Console.WriteLine("");
			Console.WriteLine($"      {H("Port", portW)}  {H("Friendly Name", nameW)}  {H("VID : PID", vidW)}");
			Console.WriteLine($"      {new string('-', portW)}  {new string('-', nameW)}  {new string('-', vidW)}");
			for (int i = 0; i < ports.Count; i++)
			{
				var p = ports[i];
				var n = GetPortNumber(p.Port).ToString();
				Console.WriteLine($"      {H(p.Port, portW)}  {H(p.FriendlyName, nameW)}  {H(p.VidPid, vidW)}");
			}
			Console.WriteLine("");
		}
		private static int GetPortNumber(string port)
		{
			if (string.IsNullOrWhiteSpace(port)) return 0;
			int i = 0;
			while (i < port.Length && !char.IsDigit(port[i])) i++;
			if (i >= port.Length) return 0;
			int n = 0;
			while (i < port.Length && char.IsDigit(port[i])) { n = (n * 10) + (port[i] - '0'); i++; }
			return n;
		}
		[GeneratedRegex(@"\((COM\d+)\)", RegexOptions.IgnoreCase, "en-GB")]
		private static partial Regex COM();
		[GeneratedRegex(@"VID_([0-9A-F]{4}).*PID_([0-9A-F]{4})", RegexOptions.IgnoreCase, "en-GB")]
		private static partial Regex VIDPID();
	}
}