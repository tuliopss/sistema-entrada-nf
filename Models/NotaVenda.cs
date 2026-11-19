namespace SistemaEntradaNotaFiscal.Models;

public class NotaVenda {
    public short CodEmp { get; set; }
    public int CodFil { get; set; }
    public string CodSnf { get; set; } = string.Empty;
    public int NumNfv { get; set; }
    public DateTime DatEmi { get; set; }
    public int CodCli { get; set; }
    public string SitNfv { get; set; } = string.Empty;
    public decimal? VlrLiq { get; set; }

    public ICollection<ItemNotaVenda> Itens { get; set; } = new List<ItemNotaVenda>();
}
