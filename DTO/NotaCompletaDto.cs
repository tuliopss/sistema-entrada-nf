namespace SistemaEntradaNotaFiscal.DTOs;

public class NotaCompletaDto {
    public int CodEmp { get; set; }
    public int CodFil { get; set; }
    public int CodFor { get; set; }
    public int NumNfc { get; set; }
    public string CodSnf { get; set; } = "";

    public DateTime DatEnt { get; set; }
    public DateTime DatEmi { get; set; }
    public string? TnsPro { get; set; }

    public List<ProdutoNotaDto> Produtos { get; set; } = new();
}
