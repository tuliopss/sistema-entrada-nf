using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SistemaEntradaNotaFiscal.Models {
    [Table("E073TRA")]
    public class Transportadora {
        [Key]
        [Column("CodTra")]
        public int CodTra { get; set; }

        [Column("NomTra")]
        public string NomTra { get; set; }

        public Transportadora(int codTra, string nomTra) {
            CodTra = codTra;
            NomTra = nomTra;
        }

        public Transportadora() {
        }
    }
}
