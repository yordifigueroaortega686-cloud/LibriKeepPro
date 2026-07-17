# Etapa de compilación (SDK de .NET 10.0)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

# Copiar el archivo de solución simplificado (.slnx) y los archivos de proyecto (.csproj)
# Esto optimiza el almacenamiento en caché de capas de Docker al restaurar dependencias
COPY LibriKeep.slnx ./
COPY src/LibriKeep.Core.Domain/LibriKeep.Core.Domain.csproj src/LibriKeep.Core.Domain/
COPY src/LibriKeep.Core.Application/LibriKeep.Core.Application.csproj src/LibriKeep.Core.Application/
COPY src/LibriKeep.Infrastructure.Persistence/LibriKeep.Infrastructure.Persistence.csproj src/LibriKeep.Infrastructure.Persistence/
COPY src/LibriKeep.Presentation.API/LibriKeep.Presentation.API.csproj src/LibriKeep.Presentation.API/

# Restaurar las dependencias de la solución apuntando al proyecto API (restaura dependencias transitivas)
RUN dotnet restore src/LibriKeep.Presentation.API/LibriKeep.Presentation.API.csproj

# Copiar el resto del código fuente del proyecto
COPY src/ src/

# Compilar y publicar el proyecto en modo Release sin volver a restaurar las dependencias
RUN dotnet publish src/LibriKeep.Presentation.API/LibriKeep.Presentation.API.csproj -c Release -o /app/out --no-restore

# Etapa de ejecución (Runtime de ASP.NET Core 10.0)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Instalar librería para soporte de Kerberos/GSSAPI requerida por PostgreSQL
USER root
RUN apt-get update && apt-get install -y libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*

# Exponer el puerto 8080 para Render
EXPOSE 8080

# Configurar variables de entorno
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Copiar los archivos compilados desde la etapa de compilación
COPY --from=build-env /app/out .

# Punto de entrada apuntando a la DLL principal de la API
ENTRYPOINT ["dotnet", "LibriKeep.Presentation.API.dll"]

