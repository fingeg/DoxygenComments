using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Task = System.Threading.Tasks.Task;

namespace DoxygenComments
{
    enum DocLineType
    {
        _params,
        _return
    }

    /// <summary>
    /// The command handler for all doxygen comment updates (Entry in the tools menu)
    /// </summary>
    internal sealed class UpdateDoxygenCommentCmd
    {

        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("49c56c76-25c1-428b-8e1d-da7a1fa3fa4e");
        private readonly AsyncPackage package;
        private DTE2 m_dte;
        private SettingsHelper m_settings;
        private IWpfTextView m_textView;

        private UpdateDoxygenCommentCmd(AsyncPackage package, OleMenuCommandService commandService, DTE2 dte, IWpfTextView textView)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
            m_dte = dte;
            m_settings = new SettingsHelper();
            m_textView = textView;
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static UpdateDoxygenCommentCmd Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in UpdateDoxygenCommentCmd's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            // Get DTE for text editing
            DTE2 dte = (DTE2)await package.GetServiceAsync(typeof(DTE));

            // Get the text view for text reading
            var textManager = (IVsTextManager)await package.GetServiceAsync(typeof(SVsTextManager));
            var componentModel = (IComponentModel)await package.GetServiceAsync(typeof(SComponentModel));
            Assumes.Present(textManager);
            Assumes.Present(componentModel);
            var editor = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            textManager.GetActiveView(1, null, out IVsTextView textViewCurrent);
            var textView = editor.GetWpfTextView(textViewCurrent);

            // Get the command service
            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;

            // Initialize the command
            Instance = new UpdateDoxygenCommentCmd(package, commandService, dte, textView);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Get the current text properties
            TextSelection ts = m_dte.ActiveDocument.Selection as TextSelection;

            // Save current cursor position
            int start = ts.TopPoint.AbsoluteCharOffset,
                end = ts.BottomPoint.AbsoluteCharOffset,
                startLine = ts.TopPoint.Line,
                startColumn = ts.TopPoint.VirtualDisplayColumn;

            int line = ts.ActivePoint.Line;
            int offset = ts.ActivePoint.LineCharOffset;

            // Check if a function is edited and the comment has to be updated
            if (CheckIfInDocumentetFunction(ts, line, offset, out var function, out var functionLine))
            {
                string lineEnding = m_textView.TextSnapshot.GetLineFromPosition(m_textView.Caret.Position.BufferPosition.Position).GetLineBreakText();

                if (GetFunctionDoc(functionLine, lineEnding, out var oldDoc, out var docLine))
                {
                    // Update the comment
                    UpdateComment(function, oldDoc, docLine, ts, line, offset, lineEnding);
                }
            }

            // Go back with the curser
            try
            {
                if (startLine == line && startColumn == offset)
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
            catch { }
        }

        private bool CheckIfInDocumentetFunction(TextSelection ts, int currentLine, int currentOffset, out CodeFunction codeFunction, out int functionLine)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Get comment beginnings
            string commentFormat = m_settings.FunctionFormat;
            string[] formatLines = commentFormat.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string commentStart = formatLines.First().Trim().Split(' ').First().Trim();
            string commentMiddle = formatLines[1].Trim().Split(' ').First().Trim();
            string commentEnd = formatLines.Last().Trim().Split(' ').First().Trim();

            // Check if in comment
            bool isComment = false;
            functionLine = currentLine;
            string line = m_textView.TextSnapshot.GetLineFromPosition(m_textView.Caret.Position.BufferPosition.Position).GetText().Trim();
            if (line.StartsWith(commentStart) || line.StartsWith(commentMiddle) || line.StartsWith(commentEnd))
            {
                while (!ts.ActivePoint.AtEndOfDocument)
                {
                    // Get the line of the curser
                    string _line = m_textView.TextSnapshot.GetLineFromLineNumber(ts.ActivePoint.Line - 1).GetText();

                    // Check if the comment ends
                    if (_line.Contains(commentEnd))
                    {
                        functionLine = ts.ActivePoint.Line + 1;
                        isComment = true;
                        break;
                    }

                    // If the end of the comment was not there, go one line down
                    ts.LineDown();
                }

                // If there is a comment start, but no correct ending, this is not a doxygen documentation
                if (functionLine == currentLine)
                {
                    codeFunction = null;
                    return false;
                }
            }

            codeFunction = null;
            CodeElement codeElement = null;
            FileCodeModel fcm = m_dte.ActiveDocument.ProjectItem.FileCodeModel;
            if (fcm != null)
            {
                // Move to end of the current line
                ts.MoveToLineAndOffset(functionLine, 1);
                ts.EndOfLine();

                // Check if there is a function
                for (int lineNumber = functionLine; lineNumber <= functionLine + 3; lineNumber++)
                {
                    codeElement = fcm.CodeElementFromPoint(ts.ActivePoint, vsCMElement.vsCMElementFunction);

                    if (codeElement != null && codeElement.Kind == vsCMElement.vsCMElementFunction)
                    {
                        functionLine = lineNumber;
                        break;
                    }

                    // Only search in the next line if the cursor was in the documentation
                    if (!isComment)
                    {
                        break;
                    }

                    string _line = m_textView.TextSnapshot.GetLineFromLineNumber(lineNumber - 1).GetText().Trim();
                    // If there was an empty line, check next one
                    if (_line.Length == 0)
                    {
                        ts.LineDown();
                        ts.EndOfLine();
                    }
                    // Otherwise the comment is not for a function
                    else
                    {
                        return false;
                    }
                }
            }

            bool isFunction = codeElement != null && codeElement.Kind == vsCMElement.vsCMElementFunction;
            if (isFunction)
            {
                codeFunction = codeElement as CodeFunction;

                return codeFunction != null;
            }

            // Return if the line containes a function or not
            return isFunction;
        }

        private bool GetFunctionDoc(int currentLine, string lineEnding, out string doc, out int docLine)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string functionFormat = m_settings.FunctionFormat.Trim();
            string openChars = functionFormat.Substring(0, 3).Trim();
            string closeChars = functionFormat.Substring(functionFormat.Length - 3, 3).Trim();

            bool isComment = false;
            bool finishedComment = false;
            doc = null;
            docLine = -1;

            // Check the three lines above the function if there is a comment close statment
            for (int i = 2; i <= 4; i++)
            {

                // Get the text to the line number
                string line = m_textView.TextSnapshot.GetLineFromLineNumber(currentLine - i).GetText();

                // Check if there is a comment close statement
                if (line.Contains(closeChars))
                {
                    isComment = true;
                    doc = "";

                    // Add all lines above until the comment ends (The start segment is found) 
                    // or the top of the file is reached (But normally there should always be an open segment)
                    for (int j = currentLine - i; j >= 0; j--)
                    {
                        line = m_textView.TextSnapshot.GetLineFromLineNumber(j).GetText();
                        doc = line + lineEnding + doc;
                        if (line.Contains(openChars))
                        {
                            finishedComment = true;
                            docLine = j;
                            break;
                        }
                    }

                    break;
                }
            }

            // If there is a comment, check if it is a doxygen documentation
            if (isComment && finishedComment && docLine >= 0)
            {
                return true;
            }

            // There is no comment above the function
            return false;
        }

        private Dictionary<string, string> GetDocLinesOfType(string lineEnding, string doc, DocLineType lineType)
        {
            var descriptions = new Dictionary<string, string>();

            string functionFormat = m_settings.FunctionFormat;

            // Check if the format containes parameters or returns
            var variable = lineType.Equals(DocLineType._params) ? "PARAMS" : "RETURN";
            Match match = Regex.Match(functionFormat, @".*\$" + variable);
            if (match != null && match.Success)
            {
                // Get the line start for the parameter or return lines
                var paramKey = match.Value.Split(new[] { "$" + variable }, StringSplitOptions.RemoveEmptyEntries).First().Trim();

                // Find all lines starting with the parameter or return line start
                var lines = doc.Split(new[] { lineEnding }, StringSplitOptions.None);
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line.Trim().StartsWith(paramKey))
                    {
                        // Find the name that $PARAMS or $RETURN was replaced with
                        string variableName = "return";
                        if (lineType.Equals(DocLineType._params))
                        {
                            string lineEnd = line.Trim().Substring(paramKey.Length).Trim();
                            variableName = lineEnd.Split(' ').First().Trim();
                        }

                        // Get the whole description to this attribute (Check the possibility of multi-line description)
                        string description = line;
                        string attributeKey = Regex.Replace(paramKey, @"[a-z].*", "", RegexOptions.IgnoreCase).Trim();
                        for (int j = i + 1; j < lines.Length; j++)
                        {
                            var _line = lines[j];
                            if (_line.Trim().StartsWith(attributeKey))
                            {
                                break;
                            }
                            else if (Regex.Replace(_line, @"[^a-z]", "", RegexOptions.IgnoreCase).Trim().Length == 0)
                            {
                                break;
                            }
                            description += lineEnding + _line;
                        }

                        // If the name does not exists yet, add the line to the dictionary
                        if (!descriptions.ContainsKey(variableName))
                        {
                            descriptions.Add(variableName, description);
                        }
                    }
                }

                // Add the default format value for new entries
                descriptions.Add("__default__", match.Value.Split(new[] { "$" + variable }, StringSplitOptions.RemoveEmptyEntries).First());
            }

            return descriptions;
        }

        private string[] GetFunctionParams(CodeFunction codeFunction)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var functionParams = new List<string>();

            foreach (CodeElement child in codeFunction.Children)
            {
                CodeParameter parameter = child as CodeParameter;
                if (parameter != null)
                {
                    functionParams.Add(parameter.Name);
                }
            }

            return functionParams.ToArray();
        }

        private void UpdateComment(CodeFunction function, string oldDoc, int docLine, TextSelection ts, int oldLine, int offset, string lineEnding)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Get the old parameters and return valus
            var _oldParameters = GetDocLinesOfType(lineEnding, oldDoc, DocLineType._params);
            var oldParameters = _oldParameters.Keys.Except(new[] { "__default__" }).ToList();
            var oldReturns = GetDocLinesOfType(lineEnding, oldDoc, DocLineType._return);
            string oldReturn = null;
            if (oldReturns.ContainsKey("return"))
            {
                oldReturn = oldReturns["return"];
            }

            // Get all new function parameters and return value
            var updatedParams = GetFunctionParams(function);
            var updatedType = function.Type.AsString;

            // Create new comment step by step...
            string newDoc = oldDoc;

            // Remove all old parameters
            foreach (var param in oldParameters)
            {
                // Delete the line(s)
                var pattern = Regex.Escape(_oldParameters[param]) + "( .*" + lineEnding + "|" + lineEnding + ")";
                newDoc = Regex.Replace(newDoc, pattern, "");
            }

            // Remove old return statement
            if (oldReturn != null)
            {
                // Delete the line(s)
                var pattern = Regex.Escape(oldReturns["return"]) + "( .*" + lineEnding + "|" + lineEnding + ")";
                newDoc = Regex.Replace(newDoc, pattern, "");
            }

            // Then add all parameters of the new function
            var _oldParams = oldParameters.ToList();
            var _uncertainNewParams = new List<string>();
            var newParams = new Dictionary<string, string>();
            foreach (var param in updatedParams)
            {
                // If there was the exact same parameter, add it again
                if (_oldParams.Contains(param))
                {
                    _oldParams.Remove(param);
                    newParams.Add(param, _oldParameters[param]);
                }
                else
                {
                    newParams.Add(param, null);
                    _uncertainNewParams.Add(param);
                }
            }

            // Check for special cases
            if (_uncertainNewParams.Count > 0 && _oldParams.Count == 0)
            {
                _uncertainNewParams.Clear();
            }
            // If there are no special cases, but still open issues, ask the user
            else if (_uncertainNewParams.Count > 0)
            {
                var dialog = new UpdateDoxygenCommentDialog(_oldParams, _uncertainNewParams);

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    // Add the selected parameters to the new parameter list
                    var selectedParams = dialog.finalParams;
                    foreach (var param in _uncertainNewParams)
                    {
                        var oldParam = selectedParams[param];
                        if (oldParam != null)
                        {
                            newParams[param] = _oldParameters[oldParam].Replace(oldParam, param);
                        }
                    }
                }
            }

            // Add the new params to the documentation
            foreach (var param in newParams)
            {
                newDoc = AddParamToDoc(lineEnding, newDoc, param.Key, param.Value, _oldParameters);
            }

            // Then add the return statement
            if (!updatedType.Equals("void"))
            {
                newDoc = AddReturnToDoc(lineEnding, newDoc, oldReturn, oldReturns);
            }

            var oldDocLines = oldDoc.Split('\n');
            var lineDiff = newDoc.Split('\n').Length - oldDocLines.Length;

            ts.MoveToLineAndOffset(docLine + 1, 1);
            ts.MoveToLineAndOffset(docLine + oldDocLines.Length - 1, 1, true);
            ts.EndOfLine(true);
            ts.Insert(newDoc.TrimSuffix(lineEnding));
            ts.MoveToLineAndOffset(oldLine + lineDiff, offset);

        }

        private string AddParamToDoc(string lineEnding, string doc, string paramName, string paramDescription, Dictionary<string, string> _oldParameters)
        {
            if (_oldParameters.ContainsKey("__default__"))
            {
                //TODO: Add at correct position
                var lines = doc.Split(new[] { lineEnding }, StringSplitOptions.RemoveEmptyEntries).ToList();
                var spacing = lines[0].Length - lines[0].TrimStart().Length;
                if (paramDescription == null)
                {
                    lines.Insert(lines.Count() - 1, lines[0].Substring(0, spacing) + _oldParameters["__default__"] + paramName);
                }
                else
                {
                    lines.Insert(lines.Count() - 1, paramDescription);
                }
                return string.Join(lineEnding, lines) + lineEnding;
            }
            return doc;
        }

        private string AddReturnToDoc(string lineEnding, string doc, string returnDescription, Dictionary<string, string> _oldReturns)
        {
            if (_oldReturns.ContainsKey("__default__"))
            {
                //TODO: Add at correct position
                var lines = doc.Split(new[] { lineEnding }, StringSplitOptions.RemoveEmptyEntries).ToList();
                var spacing = lines[0].Length - lines[0].TrimStart().Length;
                if (returnDescription == null)
                {
                    lines.Insert(lines.Count() - 1, lines[0].Substring(0, spacing) + _oldReturns["__default__"]);
                }
                else
                {
                    lines.Insert(lines.Count() - 1, returnDescription);
                }
                return string.Join(lineEnding, lines) + lineEnding;
            }
            return doc;
        }
    }
}
