using LibriKeep.Presentation.API.Middleware;
using LibriKeep.Core.Application.Circulacion.Commands.RegistrarPrestamo;
using LibriKeep.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Registrar controladores de la API configurados con serialización de strings para Enums
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Registrar capa de infraestructura y persistencia (PostgreSQL / In-Memory Db fallback)
builder.Services.AddInfrastructurePersistence(builder.Configuration);

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
