namespace SistemaEntradaNotaFiscal.DTOs;

public sealed record SugestaoVinculoVendaDto(
    short SeqIpc, string CodPro, string Unidade, decimal QuantidadeSolicitada,
    decimal QuantidadeFaltante, IReadOnlyList<OrigemVendaSugeridaDto> Vendas);

public sealed record OrigemVendaSugeridaDto(
    short CodEmp, int CodFil, string CodSnf, int NumNfv, short SeqIpv, decimal Quantidade);
