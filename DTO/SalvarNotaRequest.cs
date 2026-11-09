using SistemaEntradaNotaFiscal.Models;

namespace SistemaEntradaNotaFiscal.DTO {
    public class SalvarNotaRequest {
        // Chave original da nota (vem do <tr>, nunca é editável)
        public int CodEmp { get; set; }
        public int CodFil { get; set; }
        public int CodFor { get; set; }
        public int NumNfc { get; set; }
        public string CodSnf { get; set; } = string.Empty;

        // Capa: null = não foi alterado
        public DateTime? DatEnt { get; set; }
        public DateTime? DatEmi { get; set; }
        public string? TnsPro { get; set; }
       

        // Só os itens que tiveram alteração
        public List<ItemAlteradoDto> Itens { get; set; } = new();
    }

 
}
