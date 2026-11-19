namespace SistemaEntradaNotaFiscal.DTOs;
public class ProdutoNotaDto {
    public short SeqIpc { get; set; }
    public short? EmpNfv { get; set; }
    public int? FilNfv { get; set; }
    public string? SnfNfv { get; set; }
    public int? NumNfv { get; set; }
    public short? SeqIpv { get; set; }

    public string CodPro { get; set; } = "";
    public string? CodDer { get; set; }
    public string TnsPro { get; set; } = "";

    public decimal QtdRec { get; set; }
    public decimal PreUni { get; set; }
    public decimal VlrLiq { get; set; }
    public decimal VlrBru { get; set; }

    public decimal VlrBsi { get; set; }
    public decimal VlrBic { get; set; }

    public string? USU_CodMot { get; set; }
}
