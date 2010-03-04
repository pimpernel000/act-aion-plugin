using System;
using System.Windows.Forms;

namespace AionParsePlugin
{
    public partial class AionParseForm : UserControl
    {
        private AionParse plugin;

        public AionParseForm(AionParse plugin, string defaultCharName)
        {
            InitializeComponent();

            ToggleAdvancedTabs(CheckboxAdvancedToggle.Checked);

            this.plugin = plugin;
            if (String.IsNullOrEmpty(TextboxDefaultCharacter.Text) || TextboxDefaultCharacter.Text == "YOU")
            {
                TextboxDefaultCharacter.Text = defaultCharName;
            }

            plugin.LastCharName = TextboxDefaultCharacter.Text;
            AddText("Plugin Initialized with current character as " + TextboxDefaultCharacter.Text + ".");

            this.dgGainSpells.DataSource = plugin.HealerRecordSet;
            this.dgDamageSpells.DataSource = plugin.ContinuousDamageSet;
            this.dgParty.DataSource = plugin.PartyMembers;
        }

        public void AddText(string text)
        {
            TextboxLog.Text += text + Environment.NewLine;
        }

        internal void UpdateDefaultCharacter(string newGuy)
        {
            string oldGuy = plugin.LastCharName;
            plugin.PartyMembers.Replace(oldGuy, newGuy);
            plugin.LastCharName = newGuy;
            TextboxDefaultCharacter.Text = newGuy;
            AddText("Default character changed from " + oldGuy + " to " + newGuy);
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

            string toolTip5Msg = 
                "When checked, damage done by Blessing of Fire will be listed under the most recent Spiritmaster who casted it.";

            string toolTip6Msg = 
                "When checked, damage done by Promise of Wind, Apply Poison, and Apply Deadly Poison will be listed under the most recent person who casted it.";
         
            toolTip0.SetToolTip(CheckboxDebugParse, "Show developer debug messages in log. This will slow down the parser.");
            toolTip1.SetToolTip(CheckboxTagBlockedAttacks, toolTip1Msg);
            toolTip2.SetToolTip(CheckboxGuessDoTCasters, toolTip2Msg);
            toolTip3.SetToolTip(CheckboxLinkPets, toolTip3Msg);
            toolTip4.SetToolTip(CheckboxParseDmgToTmpPets, toolTip4Msg);
            toolTip5.SetToolTip(CheckboxLinkBOFtoSM, toolTip5Msg);
            toolTip6.SetToolTip(CheckboxLinkDamageProcs, toolTip6Msg);
        }

        private void ApplyDefaultCharacter_Click(object sender, EventArgs e)
        {
            UpdateDefaultCharacter(TextboxDefaultCharacter.Text);
        }

        private void TextboxDefaultCharacter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ApplyDefaultCharacter_Click(sender, e);
            }
        }

        private void CheckboxAdvancedToggle_CheckedChanged(object sender, EventArgs e)
        {
            ToggleAdvancedTabs(CheckboxAdvancedToggle.Checked);
        }

        private void ToggleAdvancedTabs(bool showTabs)
        {
            if (showTabs)
            {
                tabControl1.TabPages.Add(tabPartyInfo);
                tabControl1.TabPages.Add(tabGain);
                tabControl1.TabPages.Add(tabDamage);
            }
            else
            {
                tabControl1.TabPages.Remove(tabPartyInfo);
                tabControl1.TabPages.Remove(tabGain);
                tabControl1.TabPages.Remove(tabDamage);
            }
        }

        private void LinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Specify that the link was visited.
            this.linkLabel1.LinkVisited = true;

            // Navigate to a URL.
            System.Diagnostics.Process.Start("http://www.eq2flames.com/plugin-discussion/53384-aion-parse-plugin-act-v-0-1-0-3-a.html");
        }
    }
}