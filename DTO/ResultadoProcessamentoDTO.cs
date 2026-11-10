namespace SistemaEntradaNotaFiscal.DTOs;

public class ResultadoProcessamentoDto {
    public int CodEmp { get; set; }
    public int CodFil { get; set; }
    public int CodFor { get; set; }
    public int NumNfc { get; set; }
    public string CodSnf { get; set; } = string.Empty;

    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public bool ConfirmacaoPendente { get; set; }
}
