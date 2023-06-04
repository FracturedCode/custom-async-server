use tokio::io::{self, AsyncReadExt, AsyncWriteExt};
use tokio::net::TcpStream;

async fn send_request(msg: &[u8]) -> io::Result<()> {
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

    Ok(())
}

#[tokio::main]
async fn main() -> io::Result<()> {
    let msg =
        b"GET / HTTP/1.1\r\nHost: localhost:5000\r\nUser-Agent: CustomRustClient/0.1.0\r\nUseless body info Lorem ipsum dolor sit amet, consectetur adipiscing elit. Fusce euismod risus eget turpis eleifend\r\n\r\n";

    let (mut requests, mut failures) = (0, 0);
    loop {
        if let Err(_) = send_request(msg).await {
            failures += 1;
        }
        requests += 1;
        print!("\rRequests: {}, Failures: {}", requests, failures);
    }
}
