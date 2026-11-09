namespace SistemaEntradaNotaFiscal.Services {
    public record LoginResult(bool Success, string? ConnectionCode, string? Error);

    public class AuthService {
        private static readonly HttpClient client = new() { Timeout = TimeSpan.FromSeconds(15) };
        private static readonly string URL = "http://172.17.0.7:8080/sapiensweb/conector?";

        public async Task<LoginResult> DoLoginAsync(string username, string password) {
      
            var url = URL + "SIS=CO&LOGIN=SID&ACAO=EXESENHA"
                          + "&NOMUSU=" + Uri.EscapeDataString(username)
                          + "&SENUSU=" + Uri.EscapeDataString(password);

            string body;
            try {
                using var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return Falha($"O ERP respondeu com erro (HTTP {(int)response.StatusCode}). Tente novamente.");

                body = (await response.Content.ReadAsStringAsync()).Trim();
            }
            catch (TaskCanceledException) {
                return Falha("O ERP demorou demais para responder. Tente novamente.");
            }
            catch (HttpRequestException) {
                return Falha("Não foi possível conectar ao ERP. Tente novamente em instantes.");
            }

            // Sucesso: o conector devolve apenas o código de conexão (somente dígitos).
            // O status HTTP sozinho não serve: o erro também vem no corpo da resposta.
            if (body.Length > 0 && body.All(char.IsAsciiDigit))
                return new LoginResult(true, body, null);

            // Erro de negócio, ex.: "ERRO: Usuário ou senha inválido"
            if (body.StartsWith("ERRO", StringComparison.OrdinalIgnoreCase)) {
                var mensagem = body.Substring(4).TrimStart(':', ' ');
                if (string.IsNullOrWhiteSpace(mensagem))
                    mensagem = "Usuário ou senha inválidos.";
                if (mensagem.Length > 200)
                    mensagem = mensagem[..200];
                return Falha(mensagem);
            }

            // Qualquer outra coisa (ex.: HTML de timeout) nunca é tratada como login válido.
            return Falha("Resposta inesperada do ERP. Tente novamente.");
        }

        private static LoginResult Falha(string erro) => new(false, null, erro);
    }
}
