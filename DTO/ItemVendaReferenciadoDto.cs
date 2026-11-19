namespace SistemaEntradaNotaFiscal.DTO;

public sealed record ItemVendaReferenciadoDto(
    short SeqIpc,
    short CodEmp,
    int CodFil,
    string CodSnf,
    int NumNfv,
    short SeqIpv,
    DateTime DatEmi,
    string? CodPro,
    string? CodDer,
    decimal? QtdFat,
    decimal? PreUni,
    decimal? VlrBru,
    decimal? VlrLiq);
