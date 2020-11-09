using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace DoxygenComments
{
    public partial class UpdateDoxygenCommentDialog : Form
    {

        public Dictionary<string, string> finalParams { get; set; }
        private List<string> newParams;
        private List<string> oldParams;
        private ComboBox[] comboBoxes;

        public UpdateDoxygenCommentDialog(List<string> oldParams, List<string> newParams)
        {
            InitializeComponent();

            // Always show the dialog in the center of the parent form
            StartPosition = FormStartPosition.CenterParent;

            this.oldParams = oldParams;
            this.newParams = newParams;

            AddWidgets();
        }

        private void AddWidgets()
        {
            tableLayout.RowCount = newParams.Count();
            tableLayout.Controls.Clear();
            comboBoxes = new ComboBox[newParams.Count];

            var cbItems = new List<string>() { "New parameter" };
            cbItems.AddRange(oldParams);

            for (int i = 0; i < newParams.Count; i++)
            {
                var param = newParams[i];

                // Init the label
                var label = new Label
                {
                    Name = "label-" + i,
                    Text = param,
                    AutoSize = true
                };

                // Init the combo box
                var cb = new ComboBox
                {
                    Name = "cb-" + i,
                    TabIndex = i,
                    Dock = DockStyle.Fill,
                    DropDownStyle = ComboBoxStyle.DropDownList                
                };
                cb.Items.AddRange(cbItems.ToArray());
                cb.SelectedIndex = 0;
                comboBoxes[i] = cb;

                // Add both to the table
                tableLayout.Controls.Add(label, 0, i);
                tableLayout.Controls.Add(cb, 1, i);
            }
        }

        private void Btn_ok_Click(object sender, EventArgs e)
        {
            finalParams = new Dictionary<string, string>();
            var usedParams = new List<string>();
            for (int i = 0; i < newParams.Count(); i++)
            {
                var param = newParams[i];
                var cb = comboBoxes[i];
                string oldParam = null;
                if (cb.SelectedIndex > 0)
                {
                    oldParam = oldParams[cb.SelectedIndex - 1];
                }

                // Check if the parameter is not used multiple times
                if (oldParam != null)
                {
                    if (usedParams.Contains(oldParam))
                    {
                        MessageBox.Show("You can't use the same old parameter more than once!");
                        return;
                    }

                    usedParams.Add(oldParam);
                }

                finalParams.Add(param, oldParam);
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void Btn_cancel_Click(object sender, EventArgs e)
        {
            // Close the dialog with a cancel status
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
