using System;
using System.Collections.Generic;
using System.Text;

namespace AionData
{
    public class Player
    {
        public enum Classes 
        { 
            Unknown,
            Warrior, Scout, Mage, Priest, 
            Templar, Gladiator, 
            Assassin, Ranger, 
            Sorcerer, Spiritmaster, 
            Chanter, Cleric 
        }

        public string Name { get; set; }

        public Classes Class { get; set; }

        public static bool IsTier1Class(Classes playerClass)
        {
            return playerClass == Classes.Warrior ||
                playerClass == Classes.Scout ||
                playerClass == Classes.Mage ||
                playerClass == Classes.Priest;
        }

        public static bool IsTier2Class(Classes playerClass)
        {
            return playerClass != Classes.Unknown && !IsTier1Class(playerClass);
        }
    }
}
