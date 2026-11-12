using Microsoft.EntityFrameworkCore;
using SistemaEntradaNotaFiscal.Data;
using SistemaEntradaNotaFiscal.DTO;
using SistemaEntradaNotaFiscal.DTOs;

namespace SistemaEntradaNotaFiscal.Services;

public class ReabilitacaoNotaService {
    private readonly HmlContext _context;
    private readonly ISeniorNotaFiscalService _senior;

    public ReabilitacaoNotaService(HmlContext context, ISeniorNotaFiscalService senior) {
        _context = context;
        _senior = senior;
    }

    public async Task<List<ResultadoProcessamentoDto>> ReabilitarLoteAsync(
        IReadOnlyList<ReabilitarNotaRequest> notas, CancellationToken ct) {
        var elegiveis = new List<ReabilitarNotaRequest>();
        var resultados = new List<ResultadoProcessamentoDto>();

        foreach (var nota in notas) {
            var serie = nota.CodSnf.Trim();
            var situacao = await _context.NotasEntradas
                .AsNoTracking()
                .Where(n => n.CodEmp == nota.CodEmp && n.CodFil == nota.CodFil &&
                    n.CodFor == nota.CodFor && n.NumNfc == nota.NumNfc &&
                    n.CodSnf == serie && n.TipNfe == 2)
                .Select(n => n.SitNfc)
                .SingleOrDefaultAsync(ct);

            if (situacao?.Trim() == "2") {
                elegiveis.Add(nota);
                continue;
            }

            resultados.Add(new ResultadoProcessamentoDto {
                CodEmp = nota.CodEmp,
                CodFil = nota.CodFil,
                CodFor = nota.CodFor,
                NumNfc = nota.NumNfc,
                CodSnf = nota.CodSnf
                //Mensagem = situacao == null
                //    ? "Nota de devolução não encontrada."
                //    : "A reabilitação exige uma nota na situação 2 (Fechada)."
            });
        }

        if (elegiveis.Count > 0)
            resultados.AddRange(await _senior.ReabilitarNotasAsync(elegiveis, ct));

        return resultados;
    }
}
