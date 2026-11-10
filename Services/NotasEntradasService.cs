using Microsoft.EntityFrameworkCore;
using SistemaEntradaNotaFiscal.Data;
using SistemaEntradaNotaFiscal.DTO;
using SistemaEntradaNotaFiscal.Models;
using WS_NF_HML;

namespace SistemaEntradaNotaFiscal.Services {
    public class NotasEntradasService {
        private readonly HmlContext _context;
        private readonly sapiens_Synccom_senior_g5_co_mcm_cpr_notafiscalClient _client;

        public NotasEntradasService(
            HmlContext context) 
            {
            _context = context;
            
            }

        public async Task<PagedResult<NotasEntrada>> ListarCapasNotas(DateTime? DatEmi, DateTime? DateFim, int? codFor, int page = 1, int pageSize = 20) {
            //var query = from obj in _context.NotasEntradas select obj;
            var query = _context.NotasEntradas
                .AsNoTracking()
                .Where(n => n.TipNfe == 2);

            if (DatEmi.HasValue) {
                query = query.Where(x => x.DatEmi >= DatEmi.Value);
            }

            if (DateFim.HasValue) {
                query = query.Where(x => x.DatEmi <= DateFim.Value);
            }

            if (codFor.HasValue) {
                query = query.Where(x => x.CodFor == codFor.Value);
            }

            var total = await query.CountAsync();

            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            page = Math.Clamp(page, 1 , pageSize);

            var items = await query
                        .Where(n => n.TipNfe == 2)
                        .Include(f => f.Fornecedor)
                        .OrderByDescending(n => n.DatEmi)
                        .ThenByDescending(v => v.VlrLiq)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();

            return new PagedResult<NotasEntrada> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };

        }


        public async Task<List<ItemNota>> ListarItens(
            int numNfc, short codEmp, int codFil, int codFor, string codSnf,
            CancellationToken ct, bool incluirProduto = true) {
            var itens = _context.ItemNota
                .AsNoTracking()
                .Where(n => n.NumNfc == numNfc && n.CodEmp == codEmp && n.CodFil == codFil && n.CodFor == codFor && n.CodSnf == codSnf);

            if (incluirProduto)
                itens = itens.Include(i => i.produto);

            return await itens.OrderBy(n => n.SeqIpc).ToListAsync(ct);
        }

        //public async Task<NotasEntrada> Teste(GravarNotaDTO) {
        //    //return await _client.GravarNotasFiscaisEntrada_27Async()
        //}
    }
}
/*
  sapiens_Synccom_senior_g5_co_mcm_cpr_notafiscalClient client
 */
