using Microsoft.EntityFrameworkCore;
using SistemaEntradaNotaFiscal.Data;
using SistemaEntradaNotaFiscal.DTO;
using SistemaEntradaNotaFiscal.DTOs;
using SistemaEntradaNotaFiscal.Models;
using System.Text.Json;

namespace SistemaEntradaNotaFiscal.Services {
    public class NotaVendaService {
        private readonly HmlContext _context;
        private readonly ILogger<NotaVendaService> _logger;

        public NotaVendaService(HmlContext context, ILogger<NotaVendaService> logger) {
            _context = context;
            _logger = logger;
        }

        public async Task<List<ItemVendaReferenciadoDto>> RecuperarNotasVendaEntrada(
            ChaveNotaRequest nota, CancellationToken cancellationToken) {
            var serie = nota.CodSnf.Trim();

            return await _context.ItemNota
                .AsNoTracking()
                .Where(i => i.CodEmp == nota.CodEmp && i.CodFil == nota.CodFil &&
                    i.CodFor == nota.CodFor && i.NumNfc == nota.NumNfc && i.CodSnf == serie &&
                    i.ItemVendaOrigem != null)
                .OrderBy(i => i.SeqIpc)
                .Select(i => new ItemVendaReferenciadoDto(
                    i.SeqIpc,
                    i.ItemVendaOrigem!.CodEmp,
                    i.ItemVendaOrigem.CodFil,
                    i.ItemVendaOrigem.CodSnf,
                    i.ItemVendaOrigem.NumNfv,
                    i.ItemVendaOrigem.SeqIpv,
                    i.ItemVendaOrigem.Nota.DatEmi,
                    i.ItemVendaOrigem.CodPro,
                    i.ItemVendaOrigem.CodDer,
                    i.ItemVendaOrigem.QtdFat,
                    i.ItemVendaOrigem.PreUni,
                    i.ItemVendaOrigem.VlrBru,
                    i.ItemVendaOrigem.VlrLiq))
                .ToListAsync(cancellationToken);              
        }

        public static string? ValidaNotaVenda(
            IReadOnlyList<ItemVendaReferenciadoDto> itensVenda, DateTime dataEmissaoEntrada,
            IReadOnlyList<ItemNotaEntrada> itensEntrada) {

            var produtosPorSequencia = itensEntrada.ToDictionary(p => p.SeqIpc);
            var itensPorOrigem = itensVenda.GroupBy(v =>
                (v.CodEmp, v.CodFil, v.CodSnf, v.NumNfv, v.SeqIpv));

            foreach (var grupo in itensPorOrigem) {
                var venda = grupo.First();
                var origem = $"venda {venda.NumNfv}/{venda.CodSnf}, item {venda.SeqIpv}";
                if (venda.DatEmi.Date > dataEmissaoEntrada.Date)
                    return $"A data de emissão da {origem} é posterior à da nota de entrada.";

                decimal quantidadeDevolvida = 0;
                foreach (var referencia in grupo) {
                    if (!produtosPorSequencia.TryGetValue(referencia.SeqIpc, out var produto))
                        return $"Item de entrada {referencia.SeqIpc} não encontrado para a {origem}.";

                    if (!string.Equals(produto.CodPro.Trim(), venda.CodPro?.Trim(), StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(produto.CodDer?.Trim() ?? "", venda.CodDer?.Trim() ?? "", StringComparison.OrdinalIgnoreCase))
                        return $"O produto ou a derivação do item de entrada {produto.SeqIpc} difere da {origem}.";

                    if (produto.QtdRec <= 0)
                        return $"A quantidade devolvida do item de entrada {produto.SeqIpc} deve ser positiva.";

                    quantidadeDevolvida += produto.QtdRec;
                }

                if (venda.QtdFat is not decimal quantidadeFaturada)
                    return $"A quantidade faturada da {origem} não está informada.";

                if (quantidadeDevolvida > quantidadeFaturada)
                    return $"A quantidade devolvida ({quantidadeDevolvida}) excede a faturada ({quantidadeFaturada}) na {origem}.";
            }

            return null;
        }

        public async Task<List<NotaVenda>?> VendasUltimos6mesesCliente(
            ChaveNotaRequest nota, CancellationToken ct = default, DateTime? dataReferencia = null) {
            var serie = nota.CodSnf.Trim();
            var entrada = await _context.NotasEntradas
                .AsNoTracking()
                .Where(n => n.CodEmp == nota.CodEmp && n.CodFil == nota.CodFil &&
                            n.CodFor == nota.CodFor && n.NumNfc == nota.NumNfc && n.CodSnf == serie)
                .Select(n => new { n.DatEmi, n.Fornecedor.CodCli })
                .SingleOrDefaultAsync(ct);

            if (entrada == null)
                return null;

            if (entrada.CodCli is not int codCliente || codCliente <= 0)
                return new List<NotaVenda>();

           
            var dataEmissao = (dataReferencia ?? entrada.DatEmi).Date;
            var inicio = dataEmissao.AddMonths(-6);
            var fimExclusivo = dataEmissao;
            
            return await _context.NotasVenda
                .AsNoTracking()
                .Include(nf => nf.Itens.OrderBy(item => item.SeqIpv))
                .Where(nf => nf.CodCli == codCliente &&
                             nf.DatEmi >= inicio &&
                             nf.DatEmi < fimExclusivo)
                .OrderByDescending(nf => nf.DatEmi)
                .ToListAsync(ct);
        }

        public async Task<(string Mensagem, IReadOnlyList<SugestaoVinculoVendaDto> Sugestoes)> SugerirVendasParaDevolucaoAsync(
            ChaveNotaRequest nota, IReadOnlyList<ItemNotaEntrada> itens, CancellationToken ct) {
            if (itens.Count == 0) {
                _logger.LogInformation(
                    "Consulta de sugestões sem itens. Entrada {CodEmp}/{CodFil}/{CodFor}/{NumNfc}/{CodSnf}",
                    nota.CodEmp, nota.CodFil, nota.CodFor, nota.NumNfc, nota.CodSnf);
                return ("Nota de entrada sem itens de produto para consultar.", []);
            }

            var vendas = await VendasUltimos6mesesCliente(nota, ct) ?? [];
            var resultado = MontarSugestaoDeDevolucao(nota.CodEmp, itens, vendas);
            _logger.LogInformation(
                "Sugestões de venda. Entrada {CodEmp}/{CodFil}/{CodFor}/{NumNfc}/{CodSnf}. " +
                "Vendas consultadas {QuantidadeVendas}. Resultado {Mensagem}. Sugestões {Sugestoes}",
                nota.CodEmp, nota.CodFil, nota.CodFor, nota.NumNfc, nota.CodSnf,
                vendas.Count, resultado.Mensagem, JsonSerializer.Serialize(resultado.Sugestoes));
            return resultado;
        }

        internal static (string Mensagem, IReadOnlyList<SugestaoVinculoVendaDto> Sugestoes) MontarSugestaoDeDevolucao(
            short codEmp, IReadOnlyList<ItemNotaEntrada> itens, IReadOnlyList<NotaVenda> vendas) {
            var saldos = vendas
                .Where(v => v.CodEmp == codEmp && v.SitNfv.Trim() == "2")
                .OrderByDescending(v => v.DatEmi)
                .ThenBy(v => v.CodFil).ThenBy(v => v.CodSnf).ThenBy(v => v.NumNfv)
                .SelectMany(v => v.Itens.OrderBy(i => i.SeqIpv))
                .Where(i => i.QtdDev.GetValueOrDefault() >= 0)
                .Select(i => (Item: i, Saldo: i.QtdFat.GetValueOrDefault() - i.QtdDev.GetValueOrDefault()))
                .Where(v => v.Saldo > 0)
                .ToList();

            var sugestoes = new List<SugestaoVinculoVendaDto>();
            
            
            var itensInvalidos = new List<short>();
            foreach (var item in itens.OrderBy(i => i.SeqIpc)) {
                if (item.QtdRec <= 0 || string.IsNullOrWhiteSpace(item.CodPro) || string.IsNullOrWhiteSpace(item.UniNfc)) {
                    itensInvalidos.Add(item.SeqIpc);
                    continue;
                }

                var origens = new List<OrigemVendaSugeridaDto>();
                var restante = item.QtdRec;
                for (var indice = 0; indice < saldos.Count; indice++) {
                    var (venda, saldo) = saldos[indice];
                    if (saldo <= 0 ||
                        !string.Equals(venda.CodPro?.Trim(), item.CodPro.Trim(), StringComparison.OrdinalIgnoreCase))
                        continue;

                    var quantidade = Math.Min(saldo, restante);
                    saldos[indice] = (venda, saldo - quantidade);
                    restante -= quantidade;
                    origens.Add(new OrigemVendaSugeridaDto(
                        venda.CodEmp, venda.CodFil, venda.CodSnf, venda.NumNfv, venda.SeqIpv, quantidade));
                    if (restante == 0)
                        break;
                }

                sugestoes.Add(new SugestaoVinculoVendaDto(
                    item.SeqIpc, item.CodPro, item.UniNfc, item.QtdRec, restante, origens));
            }

            var mensagem = itensInvalidos.Count > 0
                ? $"Confira o produto, a quantidade e a unidade dos itens {string.Join(", ", itensInvalidos)}."
                : sugestoes.Any(s => s.QuantidadeFaltante > 0)
                    ? "Saldo insuficiente nos últimos seis meses para cobrir toda a devolução."
                    : "Há saldo para a devolução. Vincule as vendas sugeridas antes de fechar.";
            return (mensagem, sugestoes);
        }
    }
}
