namespace SteeleTerm.AddonModules
{
	public class ConsoleSpinner(Lock outputLock, string prefix, int intervalMs = 100, int minSpinnerMs = 0)
	{
		readonly Lock outputLock = outputLock;
		readonly string prefix = prefix;
		readonly int intervalMs = intervalMs;
		readonly int minSpinnerMs = minSpinnerMs;
		readonly Queue<(string Line, bool IsErr)> pending = new();
		readonly Lock pendingLock = new();
		Thread? spinnerThread;
		volatile bool spinning;
		long spinnerStartedAt;
		int active;
		int stopScheduled;
		string text = "";
		bool cursorOldVisible;
		bool cursorCaptured;
		public bool Active => Volatile.Read(ref active) != 0;
		public void Start(string text)
		{
			if (Console.IsOutputRedirected) return;
			if (Interlocked.Exchange(ref active, 1) != 0) return;
			this.text = text;
			spinnerStartedAt = Environment.TickCount64;
			try { cursorOldVisible = Console.CursorVisible; Console.CursorVisible = false; cursorCaptured = true; } catch { cursorCaptured = false; }
			spinning = true;
			spinnerThread = new Thread(() => {
				char[] frames = ['|', '/', '-', '\\'];
				int i = 0;
				while (spinning)
				{
					lock (outputLock) { try { Console.Write("\r" + prefix + this.text + " " + frames[i++ & 3] + " "); } catch { } }
					Thread.Sleep(intervalMs);
				}
			})
			{ IsBackground = true };
			lock (outputLock) { try { Console.Write("\r" + prefix + this.text + " | "); } catch { } }
			spinnerThread.Start();
		}
		public void Enqueue(string line, bool isErr) { lock (pendingLock) pending.Enqueue((line, isErr)); }
		public void RequestStopAndFlush()
		{
			if (!Active) return;
			long elapsed = Environment.TickCount64 - spinnerStartedAt;
			if (elapsed >= minSpinnerMs) { StopAndFlush(); return; }
			if (Interlocked.Exchange(ref stopScheduled, 1) != 0) return;
			Task.Run(() => {
				int wait = (int)Math.Max(0, minSpinnerMs - (Environment.TickCount64 - spinnerStartedAt));
				if (wait > 0) Thread.Sleep(wait);
				Interlocked.Exchange(ref stopScheduled, 0);
				StopAndFlush();
			});
		}
		public void StopAndFlush()
		{
			if (Interlocked.Exchange(ref active, 0) == 0) return;
			spinning = false;
			try { spinnerThread?.Join(); } catch { }
			if (cursorCaptured) { try { Console.CursorVisible = cursorOldVisible; } catch { } cursorCaptured = false; }
			lock (outputLock)
			{
				try { Console.Write("\r" + new string(' ', Math.Max(0, Console.BufferWidth - 1)) + "\r"); } catch { try { Console.Write("\r"); } catch { } }
				while (true)
				{
					(string Line, bool IsErr) item;
					lock (pendingLock) { if (pending.Count == 0) break; item = pending.Dequeue(); }
					if (item.Line.Length == 0) { if (item.IsErr) Console.Error.WriteLine(); else Console.WriteLine(); }
					else { if (item.IsErr) Console.Error.WriteLine(prefix + item.Line); else Console.WriteLine(prefix + item.Line); }
				}
			}
		}
	}
}