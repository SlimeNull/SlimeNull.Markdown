using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace WpfMarkdown
{
    static class MarkdownUtilities
    {
        public record struct MarkdownConfig
        {
            public RenderMode RenderMode;

            public int FontWeightNormal;
            public int FontWeightBold;

            public double FontSizeNormal;
            public double FontSizeFactorHeading1;
            public double FontSizeFactorHeading2;
            public double FontSizeFactorHeading3;
            public double FontSizeFactorHeading4;
            public double FontSizeFactorHeading5;
            public double FontSizeFactorHeading6;

            public static MarkdownConfig Default =>
                new MarkdownConfig()
                {
                    FontWeightNormal = 400,
                    FontWeightBold = 700,

                    FontSizeNormal = 16,
                    FontSizeFactorHeading1 = 2,
                    FontSizeFactorHeading2 = 1.5,
                    FontSizeFactorHeading3 = 1.17,
                    FontSizeFactorHeading4 = 1,
                    FontSizeFactorHeading5 = .83,
                    FontSizeFactorHeading6 = .67,
                };
        }

        public enum RenderMode
        {
            Default,
            Immediate,
            Complete,
        }

        struct MarkdownContext
        {
            public Block? ActiveFlowBlock;
            public DependencyObject? ActiveFlowElement;
        }

        static T CastOrCreate<TParent, T>(TParent? source, out TParent? createdInstance)
            where TParent : class
            where T : TParent, new()
        {
            createdInstance = default;

            if (source is T existed)
                return existed;

            var newInstance = new T();
            createdInstance = newInstance;
            return newInstance;
        }

        static TItem GetOrCreateAndUpdate<TList, TItem>(TList list, int index)
            where TList : IList
            where TItem : new()
        {
            if (index < list.Count && list[index] is TItem existedItem)
                return existedItem;

            var newItem = new TItem();

            if (index < list.Count)
                list[index] = newItem;
            else
                list.Add(newItem);

            return newItem;
        }

        static bool HasElementInSpan(Span span, DependencyObject element)
        {
            var child = span.Inlines.FirstInline;
            while (child != null)
            {
                if (child == element)
                    return true;

                if (child is Span childSpan && HasElementInSpan(childSpan, element))
                    return true;

                child = child.NextInline;
            }

            return false;
        }

        static void PopulateHeading(
            MarkdownConfig config,
            MarkdownContext context,
            Markdig.Syntax.HeadingBlock markdown,
            System.Windows.Documents.Paragraph flowElement)
        {
            flowElement.Tag = markdown;

            bool active = context.ActiveFlowBlock == flowElement;

            string? prefix = null;
            if (config.RenderMode == RenderMode.Immediate && active)
            {
                StringBuilder sb = new StringBuilder(markdown.Level + 1);
                sb.Append('#', markdown.Level);
                sb.Append(' ');

                prefix = sb.ToString();
            }

            if (markdown.Inline is { } inlines)
                PopulateInlines(config, context, inlines, flowElement.Inlines, prefix, null);

            var fontSize = markdown.Level switch
            {
                1 => config.FontSizeNormal * config.FontSizeFactorHeading1,
                2 => config.FontSizeNormal * config.FontSizeFactorHeading2,
                3 => config.FontSizeNormal * config.FontSizeFactorHeading3,
                4 => config.FontSizeNormal * config.FontSizeFactorHeading4,
                5 => config.FontSizeNormal * config.FontSizeFactorHeading5,
                6 => config.FontSizeNormal * config.FontSizeFactorHeading6,
                _ => config.FontSizeNormal,
            };

            flowElement.FontSize = fontSize;
            flowElement.FontWeight = System.Windows.FontWeight.FromOpenTypeWeight(config.FontWeightBold);
        }

        static void PopulateParagraph(
            MarkdownConfig config,
            MarkdownContext context,
            Markdig.Syntax.ParagraphBlock markdown,
            System.Windows.Documents.Paragraph flowElement)
        {
            flowElement.Tag = markdown;

            if (markdown.Inline is { } inlines)
                PopulateInlines(config, context, inlines, flowElement.Inlines, null, null);

            flowElement.FontSize = config.FontSizeNormal;
            flowElement.FontWeight = FontWeights.Normal;
        }

        static void PopulateList(
            MarkdownConfig config,
            MarkdownContext context,
            Markdig.Syntax.ListBlock markdown,
            System.Windows.Documents.List flowElement)
        {
            flowElement.Tag = markdown;
            flowElement.MarkerStyle = markdown.IsOrdered ? TextMarkerStyle.Decimal : TextMarkerStyle.Disc;
            System.Windows.Documents.ListItem? currentFlowItem = flowElement.ListItems.FirstListItem;

            foreach (var childBlock in markdown)
            {
                if (childBlock is not Markdig.Syntax.ListItemBlock markdownItem)
                    break;

                var flowItem = CastOrCreate<System.Windows.Documents.ListItem, System.Windows.Documents.ListItem>(currentFlowItem, out var createdFlowItem);
                flowItem.Tag = markdownItem;

                if (createdFlowItem != null)
                {
                    currentFlowItem = createdFlowItem;
                    flowElement.ListItems.Add(createdFlowItem);
                }

                PopulateListItem(config, context, markdownItem, flowItem);

                currentFlowItem = currentFlowItem.NextListItem;
            }
        }

        static void PopulateListItem(
            MarkdownConfig config,
            MarkdownContext context,
            Markdig.Syntax.ListItemBlock markdown,
            System.Windows.Documents.ListItem flowElement)
        {
            flowElement.Tag = markdown;
            PopulateBlocks(config, context, markdown, flowElement.Blocks);
        }

        static void PopulateBlankLine(
            MarkdownConfig config,
            MarkdownContext context,
            Markdig.Syntax.BlankLineBlock markdown,
            System.Windows.Documents.Paragraph flowElement)
        {

        }

        static void PopulateLiteralInline(
            MarkdownConfig config,
            MarkdownContext context,
            Markdig.Syntax.Inlines.LiteralInline markdown,
            System.Windows.Documents.Run flowElement,
            string? prefix,
            string? suffix)
        {
            var newText = markdown.Content.ToString();

            if (config.RenderMode == RenderMode.Complete ||
                config.RenderMode == RenderMode.Immediate)
            {
                newText = $"{prefix}{newText}{suffix}";
            }

            flowElement.Tag = markdown;

            if (flowElement.Text != newText)
                flowElement.Text = newText;
        }

        static void PopulateEmphasisInline(
            MarkdownConfig config,
            MarkdownContext context,
            Markdig.Syntax.Inlines.EmphasisInline markdown,
            System.Windows.Documents.Span flowElement,
            string? prefix,
            string? suffix)
        {
            flowElement.Tag = markdown;

            if (config.RenderMode == RenderMode.Immediate && context.ActiveFlowElement != null)
            {
                bool active = context.ActiveFlowElement == flowElement || HasElementInSpan(flowElement, context.ActiveFlowElement);
                string? selfPrefixAndSuffix = active ? new string(markdown.DelimiterChar, markdown.DelimiterCount) : null;

                prefix = $"{prefix}{selfPrefixAndSuffix}";
                suffix = $"{selfPrefixAndSuffix}{suffix}";
            }

            PopulateInlines(config, context, markdown, flowElement.Inlines, prefix, suffix);

            flowElement.FontWeight = default;

            if (markdown.DelimiterCount >= 2)
                flowElement.FontWeight = System.Windows.FontWeight.FromOpenTypeWeight(config.FontWeightBold);

            if (markdown.DelimiterCount % 2 == 1)
                flowElement.FontStyle = FontStyles.Italic;
        }

        static void PopulateLineBreak(
            MarkdownConfig config,
            MarkdownContext context,
            Markdig.Syntax.Inlines.LineBreakInline markdown,
            System.Windows.Documents.LineBreak flowElement,
            string? prefix,
            string? suffix)
        {

        }

        static void PopulateBlock(
            MarkdownConfig config,
            MarkdownContext context,
            Markdig.Syntax.Block markdownBlock,
            System.Windows.Documents.Block? flowBlock,
            out System.Windows.Documents.Block? createdFlowBlock)
        {
            createdFlowBlock = null;

            switch (markdownBlock)
            {
                case Markdig.Syntax.HeadingBlock headingBlock:
                {
                    var flowElement = CastOrCreate<System.Windows.Documents.Block, System.Windows.Documents.Paragraph>(flowBlock, out createdFlowBlock);
                    PopulateHeading(config, context, headingBlock, flowElement);
                    return;
                }

                case Markdig.Syntax.ParagraphBlock paragraphBlock:
                {
                    var flowElement = CastOrCreate<System.Windows.Documents.Block, System.Windows.Documents.Paragraph>(flowBlock, out createdFlowBlock);
                    PopulateParagraph(config, context, paragraphBlock, flowElement);
                    return;
                }

                case Markdig.Syntax.ListBlock listBlock:
                {
                    var flowElement = CastOrCreate<System.Windows.Documents.Block, System.Windows.Documents.List>(flowBlock, out createdFlowBlock);
                    PopulateList(config, context, listBlock, flowElement);
                    return;
                }

                case Markdig.Syntax.BlankLineBlock blankLineBlock:
                {
                    var flowElement = CastOrCreate<System.Windows.Documents.Block, System.Windows.Documents.Paragraph>(flowBlock, out createdFlowBlock);
                    PopulateBlankLine(config, context, blankLineBlock, flowElement);
                    return;
                }

                default:
                    return;
            }
        }

        static void PopulateInline(
            MarkdownConfig config,
            MarkdownContext context,
            Markdig.Syntax.Inlines.Inline markdownInline,
            System.Windows.Documents.Inline? flowInline,
            string? prefix, string? suffix,
            out System.Windows.Documents.Inline? createdFlowInline)
        {
            createdFlowInline = null;

            switch (markdownInline)
            {
                case Markdig.Syntax.Inlines.LiteralInline literalInline:
                {
                    var flowElement = CastOrCreate<System.Windows.Documents.Inline, System.Windows.Documents.Run>(flowInline, out createdFlowInline);
                    PopulateLiteralInline(config, context, literalInline, flowElement, prefix, suffix);
                    return;
                }

                case Markdig.Syntax.Inlines.EmphasisInline emphasisInline:
                {
                    var flowElement = CastOrCreate<System.Windows.Documents.Inline, System.Windows.Documents.Span>(flowInline, out createdFlowInline);
                    PopulateEmphasisInline(config, context, emphasisInline, flowElement, prefix, suffix);
                    return;
                }

                case Markdig.Syntax.Inlines.LineBreakInline lineBreakInline:
                {
                    var flowElement = CastOrCreate<System.Windows.Documents.Inline, System.Windows.Documents.LineBreak>(flowInline, out createdFlowInline);
                    PopulateLineBreak(config, context, lineBreakInline, flowElement, prefix, suffix);
                    return;
                }

                default:
                    return;
            }
        }

        static void PopulateBlocks(
            MarkdownConfig config,
            MarkdownContext context,
            IList<Markdig.Syntax.Block> markdownBlocks,
            System.Windows.Documents.BlockCollection flowBlocks)
        {
            System.Windows.Documents.Block? currentFlowBlock = flowBlocks.FirstBlock;
            System.Windows.Documents.Block? lastFlowBlock = null;

            for (int i = 0; i < markdownBlocks.Count; i++)
            {
                Markdig.Syntax.Block markdownBlock = markdownBlocks[i];
                System.Windows.Documents.Block? flowBlock = currentFlowBlock;

                PopulateBlock(config, context, markdownBlock, flowBlock, out var createdFlowBlock);

                if (createdFlowBlock != null)
                {
                    if (flowBlock != null)
                        flowBlocks.InsertBefore(flowBlock, createdFlowBlock);
                    else
                        flowBlocks.Add(createdFlowBlock);

                    flowBlock = createdFlowBlock;
                    currentFlowBlock = createdFlowBlock;
                }

                if (flowBlock != null)
                    lastFlowBlock = flowBlock;

                if (currentFlowBlock != null)
                    currentFlowBlock = currentFlowBlock.NextBlock;
            }

            while (flowBlocks.LastBlock != lastFlowBlock && flowBlocks.Count > 0)
                flowBlocks.Remove(flowBlocks.LastBlock);
        }

        static void PopulateInlines(
            MarkdownConfig config,
            MarkdownContext context,
            IEnumerable<Markdig.Syntax.Inlines.Inline> markdownInlines,
            System.Windows.Documents.InlineCollection flowInlines,
            string? prefix,
            string? suffix)
        {
            Markdig.Syntax.Inlines.Inline? currentMarkdownInline = markdownInlines.FirstOrDefault();
            System.Windows.Documents.Inline? currentFlowInline = flowInlines.FirstInline;
            System.Windows.Documents.Inline? lastFlowInline = null;

            while (currentMarkdownInline != null)
            {
                Markdig.Syntax.Inlines.Inline markdownInline = currentMarkdownInline;
                System.Windows.Documents.Inline? flowInline = currentFlowInline;

                PopulateInline(config, context, markdownInline, flowInline, prefix, suffix, out var createdFlowInline);

                if (createdFlowInline != null)
                {
                    if (flowInline != null)
                        flowInlines.InsertBefore(flowInline, createdFlowInline);
                    else
                        flowInlines.Add(createdFlowInline);

                    flowInline = createdFlowInline;
                    currentFlowInline = createdFlowInline;
                }

                currentMarkdownInline = currentMarkdownInline.NextSibling;

                if (currentFlowInline != null)
                    currentFlowInline = currentFlowInline.NextInline;

                if (flowInline != null)
                    lastFlowInline = flowInline;
            }

            while (flowInlines.LastInline != lastFlowInline && flowInlines.Count > 0)
                flowInlines.Remove(flowInlines.LastInline);
        }

        public static void Populate(MarkdownConfig config, Markdig.Syntax.MarkdownDocument markdownDocument, FlowDocument flowDocument, TextPointer? caret)
        {
            MarkdownContext context = new()
            {
                ActiveFlowBlock = caret?.Paragraph,
                ActiveFlowElement = caret?.Parent,
            };

            PopulateBlocks(config, context, markdownDocument, flowDocument.Blocks);
        }
    }
}
