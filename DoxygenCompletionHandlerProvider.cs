using EnvDTE;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;

namespace DoxygenComments
{
    [Export(typeof(IVsTextViewCreationListener))]
    [Name("C++ Doxygen Completion Handler")]
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    class DoxygenCompletionHandlerProvider : IVsTextViewCreationListener
    {
        [Import]
        public IVsEditorAdaptersFactoryService AdapterService = null;

        [Import]
        public ICompletionBroker CompletionBroker { get; set; }

        [Import]
        public SVsServiceProvider ServiceProvider { get; set; }

        [Import]
        public ITextDocumentFactoryService textDocumentFactory { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            try
            {
                IWpfTextView textView = this.AdapterService.GetWpfTextView(textViewAdapter);
                if (textView == null)
                {
                    return;
                }

                Func<DoxygenCompletionHandler> createCommandHandler = delegate ()
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    var dte = (DTE) ServiceProvider.GetService(typeof(DTE));
                    var vsShell = (IVsShell)ServiceProvider.GetService(typeof(IVsShell));

                    return new DoxygenCompletionHandler(textViewAdapter, textView, this, textDocumentFactory, dte, vsShell);
                };

                textView.Properties.GetOrCreateSingletonProperty(createCommandHandler);
            }
            catch
            {
            }
        }
    }
}
