namespace AionParse_Plugin
{
    using Advanced_Combat_Tracker;
    using System;
    using System.Drawing;
    using System.Globalization;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;

    /* TODO
     * 
     * Spirits from Summoners
     *  The spirit used a skill on Iceghost Priest because Coszet used Spirit Thunderbolt Claw I.
     *  The spirit used a skill on Brutal Mist Mane Pawsoldier because Hexis used Spirit Erosion I.
     *  
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
     */

    public class AionParse : IActPluginV1
    {
        Regex rInflictDamageOnYou = new Regex(@"^(?<attacker>[a-zA-Z ]*) inflicted (?<damage>(\d+,)?\d+) damage and the rune carve effect on you by using (?<skill>[a-zA-Z \-']*)\.$", RegexOptions.Compiled);
        Regex rInflictDamage = new Regex(@"^(?<attacker>[a-zA-Z ]*?)( has)? inflicted (?<damage>(\d+,)?\d+) (?<critical>critical )?damage on (?<targetclause>[a-zA-Z \-']*)\.$", RegexOptions.Compiled);
        Regex rUsingAttack = new Regex(@"^(?<victimclause>[a-zA-Z ]*) by using (?<skill>[a-zA-Z \-']*)$", RegexOptions.Compiled);
        Regex rPatternEngraving = new Regex(@"^(?<victim>[a-zA-Z ]*) and caused the (?<special>[a-zA-Z ]*) effect$", RegexOptions.Compiled);
        Regex rIgniteAether = new Regex(@"^(?<victim>[a-zA-Z ]*) and dispelled some of its magical buffs by using (?<skill>[a-zA-Z \-']*)$", RegexOptions.Compiled); // I think only Ignite Aether spells has this line
        Regex rReflect = new Regex(@"^(?<victim>[a-zA-Z ]*) by reflecting the attack$", RegexOptions.Compiled);
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


        string lastActivatedSkill = "";
        int lastActivatedSkillGlobalTime = -1;
        DateTime lastActivedSkillTime = DateTime.MinValue;

        ContinuousDamageSet continuousDamageSet = new ContinuousDamageSet();

        string lastCharName = ActGlobals.charName;

        AionParseForm ui;

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
            ui.InitFromPlugin(lastCharName);
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

            continuousDamageSet.Clear();
        }

        private void oFormActMain_BeforeLogLineRead(bool isImport, LogLineEventArgs logInfo)
        {
            string str = logInfo.logLine.Substring(0x16, logInfo.logLine.Length - 0x16).Trim();
            string incName = string.Empty;
            string outName = string.Empty;
            string damage = string.Empty;
            int swingType = 1;
            string theAttackType = string.Empty;
            string special = string.Empty;
            bool critical = false;
            bool flag2 = false;

            if (str.Contains("[charname:")) return; //ignore chats ([charname:...] is a link to a name
            if (str.StartsWith(CheckYou("you") + ":")) return; // ignore your own chats
            if (str.Contains("You are gathering")) return;
            if (str.Contains("You must level up to raise your skill level.")) return;
            if (str.Contains("gathered successfully")) return;
            if (str.Contains("failed to gather")) return;
            if (str.StartsWith("You have acquired [item:")) return;
            if (str.Contains("has acquired [item:")) return;
            if (str.Contains("has logged in")) return;
            if (str.StartsWith("You changed the connection status")) return;
            if (str.StartsWith("You changed the group")) return;
            if (str.StartsWith("You have joined the")) return; // ignore channels
            if (str.Contains("rolled the dice and got a")) return;
            if (str == "You gave up rolling the dice.") return;
            if (str.StartsWith("You have gained") && str.Contains("EXP")) return;
            if (str.StartsWith("You boosted your")) return; // ignore You boosted your evasion by using Focused Evasion I. 
            if (str.Contains("has conquered")) return; // ignore fortress
            if (str.Contains("is no longer vulnerable")) return; // ignore fortress
            if (str.Contains("movement speed decreased as you used")) return;
            if (str.EndsWith("restored its movement speed.")) return;
            if (str.EndsWith("restored its attack speed.")) return;
            if (str == "Your movement speed is restored to normal.") return;
            if (str == "Your attack speed is restored to normal.") return;
            if (str.EndsWith("is no longer immobilized.")) return;
            if (str.EndsWith("is no longer afraid.")) return;
            if (str.Contains("loot rate has increased because")) return;
            if (str.Contains("became poisoned because")) return;
            if (str.EndsWith("is no longer poisoned.")) return;
            if (str.EndsWith("is no longer bleeding.")) return;
            if (str.EndsWith("woke up.")) return;
            if (str.Contains("became blinded because")) return;
            if (str.EndsWith("is no longer blind.")) return;
            if (str.Contains("became stunned because")) return;
            if (str.Contains("is no longer stunned.")) return;
            if (str.Contains("fell down from shock because")) return;
            if (str.Contains("is no longer shocked.")) return;
            if (str.Contains("is spinning because")) return;
            if (str.Contains("is no longer spinning.")) return;
            if (str.StartsWith("You knocked") && str.Contains("back by using")) return; // knockback by you
            if (str.Contains("was knocked back from shock because")) return; // knockback by others
            if (str.Contains("is no longer staggering.")) return;
            if (str.Contains("became snared in mid-air because")) return;
            if (str.Contains("was released from the aerial snare because")) return;
            if (str.Contains("released from the Aerial Snare.")) return;
            if (str.Contains("is unable to fly because")) return;
            if (str.Contains("The target is too far away")) return;
            if (str.StartsWith("Quest updated:")) return;
            if (str.StartsWith("Quest complete:")) return;
            if (str.StartsWith("You failed to share the quest with")) return;
            if (str == "You cannot receive a quest that you are already working on.") return;
            if (str.StartsWith("You learned")) return;
            if (str.StartsWith("You interrupted the target's skill.")) return;
            if (str.StartsWith("You are no longer")) return;
            if (str.StartsWith("You have stopped gathering.")) return;
            if (str.StartsWith("You were killed")) return;
            if (str.StartsWith("You have earned")) return;
            if (str.StartsWith("Legion Message:")) return;
            if (str.Contains("speed has decreased")) return;
            if (str.Contains("was affected by its own")) return;
            if (str == "Invalid target.") return;
            if (str == "You stopped using the Macro.") return;
            if (str == "You cannot use a Macro yet.") return;
            if (str == "The skill was cancelled.") return;
            if (str == "You cannot use that because there is an obstacle in the way.") return;
            if (str == "You cannot use the item as its cooldown time has not expired yet.") return;
            if (str.StartsWith("You restored your flight time by")) return;
            if (str.Contains("to sleep by using")) return;
            if (str.StartsWith("You transformed") && str.Contains("into Cursed Tree by using Curse of Roots")) return;
            if (str.Contains("boosted") && (str.Contains("by using Curse of Roots") || str.Contains("by using Sleep"))) return;
            if (str == "Starts the auto-distribution of miscellaneous items.") return;
            if (str == "You cannot use that on your target.") return;
            if (str.EndsWith("existing skill.") && str.Contains("conflicted with")) return;
            if (str.StartsWith("You have played for") && str.EndsWith("Please take a break.")) return;
            if (str.StartsWith("You teleported yourself by using Blind Leap")) return;
            if (str.StartsWith("You dispelled the magic effect by using Blind Leap")) return;
            if (str == "You have died.") return;


            int num2;
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
            }

            // check for critical
            if (str.Contains("Critical Hit!"))
            {
                critical = true;
                str = str.Substring(14, str.Length - 14);
            }

            // match "xxx has been activated." for use in damage shields like Robe of Cold
            if (rActivated.IsMatch(str))
            {
                Match match = rActivated.Match(str);
                lastActivatedSkill = match.Groups["skill"].Value;
                lastActivatedSkillGlobalTime = ActGlobals.oFormActMain.GlobalTimeSorter;
                lastActivedSkillTime = logInfo.detectedTime;
                return;
            }

            // match "Your attack on xxx was reflected and inflicted xxx damage on you"
            if (rReflectDamageOnYou.IsMatch(str))
            {
                Match match = rReflectDamageOnYou.Match(str);
                incName = CheckYou("you");
                outName = match.Groups["attacker"].Value;
                damage = match.Groups["damage"].Value;
                special = "reflected";
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

                    // submatch Assassin rune carving
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

                    theAttackType = mUsingAttack.Groups["skill"].Value;
                    if (theAttackType.StartsWith("Blood Rune"))
                    {
                        continuousDamageSet.Add(outName, incName, theAttackType, logInfo.detectedTime); // record Blood Rune actor for when it deals payload damage later
                    }

                    var inflictSwingType = SwingTypeEnum.NonMelee;
                    if (mInflict.Groups[1].Value == "you" &&
                        (theAttackType.Contains("Healing Wind") || theAttackType.Contains("Light of Recovery") ||
                        theAttackType.Contains("Healing Light") || theAttackType.Contains("Radiant Cure") ||
                        theAttackType.Contains("Flash of Recovery")))
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
                    theAttackType = mIgniteAether.Groups["skill"].Value;
                    AddCombatAction(logInfo, outName, incName, theAttackType, critical, special, Dnum.NoDamage, SwingTypeEnum.CureDispel);
                    AddCombatAction(logInfo, outName, incName, theAttackType, critical, special, damage, SwingTypeEnum.NonMelee);
                    return;
                }

                // submatch "reflecting the attack"
                var mReflect = rReflect.Match(targetClause);
                if (mReflect.Success)
                {
                    special = "reflected";
                    incName = CheckYou(mReflect.Groups["victim"].Value);

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
                AddCombatAction(logInfo, outName, incName, "Melee", critical, String.Empty, damage, SwingTypeEnum.Melee);
                return;
            }

            // match "continuous damage"
            if (rContDmg1.IsMatch(str))
            {
                Match match = rContDmg1.Match(str);
                string actor = CheckYou(match.Groups["actor"].Value);
                string target = CheckYou(match.Groups["target"].Value);
                string skill = match.Groups["skill"].Value;
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
                continuousDamageSet.Add(actor, target, skill, logInfo.detectedTime);
                AddCombatAction(logInfo, actor, target, skill, false, String.Empty, Dnum.NoDamage, SwingTypeEnum.NonMelee);
                return;
            }

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

                AddCombatAction(logInfo, outName, incName, theAttackType, critical, special, damage, SwingTypeEnum.NonMelee);
                return;
            }


            // match "xxx inflicted xxx damage and the rune carve effect on you by using xxx ."
            var mInflictDamageOnYou = rInflictDamageOnYou.Match(str);
            if (mInflictDamageOnYou.Success)
            {
                outName = mInflictDamageOnYou.Groups["attacker"].Value;
                incName = CheckYou("you");
                //special = "pattern engraving";
                damage = mInflictDamageOnYou.Groups["damage"].Value;
                theAttackType = mInflictDamageOnYou.Groups["skill"].Value;
                AddCombatAction(logInfo, outName, incName, theAttackType, critical, special, damage, SwingTypeEnum.NonMelee);
                return;
            }


            // match "xxx received xxx damage from xxx"
            if (rReceiveDamage.IsMatch(str))
            {
                Match match = rReceiveDamage.Match(str);
                outName = match.Groups["attacker"].Value;
                incName = CheckYou(match.Groups["victim"].Value);
                damage = match.Groups["damage"].Value;
                AddCombatAction(logInfo, outName, incName, "Melee", critical, "", damage, SwingTypeEnum.Melee);
                return;
            }

            // match "xxx recieved xxx yyy damage after you used xxx" 
            if (rContDmg3.IsMatch(str))
            {
                Match match = rContDmg3.Match(str);
                outName = "Unknown";
                incName = match.Groups["victim"].Value;
                damage = match.Groups["damage"].Value;
                string damageType = match.Groups["damagetype"].Value;
                theAttackType = match.Groups["skill"].Value; // only DoT skills: Poison, Poison Arrow, or Wind Cut Down skills match this... often mob skills
                AddCombatAction(logInfo, outName, incName, theAttackType, critical, special, damage, SwingTypeEnum.NonMelee, damageType);
                return;
            }


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

            if (rWeakened.IsMatch(str))
            {
                //Match match = rWeakened.Match(str);
                return;
            }

            /* NOTE: here's an example of battle data
             * > You received continuous damage because Black Blaze Spirit used Wing Ignition.
             * > Vyn inflicted 45 damage on you by using Wing Ignition. 
             */


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
                if (ActGlobals.oFormActMain.InCombat)
                {
                    Match match = (new Regex(@"^(?<actor>[a-zA-Z ]*) restored (?<mp>.*) MP\.$", RegexOptions.Compiled)).Match(str);
                    incName = CheckYou(match.Groups["actor"].Value);
                    outName = incName; // assume: this log comes from a self action
                    damage = match.Groups["mp"].Value;
                    theAttackType = "Unknown";
                    AddCombatAction(logInfo, outName, incName, theAttackType, critical, special, damage, SwingTypeEnum.PowerHealing);
                }
                return;
            }

            if (str.Contains("blocked"))
            {
                // match "The attack was blocked by the xxx effect cast on xxx."
                if (str.StartsWith("The attack was blocked by the "))
                {
                    Regex rBlockAnon = new Regex(@"The attack was blocked by the (?<skill>[a-zA-Z \-']*?) effect cast on (?<target>[a-zA-Z ]*)\.", RegexOptions.Compiled);
                    Match match = rBlockAnon.Match(str);
                    if (!match.Success)
                    {
                        ui.AddText("Exception-Unable to parse[e4]: " + str);
                        return;
                    }
                    incName = CheckYou(match.Groups["target"].Value);
                    theAttackType = match.Groups["skill"].Value;
                    AddCombatAction(logInfo, "Unknown", incName, theAttackType, critical, special, Dnum.NoDamage, SwingTypeEnum.Melee);
                    return;
                }

                else if ((str.IndexOf("blocked") != -1) && (str.IndexOf("'s attack with the") != -1))
                {
                    incName = str.Substring(0, str.IndexOf("blocked") - 1);
                    incName = this.CheckYou(incName);
                    outName = str.Substring(str.IndexOf("blocked") + 8, str.IndexOf("'s attack with the") - (str.IndexOf("blocked") + 8));
                    outName = this.CheckYou(outName);
                    special = str.Substring(str.IndexOf("'s attack with the") + 0x13, (str.Length - (str.IndexOf("'s attack with the") + 0x13)) - 9);
                    if (ActGlobals.oFormActMain.SetEncounter(logInfo.detectedTime, outName, incName))
                    {
                        int num19;
                        ActGlobals.oFormActMain.GlobalTimeSorter = (num19 = ActGlobals.oFormActMain.GlobalTimeSorter) + 1;
                        ActGlobals.oFormActMain.AddCombatAction(1, critical, special, outName, "Melee", new Dnum((int)Dnum.Unknown, "blocked"), logInfo.detectedTime, num19, incName, string.Empty);
                        if (flag2)
                        {
                            logInfo.detectedType = Color.Yellow.ToArgb();
                        }
                    }
                }
                else if ((str.IndexOf("blocked") != -1) && (str.IndexOf("'s attack") != -1))
                {
                    incName = str.Substring(0, str.IndexOf("blocked") - 1);
                    incName = this.CheckYou(incName);
                    outName = str.Substring(str.IndexOf("blocked") + 8, str.IndexOf("'s attack") - (str.IndexOf("blocked") + 8));
                    outName = this.CheckYou(outName);
                    if (ActGlobals.oFormActMain.SetEncounter(logInfo.detectedTime, outName, incName))
                    {
                        int num20;
                        ActGlobals.oFormActMain.GlobalTimeSorter = (num20 = ActGlobals.oFormActMain.GlobalTimeSorter) + 1;
                        ActGlobals.oFormActMain.AddCombatAction(1, critical, special, outName, "Melee", new Dnum((int)Dnum.Unknown, "blocked"), logInfo.detectedTime, num20, incName, string.Empty);
                        if (flag2)
                        {
                            logInfo.detectedType = Color.Yellow.ToArgb();
                        }
                    }
                }
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
                }
            }
            else if (str.Contains("resisted"))
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
            else if (str.Contains("evaded"))
            {
                if ((str.IndexOf("evaded") != -1) && (str.IndexOf("'s ") != -1))
                {
                    incName = str.Substring(0, str.IndexOf(" evaded "));
                    incName = this.CheckYou(incName);
                    outName = str.Substring(str.IndexOf(" evaded ") + 7, str.IndexOf("'s ") - (str.IndexOf(" evaded") + 7));
                    outName = this.CheckYou(outName);
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
                AddCombatAction(logInfo, outName, incName, theAttackType, false, string.Empty, "0", SwingTypeEnum.CureDispel);
                return;
            }
            else
            {
                ui.AddText("Unable to parse: " + str);
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
    }
}