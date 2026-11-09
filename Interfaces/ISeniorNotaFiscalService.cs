using SistemaEntradaNotaFiscal.DTOs;

namespace SistemaEntradaNotaFiscal.Services;

public interface ISeniorNotaFiscalService {
    Task<ResultadoProcessamentoDto> GravarNotaAsync(
        NotaCompletaDto nota,
        CancellationToken cancellationToken);
}