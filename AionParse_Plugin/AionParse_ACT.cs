using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Advanced_Combat_Tracker;

namespace AionParsePlugin
{
    public partial class AionParse : IActPluginV1
    {
        #region private members
        // for Robe of Ice damage reflect
        string lastActivatedSkill = string.Empty;
        int lastActivatedSkillGlobalTime = -1;
        DateTime lastActivedSkillTime = DateTime.MinValue;

        // for using potions by you
        string lastPotion;
        int lastPotionGlobalTime = -1;
        DateTime lastPotionTime = DateTime.MinValue;

        // remembering who cast DoTs
        internal UsingSkillRecordSetBase ContinuousDamageSet { get; set; }

        // remembering who cast HoTs
        internal UsingSkillRecordSetBase HealerRecordSet { get; set; }

        // remembering who who got blocked
        internal BlockedSet BlockedHistory { get; set; }

        // remembering summoners
        internal UsingSkillRecordSetBase SummonerRecordSet { get; set; }

        // ui variables (initial values reset by UI on init)
        AionParseForm ui;

        internal string LastCharName { get; set; }

        internal bool GuessDotCasters { get; set; }

        internal bool DebugParse { get; set; } // for debugging purposes, causes all messages to be shown in log that aren't caught by parser

        internal bool TagBlockedAttacks { get; set; }

        internal bool LinkPets { get; set; } // TODO: link pets with their summoners for damage totalling; maybe label all pet skills as "Pet Skill (petname)" and name pet melee as "Melee (petname)"
        
        internal bool LinkBOFtoSM { get; set; }

        internal bool LinkDmgProcs { get; set; }

        #endregion

        public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
        {
            ActGlobals.oFormActMain.SetParserToNull();
            ActGlobals.oFormActMain.LogFileFilter = "Chat*.log";
            ActGlobals.oFormActMain.LogPathHasCharName = false;
            ActGlobals.oFormActMain.ResetCheckLogs();
            ActGlobals.oFormActMain.TimeStampLen = 0x16;
            ActGlobals.oFormActMain.GetDateTimeFromLog = new FormActMain.DateTimeLogParser(ParseDateTime);
            ActGlobals.oFormActMain.ZoneChangeRegex = new Regex(@"[\d :\.]{22}You have joined the (?<channel>.+?) region channel. ", RegexOptions.Compiled);
            ActGlobals.oFormActMain.BeforeLogLineRead += new LogLineEventDelegate(this.BeforeLogLineRead);
            ActGlobals.oFormActMain.OnCombatEnd += new CombatToggleEventDelegate(this.OnCombatEnd);

            // private variables that must be set before UI
            ContinuousDamageSet = new UsingSkillRecordSetBase()
            {
                new UsingSkillRecord("Unknown (Templar)", "Slash Artery"),
                new UsingSkillRecord("Unknown (Assassin)", "Apply Poison"),
                new UsingSkillRecord("Unknown (Assassin)", "Poison Slash"),
                new UsingSkillRecord("Unknown (Ranger)", "Poison Arrow"),
                new UsingSkillRecord("Unknown (Ranger)", "Poisoning Trap"),
                new UsingSkillRecord("Unknown (Priest)", "Promise of Wind I"),
                new UsingSkillRecord("Unknown (Chanter)", "Promise of Wind"),
                new UsingSkillRecord("Unknown (Assassin)", "Blood Rune"),
                new UsingSkillRecord("Unknown (Sorcerer)", "Wind Cut Down"),
                new UsingSkillRecord("Unknown (Sorcerer)", "Delayed Blast"),
                new UsingSkillRecord("Unknown (Sorcerer)", "Flame Cage"),
                new UsingSkillRecord("Unknown (Mage)", "Erosion I"),
                new UsingSkillRecord("Unknown (Spiritmaster)", "Erosion"),
                new UsingSkillRecord("Unknown (Spiritmaster)", "Chain of Earth"),
                new UsingSkillRecord("Unknown (Spiritmaster)", "Blessing of Fire"),
                new UsingSkillRecord("Unknown (Spiritmaster)", "Sandblaster")
            };

            HealerRecordSet = new UsingSkillRecordSetBase() 
            {
                new UsingSkillRecord("Unknown (Priest)", "Healing Light"),
                new UsingSkillRecord("Unknown (Cleric)", "Radiant Cure"),
                new UsingSkillRecord("Unknown (Cleric)", "Healing Wind"),
                new UsingSkillRecord("Unknown (Cleric)", "Splendor of Recovery"),
                new UsingSkillRecord("Unknown (Cleric)", "Flash of Recovery"),
                new UsingSkillRecord("Unknown (Cleric)", "Light of Recovery"),
                new UsingSkillRecord("Unknown (Cleric)", "Light of Rejuvenation"),
                new UsingSkillRecord("Unknown (Chanter)", "Promise of Aether"),
                new UsingSkillRecord("Unknown (Chanter)", "Revival Mantra"),
                new UsingSkillRecord("Unknown (Chanter)", "Word of Life"),
                new UsingSkillRecord("Unknown (Chanter)", "Word of Revival"),
                new UsingSkillRecord("Unknown (Chanter)", "Healing Conduit"),
                new UsingSkillRecord("Unknown (Chanter)", "Recovery Spell"),
                new UsingSkillRecord("Unknown (Chanter)", "Clement Mind Mantra"),
                new UsingSkillRecord("Unknown (Chanter)", "Invincibility Mantra"),
                new UsingSkillRecord("Unknown (Chanter)", "Magic Recovery")

            };

            BlockedHistory = new BlockedSet();
            SummonerRecordSet = new UsingSkillRecordSetBase();

            // UI initialization
            ui = new AionParseForm(this, ActGlobals.charName);
            pluginScreenSpace.Controls.Add(ui);
            ui.Dock = DockStyle.Fill;
        }

        public void DeInitPlugin()
        {
            ActGlobals.oFormActMain.BeforeLogLineRead -= new LogLineEventDelegate(this.BeforeLogLineRead);
            ActGlobals.oFormActMain.OnCombatEnd -= new CombatToggleEventDelegate(this.OnCombatEnd);
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            DeInitPlugin();
            ui.Dispose();
        }

        #endregion
    }
}