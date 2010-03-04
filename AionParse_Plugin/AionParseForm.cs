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
            System.Diagnostics.Process.Start("http://www.eq2flames.com/plugin-discussion/53384-aion-parse-plugin-act-v-0-1-0-3-a-9.html");
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.linkLabel2.LinkVisited = true;
            System.Diagnostics.Process.Start("http://code.google.com/p/act-aion-plugin/wiki/InstallationNotes");
        }
    }
}