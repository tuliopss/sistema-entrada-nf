using System.Text.Json.Serialization;

namespace SistemaEntradaNotaFiscal.Models;

public class ItemNotaVenda {
    public short CodEmp { get; set; }
    public int CodFil { get; set; }
    public string CodSnf { get; set; } = string.Empty;
    public int NumNfv { get; set; }
    public short SeqIpv { get; set; }
    public string? CodPro { get; set; }
    public string? CodDer { get; set; }
    public decimal? QtdFat { get; set; }
    public decimal? QtdDev { get; set; }
    public string UniMed { get; set; } = string.Empty;
    public decimal? PreUni { get; set; }
    public decimal? VlrBru { get; set; }
    public decimal? VlrLiq { get; set; }

    [JsonIgnore]
    public NotaVenda Nota { get; set; } = null!;
}
