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

        private void AionParseForm_Load(object sender, EventArgs e)
        {
            toolTip1.SetToolTip(CheckboxTagBlockedAttacks,
                "When checked, the parser will attempt to tag attacks after a block as special:blocked");
            toolTip2.SetToolTip(CheckboxGuessDoTCasters,
                "When checked, the parser will store who cast skills that cause DoT or damage after a delay, \nas to associate the later damage to the caster.");
        }

        public void AddText(string text)
        {
            TextboxLog.Text += text + Environment.NewLine;
        }

        internal void InitFromPlugin(string lastCharName, bool guessDotCasters, bool debugParse, bool tagBlockedAttacks)
        {
            TextboxDefaultCharacter.Text = lastCharName;
            CheckboxGuessDoTCasters.Checked = guessDotCasters;
            CheckboxDebugParse.Checked = debugParse;
            CheckboxTagBlockedAttacks.Checked = tagBlockedAttacks;
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

        private void CheckboxTagBlockedAttacks_CheckedChanged(object sender, EventArgs e)
        {
            plugin.SetTagBlockedAttacks(CheckboxTagBlockedAttacks.Checked);
        }

        private void CheckboxLinkPets_CheckedChanged(object sender, EventArgs e)
        {
            plugin.SetLinkPets(CheckboxLinkPets.Checked);
        }
    }
}
