using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace DoxygenComments
{
    class DoxygenCompletionHandler : IOleCommandTarget
    {
        public const string CppTypeName = "C/C++";
        private vsCMElement[] supportedElements = { vsCMElement.vsCMElementClass, vsCMElement.vsCMElementFunction };
        private IOleCommandTarget m_nextCommandHandler;
        private IWpfTextView m_textView;
        private DoxygenCompletionHandlerProvider m_provider;
        private ICompletionSession m_session;
        private ITextDocumentFactoryService m_document;
        DTE m_dte;

        public DoxygenCompletionHandler(
            IVsTextView textViewAdapter,
            IWpfTextView textView,
            DoxygenCompletionHandlerProvider provider,
            ITextDocumentFactoryService textDocument,
            DTE dte)
        {
            //AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

            this.m_textView = textView;
            this.m_provider = provider;
            this.m_document = textDocument;
            this.m_dte = dte;

            // add the command to the command chain
            if (textViewAdapter != null &&
                textView != null &&
                textView.TextBuffer != null &&
                textView.TextBuffer.ContentType.TypeName == CppTypeName)
            {
                textViewAdapter.AddCommandFilter(this, out m_nextCommandHandler);
            }
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return m_nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            try
            {
                if (VsShellUtilities.IsInAutomationFunction(m_provider.ServiceProvider))
                {
                    return m_nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                }

                // make a copy of this so we can look at it after forwarding some commands 
                uint commandID = nCmdID;
                char typedChar = char.MinValue;

                // make sure the input is a char before getting it 
                if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
                {
                    typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
                }

                bool isCommit = nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN
                        || nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB;

                // check if the last character of one of the supported shortcuts is typed
                if ((typedChar == '!' || typedChar == '*' || isCommit) && m_dte != null)
                {
                    var currentILine = m_textView.TextSnapshot.GetLineFromPosition(
                        m_textView.Caret.Position.BufferPosition.Position);
                    int len = m_textView.Caret.Position.BufferPosition.Position - currentILine.Start.Position;
                    string currentLine = m_textView.TextSnapshot.GetText(currentILine.Start.Position, len);
                    string currentLineFull = currentILine.GetText();

                    // Get the current text properties
                    TextSelection ts = m_dte.ActiveDocument.Selection as TextSelection;
                    int oldLine = ts.ActivePoint.Line;
                    int oldOffset = ts.ActivePoint.LineCharOffset;

                    string shortcut = (currentLine + typedChar).Trim();
                    // First use only single line comment
                    if (typedChar == '*' && shortcut == "/**" && oldLine > 1)
                    {
                        ts.Insert(typedChar + "  ");
                        if (!currentLineFull.Contains("*/"))
                        {
                            ts.Insert("*/");
                        }
                        ts.MoveToLineAndOffset(oldLine, oldOffset + 2);
                        return VSConstants.S_OK;
                    }

                    // If it is a commit character in a '/** */' single line format, convert it to multiline
                    if (isCommit && currentLineFull.Contains("/**") && currentLineFull.Contains("*/") && oldOffset <= currentLineFull.IndexOf("*/") && oldOffset >= currentLineFull.IndexOf("/*") + 2)
                    {
                        string currentText = Regex.Replace(currentLineFull, "\\/\\*+|\\*+\\/", "").Trim();
                        if (currentText.Length > 0 && !currentText.EndsWith("."))
                        {
                            currentText += ".";
                        }

                        // Delete current comment
                        int lenToDelete = Regex.Replace(currentLineFull, ".*\\/\\*", "").Length;
                        ts.EndOfLine();
                        ts.DeleteLeft(lenToDelete);

                        // Create new multiline comment
                        currentLine = currentLineFull.Substring(0, currentLineFull.Length - lenToDelete);
                        currentLineFull = currentLine;
                        oldOffset = ts.ActivePoint.LineCharOffset;
                        return InsertMultilineComment(ts, '*', currentLineFull, currentLine, oldLine, oldOffset, currentText);
                    }

                    // This shortcut can be used without return (Because there is no single line format)
                    else if (shortcut == "/*!" || (oldLine == 1 && shortcut == "/**"))
                    {
                        return InsertMultilineComment(ts, typedChar, currentLineFull, currentLine, oldLine, oldOffset, "");
                    }
                }

                if (m_session != null && !m_session.IsDismissed)
                {
                    // check for a commit character 
                    if (nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN
                        || nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB)
                    {
                        // check for a selection 
                        // if the selection is fully selected, commit the current session 
                        if (m_session.SelectedCompletionSet.SelectionStatus.IsSelected)
                        {
                            string selectedCompletion = m_session.SelectedCompletionSet.SelectionStatus.Completion.DisplayText;
                            m_session.Commit();

                            // also, don't add the character to the buffer 
                            return VSConstants.S_OK;
                        }
                        else
                        {
                            // if there is no selection, dismiss the session
                            m_session.Dismiss();
                        }
                    }
                }
                else
                {
                    if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN)
                    {
                        string currentLine = m_textView.TextSnapshot.GetLineFromPosition(
                                m_textView.Caret.Position.BufferPosition.Position).GetText();
                        if (currentLine.TrimStart().StartsWith("*") && !currentLine.Contains("*/"))
                        {
                            TextSelection ts = m_dte.ActiveDocument.Selection as TextSelection;
                            string spaces = currentLine.Replace(currentLine.TrimStart(), "");
                            ts.Insert("\r\n" + spaces + "* ");
                            return VSConstants.S_OK;
                        }
                    }
                }

                // pass along the command so the char is added to the buffer
                int retVal = m_nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                if (typedChar == '\\')
                {
                    string currentLine = m_textView.TextSnapshot.GetLineFromPosition(
                                m_textView.Caret.Position.BufferPosition.Position).GetText();
                    if (currentLine.TrimStart().StartsWith("*"))
                    {
                        if (m_session == null || m_session.IsDismissed) // If there is no active session, bring up completion
                        {
                            if (this.TriggerCompletion())
                            {
                                m_session.SelectedCompletionSet.SelectBestMatch();
                                m_session.SelectedCompletionSet.Recalculate();
                                return VSConstants.S_OK;
                            }
                        }
                    }
                }
                else if (
                    commandID == (uint)VSConstants.VSStd2KCmdID.BACKSPACE ||
                    commandID == (uint)VSConstants.VSStd2KCmdID.DELETE ||
                    char.IsLetter(typedChar))
                {
                    if (m_session != null && !m_session.IsDismissed) // the completion session is already active, so just filter
                    {
                        m_session.SelectedCompletionSet.SelectBestMatch();
                        m_session.SelectedCompletionSet.Recalculate();
                        return VSConstants.S_OK;
                    }
                }

                return retVal;
            }
            catch
            {
            }

            return VSConstants.E_FAIL;
        }

        private int InsertMultilineComment(TextSelection ts, char typedChar, string currentLineFull, string currentLine, int oldLine, int oldOffset, string brief)
        {
            // Calculate how many spaces
            string spaces = currentLine.Replace(currentLine.TrimStart(), "");

            if (!currentLineFull.Contains("*/"))
                ts.Insert("*/");
            ts.LineDown();
            ts.EndOfLine();

            CodeElement codeElement = null;
            FileCodeModel fcm = m_dte.ActiveDocument.ProjectItem.FileCodeModel;
            if (fcm != null)
            {
                while (codeElement == null)
                {
                    foreach (vsCMElement e in supportedElements)
                    {
                        codeElement = fcm.CodeElementFromPoint(ts.ActivePoint, e);
                        if (codeElement != null)
                        {
                            break;
                        }
                    }

                    if (codeElement == null)
                    {
                        if (ts.CurrentLine == ts.BottomLine)
                        {
                            break;
                        }
                        ts.LineDown();
                    }
                }
            }

            if (codeElement == null && oldLine == 1)
            {

                string file = "";
                ITextDocument document;
                if (m_document.TryGetTextDocument(m_textView.TextBuffer, out document))
                {
                    var path = document.FilePath.Split('\\');
                    file = path[path.Length - 1];
                }

                StringBuilder sb = new StringBuilder("****************************************************************//**");
                sb.AppendFormat("\r\n" + spaces + " * \\file   {0}", file);
                sb.AppendFormat("\r\n" + spaces + " * \\brief  {0}", brief);
                sb.AppendFormat("\r\n" + spaces + " * ");
                sb.AppendFormat("\r\n" + spaces + " * \\author {0}", Environment.UserName);
                sb.AppendFormat("\r\n" + spaces + " * \\date   {0}", DateTime.Now.ToString("MMMM yyyy", new CultureInfo("en-GB")));
                sb.AppendFormat("\r\n**********************************************************************");

                ts.MoveToLineAndOffset(oldLine, oldOffset);
                ts.Insert(sb.ToString());
                ts.MoveToLineAndOffset(3, oldOffset);
                ts.EndOfLine();

                return VSConstants.S_OK;
            }

            if (codeElement != null && codeElement.Kind == vsCMElement.vsCMElementFunction)
            {
                CodeFunction function = codeElement as CodeFunction;
                StringBuilder sb = new StringBuilder(typedChar + "\r\n" + spaces + " * " + brief + "\r\n" + spaces + " * ");
                foreach (CodeElement child in codeElement.Children)
                {
                    CodeParameter parameter = child as CodeParameter;
                    if (parameter != null)
                    {
                        sb.AppendFormat("\r\n" + spaces + " * \\param {0}", parameter.Name);
                    }
                }

                if (function.Type.AsString != "void")
                {
                    sb.AppendFormat("\r\n" + spaces + " * \\return ");
                }

                sb.AppendFormat("\r\n" + spaces + " ");

                ts.MoveToLineAndOffset(oldLine, oldOffset);
                ts.Insert(sb.ToString());
                ts.MoveToLineAndOffset(oldLine, oldOffset);
                ts.LineDown();
                ts.EndOfLine();
                return VSConstants.S_OK;
            }
            else if (codeElement != null && codeElement.Kind == vsCMElement.vsCMElementClass)
            {
                CodeClass vsClass = codeElement as CodeClass;
                StringBuilder sb = new StringBuilder(typedChar + "\r\n" + spaces + " * " + brief);
                bool addedSpace = false;
                foreach (CodeElement child in vsClass.Children)
                {
                    CodeParameter parameter = child as CodeParameter;
                    if (parameter != null)
                    {
                        if (!addedSpace)
                        {
                            sb.Append("\r\n" + spaces + " * ");
                        }
                        sb.AppendFormat("\r\n" + spaces + " * \\tparam {0}", parameter.Name);
                    }
                }

                sb.AppendFormat("\r\n" + spaces + " ");

                ts.MoveToLineAndOffset(oldLine, oldOffset);
                ts.Insert(sb.ToString());
                ts.MoveToLineAndOffset(oldLine, oldOffset);
                ts.LineDown();
                ts.EndOfLine();
                return VSConstants.S_OK;
            }
            else
            {
                ts.MoveToLineAndOffset(oldLine, oldOffset);
                ts.Insert(typedChar + "\r\n" + spaces + " * " + brief + "\r\n" + spaces + " ");
                ts.MoveToLineAndOffset(oldLine, oldOffset);
                ts.LineDown();
                ts.EndOfLine();
                return VSConstants.S_OK;
            }
        }

        private bool TriggerCompletion()
        {
            try
            {
                if (m_session != null)
                {
                    return false;
                }

                // the caret must be in a non-projection location 
                SnapshotPoint? caretPoint =
                m_textView.Caret.Position.Point.GetPoint(
                    textBuffer => (!textBuffer.ContentType.IsOfType("projection")), PositionAffinity.Predecessor);
                if (!caretPoint.HasValue)
                {
                    return false;
                }

                m_session = m_provider.CompletionBroker.CreateCompletionSession(
                    m_textView,
                    caretPoint.Value.Snapshot.CreateTrackingPoint(caretPoint.Value.Position, PointTrackingMode.Positive),
                    true);

                // subscribe to the Dismissed event on the session 
                m_session.Dismissed += this.OnSessionDismissed;
                m_session.Start();

                return m_session != null && !m_session.IsDismissed;
            }
            catch
            {
            }

            return false;
        }

        private void OnSessionDismissed(object sender, EventArgs e)
        {
            if (m_session != null)
            {
                m_session.Dismissed -= this.OnSessionDismissed;
                m_session = null;
            }
        }

    }
}
