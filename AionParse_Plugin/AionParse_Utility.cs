using System;
using System.Globalization;
using Advanced_Combat_Tracker;

namespace AionParse_Plugin
{
    public partial class AionParse : IActPluginV1
    {
        #region ui setters

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

        internal void SetLinkPets(bool linkPets)
        {
            this.linkPets = linkPets;
        }

        #endregion

        #region AddCombatAction overloads
        private static void AddCombatAction(LogLineEventArgs logInfo, string attacker, string victim, string theAttackType, bool critical, string special, string damage, SwingTypeEnum swingType)
        {
            AddCombatAction(logInfo, attacker, victim, theAttackType, critical, special, damage, swingType, string.Empty);
        }

        private static void AddCombatAction(LogLineEventArgs logInfo, string attacker, string victim, string theAttackType, bool critical, string special, string damage, SwingTypeEnum swingType, string damageType)
        {
            AddCombatAction(logInfo, attacker, victim, theAttackType, critical, special, int.Parse(damage.Replace(",", String.Empty)), swingType, damageType);
        }

        private static void AddCombatAction(LogLineEventArgs logInfo, string attacker, string victim, string theAttackType, bool critical, string special, Dnum damage, SwingTypeEnum swingType)
        {
            AddCombatAction(logInfo, attacker, victim, theAttackType, critical, special, damage, swingType, string.Empty);
        }

        private static void AddCombatAction(LogLineEventArgs logInfo, string attacker, string victim, string theAttackType, bool critical, string special, Dnum damage, SwingTypeEnum swingType, string damageType)
        {
            if (ActGlobals.oFormActMain.SetEncounter(logInfo.detectedTime, attacker, victim))
            {
                int globalTime = ActGlobals.oFormActMain.GlobalTimeSorter++;
                ActGlobals.oFormActMain.AddCombatAction((int)swingType, critical, special, attacker, theAttackType, damage, logInfo.detectedTime, globalTime, victim, damageType);
            }
        }

        #endregion

        #region utility methods
        private static DateTime ParseDateTime(string fullLogLine)
        {
            string str = fullLogLine.Substring(0, 4) + "-" + fullLogLine.Substring(5, 2) + fullLogLine.Substring(8, 2);
            string str2 = fullLogLine.Substring(11, 8);
            return DateTime.ParseExact(str + "-" + str2, "yyyy-MMdd-HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private static Dnum NewDnum(string damage, string damageString)
        {
            int d = int.Parse(damage.Replace(",", string.Empty).Trim());
            if (String.IsNullOrEmpty(damageString))
            {
                return new Dnum(d);
            }
            else
            {
                return new Dnum(d, damageString);
            }
        }

        private string CheckYou(string incName)
        {
            switch (incName.ToUpper().Trim())
            {
                case "YOU":
                case "YOUR":
                case "YOURSELF":
                    return ActGlobals.charName == "YOU" ? lastCharName : ActGlobals.charName;
                default:
                    return incName;
            }
        }
        #endregion
    }
}