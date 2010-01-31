using System;
using System.Windows.Forms;

namespace AionParsePlugin
{
    public partial class AionParseForm : UserControl
    {
        private AionParse plugin;

        public AionParseForm(AionParse plugin, string lastCharName)
        {
            InitializeComponent();
            this.plugin = plugin;
            TextboxDefaultCharacter.Text = lastCharName;
            plugin.SetDebugParse(CheckboxDebugParse.Checked);
            plugin.SetGuessDotCasters(CheckboxGuessDoTCasters.Checked);
            plugin.SetLinkPets(CheckboxLinkPets.Checked);
            plugin.SetTagBlockedAttacks(CheckboxTagBlockedAttacks.Checked);
        }

        public void AddText(string text)
        {
            TextboxLog.Text += text + Environment.NewLine;
        }

        private void AionParseForm_Load(object sender, EventArgs e)
        {
            string toolTip1Msg = 
                "When checked, the parser will attempt to tag attacks after a block as special:blocked";

            string toolTip2Msg = 
                "When checked, the parser will store who cast skills that cause DoT or damage after a delay, \n" +
                "as to associate the later damage to the caster. This is also used to track healing done by Word of Life.\n" +
                "(The limitation is that this really muddles the damage if you have casters casting the same DoTs\n" +
                " on different mobs with the same name; whoever casted second gets attributed all the damage.)";

            string toolTip3Msg =
                "When checked, skills and melee attacks from Spirits (ex-Fire Spirit) will be listed under the Spiritmaster instead. (Default: unchecked)";

            string toolTip4Msg =
                "When checked, damage done to known temporary pets (i.e. Holy Servants) and unknown pets (i.e. monster's trap summons) will be recorded. (Default: unchecked)";
         
            toolTip0.SetToolTip(CheckboxDebugParse, "Show developer debug messages in log.");
            toolTip1.SetToolTip(CheckboxTagBlockedAttacks, toolTip1Msg);
            toolTip2.SetToolTip(CheckboxGuessDoTCasters, toolTip2Msg);
            toolTip3.SetToolTip(CheckboxLinkPets, toolTip3Msg);
            toolTip4.SetToolTip(CheckboxParseDmgToTmpPets, toolTip4Msg);
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