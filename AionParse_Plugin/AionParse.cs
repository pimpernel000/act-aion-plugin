namespace AionParsePlugin
{
    using System;
    using System.Text.RegularExpressions;
    using Advanced_Combat_Tracker;

    /* 
     * TODO: test results:
     *  - proc for Promise of Wind only works for Unknown (Chanter/Priest) if you check Guess damage proc owners
     *    - this should be moved out of damage list as it should work regardless
     *  - proc for heals don't work for Unknown (Chanter/Cleric) as it doesn't check GetAnyActor, 
     *    - should this be moved out? or should GetAnyActor be the default behaviour?
     *      -i.e. if Target == null, it means you've reached the end of the list anyways, you might as well match.
     * 
     * TODO: test to make sure it works with a chanter in the party
     * 
     * TODO: make it work with ACT Selective Parsing (Combat Limiting toggle and listed combatants)
     * 
     * TODO: rename continuousdamageset to indirectdamageset
     * TODO: have continuousdamageset also contain skill specific durations.. some spells like Delayed Blast only lasts 2 seconds, others like Word of Life lasts 10 seconds, Spirit Erosion lasts 30 seconds.
     *        -when looking back on the list, do so with skill specific durations in mind
     * 
     * TODO: have a UI option that saves your party member's class info while in dungeons
     *         -also determine the class base on their skills
     *         -add people into your party? how the heck do you know who's in your party???
     *         
     * TODO: summoners using skills for thier pets can be indicators of the owner of the pet. (see rSummonerSkill)
     *         
     * TODO: things to parse:
     *  
     * Resists to you (and maybe others?)   TODO: put resists in their class specific Unknown (class)... and perhaps in the future, give option to specify name... i.e. all Unknown (Sorcerer) will be defaulted to MyDefaultName
     *  Alpine Gorgon resisted Chastisement I.
     *  
     * Poisoning by unknown assassins (NOTE: ACT already has a feature for Combatant Rename... so users can just rename Unknown (Assassin) to your party's assassin.)
     *   (NOTE: it seems that Apply Poison doesn't have a "became poisoned" message when it procs, so logs alone cannot determine caster)
     *   You received 51 poisoning damage due to the effect of Apply Poison II Effect. 
     * 
     * Healing Holy Servants (TODO: need to parse this as healing but also put an option to not parse this as it is pet healing) (Need more data, does healing SM's pets also look like this?)
     *   Vyrana has caused Holy Servant to recover HP over time by using Light of Rejuvenation II. 
     *   
     * Bodyguard transfering damage  (label damage taken as redirected damage?)
     *  Brutal Mist Mane Bodyguard received 577 damage inflicted on Brutal Mist Mane Dark Mage by Ikite. because of the protection effect cast on it.
     *  Ione received 562 damage inflicted on Azshadela by Brutal Mist Mane Scratcher. because of the protection effect cast on it. 
     *  
     * Mantras by Chanters (does turning off mantras show up on logs? TODO: have a UI option that saves mantra casters while in dungeons.)
     *  Jessex started using Clement Mind Mantra II. 
     *  You recovered 24 MP due to the effect of Clement Mind Mantra II Effect. 
     *  Ione recovered 24 MP due to the effect of Clement Mind Mantra II Effect. 
     *  You began using Celerity Mantra I.
     * Skills not yet implemented:
     *  Draining Blow by Gladiators (this heals)
     *  
     * Poison Slash... is this a godstone effect?
     *  Hungry Karnif's attack speed has decreased because Bip used Booming Smash II.
     *  Hungry Karnif received 10 poisoning damage after you used Poison Slash. 
     *  
     * TODO: determine if Wind Cut Down has a secondary Magical Wind Damage effect?! (Most likely, it is the effect of a godstone.)
     * 2010.01.31 19:19:08 : Matteous inflicted 1,393 damage on Brutal Mist Mane Pawsoldier by using Wind Cut Down II. 
     * 2010.01.31 19:19:08 : Brutal Mist Mane Pawsoldier is bleeding because Matteous used Wind Cut Down II.
     * 2010.01.31 19:19:13 : Brutal Mist Mane Pawsoldier received 338 bleeding damage after you used Wind Cut Down II. 
     * 2010.01.31 19:19:16 : Brutal Mist Mane Pawsoldier received 338 bleeding damage after you used Wind Cut Down II. 
     * 2010.01.31 19:19:16 : Brutal Mist Mane Pawsoldier received 72 damage due to the effect of Magical Wind Damage Effect. 
     * 
     * 
     * NOTE: currently, resists assume that attacker can have a "'s" in their name, and that is higher priority than "'s" in skill names. A list of possible player skill names are hardcoded
     *   in this parser and the attacker name will be corrected.  However, if there are many more skill names that have "'s" (i.e. mob skills), we might change the regex instead to exclude
     *   "'s" from the attacker's name, and then we hardcode a list of attackers that have "'s" in their name.
     * 
     * TODO: Implement handling of Selective Parsing tab in ACT so that you can parse your own party members only
     * 
     * TODO: use ActGlobals.oFormActMain.SetEncounters and a timer to handle sleep durations that last 20 seconds without any combat activity.
     *   - EQAditu mentions SetEncounter() combined with LastEstimatedTime
     *   - he also suggests OnLogLineRead event to set encounters
     */

    public partial class AionParse : IActPluginV1, IDisposable
    {
        #region regex
        static string ngs = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator;
        Regex rInflictDamageRuneCarve = new Regex(@"^(?<attacker>[a-zA-Z ']*) inflicted (?<damage>(\d+" + ngs + @")?\d+) (?<critical>critical )?damage and the (rune carve|pattern engraving) effect on (?<victim>[a-zA-Z ']*) by using (?<skill>[a-zA-Z \-']*)\.$", RegexOptions.Compiled);
        Regex rInflictDamage = new Regex(@"^(?<attacker>[a-zA-Z ']*?)( has)? inflicted (?<damage>(\d+" + ngs + @")?\d+) (?<critical>critical )?damage on (?<targetclause>[a-zA-Z \-']*)\.$", RegexOptions.Compiled);
        Regex rUsingAttack = new Regex(@"^(?<victimclause>[a-zA-Z ']*) by using (?<skill>[a-zA-Z \-']*)$", RegexOptions.Compiled);
        Regex rPatternEngraving = new Regex(@"^(?<victim>[a-zA-Z ']*) and caused the (?<statuseffect>[a-zA-Z ']*) effect$", RegexOptions.Compiled);
        Regex rAndDispelled = new Regex(@"^(?<victim>[a-zA-Z ']*) and dispelled some of its magical buffs by using (?<skill>[a-zA-Z \-']*)$", RegexOptions.Compiled); // only for Ignite Aether spell
        Regex rReflect = new Regex(@"^(?<victim>[a-zA-Z ']*) by reflecting the attack$", RegexOptions.Compiled);
        Regex rReceiveDamage = new Regex(@"^(?<victim>[a-zA-Z ']*) received (?<damage>(\d+" + ngs + @")?\d+) damage from (?<attacker>[a-zA-Z ']*)\.$", RegexOptions.Compiled);
        Regex rReceiveEffect = new Regex(@"^(?<victim>[a-zA-Z ']*) received the (?<statuseffect>[a-zA-Z ']*) effect (as (?<attacker>you)|because (?<attacker>[a-zA-Z ']*)) used (?<skill>[a-zA-Z \-']*)\.$", RegexOptions.Compiled); // only for Delayed Blast spell
        Regex rStatusEffect1 = new Regex(@"^(?<victim>[a-zA-Z ']*) became poisoned because (?<attacker>[a-zA-Z ']*) used (?<skill>[a-zA-Z \-']*)\.$", RegexOptions.Compiled); // NOTE: there are other status effects (see comments below), but we're only interested in damage
        Regex rStatusEffect2 = new Regex(@"^(?<victim>[a-zA-Z ']*) is bleeding because (?<attacker>[a-zA-Z ']*) used (?<skill>[a-zA-Z \-']*)\.$", RegexOptions.Compiled); // NOTE: there are other status effects (see comments below), but we're only interested in damage
        Regex rStatusEffectByYou1 = new Regex(@"^(?<attacker>You) caused (?<victim>[a-zA-Z ']*) to become poisoned by using (?<skill>[a-zA-Z \-']*)\.$", RegexOptions.Compiled); // TODO: confirm this regex as this is a total guess on my part
        Regex rStatusEffectByYou2 = new Regex(@"^(?<attacker>You) caused (?<victim>[a-zA-Z ']*) to bleed by using (?<skill>[a-zA-Z \-']*)\.$", RegexOptions.Compiled);
        Regex rStatusEffectToYou1 = new Regex(@"^(?<attacker>[a-zA-Z ']*) poisoned (?<victim>you) by using (?<skill>[a-zA-Z \-']*)\.$", RegexOptions.Compiled);
        Regex rStatusEffectToYou2 = new Regex(@"^(?<attacker>[a-zA-Z ']*) caused (?<victim>you) to bleed by using (?<skill>[a-zA-Z \-']*) on you\.$", RegexOptions.Compiled);
        /*
        Regex rStateAbility = new Regex(@"^(?<target>[a-zA-Z ']*) is in the (?<buff>[a-zA-Z ']*) state (because (?<actor>[a-zA-Z ']*)|as it) used (?<skill>[a-zA-Z \-']*)\.$", RegexOptions.Compiled); // NOTE: the only one we care about is the continuous recovery state caused by Word of Life (the only one I know of); so I put a special handling in the continuous section
        Regex rWeakened = new Regex(@"^(?<actor>[a-zA-Z ']*) has weakened (?<target>[a-zA-Z ']*)'s (?<stat>[a-zA-Z ']*) by using (?<skill>[a-zA-Z \-']*)\.$", RegexOptions.Compiled);
        Regex rStatusEffect1 = new Regex(@"^(?<victim>[a-zA-Z ']*) became (?<statuseffect>[a-zA-Z ']*) because (?<attacker>[a-zA-Z ']*) used (?<skill>[a-zA-Z \-']*)\.$", RegexOptions.Compiled); // i.e. Brutal Mist Mane Tamer became poisoned because Stalker used Poison Arrow II. (also for stunned, snared (by Aether's Hold), snared in mid-air (by Aerial Lockdown), paralyzed, silenced, bound, blinded)
        Regex rStatusEffect2 = new Regex(@"^(?<victim>[a-zA-Z ']*) is (?<statuseffect>[a-zA-Z ']*) because (?<attacker>[a-zA-Z ']*) used (?<skill>[a-zA-Z \-']*)\.$", RegexOptions.Compiled); // i.e. Ione is bleeding because Recondo used Area Cause Wound. (also other effects are: unable to fly and spinning)  NOTE: this also matches the "is in xxx state" so that must be used before this one.
        Regex rStatusEffectByYou1 = new Regex(@"^(?<attacker>You) caused (?<victim>[a-zA-Z ']*) to become (?<statuseffect>[a-zA-Z ']*) by using (?<skill>[a-zA-Z \-']*)\.$", RegexOptions.Compiled); // TODO: confirm this regex as this is a total guess on my part
        Regex rStatusEffectByYou2 = new Regex(@"^(?<attacker>You) caused (?<victim>[a-zA-Z ']*) to (?<statuseffect>[a-zA-Z ']*) by using (?<skill>[a-zA-Z \-']*)\.$", RegexOptions.Compiled);
        Regex rStatusEffectToYou1 = new Regex(@"^(?<attacker>[a-zA-Z ']*) (?<statuseffect>[a-zA-Z ']*) (?<victim>you) by using (?<skill>[a-zA-Z \-']*)\.$", RegexOptions.Compiled);
        Regex rStatusEffectToYou2 = new Regex(@"^(?<attacker>[a-zA-Z ']*) caused (?<victim>you) to (?<statuseffect>[a-zA-Z ']*) by using (?<skill>[a-zA-Z \-']*) on you\.$", RegexOptions.Compiled);
         */
        Regex rActivated = new Regex(@"^(?<skill>[a-zA-Z \-']*) Effect has been activated\.$", RegexOptions.Compiled);
        Regex rContDmg1 = new Regex(@"^You inflicted continuous damage on (?<victim>[a-zA-Z ']*) by using (?<skill>[a-zA-Z \-']*)\.$", RegexOptions.Compiled);
        Regex rContDmg2 = new Regex(@"^(?<attacker>[a-zA-Z ']*) used (?<skill>[a-zA-Z ']*) to inflict the continuous damage effect on (?<victim>[a-zA-Z ']*)\.$", RegexOptions.Compiled);
        Regex rContDmg3 = new Regex(@"^You received continuous damage because (?<attacker>[a-zA-Z ']*) used (?<skill>[a-zA-Z \-']*)\.$", RegexOptions.Compiled); // NOTE: this usually causes log lines to say that you start damaging yourself... i.e. my name is Vyn, but if I am hit by Chastisement in PvP, log lines will say: Vyn inflicted 70 damage on you by using Chastisement I.
        Regex rIndirectDmg1 = new Regex(@"^(?<victim>[a-zA-Z ']*) received (?<damage>(\d+" + ngs + @")?\d+) (?<damagetype>[a-zA-Z]*) damage after you used (?<skill>[a-zA-Z \-']*)( Effect)?\.$", RegexOptions.Compiled);
        Regex rIndirectDmg2 = new Regex(@"^(?<victim>[a-zA-Z ']*) received (?<damage>(\d+" + ngs + @")?\d+) (?<damagetype>[a-zA-Z]* )?damage due to the effect of (?<skill>[a-zA-Z \-']*?)( Effect)?\.$", RegexOptions.Compiled);
        Regex rReflectDamageOnYou = new Regex(@"^Your attack on (?<attacker>[a-zA-Z ']*) was reflected and inflicted (?<damagetype>[a-zA-Z ]*) damage on you\.$", RegexOptions.Compiled);
        Regex rRecoverMP = new Regex(@"^(?<target>[a-zA-Z ']*) recovered (?<mp>(\d+" + ngs + @")?\d+) MP (due to the effect of|by using|after using) (?<skill>[a-zA-Z \-']*?)( Effect)?\.$", RegexOptions.Compiled);
        Regex rRecoverHP = new Regex(@"^(?<target>[a-zA-Z ']*) recovered (?<hp>(\d+" + ngs + @")?\d+) HP (because (?<actor>[a-zA-Z ']*) used|by using) (?<skill>[a-zA-Z \-']*?)\.$", RegexOptions.Compiled);
        Regex rResist = new Regex(@"^(?<victim>[a-zA-Z ']*) resisted ((?<attacker>[a-zA-Z ]*('s[a-zA-Z ]*)?)'s )?(?<skill>[a-zA-Z \-']*?)\.$", RegexOptions.Compiled);  // TODO: should we remove the word "Effect" from the end for traps and holy servant attacks?  need to be consistent.   NOTE: attacker match is a bit more complex to handle a string like "Guy resisted Hirmilden's Tipolid's Animal's Rights"
        /* TODO: use timer based on skill to keep encounter going if these skills are used in case no other combat action is taking place
        Regex rSleep1 = new Regex(@"^(?<victim>[a-zA-Z ']*) fell asleep because (?<attacker>[a-zA-Z ']*) used (?<skill>[a-zA-Z \-']*).", RegexOptions.Compiled);
        Regex rSleep2 = new Regex(@"^(?<attacker>You) put (?<victim>[a-zA-Z ']*) to sleep by using (?<skill>[a-zA-Z \-']*).", RegexOptions.Compiled);
        Regex rSleepEnd = new Regex(@"^(?<victim>[a-zA-Z ']*) woke up.", RegexOptions.Compiled);  // TODO: ignore "You woke up." if you put yourself to sleep via Gain Mana; NOTE: Curse of Roots seem to use "woke up" inconsistently when broken.
        Regex rRoot1 = new Regex(@"^(?<victim>[a-zA-Z ']*) is unable to fly because (?<attacker>[a-zA-Z ']*) used (?<skill>[a-zA-Z \-']*).", RegexOptions.Compiled);
        Regex rRoot2 = new Regex(@"^(?<attacker>You) immobilized (?<victim>[a-zA-Z ']*) by using (?<skill>[a-zA-Z \-']*).", RegexOptions.Compiled);
        Regex rRootEnd = new Regex(@"^(?<victim>[a-zA-Z ']*) is no longer immobilized.", RegexOptions.Compiled);
        Regex rCurseOfRoot1 = new Regex(@"^(?<victim>[a-zA-Z ']*) has transformed into Cursed Tree because (?<attacker>[a-zA-Z ']*) used (?<skill>[a-zA-Z \-']*).", RegexOptions.Compiled);
        Regex rCurseOfRoot2 = new Regex(@"^(?<attacker>You) transformed (?<victim>[a-zA-Z ']*) into Cursed Tree by using (?<skill>[a-zA-Z \-']*).", RegexOptions.Compiled);
         */

        Regex rSummonSpirit = new Regex(@"^(?<summoner>[a-zA-Z ']*) summoned (?<pet>[a-zA-Z ']*) by using (?<skill>[a-zA-Z \-']*?)\.$", RegexOptions.Compiled);
        Regex rSummonServant1 = new Regex(@"^(?<summoner>[a-zA-Z ']*) has summoned (?<pet>[a-zA-Z ']*) to attack (?<victim>[a-zA-Z ']*) by using (?<skill>[a-zA-Z \-']*?)\.$", RegexOptions.Compiled);
        Regex rSummonServant2 = new Regex(@"^(?<summoner>[a-zA-Z ']*) has caused you to summon (?<pet>[a-zA-Z ']*) by using (?<skill>[a-zA-Z \-']*?)\.$", RegexOptions.Compiled);
        Regex rSummonServant3 = new Regex(@"^(?<summoner>You) summoned (?<pet>[a-zA-Z ']*) by using (?<skill>[a-zA-Z \-']*?) to let it attack (?<victim>[a-zA-Z ']*)\.$", RegexOptions.Compiled);  // NOTE: this regex is a subset of rSummonSpirit, so make sure this is matched first before the other
        Regex rSummonerSkill = new Regex(@"^The spirit used a skill on (?<victim>[a-zA-Z ']*) because (?<summoner>[a-zA-Z ']*) used (?<skill>[a-zA-Z \-']*)\.$", RegexOptions.Compiled);

        Regex rProcBuff = new Regex(@"(?<actor>[a-zA-Z ']*) was affected by its own (?<skill>[a-zA-Z \-']*?)\.", RegexOptions.Compiled);

        #endregion

        private void OnCombatEnd(bool isImport, CombatToggleEventArgs encounterInfo)
        {
            lastActivatedSkill = string.Empty;
            lastActivatedSkillGlobalTime = -1;
            lastActivedSkillTime = DateTime.MinValue;

            lastPotion = string.Empty;

            ContinuousDamageSet.Clear();
            BlockedHistory.Clear();
            HealerRecordSet.Clear();
        }

        private void BeforeLogLineRead(bool isImport, LogLineEventArgs logInfo)
        {
            if (this.LastCharName == "YOU" && ActGlobals.charName != "YOU") ui.UpdateDefaultCharacter(ActGlobals.charName);

            string str = logInfo.logLine.Substring(0x16, logInfo.logLine.Length - 0x16).Trim();
            string victim = string.Empty;
            string attacker = string.Empty;
            string damage = string.Empty;
            string damageString = string.Empty;
            string skill = string.Empty;
            string special = string.Empty;
            bool critical = false;

            #region misc parse
            // ignore chats (channels i.e. [3.LFG] or whispers [charname:Drakkon] all start with "[")
            if (str.StartsWith("[") || str.StartsWith("You Whisper to ") || 
                str.StartsWith(CheckYou("you") + ": ") || str.StartsWith(CheckYou("you") + " Whispers: "))
            {
                return;
            }

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
                victim = CheckYou("you");
                attacker = match.Groups["attacker"].Value;
                damage = match.Groups["damage"].Value;
                if (TagBlockedAttacks)
                {
                    string blockType = BlockedHistory.IsBlocked(attacker, victim, logInfo.detectedTime);
                    if (!String.IsNullOrEmpty(blockType))
                        special = blockType + "&";
                }

                special += "reflected";

                // assume: the attack that caused the reflection is recorded on it's own line so we don't have to log an unknown attack
                AddCombatAction(logInfo, attacker, victim, "Damage Shield", critical, special, damage, SwingTypeEnum.NonMelee);
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

                attacker = CheckYou(mInflict.Groups["attacker"].Value); // source
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
                        victim = CheckYou(mPatternEngraving.Groups["victim"].Value);
                        ////special = mPatternEngraving.Groups["statuseffect"].Value;
                    }
                    else
                    {
                        victim = CheckYou(mUsingAttack.Groups["victimclause"].Value);
                    }

                    if (TagBlockedAttacks)
                    {
                        string blockType = BlockedHistory.IsBlocked(attacker, victim, logInfo.detectedTime);
                        if (!String.IsNullOrEmpty(blockType))
                            special = blockType;
                    }

                    skill = mUsingAttack.Groups["skill"].Value;

                    // check if skill has an extra payload damage that can't be found other than in here
                    if (GuessDotCasters)
                    {
                        if (AionData.Skill.HasAdditionalEffect(skill))
                        {
                            string additionalEffect = skill + " Additional";
                            ContinuousDamageSet.Add(attacker, victim, additionalEffect, logInfo.detectedTime); // record Blood Rune actor for when it deals payload damage later as Blood Rune X Additional Effect (regex will parse out Effect)
                            HealerRecordSet.Add(attacker, victim, additionalEffect, logInfo.detectedTime); // record Blood Rune actor when he heals later as single HoT tick
                        }
                    }

                    var inflictSwingType = SwingTypeEnum.NonMelee;

                    // correct the false damage on you that are actually heals
                    if (victim == CheckYou("you") && AionData.Skill.IsHealThatInflictsDamage(skill))
                    {
                        inflictSwingType = SwingTypeEnum.Healing;
                    }

                    // correct the false self damage message that are actually continuous damage from others to you
                    if (victim == "you" && attacker == CheckYou("you"))
                    {
                        string realAttacker = ContinuousDamageSet.GetActor(victim, skill, logInfo.detectedTime);
                        if (!String.IsNullOrEmpty(realAttacker))
                            attacker = realAttacker;
                    }

                    AddCombatAction(logInfo, attacker, victim, skill, critical, special, damage, inflictSwingType);
                    return;
                }

                // submatch "and dispelled buffs using Ignite Aether"
                var mIgniteAether = rAndDispelled.Match(targetClause);
                if (mIgniteAether.Success)
                {
                    victim = CheckYou(mIgniteAether.Groups["victim"].Value);
                    if (TagBlockedAttacks)
                    {
                        string blockType = BlockedHistory.IsBlocked(attacker, victim, logInfo.detectedTime);
                        if (!String.IsNullOrEmpty(blockType))
                            special = blockType;
                    }

                    skill = mIgniteAether.Groups["skill"].Value;
                    AddCombatAction(logInfo, attacker, victim, skill, critical, string.Empty, Dnum.NoDamage, SwingTypeEnum.CureDispel);
                    AddCombatAction(logInfo, attacker, victim, skill, critical, special, damage, SwingTypeEnum.NonMelee);
                    return;
                }

                // submatch "reflecting the attack"
                var mReflect = rReflect.Match(targetClause);
                if (mReflect.Success)
                {
                    special = "reflected";
                    victim = CheckYou(mReflect.Groups["victim"].Value);
                    if (TagBlockedAttacks)
                    {
                        string blockType = BlockedHistory.IsBlocked(attacker, victim, logInfo.detectedTime);
                        if (!String.IsNullOrEmpty(blockType))
                            special = blockType;
                    }

                    if (ActGlobals.oFormActMain.GlobalTimeSorter == lastActivatedSkillGlobalTime || (logInfo.detectedTime - lastActivedSkillTime).TotalSeconds < 2)
                    {
                        skill = lastActivatedSkill;
                    }
                    else
                    {
                        skill = "Damage Shield";
                    }

                    AddCombatAction(logInfo, attacker, victim, skill, critical, special, damage, SwingTypeEnum.NonMelee);
                    return;
                }

                // no ability submatch
                victim = CheckYou(targetClause);
                if (TagBlockedAttacks)
                {
                    string blockType = BlockedHistory.IsBlocked(attacker, victim, logInfo.detectedTime, false); // block record consume set to false because auto-attacks can be multi-hitting, and multiple attacks can be blocked
                    if (!String.IsNullOrEmpty(blockType))
                    {
                        special = blockType;
                        ////damageString = blockType; // nah, I don't want to cover the numbers with damageString
                    }
                }

                AddCombatAction(logInfo, attacker, victim, "Melee", critical, special, NewDnum(damage, damageString), SwingTypeEnum.Melee);
                return;
            }

            // match "xxx inflicted xxx damage and the rune carve effect on xxx by using xxx ."  (assassin rune abilities)
            var mInflictDamageRuneCarve = rInflictDamageRuneCarve.Match(str);
            if (mInflictDamageRuneCarve.Success)
            {
                attacker = CheckYou(mInflictDamageRuneCarve.Groups["attacker"].Value);
                victim = CheckYou(mInflictDamageRuneCarve.Groups["victim"].Value);
                ////special = "pattern engraving";
                damage = mInflictDamageRuneCarve.Groups["damage"].Value;
                skill = mInflictDamageRuneCarve.Groups["skill"].Value;
                critical = mInflictDamageRuneCarve.Groups["critical"].Success;
                if (TagBlockedAttacks)
                {
                    string blockType = BlockedHistory.IsBlocked(attacker, victim, logInfo.detectedTime, false); // block record consume set to false because auto-attacks can be multi-hitting, and multiple attacks can be blocked
                    if (!String.IsNullOrEmpty(blockType))
                        special = blockType;
                }

                AddCombatAction(logInfo, attacker, victim, skill, critical, special, damage, SwingTypeEnum.NonMelee);
                return;
            }

            #endregion

            #region indicator parsers
            // match "You have used xxx Potion."
            if (str.StartsWith("You have used") && str.EndsWith("Potion."))
            {
                Match match = (new Regex("You have used (?<potion>[a-zA-Z ']*).", RegexOptions.Compiled)).Match(str);
                lastPotion = match.Groups["potion"].Value;
                return;
            }

            // match "xxx has been activated." for use in damage shields like Robe of Cold
            if (rActivated.IsMatch(str))
            {
                if (!GuessDotCasters) return;

                Match match = rActivated.Match(str);
                lastActivatedSkill = match.Groups["skill"].Value;
                lastActivatedSkillGlobalTime = ActGlobals.oFormActMain.GlobalTimeSorter;
                lastActivedSkillTime = logInfo.detectedTime;
                return;
            }

            #region continuous
            if (str.Contains("continuous"))
            {
                Match contDmgMatch = null;

                // match "You inflicted continuous damage on xxx by using xxx."
                if (rContDmg1.IsMatch(str))
                {
                    contDmgMatch = rContDmg1.Match(str);
                    attacker = CheckYou("you");
                    victim = contDmgMatch.Groups["victim"].Value;
                }

                // match "xxx used xxx to inflict continuous damage effect on xxx."
                if (rContDmg2.IsMatch(str))
                {
                    contDmgMatch = rContDmg2.Match(str);
                    attacker = contDmgMatch.Groups["attacker"].Value;
                    victim = contDmgMatch.Groups["victim"].Value;
                }

                // match "You received continuous damage because xxx used xxx."
                if (rContDmg3.IsMatch(str))
                {
                    contDmgMatch = rContDmg3.Match(str);
                    attacker = CheckYou("you");
                    victim = contDmgMatch.Groups["victim"].Value;
                }

                if (contDmgMatch != null)
                {
                    skill = contDmgMatch.Groups["skill"].Value;
                    if (GuessDotCasters)
                        ContinuousDamageSet.Add(attacker, victim, skill, logInfo.detectedTime);
                    return;
                }

                Match contHPMatch = null; // continuous heals like Word of Life or Light of Rejuvenation

                // match "xxx is in the continuous HP recovery state because he/xxx used xxx."
                Regex rContHP = new Regex(@"^(?<victim>[a-zA-Z ']*) is in the continuous HP recovery state because (?<attacker>[a-zA-Z ']*) used (?<skill>[a-zA-Z \-']*)\.$", RegexOptions.Compiled);
                if (rContHP.IsMatch(str))
                {
                    contHPMatch = rContHP.Match(str);
                    victim = contHPMatch.Groups["victim"].Value;
                    attacker = contHPMatch.Groups["attacker"].Value;
                    if (attacker == "he" || attacker == "she") attacker = victim;
                }

                // match "xxx is continuously restoring your HP by using xxx."
                Regex rContHPYou = new Regex(@"^(?<attacker>[a-zA-Z ']*) is continuously restoring your HP by using (?<skill>[a-zA-Z \-']*)\.$", RegexOptions.Compiled);
                if (rContHPYou.IsMatch(str))
                {
                    contHPMatch = rContHPYou.Match(str);
                    victim = CheckYou("you");
                    attacker = contHPMatch.Groups["attacker"].Value;
                }

                // match "You are continuously recovering HP because of xxx." (cast HoT on yourself)
                Regex rContHPSelf = new Regex(@"^You are continuously recovering HP because of (?<skill>[a-zA-Z \-']*)\.$", RegexOptions.Compiled);
                if (rContHPSelf.IsMatch(str))
                {
                    contHPMatch = rContHPSelf.Match(str);
                    victim = CheckYou("you");
                    attacker = victim;
                }

                if (contHPMatch != null)
                {
                    skill = contHPMatch.Groups["skill"].Value;
                    HealerRecordSet.Add(attacker, victim, skill, logInfo.detectedTime);
                    return;
                }
            }
            #endregion

            #region poisoned
            if (str.Contains("poisoned"))
            {
                Match poisonMatch = null;

                // match "xxx became poisoned because xxx used xxx."
                if (rStatusEffect1.IsMatch(str))
                    poisonMatch = rStatusEffect1.Match(str);

                // match "You caused xxx to become poisoned by using xxx."
                if (rStatusEffectByYou1.IsMatch(str))
                    poisonMatch = rStatusEffectByYou1.Match(str);

                // match "xxx poisoned you by using xxx."
                if (rStatusEffectToYou1.IsMatch(str))
                    poisonMatch = rStatusEffectToYou1.Match(str);

                if (poisonMatch != null && poisonMatch.Success)
                {
                    if (GuessDotCasters)
                    {
                        attacker = CheckYou(poisonMatch.Groups["attacker"].Value);
                        victim = CheckYou(poisonMatch.Groups["victim"].Value);
                        skill = poisonMatch.Groups["skill"].Value;
                        ContinuousDamageSet.Add(attacker, victim, skill, logInfo.detectedTime);
                    }

                    return;
                }
            }
            #endregion

            #region bleed
            if (str.Contains("bleed"))
            {
                Match bleedMatch = null;

                // match "xxx is bleeding because xxx used xxx."
                if (rStatusEffect2.IsMatch(str))
                    bleedMatch = rStatusEffect2.Match(str);

                // match "You caused xxx to bleed by using xxx."
                if (rStatusEffectByYou2.IsMatch(str))
                    bleedMatch = rStatusEffectByYou2.Match(str);

                // match "xxx caused you to bleed by using xxx."
                if (rStatusEffectToYou2.IsMatch(str))
                    bleedMatch = rStatusEffectToYou2.Match(str);

                if (bleedMatch != null && bleedMatch.Success)
                {
                    if (GuessDotCasters)
                    {
                        attacker = CheckYou(bleedMatch.Groups["attacker"].Value);
                        victim = CheckYou(bleedMatch.Groups["victim"].Value);
                        skill = bleedMatch.Groups["skill"].Value;
                        ContinuousDamageSet.Add(attacker, victim, skill, logInfo.detectedTime);
                    }

                    return;
                }
            }
            #endregion

            // match "xxx received the xxx effect because xxx used xxx"  occurs when you use Delayed Blast
            if (rReceiveEffect.IsMatch(str))
            {
                Match match = rReceiveEffect.Match(str);
                attacker = CheckYou(match.Groups["attacker"].Value);
                victim = match.Groups["victim"].Value;
                skill = match.Groups["skill"].Value;
                if (GuessDotCasters)
                    ContinuousDamageSet.Add(attacker, victim, skill, logInfo.detectedTime);
                ActGlobals.oFormActMain.SetEncounter(logInfo.detectedTime, attacker, victim);
                ////AddCombatAction(logInfo, attacker, victim, skill, critical, special, new Dnum((int)Dnum.Unknown, "effect"), SwingTypeEnum.NonMelee);
                return;
            }

            #region summon
            if (str.Contains("summon"))
            {
                Match summonMatch = null;
                int petDuration = 30;

                if (rSummonServant1.IsMatch(str))
                {
                    summonMatch = rSummonServant1.Match(str); // xxx has summoned pet to attack xxx by using skill
                    victim = summonMatch.Groups["victim"].Value;
                }
                else if (rSummonServant2.IsMatch(str))
                {
                    summonMatch = rSummonServant2.Match(str); // xxx has caused you to summon pet by using skill
                    victim = CheckYou("you");
                }
                else if (rSummonServant3.IsMatch(str))
                {
                    summonMatch = rSummonServant3.Match(str); // You summoned xx by using skill to let it attack xxx
                    victim = summonMatch.Groups["victim"].Value;
                }
                else if (rSummonSpirit.IsMatch(str))
                {
                    if (LinkPets)
                    {
                        summonMatch = rSummonSpirit.Match(str); // xxx summoned pet by using skill
                        victim = null;
                        petDuration = 600;
                    }
                    else
                    {
                        return; // ignore SM's summons
                    }
                }

                if (summonMatch != null && summonMatch.Success)
                {
                    string summoner = CheckYou(summonMatch.Groups["summoner"].Value);
                    string pet = summonMatch.Groups["pet"].Value;
                    skill = summonMatch.Groups["skill"].Value;

                    if (AionData.Pet.PetDurations.ContainsKey(pet))
                        petDuration = AionData.Pet.PetDurations[pet];

                    SummonerRecordSet.Add(summoner, victim, skill, pet, logInfo.detectedTime, petDuration);

                    if (!string.IsNullOrEmpty(victim))
                    {
                        ActGlobals.oFormActMain.SetEncounter(logInfo.detectedTime, summoner, victim);
                    }

                    return;
                }

                if (this.DebugParse)
                    ui.AddText("NO MATCH on summon: " + str);
            }
            #endregion

            #region spirit
            // match "The spirit used a skill on xtargetx because xsummonerx used xskillx." which indicates later one of the following:
            // "xElementalx Spirit inflicted xxx damage on xtargetx by using xskillx xelementx." i.e. Thunderbolt Claw I -> Water Spirit uses Thunderbolt Claw I Water
            // "xElementalx Spirit used xskillx xelementx to inflict the continuous damage effect on xtargetx." i.e. Spirit Erosion I -> Wind Spirit uses Spirit Erosion III Wind.
            // "xElementalx Spirit is in the boost xbuffx state because xElementalx Spirit used xskillx xelementx." i.e. Spirit Wrath Position I -> Fire Spirit is in Physical and Magical attack state using Spirit Wrath Position III Fire 
            // "xElementalx Spirit has cast a reflector on you by using Spirit Wall of Protection III xelementx."
            // NOTE: these skills can be resisted.
            if (rSummonerSkill.IsMatch(str))
            {
                // TODO: use indicator to catch owner of pet.  Store the skill so that the next pet that uses a matching skill will have that pet assigned to the summoner.
                // NOTE: the skill used by the summoner (i.e. Spirit Erosion I) will not exactly match the skill of the spirit (i.e. Spirit Erosion III Wind)
                return;
            }
            #endregion

            #region buffSelf
            // match "xxx was affected by its own xxx."
            if (rProcBuff.IsMatch(str))
            {
                Match match = rProcBuff.Match(str);
                string actor = match.Groups["actor"].Value;
                skill = match.Groups["skill"].Value;
                string playerSkill = AionData.Skill.PlayerSkill(skill);

                if (LinkBOFtoSM && playerSkill == "Blessing of Fire")
                {
                    ContinuousDamageSet.Add(actor, null, skill, logInfo.detectedTime, 10 * 60);
                    return;
                }

                if (LinkDmgProcs)
                {
                    if (playerSkill == "Promise of Wind")
                    {
                        ContinuousDamageSet.Add(actor, null, skill, logInfo.detectedTime, 30 * 60);
                        return;
                    }

                    if (playerSkill == "Apply Poison" || skill == "Apply Deadly Poison")
                    {
                        ContinuousDamageSet.Add(actor, null, skill, logInfo.detectedTime, 2 * 60);
                        return;
                    }
                }

                if (playerSkill == "Promise of Aether")
                {
                    PartyMembers.SetClass(actor, AionData.Player.Classes.Chanter);
                    HealerRecordSet.Add(actor, null, skill, logInfo.detectedTime, 30 * 60);
                    return;
                }

                if (AionData.Skill.IsGainMantra(skill))
                {
                    if (PartyMembers.Find(actor) != null)
                    {
                        PartyMembers.SetClass(actor, AionData.Player.Classes.Chanter);
                        foreach (AionData.Player player in PartyMembers)
                        {
                            HealerRecordSet.Add(actor, player.Name, skill, logInfo.detectedTime, 0); // add every party member as receiving mantra
                        }
                    }
                    else
                    {
                        HealerRecordSet.Add(actor, actor, skill, logInfo.detectedTime, 0); // if not in party, assume chanter is soling
                    }
                }

                // ignore non-damage and non-heal spells
                foreach (string otherSkill in new string[] { "Promise of Earth", "Arrow Flurry", "Blessing of Rock", "Tactical Retreat", "Robe of Cold" })
                {
                    if (playerSkill == otherSkill)
                        return;
                }

                // ignore mob and other unhandled spells
                if (DebugParse)
                {
                    ui.AddText("Unhandled self buff: " + str);
                }

                return;
            }
            #endregion

            #endregion

            #region continuous/extra damage from specific skills
            // match "xxx received xxx damage due to the effect of xxx"
            if (rIndirectDmg2.IsMatch(str))
            {
                Match match = rIndirectDmg2.Match(str);
                victim = CheckYou(match.Groups["victim"].Value);
                damage = match.Groups["damage"].Value;
                skill = match.Groups["skill"].Value;

                if (LinkBOFtoSM || LinkDmgProcs)
                    attacker = ContinuousDamageSet.GetAnyActor(victim, skill, logInfo.detectedTime);
                else
                    attacker = ContinuousDamageSet.GetActor(victim, skill, logInfo.detectedTime);

                if (String.IsNullOrEmpty(attacker))
                {
                    attacker = "Unknown";
                }

                if (TagBlockedAttacks)
                {
                    string blockType = BlockedHistory.IsBlocked(attacker, victim, logInfo.detectedTime);
                    if (!String.IsNullOrEmpty(blockType))
                        special = blockType + "&";
                }
                ////special += "DoT";
                AddCombatAction(logInfo, attacker, victim, skill, critical, special, damage, SwingTypeEnum.NonMelee);
                return;
            }

            // match "xxx recieved xxx yyy damage after you used xxx" 
            if (rIndirectDmg1.IsMatch(str))
            {
                Match match = rIndirectDmg1.Match(str);
                victim = match.Groups["victim"].Value;
                damage = match.Groups["damage"].Value;
                string damageType = match.Groups["damagetype"].Value;
                skill = match.Groups["skill"].Value; // only DoT skills: Poison, Poison Arrow, or Wind Cut Down skills match this... often mob skills
                attacker = ContinuousDamageSet.GetActor(victim, skill, logInfo.detectedTime);
                if (String.IsNullOrEmpty(attacker))
                {
                    /*
                    if (skill.StartsWith("Wind Cut Down"))
                    {
                        attacker = "Unknown (Sorcerer)";
                    }
                    else if (skill.StartsWith("Slash Artery"))
                    {
                        attacker = "Unknown (Templar)";
                    }
                    else if (skill.StartsWith("Apply Poison") || skill.StartsWith("Poison Slash")) // not sure, is Poison Slash an Assassin ability?!?
                    {
                        attacker = "Unknown (Assassin)";
                    }
                    else if (skill.StartsWith("Poison Arrow") || skill.StartsWith("Poisoning Trap"))
                    {
                        attacker = "Unknown (Ranger)";
                    }
                    else
                    {
                        attacker = "Unknown"; // unknown class abilities are: Poison, Poison Slash (assassin?), Bleeding (spiritmaster?)
                    }
                     */
                    attacker = "Unknown";
                }

                if (TagBlockedAttacks)
                {
                    string blockType = BlockedHistory.IsBlocked(attacker, victim, logInfo.detectedTime, false); // block record consume set to false because auto-attacks can be multi-hitting, and multiple attacks can be blocked
                    if (!String.IsNullOrEmpty(blockType))
                        special = blockType + "&";
                }

                special += "special";
                AddCombatAction(logInfo, attacker, victim, skill, critical, special, damage, SwingTypeEnum.NonMelee, damageType);
                return;
            }
            #endregion

            #region melee attack
            // match "xxx received xxx damage from xxx."  (basic melee attack?)
            if (rReceiveDamage.IsMatch(str))
            {
                Match match = rReceiveDamage.Match(str);
                attacker = match.Groups["attacker"].Value;
                victim = CheckYou(match.Groups["victim"].Value);
                damage = match.Groups["damage"].Value;
                if (TagBlockedAttacks)
                {
                    string blockType = BlockedHistory.IsBlocked(attacker, victim, logInfo.detectedTime, false); // block record consume set to false because auto-attacks can be multi-hitting, and multiple attacks can be blocked
                    if (!String.IsNullOrEmpty(blockType))
                        special = blockType;
                }

                AddCombatAction(logInfo, attacker, victim, "Melee", critical, string.Empty, damage, SwingTypeEnum.Melee);
                return;
            }
            #endregion

            #region hp/mp heals
            if (ActGlobals.oFormActMain.InCombat)
            {
                // match "You restored xx of xxx's HP by using xxx."  the actor in this case is ambigious and not really you.
                if (str.StartsWith("You restored"))
                {
                    Regex rYouRestoreHP = new Regex(@"You restored (?<hp>(\d+" + ngs + @")?\d+) of (?<target>[a-zA-Z ']*)'s HP by using (?<skill>[a-zA-Z \-']*?)( Effect)?\.", RegexOptions.Compiled);
                    Match match = rYouRestoreHP.Match(str);
                    if (!match.Success)
                    {
                        ui.AddText("Exception-Unable to parse[e2]: " + str);
                        return;
                    }

                    victim = match.Groups["target"].Value;
                    damage = match.Groups["hp"].Value;
                    skill = match.Groups["skill"].Value;

                    if (GuessDotCasters)
                    {
                        attacker = HealerRecordSet.GetActor(victim, skill, logInfo.detectedTime); // attempt to get the healer from a past indicator log line; or default to an Unknown (Class) for HoTs
                    }

                    if (string.IsNullOrEmpty(attacker))
                    {
                        if (skill == "Healing")
                        {
                            attacker = victim;
                            skill = "Healing Potion HoT"; // HoTs from healing potions are oddly named Healing
                            if (attacker == CheckYou("you"))
                                skill = lastPotion;
                        }
                        else if (skill == "Recovery Potion")
                        {
                            attacker = victim; // HoTs from potions

                            skill = "Recovery Potion HoT";
                            if (attacker == CheckYou("you"))
                                skill = lastPotion;
                        }
                        else if (AionData.Skill.IsSelfHeal(skill))
                        {
                            attacker = victim; // known HoT skills: Blood Rune Additional Effect (only 1 tick), Stamina Recovery, Absorb Vitality (only 1 tick)
                        }
                        else
                        {
                            attacker = "Unknown";
                            if (DebugParse)
                                ui.AddText("Unknown HoT: " + str);
                        }
                    }

                    AddCombatAction(logInfo, attacker, victim, skill, critical, special, damage, SwingTypeEnum.Healing);
                    return;
                }

                // match "xx restored xx HP."  caused by initial heal of using a potion
                if (str.EndsWith(" HP.") && str.Contains("restored"))
                {
                    Regex rYouRestoreHP = new Regex(@"(?<actor>[a-zA-Z ']*) restored (?<hp>(\d+" + ngs + @")?\d+) HP\.");
                    Match match = rYouRestoreHP.Match(str);
                    if (!match.Success)
                    {
                        ui.AddText("Exception-Unable to parse[e3]: " + str);
                        return;
                    }

                    attacker = match.Groups["actor"].Value;
                    victim = attacker;
                    damage = match.Groups["hp"].Value;

                    skill = "Healing/Recovery Potion";
                    if (victim == CheckYou("you"))
                        skill = lastPotion;

                    AddCombatAction(logInfo, attacker, victim, skill, critical, special, damage, SwingTypeEnum.Healing);
                    return;
                }

                // match "xxx recovered xx HP (because xxx used|by using) xxx."
                if (rRecoverHP.IsMatch(str))
                {
                    Match match = rRecoverHP.Match(str);
                    victim = CheckYou(match.Groups["target"].Value);
                    skill = match.Groups["skill"].Value;
                    if (match.Groups["actor"].Success)
                    {
                        attacker = CheckYou(match.Groups["actor"].Value);
                    }
                    else
                    {
                        attacker = victim; // no healer is specified if you healed yourself, unless it was from a HoT (see check below)

                        if (skill == "Recovery Potion")
                        {
                        }
                        else if (skill == "Healing")
                        {
                            skill = "Healing Potion";
                        }
                        else if (GuessDotCasters && skill != "Prayer of Resilience I" /* temp self heal stigma */ && skill != "Improved Stamina I" /* glad self hot */)
                        {
                            string healerHoT = HealerRecordSet.GetActor(victim, skill, logInfo.detectedTime); // check to see if you were recovering because healer placed a HoT on you
                            if (!String.IsNullOrEmpty(healerHoT) && (!healerHoT.StartsWith("Unknown") || AionData.Skill.IsHoT(skill))) attacker = healerHoT; // only assign from HeaderRecordSet if the skill is actually a HoT; otherwise, direct healing spells without a target is actually a self-heal.
                        }
                    }

                    if (AionData.Skill.PlayerSkill(skill) == "Word of Revival")
                    {
                        HealerRecordSet.Add(attacker, victim, skill, logInfo.detectedTime, 40); // Word of Revival doesn't trigger the "is in the continuous recovery state" log line, so we have to catch it here when it does the first heal. (It heals like a potion, first payload, then HoT).
                    }

                    if (AionData.Skill.PlayerSkill(skill) == "Splendor of Rebirth")
                    {
                        HealerRecordSet.Add(attacker, victim, skill, logInfo.detectedTime, 23);
                    }

                    damage = match.Groups["hp"].Value;
                    AddCombatAction(logInfo, attacker, victim, skill, critical, special, damage, SwingTypeEnum.Healing);

                    return;
                }

                // match "xxx recovered x MP (due to|by using|after using) xxx."
                if (rRecoverMP.IsMatch(str))
                {
                    Match match = rRecoverMP.Match(str);
                    victim = CheckYou(match.Groups["target"].Value);
                    damage = match.Groups["mp"].Value;
                    skill = match.Groups["skill"].Value;

                    attacker = HealerRecordSet.GetActor(victim, skill, logInfo.detectedTime);
                    if (attacker.StartsWith("Unknown"))
                    {
                        if (AionData.Skill.IsGainMantra(skill) && PartyMembers.Contains(victim))
                        {
                            var chanters = PartyMembers.FindByClass(AionData.Player.Classes.Chanter);
                            if (chanters.Count == 1)
                            {
                                attacker = chanters[0].Name;
                            }
                        }

                        if (AionData.Skill.PlayerSkill(skill) == "Magic Recovery")
                        { // NOTE: the only example I have of this spell is "Becca recovered 1,500 MP by using Magic Recovery I." which I assume is self-cast.  I need more examples on this spell: i.e. you cast Magic Recovery on yourself, you cast on another player, another player cast on a different player.
                            ////attacker = victim; // we could guess that most of the time chanter is self-casting this spell
                        }
                    }

                    if (string.IsNullOrEmpty(attacker))
                    {
                        attacker = victim; // almost any MP recovery spell/potion is self cast (i.e. Promise of Aether)

                        if (skill == "Recovery Potion")
                        {
                            if (attacker == CheckYou("you"))
                                skill = lastPotion;
                        }

                        if (skill == "Mana Treatment")
                        {
                            skill = "Mana Potion";
                            if (attacker == CheckYou("you"))
                                skill = lastPotion; // oddly enough, healing potions are Healing and mana potions are Mana Treatment.  NOTE: Mana Treatment skills have roman numerals after them, potions are just Mana Treatment.
                        }
                    }

                    AddCombatAction(logInfo, attacker, victim, skill, critical, special, damage, SwingTypeEnum.PowerHealing);
                    return;
                }

                // match "xxx restored x MP." or "You recovered x MP."  caused by initial mana heal of using a potion
                if (str.EndsWith(" MP.") && (str.Contains("restored") || str.Contains("recovered")))
                {
                    Match match = (new Regex(@"^(?<actor>[a-zA-Z ']*) (restored|recovered) (?<mp>.*) MP\.$", RegexOptions.Compiled)).Match(str);
                    if (!match.Success)
                    {
                        ui.AddText("Exception-Unable to parse[e5]: " + str);
                        return;
                    }

                    victim = CheckYou(match.Groups["actor"].Value);
                    attacker = victim; // assume: this log comes from a self action
                    damage = match.Groups["mp"].Value;

                    skill = "Mana/Recovery Potion";
                    if (victim == CheckYou("you"))
                        skill = lastPotion;

                    AddCombatAction(logInfo, attacker, victim, skill, critical, special, damage, SwingTypeEnum.PowerHealing);
                    return;
                }
            }
            else if (str.Contains(" HP ") || str.EndsWith(" HP.") || str.Contains(" MP ") || str.EndsWith(" MP."))
            {
                return; // ignore heals/mp out of combat
            }
            #endregion

            #region blocked
            if (str.Contains("blocked"))
            {
                if (!TagBlockedAttacks) return;

                if (str.StartsWith("You have blocked ")) return; // ignore chat logs that indicate adding a player to the block list

                if (str.StartsWith("The attack was blocked by the "))
                {
                    // match "The attack was blocked by the xxx effect cast on xxx."  ( means your next attack has reduced dmg)
                    Regex rBlockYou = new Regex(@"The attack was blocked by the (?<skill>[a-zA-Z \-']*?) effect cast on (?<target>[a-zA-Z ']*)\.", RegexOptions.Compiled);
                    Match match = rBlockYou.Match(str);
                    if (!match.Success)
                    {
                        ui.AddText("Exception-Unable to parse[e4]: " + str);
                        return;
                    }

                    victim = CheckYou(match.Groups["target"].Value);
                    ////theAttackType = match.Groups["skill"].Value;
                    ////AddCombatAction(logInfo, "Unknown", incName, theAttackType, critical, special, Dnum.NoDamage, SwingTypeEnum.Melee); // don't add action; this event occurs even on spells if they have armor up
                    BlockedHistory.Add(CheckYou("you"), victim, logInfo.detectedTime, "blocked");
                    return;
                }
                else
                {
                    // match "xxx blocked xxx's attack with the xxx effect."
                    Regex rBlock = new Regex(@"(?<victim>[a-zA-Z ']*) blocked (?<attacker>[a-zA-Z ']*)'s attack( with the (?<skill>[a-zA-Z \-']*?) effect)?\.", RegexOptions.Compiled);
                    Match match = rBlock.Match(str);
                    if (!match.Success)
                    {
                        ui.AddText("Exception-Unable to parse[e8]: " + str);
                        return;
                    }

                    victim = match.Groups["victim"].Value;
                    attacker = match.Groups["attacker"].Value;
                    ////theAttackType = match.Groups["skill"].Value;
                    BlockedHistory.Add(attacker, victim, logInfo.detectedTime, "blocked");
                    return;
                }
            }
            #endregion

            #region parried
            if ((str.IndexOf("parried") != -1) && (str.IndexOf("'s attack") != -1))
            {
                victim = str.Substring(0, str.IndexOf("parried") - 1);
                victim = this.CheckYou(victim);
                attacker = str.Substring(str.IndexOf("parried") + 8, str.IndexOf("'s attack") - (str.IndexOf("parried") + 8));
                attacker = this.CheckYou(attacker);
                if (TagBlockedAttacks)
                    BlockedHistory.Add(attacker, victim, logInfo.detectedTime, "parried");
                return;
            }
            #endregion

            #region resisted
            else if (str.Contains("resisted"))
            {
                if (rResist.IsMatch(str))
                {
                    Match match = rResist.Match(str);

                    SwingTypeEnum swingType = SwingTypeEnum.NonMelee;
                    victim = CheckYou(match.Groups["victim"].Value);
                    if (match.Groups["attacker"].Success)
                    {
                        attacker = CheckYou(match.Groups["attacker"].Value);
                    }
                    else
                    {
                        attacker = CheckYou("you");
                    }

                    skill = match.Groups["skill"].Value;
                    if (skill == "your attack" || skill == "attack")
                    { // match the generic "attack" word
                        skill = "Melee"; // are these elemental melee attacks being resisted?  I mostly see assassin's attacks being resisted here, but also saw a ranger attack in my personal logs and well as my own sorc attacks.  (speaking of which, I haven't seen in my logs where my sorc's attacks were evaded.)
                        swingType = SwingTypeEnum.Melee;
                    }

                    #region resolve skills with quotes
                    /* Initially, we let the regex let the actor name be greedy with the 's.  i.e. Hirmilden's Tipolid is a possible actor.
                     * However, given the set of skills that contain 's is finite, we double check here if a skill would make more sense.
                     */
                    string blob = attacker + "'s " + skill; // example blob: "Heaven" + "Judgement II" = "Heaven's Judgement II" or "Vyn's Aether" + "Hold" = "Vyn's Aether's Hold I"
                    foreach (string skillThatContainsQuote in AionData.Skill.SkillsThatContainQuote)
                    {
                        string possiblePlayerSkill = skillThatContainsQuote + " I"; // the rank I is just there to end the skill name
                        if (blob.Contains(possiblePlayerSkill))
                        {
                            int indexForSkill = blob.IndexOf(possiblePlayerSkill);
                            string newattacker = blob.Substring(0, indexForSkill).Trim(); // example newattacker: "" or "Vyn's"
                            string newskill = blob.Substring(indexForSkill); // example newskill = "Heaven's Judgement" or "Aether's Hold"

                            if (String.IsNullOrEmpty(newattacker)) // attacker not specified? is this a self cast?
                            {
                                if (attacker == CheckYou("you")) // in the unlikely event that your character's name is Heaven, then we know your name does not appear in your own spell's resist log, so you must be a cleric and you casted Heaven's Judgment
                                {
                                    // leave attacker as "you"
                                    skill = newskill;
                                    break;
                                }
                                else
                                {
                                    // ambiguous case! either there is a templar nearby named Heaven who casted Judgment (unlikely but there is one on Siel), or you're a cleric who casted Heaven's Judgment
                                    // TODO: perhaps knowing more information will help (i.e. surrounding players during encounter, your class versus the class requirement of the spell, etc.)
                                    // just assume it was your spell that got resisted and ignore the other possibility for now
                                    attacker = CheckYou("you");
                                    skill = newskill;
                                    break;
                                }
                            }
                            else
                            {
                                attacker = newattacker.Remove(newattacker.Length - 2); // remove the 's at the end
                                skill = newskill;
                                break;
                            }
                        }
                    }
                    #endregion

                    AddCombatAction(logInfo, attacker, victim, skill, critical, special, new Dnum((int)Dnum.Miss, "resisted"), swingType);
                    return;
                }
            }
            #endregion

            #region evaded
            else if (str.Contains("evaded"))
            {
                Regex rEvaded = new Regex(@"^(?<victim>[a-zA-Z ']*) evaded (the|(?<attacker>[a-zA-Z ']*?)'s) (attack|(?<skill>[a-zA-Z \-']*?))\.$", RegexOptions.Compiled);
                Match match = rEvaded.Match(str);

                victim = CheckYou(match.Groups["victim"].Value);
                
                if (match.Groups["attacker"].Success)
                {
                    attacker = CheckYou(match.Groups["attacker"].Value);
                }
                else
                {
                    attacker = "Unknown";
                }

                SwingTypeEnum swingType;
                if (match.Groups["skill"].Success)
                {
                    swingType = SwingTypeEnum.NonMelee;
                    skill = match.Groups["skill"].Value;
                }
                else
                {
                    swingType = SwingTypeEnum.Melee;
                    skill = "Melee";
                }

                AddCombatAction(logInfo, attacker, victim, skill, critical, special, new Dnum((int)Dnum.Miss, "evaded"), swingType);
                return;
            }
            #endregion

            #region removed/dispel
            else if (str.IndexOf("removed its abnormal physical conditions by using") != -1)
            {
                attacker = str.Substring(0, str.IndexOf("removed its abnormal physical conditions by using") - 1);
                attacker = this.CheckYou(attacker);
                victim = attacker;
                skill = str.Substring(str.IndexOf("removed its abnormal physical conditions by using") + 50, (str.Length - (str.IndexOf("removed its abnormal physical conditions by using") + 50)) - 2);
                AddCombatAction(logInfo, attacker, victim, skill, critical, special, new Dnum((int)Dnum.NoDamage, "cure"), SwingTypeEnum.CureDispel);
                return;
            }
            else if ((str.IndexOf("removed abnormal physical conditions from") != -1) && (str.IndexOf("by using") != -1))
            {
                attacker = str.Substring(0, str.IndexOf("removed abnormal physical conditions from") - 1);
                attacker = this.CheckYou(attacker);
                victim = str.Substring(str.IndexOf("removed abnormal physical conditions from") + 0x2a, (str.IndexOf("by using") - (str.IndexOf("removed abnormal physical conditions from") + 0x2a)) - 1);
                victim = this.CheckYou(victim);
                skill = str.Substring(str.IndexOf("by using") + 9, (str.Length - (str.IndexOf("by using") + 9)) - 2);
                AddCombatAction(logInfo, attacker, victim, skill, critical, special, new Dnum((int)Dnum.NoDamage, "cure"), SwingTypeEnum.CureDispel);
                return;
            }
            else if ((str.IndexOf("dispelled the magical buffs from") != -1) && (str.IndexOf("by using") != -1))
            {
                attacker = str.Substring(0, str.IndexOf("dispelled the magical buffs from") - 1);
                attacker = this.CheckYou(attacker);
                victim = str.Substring(str.IndexOf("dispelled the magical buffs from") + 0x21, (str.IndexOf("by using") - (str.IndexOf("dispelled the magical buffs from") + 0x21)) - 1);
                victim = this.CheckYou(victim);
                skill = str.Substring(str.IndexOf("by using") + 9, (str.Length - (str.IndexOf("by using") + 9)) - 2);
                AddCombatAction(logInfo, attacker, victim, skill, critical, special, new Dnum((int)Dnum.NoDamage, "dispel"), SwingTypeEnum.CureDispel);
                return;
            }
            else if ((str.IndexOf("dispelled the magical debuffs from") != -1) && (str.IndexOf("by using") != -1))
            {
                attacker = str.Substring(0, str.IndexOf("dispelled the magical debuffs from") - 1);
                attacker = this.CheckYou(attacker);
                victim = str.Substring(str.IndexOf("dispelled the magical debuffs from") + 0x21, (str.IndexOf("by using") - (str.IndexOf("dispelled the magical debuffs from") + 0x21)) - 1);
                victim = this.CheckYou(victim);
                skill = str.Substring(str.IndexOf("by using") + 9, (str.Length - (str.IndexOf("by using") + 9)) - 2);
                AddCombatAction(logInfo, attacker, victim, skill, critical, special, new Dnum((int)Dnum.NoDamage, "cure"), SwingTypeEnum.CureDispel);
                return;
            }
            else if (str.IndexOf("dispelled its magic effect by using") != -1) // Blind Leap
            {
                attacker = str.Substring(0, str.IndexOf("dispelled its magic effect by using") - 1);
                attacker = this.CheckYou(attacker);
                victim = attacker;
                skill = str.Substring(str.IndexOf("by using") + 9, (str.Length - (str.IndexOf("by using") + 9)) - 2);
                AddCombatAction(logInfo, attacker, victim, skill, critical, special, new Dnum((int)Dnum.NoDamage, "cure"), SwingTypeEnum.CureDispel);
                return;
            }
            else if (str.Contains("dispelled its magical debuffs by using"))
            {
                attacker = CheckYou(str.Substring(0, str.IndexOf("dispelled its magical debuffs") - 1));
                victim = attacker;
                skill = str.Substring(str.IndexOf("by using") + 9, (str.Length - (str.IndexOf("by using") + 9)) - 2);
                AddCombatAction(logInfo, attacker, victim, skill, critical, special, new Dnum((int)Dnum.NoDamage, "cure"), SwingTypeEnum.CureDispel);
                return;
            }
            else if (str.StartsWith("Your abnormal physical conditions were removed because"))
            {
                Regex rDispelOnYou = new Regex(@"Your abnormal physical conditions were removed because (?<actor>[a-zA-Z ']*) used (?<skill>[a-zA-Z \-']*?) on you", RegexOptions.Compiled);
                Match match = rDispelOnYou.Match(str);
                victim = CheckYou("you");
                attacker = match.Groups["actor"].Value;
                skill = match.Groups["skill"].Value;
                AddCombatAction(logInfo, attacker, victim, skill, critical, special, new Dnum((int)Dnum.NoDamage, "cure"), SwingTypeEnum.CureDispel);
                return;
            }

            #endregion

            #region state parses
            /*
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

            else if (rWeakened.IsMatch(str))
            {
                //Match match = rWeakened.Match(str);
                return;
            }
             */
            #endregion

            #region debug output
            else
            {
                if (DebugParse && !IsIgnore(str))
                    ui.AddText("unparsed: " + str);
            }
            #endregion
        }
    }
}