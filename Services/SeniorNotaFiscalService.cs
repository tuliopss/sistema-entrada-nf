using SistemaEntradaNotaFiscal.DTOs;
using System.Globalization;
using System.ServiceModel;
using WS_NF_HML;

namespace SistemaEntradaNotaFiscal.Services;

public class SeniorNotaFiscalService : ISeniorNotaFiscalService {
    private readonly IConfiguration _configuration;

    public SeniorNotaFiscalService(IConfiguration configuration) {
        _configuration = configuration;
    }

    public async Task<ResultadoProcessamentoDto> GravarNotaAsync(
        NotaCompletaDto nota,
        CancellationToken cancellationToken) {
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

        var client =
            new sapiens_Synccom_senior_g5_co_mcm_cpr_notafiscalClient(
                binding, endpoint);

        try {
            // O mapper deve produzir a nota fiscal completa.
            var parameters = MontarRequisicao(nota);
            Console.WriteLine("PARAMETROS" + parameters);
            cancellationToken.ThrowIfCancellationRequested();

            var resposta = await client.GravarNotasFiscaisEntradaAsync(
                usuario,
                senha,
                0,
                parameters);

            var erros = resposta.retornosNotasEntrada?
                .Where(r => !string.IsNullOrWhiteSpace(r.retorno))
                .Select(r => r.retorno)
                .ToArray() ?? Array.Empty<string>();

            var mensagem = resposta.mensagemRetorno
                ?? resposta.erroExecucao
                ?? string.Join("; ", erros);

            // O sucesso só deve ser definido após validar
            // a semântica do tipoRetorno e do retorno por nota.
            var sucesso = AvaliarSucesso(resposta);

            return new ResultadoProcessamentoDto {
                CodEmp = nota.CodEmp,
                CodFil = nota.CodFil,
                CodFor = nota.CodFor,
                NumNfc = nota.NumNfc,
                CodSnf = nota.CodSnf,
                Sucesso = sucesso,
                Mensagem = mensagem
            };
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
    }

    private static notafiscalGravarNotasFiscaisEntradaIn
    MontarRequisicao(NotaCompletaDto nota) {
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

                datEnt = nota.DatEnt.ToString("dd/MM/yyyy"),
                datEmi = nota.DatEmi.ToString("dd/MM/yyyy"),
                tnsPro = nota.TnsPro,

                // Envia TODOS os produtos da nota
                produtos = nota.Produtos
                    .Select(MapearProduto)
                    .ToArray()
            };

        // Montagem da requisição SOAP
        var requisicao = new notafiscalGravarNotasFiscaisEntradaIn {
            tipoProcessamento = 2,
            tipoProcessamentoSpecified = true,

            tipCal = 2,
            tipCalSpecified = true,

            fecNot = "2",

            dadosGerais = new[]
            {
            dadosGerais
        }
        };
        return requisicao;
    }

    private static bool AvaliarSucesso(
        notafiscalGravarNotasFiscaisEntradaOut resposta) {
        // Não considerar sucesso apenas porque o SOAP respondeu HTTP 200.
        // Validar tipoRetorno, erroExecucao e retornosNotasEntrada.
        throw new NotSupportedException(
            "Validação do retorno Senior ainda não implementada.");
    }

    private static notafiscalGravarNotasFiscaisEntradaInDadosGeraisProdutos
    MapearProduto(ProdutoNotaDto produto) {
        var cultura = CultureInfo.GetCultureInfo("pt-BR");

        return new notafiscalGravarNotasFiscaisEntradaInDadosGeraisProdutos {
            seqIpc = produto.SeqIpc,
            seqIpcSpecified = true,

            codPro = produto.CodPro,
            tnsPro = produto.TnsPro,

            qtdRec = produto.QtdRec.ToString(cultura),
            preUni = produto.PreUni.ToString(cultura),

            vlrLiq = produto.VlrLiq.ToString(cultura),
            vlrBru = produto.VlrBru.ToString(cultura),

            vlrBsi = produto.VlrBsi.ToString(cultura),
            vlrBic = produto.VlrBic.ToString(cultura),

            camposUsuario = new[]
            {
            new notafiscalGravarNotasFiscaisEntradaInDadosGeraisProdutosCamposUsuario
            {
                cmpUsu = "USU_CodMot",
                vlrUsu = produto.USU_CodMot ?? ""
            }
        }
        };
    }

}
