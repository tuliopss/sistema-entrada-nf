using SistemaEntradaNotaFiscal.DTOs;

namespace SistemaEntradaNotaFiscal.Services;

public interface ISeniorNotaFiscalService {
    Task<ResultadoProcessamentoDto> GravarNotaAsync(
        NotaCompletaDto nota,
        CancellationToken cancellationToken);

    Task<List<ResultadoProcessamentoDto>> ReabilitarNotasAsync(
        IReadOnlyList<DTO.ChaveNotaRequest> notas, CancellationToken cancellationToken);

    Task<List<ResultadoProcessamentoDto>> FecharNotasAsync(
        IReadOnlyList<DTO.ChaveNotaRequest> notas, CancellationToken cancellationToken);

    //Task<ResultadoProcessamentoDto?> GravarNotaAsync(
}
