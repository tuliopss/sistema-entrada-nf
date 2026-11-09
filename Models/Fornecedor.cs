using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SistemaEntradaNotaFiscal.Models {
    [Table("E095FOR")]
    public class Fornecedor {
        [Key]
        [Column("CodFor")]
        public int CodFor { get; set; }

        [Column("NomFor")]
        public string NomFor { get; set; }

        [Column("ApeFor")]
        public string ApeFor { get; set; }

        [Column("DocIde")]
        public string DocIde { get; set; }

        public Fornecedor() { }

        public Fornecedor(int codFor, string nomFor, string apeFor, string docIde) {
            CodFor = codFor;
            NomFor = nomFor;
            ApeFor = apeFor;
            DocIde = docIde;
        }
    }
}
