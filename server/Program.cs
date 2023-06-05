using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

IPEndPoint ip = new(IPAddress.Any, 5000);

TcpListener listener = new(ip);
listener.Start(10100);

using CancellationTokenSource cts = new();
Console.CancelKeyPress += delegate (object? s, ConsoleCancelEventArgs e)
{
	e.Cancel = true;
	cts.Cancel();
};

_ = Task.Run(async () => await UpdateInfo(cts.Token));

while (!cts.IsCancellationRequested)
{
	try
	{
		TcpClient connectionHandle = await listener.AcceptTcpClientAsync(cts.Token);
		Locks.RequestsReceived++;
		Interlocked.Increment(ref Locks.OpenConnections);
		_ = Task.Run(() => ClientHandler(connectionHandle, cts.Token));
	}
	catch (OperationCanceledException)
	{
		break;
	}
}

listener.Stop();
Console.WriteLine();

static async Task UpdateInfo(CancellationToken token)
{
	while (!token.IsCancellationRequested)
	{
		long requestsBeforeDelay = Locks.RequestsReceived;
		// Yes, we are timing the Task.Delay.
		// AFAIK the SynchronizationContext doesn't give any special timing guarantees
		// about resuming after a Task.Delay completes.
		Stopwatch sw = new();
		sw.Start();
		await Task.Delay(100, token);
		sw.Stop();
		long requestsDuringDelay = Locks.RequestsReceived - requestsBeforeDelay;
		decimal rate = requestsDuringDelay / (decimal)sw.ElapsedMilliseconds * 1000;
		lock (Locks.ConsoleLock)
		{
			Console.Write($"\r{Locks.RequestsReceived} requests total. {Locks.OpenConnections} open connections. {rate:n0}r/s     ");
		}
	}
}

static async Task ClientHandler(TcpClient connectionHandle, CancellationToken cancellationToken)
{
	byte[] buffer = ArrayPool<byte>.Shared.Rent(512);
	try
	{
		using TcpClient handle = connectionHandle;
		await using NetworkStream stream = handle.GetStream();
		int bytesRead = await stream.ReadAsync(buffer, cancellationToken);
		if (bytesRead < 1 || cancellationToken.IsCancellationRequested)
		{
			return;
		}
		await stream.WriteAsync(buffer.AsMemory(..bytesRead), cancellationToken);
		Interlocked.Decrement(ref Locks.OpenConnections);
	}
	catch (Exception e)
	{
		lock (Locks.ConsoleLock)
		{
			Console.WriteLine($"Had an oopsie: {e.Message}\n\n{e.StackTrace}");
		}
	}
	finally
	{
		ArrayPool<byte>.Shared.Return(buffer);
	}
}

class Locks
{
	public static readonly object ConsoleLock = new();

	public static long RequestsReceived = 0;

	public static int OpenConnections = 0;
}