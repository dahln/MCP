# Dockerfile for Portal.LUNA
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore
COPY Portal.LUNA.Dto/Portal.LUNA.Dto.csproj ./Portal.LUNA.Dto/
COPY Portal.LUNA.Database/Portal.LUNA.Database.csproj ./Portal.LUNA.Database/
COPY Portal.LUNA.Service/Portal.LUNA.Service.csproj ./Portal.LUNA.Service/
COPY Portal.LUNA.App/Portal.LUNA.App.csproj ./Portal.LUNA.App/
COPY Portal.LUNA.API/Portal.LUNA.API.csproj ./Portal.LUNA.API/

RUN dotnet restore Portal.LUNA.API/Portal.LUNA.API.csproj

# Copy source code
COPY Portal.LUNA.Dto/ ./Portal.LUNA.Dto/
COPY Portal.LUNA.Database/ ./Portal.LUNA.Database/
COPY Portal.LUNA.Service/ ./Portal.LUNA.Service/
COPY Portal.LUNA.App/ ./Portal.LUNA.App/
COPY Portal.LUNA.API/ ./Portal.LUNA.API/

WORKDIR /src/Portal.LUNA.API
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Data directory for SQLite database
RUN mkdir -p /data
ENV ConnectionStrings__DefaultConnection="Data Source=/data/luna.db"
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

VOLUME ["/data", "/var/run/docker.sock"]

ENTRYPOINT ["dotnet", "Portal.LUNA.API.dll"]
