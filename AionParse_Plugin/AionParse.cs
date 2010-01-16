namespace AionParse_Plugin
{
    using Advanced_Combat_Tracker;
    using System;
    using System.Drawing;
    using System.Globalization;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;

    public class AionParse : IActPluginV1
    {
        private string CheckYou(string IncName)
        {
            string charName = IncName;
            if (IncName.ToUpper() == "YOU")
            {
                return ActGlobals.charName;
            }
            if (IncName.ToUpper() == "YOUR")
            {
                return ActGlobals.charName;
            }
            if (IncName.ToUpper() == "YOURSELF")
            {
                charName = ActGlobals.charName;
            }
            return charName;
        }

        private string DamageNumberFix(string damage)
        {
            damage = Regex.Replace(damage, "[^0-9]", "");
            return damage;
        }

        public void DeInitPlugin()
        {
            ActGlobals.oFormActMain.BeforeLogLineRead -= new LogLineEventDelegate(this.oFormActMain_BeforeLogLineRead);
        }

        private string FindAttacker(string victim, string spellname)
        {
            return "me?!?";
        }

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
            
        }

        private void oFormActMain_BeforeLogLineRead(bool isImport, LogLineEventArgs logInfo)
        {
            string str = logInfo.logLine.Substring(0x16, logInfo.logLine.Length - 0x16);
            string incName = string.Empty;
            string outName = string.Empty;
            string damage = string.Empty;
            int swingType = 1;
            string theAttackType = string.Empty;
            string special = string.Empty;
            bool critical = false;
            bool flag2 = false;

            if (str.Contains("[charname:")) return; //ignore chats ([charname:...] is a link to a name
            if (str.Contains("You are gathering")) return;
            if (str.Contains("You must level up to raise your skill level.")) return;
            if (str.Contains("gathered successfully")) return;
            if (str.Contains("failed to gather")) return;
            if (str.Contains("You have aquired")) return;
            if (str.Contains("has logged in")) return;

            int num2;
            if (ActGlobals.oFormActMain.ZoneChangeRegex.IsMatch(logInfo.logLine))
            {
                ActGlobals.oFormActMain.ChangeZone(ActGlobals.oFormActMain.ZoneChangeRegex.Replace(logInfo.logLine, "$1"));
            }
            if ((str.IndexOf("/act ") != -1) && (str.Length > (str.IndexOf("/act ") + 7)))
            {
                string commandText = str.Substring(str.IndexOf("/act ") + 5, str.Length - (str.IndexOf("/act ") + 5)).Trim();
                ActGlobals.oFormActMain.ActCommands(commandText);
            }
            if (str.IndexOf("Critical Hit!") != -1)
            {
                critical = true;
                str = str.Substring(14, str.Length - 14);
            }


            if (((str.IndexOf("attack on") != -1) && (str.IndexOf("was reflected and inflicted") != -1)) && (str.IndexOf("damage on") != -1))
            {
                outName = str.Substring(0, str.IndexOf("attack on") - 1);
                outName = this.CheckYou(outName);
                incName = str.Substring(str.IndexOf("attack on") + 10, (str.IndexOf("was reflected and inflicted") - (str.IndexOf("attack on") + 10)) - 1);
                incName = this.CheckYou(incName);
                damage = str.Substring(str.IndexOf("was reflected and inflicted") + 0x1d, (str.IndexOf("damage on") - (str.IndexOf("was reflected and inflicted") + 0x1d)) - 1);
                damage = this.DamageNumberFix(damage);
                special = "Reflected";
                string victim = str.Substring(str.IndexOf("damage on") + 10, (str.Length - (str.IndexOf("damage on") + 10)) - 2);
                if (ActGlobals.oFormActMain.SetEncounter(logInfo.detectedTime, outName, incName))
                {
                    int num3;
                    ActGlobals.oFormActMain.GlobalTimeSorter = (num2 = ActGlobals.oFormActMain.GlobalTimeSorter) + 1;
                    ActGlobals.oFormActMain.AddCombatAction(1, critical, special, outName, "Melee", new Dnum((int)Dnum.Unknown, "reflected"), logInfo.detectedTime, num2, incName, string.Empty);
                    ActGlobals.oFormActMain.GlobalTimeSorter = (num3 = ActGlobals.oFormActMain.GlobalTimeSorter) + 1;
                    ActGlobals.oFormActMain.AddCombatAction(1, critical, special, incName, "Melee", int.Parse(damage), logInfo.detectedTime, num3, victim, string.Empty);
                    if (flag2)
                    {
                        logInfo.detectedType = Color.Red.ToArgb();
                    }
                }
                return;
            }

            // match "xxx inflicted xxx damage and the rune carve effect on you by using xxx ."
            var rInflictDamageOnYou = new Regex(@"([a-zA-Z ]*) inflicted ((\d+,)?\d+) damage and the rune carve effect on you by using ([a-zA-Z ]*)");
            var mInflictDamageOnYou = rInflictDamageOnYou.Match(str);
            if (mInflictDamageOnYou.Success)
            {
                outName = mInflictDamageOnYou.Groups[1].Value;
                incName = CheckYou("you");
                special = "pattern engraving";
                damage = DamageNumberFix(mInflictDamageOnYou.Groups[2].Value);
                theAttackType = mInflictDamageOnYou.Groups[3].Value;
                SetEncounter(logInfo, outName, incName, theAttackType, critical, special, damage, SwingTypeEnum.NonMelee, flag2);
                return;
            }

            // match "xxx inflicted xxx damage on xxx ..."
            var rInflictDamage = new Regex(@"([a-zA-Z ]*?)( has)? inflicted ((\d+,)?\d+) damage on ([a-zA-Z ]*)");
            var mInflict = rInflictDamage.Match(str);
            if (mInflict.Success)
            {
                outName = CheckYou(mInflict.Groups[1].Value); // source
                damage = DamageNumberFix(mInflict.Groups[3].Value); // dmg

                // submatch "using ability"
                string targetGroup = mInflict.Groups[5].Value; // target & extra info
                var rUsingAttack = new Regex("([a-zA-Z ]*) by using ([a-zA-Z ]*)");
                var mUsingAttack = rUsingAttack.Match(targetGroup);
                if (mUsingAttack.Success)
                {
                    // submatch Assassin rune 
                    var rPatternEngraving = new Regex("([a-zA-Z ]*) caused the ([a-zA-Z ]*) effect");
                    var mPatternEngraving = rPatternEngraving.Match(mUsingAttack.Groups[1].Value);
                    if (mPatternEngraving.Success)
                    {
                        incName = CheckYou(mPatternEngraving.Groups[1].Value);
                        special = mPatternEngraving.Groups[2].Value;
                    }
                    else
                    {
                        incName = CheckYou(mUsingAttack.Groups[1].Value);
                        special = String.Empty;
                    }

                    theAttackType = mUsingAttack.Groups[2].Value;
                    var inflictSwingType = SwingTypeEnum.NonMelee;
                    if (mInflict.Groups[1].Value == "you" &&
                        (theAttackType.Contains("Healing Wind") || theAttackType.Contains("Light of Recovery") ||
                        theAttackType.Contains("Healing Light") || theAttackType.Contains("Radiant Cure") ||
                        theAttackType.Contains("Flash of Recovery")))
                    {
                        inflictSwingType = SwingTypeEnum.Healing;
                    }

                    SetEncounter(logInfo, outName, incName, theAttackType, critical, special, damage, inflictSwingType, flag2);
                    return;
                }

                // submatch "and dispelled buffs using Ignite Aether"
                var rIgniteAether = new Regex("([a-zA-Z ]*) and dispelled some of its magical buffs by using ([a-zA-Z ]*)"); // I think only Ignite Aether spells has this line
                var mIgniteAether = rIgniteAether.Match(targetGroup);
                if (mIgniteAether.Success)
                {
                    incName = CheckYou(mIgniteAether.Groups[1].Value);
                    theAttackType = mIgniteAether.Groups[2].Value;
                    special = "Dispelled";
                    SetEncounterSpecial(logInfo, outName, incName, theAttackType, critical, special, damage, SwingTypeEnum.CureDispel, SwingTypeEnum.NonMelee, flag2);
                    return;
                }

                // match "reflecting the attack"
                var rReflect = new Regex("([a-zA-Z ]*) by reflecting the attack");
                var mReflect = rReflect.Match(targetGroup);
                if (mReflect.Success)
                {
                    incName = CheckYou(mReflect.Groups[1].Value);
                    theAttackType = "Damage Shield";
                    special = "Reflected";
                    SetEncounter(logInfo, outName, incName, theAttackType, critical, special, damage, SwingTypeEnum.NonMelee, flag2);
                    return;
                }

                
                // no ability match
                incName = CheckYou(targetGroup);
                theAttackType = "Melee";
                SetEncounter(logInfo, outName, incName, theAttackType, critical, String.Empty, damage, SwingTypeEnum.Melee, flag2);
                return;
            }

            else if ((str.IndexOf("received") != -1) && (str.IndexOf("damage from") != -1))
            {
                incName = str.Substring(0, str.IndexOf("received") - 1);
                incName = this.CheckYou(incName);
                if (str.IndexOf("by using") != -1)
                {
                    outName = str.Substring(str.IndexOf("damage from") + 12, str.IndexOf("by using") - (str.IndexOf("damage from") + 12));
                    outName = this.CheckYou(outName);
                    swingType = 2;
                    theAttackType = str.Substring(str.IndexOf("by using") + 9, (str.Length - (str.IndexOf("by using") + 9)) - 2);
                }
                else
                {
                    outName = str.Substring(str.IndexOf("damage from") + 12, (str.Length - (str.IndexOf("damage from") + 12)) - 2);
                    outName = this.CheckYou(outName);
                    swingType = 1;
                    theAttackType = "Melee";
                }
                damage = str.Substring(str.IndexOf("received") + 9, (str.IndexOf("damage from") - (str.IndexOf("received") + 9)) - 1);
                damage = this.DamageNumberFix(damage);
                if (ActGlobals.oFormActMain.SetEncounter(logInfo.detectedTime, outName, incName))
                {
                    int num10;
                    ActGlobals.oFormActMain.GlobalTimeSorter = (num10 = ActGlobals.oFormActMain.GlobalTimeSorter) + 1;
                    ActGlobals.oFormActMain.AddCombatAction(swingType, critical, special, outName, theAttackType, int.Parse(damage), logInfo.detectedTime, num10, incName, string.Empty);
                    if (flag2)
                    {
                        logInfo.detectedType = Color.Blue.ToArgb();
                    }
                }
            }

                /* NOTE: here's an example of battle data
                 * > You received continuous damage because Black Blaze Spirit used Wing Ignition.
                 * > Vyn inflicted 45 damage on you by using Wing Ignition. 
                 * 
                 * > You inflicted continuous damage on Black Blaze Spirit by using Flame Cage II. 
                 * > Black Blaze Spirit received 153 damage due to the effect of Flame Cage II. 
                 * The logs don't tell when the DoT ends tho. =(
                 */

            else if ((str.IndexOf(" used ") != -1) && (str.IndexOf("to inflict the continuous damage effect on") != -1))
            {
                outName = str.Substring(0, str.IndexOf(" used "));
                outName = this.CheckYou(outName);
                incName = str.Substring(str.IndexOf("to inflict the continuous damage effect on") + 0x2b, (str.Length - (str.IndexOf("to inflict the continuous damage effect on") + 0x2b)) - 2);
                incName = this.CheckYou(incName);
                theAttackType = str.Substring(str.IndexOf(" used ") + 6, (str.IndexOf("to inflict the continuous damage effect on") - (str.IndexOf(" used ") + 6)) - 1);
                swingType = 2;
                damage = "0";
                special = "DoT Start";
                if (ActGlobals.oFormActMain.SetEncounter(logInfo.detectedTime, outName, incName))
                {
                    int num11;
                    ActGlobals.oFormActMain.GlobalTimeSorter = (num11 = ActGlobals.oFormActMain.GlobalTimeSorter) + 1;
                    ActGlobals.oFormActMain.AddCombatAction(swingType, critical, special, outName, theAttackType, int.Parse(damage), logInfo.detectedTime, num11, incName, string.Empty);
                    if (flag2)
                    {
                        logInfo.detectedType = Color.Red.ToArgb();
                    }
                }
            }
            else if ((str.IndexOf("received") != -1) && (str.IndexOf("damage due to the effect of") != -1))
            {
                incName = str.Substring(0, str.IndexOf("received") - 1);
                incName = this.CheckYou(incName);
                outName = "Unknown";
                swingType = 2;
                theAttackType = str.Substring(str.IndexOf("damage due to the effect of") + 0x1c, (str.Length - (str.IndexOf("damage due to the effect of") + 0x1c)) - 2);
                damage = str.Substring(str.IndexOf("received") + 9, (str.IndexOf("damage due to the effect of") - (str.IndexOf("received") + 9)) - 1);
                damage = this.DamageNumberFix(damage);
                if (ActGlobals.oFormActMain.SetEncounter(logInfo.detectedTime, outName, incName))
                {
                    int num12;
                    ActGlobals.oFormActMain.GlobalTimeSorter = (num12 = ActGlobals.oFormActMain.GlobalTimeSorter) + 1;
                    ActGlobals.oFormActMain.AddCombatAction(swingType, critical, special, outName, theAttackType, int.Parse(damage), logInfo.detectedTime, num12, incName, string.Empty);
                    if (flag2)
                    {
                        logInfo.detectedType = Color.Blue.ToArgb();
                    }
                }
            }
                /* > You restored 74 of Becca's HP by using Word of Revival IV. 
                 * it's probably self heals or unknown healer
                 */
            else if ((str.IndexOf("restored") != -1) && (str.IndexOf("'s HP by using") != -1))
            {
                outName = str.Substring(0, str.IndexOf("restored") - 1);
                outName = this.CheckYou(outName);
                incName = str.Substring(str.IndexOf(" of ") + 4, str.IndexOf("'s HP by using") - (str.IndexOf(" of ") + 4));
                incName = this.CheckYou(incName);
                swingType = 3;
                theAttackType = str.Substring(str.IndexOf("'s HP by using") + 15, (str.Length - (str.IndexOf("'s HP by using") + 15)) - 2);
                damage = str.Substring(str.IndexOf("restored") + 9, str.IndexOf(" of ") - (str.IndexOf("restored") + 9));
                damage = this.DamageNumberFix(damage);
                if (ActGlobals.oFormActMain.InCombat)
                {
                    int num13;
                    ActGlobals.oFormActMain.GlobalTimeSorter = (num13 = ActGlobals.oFormActMain.GlobalTimeSorter) + 1;
                    ActGlobals.oFormActMain.AddCombatAction(swingType, critical, special, outName, theAttackType, int.Parse(damage), logInfo.detectedTime, num13, incName, string.Empty);
                    if (flag2)
                    {
                        logInfo.detectedType = Color.Green.ToArgb();
                    }
                }
            }
            else if ((str.IndexOf("recovered") != -1) && (str.IndexOf("HP by using") != -1))
            {
                incName = str.Substring(0, str.IndexOf("recovered") - 1);
                incName = this.CheckYou(incName);
                outName = incName;
                outName = this.CheckYou(outName);
                swingType = 3;
                theAttackType = str.Substring(str.IndexOf("HP by using") + 12, (str.Length - (str.IndexOf("HP by using") + 12)) - 2);
                damage = str.Substring(str.IndexOf("recovered") + 10, (str.IndexOf("HP by using") - (str.IndexOf("recovered") + 10)) - 1);
                damage = this.DamageNumberFix(damage);
                if (ActGlobals.oFormActMain.InCombat)
                {
                    int num14;
                    ActGlobals.oFormActMain.GlobalTimeSorter = (num14 = ActGlobals.oFormActMain.GlobalTimeSorter) + 1;
                    ActGlobals.oFormActMain.AddCombatAction(swingType, critical, special, outName, theAttackType, int.Parse(damage), logInfo.detectedTime, num14, incName, string.Empty);
                    if (flag2)
                    {
                        logInfo.detectedType = Color.Green.ToArgb();
                    }
                }
            }
            else if ((str.IndexOf("restored") != -1) && (str.IndexOf(" HP.") != -1))
            {
                outName = str.Substring(0, str.IndexOf("restored") - 1);
                outName = this.CheckYou(outName);
                incName = outName;
                incName = this.CheckYou(incName);
                swingType = 3;
                theAttackType = "Heal";
                damage = str.Substring(str.IndexOf("restored") + 9, str.IndexOf(" HP.") - (str.IndexOf("restored") + 9));
                damage = this.DamageNumberFix(damage);
                if (ActGlobals.oFormActMain.InCombat)
                {
                    int num15;
                    ActGlobals.oFormActMain.GlobalTimeSorter = (num15 = ActGlobals.oFormActMain.GlobalTimeSorter) + 1;
                    ActGlobals.oFormActMain.AddCombatAction(swingType, critical, special, outName, theAttackType, int.Parse(damage), logInfo.detectedTime, num15, incName, string.Empty);
                    if (flag2)
                    {
                        logInfo.detectedType = Color.Green.ToArgb();
                    }
                }
            }
            else if (((str.IndexOf("recovered") != -1) && (str.IndexOf("HP because") != -1)) && (str.IndexOf(" used ") != -1))
            {
                incName = str.Substring(0, str.IndexOf("recovered") - 1);
                incName = this.CheckYou(incName);
                outName = str.Substring(str.IndexOf("HP because") + 11, str.IndexOf(" used ") - (str.IndexOf("HP because") + 11));
                outName = this.CheckYou(outName);
                swingType = 3;
                theAttackType = str.Substring(str.IndexOf(" used ") + 6, (str.Length - (str.IndexOf(" used ") + 6)) - 2);
                damage = str.Substring(str.IndexOf("recovered") + 10, (str.IndexOf("HP because") - (str.IndexOf("recovered") + 10)) - 1);
                damage = this.DamageNumberFix(damage);
                if (ActGlobals.oFormActMain.InCombat)
                {
                    int num16;
                    ActGlobals.oFormActMain.GlobalTimeSorter = (num16 = ActGlobals.oFormActMain.GlobalTimeSorter) + 1;
                    ActGlobals.oFormActMain.AddCombatAction(swingType, critical, special, outName, theAttackType, int.Parse(damage), logInfo.detectedTime, num16, incName, string.Empty);
                    if (flag2)
                    {
                        logInfo.detectedType = Color.Green.ToArgb();
                    }
                }
            }
            else if ((str.IndexOf("recovered") != -1) && (str.IndexOf(" MP ") != -1))
            {
                incName = str.Substring(0, str.IndexOf("recovered") - 1);
                incName = this.CheckYou(incName);
                outName = incName;
                damage = str.Substring(str.IndexOf("recovered") + 10, str.IndexOf(" MP ") - (str.IndexOf("recovered") + 10));
                damage = this.DamageNumberFix(damage);
                swingType = 13;
                if ((str.IndexOf("MP due to the effect of") != -1) && (str.IndexOf(" Effect. ") != -1))
                {
                    theAttackType = str.Substring(str.IndexOf("MP due to the effect of") + 0x19, str.IndexOf(" Effect. ") - (str.IndexOf("MP due to the effect of") + 0x19));
                }
                else if (str.IndexOf("MP due to the effect of") != -1)
                {
                    theAttackType = str.Substring(str.IndexOf("MP due to the effect of") + 0x19, (str.Length - (str.IndexOf("MP due to the effect of") + 0x19)) - 2);
                }
                else
                {
                    theAttackType = str.Substring(str.IndexOf("MP by using") + 12, (str.Length - (str.IndexOf("MP by using") + 12)) - 2);
                }
                if (ActGlobals.oFormActMain.InCombat)
                {
                    int num17;
                    ActGlobals.oFormActMain.GlobalTimeSorter = (num17 = ActGlobals.oFormActMain.GlobalTimeSorter) + 1;
                    ActGlobals.oFormActMain.AddCombatAction(swingType, critical, special, outName, theAttackType, int.Parse(damage), logInfo.detectedTime, num17, incName, string.Empty);
                    if (flag2)
                    {
                        logInfo.detectedType = Color.Cyan.ToArgb();
                    }
                }
            }
            else if ((str.IndexOf("restored") != -1) && (str.IndexOf(" MP.") != -1))
            {
                incName = str.Substring(0, str.IndexOf("restored") - 1);
                incName = this.CheckYou(incName);
                outName = incName;
                damage = str.Substring(str.IndexOf("restored") + 9, str.IndexOf(" MP.") - (str.IndexOf("restored") + 9));
                damage = this.DamageNumberFix(damage);
                swingType = 13;
                theAttackType = "Unknown";
                if (ActGlobals.oFormActMain.InCombat)
                {
                    int num18;
                    ActGlobals.oFormActMain.GlobalTimeSorter = (num18 = ActGlobals.oFormActMain.GlobalTimeSorter) + 1;
                    ActGlobals.oFormActMain.AddCombatAction(swingType, critical, special, outName, theAttackType, int.Parse(damage), logInfo.detectedTime, num18, incName, string.Empty);
                    if (flag2)
                    {
                        logInfo.detectedType = Color.Cyan.ToArgb();
                    }
                }
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
                outName = str.Substring(str.IndexOf(" resisted ") + 10, str.IndexOf("'s ") - (str.IndexOf("resisted") + 10));
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
            else if ((str.IndexOf("resisted") != -1) && (str.IndexOf(". ") != -1))
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
            else if ((str.IndexOf("evaded") != -1) && (str.IndexOf("'s ") != -1))
            {
                incName = str.Substring(0, str.IndexOf(" evaded "));
                incName = this.CheckYou(incName);
                outName = str.Substring(str.IndexOf(" evaded ") + 7, str.IndexOf("'s ") - (str.IndexOf("evaded") + 7));
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
                    ActGlobals.oFormActMain.AddCombatAction(1, critical, special, outName, "Melee", new Dnum((int)Dnum.Unknown, "evaded"), logInfo.detectedTime, num25, incName, string.Empty);
                    if (flag2)
                    {
                        logInfo.detectedType = Color.Yellow.ToArgb();
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
            else
            {
                //throw new Exception("Did not match: " + str);
            }
        }


        private void SetEncounter(LogLineEventArgs logInfo, string attacker, string victim, string theAttackType, bool critical, string special, string damage, SwingTypeEnum swingType, bool flag)
        {
            if (ActGlobals.oFormActMain.SetEncounter(logInfo.detectedTime, attacker, victim))
            {
                int num8;
                ActGlobals.oFormActMain.GlobalTimeSorter = (num8 = ActGlobals.oFormActMain.GlobalTimeSorter) + 1;
                ActGlobals.oFormActMain.AddCombatAction((int)swingType, critical, special, attacker, theAttackType, int.Parse(damage), logInfo.detectedTime, num8, victim, string.Empty);
                if (flag)
                {
                    logInfo.detectedType = Color.Red.ToArgb();
                }
            }
        }

        private void SetEncounterSpecial(LogLineEventArgs logInfo, string attacker, string victim, string theAttackType, bool critical, string special, string damage, SwingTypeEnum swingType1, SwingTypeEnum swingType2, bool flag)
        {
            if (ActGlobals.oFormActMain.SetEncounter(logInfo.detectedTime, attacker, victim))
            {
                int num4;
                int num5;
                ActGlobals.oFormActMain.GlobalTimeSorter = (num4 = ActGlobals.oFormActMain.GlobalTimeSorter) + 1;
                ActGlobals.oFormActMain.AddCombatAction((int)swingType1, critical, special, attacker, theAttackType, new Dnum(0, special.ToLower()), logInfo.detectedTime, num4, victim, string.Empty);
                ActGlobals.oFormActMain.GlobalTimeSorter = (num5 = ActGlobals.oFormActMain.GlobalTimeSorter) + 1;
                ActGlobals.oFormActMain.AddCombatAction((int)swingType2, critical, special, attacker, theAttackType, int.Parse(damage), logInfo.detectedTime, num5, victim, string.Empty);
                if (flag)
                {
                    logInfo.detectedType = Color.Red.ToArgb();
                }
            }
        }

        private DateTime ParseDateTime(string FullLogLine)
        {
            string str = FullLogLine.Substring(0, 4) + "-" + FullLogLine.Substring(5, 2) + FullLogLine.Substring(8, 2);
            string str2 = FullLogLine.Substring(11, 8);
            return DateTime.ParseExact(str + "-" + str2, "yyyy-MMdd-HH:mm:ss", CultureInfo.InvariantCulture);
        }
    }
}

