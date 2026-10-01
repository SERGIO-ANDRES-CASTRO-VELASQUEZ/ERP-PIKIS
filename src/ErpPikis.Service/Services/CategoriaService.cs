using ErpPikis.Domain.Entities;
using ErpPikis.Domain.Exceptions;
using ErpPikis.Repository.Interfaces;
using ErpPikis.Service.Common;
using ErpPikis.Service.Dtos;
using ErpPikis.Service.Interfaces;

namespace ErpPikis.Service.Services;

public class CategoriaService : ICategoriaService
{
    private readonly ICategoriaRepository _categorias;
    private readonly IUnitOfWork _uow;

    public CategoriaService(ICategoriaRepository categorias, IUnitOfWork uow)
    {
        _categorias = categorias;
        _uow = uow;
    }

    public async Task<List<CategoriaDto>> ListarAsync(CancellationToken ct = default) =>
        (await _categorias.ListarAsync(ct)).Select(c => c.ADto()).ToList();

    public async Task<CategoriaDto> CrearAsync(CrearCategoriaRequest request, CancellationToken ct = default)
    {
        var nombre = request.Nombre.Trim();
        if (await _categorias.ExisteNombreAsync(nombre, ct))
            throw new DomainException($"La categoría '{nombre}' ya existe.");

        var categoria = new Categoria { Nombre = nombre };
        _categorias.Agregar(categoria);
        await _uow.GuardarCambiosAsync(ct);
        return categoria.ADto();
    }
}
