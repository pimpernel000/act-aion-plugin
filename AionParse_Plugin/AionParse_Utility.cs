using System;
using System.Globalization;
using Advanced_Combat_Tracker;

namespace AionParsePlugin
{
    public partial class AionParse : IActPluginV1
    {
        #region AddCombatAction overloads
        private void AddCombatAction(LogLineEventArgs logInfo, string attacker, string victim, string theAttackType, bool critical, string special, string damage, SwingTypeEnum swingType)
        {
            AddCombatAction(logInfo, attacker, victim, theAttackType, critical, special, damage, swingType, string.Empty);
        }

        private void AddCombatAction(LogLineEventArgs logInfo, string attacker, string victim, string theAttackType, bool critical, string special, string damage, SwingTypeEnum swingType, string damageType)
        {
            Dnum dnumDamage = NewDnum(damage, null);
            AddCombatAction(logInfo, attacker, victim, theAttackType, critical, special, dnumDamage, swingType, damageType);
        }

        private void AddCombatAction(LogLineEventArgs logInfo, string attacker, string victim, string theAttackType, bool critical, string special, Dnum damage, SwingTypeEnum swingType)
        {
            AddCombatAction(logInfo, attacker, victim, theAttackType, critical, special, damage, swingType, string.Empty);
        }

        private void AddCombatAction(LogLineEventArgs logInfo, string attacker, string victim, string theAttackType, bool critical, string special, Dnum damage, SwingTypeEnum swingType, string damageType)
        {
            DateTime now = logInfo.detectedTime;
            if (ActGlobals.oFormActMain.SetEncounter(now, attacker, victim))
            {
                // redirect attacks from pets/servants as coming from summoner
                if (SummonerRecordSet.IsSummonedPet(attacker))
                {
                    var summonerRecord = SummonerRecordSet.GetSummonerRecord(victim, attacker, now);
                    if (summonerRecord != null)
                    {
                        string pet = attacker;
                        if (AionData.Pet.IsTargettedPet(pet))
                        {
                            attacker = summonerRecord.Actor;
                            theAttackType = summonerRecord.Skill;
                        }
                        else if (LinkPets)
                        {
                            attacker = summonerRecord.Actor;
                            if (summonerRecord.Duration <= 60)
                                theAttackType = summonerRecord.Skill;
                            else
                                theAttackType += "(" + pet + ")";
                        }
                    }
                }
                else if (SummonerRecordSet.IsSummonedPet(victim))
                {
                    if (AionData.Pet.IsPet(victim))
                    {
                        // handle player pets
                        if (AionData.Pet.PetDurations[victim] <= 60) return; // ignore damage done to short-duration temporary pets // TODO: this should be a checkbox as this will decrease the dps of the attacker

                        var summonerRecord = SummonerRecordSet.GetSummonerRecord(null, victim, now);
                        if (LinkPets)
                        {
                            ////return; // TODO: how do we treat damage done to spiritmaster's pets?
                        }
                    }
                    else
                    {
                        // handle monster/unknown pets
                        if (LinkPets)
                        {
                            // TODO: how do we treat damage done to mob's pets?
                        }
                        else
                        {
                            ////victim += " (inc)"; // TODO: this should be a checkbox if we want damage shown to mob pets
                        }
                    }
                }

                int globalTime = ActGlobals.oFormActMain.GlobalTimeSorter++;
                ActGlobals.oFormActMain.AddCombatAction((int)swingType, critical, special, attacker, theAttackType, damage, now, globalTime, victim, damageType);
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
            int d = int.Parse(damage, NumberStyles.AllowThousands, CultureInfo.CurrentCulture.NumberFormat);
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
            switch (incName.ToUpper(CultureInfo.InvariantCulture).Trim())
            {
                case "YOU":
                case "YOUR":
                case "YOURSELF":
                    return LastCharName;
                default:
                    return incName;
            }
        }
        #endregion
    }
}