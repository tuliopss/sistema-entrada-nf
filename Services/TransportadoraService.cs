using Microsoft.EntityFrameworkCore;
using SistemaEntradaNotaFiscal.Data;
using SistemaEntradaNotaFiscal.Models;

namespace SistemaEntradaNotaFiscal.Services;

public class TransportadoraService(HmlContext context) {
    public Task<Transportadora?> BuscarAsync(int codTra, CancellationToken ct) =>
        context.Set<Transportadora>().AsNoTracking()
            .SingleOrDefaultAsync(t => t.CodTra == codTra, ct);
}
