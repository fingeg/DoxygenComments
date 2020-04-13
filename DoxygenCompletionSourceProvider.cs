using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace DoxygenComments
{
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType("code")]
    [Name("doxygen doc comment completion")]
    class DoxygenCompletionSourceProvider : ICompletionSourceProvider
    {
        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }
        
        [Import]
        internal IGlyphService GlyphService { get; set; }

        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            return new DoxygenCompletionSource(this, textBuffer);
        }
    }
}
