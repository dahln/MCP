FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Dev.MCP.LUNA/Dev.MCP.LUNA.csproj", "Dev.MCP.LUNA/"]
COPY ["Portal.LUNA.Dto/Portal.LUNA.Dto.csproj", "Portal.LUNA.Dto/"]
RUN dotnet restore "Dev.MCP.LUNA/Dev.MCP.LUNA.csproj"
COPY . .
WORKDIR "/src/Dev.MCP.LUNA"
RUN dotnet build "Dev.MCP.LUNA.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Dev.MCP.LUNA.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN apt-get update && apt-get install -y docker.io git && rm -rf /var/lib/apt/lists/*
ENTRYPOINT ["dotnet", "Dev.MCP.LUNA.dll"]
