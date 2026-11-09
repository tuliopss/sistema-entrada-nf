using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEntradaNotaFiscal.Models {
    [Table("E012FAM")]
    public class Familia {
        [Key]
        [Column("CodFam")]
        public string CodFam { get; set; }

        [Column("DesFam")]
        public string DesFam { get; set; }

        public Familia() { }

        public Familia(string codFam, string desFam) {
            CodFam = codFam;
            DesFam = desFam;
        }
    }
}
