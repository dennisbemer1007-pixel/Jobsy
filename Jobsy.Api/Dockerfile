# Jobsy.Api — multi-stage image for Render / Docker
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Central props / SDK pin must be present before restore (CPM + shared TFM).
COPY Directory.Build.props Directory.Packages.props global.json .editorconfig ./

COPY Jobsy.Core/Jobsy.Core.csproj Jobsy.Core/
COPY Jobsy.Infrastructure/Jobsy.Infrastructure.csproj Jobsy.Infrastructure/
COPY Jobsy.Api/Jobsy.Api.csproj Jobsy.Api/
RUN dotnet restore Jobsy.Api/Jobsy.Api.csproj

COPY Jobsy.Core/ Jobsy.Core/
COPY Jobsy.Infrastructure/ Jobsy.Infrastructure/
COPY Jobsy.Api/ Jobsy.Api/
# Jobsy.Core embeds this file from outside the project tree (see Jobsy.Core.csproj).
COPY tools/occupations/ilo_tasks_nl.json tools/occupations/ilo_tasks_nl.json
RUN dotnet publish Jobsy.Api/Jobsy.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# Render/Linux: avoid FileSystemWatcher (inotify) exhaustion on small instances.
ENV DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
CMD ["sh", "-c", "dotnet Jobsy.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
