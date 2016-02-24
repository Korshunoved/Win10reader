namespace BookParser.Tokens
{
    public class ImageTokenBlock : TokenBlockBase
    {
        public ImageTokenBlock()
        {
            Height = -1.0;
        }

        public string ImageID { get; set; }
    }
}