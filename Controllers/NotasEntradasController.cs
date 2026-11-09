using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaEntradaNotaFiscal.DTO;
using SistemaEntradaNotaFiscal.Models;
using SistemaEntradaNotaFiscal.Services;

namespace SistemaEntradaNotaFiscal.Controllers {
    [Authorize]
    public class NotasEntradasController : Controller {
        private readonly NotasEntradasService _notasEntradasService;
        private readonly ProdutoService _produtoService;
        private readonly ProcessamentoNotaService _processamentoNotaService;
        private const int pageSize = 20;

        public NotasEntradasController(NotasEntradasService notasEntradasService, ProdutoService produtoService, ProcessamentoNotaService processamentoNotaService) {
            _notasEntradasService = notasEntradasService;
            _produtoService = produtoService;
            _processamentoNotaService = processamentoNotaService;
        }

        public IActionResult Index() {
            return View();
        }

        public async Task<IActionResult> ListarCapasNotas(DateTime? minDate, DateTime? maxDate, int? codFor, int page = 1) {
            var hoje = DateTime.Today;
            minDate ??= hoje.AddDays(-15);
            maxDate ??= hoje;

            if (minDate > maxDate) {
                ModelState.AddModelError(string.Empty, "A data inicial não pode ser maior que a final.");
                return View(new List<NotasEntrada>());
            }

            ViewData["minDate"] = minDate.Value.ToString("yyyy-MM-dd");
            ViewData["maxDate"] = maxDate.Value.ToString("yyyy-MM-dd");
            ViewData["codFor"] = codFor;

            var result = await _notasEntradasService.ListarCapasNotas(minDate, maxDate, codFor, page, pageSize);
            return View(result);
        }

        public async Task<IActionResult> ListarItensNotas(int numNfc, short codEmp, int codFil, int codFor, string codSnf) {

            var result = await _notasEntradasService.ListarItens(numNfc, codEmp, codFil, codFor, codSnf);

            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> BuscarProduto(string codPro) {

            var produto = await _produtoService.GetProduto(codPro);

            if (produto == null)
                return NotFound();

            return Json(new {
                codPro = produto.CodPro,
                desPro = produto.DesPro
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessarNotas(
            [FromBody] List<SalvarNotaRequest> notas,
            CancellationToken cancellationToken) {
                    if (notas == null || notas.Count == 0)
                        return BadRequest("Nenhuma nota enviada.");

                    if (notas.Count > 100)
                        return BadRequest("O lote excede o limite de 100 notas.");

                    var resultados = await _processamentoNotaService
                        .ProcessarLoteAsync(notas, cancellationToken);

                    return Ok(resultados);
                }
    }
}