using System;
using System.Collections.Generic;
using System.Text;

namespace AionData
{
    public static class Pet
    {
        // TODO: add pet elemental damage?

        private static Dictionary<string, int> petDurations = new Dictionary<string, int>()
        {
            { "Fire Spirit", 86400 },
            { "Earth Spirit", 86400 },
            { "Water Spirit", 86400 },
            { "Wind Spirit", 86400 },
            { "Tempest Spirit", 1200 }, // dp summons, lasts 20min
            { "Magma Spirit", 1200 },
            { "Fire Energy", 12 }, // usually 8~12 secs to attack depending on travel time
            { "Stone Energy", 12 },
            { "Water Energy", 12 },
            { "Wind Servant", 12 },
            { "Energy of Cyclone", 30 }, // TODO: spiritmaster 45 stigma skill... this is a placeholder until I can get actual information on the pet name and pet duration
            { "Holy Servant", 21 }, // cleric servants usually shoots 3 times and lasts less than 15 secs; maybe shoots a 4th time if given a heal
            { "Noble Energy", 30 }, // TODO: find more information about the duration of this spell
            { "Slowing Trap", 60 }, // ranger traps lasts 60 secs
            { "Poisoning Trap", 60 },
            { "Spike Trap", 60 },
            { "Explosion Trap", 60 },
            { "Destruction Trap", 60 },
            { "Trap of Clairvoyance", 60 },
            { "Sandstorm Trap", 60 },
            { "Spike Bite Trap", 60 },
            { "Trap of Slowing", 60 },
        };

        private static List<string> targettedPets = new List<string>()
        {
            "Fire Energy", 
            "Stone Energy", 
            "Water Energy", 
            "Wind Servant",
            "Energy of Cyclone",
            "Holy Servant",
            "Noble Energy" // TODO: confirm that this is a targetted spell
        };

        public static Dictionary<string, int> PetDurations
        {
            get { return petDurations; }
        }

        public static bool IsPet(string pet)
        {
            return petDurations.ContainsKey(pet);
        }

        public static bool IsTargettedPet(string pet)
        {
            return targettedPets.Contains(pet);
        }
    }
}
