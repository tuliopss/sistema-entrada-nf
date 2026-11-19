// Execute com: node tests/transportadora-grid.test.cjs. Não acessa o ERP.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const view = fs.readFileSync(path.resolve(__dirname, '../Views/NotasEntradas/ListarCapasNotas.cshtml'), 'utf8');
const lookup = view.slice(view.indexOf('const consultasTransportadora ='), view.indexOf('function confirmarDescarte()'));
const collect = view.slice(view.indexOf('function coletarNotasAlteradas()'), view.indexOf("const painel = document.getElementById('resultadoProcessamento')"));
const requests = [];
const context = vm.createContext({ assert, AbortController, URLSearchParams,
    fetch: (url, options) => new Promise(resolve => requests.push({ url, options, resolve })) });
vm.runInContext(`
    const classes = () => {
        const values = new Set();
        return { add: v => values.add(v), remove: v => values.delete(v),
            contains: v => values.has(v), toggle(v, on) { on ? values.add(v) : values.delete(v); } };
    };
    const nome = { dataset: { original: 'Transportadora Original' }, textContent: 'Transportadora Original', classList: classes() };
    const input = { value: '1', dataset: { original: '1', campo: 'CodTra' },
        validity: '', setCustomValidity(message) { this.validity = message; },
        reportValidity() { return !this.validity; } };
    const linha = { dataset: { emp: '1', fil: '1', for: '65965', num: '9009', snf: 'NFE' },
        querySelector: () => nome, querySelectorAll: () => [input] };
    input.closest = () => linha;
    const marcarLinha = () => {};
    const estado = {};
    const chaveDaLinha = () => 'nota';
    const coletarItensAlterados = () => [];
    const document = { querySelectorAll: () => [linha] };
    const URL_TRANSPORTADORA = '/NotasEntradas/BuscarTransportadora';
    ${lookup}
    ${collect}
`, context);
const reply = (request, body, status = 200) => request.resolve({ status, ok: status === 200, json: async () => body });
(async () => {
    vm.runInContext("input.value = '2'; globalThis.primeira = buscarNomeTransportadora(input);", context);
    assert.match(requests[0].url, /codTra=2/);
    vm.runInContext("assert.throws(() => coletarNotasAlteradas()); input.value = '3'; globalThis.segunda = buscarNomeTransportadora(input);", context);
    assert.equal(requests[0].options.signal.aborted, true);
    reply(requests[1], { codTra: 3, nomTra: 'Transportadora Nova' });
    await context.segunda;
    reply(requests[0], { codTra: 2, nomTra: 'Resposta Antiga' });
    await context.primeira;
    vm.runInContext(`
        assert.equal(nome.textContent, 'Transportadora Nova');
        assert.equal(nome.classList.contains('campo-alterado'), true);
        const payload = coletarNotasAlteradas();
        assert.equal(payload.length, 1);
        assert.equal(payload[0].CodTra, 3);
        assert.equal(typeof payload[0].CodTra, 'number');
        assert.equal(payload[0].numNfc, 9009);
        input.value = '1'; buscarNomeTransportadora(input);
        assert.equal(nome.textContent, 'Transportadora Original');
        assert.equal(nome.classList.contains('campo-alterado'), false);
        assert.equal(coletarNotasAlteradas().length, 0);
        input.value = ''; buscarNomeTransportadora(input);
        assert.throws(() => coletarNotasAlteradas());
        input.value = '999'; globalThis.inexistente = buscarNomeTransportadora(input);
    `, context);
    reply(requests[2], null, 404);
    await context.inexistente;
    vm.runInContext(`
        assert.equal(nome.textContent, 'Transportadora não encontrada.');
        assert.throws(() => coletarNotasAlteradas());
    `, context);
    console.log('OK: nome, payload numérico, destaque, reversão, código inválido e respostas fora de ordem.');
})().catch(error => { console.error(error); process.exitCode = 1; });
