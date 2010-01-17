namespace AionParse_Plugin
{
    using System;
    using System.Drawing;
    using System.Globalization;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;
    using Advanced_Combat_Tracker;

    /* TODO
     * 
     * Spirits from Summoners
     *  The spirit used a skill on Iceghost Priest because Coszet used Spirit Thunderbolt Claw I.
     *  The spirit used a skill on Brutal Mist Mane Pawsoldier because Hexis used Spirit Erosion I.
     *  
     * Servants from Summoners
     *  Konata has summoned Water Energy to attack Brutal Mist Mane Grappler by using Summon Water Energy I. 
     *  Wind Servant inflicted 301 damage on Brutal Mist Mane Pawsoldier. (wind servant attacks 6 times over 10 secs)

     * Holy Spirit from Clerics
     *  You summoned Holy Servant by using Summon Holy Servant II to let it attack Pale Worg.
     *  Azshadela has summoned Holy Servant to attack Infiltrator by using Summon Holy Servant II. 
     *  Holy Servant inflicted 300 damage on Sergeant by using Summon Holy Servant II Effect. 
     * 
     * Resists to you
     *  Alpine Gorgon resisted Chastisement I.
     *  
     * Delayed Blast
     *  Hunter Arachna received the delayed explosion effect as you used Delayed Blast II. 
     *  Hunter Arachna received 1,118 damage due to the effect of Delayed Blast II. 
     *  
     * Cause
     *   Malemodel has caused you to summon Water Energy by using Summon Water Energy I.  (elyos summoning water servant on you in pvp)
     *   You received 97 damage from Water Energy. 
     *   Bondmetoo has caused you to summon Holy Servant by using Summon Holy Servant III.  (elyos summoning holy servant on you in pvp)
     *   You resisted Holy Servant's Summon Holy Servant III Effect. 
     *   Vyrana has caused Holy Servant to recover HP over time by using Light of Rejuvenation II. 
     *   Scout Captain caused you to bleed by using Area Cause Wound on you. 

    * NOTE: here's an example of battle data
    * > You received continuous damage because Black Blaze Spirit used Wing Ignition.
    * > Vyn inflicted 45 damage on you by using Wing Ignition. 
    *




     * 
     */

    public partial class AionParse : IActPluginV1
    {
        Regex rInflictDamageOnYou = new Regex(@"^(?<attacker>[a-zA-Z ]*) inflicted (?<damage>(\d+,)?\d+) damage and the rune carve effect on you by using (?<skill>[a-zA-Z \-']*)\.$", RegexOptions.Compiled);
        Regex rInflictDamage = new Regex(@"^(?<attacker>[a-zA-Z ]*?)( has)? inflicted (?<damage>(\d+,)?\d+) (?<critical>critical )?damage on (?<targetclause>[a-zA-Z \-']*)\.$", RegexOptions.Compiled);
        Regex rUsingAttack = new Regex(@"^(?<victimclause>[a-zA-Z ]*) by using (?<skill>[a-zA-Z \-']*)$", RegexOptions.Compiled);
        Regex rPatternEngraving = new Regex(@"^(?<victim>[a-zA-Z ]*) and caused the (?<special>[a-zA-Z ]*) effect$", RegexOptions.Compiled);
        Regex rIgniteAether = new Regex(@"^(?<victim>[a-zA-Z ]*) and dispelled some of its magical buffs by using (?<skill>[a-zA-Z \-']*)$", RegexOptions.Compiled); // I think only Ignite Aether spells has this line
        Regex rReflect = new Regex(@"^(?<victim>[a-zA-Z ]*) by reflecting the attack$", RegexOptions.Compiled);
        Regex rReceiveState = new Regex(@"^(?<victim>[a-zA-Z ]*) received the (?<special>[a-zA-Z ]*) effect as you used (?<skill>[a-zA-Z \-']*)\.$", RegexOptions.Compiled);
        Regex rReceiveDamage = new Regex(@"^(?<victim>[a-zA-Z ]*) received (?<damage>(\d+,)?\d+) damage from (?<attacker>[a-zA-Z ]*)\.$", RegexOptions.Compiled);
        Regex rStateAbility = new Regex(@"^(?<target>[a-zA-Z ]*) is in the (?<buff>[a-zA-Z ]*) state (because (?<actor>[a-zA-Z ]*)|as it) used (?<skill>[a-zA-Z \-']*)\.$", RegexOptions.Compiled);
        Regex rWeakened = new Regex(@"^(?<actor>[a-zA-Z ]*) has weakened (?<target>[a-zA-Z ]*)'s (?<stat>[a-zA-Z ]*) by using (?<skill>[a-zA-Z \-']*)\.$", RegexOptions.Compiled);
        Regex rActivated = new Regex(@"^(?<skill>[a-zA-Z \-']*) Effect has been activated\.$", RegexOptions.Compiled);
        Regex rContDmg1 = new Regex(@"^(?<actor>[a-zA-Z ]*) inflicted continuous damage on (?<target>[a-zA-Z ]*) by using (?<skill>[a-zA-Z \-']*)\.$", RegexOptions.Compiled);
        Regex rContDmg2 = new Regex(@"^(?<actor>[a-zA-Z ]*) used (?<skill>[a-zA-Z ']*) to inflict the continuous damage effect on (?<target>[a-zA-Z ]*)\.$", RegexOptions.Compiled);
        Regex rContDmg3 = new Regex(@"^(?<victim>[a-zA-Z ]*) received (?<damage>(\d+,)?\d+) (?<damagetype>[a-zA-Z ]*) damage after you used (?<skill>[a-zA-Z \-']*)\.$", RegexOptions.Compiled);
        Regex rReceivedContDmg = new Regex(@"^(?<victim>[a-zA-Z ]*) received (?<damage>(\d+,)?\d+) (?<damagetype>[a-zA-Z]* )?damage due to the effect of (?<skill>[a-zA-Z \-']*)\.$", RegexOptions.Compiled);
        Regex rReflectDamageOnYou = new Regex(@"^Your attack on (?<attacker>[a-zA-Z ]*) was reflected and inflicted (?<damagetype>[a-zA-Z ]*) damage on you\.$", RegexOptions.Compiled);
        Regex rRecoverMP = new Regex(@"^(?<target>[a-zA-Z ]*) recovered (?<mp>(\d+,)?\d+) MP (due to the effect of|by using|after using) (?<skill>[a-zA-Z \-']*?)( Effect)?\.$", RegexOptions.Compiled);
        Regex rRecoverHP = new Regex(@"^(?<target>[a-zA-Z ]*) recovered (?<hp>(\d+,)?\d+) HP (because (?<actor>[a-zA-Z ]*) used|by using) (?<skill>[a-zA-Z \-']*?)\.$", RegexOptions.Compiled);
        Regex rResist = new Regex(@"^(?<target>[a-zA-Z ]*) resisted ((?<actor>[a-zA-Z ]*)'s )?(?<skill>[a-zA-Z \-']*?)\.$", RegexOptions.Compiled);

        // for Robe of Ice damage reflect
        string lastActivatedSkill = "";
        int lastActivatedSkillGlobalTime = -1;
        DateTime lastActivedSkillTime = DateTime.MinValue;

        // for using potions by you
        string lastPotion;
        int lastPotionGlobalTime = -1;
        DateTime lastPotionTime = DateTime.MinValue;

        // remembering who cast DoTs
        ContinuousDamageSet continuousDamageSet = new ContinuousDamageSet();

        // remembering who who got blocked
        BlockedSet blockedHistory = new BlockedSet();

        string lastCharName = ActGlobals.charName;
        bool guessDotCasters = true;
        bool debugParse = true;
        bool tagBlockedAttacks = true;

        AionParseForm ui;

        System.Collections.Generic.List<string> extraDamageSkills; // list of skills that also contain DoT component or secondary payload damage later but cannot be found outside of rUsingAttack regex

        public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
        {
            ActGlobals.oFormActMain.SetParserToNull();
            ActGlobals.oFormActMain.LogFileFilter = "Chat*.log";
            ActGlobals.oFormActMain.LogPathHasCharName = false;
            ActGlobals.oFormActMain.ResetCheckLogs();
            ActGlobals.oFormActMain.TimeStampLen = 0x16;
            ActGlobals.oFormActMain.GetDateTimeFromLog = new FormActMain.DateTimeLogParser(this.ParseDateTime);
            ActGlobals.oFormActMain.ZoneChangeRegex = new Regex(@"[\d :\.]{22}You have joined the (?<channel>.+?) region channel. ", RegexOptions.Compiled);
            ActGlobals.oFormActMain.BeforeLogLineRead += new LogLineEventDelegate(this.oFormActMain_BeforeLogLineRead);
            ActGlobals.oFormActMain.OnCombatEnd += new CombatToggleEventDelegate(this.oFormActMain_OnCombatEnd);

            ui = new AionParseForm(this);
            pluginScreenSpace.Controls.Add(ui);
            ui.Dock = DockStyle.Fill;
            ui.AddText("Plugin Initialized with current character as " + lastCharName + ".");
            ui.InitFromPlugin(lastCharName, guessDotCasters, debugParse, tagBlockedAttacks);

            extraDamageSkills = new System.Collections.Generic.List<string> {
                "Blood Rune",
                "Wind Cut Down"
            };
        }

        public void DeInitPlugin()
        {
            ActGlobals.oFormActMain.BeforeLogLineRead -= new LogLineEventDelegate(this.oFormActMain_BeforeLogLineRead);
            ActGlobals.oFormActMain.OnCombatEnd -= new CombatToggleEventDelegate(this.oFormActMain_OnCombatEnd);
        }

        private void oFormActMain_OnCombatEnd(bool isImport, CombatToggleEventArgs encounterInfo)
        {
            lastActivatedSkill = "";
            lastActivatedSkillGlobalTime = -1;
            lastActivedSkillTime = DateTime.MinValue;

            lastPotion = "";
            lastPotionGlobalTime = -1;
            lastPotionTime = DateTime.MinValue;

            continuousDamageSet.Clear();
            blockedHistory.Clear();
        }

        private void oFormActMain_BeforeLogLineRead(bool isImport, LogLineEventArgs logInfo)
        {
            string str = logInfo.logLine.Substring(0x16, logInfo.logLine.Length - 0x16).Trim();
            string incName = string.Empty;
            string outName = string.Empty;
            string damage = string.Empty;
            string theAttackType = string.Empty;
            string special = string.Empty;
            bool critical = false;
            bool flag2 = false;

            #region misc parse
            // zone change
            if (ActGlobals.oFormActMain.ZoneChangeRegex.IsMatch(logInfo.logLine))
            {
                ActGlobals.oFormActMain.ChangeZone(ActGlobals.oFormActMain.ZoneChangeRegex.Replace(logInfo.logLine, "$1"));
                return;
            }

            // act commands
            if (str.StartsWith("/act ") && str.Length > 5)
            {
                string commandText = str.Substring(5);
                ActGlobals.oFormActMain.ActCommands(commandText);
                return;
            }

            // check for critical
            if (str.Contains("Critical Hit!"))
            {
                critical = true;
                str = str.Substring(14, str.Length - 14);
            }
            #endregion

            #region inflict damage parse

            // match "Your attack on xxx was reflected and inflicted xxx damage on you"
            if (rReflectDamageOnYou.IsMatch(str))
            {
                Match match = rReflectDamageOnYou.Match(str);
                incName = CheckYou("you");
                outName = match.Groups["attacker"].Value;
                damage = match.Groups["damage"].Value;
                if (blockedHistory.IsBlocked(outName, incName, logInfo.detectedTime)) special = "blocked&";
                special += "reflected";
                // assume: the attack that caused the reflection is recorded on it's own line so we don't have to log an unknown attack
                AddCombatAction(logInfo, outName, incName, "Damage Shield", critical, special, damage, SwingTypeEnum.NonMelee);
                return;
            }

            // match "xxx inflicted xxx damage on xxx ..."
            var mInflict = rInflictDamage.Match(str);
            if (mInflict.Success)
            {
                if (mInflict.Groups["critical"].Success)
                {
                    critical = true;
                }

                outName = CheckYou(mInflict.Groups["attacker"].Value); // source
                damage = mInflict.Groups["damage"].Value; // dmg
    
                // submatch "using ability"
                string targetClause = mInflict.Groups["targetclause"].Value; // target & extra info
                if (rUsingAttack.IsMatch(targetClause))
                {
                    var mUsingAttack = rUsingAttack.Match(targetClause);

                    // sub-submatch Assassin rune carving
                    var mPatternEngraving = rPatternEngraving.Match(mUsingAttack.Groups["victimclause"].Value);
                    if (mPatternEngraving.Success)
                    {
                        incName = CheckYou(mPatternEngraving.Groups["victim"].Value);
                        //special = mPatternEngraving.Groups["special"].Value;
                    }
                    else
                    {
                        incName = CheckYou(mUsingAttack.Groups["victimclause"].Value);
                    }

                    if (blockedHistory.IsBlocked(outName, incName, logInfo.detectedTime)) special = "blocked";
                    theAttackType = mUsingAttack.Groups["skill"].Value;

                    // check if skill has an extra payload damage that can't be found other than in here
                    if (guessDotCasters)
                    {
                        foreach (string skill in extraDamageSkills)
                        {
                            if (theAttackType.StartsWith(skill))
                            {
                                continuousDamageSet.Add(outName, incName, theAttackType, logInfo.detectedTime); // record Blood Rune actor for when it deals payload damage later or when Wind Cut Down does bleeding damage later
                                break;
                            }
                        }
                    }

                    var inflictSwingType = SwingTypeEnum.NonMelee;

                    // correct the false damage that are actually group heals
                    if (incName == CheckYou("you") &&
                        (theAttackType.StartsWith("Healing Wind") || theAttackType.StartsWith("Light of Recovery") ||
                        theAttackType.StartsWith("Healing Light") || theAttackType.StartsWith("Radiant Cure") ||
                        theAttackType.StartsWith("Flash of Recovery")))
                    {
                        inflictSwingType = SwingTypeEnum.Healing;
                    }

                    AddCombatAction(logInfo, outName, incName, theAttackType, critical, special, damage, inflictSwingType);
                    return;
                }

                // submatch "and dispelled buffs using Ignite Aether"
                var mIgniteAether = rIgniteAether.Match(targetClause);
                if (mIgniteAether.Success)
                {
                    incName = CheckYou(mIgniteAether.Groups["victim"].Value);
                    if (blockedHistory.IsBlocked(outName, incName, logInfo.detectedTime)) special = "blocked";
                    theAttackType = mIgniteAether.Groups["skill"].Value;
                    AddCombatAction(logInfo, outName, incName, theAttackType, critical, string.Empty, Dnum.NoDamage, SwingTypeEnum.CureDispel);
                    AddCombatAction(logInfo, outName, incName, theAttackType, critical, special, damage, SwingTypeEnum.NonMelee);
                    return;
                }

                // submatch "reflecting the attack"
                var mReflect = rReflect.Match(targetClause);
                if (mReflect.Success)
                {
                    special = "reflected";
                    incName = CheckYou(mReflect.Groups["victim"].Value);
                    if (blockedHistory.IsBlocked(outName, incName, logInfo.detectedTime)) special = "blocked";

                    if (ActGlobals.oFormActMain.GlobalTimeSorter == lastActivatedSkillGlobalTime || (logInfo.detectedTime - lastActivedSkillTime).TotalSeconds < 2)
                    {
                        theAttackType = lastActivatedSkill;
                    }
                    else
                    {
                        theAttackType = "Damage Shield";
                    }

                    AddCombatAction(logInfo, outName, incName, theAttackType, critical, special, damage, SwingTypeEnum.NonMelee);
                    return;
                }


                // no ability submatch
                incName = CheckYou(targetClause);
                if (blockedHistory.IsBlocked(outName, incName, logInfo.detectedTime, false)) special = "blocked";  // block record consume set to false because auto-attacks can be multi-hitting, and multiple attacks can be blocked
                AddCombatAction(logInfo, outName, incName, "Melee", critical, special, damage, SwingTypeEnum.Melee);
                return;
            }

            // match "xxx inflicted xxx damage and the rune carve effect on you by using xxx ."  (assassin rune abilities on you in pvp)
            var mInflictDamageOnYou = rInflictDamageOnYou.Match(str);
            if (mInflictDamageOnYou.Success)
            {
                outName = mInflictDamageOnYou.Groups["attacker"].Value;
                incName = CheckYou("you");
                //special = "pattern engraving";
                damage = mInflictDamageOnYou.Groups["damage"].Value;
                theAttackType = mInflictDamageOnYou.Groups["skill"].Value;
                if (blockedHistory.IsBlocked(outName, incName, logInfo.detectedTime)) special = "blocked";
                AddCombatAction(logInfo, outName, incName, theAttackType, critical, special, damage, SwingTypeEnum.NonMelee);
                return;
            }

            #endregion

            #region indicator parsers
            // match "You have used xxx Potion."
            if (str.StartsWith("You have used") && str.EndsWith("Potion."))
            {
                Match match = (new Regex("You have used (?<potion>[a-zA-Z ]*).", RegexOptions.Compiled)).Match(str);
                lastPotion = match.Groups["potion"].Value;
                lastPotionGlobalTime = ActGlobals.oFormActMain.GlobalTimeSorter;
                lastPotionTime = logInfo.detectedTime;
                return;
            }

            // match "xxx has been activated." for use in damage shields like Robe of Cold
            if (rActivated.IsMatch(str))
            {
                if (!guessDotCasters) return;

                Match match = rActivated.Match(str);
                lastActivatedSkill = match.Groups["skill"].Value;
                lastActivatedSkillGlobalTime = ActGlobals.oFormActMain.GlobalTimeSorter;
                lastActivedSkillTime = logInfo.detectedTime;
                return;
            }

            // match "continuous damage"
            if (rContDmg1.IsMatch(str))
            {
                Match match = rContDmg1.Match(str);
                string actor = CheckYou(match.Groups["actor"].Value);
                string target = CheckYou(match.Groups["target"].Value);
                string skill = match.Groups["skill"].Value;
                if (guessDotCasters)
                    continuousDamageSet.Add(actor, target, skill, logInfo.detectedTime);
                AddCombatAction(logInfo, actor, target, skill, false, String.Empty, Dnum.NoDamage, SwingTypeEnum.NonMelee);
                return;
            }
            if (rContDmg2.IsMatch(str))
            {
                Match match = rContDmg2.Match(str);
                string actor = CheckYou(match.Groups["actor"].Value);
                string target = CheckYou(match.Groups["target"].Value);
                string skill = match.Groups["skill"].Value;
                if (guessDotCasters)
                    continuousDamageSet.Add(actor, target, skill, logInfo.detectedTime);
                AddCombatAction(logInfo, actor, target, skill, false, String.Empty, Dnum.NoDamage, SwingTypeEnum.NonMelee);
                return;
            }

            // match "xxx received the xxx effect as you used xxx"  occurs when you use Delayed Blast
            if (rReceiveState.IsMatch(str))
            {
                Match match = rReceiveState.Match(str);
                string actor = CheckYou("you");
                string target = match.Groups["victim"].Value;
                string skill = match.Groups["skill"].Value;
                if (!guessDotCasters)
                    continuousDamageSet.Add(actor, target, skill, logInfo.detectedTime);
                AddCombatAction(logInfo, actor, target, skill, critical, special, Dnum.NoDamage, SwingTypeEnum.NonMelee);
                return;
            }
            #endregion

            #region continuous/extra damage from specific skills
            // match "xxx received xxx damage due to the effect of xxx"
            if (rReceivedContDmg.IsMatch(str))
            {
                Match match = rReceivedContDmg.Match(str);
                incName = CheckYou(match.Groups["victim"].Value);
                damage = match.Groups["damage"].Value;
                theAttackType = match.Groups["skill"].Value;

                outName = continuousDamageSet.GetActor(incName, theAttackType, logInfo.detectedTime);
                if (String.IsNullOrEmpty(outName)) // skills like Promise of Wind or Blood Rune
                {
                    if (theAttackType.StartsWith("Promise of Wind"))
                    {
                        outName = "Unknown (Priest)";
                    }
                    else if (theAttackType.StartsWith("Blood Rune"))
                    {
                        outName = "Unknown (Assassin)";
                    }
                    else
                    {
                        outName = "Unknown";
                    }
                }

                if (blockedHistory.IsBlocked(outName, incName, logInfo.detectedTime)) special = "blocked";
                AddCombatAction(logInfo, outName, incName, theAttackType, critical, special, damage, SwingTypeEnum.NonMelee);
                return;
            }

            // match "xxx recieved xxx yyy damage after you used xxx" 
            if (rContDmg3.IsMatch(str))
            {
                Match match = rContDmg3.Match(str);
                incName = match.Groups["victim"].Value;
                damage = match.Groups["damage"].Value;
                string damageType = match.Groups["damagetype"].Value;
                theAttackType = match.Groups["skill"].Value; // only DoT skills: Poison, Poison Arrow, or Wind Cut Down skills match this... often mob skills
                outName = continuousDamageSet.GetActor(incName, theAttackType, logInfo.detectedTime);
                if (String.IsNullOrEmpty(outName))
                {
                    if (theAttackType.StartsWith("Wind Cut Down"))
                    {
                        outName = "Unknown (Sorcerer)";
                    }
                    else if (theAttackType.StartsWith("Slash Artery"))
                    {
                        outName = "Unknown (Templar)";
                    }
                    else if (theAttackType.StartsWith("Apply Poison") || theAttackType.StartsWith("Poison Slash")) // not sure, is Poison Slash an Assassin ability?!?
                    {
                        outName = "Unknown (Assassin)";
                    }
                    else if (theAttackType.StartsWith("Poison Arrow") || theAttackType.StartsWith("Poisoning Trap"))
                    {
                        outName = "Unknown (Ranger)";
                    }
                    else
                    {
                        outName = "Unknown"; // unknown class abilities are: Poison, Poison Slash (assassin?), Bleeding (spiritmaster?)
                    }
                }
                if (blockedHistory.IsBlocked(outName, incName, logInfo.detectedTime)) special = "blocked";
                AddCombatAction(logInfo, outName, incName, theAttackType, critical, special, damage, SwingTypeEnum.NonMelee, damageType);
                return;
            }
            #endregion

            #region melee attack
            // match "xxx received xxx damage from xxx."  (basic melee attack?)
            if (rReceiveDamage.IsMatch(str))
            {
                Match match = rReceiveDamage.Match(str);
                outName = match.Groups["attacker"].Value;
                incName = CheckYou(match.Groups["victim"].Value);
                damage = match.Groups["damage"].Value;
                if (blockedHistory.IsBlocked(outName, incName, logInfo.detectedTime)) special = "blocked";
                AddCombatAction(logInfo, outName, incName, "Melee", critical, "", damage, SwingTypeEnum.Melee);
                return;
            }
            #endregion

            #region hp/mp heals
            if (ActGlobals.oFormActMain.InCombat)
            {
                // match "You restored xx of xxx's HP by using xxx."  the actor in this case is ambigious and not really you.
                if (str.StartsWith("You restored"))
                {
                    Regex rYouRestoreHP = new Regex(@"You restored (?<hp>(\d+,)?\d+) of (?<target>[a-zA-Z ]*)'s HP by using (?<skill>[a-zA-Z \-']*?)\.");
                    Match match = rYouRestoreHP.Match(str);
                    if (!match.Success)
                    {
                        ui.AddText("Exception-Unable to parse[e2]: " + str);
                        return;
                    }
                    incName = match.Groups["target"].Value;
                    damage = match.Groups["hp"].Value;
                    theAttackType = match.Groups["skill"].Value;

                    if (theAttackType.StartsWith("Revival Mantra"))
                    {
                        outName = "Unknown (Chanter)"; // Revival Mantra is group heal; this does indeed show up if the chanter heals itself. TODO: confirm if chanter healing party with this spells shows up in logs the same way
                    }
                    else if (theAttackType.StartsWith("Blood Rune"))
                    {
                        outName = incName; // Blood Rune heals caster
                    }
                    else
                    {
                        outName = "Unknown";
                    }

                    AddCombatAction(logInfo, outName, incName, theAttackType, critical, special, damage, SwingTypeEnum.Healing);
                    return;
                }

                // match "xx restored xx HP."
                if (str.EndsWith(" HP.") && str.Contains("restored"))
                {
                    Regex rYouRestoreHP = new Regex(@"(?<actor>[a-zA-Z ]*) restored (?<hp>(\d+,)?\d+) HP\.");
                    Match match = rYouRestoreHP.Match(str);
                    if (!match.Success)
                    {
                        ui.AddText("Exception-Unable to parse[e3]: " + str);
                        return;
                    }
                    outName = match.Groups["actor"].Value;
                    incName = outName;
                    damage = match.Groups["hp"].Value;
                    theAttackType = "Unknown";
                    if (incName == CheckYou("you") && (ActGlobals.oFormActMain.GlobalTimeSorter == lastPotionGlobalTime || (logInfo.detectedTime - lastPotionTime).TotalSeconds < 2))
                    {
                        theAttackType = lastPotion;
                    }

                    AddCombatAction(logInfo, outName, incName, theAttackType, critical, special, damage, SwingTypeEnum.Healing);
                    return;
                }

                // match "xxx recovered xx HP ..."
                if (rRecoverHP.IsMatch(str))
                {
                    Match match = rRecoverHP.Match(str);
                    incName = CheckYou(match.Groups["target"].Value);
                    if (match.Groups["actor"].Success)
                    {
                        outName = CheckYou(match.Groups["actor"].Value);
                    }
                    else
                    {
                        outName = incName;
                    }
                    damage = match.Groups["hp"].Value;
                    theAttackType = match.Groups["skill"].Value;
                    AddCombatAction(logInfo, outName, incName, theAttackType, critical, special, damage, SwingTypeEnum.Healing);
                    return;
                }

                // match "xxx recovered x MP ..."
                if (rRecoverMP.IsMatch(str))
                {
                    Match match = rRecoverMP.Match(str);
                    incName = CheckYou(match.Groups["target"].Value);
                    damage = match.Groups["mp"].Value;
                    theAttackType = match.Groups["skill"].Value;
                    if (theAttackType.Contains("Clement Mind Mantra") || theAttackType.Contains("Invincibility Mantra") || theAttackType.StartsWith("Magic Recovery"))
                    {
                        outName = "Unknown (Chanter)"; // TODO: try to guess the chanter based on who casted the mantra
                    }
                    else
                    {
                        outName = incName; // almost any MP recovery spell/potion is self cast
                    }

                    AddCombatAction(logInfo, outName, incName, theAttackType, critical, special, damage, SwingTypeEnum.PowerHealing);
                    return;
                }

                // match "xxx restored x MP."
                if (str.EndsWith(" MP.") && str.Contains("restored"))
                {
                    Match match = (new Regex(@"^(?<actor>[a-zA-Z ]*) restored (?<mp>.*) MP\.$", RegexOptions.Compiled)).Match(str);
                    if (!match.Success)
                    {
                        ui.AddText("Exception-Unable to parse[e5]: " + str);
                        return;
                    }

                    incName = CheckYou(match.Groups["actor"].Value);
                    outName = incName; // assume: this log comes from a self action
                    damage = match.Groups["mp"].Value;
                    theAttackType = "Unknown";
                    if (incName == CheckYou("you") && (ActGlobals.oFormActMain.GlobalTimeSorter == lastPotionGlobalTime || (logInfo.detectedTime - lastPotionTime).TotalSeconds < 2))
                    {
                        theAttackType = lastPotion;
                    }

                    AddCombatAction(logInfo, outName, incName, theAttackType, critical, special, damage, SwingTypeEnum.PowerHealing);
                    return;
                }
            } // ignore heals/mp out of combat
            #endregion

            #region blocked
            if (str.Contains("blocked"))
            {
                // match "The attack was blocked by the xxx effect cast on xxx."  means your next attack has reduced dmg
                if (str.StartsWith("The attack was blocked by the "))
                {
                    if (tagBlockedAttacks)
                    {
                        Regex rBlockYou = new Regex(@"The attack was blocked by the (?<skill>[a-zA-Z \-']*?) effect cast on (?<target>[a-zA-Z ]*)\.", RegexOptions.Compiled);
                        Match match = rBlockYou.Match(str);
                        if (!match.Success)
                        {
                            ui.AddText("Exception-Unable to parse[e4]: " + str);
                            return;
                        }
                        incName = CheckYou(match.Groups["target"].Value);
                        //theAttackType = match.Groups["skill"].Value;
                        //AddCombatAction(logInfo, "Unknown", incName, theAttackType, critical, special, Dnum.NoDamage, SwingTypeEnum.Melee); // don't add action; this event occurs even on spells if they have armor up
                        blockedHistory.Add(CheckYou("you"), incName, logInfo.detectedTime);
                    }
                    return;
                }
                else
                {
                    // match "xxx blocked xxx's attack with the xxx effect."
                    Regex rBlock = new Regex(@"(?<victim>[a-zA-Z ]*) blocked (?<attacker>[a-zA-Z ]*)'s attack( with the (?<skill>[a-zA-Z \-']*?) effect)?\.");
                    Match match = rBlock.Match(str);
                    if (!match.Success)
                    {
                        ui.AddText("Exception-Unable to parse[e8]: " + str);
                        return;
                    }

                    if (tagBlockedAttacks)
                    {
                        incName = match.Groups["victim"].Value;
                        outName = match.Groups["attacker"].Value;
                        //theAttackType = match.Groups["skill"].Value;
                        blockedHistory.Add(outName, incName, logInfo.detectedTime);
                    }
                    return;
                }
            }
            #endregion

            #region parried
            else if ((str.IndexOf("parried") != -1) && (str.IndexOf("'s attack") != -1))
            {
                incName = str.Substring(0, str.IndexOf("parried") - 1);
                incName = this.CheckYou(incName);
                outName = str.Substring(str.IndexOf("parried") + 8, str.IndexOf("'s attack") - (str.IndexOf("parried") + 8));
                outName = this.CheckYou(outName);
                if (ActGlobals.oFormActMain.SetEncounter(logInfo.detectedTime, outName, incName))
                {
                    int num21;
                    ActGlobals.oFormActMain.GlobalTimeSorter = (num21 = ActGlobals.oFormActMain.GlobalTimeSorter) + 1;
                    ActGlobals.oFormActMain.AddCombatAction(1, critical, special, outName, "Melee", new Dnum((int)Dnum.Unknown, "parried"), logInfo.detectedTime, num21, incName, string.Empty);
                    if (flag2)
                    {
                        logInfo.detectedType = Color.Yellow.ToArgb();
                    }
                }
                return;
            }
            #endregion

            #region resisted
            else
                if (str.Contains("resisted"))
                {
                    if (rResist.IsMatch(str))
                    {
                        Match match = rResist.Match(str);

                        incName = CheckYou(match.Groups["target"].Value);
                        if (match.Groups["actor"].Success)
                        {
                            outName = CheckYou(match.Groups["actor"].Value);
                        }
                        else
                        {
                            outName = "Unknown";
                        }

                        if (match.Groups["skill"].Success)
                        {
                            theAttackType = match.Groups["skill"].Value;
                        }
                        else
                        {
                            theAttackType = "Unknown";
                        }

                        if (outName == "Aether" && theAttackType.StartsWith("Hold"))
                        {
                            theAttackType = "Aether's " + theAttackType;
                            outName = "Unknown (Sorcerer)";
                        }

                        AddCombatAction(logInfo, outName, incName, theAttackType, critical, special, Dnum.Resist, SwingTypeEnum.NonMelee);
                    }

                    else if (((str.IndexOf("resisted") != -1) && (str.IndexOf("'s ") != -1)) && (str.IndexOf("Effect.") != -1))
                    {
                        incName = str.Substring(0, str.IndexOf(" resisted "));
                        incName = this.CheckYou(incName);
                        outName = str.Substring(str.IndexOf(" resisted ") + 10, str.IndexOf("'s ") - (str.IndexOf("resisted") + 10));
                        outName = this.CheckYou(outName);
                        theAttackType = str.Substring(str.IndexOf("'s ") + 3, str.IndexOf(" Effect.") - (str.IndexOf("'s ") + 3));
                        if (ActGlobals.oFormActMain.SetEncounter(logInfo.detectedTime, outName, incName))
                        {
                            int num22;
                            ActGlobals.oFormActMain.GlobalTimeSorter = (num22 = ActGlobals.oFormActMain.GlobalTimeSorter) + 1;
                            ActGlobals.oFormActMain.AddCombatAction(1, critical, special, outName, "Melee", new Dnum((int)Dnum.Unknown, "resisted"), logInfo.detectedTime, num22, incName, string.Empty);
                            if (flag2)
                            {
                                logInfo.detectedType = Color.Yellow.ToArgb();
                            }
                        }
                    }
                    else if ((str.IndexOf("resisted") != -1) && (str.IndexOf("'s ") != -1))
                    {
                        incName = str.Substring(0, str.IndexOf(" resisted "));
                        incName = this.CheckYou(incName);
                        outName = str.Substring(str.IndexOf(" resisted ") + 10, str.IndexOf("'s ") - (str.IndexOf(" resisted") + 10));
                        outName = this.CheckYou(outName);
                        theAttackType = str.Substring(str.IndexOf("'s ") + 3, (str.Length - (str.IndexOf("'s ") + 3)) - 2);
                        if (ActGlobals.oFormActMain.SetEncounter(logInfo.detectedTime, outName, incName))
                        {
                            int num23;
                            ActGlobals.oFormActMain.GlobalTimeSorter = (num23 = ActGlobals.oFormActMain.GlobalTimeSorter) + 1;
                            ActGlobals.oFormActMain.AddCombatAction(1, critical, special, outName, "Melee", new Dnum((int)Dnum.Unknown, "resisted"), logInfo.detectedTime, num23, incName, string.Empty);
                            if (flag2)
                            {
                                logInfo.detectedType = Color.Yellow.ToArgb();
                            }
                        }
                    }
                    else if ((str.IndexOf("resisted") != -1) && (str.IndexOf(".") != -1))
                    {
                        incName = str.Substring(0, str.IndexOf(" resisted "));
                        incName = this.CheckYou(incName);
                        outName = "You";
                        outName = this.CheckYou(outName);
                        theAttackType = str.Substring(str.IndexOf(" resisted ") + 10, (str.Length - (str.IndexOf(" resisted ") + 10)) - 2);
                        if (ActGlobals.oFormActMain.SetEncounter(logInfo.detectedTime, outName, incName))
                        {
                            int num24;
                            ActGlobals.oFormActMain.GlobalTimeSorter = (num24 = ActGlobals.oFormActMain.GlobalTimeSorter) + 1;
                            ActGlobals.oFormActMain.AddCombatAction(1, critical, special, outName, "Melee", new Dnum((int)Dnum.Unknown, "resisted"), logInfo.detectedTime, num24, incName, string.Empty);
                            if (flag2)
                            {
                                logInfo.detectedType = Color.Yellow.ToArgb();
                            }
                        }
                    }
                }
            #endregion

                #region evaded
                else if (str.Contains("evaded"))
                {
                    if ((str.IndexOf("evaded") != -1) && (str.IndexOf("'s ") != -1))
                    {
                        incName = str.Substring(0, str.IndexOf(" evaded "));
                        incName = this.CheckYou(incName);
                        outName = str.Substring(str.IndexOf(" evaded ") + 7, str.IndexOf("'s ") - (str.IndexOf(" evaded") + 7));
                        outName = this.CheckYou(outName);
                        int swingType = 1;
                        if (str.IndexOf(" attack. ") != -1)
                        {
                            theAttackType = "Melee";
                            swingType = 1;
                        }
                        else
                        {
                            theAttackType = str.Substring(str.IndexOf("'s ") + 3, (str.Length - (str.IndexOf("'s ") + 3)) - 2);
                            swingType = 2;
                        }
                        if (ActGlobals.oFormActMain.SetEncounter(logInfo.detectedTime, outName, incName))
                        {
                            int num25;
                            ActGlobals.oFormActMain.GlobalTimeSorter = (num25 = ActGlobals.oFormActMain.GlobalTimeSorter) + 1;
                            ActGlobals.oFormActMain.AddCombatAction(swingType, critical, special, outName, "Melee", new Dnum((int)Dnum.Unknown, "evaded"), logInfo.detectedTime, num25, incName, string.Empty);
                            if (flag2)
                            {
                                logInfo.detectedType = Color.Yellow.ToArgb();
                            }
                        }
                    }
                }
                #endregion

                #region removed/dispel
                else if (str.IndexOf("removed its abnormal physical conditions by using") != -1)
                {
                    outName = str.Substring(0, str.IndexOf("removed its abnormal physical conditions by using") - 1);
                    outName = this.CheckYou(outName);
                    incName = outName;
                    theAttackType = str.Substring(str.IndexOf("removed its abnormal physical conditions by using") + 50, (str.Length - (str.IndexOf("removed its abnormal physical conditions by using") + 50)) - 2);
                    special = "Cure";
                    if (ActGlobals.oFormActMain.InCombat)
                    {
                        int num26;
                        ActGlobals.oFormActMain.GlobalTimeSorter = (num26 = ActGlobals.oFormActMain.GlobalTimeSorter) + 1;
                        ActGlobals.oFormActMain.AddCombatAction(20, critical, special, outName, "Melee", new Dnum(0, "cured"), logInfo.detectedTime, num26, incName, string.Empty);
                        if (flag2)
                        {
                            logInfo.detectedType = Color.Green.ToArgb();
                        }
                    }
                }
                else if ((str.IndexOf("removed abnormal physical conditions from") != -1) && (str.IndexOf("by using") != -1))
                {
                    outName = str.Substring(0, str.IndexOf("removed abnormal physical conditions from") - 1);
                    outName = this.CheckYou(outName);
                    incName = str.Substring(str.IndexOf("removed abnormal physical conditions from") + 0x2a, (str.IndexOf("by using") - (str.IndexOf("removed abnormal physical conditions from") + 0x2a)) - 1);
                    incName = this.CheckYou(incName);
                    theAttackType = str.Substring(str.IndexOf("by using") + 9, (str.Length - (str.IndexOf("by using") + 9)) - 2);
                    special = "Cure";
                    if (ActGlobals.oFormActMain.InCombat)
                    {
                        int num27;
                        ActGlobals.oFormActMain.GlobalTimeSorter = (num27 = ActGlobals.oFormActMain.GlobalTimeSorter) + 1;
                        ActGlobals.oFormActMain.AddCombatAction(20, critical, special, outName, "Melee", new Dnum(0, "cured"), logInfo.detectedTime, num27, incName, string.Empty);
                        if (flag2)
                        {
                            logInfo.detectedType = Color.Green.ToArgb();
                        }
                    }
                }
                else if ((str.IndexOf("dispelled the magical buffs from") != -1) && (str.IndexOf("by using") != -1))
                {
                    outName = str.Substring(0, str.IndexOf("dispelled the magical buffs from") - 1);
                    outName = this.CheckYou(outName);
                    incName = str.Substring(str.IndexOf("dispelled the magical buffs from") + 0x21, (str.IndexOf("by using") - (str.IndexOf("dispelled the magical buffs from") + 0x21)) - 1);
                    incName = this.CheckYou(incName);
                    theAttackType = str.Substring(str.IndexOf("by using") + 9, (str.Length - (str.IndexOf("by using") + 9)) - 2);
                    special = "Dispelled";
                    if (ActGlobals.oFormActMain.InCombat)
                    {
                        int num28;
                        ActGlobals.oFormActMain.GlobalTimeSorter = (num28 = ActGlobals.oFormActMain.GlobalTimeSorter) + 1;
                        ActGlobals.oFormActMain.AddCombatAction(20, critical, special, outName, "Melee", new Dnum(0, "dispelled"), logInfo.detectedTime, num28, incName, string.Empty);
                        if (flag2)
                        {
                            logInfo.detectedType = Color.Green.ToArgb();
                        }
                    }
                }
                else if (str.IndexOf("dispelled its magic effect by using") != -1)
                {
                    outName = str.Substring(0, str.IndexOf("dispelled its magic effect by using") - 1);
                    outName = this.CheckYou(outName);
                    incName = outName;
                    theAttackType = str.Substring(str.IndexOf("by using") + 9, (str.Length - (str.IndexOf("by using") + 9)) - 2);
                    special = "Cure";
                    if (ActGlobals.oFormActMain.InCombat)
                    {
                        int num2;
                        ActGlobals.oFormActMain.GlobalTimeSorter = (num2 = ActGlobals.oFormActMain.GlobalTimeSorter) + 1;
                        ActGlobals.oFormActMain.AddCombatAction(20, critical, special, outName, "Melee", new Dnum(0, "cured"), logInfo.detectedTime, num2, incName, string.Empty);
                        if (flag2)
                        {
                            logInfo.detectedType = Color.Green.ToArgb();
                        }
                    }
                }
                else if (str.StartsWith("Your abnormal physical conditions were removed because"))
                {
                    Regex rDispelOnYou = new Regex(@"Your abnormal physical conditions were removed because (?<actor>[a-zA-Z ]*) used (?<skill>[a-zA-Z \-']*?) on you", RegexOptions.Compiled);
                    Match match = rDispelOnYou.Match(str);
                    incName = CheckYou("you");
                    outName = match.Groups["actor"].Value;
                    theAttackType = match.Groups["skill"].Value;
                    AddCombatAction(logInfo, outName, incName, theAttackType, critical, special, Dnum.NoDamage, SwingTypeEnum.CureDispel);
                    return;
                }
                #endregion

                #region state parses
                else
                    // match "xxx is in the xxx state..."
                    if (rStateAbility.IsMatch(str))
                    {
                        Match match = rStateAbility.Match(str);
                        string target = CheckYou(match.Groups["target"].Value);
                        string actor = CheckYou(match.Groups["actor"].Value);
                        if (String.IsNullOrEmpty(actor)) actor = target;
                        string skill = match.Groups["skill"].Value;
                        return;
                    }
                    else // TODO remove these useless else ifs
                        if (rWeakened.IsMatch(str))
                        {
                            //Match match = rWeakened.Match(str);
                            return;
                        }
                #endregion

                        else
                        {
                            if (debugParse && !IsIgnore(str))
                                ui.AddText("unparsed: " + str);
                        }

        }

        private void AddCombatAction(LogLineEventArgs logInfo, string attacker, string victim, string theAttackType, bool critical, string special, string damage, SwingTypeEnum swingType)
        {
            AddCombatAction(logInfo, attacker, victim, theAttackType, critical, special, damage, swingType, string.Empty);
        }

        private void AddCombatAction(LogLineEventArgs logInfo, string attacker, string victim, string theAttackType, bool critical, string special, string damage, SwingTypeEnum swingType, string damageType)
        {
            AddCombatAction(logInfo, attacker, victim, theAttackType, critical, special, int.Parse(damage.Replace(",", String.Empty)), swingType, damageType);
        }

        private void AddCombatAction(LogLineEventArgs logInfo, string attacker, string victim, string theAttackType, bool critical, string special, Dnum damage, SwingTypeEnum swingType)
        {
            AddCombatAction(logInfo, attacker, victim, theAttackType, critical, special, damage, swingType, string.Empty);
        }

        private void AddCombatAction(LogLineEventArgs logInfo, string attacker, string victim, string theAttackType, bool critical, string special, Dnum damage, SwingTypeEnum swingType, string damageType)
        {
            if (ActGlobals.oFormActMain.SetEncounter(logInfo.detectedTime, attacker, victim))
            {
                int globalTime = ActGlobals.oFormActMain.GlobalTimeSorter++;
                ActGlobals.oFormActMain.AddCombatAction((int)swingType, critical, special, attacker, theAttackType, damage, logInfo.detectedTime, globalTime, victim, damageType);
            }
        }

        private void AddCombatActionSpecial(LogLineEventArgs logInfo, string attacker, string victim, string theAttackType, bool critical, string special, string damage, SwingTypeEnum swingType1, SwingTypeEnum swingType2)
        {
            if (ActGlobals.oFormActMain.SetEncounter(logInfo.detectedTime, attacker, victim))
            {
                int globalTime = ActGlobals.oFormActMain.GlobalTimeSorter++;
                ActGlobals.oFormActMain.AddCombatAction((int)swingType1, critical, special, attacker, theAttackType, new Dnum(0, special.ToLower()), logInfo.detectedTime, globalTime, victim, string.Empty);
                globalTime = ActGlobals.oFormActMain.GlobalTimeSorter++;
                ActGlobals.oFormActMain.AddCombatAction((int)swingType2, critical, special, attacker, theAttackType, int.Parse(damage), logInfo.detectedTime, globalTime, victim, string.Empty);
            }
        }

        private DateTime ParseDateTime(string FullLogLine)
        {
            string str = FullLogLine.Substring(0, 4) + "-" + FullLogLine.Substring(5, 2) + FullLogLine.Substring(8, 2);
            string str2 = FullLogLine.Substring(11, 8);
            return DateTime.ParseExact(str + "-" + str2, "yyyy-MMdd-HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private string CheckYou(string IncName)
        {
            switch (IncName.ToUpper().Trim())
            {
                case "YOU":
                case "YOUR":
                case "YOURSELF":
                    return ActGlobals.charName == "YOU" ? lastCharName : ActGlobals.charName;
                default:
                    return IncName;
            }
        }

        private string DamageNumberFixX(string damage)
        {
            damage = Regex.Replace(damage, "[^0-9]", "");
            return damage;
        }

        private string FindAttacker(string victim, string spellname)
        {
            return "me?!?";
        }

        internal void SetCharName(string charName)
        {
            lastCharName = charName;
            ActGlobals.charName = charName;
        }

        internal void SetGuessDotCasters(bool guessDotCasters)
        {
            this.guessDotCasters = guessDotCasters;
        }

        internal void SetDebugParse(bool debugParse)
        {
            this.debugParse = debugParse;
        }

        internal void SetTagBlockedAttacks(bool tagBlockedAttacks)
        {
            this.tagBlockedAttacks = tagBlockedAttacks;
        }
    }
}