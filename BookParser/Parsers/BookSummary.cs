namespace BookParser.Parsers
{
    public class BookSummary
    {
        public string Title
        {
            get;
            set;
        }

        public string AuthorName
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        public string UniqueId
        {
            get;
            set;
        }

        public string Language
        {
            get;
            set;
        }

        public bool IsTrial { get; set; }

        public BookSummary()
        {
            UniqueId = Description = Title = AuthorName = string.Empty;
        }
    }
}
