// Execute com: node tests/sugestoes-modal.test.cjs (sem acesso ao ERP).
const { readFileSync } = require('node:fs');
const { resolve } = require('node:path');
const vm = require('node:vm');
const assert = require('node:assert/strict');

const view = readFileSync(resolve(__dirname, '../Views/NotasEntradas/ListarCapasNotas.cshtml'), 'utf8');
const script = view.slice(view.indexOf('<script>') + 8, view.lastIndexOf('</script>'));
new vm.Script(script); // Também verifica a sintaxe do script completo da View.
const helpers = script.slice(script.indexOf('const camposItem ='), script.indexOf('// ---------- Grid principal'));
const render = script.slice(script.indexOf('function renderizarItens()'), script.indexOf("tbodyItens.addEventListener('input'"));
const source = { seqIpc: 1, codPro: '010.01', qtdRec: 12, uniNfc: 'LT', numNfv: null,
    tnsPro: '1201D', usU_CodMot: '', vlrBsi: 0, vlrBic: 0 };
const origem = { codEmp: 1, codFil: 1, codSnf: 'NFE', numNfv: 1723362, seqIpv: 1, quantidade: 12 };
let fetches = 0;
const context = vm.createContext({ assert, source, origem, URLSearchParams, DOMException,
    fetch: async () => {
        fetches++;
        return { ok: true, headers: { get: () => 'application/json' }, json: async () => [source] };
    }
});
vm.runInContext(`
    const estado = {};
    const URL_ITENS = '/itens';
    const tbodyItens = { innerHTML: '' };
    let chaveAberta = 'nota', trabalho = [];
    const chaveDaLinha = () => 'nota';
    let marcada = false;
    const marcarLinha = () => { marcada = true; };
    const tr = { dataset: { num: '9009', emp: '1', fil: '1', for: '65965', snf: 'NFE' },
        querySelector: () => ({ classList: { toggle() {} } }) };
    ${helpers}
    const inputHtml = () => '';
    ${render}
`, context);

(async () => {
    await vm.runInContext(`(async () => {
        const nota = { sugestoesVenda: [{ seqIpc: 1, codPro: '010.01', quantidadeSolicitada: 12,
            quantidadeFaltante: 0, vendas: [origem] }] };
        await aplicarSugestoes(nota, tr);
        assert.equal(marcada, true);
        assert.equal(estado.nota.original[0].vendasSugeridas, undefined);
        assert.equal(coletarItensAlterados(estado.nota)[0].vendasSugeridas[0].numNfv, 1723362);

        // Abrir, salvar e reabrir o modal preserva a sugestão e seu destaque.
        trabalho = clonar(estado.nota.atual);
        renderizarItens();
        assert.match(tbodyItens.innerHTML, /campo-alterado/);
        assert.ok(tbodyItens.innerHTML.includes('1723362/NFE'));
        assert.match(tbodyItens.innerHTML, /Sugestão automática/);
        estado.nota.atual = trabalho;
        trabalho = clonar(estado.nota.atual);
        assert.equal(trabalho[0].vendasSugeridas[0].quantidade, 12);

        // Reconsultar não perde outras alterações do usuário nem recarrega os originais.
        estado.nota.atual[0].tnsPro = '1410D';
        await aplicarSugestoes(nota, tr);
        assert.equal(estado.nota.atual[0].tnsPro, '1410D');
        const payload = coletarItensAlterados(estado.nota)[0];
        assert.equal(payload.tnsPro, '1410D');
        assert.equal(payload.seqIpc, 1);
        assert.equal(payload.vendasSugeridas.length, 1);

        // Não preenche sugestão incompleta ou incompatível com uma quantidade editada.
        delete estado.nota.atual[0].vendasSugeridas;
        nota.sugestoesVenda[0].quantidadeFaltante = 2;
        await aplicarSugestoes(nota, tr);
        assert.equal(estado.nota.atual[0].vendasSugeridas, undefined);
        nota.sugestoesVenda[0].quantidadeFaltante = 0;
        estado.nota.atual[0].qtdRec = 20;
        await aplicarSugestoes(nota, tr);
        assert.equal(estado.nota.atual[0].vendasSugeridas, undefined);

        // Todas as origens do rateio permanecem visíveis, sem escolher uma arbitrariamente.
        estado.nota.atual[0].qtdRec = 12;
        nota.sugestoesVenda[0].vendas = [{ ...origem, quantidade: 6 },
            { ...origem, numNfv: 1722806, quantidade: 6 }];
        await aplicarSugestoes(nota, tr);
        trabalho = clonar(estado.nota.atual);
        renderizarItens();
        assert.match(tbodyItens.innerHTML, /1723362/);
        assert.match(tbodyItens.innerHTML, /1722806/);
        assert.match(tbodyItens.innerHTML, /gravação ainda indisponível/);
        delete estado.nota.atual[0].vendasSugeridas;
        estado.nota.atual[0].tnsPro = source.tnsPro;
        assert.equal(coletarItensAlterados(estado.nota).length, 0);
    })()`, context);
    assert.equal(fetches, 1);
    console.log('OK: estado, payload, destaque, reabertura, remoção, saldo parcial e múltiplas origens.');
})().catch(error => { console.error(error); process.exitCode = 1; });
