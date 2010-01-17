using System;
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

        internal void InitFromPlugin(string lastCharName, bool guessDotCasters, bool debugParse)
        {
            TextboxDefaultCharacter.Text = lastCharName;
            CheckboxGuessDoTCasters.Checked = guessDotCasters;
            CheckboxDebugParse.Checked = debugParse;
        }

        private void ApplyDefaultCharacter_Click(object sender, EventArgs e)
        {
            plugin.SetCharName(TextboxDefaultCharacter.Text);
            AddText("Default character changed to " + TextboxDefaultCharacter.Text);
        }

        private void TextboxDefaultCharacter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ApplyDefaultCharacter_Click(sender, e);
            }
        }

        private void CheckboxGuessDoTCasters_CheckedChanged(object sender, EventArgs e)
        {
            plugin.SetGuessDotCasters(CheckboxGuessDoTCasters.Checked);
        }

        private void CheckboxDebugParse_CheckedChanged(object sender, EventArgs e)
        {
            plugin.SetDebugParse(CheckboxDebugParse.Checked);
        }
    }
}
