using Microsoft.EntityFrameworkCore;
using SistemaEntradaNotaFiscal.Data;
using SistemaEntradaNotaFiscal.Models;

namespace SistemaEntradaNotaFiscal.Services {
    public class ProdutoService {
        private readonly HmlContext _context;

        public ProdutoService(HmlContext context) { 
            _context = context;
        }

        public async Task<Produto> GetProduto (string codPro) {
            return await _context.Produto.FirstOrDefaultAsync(p => p.CodPro == codPro);
        }     
      }   
}


