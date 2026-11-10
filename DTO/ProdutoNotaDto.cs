namespace SistemaEntradaNotaFiscal.DTOs;
public class ProdutoNotaDto {
    public short SeqIpc { get; set; }

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
