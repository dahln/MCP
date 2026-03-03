FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Ping.MCP.LUNA/Ping.MCP.LUNA.csproj", "Ping.MCP.LUNA/"]
RUN dotnet restore "Ping.MCP.LUNA/Ping.MCP.LUNA.csproj"
COPY . .
WORKDIR "/src/Ping.MCP.LUNA"
RUN dotnet build "Ping.MCP.LUNA.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Ping.MCP.LUNA.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN apt-get update && apt-get install -y iputils-ping && rm -rf /var/lib/apt/lists/*
ENTRYPOINT ["dotnet", "Ping.MCP.LUNA.dll"]
