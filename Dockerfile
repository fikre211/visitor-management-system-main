#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
EXPOSE 8080
EXPOSE 8081
WORKDIR /app

FROM your_base_image

# Create the 'app' user and set permissions for the uploads directory
RUN useradd -m app && \
    mkdir -p /app/wwwroot/uploads && \
    chown -R app:app /app/wwwroot/uploads && \
    chmod -R 755 /app/wwwroot/uploads

# Switch to the 'app' user
USER app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["GatePass.MS.ClientApp/GatePass.MS.ClientApp.csproj", "GatePass.MS.ClientApp/"]
COPY ["GatePass.MS.Application/GatePass.MS.Application.csproj", "GatePass.MS.Application/"]
COPY ["GatePass.MS.Domain/GatePass.MS.Domain.csproj", "GatePass.MS.Domain/"]
COPY ["GatePass.MS.Persistence/GatePass.MS.Persistence.csproj", "GatePass.MS.Persistence/"]
COPY ["GatePass.MS.Infrastructure/GatePass.MS.Infrastructure.csproj", "GatePass.MS.Infrastructure/"]
RUN dotnet restore "./GatePass.MS.ClientApp/GatePass.MS.ClientApp.csproj"
COPY . .
WORKDIR "/src/GatePass.MS.ClientApp"
RUN dotnet build "./GatePass.MS.ClientApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GatePass.MS.ClientApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "GatePass.MS.ClientApp.dll"]
