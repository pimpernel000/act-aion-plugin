using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace AionParse_Plugin
{
    public partial class AionParseForm : UserControl
	{
        AionParse plugin;

		public AionParseForm(AionParse plugin)
		{
			InitializeComponent();
            this.plugin = plugin;
		}

        public void AddText(string text)
        {
            TextboxLog.Text += text + Environment.NewLine;
        }

        private void ApplyDefaultCharacter_Click(object sender, EventArgs e)
        {
            plugin.SetCharName(TextboxDefaultCharacter.Text);
            AddText("Default character changed to " + TextboxDefaultCharacter.Text);
        }

        internal void InitFromPlugin(string lastCharName)
        {
            TextboxDefaultCharacter.Text = lastCharName;
        }

        private void TextboxDefaultCharacter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ApplyDefaultCharacter_Click(sender, e);
            }
        }
    }
}
