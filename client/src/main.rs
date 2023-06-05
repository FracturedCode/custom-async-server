use futures::StreamExt;
use futures::stream::FuturesUnordered;
use tokio::io::{self, AsyncReadExt, AsyncWriteExt};
use tokio::net::TcpStream;
use tokio::sync::{OwnedSemaphorePermit};
use tokio::sync::Semaphore;
use tokio::task;
use std::sync::Arc;

async fn send_request(msg: &[u8], permit: OwnedSemaphorePermit) -> io::Result<()> {
	let mut stream: TcpStream = TcpStream::connect("127.0.0.1:5000").await?;
	stream.write_all(msg).await?;

	let mut data: [u8; 200] = [0 as u8; 200];
	let bytes_read = stream.read(&mut data).await?;

	if bytes_read == 0 {
		return Err(io::Error::new(
			io::ErrorKind::UnexpectedEof,
			"Connection closed or no bytes sent",
		));
	}
	drop(permit);
	Ok(())
}

#[tokio::main]
async fn main() -> io::Result<()> {
	let msg =
		b"GET / HTTP/1.1\r\nHost: localhost:5000\r\nUser-Agent: CustomRustClient/0.1.0\r\nUseless body info Lorem ipsum dolor sit amet, consectetur adipiscing elit. Fusce euismod risus eget turpis eleifend\r\n\r\n";
	let simultaneity: usize = 10000;

	let mut tasks: FuturesUnordered<task::JoinHandle<Result<(), io::Error>>> = FuturesUnordered::new();
	let (mut requests, mut failures) = (0, 0);
	let semaphore = Arc::new(Semaphore::new(simultaneity));
	loop {
		let completed_tasks: i32  = tasks.len() as i32 - simultaneity as i32;
		if completed_tasks > 0 {
			for _ in 0..completed_tasks {
				match tasks.next().await {
					Some(Err(_)) => failures += 1,
					_ => (),
				}
			}
		}
		
		let permit = semaphore.clone().acquire_owned().await.unwrap();
		let task = task::spawn(send_request(msg, permit));
		tasks.push(task);
		
		requests += 1;

		if requests % 10000 == 0 {
			print!("\rTotal requests: {}, Failures: {}", requests, failures);
		}
	}
}
