FROM mcr.microsoft.com/dotnet/sdk:8.0

# Node.js 20.x のインストール
RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && npm install -g npm@latest

# GitHub CLI のインストール
RUN curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg \
    | dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg \
    && chmod go+r /usr/share/keyrings/githubcli-archive-keyring.gpg \
    && echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" \
    | tee /etc/apt/sources.list.d/github-cli.list > /dev/null \
    && apt-get update \
    && apt-get install -y gh

# Entity Framework Core ツールのインストール
RUN dotnet tool install --global dotnet-ef

# GitHub Copilot CLI のインストール
RUN curl -fsSL https://gh.io/copilot-install | bash

# dotnet ツールへの PATH 設定
ENV PATH="${PATH}:/root/.dotnet/tools"
