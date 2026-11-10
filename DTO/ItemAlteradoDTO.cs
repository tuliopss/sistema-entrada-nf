namespace SistemaEntradaNotaFiscal.DTO {
        public class ItemAlteradoDto {
            public short SeqIpc { get; set; }      // identifica o item
            public string? CodPro { get; set; }   // null = não alterado, "" = limpar

            public decimal? QtdRec { get; set; }
            public string? TnsPro { get; set; }
            public decimal? VlrBsi { get; set; }
            public decimal? VlrBic { get; set; }
            public string? USU_CodMot { get; set; }   // null = não alterado, "" = limpar
        }
    
}
