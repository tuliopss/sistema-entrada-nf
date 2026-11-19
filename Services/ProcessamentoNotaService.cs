using Microsoft.EntityFrameworkCore;
using SistemaEntradaNotaFiscal.Data;
using SistemaEntradaNotaFiscal.DTO;
using SistemaEntradaNotaFiscal.DTOs;
using SistemaEntradaNotaFiscal.Models;

namespace SistemaEntradaNotaFiscal.Services;

public class ProcessamentoNotaService {
    private readonly HmlContext _context;
    private readonly NotasEntradasService _notasEntradasService;
    private readonly ISeniorNotaFiscalService _senior;
    private readonly ILogger<ProcessamentoNotaService> _logger;
    private readonly NotaVendaService _notaVendaService;

    public ProcessamentoNotaService(
        HmlContext context,
        NotasEntradasService notasEntradasService,
        ISeniorNotaFiscalService senior,
        ILogger<ProcessamentoNotaService> logger,
        NotaVendaService notaVendaService) {
        _context = context;
        _notasEntradasService = notasEntradasService;
        _senior = senior;
        _logger = logger;
        _notaVendaService = notaVendaService;
    }

    public async Task<List<ResultadoProcessamentoDto>> ProcessarLoteAsync(
        List<SalvarNotaRequest> notas,
        CancellationToken ct) {
        var resultados = new List<ResultadoProcessamentoDto>();

        foreach (var request in notas) {
            ct.ThrowIfCancellationRequested();

            var resultado = new ResultadoProcessamentoDto {
                CodEmp = request.CodEmp,
                CodFil = request.CodFil,
                CodFor = request.CodFor,
                NumNfc = request.NumNfc,
                CodSnf = request.CodSnf
            };
            try {
                var nota = await MontarNotaAsync(request, ct);
                resultado = await _senior.GravarNotaAsync(nota, ct);
                if (resultado.Sucesso)
                    await ConferirParcelasAsync(nota, resultado, ct);
                if (resultado.Sucesso && request.Itens?.Any(i => i.VendasSugeridas != null) == true)
                    await ConferirReferenciasAsync(nota, request.Itens, resultado, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) {
                throw;
            }
            catch (ArgumentException ex) {
                resultado.Mensagem = ex.Message;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Falha ao preparar a gravação da nota {NumNfc}", request.NumNfc);
                resultado.Mensagem = "Falha ao preparar a gravação.";
            }
            resultados.Add(resultado);
        }

        return resultados;
    }

    private async Task<NotaCompletaDto> MontarNotaAsync(
        SalvarNotaRequest request,
        CancellationToken ct) {
        if (request.CodEmp is <= 0 or > short.MaxValue || request.CodFil <= 0 ||
            request.CodFor <= 0 || request.NumNfc <= 0 ||
            string.IsNullOrWhiteSpace(request.CodSnf) || request.CodSnf.Length > 3)
            throw new ArgumentException("Chave da nota inválida.");

        var alteracoes = request.Itens ?? new();

        if (request.DatEnt == null && request.DatEmi == null && request.TnsPro == null && request.CodTra == null && alteracoes.Count == 0)
            throw new ArgumentException("Nenhuma alteração informada.");

        if (request.TnsPro != null && (string.IsNullOrWhiteSpace(request.TnsPro) || request.TnsPro.Length > 5))
            throw new ArgumentException("Transação da capa inválida.");

        if (request.CodTra.HasValue && (request.CodTra <= 0 ||
            !await _context.Set<Transportadora>().AsNoTracking().AnyAsync(t => t.CodTra == request.CodTra.Value, ct)))
            throw new ArgumentException("Código de transportadora inválido ou não cadastrado.");

        var capa = await _context.NotasEntradas
            .AsNoTracking()
            .FirstOrDefaultAsync(n =>
                n.CodEmp == request.CodEmp &&
                n.CodFil == request.CodFil &&
                n.CodFor == request.CodFor &&
                n.NumNfc == request.NumNfc &&
                n.CodSnf == request.CodSnf, ct);

        if (capa == null)
            throw new ArgumentException("Nota fiscal não encontrada.");

        if (capa.SitNfc.Trim() != "1")
            throw new ArgumentException("A gravação exige uma nota na situação Digitada.");

        var itens = await _notasEntradasService.ListarItens(
            request.NumNfc,
            checked((short)request.CodEmp),
            request.CodFil,
            request.CodFor,
            request.CodSnf,
            ct,
            incluirProduto: false);

        if (itens.Count == 0)
            throw new ArgumentException("Nota fiscal sem produtos.");

        var parcelas = await ConsultarParcelasAsync(capa.CodEmp, capa.CodFil, capa.CodFor, capa.NumNfc, capa.CodSnf, ct);

        var nota = new NotaCompletaDto {
            CodEmp = capa.CodEmp,
            CodFil = capa.CodFil,
            CodFor = capa.CodFor,
            NumNfc = capa.NumNfc,
            CodSnf = capa.CodSnf,
            TipNfe = capa.TipNfe,

            DatEnt = request.DatEnt ?? capa.DatEnt,
            DatEmi = request.DatEmi ?? capa.DatEmi,
            ChvNel = capa.ChvNel,
            TnsPro = request.TnsPro ?? capa.TnsPro,
            CodTra = request.CodTra ?? capa.CodTra,
            Parcelas = parcelas,

            Produtos = itens.Select(item => new ProdutoNotaDto {
                SeqIpc = item.SeqIpc,
                EmpNfv = item.EmpNfv,
                FilNfv = item.FilNfv,
                SnfNfv = item.SnfNfv,
                NumNfv = item.NumNfv,
                SeqIpv = item.SeqIpv,

                CodPro = item.CodPro,
                CodDer = item.CodDer,
                TnsPro = request.TnsPro != null && request.TnsPro != capa.TnsPro
                    ? request.TnsPro
                    : item.TnsPro,

                QtdRec = item.QtdRec,
                PreUni = item.PreUni,
                VlrLiq = item.VlrLiq,
                VlrBru = item.VlrBru,

                VlrBsi = item.VlrBsi,
                VlrBic = item.VlrBic,

                USU_CodMot = item.USU_CodMot
            }).ToList()
        };

        if (alteracoes.GroupBy(i => i.SeqIpc).Any(g => g.Count() > 1))
            throw new ArgumentException("Existem sequências de itens duplicadas.");

        var produtosPorSequencia = nota.Produtos.ToDictionary(i => i.SeqIpc);
        foreach (var alteracao in alteracoes) {
            if (alteracao.VendasSugeridas != null &&
                (alteracao.VendasSugeridas.Count != 1 || alteracao.VendasSugeridas.Any(v => v == null)))
                throw new ArgumentException($"Item {alteracao.SeqIpc}: a gravação de referências exige uma única venda que atenda toda a quantidade. O rateio entre várias vendas ainda não está disponível.");
            if (!produtosPorSequencia.TryGetValue(alteracao.SeqIpc, out var item))
                throw new ArgumentException(
                    $"Item {alteracao.SeqIpc} não encontrado na nota.");

            if (alteracao.CodPro != null && (string.IsNullOrWhiteSpace(alteracao.CodPro) || alteracao.CodPro.Length > 14))
                throw new ArgumentException($"Código de produto inválido no item {item.SeqIpc}.");

            if (alteracao.TnsPro != null && (string.IsNullOrWhiteSpace(alteracao.TnsPro) || alteracao.TnsPro.Length > 5))
                throw new ArgumentException($"Transação inválida no item {item.SeqIpc}.");

            if (alteracao.CodPro != null) {
                if (alteracao.CodPro != item.CodPro.TrimEnd())
                    item.CodDer = null;

                item.CodPro = alteracao.CodPro;
            }

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

        var referencias = alteracoes.Where(a => a.VendasSugeridas != null).ToList();
        if (referencias.Count > 0) {
            if (itens.Any(i => i.NumNfv.GetValueOrDefault() > 0))
                throw new ArgumentException("As sugestões só podem ser aplicadas a uma nota sem vendas referenciadas.");

            // Revalida no servidor com os produtos e quantidades efetivamente enviados.
            foreach (var original in itens) {
                var atualizado = produtosPorSequencia[original.SeqIpc];
                original.CodPro = atualizado.CodPro;
                original.QtdRec = atualizado.QtdRec;
            }
            var vendas = await _notaVendaService.VendasUltimos6mesesCliente(new ChaveNotaRequest {
                CodEmp = capa.CodEmp, CodFil = capa.CodFil, CodFor = capa.CodFor,
                NumNfc = capa.NumNfc, CodSnf = capa.CodSnf
            }, ct, nota.DatEmi) ?? [];
            var sugestoes = NotaVendaService.MontarSugestaoDeDevolucao(capa.CodEmp, itens, vendas).Sugestoes;
            foreach (var alteracao in referencias) {
                var sugestao = sugestoes.SingleOrDefault(s => s.SeqIpc == alteracao.SeqIpc);
                if (sugestao == null || sugestao.QuantidadeFaltante > 0 ||
                    !sugestao.Vendas.SequenceEqual(alteracao.VendasSugeridas!))
                    throw new ArgumentException($"Item {alteracao.SeqIpc}: a sugestão mudou ou não tem saldo suficiente. Consulte as sugestões novamente.");
                var origem = alteracao.VendasSugeridas![0];
                var produto = produtosPorSequencia[alteracao.SeqIpc];
                produto.EmpNfv = origem.CodEmp;
                produto.FilNfv = origem.CodFil;
                produto.SnfNfv = origem.CodSnf;
                produto.NumNfv = origem.NumNfv;
                produto.SeqIpv = origem.SeqIpv;
            }
        }

        await CompletarDerivacoesAsync(nota, ct);
        return nota;
    }

    private async Task CompletarDerivacoesAsync(NotaCompletaDto nota, CancellationToken ct) {
        var porProduto = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var produto in nota.Produtos.Where(p => string.IsNullOrWhiteSpace(p.CodDer))) {
            var codigo = produto.CodPro.Trim();
            if (!porProduto.TryGetValue(codigo, out var derivacao)) {
                // Somente leitura: a persistência continua exclusivamente no Web Service.
                var cadastradas = await _context.Database.SqlQuery<string>($"""
                    SELECT CodDer AS Value FROM E075DER
                    WHERE CodEmp = {nota.CodEmp} AND CodPro = {codigo}
                    """).ToListAsync(ct);
                var validas = cadastradas.Select(d => d.Trim())
                    .Where(d => d.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (validas.Count > 1)
                    throw new ArgumentException($"Item {produto.SeqIpc}: o produto {codigo} está sem derivação na entrada e possui várias derivações cadastradas ({string.Join(", ", validas)}). Informe a derivação correta antes de processar.");
                derivacao = validas.SingleOrDefault();
                porProduto[codigo] = derivacao;
            }
            produto.CodDer = derivacao;
        }
    }

    private async Task ConferirReferenciasAsync(
        NotaCompletaDto nota, List<ItemAlteradoDto> alteracoes,
        ResultadoProcessamentoDto resultado, CancellationToken ct) {
        try {
            var sequencias = alteracoes.Where(a => a.VendasSugeridas != null).Select(a => a.SeqIpc).ToList();
            var gravados = await _context.ItemNota.AsNoTracking()
                .Where(i => i.CodEmp == nota.CodEmp && i.CodFil == nota.CodFil &&
                    i.CodFor == nota.CodFor && i.NumNfc == nota.NumNfc && i.CodSnf == nota.CodSnf &&
                    sequencias.Contains(i.SeqIpc))
                .Select(i => new { i.SeqIpc, i.EmpNfv, i.FilNfv, i.SnfNfv, i.NumNfv, i.SeqIpv })
                .ToListAsync(ct);
            var esperados = nota.Produtos.Where(p => sequencias.Contains(p.SeqIpc)).ToList();
            if (gravados.Count == esperados.Count && esperados.All(p => gravados.Any(i =>
                i.SeqIpc == p.SeqIpc && i.EmpNfv == p.EmpNfv && i.FilNfv == p.FilNfv &&
                i.NumNfv == p.NumNfv && i.SeqIpv == p.SeqIpv && i.SnfNfv?.Trim() == p.SnfNfv?.Trim())))
                return;
            resultado.Mensagem = "A Senior retornou sucesso, mas o vínculo de venda não foi confirmado nos itens da nota.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) {
            throw;
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Falha ao conferir referências da nota {NumNfc}", nota.NumNfc);
            resultado.Mensagem = "A Senior retornou sucesso, mas a conferência das referências de venda falhou.";
        }
        resultado.Sucesso = false;
        resultado.ConfirmacaoPendente = true;
    }

    private async Task ConferirParcelasAsync(
        NotaCompletaDto nota, ResultadoProcessamentoDto resultado, CancellationToken ct) {
        try {
            var gravadas = await ConsultarParcelasAsync(
                checked((short)nota.CodEmp), nota.CodFil, nota.CodFor, nota.NumNfc, nota.CodSnf, ct);

            if (nota.Parcelas.SequenceEqual(gravadas))
                return;

            resultado.Mensagem = $"A Senior confirmou a alteração, mas as parcelas divergem do original ({nota.Parcelas.Count} antes, {gravadas.Count} depois).";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) {
            throw;
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Falha ao conferir parcelas da nota {NumNfc} após a gravação", nota.NumNfc);
            resultado.Mensagem = "A Senior confirmou a alteração, mas a conferência das parcelas falhou.";
        }

        resultado.Sucesso = false;
        resultado.ConfirmacaoPendente = true;
    }

    private Task<List<ParcelaNotaDto>> ConsultarParcelasAsync(
        short codEmp, int codFil, int codFor, int numNfc, string codSnf, CancellationToken ct) =>
        _context.Database.SqlQuery<ParcelaNotaDto>($"""
            SELECT CodPar, NumTit, CodTpt, CodCrp, CodNtg, VctPar, VlrPar, PerDdp, QtdDdd,
                   CodFcr, DatFcr, CodPor, CodCrt, ObsPar, VlrDsc, JrsNeg, MulNeg, OutNeg,
                   DatNeg, CodBan, CodAge, CcbFor, CodFav, CodFpg, NumCpr, CodRep, NumCoo,
                   CroEcf, CodEqu, TipTcc, IndPag, VlrInt, DocIdeFav, IdeFav
            FROM E440PAR
            WHERE CodEmp = {codEmp} AND CodFil = {codFil} AND CodFor = {codFor}
              AND NumNfc = {numNfc} AND CodSnf = {codSnf}
            """)
            .OrderBy(p => p.CodPar)
            .ToListAsync(ct);
}
