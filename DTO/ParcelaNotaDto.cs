namespace SistemaEntradaNotaFiscal.DTOs;

public sealed record ParcelaNotaDto {
    public short CodPar { get; set; }
    public string NumTit { get; set; } = string.Empty;
    public string? CodTpt { get; set; }
    public string? CodCrp { get; set; }
    public short? CodNtg { get; set; }
    public DateTime VctPar { get; set; }
    public decimal VlrPar { get; set; }
    public decimal? PerDdp { get; set; }
    public short? QtdDdd { get; set; }
    public string? CodFcr { get; set; }
    public DateTime? DatFcr { get; set; }
    public string CodPor { get; set; } = string.Empty;
    public string CodCrt { get; set; } = string.Empty;
    public string? ObsPar { get; set; }
    public decimal? VlrDsc { get; set; }
    public decimal? JrsNeg { get; set; }
    public decimal? MulNeg { get; set; }
    public decimal? OutNeg { get; set; }
    public DateTime? DatNeg { get; set; }
    public string? CodBan { get; set; }
    public string? CodAge { get; set; }
    public string? CcbFor { get; set; }
    public long? CodFav { get; set; }
    public short? CodFpg { get; set; }
    public string? NumCpr { get; set; }
    public int? CodRep { get; set; }
    public int? NumCoo { get; set; }
    public int? CroEcf { get; set; }
    public short? CodEqu { get; set; }
    public short? TipTcc { get; set; }
    public string? IndPag { get; set; }

    public decimal? VlrInt { get; set; }
    public string? DocIdeFav { get; set; }
    public string? IdeFav { get; set; }
}
