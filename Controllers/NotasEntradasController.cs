using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaEntradaNotaFiscal.DTO;
using SistemaEntradaNotaFiscal.Models;
using SistemaEntradaNotaFiscal.Services;

namespace SistemaEntradaNotaFiscal.Controllers {
    [Authorize]
    public class NotasEntradasController : Controller {
        private readonly NotasEntradasService _notasEntradasService;
        private readonly NotaVendaService _notaVendaService;
        private readonly ProdutoService _produtoService;
        private readonly TransportadoraService _transportadoraService;
        private readonly ProcessamentoNotaService _processamentoNotaService;
        private readonly ReabilitacaoNotaService _reabilitacaoNotaService;
        private readonly FechamentoNotaService _fechamentoNotaService;
        private const int pageSize = 20;

        public NotasEntradasController(NotasEntradasService notasEntradasService, NotaVendaService notaVendaService, ProdutoService produtoService,
            ProcessamentoNotaService processamentoNotaService, ReabilitacaoNotaService reabilitacaoNotaService,
            FechamentoNotaService fechamentoNotaService, TransportadoraService transportadoraService) {
            _notasEntradasService = notasEntradasService;
            _notaVendaService = notaVendaService;
            _produtoService = produtoService;
            _processamentoNotaService = processamentoNotaService;
            _reabilitacaoNotaService = reabilitacaoNotaService;
            _fechamentoNotaService = fechamentoNotaService;
            _transportadoraService = transportadoraService;
        }

        public IActionResult Index() {
            return View();
        }

        public async Task<IActionResult> ListarCapasNotas(DateTime? minDate, DateTime? maxDate, int? codFor, string? sitNfc, int? numNfc, int page = 1) {
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
            ViewData["sitNfc"] = sitNfc;
            ViewData["numNfc"] = numNfc;

            var result = await _notasEntradasService.ListarCapasNotas(minDate, maxDate, codFor, sitNfc, numNfc, page, pageSize);
            return View(result);
        }

        public async Task<IActionResult> ListarItensNotas(int numNfc, short codEmp, int codFil, int codFor, string codSnf, CancellationToken cancellationToken) {

            var result = await _notasEntradasService.ListarItens(numNfc, codEmp, codFil, codFor, codSnf, cancellationToken);

            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> BuscarTransportadora(int codTra, CancellationToken cancellationToken) {
            if (codTra <= 0) return BadRequest("Informe um código de transportadora válido.");
            var transportadora = await _transportadoraService.BuscarAsync(codTra, cancellationToken);
            if (transportadora == null) return NotFound("Transportadora não encontrada.");
            return Json(new { transportadora.CodTra, transportadora.NomTra });
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
            if (!ModelState.IsValid)
                return BadRequest("O lote contém campos com valores ou formatos inválidos.");

            if (notas == null || notas.Count == 0)
                return BadRequest("Nenhuma nota enviada.");

            if (notas.Count > 100)
                return BadRequest("O lote excede o limite de 100 notas.");

            if (notas.Any(n => n == null || n.Itens?.Any(i => i == null) == true))
                return BadRequest("O lote contém notas ou itens inválidos.");

            if (notas.GroupBy(n => (n.CodEmp, n.CodFil, n.CodFor, n.NumNfc, n.CodSnf.Trim().ToUpperInvariant()))
                .Any(g => g.Count() > 1))
                return BadRequest("O lote contém notas duplicadas.");

            Response.Headers.CacheControl = "no-store";
            var resultados = await _processamentoNotaService
                .ProcessarLoteAsync(notas, cancellationToken);

            return Ok(resultados);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReabilitarNotas(
            [FromBody] List<ChaveNotaRequest> notas, CancellationToken cancellationToken) {
            var erro = ValidarChaves(notas);
            if (erro != null) return erro;

            Response.Headers.CacheControl = "no-store";
            return Ok(await _reabilitacaoNotaService.ReabilitarLoteAsync(notas, cancellationToken));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FecharNotas(
            [FromBody] List<ChaveNotaRequest> notas, CancellationToken cancellationToken) {
            var erro = ValidarChaves(notas);
            if (erro != null) return erro;

            Response.Headers.CacheControl = "no-store";
            return Ok(await _fechamentoNotaService.FecharLoteAsync(notas, cancellationToken));
        }

        private IActionResult? ValidarChaves(List<ChaveNotaRequest> notas) {
            if (!ModelState.IsValid || notas == null || notas.Count == 0)
                return BadRequest("Informe as notas selecionadas.");

            if (notas.Count > 100)
                return BadRequest("O lote excede o limite de 100 notas.");

            if (notas.Any(n => n == null || n.CodEmp <= 0 || n.CodFil <= 0 || n.CodFor <= 0 ||
                n.NumNfc <= 0 || string.IsNullOrWhiteSpace(n.CodSnf) || n.CodSnf.Trim().Length > 3))
                return BadRequest("O lote contém chaves de notas inválidas.");

            if (notas.GroupBy(n => (n.CodEmp, n.CodFil, n.CodFor, n.NumNfc, n.CodSnf.Trim().ToUpperInvariant()))
                .Any(g => g.Count() > 1))
                return BadRequest("O lote contém notas duplicadas.");

            return null;
        }


        [HttpGet]
        public async Task<IActionResult> NotaVenda(
            [FromQuery] ChaveNotaRequest nota, CancellationToken cancellationToken) {
            var erro = ValidarChaves([nota]);
            if (erro != null) return erro;

            var itens = await _notaVendaService.RecuperarNotasVendaEntrada(nota, cancellationToken);
            return Json(itens);
        }

        [HttpGet]
        public async Task<IActionResult> UltimasVendas(
            [FromQuery] ChaveNotaRequest nota, CancellationToken cancellationToken) {
            //var erro = ValidarChaves([nota]);
            //if (erro != null) return erro;

            var notas = await _notaVendaService.VendasUltimos6mesesCliente(nota, cancellationToken);
            Console.WriteLine("CONTROLLER: " + nota);
            if (notas == null)
                return NotFound("Nota de entrada não encontrada.");

            return Json(notas);
        }
    }
}
