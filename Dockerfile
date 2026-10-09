FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar los archivos .csproj respetando sus rutas
COPY src/LibriKeep.Core.Domain/LibriKeep.Core.Domain.csproj src/LibriKeep.Core.Domain/
COPY src/LibriKeep.Core.Application/LibriKeep.Core.Application.csproj src/LibriKeep.Core.Application/
COPY src/LibriKeep.Infrastructure.Persistence/LibriKeep.Infrastructure.Persistence.csproj src/LibriKeep.Infrastructure.Persistence/
COPY src/LibriKeep.Infrastructure.Cache/LibriKeep.Infrastructure.Cache.csproj src/LibriKeep.Infrastructure.Cache/
COPY src/LibriKeep.Presentation.API/LibriKeep.Presentation.API.csproj src/LibriKeep.Presentation.API/

# Restaurar dependencias
RUN dotnet restore src/LibriKeep.Presentation.API/LibriKeep.Presentation.API.csproj

# Copiar todo el código fuente restante y publicar en Release
COPY . .
RUN dotnet publish src/LibriKeep.Presentation.API/LibriKeep.Presentation.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# Stage de Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copiar binarios desde la etapa de build
COPY --from=build /app/publish .

# Variables de entorno para Render
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_URLS=http://+:8080

# Exponer el puerto 8080
EXPOSE 8080

# Punto de entrada
ENTRYPOINT ["dotnet", "LibriKeep.Presentation.API.dll"]
