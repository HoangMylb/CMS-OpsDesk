# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy solution and project files first to maximize Docker layer caching
COPY OpsDesk.sln ./
COPY src/OpsDesk.Core/OpsDesk.Core.csproj src/OpsDesk.Core/
COPY src/OpsDesk.Infrastructure/OpsDesk.Infrastructure.csproj src/OpsDesk.Infrastructure/
COPY src/OpsDesk.Web/OpsDesk.Web.csproj src/OpsDesk.Web/
COPY tests/OpsDesk.Tests/OpsDesk.Tests.csproj tests/OpsDesk.Tests/

# Restore dependencies
RUN dotnet restore OpsDesk.sln

# Copy the rest of the source code
COPY src/ src/
COPY tests/ tests/

# Publish Web project in Release mode
WORKDIR /app/src/OpsDesk.Web
RUN dotnet publish OpsDesk.Web.csproj -c Release -o /out /p:UseAppHost=false

# Stage 2: Runtime Environment
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Standard port for modern .NET containers (Render also accepts 8080)
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# Copy output from build stage
COPY --from=build /out ./

# Run the web app
ENTRYPOINT ["dotnet", "OpsDesk.Web.dll"]
