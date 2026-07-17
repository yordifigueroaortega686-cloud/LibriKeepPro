using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using LibriKeep.Core.Domain.Exceptions;
using LibriKeep.Presentation.API.Controllers; // Para acceder a ErrorResponse

namespace LibriKeep.Presentation.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Excepción de dominio capturada en middleware: {ErrorCode} - {Message}", ex.ErrorCode, ex.Message);
                await WriteErrorResponseAsync(context, StatusCodes.Status400BadRequest, ex.ErrorCode, "INFRACCIÓN DE REGLA DE NEGOCIO", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción no manejada capturada en middleware");
                await WriteErrorResponseAsync(context, StatusCodes.Status500InternalServerError, "ERR_INTERNAL_SERVER_ERROR", "ERROR INTERNO DEL SERVIDOR", "Ha ocurrido un error inesperado. Por favor, contacte al administrador.");
            }
        }

        private static async Task WriteErrorResponseAsync(HttpContext context, int statusCode, string errorCode, string title, string detail)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var errorResponse = new ErrorResponse
            {
                Code = errorCode,
                Title = title,
                Detail = detail
            };

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(errorResponse, options);

            await context.Response.WriteAsync(json);
        }
    }
}
