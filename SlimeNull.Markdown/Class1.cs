namespace SlimeNull.Markdown
{
    public class Class1
    {

    }

    public class Document
    {

    }

    public class Block
    {

    }

    public class Header : Block
    {
        public int Level { get; set; }
    }

    public class Paragraph : Block
    {
        public List<Inline> Inlines { get; } = new();
    }



    public class Inline
    {

    }

    public class HyperLink : Inline
    {
        public HyperLink(Uri source, Inline title, bool preview)
        {
            Source = source;
            Title = title;
            Preview = preview;
        }

        public Uri Source { get; set; }
        public Inline Title { get; set; }
        public bool Preview { get; set; }
    }

    public class Itali

    public class CodeText : Inline
    {
        public string Content { get; set; } = string.Empty;
    }

    public class Text : Inline
    {
        public string Content { get; set; } = string.Empty;
    }
}
