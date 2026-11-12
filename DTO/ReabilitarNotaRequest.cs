namespace SistemaEntradaNotaFiscal.DTO {
    public class ReabilitarNotaRequest {
        public short CodEmp { get; set; }
        public int CodFil { get; set; }
        public int CodFor { get; set; }
        public int NumNfc { get; set; }
        public string CodSnf { get; set; } = string.Empty;

    }
}
