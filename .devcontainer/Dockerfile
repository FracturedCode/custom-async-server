FROM mcr.microsoft.com/devcontainers/dotnet:0-7.0
RUN apt update && apt upgrade -y
RUN apt update && apt install -y build-essential gcc make
USER vscode
RUN curl https://sh.rustup.rs -sSf | sh -s -- --default-toolchain stable -y
USER root