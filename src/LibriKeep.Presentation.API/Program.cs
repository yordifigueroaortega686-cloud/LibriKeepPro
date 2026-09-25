using LibriKeep.Presentation.API.Middleware;
using LibriKeep.Core.Application.Circulacion.Commands.RegistrarPrestamo;
using LibriKeep.Infrastructure.Persistence;
using LibriKeep.Infrastructure.Cache;
using LibriKeep.Infrastructure.Cache.Decorators;
using LibriKeep.Core.Application.Common.Interfaces;
using LibriKeep.Infrastructure.Persistence.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Registrar controladores de la API configurados con serialización de strings para Enums
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Registrar capa de infraestructura y persistencia (PostgreSQL primario + réplica de lectura)
// Regla VIII-4: separación de nodos primario (escrituras) / réplica (catálogo público)
builder.Services.AddInfrastructurePersistence(builder.Configuration);

// Registrar infraestructura de caché distribuida (Redis / NullCacheService)
// Regla VIII-3: Cache-Aside con TTL 60 s + Single-Flight / Mutex
builder.Services.AddInfrastructureCache(builder.Configuration);

// Registrar el Decorador de caché sobre ILibroRepository
// El CachedLibroRepository envuelve al LibroRepository concreto:
//   - Lecturas públicas: Cache-Aside sobre Redis (TTL 60 s, Single-Flight)
//   - Escrituras: nodo primario + invalidación de caché
//   - AnyAsync: siempre al primario (consistencia fuerte pre-escritura)
builder.Services.AddScoped<ILibroRepository>(sp =>
{
    var innerRepo = sp.GetRequiredService<LibroRepository>();
    var cache     = sp.GetRequiredService<ICacheService>();
    var logger    = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CachedLibroRepository>>();
    return new CachedLibroRepository(innerRepo, cache, logger);
});

// Registrar MediatR apilado en la capa de Aplicación
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(RegistrarPrestamoCommand).Assembly));

// Registrar NSwag Swagger OpenAPI Generator
builder.Services.AddOpenApiDocument(config =>
{
    config.PostProcess = document =>
    {
        document.Info.Title = "LibriKeep Pro - API REST de Circulación y Catalogación";
        document.Info.Version = "v1.0";
        document.Info.Description = "Especificación técnica inmutable para la gestión de biblioteca empresarial.";
    };
});

// Registrar servicios CORS para el consumo del Frontend React
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configurar el pipeline de solicitudes HTTP
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Activar CORS
app.UseCors();

// Activar NSwag Swagger UI
app.UseOpenApi();
app.UseSwaggerUi(config =>
{
    config.Path = "/swagger";
    config.DocumentPath = "/swagger/v1/swagger.json";
});

// Registrar Middleware Global de Excepciones antes del ruteo para aislar errores (RNF-3.2)
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

// Mapear rutas de controladores REST
app.MapControllers();

app.Run();

public partial class Program { }

