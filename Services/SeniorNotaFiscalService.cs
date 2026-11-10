using SistemaEntradaNotaFiscal.DTOs;
using System.Globalization;
using System.ServiceModel;
using WS_NF_HML;

namespace SistemaEntradaNotaFiscal.Services;

public class SeniorNotaFiscalService : ISeniorNotaFiscalService {
    private static readonly CultureInfo CulturaSenior = CultureInfo.GetCultureInfo("pt-BR");
    private readonly IConfiguration _configuration;
    private readonly ILogger<SeniorNotaFiscalService> _logger;

    public SeniorNotaFiscalService(IConfiguration configuration, ILogger<SeniorNotaFiscalService> logger) {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ResultadoProcessamentoDto> GravarNotaAsync(
        NotaCompletaDto nota,
        CancellationToken cancellationToken) {
        var resultado = new ResultadoProcessamentoDto {
            CodEmp = nota.CodEmp,
            CodFil = nota.CodFil,
            CodFor = nota.CodFor,
            NumNfc = nota.NumNfc,
            CodSnf = nota.CodSnf
        };

        // A porta GravarNotasFiscaisEntrada usa 0 para permitir o recálculo.
        var tipCal = 0;
        if (!_configuration.GetValue<bool>("Senior:RecalcularValores") &&
            (!int.TryParse(_configuration["Senior:TipCalSemRecalculo"], out tipCal) ||
             tipCal is not (1 or 2))) {
            resultado.Mensagem = "Gravação bloqueada até configurar o código validado para desativar o recálculo.";
            return resultado;
        }

        // Esta integração fornece as parcelas originais. O modo 0 instrui
        // GravarNotasFiscaisEntrada a acatar os valores enviados.
        if (_configuration["Senior:GerParSemGerarParcelas"]?.Trim() != "0") {
            resultado.Mensagem = "Nota não enviada à Senior. Configure Senior__GerParSemGerarParcelas=0 para acatar as parcelas originais.";
            return resultado;
        }

        var parameters = MontarRequisicao(nota, tipCal);
        var url = _configuration["Senior:Url"]
            ?? throw new InvalidOperationException("URL Senior não configurada.");

        var usuario = _configuration["Senior:Usuario"]
            ?? throw new InvalidOperationException("Usuário Senior não configurado.");

        var senha = _configuration["Senior:Senha"]
            ?? throw new InvalidOperationException("Senha Senior não configurada.");

        var binding = new BasicHttpBinding(BasicHttpSecurityMode.None) {
            MaxReceivedMessageSize = 20 * 1024 * 1024,
            SendTimeout = TimeSpan.FromMinutes(3),
            ReceiveTimeout = TimeSpan.FromMinutes(3),
            OpenTimeout = TimeSpan.FromSeconds(30)
        };

        var endpoint = new EndpointAddress(url);
        Console.WriteLine("CREDENCIAIS:" + usuario + " " + senha);
        var client =
            new sapiens_Synccom_senior_g5_co_mcm_cpr_notafiscalClient(
                binding, endpoint);

        try {
            cancellationToken.ThrowIfCancellationRequested();

            var resposta = await client.GravarNotasFiscaisEntradaAsync(
                usuario,
                senha,
                0,
                parameters);

            var mensagens = new[] { resposta?.erroExecucao, resposta?.mensagemRetorno }
                .Concat(resposta?.retornosNotasEntrada?.Select(r => r?.retorno) ?? [])
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct();
            resultado.Sucesso = resposta != null && AvaliarSucesso(resposta, nota);
            resultado.ConfirmacaoPendente = !resultado.Sucesso &&
                (resposta?.tipoRetornoSpecified != true || resposta.tipoRetorno != 2);
            resultado.Mensagem = string.Join(" | ", mensagens.DefaultIfEmpty("Retorno da Senior sem mensagem."));
        }
        catch (Exception ex) when (ex is CommunicationException or TimeoutException) {
            client.Abort();
            _logger.LogWarning(ex, "Gravação da nota {NumNfc} sem retorno conclusivo da Senior", nota.NumNfc);
            resultado.ConfirmacaoPendente = true;
            resultado.Mensagem = "A comunicação com a Senior foi interrompida.";
        }
        finally {
            try {
                if (client.State == CommunicationState.Faulted)
                    client.Abort();
                else
                    await client.CloseAsync();
            }
            catch {
                client.Abort();
            }
        }
        return resultado;
    }

    private static notafiscalGravarNotasFiscaisEntradaIn
    MontarRequisicao(NotaCompletaDto nota, int tipCal) {
        // Montagem da capa da nota
        var dadosGerais =
            new notafiscalGravarNotasFiscaisEntradaInDadosGerais {
                codEmp = nota.CodEmp,
                codEmpSpecified = true,

                codFil = nota.CodFil,
                codFilSpecified = true,

                codFor = nota.CodFor,
                codForSpecified = true,

                numNfc = nota.NumNfc,
                numNfcSpecified = true,

                codSnf = nota.CodSnf,
                tipNfe = nota.TipNfe,
                tipNfeSpecified = true,

                datEnt = FormatarData(nota.DatEnt),
                datEmi = FormatarData(nota.DatEmi),
                tnsPro = nota.TnsPro,

                // Envia TODOS os produtos da nota
                produtos = nota.Produtos
                    .Select(MapearProduto)
                    .ToArray(),
                parcelas = nota.Parcelas.Select(MapearParcela).ToArray()
            };

        // Montagem da requisição SOAP
        var requisicao = new notafiscalGravarNotasFiscaisEntradaIn {
            tipoProcessamento = 2,
            tipoProcessamentoSpecified = true,

            tipCal = tipCal,
            tipCalSpecified = true,

            gerPar = 0,
            gerParSpecified = true,

            fecNot = "2",

            dadosGerais = new[]
            {
            dadosGerais
        }
        };
        return requisicao;
    }

    private static notafiscalGravarNotasFiscaisEntradaInDadosGeraisParcelas
    MapearParcela(ParcelaNotaDto parcela, int indice) {
        // A porta recebe as parcelas em sequência e não tem um campo CodPar.
        if (parcela.CodPar != indice + 1)
            throw new ArgumentException($"A sequência da parcela {parcela.CodPar} não pode ser preservada pelo SOAP.");

        if (parcela.VlrInt.GetValueOrDefault() != 0 || !string.IsNullOrWhiteSpace(parcela.DocIdeFav) ||
            !string.IsNullOrWhiteSpace(parcela.IdeFav))
            throw new ArgumentException($"A parcela {parcela.CodPar} contém dados que esta porta SOAP não consegue reenviar.");

        return new notafiscalGravarNotasFiscaisEntradaInDadosGeraisParcelas {
            numTit = parcela.NumTit,
            codTpt = parcela.CodTpt,
            codCrp = parcela.CodCrp,
            codNtg = parcela.CodNtg?.ToString(CultureInfo.InvariantCulture),
            vctPar = FormatarData(parcela.VctPar),
            vlrPar = FormatarDecimal(parcela.VlrPar, 2, nameof(parcela.VlrPar)),
            perDdp = FormatarDecimalOpcional(parcela.PerDdp, nameof(parcela.PerDdp)),
            qtdDdd = parcela.QtdDdd?.ToString(CultureInfo.InvariantCulture),
            codFcr = parcela.CodFcr,
            datFcr = FormatarData(parcela.DatFcr),
            codPor = parcela.CodPor,
            codCrt = parcela.CodCrt,
            obsPar = parcela.ObsPar,
            vlrDsc = FormatarDecimalOpcional(parcela.VlrDsc, nameof(parcela.VlrDsc)),
            jrsNeg = FormatarDecimalOpcional(parcela.JrsNeg, nameof(parcela.JrsNeg)),
            mulNeg = FormatarDecimalOpcional(parcela.MulNeg, nameof(parcela.MulNeg)),
            outNeg = FormatarDecimalOpcional(parcela.OutNeg, nameof(parcela.OutNeg)),
            datNeg = FormatarData(parcela.DatNeg),
            codBan = parcela.CodBan,
            codAge = parcela.CodAge,
            ccbFor = parcela.CcbFor,
            codFav = parcela.CodFav?.ToString(CultureInfo.InvariantCulture),
            codFpg = parcela.CodFpg,
            codFpgSpecified = parcela.CodFpg.HasValue,
            numCpr = parcela.NumCpr,
            codRep = parcela.CodRep,
            codRepSpecified = parcela.CodRep.HasValue,
            numCoo = parcela.NumCoo,
            numCooSpecified = parcela.NumCoo.HasValue,
            croEcf = parcela.CroEcf,
            croEcfSpecified = parcela.CroEcf.HasValue,
            codEqu = parcela.CodEqu,
            codEquSpecified = parcela.CodEqu.HasValue,
            tipTcc = parcela.TipTcc,
            tipTccSpecified = parcela.TipTcc.HasValue,
            indPag = parcela.IndPag
        };
    }

    private static string? FormatarData(DateTime? valor) =>
        valor?.ToString("dd'/'MM'/'yyyy", CultureInfo.InvariantCulture);

    private static string? FormatarDecimalOpcional(decimal? valor, string campo) =>
        valor.HasValue ? FormatarDecimal(valor.Value, 2, campo) : null;

    private static bool AvaliarSucesso(
        notafiscalGravarNotasFiscaisEntradaOut resposta, NotaCompletaDto nota) {
        if (!resposta.tipoRetornoSpecified || resposta.tipoRetorno != 1 ||
            !string.IsNullOrWhiteSpace(resposta.erroExecucao) ||
            resposta.retornosNotasEntrada is not { Length: 1 })
            return false;

        var retorno = resposta.retornosNotasEntrada[0];
        var mensagemIndividual = retorno?.retorno?.Trim();
        return retorno != null &&
            retorno.codEmp == nota.CodEmp && retorno.codFil == nota.CodFil &&
            int.TryParse(retorno.codFor, out var codFor) && codFor == nota.CodFor &&
            int.TryParse(retorno.numNfc, out var numNfc) && numNfc == nota.NumNfc &&
            string.Equals(retorno.codSnf?.Trim(), nota.CodSnf.Trim(), StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrEmpty(mensagemIndividual) ||
                string.Equals(mensagemIndividual, "OK", StringComparison.OrdinalIgnoreCase));
    }

    private static notafiscalGravarNotasFiscaisEntradaInDadosGeraisProdutos
    MapearProduto(ProdutoNotaDto produto) {
        return new notafiscalGravarNotasFiscaisEntradaInDadosGeraisProdutos {
            seqIpc = produto.SeqIpc,
            seqIpcSpecified = true,

            codPro = produto.CodPro,
            codDer = produto.CodDer,
            tnsPro = produto.TnsPro,

            qtdRec = FormatarDecimal(produto.QtdRec, 5, nameof(produto.QtdRec)),
            preUni = FormatarDecimal(produto.PreUni, 10, nameof(produto.PreUni)),

            vlrLiq = FormatarDecimal(produto.VlrLiq, 2, nameof(produto.VlrLiq)),
            vlrBru = FormatarDecimal(produto.VlrBru, 2, nameof(produto.VlrBru)),

            vlrBsi = FormatarDecimal(produto.VlrBsi, 2, nameof(produto.VlrBsi)),
            vlrBic = FormatarDecimal(produto.VlrBic, 2, nameof(produto.VlrBic)),

            camposUsuario = produto.USU_CodMot == null ? null : new[]
            {
            new notafiscalGravarNotasFiscaisEntradaInDadosGeraisProdutosCamposUsuario
            {
                cmpUsu = "USU_CodMot",
                vlrUsu = produto.USU_CodMot
            }
        }
        };
    }

    private static string FormatarDecimal(decimal valor, int casas, string campo) {
        if (decimal.Round(valor, casas) != valor)
            throw new ArgumentException($"O campo {campo} tem mais de {casas} casas decimais.");

        return valor.ToString($"F{casas}", CulturaSenior);
    }
}
