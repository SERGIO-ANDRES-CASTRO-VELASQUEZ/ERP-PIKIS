using ErpPikis.Domain.Exceptions;
using ErpPikis.Service.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpPikis.Api.Middleware;

public class ManejadorErrores : IExceptionHandler
{
    private readonly ILogger<ManejadorErrores> _logger;

    public ManejadorErrores(ILogger<ManejadorErrores> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, titulo, detalle) = exception switch
        {
            DomainException e => (StatusCodes.Status400BadRequest, "Regla de negocio", e.Message),
            NoEncontradoException e => (StatusCodes.Status404NotFound, "No encontrado", e.Message),
            CredencialesInvalidasException e => (StatusCodes.Status401Unauthorized, "No autorizado", e.Message),
            ImpresoraException e => (StatusCodes.Status503ServiceUnavailable, "Impresora no disponible", e.Message),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflicto",
                "Otro usuario modificó los mismos datos al mismo tiempo. Intente de nuevo."),
            DbUpdateException => (StatusCodes.Status409Conflict, "Conflicto",
                "La operación viola una restricción de la base de datos (dato duplicado o inválido)."),
            _ => (StatusCodes.Status500InternalServerError, "Error interno", "Ocurrió un error inesperado.")
        };

        if (status == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Error no controlado");
        else if (exception is DbUpdateException or ImpresoraException)
            _logger.LogWarning(exception, "{Titulo}", titulo);

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = titulo,
            Detail = detalle
        }, ct);

        return true;
    }
}
