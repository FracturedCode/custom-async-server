using System.Buffers;
using System.Net;
using System.Net.Sockets;

IPEndPoint ip = new(IPAddress.Any, 5000);

TcpListener listener = new(ip);
listener.Start(10000);

using CancellationTokenSource cts = new();
AppDomain.CurrentDomain.ProcessExit += (_, _) => cts.Cancel();

while (!cts.IsCancellationRequested)
{
	TcpClient connectionHandle = await listener.AcceptTcpClientAsync(cts.Token);
	_ = Task.Run(() => ClientHandler(connectionHandle, cts.Token));
}


static async Task ClientHandler(TcpClient connectionHandle, CancellationToken cancellationToken)
{
	byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
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
}