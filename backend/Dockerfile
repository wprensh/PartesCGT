# API de la tienda en contenedor Linux. Contexto de build: la carpeta backend/
#   docker build -t tienda-api backend
# En Render: Root Directory = backend (ver render.yaml en la raíz del repositorio).

# ---------- Ejecución: imagen liviana, usuario sin privilegios ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
# Carpeta para imágenes cuando Storage:Provider = Local (con Azure Blob no se usa).
RUN mkdir -p /app/App_Data/files && chown -R $APP_UID:$APP_UID /app/App_Data
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# ---------- Compilación ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Primero solo el .csproj: la restauración queda en caché mientras no cambien los paquetes.
COPY ["TiendaVirtual.Api/TiendaVirtual.Api.csproj", "TiendaVirtual.Api/"]
RUN dotnet restore "TiendaVirtual.Api/TiendaVirtual.Api.csproj"
# .editorconfig: excepciones de los analizadores (el proyecto trata los avisos como errores).
COPY [".editorconfig", "./"]
COPY ["TiendaVirtual.Api/", "TiendaVirtual.Api/"]
RUN dotnet publish "TiendaVirtual.Api/TiendaVirtual.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish \
    --no-restore /p:UseAppHost=false

# ---------- Imagen final ----------
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TiendaVirtual.Api.dll"]
