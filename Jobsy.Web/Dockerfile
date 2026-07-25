# Jobsy.Web — multi-stage image for Render / Docker
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Jobsy.Core/Jobsy.Core.csproj Jobsy.Core/
COPY Jobsy.Web/Jobsy.Web.csproj Jobsy.Web/
RUN dotnet restore Jobsy.Web/Jobsy.Web.csproj

COPY Jobsy.Core/ Jobsy.Core/
COPY Jobsy.Web/ Jobsy.Web/
RUN dotnet publish Jobsy.Web/Jobsy.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
CMD ["sh", "-c", "dotnet Jobsy.Web.dll --urls http://0.0.0.0:${PORT:-8080}"]
