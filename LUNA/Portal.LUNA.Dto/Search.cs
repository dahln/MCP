namespace Portal.LUNA.Dto
{
    public class Search
    {
        public string? FilterText { get; set; }
        public int Page { get; set; } = 0;
        public int PageSize { get; set; } = 15;
        public string? SortBy { get; set; }
        public SortDirection SortDirection { get; set; } = SortDirection.Ascending;
    }

    public class SearchResponse<T>
    {
        public List<T> Results { get; set; } = new();
        public int Total { get; set; }
    }

    public enum SortDirection
    {
        Ascending,
        Descending
    }
}
