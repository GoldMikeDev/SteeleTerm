using Renci.SshNet;
using System.Net;
using SteeleTerm.FileBrowser;
namespace SteeleTerm.SSH
{
    partial class SteeleTermSSH
    {
        private static string prompt = " 🔒 > ";
        public static int SSH()
        {
        Reset:
            SetPromptDisconnected();
        EnterHost:
            int hostTop = Console.CursorTop;
            var hostAddress = SteeleTerm.ReadToken(prompt, "Enter Hostname or IP address: ");
            if (hostAddress == null) { SteeleTerm.ClearLine(hostTop); goto EnterHost; }
            hostAddress = hostAddress.Trim();
            if (hostAddress.Length == 0) { SteeleTerm.ClearLine(hostTop); goto EnterHost; }
            if (string.Equals(hostAddress, "Exit", StringComparison.Ordinal)) { Console.WriteLine(""); return 0; }
            SetPromptHost(hostAddress);
            Console.WriteLine("");
        EnterPort:
            int portTop = Console.CursorTop;
            int portNum = 22;
            var port = SteeleTerm.ReadToken(prompt, "Enter port (Default 22): ");
            if (string.Equals(port, "Exit", StringComparison.Ordinal)) { Console.WriteLine(""); return 0; }
            if (port == null || port.Trim().Length == 0) { Console.WriteLine(""); portNum = 22; }
            else
            {
                try { portNum = int.Parse(port.Trim()); }
                catch { SteeleTerm.ClearLine(portTop); goto EnterPort; }
                if (portNum < 1 || portNum > 65535) { SteeleTerm.ClearLine(portTop); goto EnterPort; }
                Console.WriteLine("");
            }
            SetPromptPort(hostAddress, portNum);
        EnterUser:
            int userTop = Console.CursorTop;
            var userID = SteeleTerm.ReadToken(prompt, "Enter user ID: ");
            if (userID == null) { SteeleTerm.ClearLine(userTop); goto EnterUser; }
            userID = userID.Trim();
            if (userID.Length == 0) { SteeleTerm.ClearLine(userTop); goto EnterUser; }
            if (string.Equals(userID, "Exit", StringComparison.Ordinal)) { Console.WriteLine(""); return 0; }
            SetPromptUser(hostAddress, portNum, userID);
            Console.WriteLine("");
        AuthMethod:
            int authTop = Console.CursorTop;
            Console.WriteLine();
            Console.WriteLine("      ## Authentication Method:");
            Console.WriteLine("      -- --------------------------------");
            Console.WriteLine("      01 Password / OTP Authentication");
            Console.WriteLine("      02 Private Key Authentication");
            Console.WriteLine("      03 Both");
            //Console.WriteLine("      04 Host-based authentication");
            //Console.WriteLine("      05 GSSAPI/Kerberos authentication");
            //Console.WriteLine("      06 Certificate-based authentication");
            Console.WriteLine();
            string? authMethod = SteeleTerm.ReadToken(prompt, "Select authentication method: ");
            if (authMethod == null || authMethod.Trim().Length == 0) { SteeleTerm.ClearLine(authTop); goto AuthMethod; }
            authMethod = authMethod.Trim();
            if (authMethod == "01" || authMethod == "1") { authMethod = "PW"; Console.WriteLine(""); }
            else if (authMethod == "02" || authMethod == "2") { authMethod = "PK"; Console.WriteLine(""); }
            else if (authMethod == "03" || authMethod == "3") { authMethod = "BTH"; Console.WriteLine(""); }
            else { SteeleTerm.ClearLine(authTop); goto AuthMethod; }
            SetPromptAuthMethod(hostAddress, portNum, userID, authMethod);
            string? password = null;
            string? keyPath = null;
            string? keyPassphrase = null;
            if (authMethod == "PW" || authMethod == "BTH")
            {
            EnterPassword:
                int passTop = Console.CursorTop;
                password = SteeleTerm.ReadToken(prompt, "Enter password: ", false, true, true);
                if (password == null || password.Length == 0) { SteeleTerm.ClearLine(passTop); goto EnterPassword; }
                if (string.Equals(password, "Exit", StringComparison.Ordinal)) { Console.WriteLine(""); return 0; }
            }
            if (authMethod == "PK" || authMethod == "BTH")
            {
            EnterKeyPath:
                int keyPathTop = Console.CursorTop;
                Console.WriteLine();
                Console.WriteLine("      ## Key Entry Method:");
                Console.WriteLine("      -- --------------------------------");
                Console.WriteLine("      01 Manual File Path Entry");
                Console.WriteLine("      02 Drag & Drop Key File");
                Console.WriteLine("      03 Browse File Directory");
                Console.WriteLine();
                string? keyEntryMethod = SteeleTerm.ReadToken(prompt, "Select key entry method: ");
                if (keyEntryMethod == null || keyEntryMethod.Trim().Length == 0) { SteeleTerm.ClearLine(keyPathTop); goto EnterKeyPath; }
                keyEntryMethod = keyEntryMethod.Trim();
                if (keyEntryMethod == "01" || keyEntryMethod == "1")
                {
                    keyEntryMethod = "MKE"; //Manual Key Entry
                    Console.WriteLine();
                    keyPath = SteeleTerm.ReadToken(prompt, "Enter key file path: ", true, true, true);
                    if (keyPath == null || keyPath.Trim().Length == 0) goto EnterKeyPath;
                    if (!KeyChecker(keyPath)) goto EnterKeyPath;
                }
                else if (keyEntryMethod == "02" || keyEntryMethod == "2")
                {
                    keyEntryMethod = "D&D"; //Drag and Drop
                    Console.WriteLine();
                    bool dragAndDrop = false;
                    while (!dragAndDrop)
                    {
                        keyPath = SteeleTerm.ReadToken(prompt, "Please drag and drop the key file into the console: ", true, true, true);
                        if (keyPath == null || keyPath.Trim().Length == 0) goto EnterKeyPath;
                        else if (string.Equals(keyPath, "Exit", StringComparison.Ordinal)) { Console.WriteLine(); return 0; }
                        else { if (KeyChecker(keyPath)) dragAndDrop = true; }
                    }
                }
                else if (keyEntryMethod == "03" || keyEntryMethod == "3")
                {
                    keyEntryMethod = "BFD"; //Browse File Directory
                    Console.WriteLine();
                BrowseFileDirectory:
                    keyPath = SteeleTermFileBrowser.FileBrowser(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh"), false);
                    if (keyPath == null) goto BrowseFileDirectory;
                    if (keyPath == "exit") goto EnterKeyPath;
                    if (keyPath == "Exit") return 0;
                    if (!KeyChecker(keyPath)) goto BrowseFileDirectory;

                }
                else if (string.Equals(keyEntryMethod, "Exit", StringComparison.Ordinal)) { Console.WriteLine(); return 0; }
                else { SteeleTerm.ClearLine(keyPathTop); goto EnterKeyPath; }
                if (keyPath == null) { SteeleTerm.ClearLine(keyPathTop); goto EnterKeyPath; }
                if (string.Equals(keyPath, "Exit", StringComparison.Ordinal)) { Console.WriteLine(); return 0; }
                keyPath = keyPath.Trim();
                if (keyPath.Length == 0) { SteeleTerm.ClearLine(keyPathTop); goto EnterKeyPath; }
                if (keyPath.Length >= 2 && ((keyPath[0] == '"' && keyPath[^1] == '"') || (keyPath[0] == '\'' && keyPath[^1] == '\''))) keyPath = keyPath[1..^1];
                if (!File.Exists(keyPath)) { SteeleTerm.ClearLine(keyPathTop); goto EnterKeyPath; }
            EnterKeyPassphrase:
                int keyPassphraseTop = Console.CursorTop;
                keyPassphrase = SteeleTerm.ReadToken(prompt, "Enter key passphrase (blank if none): ", false, true, true);
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
                Console.WriteLine();
                int dnsTop = Console.CursorTop;
                IPAddress[] hostIP;
                if (IPAddress.TryParse(hostAddress, out var literalIP)) hostIP = [literalIP];
                else
                {
                    int waitingDns = 0;
                    using var dnsSpinner = new SteeleTerm.ConsoleSpinner(SteeleTerm.consoleLock, 80);
                    Interlocked.Exchange(ref waitingDns, 1);
                    dnsSpinner.Start(prompt, $"Resolving {hostAddress}");
                    try { hostIP = Dns.GetHostAddresses(hostAddress); }
                    catch
                    {
                        SteeleTerm.StopSpinnerIfArmed(ref waitingDns, dnsSpinner);
                        SteeleTerm.ClearLine(dnsTop);
                        Console.SetCursorPosition(0, dnsTop);
                        Console.WriteLine($"{prompt}Resolving {hostAddress} ❌");
                        goto Reset;
                    }
                    SteeleTerm.StopSpinnerIfArmed(ref waitingDns, dnsSpinner);
                    SteeleTerm.ClearLine(dnsTop);
                    Console.SetCursorPosition(0, dnsTop);
                    if (hostIP.Length == 0) { Console.WriteLine($"{prompt}Resolving {hostAddress} ❌"); goto Reset; }
                    Console.WriteLine($"{prompt}Resolving {hostAddress} ✅");
                    Console.WriteLine($"{prompt}Resolved: {string.Join(", ", hostIP.Select(ip => ip.ToString()))}");
                }
                int i = 0;
                IPAddress? reachableIP = null;
                var sshCandidates = new List<IPAddress>();
                var tcpCandidates = new List<IPAddress>();
                while (i < hostIP.Length)
                {
                    var ip = hostIP[i++];
                    if (Console.CursorLeft != 0) Console.WriteLine("");
                    int checkTop = Console.CursorTop;
                    int waitingTcp = 1;
                    using var tcpSpinner = new SteeleTerm.ConsoleSpinner(SteeleTerm.consoleLock);
                    Interlocked.Exchange(ref waitingTcp, 1);
                    tcpSpinner.Start(prompt, $"Checking {ip}:{portNum}");
                    bool tcpOk = false;
                    bool sshOk = false;
                    string sshBanner = "";
                    try
                    {
                        using var socket = new System.Net.Sockets.Socket(ip.AddressFamily, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                        var ar = socket.BeginConnect(new IPEndPoint(ip, portNum), null, null);
                        if (!ar.AsyncWaitHandle.WaitOne(5000)) { try { socket.Close(); } catch { } tcpOk = false; }
                        else
                        {
                            socket.EndConnect(ar);
                            tcpOk = true;
                            try
                            {
                                socket.ReceiveTimeout = 5000;
                                byte[] buf = new byte[512];
                                int n = socket.Receive(buf);
                                if (n > 0)
                                {
                                    string s = System.Text.Encoding.ASCII.GetString(buf, 0, n);
                                    foreach (var lineRaw in s.Split('\n'))
                                    {
                                        var line = lineRaw.Trim('\r', '\n');
                                        if (line.StartsWith("SSH-", StringComparison.Ordinal)) { sshOk = true; sshBanner = line; break; }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                    catch { tcpOk = false; }
                    SteeleTerm.StopSpinnerIfArmed(ref waitingTcp, tcpSpinner);
                    SteeleTerm.ClearLine(checkTop);
                    Console.SetCursorPosition(0, checkTop);
                    if (!tcpOk) Console.WriteLine($"{prompt}Checking {ip}:{portNum} ❌");
                    else if (sshOk) Console.WriteLine($"{prompt}Checking {ip}:{portNum} ✅ {sshBanner}");
                    else Console.WriteLine($"{prompt}Checking {ip}:{portNum} ⚠️ (No SSH banner)");
                    if (!tcpOk) continue;
                    if (sshOk) sshCandidates.Add(ip);
                    else tcpCandidates.Add(ip);
                }
                if (sshCandidates.Count == 1) reachableIP = sshCandidates[0];
                else if (sshCandidates.Count > 1)
                {
                    Console.WriteLine($"{prompt}Multiple SSH targets found:");
                    for (int j = 0; j < sshCandidates.Count; j++) Console.WriteLine($"{prompt}  {j + 1:00} {sshCandidates[j]}");
                SelectSsh:
                    int pickTop = Console.CursorTop;
                    var pick = SteeleTerm.ReadToken(prompt, "Select target: ");
                    if (pick == null || pick.Trim().Length == 0) { SteeleTerm.ClearLine(pickTop); goto SelectSsh; }
                    int idx;
                    try { idx = int.Parse(pick.Trim()); }
                    catch { SteeleTerm.ClearLine(pickTop); goto SelectSsh; }
                    if (idx < 1 || idx > sshCandidates.Count) { SteeleTerm.ClearLine(pickTop); goto SelectSsh; }
                    reachableIP = sshCandidates[idx - 1];
                }
                else if (tcpCandidates.Count == 1) reachableIP = tcpCandidates[0];
                else if (tcpCandidates.Count > 1)
                {
                    Console.WriteLine($"{prompt}No SSH banner detected. TCP reachable targets:");
                    for (int j = 0; j < tcpCandidates.Count; j++) Console.WriteLine($"{prompt}  {j + 1:00} {tcpCandidates[j]}");
                SelectTcp:
                    int pickTop = Console.CursorTop;
                    var pick = SteeleTerm.ReadToken(prompt, "Select target: ");
                    if (pick == null || pick.Trim().Length == 0) { SteeleTerm.ClearLine(pickTop); goto SelectTcp; }
                    int idx;
                    try { idx = int.Parse(pick.Trim()); }
                    catch { SteeleTerm.ClearLine(pickTop); goto SelectTcp; }
                    if (idx < 1 || idx > tcpCandidates.Count) { SteeleTerm.ClearLine(pickTop); goto SelectTcp; }
                    reachableIP = tcpCandidates[idx - 1];
                }
                if (reachableIP == null) { Console.WriteLine(prompt + "Unable to connect to any resolved address on that port."); goto Reset; }
                Console.WriteLine($"{prompt}Selected: {reachableIP}:{portNum}");
                var methods = new List<AuthenticationMethod>();
                if (authMethod == "PW" || authMethod == "BTH")
                {
                    methods.Add(new PasswordAuthenticationMethod(userID, password!));
                    var ki = new KeyboardInteractiveAuthenticationMethod(userID);
                    ki.AuthenticationPrompt += (sender, e) =>
                    {
                        foreach (var prompt in e.Prompts)
                        {
                            prompt.Response = password!;
                        }
                    };
                    methods.Add(ki);
                }
                if (authMethod == "PK" || authMethod == "BTH")
                {
                    var keyFile = string.IsNullOrEmpty(keyPassphrase) ? new PrivateKeyFile(keyPath!) : new PrivateKeyFile(keyPath!, keyPassphrase);
                    methods.Add(new PrivateKeyAuthenticationMethod(userID, keyFile));
                }
                if (methods.Count == 0) { Console.WriteLine($"{prompt}No authentication methods configured."); goto Reset; }
                var ci = new ConnectionInfo(reachableIP!.ToString(), portNum, userID, [.. methods]);
                using var client = new SshClient(ci);
                //Optional but recommended: client.HostKeyReceived += ... (verify fingerprint / known-hosts style)
                try { client.Connect(); }
                catch (Exception ex) { Console.WriteLine($"{prompt}❌ Connect failed: {ex.Message}"); goto Reset; }
                Console.WriteLine($"{prompt}✅ Connected to {reachableIP}:{portNum}");
            }
            else goto Connect;
            return 0;
        }
        private static bool KeyChecker(string keyPath)
        {
            if (keyPath.Length >= 2 && ((keyPath[0] == '"' && keyPath[^1] == '"') || (keyPath[0] == '\'' && keyPath[^1] == '\''))) keyPath = keyPath[1..^1];
            if (!File.Exists(keyPath)) return false;
            string firstLine;
            try { firstLine = File.ReadLines(keyPath).FirstOrDefault() ?? ""; } catch { Console.WriteLine(prompt + "Cannot read the key file."); return false; }
            firstLine = firstLine.Trim();
            if (keyPath.EndsWith(".pub", StringComparison.OrdinalIgnoreCase) || firstLine.StartsWith("ssh-", StringComparison.Ordinal)) { Console.WriteLine(prompt + "Public key files are not supported. Please provide a private key file."); return false; }
            bool headerPrivateKey = firstLine.StartsWith("-----BEGIN ", StringComparison.Ordinal) || firstLine.StartsWith("PuTTY-User-Key-File-", StringComparison.Ordinal);
            if (!headerPrivateKey) { Console.WriteLine(prompt + "The provided file does not appear to be a private key file. Please try again."); return false; }
            return true;
        }
        private static void SetPromptDisconnected() { prompt = " 🔒 > "; }
        private static void SetPromptHost(string host) { prompt = $" 🔒 {host} > "; }
        private static void SetPromptPort(string host, int port) { prompt = $" 🔒 {host}:{port} > "; }
        private static void SetPromptUser(string host, int port, string user) { prompt = $" 🔒 {host}:{port} {user} > "; }
        private static void SetPromptAuthMethod(string host, int port, string user, string authMethod) { prompt = $" 🔒 {host}:{port} {user} {authMethod} > "; }
    }
}