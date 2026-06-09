FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["FitLife.API/FitLife.API.csproj", "FitLife.API/"]
COPY ["FitLife.Application/FitLife.Application.csproj", "FitLife.Application/"]
COPY ["FitLife.Domain/FitLife.Domain.csproj", "FitLife.Domain/"]
COPY ["FitLife.Infrastructure/FitLife.Infrastructure.csproj", "FitLife.Infrastructure/"]
COPY ["SharedLibrary/SharedLibrary.csproj", "SharedLibrary/"]
RUN dotnet restore "FitLife.API/FitLife.API.csproj"
COPY . .
WORKDIR "/src/FitLife.API"
RUN dotnet build "./FitLife.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FitLife.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FitLife.API.dll"]
