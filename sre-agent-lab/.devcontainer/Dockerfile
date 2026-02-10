FROM mcr.microsoft.com/devcontainers/dotnet:1-10.0

RUN rm -f /etc/apt/sources.list.d/yarn.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends postgresql-client \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*
