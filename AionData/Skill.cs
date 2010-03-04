using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AionData
{
    public class Skill
    {
        public static readonly List<string> SkillsThatContainQuote = new List<string>()
        {
            "Aether's Hold", "Heaven's Judgment", "Earth's Wrath", "Vaizel's Dirk", "Triniel's Dirk", "Vaizel's Arrow", "Triniel's Arrow", "Lumiel's Wrath", "Kaisinel's Wrath"
        };

        static List<string> healSkillsThatInflictDamage = new List<string>() 
        {
            "Healing Wind", "Light of Recovery", "Healing Light", "Radiant Cure", "Flash of Recovery", "Splendor of Recovery"
        };

        static List<Skill> procSkills = new List<Skill>()
        {
            new Skill("Promise of Wind", 30 * 60),
            new Skill("Apply Poison", 2 * 60),
            new Skill("Apply Deadly Poison", 2 * 60)
        };

        static List<string> selfHealSkills = new List<string>() 
        {
            "Stamina Recovery", "Absorb Vitality" // Stamina Recovery is gladiator self HoT skill; Absorb Vitality is Spiritmaster drain skill
        };

        static Dictionary<Player.Classes, string[]> skillList = new Dictionary<Player.Classes, string[]>() 
        {
            { 
                Player.Classes.Chanter, new string[] 
                { 
                    "Victory Mantra I", "Meteor Strike I", "Yustiel's Protection I", "Marchutan's Protection I", "Winged Mantra",
                    "Shield Mantra I", "Word of Revival I", "Booming Strike I", "Magic Mantra I", "Promise of Earth I", "Incandescent Blow I",
                    "Celerity Mantra I", "Promise of Wind II", "Rage Spell I", "Healing Conduit I", "Meteor Strike II", "Protective Ward I", 
                    "Victory Mantra II", "Booming Smash I", "Focused Parry I", "Revival Mantra I", "Booming Strike II", "Parrying Strike I", 
                    "Promise of Aether I", "Word of Revival II", "Shield Mantra II", "Winged Catalyst", "Healing Conduit II", 
                    "Clement Mind Mantra I", "Boost Mantra Range I", "Magic Mantra II", "Word of Protection I", "Protective Ward II",
                    "Binding Word I", "Promise of Earth II", "Pentacle Shock I", "Incandescent Blow II", "Healing Conduit III", 
                    "Word of Wind I", "Promise of Wind III", "Intensity Mantra I", "Blessing of Health II", "Booming Assault I",
                    "Victory Mantra III", "Meteor Strike III", "Revival Mantra II", "Word of Life I", "Resonance Haze I", "Booming Smash II",
                    "Protective Ward III", "Promise of Aether II", "Word of Revival III", "Protection Mantra I", "Healing Conduit IV",
                    "Word of Quickness I", "Shield Mantra III", "Parrying Strike II", "Booming Strike III", "Clement Mind Mantra II",
                    "Word of Inspiration I", "Pentacle Shock II", "Protective Ward IV", "Splash Swing I", "Word of Life II",
                    "Tremor I", "Magic Mantra III", "Promise of Earth III", "Inescapable Judgment I", "Incandescent Blow III",
                    "Soul Strike I", "Booming Assault II", "Promise of Wind IV", "Intensity Mantra II", "Healing Conduit V",
                    "Enhancement Mantra I", "Ancestral Aetheric Field I", "Stamina Restoration I", "Magic Recovery I", "Recovery Spell I",
                    "Meteor Strike IV", "Blessing of Wind I", "Blessing of Rock I", "Ancestral Word of Spellstopping I", "Booming Smash III",
                    "Splash Swing II", "Protective Ward V", "Swiftwing I", "Revival Mantra III", "Resonance Haze II", "Word of Life III", 
                    "Victory Mantra IV", "Word of Revival IV", "Soul Crush I", "Booming Strike IV", "Healing Conduit VI", "Shield Mantra IV", 
                    "Protection Mantra II", "Promise of Aether III", "Parrying Strike III", "Clement Mind Mantra III", "Aetheric Field I", 
                    "Invincibility Mantra I", "Divine Curtain I", "Curtain of Aether I", "Mountain Crash I", "Word of Spellstopping I", 
                    "Stilling Word I"
                } 
            }
        };

        static Dictionary<string, Player.Classes> skillClassLookup = new Dictionary<string, Player.Classes>();

        static Skill()
        {
            foreach (var playerClass in skillList.Keys)
                foreach (string skill in skillList[playerClass])
                    skillClassLookup.Add(skill, playerClass);
        }

        public Skill(string name, int duration)
        {
            Name = name;
            Duration = duration;
        }

        public string Name { get; set; }

        public int Duration { get; set; }

        public AionData.Player.Classes Class { get; set; }

        public static Skill CheckProcSkill(string skill)
        {
            foreach (var procSkill in procSkills)
            {
                if (procSkill.Name == PlayerSkill(skill))
                {
                    return procSkill;
                }
            }

            return null;
        }

        public static string PlayerSkill(string skill)
        {
            /*
            static Regex romanNumerals = new Regex("(?<skill>.*) (IX|IV|V?I{0,3})$", RegexOptions.Compiled); // original regex was : ^M{0,4}(CM|CD|D?C{0,3})(XC|XL|L?X{0,3})(IX|IV|V?I{0,3})$ from http://diveintopython.org/regular_expressions/n_m_syntax.html
            Match match = romanNumerals.Match(skill);
            if (match.Success)
            {
                return match.Groups["skill"].Value;
            }
             */
            if (skill.EndsWith(" I") || skill.EndsWith(" II") || skill.EndsWith(" III") || skill.EndsWith(" IV") || skill.EndsWith(" V") ||
                skill.EndsWith(" VI") || skill.EndsWith(" VII") || skill.EndsWith(" VIII") || skill.EndsWith(" IX"))
            {
                return skill.Substring(0, skill.LastIndexOf(' '));
            }

            return string.Empty;
        }

        public static bool IsHealThatInflictsDamage(string skill)
        {
            return healSkillsThatInflictDamage.Contains(PlayerSkill(skill));
        }

        public static bool IsSelfHeal(string skill)
        {
            return selfHealSkills.Contains(PlayerSkill(skill)) || (skill.StartsWith("Blood Rune") && skill.EndsWith("Additional")); // NOTE: Blood Rune self heal looks like "Blood Rune I Additional Effect"
        }

        public static bool HasAdditionalEffect(string skill)
        {
            return skill.EndsWith(" Additional") && skill.StartsWith("Blood Rune"); // NOTE: Blood Rune is the only ability I know of that also applies a single DoT to the target and a single HoT to the caster and the DoT and HoT are both named "Blood Rune X Additional Effect"
        }

        public static bool IsMantra(string skill)
        {
            return PlayerSkill(skill).EndsWith(" Mantra");
        }

        public static bool IsGainMantra(string skill)
        {
            if (IsMantra(skill))
            {
                return skill.StartsWith("Revival") || skill.StartsWith("Clement Mind") || skill.StartsWith("Invincibility");
            }

            return false;
        }

        public static Player.Classes GuessClass(string skill)
        {
            if (skillClassLookup.ContainsKey(skill))
            {
                return skillClassLookup[skill];
            }
            else
            {
                return Player.Classes.Unknown;
            }
        }
    }
}
