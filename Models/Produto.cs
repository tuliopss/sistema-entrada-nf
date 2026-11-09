using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SistemaEntradaNotaFiscal.Models {
    [Table("E075PRO")]
    public class Produto {
        [Key]
        [Column("CodPro")]
        public string CodPro { get; set; }

        [Column("DesPro")]
        public string DesPro { get; set; }
        

        [Column("CodFam")]
        public string CodFam { get; set; }


        [ForeignKey(nameof(CodFam))]
        public Familia? Familia { get; set; }

        public Produto() { }

        public Produto(string codPro, string desPro, Familia familia) {
            CodPro = codPro;
            DesPro = desPro;
            Familia = familia;
        }
    }
}
