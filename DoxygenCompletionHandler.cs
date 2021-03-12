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
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.SqlServer.Server;

namespace DoxygenComments
{
    enum CommentFormat
    {
        header,
        function,
        unknown
    }

    class DoxygenCompletionHandler : IOleCommandTarget
    {
        public const string CppTypeName = "C/C++";
        private char m_header_char;
        private IOleCommandTarget m_nextCommandHandler;
        private IWpfTextView m_textView;
        private DoxygenCompletionHandlerProvider m_provider;
        private ICompletionSession m_session;
        private ITextDocumentFactoryService m_document;
        private SettingsHelper m_settings;
        DTE m_dte;

        public DoxygenCompletionHandler(
            IVsTextView textViewAdapter,
            IWpfTextView textView,
            DoxygenCompletionHandlerProvider provider,
            ITextDocumentFactoryService textDocument,
            DTE dte,
            IVsShell vsShell)
        {
            //AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
            ThreadHelper.ThrowIfNotOnUIThread();

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

            // Init the formats settings
            m_settings = new SettingsHelper();

            // Set the header character, because there is no single line support
            m_header_char = m_settings.HeaderFormat.Trim()[2];
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return m_nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
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

                // Check if it is a commit character, to generate a multiline comment
                bool isCommitChar = nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN
                        || nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB;

                bool showCompletion = nCmdID == (uint)VSConstants.VSStd2KCmdID.COMPLETEWORD;

                // Handle only typed characters or in case of an active completion also deletions
                if (typedChar == '\0' && !isCommitChar && !showCompletion)
                {
                    return m_nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                }

                // check if the last character of one of the supported shortcuts is typed
                if (!m_provider.CompletionBroker.IsCompletionActive(m_textView)
                    && m_dte != null
                    && (typedChar == m_header_char || isCommitChar || typedChar == '!'))
                {
                    var currentILine = m_textView.TextSnapshot.GetLineFromPosition(
                        m_textView.Caret.Position.BufferPosition.Position);
                    int len = m_textView.Caret.Position.BufferPosition.Position - currentILine.Start.Position;
                    string currentLine = m_textView.TextSnapshot.GetText(currentILine.Start.Position, len);
                    string currentLineFull = currentILine.GetText();

                    string typed_shortcut = (currentLine + typedChar).Trim();

                    if (typed_shortcut.Trim().Length >= 3)
                    {
                        // Get the current text properties
                        TextSelection ts = m_dte.ActiveDocument.Selection as TextSelection;
                        int oldLine = ts.ActivePoint.Line;
                        int oldOffset = ts.ActivePoint.LineCharOffset;
                        string lineEnding = GetLineEnding();

                        // First use only single line comment
                        // Single line comments are not supported in the first line, because of the header comment 
                        if (typedChar == '*' && typed_shortcut == "/**" && oldLine > 1)
                        {
                            ts.Insert(typedChar + "  ");
                            if (!currentLineFull.Contains("*/"))
                            {
                                ts.Insert("*/");
                            }
                            ts.MoveToLineAndOffset(oldLine, oldOffset + 2);
                            return VSConstants.S_OK;
                        }

                        // If it is a commit character check if there is a comment to expand
                        if (isCommitChar
                            && ShouldExpand(ts, currentLineFull, oldLine, oldOffset, out var commentFormat, out var codeElement, out var shortcut))
                        {
                            // Replace all possible comment characters to get the raw brief
                            string currentText = Regex.Replace(currentLineFull.Replace(shortcut, ""), @"\/\*+|\*+\/|\/\/+", "").Trim();

                            // Delete current comment
                            int lenToDelete = Regex.Replace(currentLineFull, @".*\/\*|^[^\/]*\/\/", "").Length;
                            ts.MoveToLineAndOffset(oldLine, oldOffset);
                            ts.EndOfLine();
                            ts.DeleteLeft(lenToDelete);

                            // Create new multiline comment
                            currentLine = currentLineFull.Substring(0, currentLineFull.Length - lenToDelete);
                            currentLineFull = currentLine;
                            oldOffset = ts.ActivePoint.LineCharOffset;
                            return InsertMultilineComment(commentFormat, codeElement, ts, currentLine, lineEnding,
                                oldLine, oldOffset, currentText);
                        }

                        // The header can be used without single line format
                        else if (oldLine == 1)
                        {
                            var headerShortcut = m_settings.HeaderFormat.Substring(0, 3);

                            if (typed_shortcut == headerShortcut || typed_shortcut == "/**" || typed_shortcut == "/*!" || typed_shortcut == "///")
                            {
                                // Delete current end comment chars
                                ts.EndOfLine();
                                int lenToDelete = ts.ActivePoint.LineCharOffset - oldOffset;
                                ts.DeleteLeft(lenToDelete);

                                return InsertMultilineComment(CommentFormat.header, null, ts,  currentLine, lineEnding,
                                    oldLine, oldOffset, "");
                            }
                        }
                        // '/*!' is a always active shortcut without single line
                        // This is for an eseaier beginning and for the same workflow as older versions
                        else if (typed_shortcut == "/*!")
                        {
                            var _commentFormat = GetCommentFormat(ts, oldLine, oldOffset, out var _codeElement);

                            // Delete current end comment chars
                            ts.EndOfLine();
                            int lenToDelete = ts.ActivePoint.LineCharOffset - oldOffset;
                            ts.DeleteLeft(lenToDelete);

                            return InsertMultilineComment(_commentFormat, _codeElement, ts, currentLine, lineEnding,
                                oldLine, oldOffset, "");
                        }
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
                else if (!m_provider.CompletionBroker.IsCompletionActive(m_textView))
                {
                    if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN)
                    {
                        ITextSnapshotLine currentILine = m_textView.TextSnapshot.GetLineFromPosition(
                                m_textView.Caret.Position.BufferPosition.Position);
                        string currentLine = currentILine.GetText();

                        // TODO: check for being inside a comment block
                        // Insert a '*' when creating a new line in a mutline comment 
                        if (currentLine.TrimStart().StartsWith("*") && !currentLine.Contains("*/"))
                        {
                            TextSelection ts = m_dte.ActiveDocument.Selection as TextSelection;
                            string spaces = currentLine.Replace(currentLine.TrimStart(), "");
                            string lineEnding = GetLineEnding();
                            ts.Insert(lineEnding + spaces + "* ");
                            return VSConstants.S_OK;
                        }

                        // Insert a '///' when creating a new line in a mutline comment 
                        if (currentLine.TrimStart().StartsWith("///"))
                        {
                            TextSelection ts = m_dte.ActiveDocument.Selection as TextSelection;
                            string spaces = currentLine.Replace(currentLine.TrimStart(), "");
                            string lineEnding = GetLineEnding();
                            ts.Insert(lineEnding + spaces + "/// ");
                            return VSConstants.S_OK;
                        }
                    }
                }

                // pass along the command so the char is added to the buffer
                int retVal = m_nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                
                if (typedChar == '\\' || typedChar == '@' || showCompletion)
                {
                    string currentLine = m_textView.TextSnapshot.GetLineFromPosition(
                                m_textView.Caret.Position.BufferPosition.Position).GetText();
                    if (currentLine.TrimStart().StartsWith("*") || currentLine.TrimStart().StartsWith("///"))
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

        private bool HasCommentBlockStart(string line)
        {
            string[] formats = new string[]
            {
                "/**", m_settings.HeaderFormat, m_settings.FunctionFormat, m_settings.DefaultFormat
            };

            var trimmedLine = line.Trim();

            foreach (string format in formats)
            {
                string formatStart = format.Trim().Substring(0, 3);

                if (trimmedLine.StartsWith(formatStart))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ShouldExpand(TextSelection ts, string line, int lineNumber, int curserOffset,
            out CommentFormat commentFormat, out CodeElement codeElement, out string shortcut)
        {
            // Get format for current position
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!HasCommentBlockStart(line))
            {
                commentFormat = CommentFormat.unknown;
                codeElement = null;
                shortcut = null;
                return false;
            }

            commentFormat = GetCommentFormat(ts, lineNumber, curserOffset, out codeElement);

            // Get the shortcut for the current format
            string format;
            switch (commentFormat)
            {
                case CommentFormat.header:
                    format = m_settings.HeaderFormat;
                    break;
                case CommentFormat.function:
                    format = m_settings.FunctionFormat;
                    break;
                default:
                    format = m_settings.DefaultFormat;
                    break;
            }
            shortcut = format.Trim().Substring(0, 3);

            // Check if the line has the correct shortcut
            if (line.Trim().StartsWith(shortcut))
            {
                // The curser must be after the shortcut
                if (curserOffset <= line.IndexOf(shortcut) + 3)
                {
                    return false;
                }

                // If it is the '/*' format, the close symbol '/*' must be in the same line
                if (shortcut.StartsWith("/*"))
                {
                    if (!line.Contains("*/"))
                    {
                        return false;
                    }
                    return curserOffset <= line.IndexOf("*/") + 1;
                }
                return true;
            }
            return false;
        }

        private CommentFormat GetCommentFormat(TextSelection ts, int currentLine, int currentOffset, out CodeElement codeElement)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            codeElement = null;
            FileCodeModel fcm = m_dte.ActiveDocument.ProjectItem.FileCodeModel;
            if (fcm != null)
            {
                int start = ts.TopPoint.AbsoluteCharOffset,
                    end = ts.BottomPoint.AbsoluteCharOffset,
                    startLine = ts.TopPoint.Line,
                    startColumn = ts.TopPoint.VirtualDisplayColumn;

                // Go to the next line to check if there is a code element
                ts.MoveToLineAndOffset(currentLine, currentOffset);
                ts.LineDown();
                ts.EndOfLine();

                /// Check max five lines below the current
                for (int i = 0; i < 5; i++)
                {
                    // Check foreach supported code element if there is one in the current line
                    codeElement = fcm.CodeElementFromPoint(ts.ActivePoint, vsCMElement.vsCMElementFunction);

                    // If there is no code element, check if in the next line
                    if (codeElement == null)
                    {
                        ts.LineDown();
                    } else
                    {
                        break;
                    }
                }

                // Go back with the curser
                if (startLine == currentLine
                    && startColumn == currentOffset)
                {
                    ts.MoveToAbsoluteOffset(end);
                    ts.MoveToAbsoluteOffset(start, true);
                }
                else
                {
                    ts.MoveToAbsoluteOffset(start);
                    ts.MoveToAbsoluteOffset(end, true);
                }
            }

            // If it is the first line, create the header
            if (codeElement == null && currentLine == 1)
            {
                return CommentFormat.header;
            }

            // If it is before a function, create the function documentation
            else if (codeElement != null && codeElement.Kind == vsCMElement.vsCMElementFunction)
            {
                return CommentFormat.function;
            }
            else
            {
                return CommentFormat.unknown;
            }
        }

        private string ReplaceLineWith(string text, string oldLine, string newLine)
        {
            // Try all possible line endings
            return text
                .Replace(oldLine + "\r\n", newLine)
                .Replace(oldLine + "\n", newLine)
                .Replace(oldLine + "\r", newLine);
        }

        private int InsertMultilineComment(CommentFormat commentFormat, CodeElement codeElement, TextSelection ts,
            string currentLine, string lineEnding, int oldLine, int oldOffset, string brief)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // Calculate how many spaces
            string spaces = currentLine.Replace(currentLine.TrimStart(), "");

            // The default format:
            string format;


            if (commentFormat == CommentFormat.header)
            {
                format = m_settings.HeaderFormat;
            }
            else if (commentFormat == CommentFormat.function)
            {
                format = m_settings.FunctionFormat;

                CodeFunction function = codeElement as CodeFunction;
                if (format.Contains("$PARAMS"))
                {
                    // Get the params line
                    // Normally exact one match
                    Match match = Regex.Match(format, @".*\$PARAMS.*");
                    if (match != null && match.Success)
                    { 
                        StringBuilder sb = new StringBuilder();
                        foreach (CodeElement child in codeElement.Children)
                        {
                            CodeParameter parameter = child as CodeParameter;
                            if (parameter != null)
                            {
                                sb.AppendFormat(match.Value.Replace("$PARAMS", parameter.Name) + "\n");
                            }
                        }
                        format = ReplaceLineWith(format, match.Value, sb.ToString());
                    }
                }
                
                if (format.Contains("$RETURN"))
                {
                    Match match = Regex.Match(format, @".*\$RETURN.*");
                    if (match != null)
                    {
                        if (function.Type.AsString != "void")
                        {
                            format = format.Replace(match.Value, match.Value.Replace("$RETURN", ""));
                        } else
                        {
                            format = ReplaceLineWith(format, match.Value, "");
                        }
                    }
                }

                format = format.Replace("$FUNCTION_NAME", function.Name);
                format = format.Replace("$FUNCTION_TYPE", function.Type.AsString);
            }
            else
            {
                format = m_settings.DefaultFormat;
            }

            // Insert the format into the text field
            var insertionText = GetFinalFormat(format, brief, spaces, lineEnding, out var endPos);
            ts.MoveToLineAndOffset(oldLine, oldOffset);
            ts.Insert(insertionText);
            
            // Move the curser to the $END position if set
            if (endPos >= 0) {
                var textToEnd = insertionText.Substring(0, endPos);
                var lines = textToEnd.Split('\n');
                var offset = lines.Length > 0 ? lines[lines.Length - 1].Length : 0;
                if (lines.Length == 1)
                {
                    offset += oldOffset;
                } else
                {
                    offset += 1;
                }
                ts.MoveToLineAndOffset(oldLine + lines.Length - 1, offset);
            } else {
                ts.MoveToLineAndOffset(oldLine, oldOffset);
                ts.LineDown();
                ts.EndOfLine();
            }
            return VSConstants.S_OK;
        }

        private string GetFinalFormat(string format, string brief, string spaces, string lineEnding, out int endPos)
        {
            /// Remove first two characters, because they are typed already
            format = format.Trim().Substring(2);

            /// Use the correct line endings and indent
            format = Regex.Replace(format, @"(\r\n)|(\r|\n)", lineEnding + spaces);

            // Replace all variables with the correct values
            // Specififc variables like $PARAMS and $RETURN must be handled before in
            // a function specific part
            if (format.Contains("$BRIEF"))
            {
                format = format.Replace("$BRIEF", brief);
            }
            if (format.Contains("$MONTHNAME_EN"))
            {
                CultureInfo ci = new CultureInfo("en-US");
                var month = DateTime.Now.ToString("MMMM", ci);
                format = format.Replace("$MONTHNAME_EN", month);
            }
            if (format.Contains("$MONTH_2"))
            {
                var month = DateTime.Now.Month.ToString().PadLeft(2, '0');
                format = format.Replace("$MONTH_2", month);
            }
            if (format.Contains("$MONTH"))
            {
                var month = DateTime.Now.Month.ToString();
                format = format.Replace("$MONTH", month);
            }
            if (format.Contains("$DAY_OF_MONTH_2"))
            {
                var day = DateTime.Now.Day.ToString().PadLeft(2, '0');
                format = format.Replace("$DAY_OF_MONTH_2", day);
            }
            if (format.Contains("$DAY_OF_MONTH"))
            {
                var day = DateTime.Now.Day.ToString();
                format = format.Replace("$DAY_OF_MONTH", day);
            }
            if (format.Contains("$YEAR"))
            {
                var year = DateTime.Now.Year.ToString();
                format = format.Replace("$YEAR", year);
            }
            if (format.Contains("$FILENAME"))
            {
                string file = "";
                ITextDocument document;
                if (m_document.TryGetTextDocument(m_textView.TextBuffer, out document))
                {
                    var path = document.FilePath.Split('\\');
                    file = path[path.Length - 1];
                }
                format = format.Replace("$FILENAME", file);
            }
            if (format.Contains("$USERNAME"))
            {
                var username = Environment.UserName;
                format = format.Replace("$USERNAME", username);
            }

            if (format.Contains("$END"))
            {
                endPos = format.IndexOf("$END");
                format = format.Replace("$END", "");
            }
            else
            {
                endPos = -1;
            }

            return format;
        }

        private string GetLineEnding()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var currentILine = m_textView.TextSnapshot.GetLineFromPosition(
                m_textView.Caret.Position.BufferPosition.Position);
            string lineEnding = currentILine.GetLineBreakText();

            if (lineEnding.Length == 0)
            {
                TextSelection ts = m_dte.ActiveDocument.Selection as TextSelection;

                // Get current cursor position
                int start = ts.TopPoint.AbsoluteCharOffset,
                    end = ts.BottomPoint.AbsoluteCharOffset,
                    startLine = ts.TopPoint.Line,
                    startColumn = ts.TopPoint.VirtualDisplayColumn,
                    currentLine = ts.ActivePoint.Line,
                    currentOffset = ts.ActivePoint.LineCharOffset;

                // Add a new line at the end of the line
                ts.EndOfLine();
                ts.NewLine();

                // Go back with the curser
                if (startLine == currentLine
                    && startColumn == currentOffset)
                {
                    ts.MoveToAbsoluteOffset(end);
                    ts.MoveToAbsoluteOffset(start, true);
                }
                else
                {
                    ts.MoveToAbsoluteOffset(start);
                    ts.MoveToAbsoluteOffset(end, true);
                }

                return GetLineEnding();
            }

            return lineEnding;
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
