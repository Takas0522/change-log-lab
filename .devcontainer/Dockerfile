FROM mcr.microsoft.com/devcontainers/dotnet:1-10.0-preview

# Install PostgreSQL client for psql command
# Remove problematic Yarn repository if it exists (added by devcontainer features with expired GPG key)
RUN rm -f /etc/apt/sources.list.d/yarn.list 2>/dev/null || true \
    && apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install --no-install-recommends postgresql-client

# [Optional] Uncomment this line to install global node packages.
# RUN su vscode -c "source /usr/local/share/nvm/nvm.sh && npm install -g <your-package-here>" 2>&1