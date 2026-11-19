namespace WS_NF_HML;

// Complemento do proxy gerado. Não editar Reference.cs.
public partial class notafiscalGravarNotasFiscaisEntradaInDadosGeraisProdutos {
    // IsNullable=true no proxy faria null virar xsi:nil; omita a derivação ausente.
    public bool ShouldSerializecodDer() => !string.IsNullOrWhiteSpace(codDer);
}
