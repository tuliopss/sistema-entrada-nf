namespace SistemaEntradaNotaFiscal {
    public class PagedResult<T> {
        public List<T> Items { get; init; } = new();
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
        public int TotalCount { get; init; }
        public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    }
}
