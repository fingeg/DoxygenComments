namespace DoxygenComments
{
    partial class DoxygenToolsOptionsControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DoxygenToolsOptionsControl));
            this.richTextInfo = new System.Windows.Forms.RichTextBox();
            this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.richTextInput = new System.Windows.Forms.RichTextBox();
            this.btn_reset = new System.Windows.Forms.Button();
            this.tableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // richTextInfo
            // 
            this.richTextInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextInfo.Location = new System.Drawing.Point(2, 2);
            this.richTextInfo.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.richTextInfo.Name = "richTextInfo";
            this.richTextInfo.ReadOnly = true;
            this.richTextInfo.ShortcutsEnabled = false;
            this.richTextInfo.Size = new System.Drawing.Size(693, 146);
            this.richTextInfo.TabIndex = 2;
            this.richTextInfo.Text = resources.GetString("richTextInfo.Text");
            // 
            // tableLayoutPanel
            // 
            this.tableLayoutPanel.ColumnCount = 1;
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel.Controls.Add(this.richTextInput, 0, 1);
            this.tableLayoutPanel.Controls.Add(this.richTextInfo, 0, 0);
            this.tableLayoutPanel.Controls.Add(this.btn_reset, 0, 1);
            this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            this.tableLayoutPanel.RowCount = 1;
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 16F));
            this.tableLayoutPanel.Size = new System.Drawing.Size(697, 487);
            this.tableLayoutPanel.TabIndex = 3;
            // 
            // richTextInput
            // 
            this.richTextInput.AcceptsTab = true;
            this.richTextInput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextInput.DetectUrls = false;
            this.richTextInput.Location = new System.Drawing.Point(2, 185);
            this.richTextInput.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.richTextInput.Name = "richTextInput";
            this.richTextInput.Size = new System.Drawing.Size(693, 300);
            this.richTextInput.TabIndex = 4;
            this.richTextInput.Text = "";
            this.richTextInput.WordWrap = false;
            this.richTextInput.TextChanged += new System.EventHandler(this.richTextInput_TextChanged);
            this.richTextInput.KeyDown += new System.Windows.Forms.KeyEventHandler(this.richTextInput_KeyDown);
            this.richTextInput.Leave += new System.EventHandler(this.richTextInput_Leave);
            // 
            // btn_reset
            // 
            this.btn_reset.Location = new System.Drawing.Point(2, 152);
            this.btn_reset.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btn_reset.Name = "btn_reset";
            this.btn_reset.Size = new System.Drawing.Size(56, 29);
            this.btn_reset.TabIndex = 3;
            this.btn_reset.Text = "Reset";
            this.btn_reset.UseVisualStyleBackColor = true;
            this.btn_reset.Click += new System.EventHandler(this.btn_reset_Click);
            // 
            // DoxygenToolsOptionsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "DoxygenToolsOptionsControl";
            this.Size = new System.Drawing.Size(697, 487);
            this.tableLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox richTextInfo;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
        private System.Windows.Forms.RichTextBox richTextInput;
        private System.Windows.Forms.Button btn_reset;
    }
}
