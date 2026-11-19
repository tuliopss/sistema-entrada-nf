using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SistemaEntradaNotaFiscal.Models {
    [Table("E085CLI")]
    public class Cliente {
        [Key]
        [Column("CodCli")]
        public int CodCli { get; set; }

        [Column("NomCli")]
        public string NomCli { get; set; }

        [Column("ApeCli")]
        public string ApeCli { get; set; }

        [Column("DocIde")]
        public string? DocIde { get; set; }

        [Column("CodFor")]
        public int? CodFor { get; set; }
        [JsonIgnore]
        public Fornecedor? Fornecedor { get; set; }

        public Cliente() { }

        public Cliente(int codFor, string nomCli, string apeCli, string docIde, int codCli) {
            CodFor = codFor;
            NomCli = nomCli;
            ApeCli = apeCli;
            DocIde = docIde;
            CodCli = codCli;
        }
    }
}
