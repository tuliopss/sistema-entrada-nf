using Microsoft.EntityFrameworkCore;
using SistemaEntradaNotaFiscal.Data;
using SistemaEntradaNotaFiscal.DTO;
using SistemaEntradaNotaFiscal.DTOs;

namespace SistemaEntradaNotaFiscal.Services;

public class FechamentoNotaService(
    HmlContext context, NotasEntradasService notasEntradas, NotaVendaService notasVenda,
    ISeniorNotaFiscalService senior, ILogger<FechamentoNotaService> logger) {
    public async Task<List<ResultadoProcessamentoDto>> FecharLoteAsync(
        IReadOnlyList<ChaveNotaRequest> notas, CancellationToken ct) {
        var elegiveis = new List<ChaveNotaRequest>();
        var resultados = new List<ResultadoProcessamentoDto>();

        foreach (var nota in notas) {
            var (erro, sugestoes) = await ValidarFechamentoAsync(nota, ct);
            if (erro == null) {
                elegiveis.Add(nota);
                continue;
            }

            resultados.Add(new ResultadoProcessamentoDto {
                CodEmp = nota.CodEmp, CodFil = nota.CodFil, CodFor = nota.CodFor,
                NumNfc = nota.NumNfc, CodSnf = nota.CodSnf,
                Mensagem = erro,
                SugestoesVenda = sugestoes
            });
        }

        if (elegiveis.Count == 0)
            return resultados;

        var retornos = await senior.FecharNotasAsync(elegiveis, ct);
        foreach (var retorno in retornos) {
            try {
                // A porta retorna uma mensagem por nota. A situação persistida confirma o fechamento.
                var situacao = await ConsultarNota(
                    (short)retorno.CodEmp, retorno.CodFil, retorno.CodFor, retorno.NumNfc, retorno.CodSnf)
                    .Select(n => n.SitNfc)
                    .SingleOrDefaultAsync(ct);
                retorno.Sucesso = situacao?.Trim() == "2";
                retorno.ConfirmacaoPendente = !retorno.Sucesso;
                if (retorno.Sucesso) {
                    retorno.Mensagem = "Nota fechada.";
                } else {
                    retorno.Mensagem = $"Fechamento não confirmado no ERP. {retorno.Mensagem}".TrimEnd();
                }
            }
            catch (Exception ex) when (!ct.IsCancellationRequested) {
                logger.LogWarning(ex, "Falha ao conferir o fechamento da nota {NumNfc}", retorno.NumNfc);
                retorno.ConfirmacaoPendente = true;
                retorno.Mensagem = $"Não foi possível conferir o fechamento no ERP. {retorno.Mensagem}".TrimEnd();
            }
            resultados.Add(retorno);
        }
        return resultados;
    }

    private async Task<(string? Erro, IReadOnlyList<SugestaoVinculoVendaDto>? Sugestoes)> ValidarFechamentoAsync(
        ChaveNotaRequest nota, CancellationToken ct) {
        var capa = await ConsultarNota(nota.CodEmp, nota.CodFil, nota.CodFor, nota.NumNfc, nota.CodSnf)
            .Select(n => new { n.SitNfc, n.DatEmi })
            .SingleOrDefaultAsync(ct);
        if (capa == null)
            return ("Nota de devolução não encontrada.", null);

        if (capa.SitNfc.Trim() != "1")
            return ("O fechamento exige uma nota na situação 1 (Digitada).", null);

        var itens = await notasEntradas.ListarItens(
            nota.NumNfc, nota.CodEmp, nota.CodFil, nota.CodFor, nota.CodSnf.Trim(), ct, incluirProduto: false);

        var itensComReferencia = itens.Where(i => i.NumNfv.GetValueOrDefault() > 0).ToList();
        if (itensComReferencia.Count == 0)
            return await notasVenda.SugerirVendasParaDevolucaoAsync(nota, itens, ct);

        var vendas = await notasVenda.RecuperarNotasVendaEntrada(nota, ct);
        var sequenciasVinculadas = vendas.Select(v => v.SeqIpc).ToHashSet();
        var vinculoInvalido = itensComReferencia.FirstOrDefault(i => !sequenciasVinculadas.Contains(i.SeqIpc));
        if (vinculoInvalido != null)
            return ($"Item de venda referenciado pelo item {vinculoInvalido.SeqIpc} não encontrado.", null);

        return (NotaVendaService.ValidaNotaVenda(vendas, capa.DatEmi, itens), null);
    }

    private IQueryable<Models.NotasEntrada> ConsultarNota(
        short empresa, int filial, int fornecedor, int numero, string serie) {
        var serieNormalizada = serie.Trim();
        return context.NotasEntradas
            .AsNoTracking()
            .Where(n => n.CodEmp == empresa && n.CodFil == filial && n.CodFor == fornecedor &&
                n.NumNfc == numero && n.CodSnf == serieNormalizada && n.TipNfe == 2);
    }
}
