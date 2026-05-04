ARG DOTNET_SDK_VERSION=10.0

# ===== Build stage =====
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION} AS build
WORKDIR .

# copy csproj first (sneller cachen)
COPY WA/WA.csproj WA/
COPY SharedLibrary/SharedLibrary.csproj SharedLibrary/
RUN dotnet restore WA/WA.csproj

# copy rest
COPY WA/ WA/
COPY SharedLibrary/ SharedLibrary/

RUN dotnet publish WA/WA.csproj -c Release -o /app/out

# ===== Runtime stage =====
FROM nginx:alpine

COPY WA/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/out/wwwroot /usr/share/nginx/html

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
