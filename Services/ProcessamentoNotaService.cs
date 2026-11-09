using Microsoft.EntityFrameworkCore;
using SistemaEntradaNotaFiscal.Data;
using SistemaEntradaNotaFiscal.DTO;
using SistemaEntradaNotaFiscal.DTOs;

namespace SistemaEntradaNotaFiscal.Services;

public class ProcessamentoNotaService {
    private readonly HmlContext _context;
    private readonly NotasEntradasService _notasEntradasService;
    private readonly ISeniorNotaFiscalService _senior;

    public ProcessamentoNotaService(
        HmlContext context,
        NotasEntradasService notasEntradasService,
        ISeniorNotaFiscalService senior) {
        _context = context;
        _notasEntradasService = notasEntradasService;
        _senior = senior;
    }

    public async Task<List<ResultadoProcessamentoDto>> ProcessarLoteAsync(
        List<SalvarNotaRequest> notas,
        CancellationToken ct) {
        var resultados = new List<ResultadoProcessamentoDto>();

        foreach (var request in notas) {
            ct.ThrowIfCancellationRequested();

            try {
                var nota = await MontarNotaAsync(request, ct);

                var resultado = await _senior.GravarNotaAsync(nota, ct);

                resultados.Add(resultado);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) {
                throw;
            }
            catch (Exception ex) {
                resultados.Add(new ResultadoProcessamentoDto {
                    CodEmp = request.CodEmp,
                    CodFil = request.CodFil,
                    CodFor = request.CodFor,
                    NumNfc = request.NumNfc,
                    CodSnf = request.CodSnf,
                    Sucesso = false,
                    Mensagem = ex.Message
                });
            }
        }

        return resultados;
    }

    private async Task<NotaCompletaDto> MontarNotaAsync(
        SalvarNotaRequest request,
        CancellationToken ct) {
        var capa = await _context.NotasEntradas
            .AsNoTracking()
            .FirstOrDefaultAsync(n =>
                n.CodEmp == request.CodEmp &&
                n.CodFil == request.CodFil &&
                n.CodFor == request.CodFor &&
                n.NumNfc == request.NumNfc &&
                n.CodSnf == request.CodSnf, ct);

        if (capa == null)
            throw new Exception("Nota fiscal não encontrada.");

        var itens = await _notasEntradasService.ListarItens(
            request.NumNfc,
            checked((short)request.CodEmp),
            request.CodFil,
            request.CodFor,
            request.CodSnf);

        if (itens.Count == 0)
            throw new Exception("Nota fiscal sem produtos.");

        var nota = new NotaCompletaDto {
            CodEmp = capa.CodEmp,
            CodFil = capa.CodFil,
            CodFor = capa.CodFor,
            NumNfc = capa.NumNfc,
            CodSnf = capa.CodSnf,

            DatEnt = request.DatEnt ?? capa.DatEnt,
            DatEmi = request.DatEmi ?? capa.DatEmi,
            TnsPro = request.TnsPro ?? capa.TnsPro,

            Produtos = itens.Select(item => new ProdutoNotaDto {
                SeqIpc = item.SeqIpc,

                CodPro = item.CodPro,
                TnsPro = item.TnsPro,

                QtdRec = item.QtdRec,
                PreUni = item.PreUni,
                VlrLiq = item.VlrLiq,
                VlrBru = item.VlrBru,

                VlrBsi = item.VlrBsi,
                VlrBic = item.VlrBic,

                USU_CodMot = item.USU_CodMot
            }).ToList()
        };

        var alteracoes = request.Itens ?? new();

        if (alteracoes.GroupBy(i => i.SeqIpc).Any(g => g.Count() > 1))
            throw new Exception("Existem sequências de itens duplicadas.");

        foreach (var alteracao in alteracoes) {
            var item = nota.Produtos
                .FirstOrDefault(i => i.SeqIpc == alteracao.SeqIpc);

            if (item == null)
                throw new Exception(
                    $"Item {alteracao.SeqIpc} não encontrado na nota.");

            if (alteracao.CodPro != null)
                item.CodPro = alteracao.CodPro;

            if (alteracao.TnsPro != null)
                item.TnsPro = alteracao.TnsPro;

            if (alteracao.QtdRec.HasValue)
                item.QtdRec = alteracao.QtdRec.Value;

            if (alteracao.VlrBsi.HasValue)
                item.VlrBsi = alteracao.VlrBsi.Value;

            if (alteracao.VlrBic.HasValue)
                item.VlrBic = alteracao.VlrBic.Value;

            if (alteracao.USU_CodMot != null)
                item.USU_CodMot = alteracao.USU_CodMot;
        }

        return nota;
    }
}