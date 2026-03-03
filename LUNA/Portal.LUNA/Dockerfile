FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Portal.LUNA/Portal.LUNA.csproj", "Portal.LUNA/"]
RUN dotnet restore "Portal.LUNA/Portal.LUNA.csproj"
COPY . .
WORKDIR "/src/Portal.LUNA"
RUN dotnet build "Portal.LUNA.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Portal.LUNA.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
VOLUME /app/data
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/luna.db"
ENTRYPOINT ["dotnet", "Portal.LUNA.dll"]
