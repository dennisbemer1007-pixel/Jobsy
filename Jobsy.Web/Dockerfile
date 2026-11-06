# Jobsy.Web — multi-stage image for Render / Docker
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Central props / SDK pin must be present before restore (CPM + shared TFM).
COPY Directory.Build.props Directory.Packages.props global.json .editorconfig ./

COPY Jobsy.Core/Jobsy.Core.csproj Jobsy.Core/
COPY Jobsy.Web/Jobsy.Web.csproj Jobsy.Web/
RUN dotnet restore Jobsy.Web/Jobsy.Web.csproj

COPY Jobsy.Core/ Jobsy.Core/
COPY Jobsy.Web/ Jobsy.Web/
# Jobsy.Core embeds this file from outside the project tree (see Jobsy.Core.csproj).
COPY tools/occupations/ilo_tasks_nl.json tools/occupations/ilo_tasks_nl.json
COPY tools/scripts/precompress-wwwroot-assets.py tools/scripts/precompress-wwwroot-assets.py
RUN apt-get update && apt-get install -y --no-install-recommends python3 python3-pip \
    && pip3 install --break-system-packages brotli \
    && python3 tools/scripts/precompress-wwwroot-assets.py \
    && rm -rf /var/lib/apt/lists/*
RUN dotnet publish Jobsy.Web/Jobsy.Web.csproj -c Release -o /app/publish /p:UseAppHost=false /p:SkipPrecompressStaticJs=true

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# Render/Linux: avoid FileSystemWatcher (inotify) exhaustion on small instances.
ENV DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
CMD ["sh", "-c", "dotnet Jobsy.Web.dll --urls http://0.0.0.0:${PORT:-8080}"]
