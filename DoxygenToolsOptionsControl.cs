using System;
using System.Drawing;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using EnvDTE;

namespace DoxygenComments
{
    public partial class DoxygenToolsOptionsControl : UserControl
    {
        public DoxygenToolsOptionsControl(DoxygenToolsOptionsBase optionsPage, DTE vsEnvironment)
        {
            // Check if on Ui thread
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            this.optionsPage = optionsPage;
            InitializeComponent();
               
            // Get the visual studio default font
            Properties propertiesList = vsEnvironment.get_Properties("FontsAndColors", "TextEditor");
            int fontSize = (short)propertiesList.Item("FontSize").Value;
            string fontFamily = (string) propertiesList.Item("FontFamily").Value;

            // Init input text
            richTextInput.Font = new Font(fontFamily, fontSize, FontStyle.Regular);
            richTextInput.ForeColor = Color.FromArgb(87, 166, 74);
            richTextInput.Text = optionsPage.Format;
            highlightVariables(richTextInput);

            // Set info text colors
            richTextInfo.Font = new Font(fontFamily, fontSize, FontStyle.Regular);
            richTextInfo.ForeColor = Color.Black;
            richTextInfo.Text = richTextInfo.Text.Replace(":", ":\n" + string.Join("\n" , optionsPage.additionalKeys));
            highlightVariables(richTextInfo);
        }

        internal DoxygenToolsOptionsBase optionsPage;

        private void richTextInput_Leave(object sender, EventArgs e)
        {
            optionsPage.Format = richTextInput.Text;
        }

        private void richTextInput_TextChanged(object sender, EventArgs e)
        {
            highlightVariables(richTextInput);
            HighlightShortcut();
            optionsPage.Format = richTextInput.Text;
        }

        bool isHighlighting = false;
        private void highlightVariables(RichTextBox richTextBox)
        {   
            // Only one highlight process at one time, because otherwise the selections will crash
            if (isHighlighting)
            {
                return;
            }
            isHighlighting = true;
            int curserPos = richTextBox.SelectionStart;

            // Search all words beginning with $
            MatchCollection matches = Regex.Matches(richTextBox.Text, @"\$\w*");
            if (matches != null && matches.Count > 0)
            {
                foreach (Match m in matches)
                {
                    richTextBox.Select(m.Index, m.Length);
                    richTextBox.SelectionColor = Color.Blue;
                    if (m.Value != m.Value.ToUpper())
                    {
                        richTextBox.SelectedText = m.Value.ToUpper();
                    }
                }
            }

            richTextBox.Select(curserPos, 0);
            richTextBox.SelectionColor = Color.FromArgb(87, 166, 74);
            isHighlighting = false;
        }

        private void HighlightShortcut()
        {
            if (isHighlighting)
            {
                return;
            }
            isHighlighting = true;
            int curserPos = richTextInput.SelectionStart;

            var spaceLeft = richTextInfo.Text.Length - richTextInfo.Text.TrimStart().Length;
            var length = richTextInfo.Text.Length < 3 ? richTextInfo.Text.Length : 3;
            richTextInput.Select(spaceLeft, length);
            richTextInput.SelectionColor = Color.Orange;

            richTextInput.Select(curserPos, 0);
            richTextInput.SelectionColor = Color.FromArgb(87, 166, 74);
            isHighlighting = false;
        }

        private void richTextInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                richTextInput.SelectedText = Environment.NewLine + " * ";
            }
        }

        private void btn_reset_Click(object sender, EventArgs e)
        {
            optionsPage.SetToDefault();
            richTextInput.Text = optionsPage.Format;
        }
    }
}
