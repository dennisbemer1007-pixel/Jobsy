# Jobsy.Api — multi-stage image for Render / Docker
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Jobsy.Core/Jobsy.Core.csproj Jobsy.Core/
COPY Jobsy.Infrastructure/Jobsy.Infrastructure.csproj Jobsy.Infrastructure/
COPY Jobsy.Api/Jobsy.Api.csproj Jobsy.Api/
RUN dotnet restore Jobsy.Api/Jobsy.Api.csproj

COPY Jobsy.Core/ Jobsy.Core/
COPY Jobsy.Infrastructure/ Jobsy.Infrastructure/
COPY Jobsy.Api/ Jobsy.Api/
RUN dotnet publish Jobsy.Api/Jobsy.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# Render/Linux: avoid FileSystemWatcher (inotify) exhaustion on small instances.
ENV DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
CMD ["sh", "-c", "dotnet Jobsy.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
