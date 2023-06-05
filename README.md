# FracturedCode/custom-async-server

**This will pin your CPU. You have been warned.**

This is just a tiny weekend project. I simultaneously wanted to brush up on my Rust skills, and wanted to build an async server in C#. So, I built the C# server and a Rust client to do some (very basic) stress testing.

With the client and server running on the same CPU, this managed 100k rps in a container.

## How to develop

Requirements:
- VSCode
- dev containers extension from MS
- docker

Alternative requirements (mix and match):
- your favorite IDE/text editor
- rust toolchain and .NET 7 sdk

In the vscode command pallete, find and execute something like "Rebuild and Reopen in Container".

## How to execute

You will need the .NET sdk and rust toolchain installed if you are not using the dev container.

### Server

```bash
cd server/
dotnet run
```

### Client

```bash
cd client/
cargo run
```