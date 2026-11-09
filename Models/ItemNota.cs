using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEntradaNotaFiscal.Models {
    [Table("E440IPC")]
    public class ItemNota {

      [Key] 
      [Column("NumNfc")] public int NumNfc { get; set; }
      [Column("CodEmp")] public short CodEmp { get; set; }
      [Column("CodFil")] public int CodFil { get; set; }
      [Column("CodFor")] public int CodFor { get; set; }
      [Column("CodSnf"), MaxLength(3)] public string CodSnf { get; set; } = string.Empty;
      [Column("SeqIpc")] public short SeqIpc { get; set; }

      [ForeignKey("CodPro")]
      public Produto? produto { get; set; }

      [Column("CodPro")] public string CodPro { get; set; } = string.Empty;
      [Column("QtdRec")] public decimal QtdRec { get; set; }
      [Column("PreUni")] public decimal PreUni { get; set; }
      [Column("VlrLiq")] public decimal VlrLiq { get; set; }
      [Column("VlrBru")] public decimal VlrBru { get; set; }    
      [Column("TnsPro")] public string TnsPro { get; set; } = string.Empty;
      [Column("USU_CodMot")] public string? USU_CodMot { get; set; } = string.Empty;
      [Column("VlrBsi")] public decimal VlrBsi { get; set; }
      [Column("VlrBic")] public decimal VlrBic { get; set; }

        public NotasEntrada Nota { get; set; } = null!;   // i.Nota

    }
}
