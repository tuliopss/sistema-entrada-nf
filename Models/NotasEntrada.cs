using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEntradaNotaFiscal.Models {
    [Table("E440NFC")]
    public class NotasEntrada {
        [Key]
        [Column("NumNfc")] public int NumNfc { get; set; }
        [Column("CodEmp")] public short CodEmp { get; set; }
        [Column("CodFil")] public int CodFil { get; set; }
        [Column("CodFor")] 
        public int CodFor { get; set; }
        public Fornecedor Fornecedor { get; set; }

        [Column("CodSnf"), MaxLength(3)] public string CodSnf { get; set; } = string.Empty;
        [Column("DatEnt")] public DateTime DatEnt { get; set; }
        [Column("DatEmi")] public DateTime DatEmi { get; set; }
        [Column("TnsPro"), MaxLength(5)] public string TnsPro { get; set; } = string.Empty;
        [Column("TipNfe")] public short TipNfe { get; set; }       
        [Column("VlrLiq")] public decimal VlrLiq { get; set; }
        [Column("SitNfc")] public string SitNfc { get; set; } = string.Empty;

        public ICollection<ItemNota> ItemNotas { get; set; } = new List<ItemNota>();

   
    }
}
