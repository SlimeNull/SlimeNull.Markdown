using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfMarkdown
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            output.PreviewTextInput += Output_PreviewTextInput;
        }

        void GetRunAndOffset(TextPointer textPointer, out Run? run, out int offset)
        {
            if (textPointer.Parent is Run parentRun)
            {
                run = parentRun;
                offset = parentRun.ContentStart.GetOffsetToPosition(textPointer);

                return;
            }

            run = null;
            offset = -1;
        }

        private void Output_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (output.CaretPosition.Parent is FrameworkContentElement element)
            {
                GetRunAndOffset(output.CaretPosition, out var run, out var offset);
                if (run != null && run.Tag is Markdig.Syntax.Inlines.LiteralInline literalInline)
                {
                    int insertIndex = literalInline.Span.Start + offset;
                    input.SelectionStart = insertIndex;
                    input.SelectedText = e.Text;
                    input.SelectionStart += e.Text.Length;
                }
            }

            e.Handled = true;
        }

        void Render()
        {
            var markdownDocument = Markdig.Markdown.Parse(input.Text);
            var config = MarkdownUtilities.MarkdownConfig.Default with
            {
                RenderMode = MarkdownUtilities.RenderMode.Immediate
            };

            MarkdownUtilities.Populate(config, markdownDocument, output.Document, output.CaretPosition);
        }

        private void input_TextChanged(object sender, TextChangedEventArgs e)
        {
            Render();
        }

        private void output_TextChanged(object sender, TextChangedEventArgs e)
        {
            Debug.WriteLine(output.CaretPosition.Parent);

            foreach (var change in e.Changes)
            {
                if (change.AddedLength != 0)
                {
                    char[] buffer = new char[change.AddedLength];
                    output.CaretPosition.GetTextInRun(LogicalDirection.Backward, buffer, 0, change.AddedLength);
                }
            }
        }

        private void output_SelectionChanged(object sender, RoutedEventArgs e)
        {
            //Render();
        }
    }
}