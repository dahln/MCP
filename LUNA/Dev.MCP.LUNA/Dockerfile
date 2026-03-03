# Dockerfile for Dev.MCP.LUNA
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Dev.MCP.LUNA/Dev.MCP.LUNA.csproj ./Dev.MCP.LUNA/
RUN dotnet restore Dev.MCP.LUNA/Dev.MCP.LUNA.csproj

COPY Dev.MCP.LUNA/ ./Dev.MCP.LUNA/
WORKDIR /src/Dev.MCP.LUNA
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Dev.MCP.LUNA.dll"]
