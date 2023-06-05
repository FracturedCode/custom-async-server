using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

IPEndPoint ip = new(IPAddress.Any, 5000);

TcpListener listener = new(ip);
listener.Start(10100);

using CancellationTokenSource cts = new();
AppDomain.CurrentDomain.ProcessExit += (_, _) => cts.Cancel();

_ = Task.Run(async () => await UpdateInfo(cts.Token));

while (!cts.IsCancellationRequested)
{
	TcpClient connectionHandle = await listener.AcceptTcpClientAsync(cts.Token);
	Locks.RequestsReceived++;
	_ = Task.Run(() => ClientHandler(connectionHandle, cts.Token));
}

Console.WriteLine();

static async Task UpdateInfo(CancellationToken token)
{
	while (!token.IsCancellationRequested)
	{
		long requestsBeforeDelay = Locks.RequestsReceived;
		Stopwatch sw = new();
		sw.Start();
		await Task.Delay(100, token);
		sw.Stop();
		long requestsDuringDelay = Locks.RequestsReceived - requestsBeforeDelay;
		decimal rate = requestsDuringDelay / (decimal)sw.ElapsedMilliseconds * 1000;
		lock (Locks.ConsoleLock)
		{
			Console.Write($"\r{Locks.RequestsReceived} requests total. {rate:n0}r/s     ");
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
}