FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Portal.LUNA.API/Portal.LUNA.API.csproj", "Portal.LUNA.API/"]
COPY ["Portal.LUNA.Service/Portal.LUNA.Service.csproj", "Portal.LUNA.Service/"]
COPY ["Portal.LUNA.Database/Portal.LUNA.Database.csproj", "Portal.LUNA.Database/"]
COPY ["Portal.LUNA.Dto/Portal.LUNA.Dto.csproj", "Portal.LUNA.Dto/"]
COPY ["Portal.LUNA.App/Portal.LUNA.App.csproj", "Portal.LUNA.App/"]
RUN dotnet restore "Portal.LUNA.API/Portal.LUNA.API.csproj"
COPY . .
WORKDIR "/src/Portal.LUNA.API"
RUN dotnet build "Portal.LUNA.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Portal.LUNA.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
VOLUME /app/data
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/luna.db"
ENTRYPOINT ["dotnet", "Portal.LUNA.API.dll"]
