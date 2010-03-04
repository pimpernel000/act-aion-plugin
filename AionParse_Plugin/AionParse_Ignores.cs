using System.Collections.Generic;
using Advanced_Combat_Tracker;

namespace AionParsePlugin
{
    public partial class AionParse : IActPluginV1
    {
        bool IsIgnore(string str)
        {
            if (str.Contains("[charname:")) return true; // ignore chats ([charname:...] is a link to a name
            if (str.StartsWith(CheckYou("you") + ":")) return true; // ignore your own chats

            if (str.StartsWith("You have gained") && (str.Contains("EXP") || str.Contains("Abyss Points"))) return true;

            if (str.StartsWith("You boosted your")) return true; // ignore You boosted your evasion by using Focused Evasion I. 
            if (str.Contains("movement speed decreased as you used")) return true;

            if (str.StartsWith("You knocked") && str.Contains("back by using")) return true; // knockback by you
            if (str.Contains("was knocked back from shock because")) return true; // knockback by others

            if (str.Contains("to sleep by using")) return true;
            if (str.Contains("transformed") && str.Contains("into Cursed Tree")) return true;
            if (str.Contains("boosted") && (str.Contains("by using Curse of Roots") || str.Contains("by using Sleep"))) return true;

            List<string> fullLines = new List<string> 
            {
                "Your movement speed is restored to normal.",
                "Your attack speed is restored to normal.",
                "Invalid target.",
                "The target is too far away.",
                "You interrupted the target's skill.",
                "The skill was cancelled.",
                "You stopped using the Macro.", "You cannot use a Macro yet.",
                "You cannot use the item as its cooldown time has not expired yet.",
                "You cannot use that because there is an obstacle in the way.",
                "You cannot use that on your target.",
                "You do not have a proper target for that skill.",
                "You cannot receive a quest that you are already working on.",
                "You gave up rolling the dice.",
                "You have stopped gathering.",
                "Starts the auto-distribution of miscellaneous items.",
                "You must level up to raise your skill level.",
                "You have died.",
                "Greetings Daevas!",
                "There have been frequent scam attempts on our players using web sites that imitate ours.",
                "Our official sites are aiononline.com and ncsoft.com only.",
                "Don't enter your Aion account info on any other site, or it will be stolen and your characters will be looted.",
                "You are too far from the target to use that skill.",
                "You cannot issue commands in resting.",
                "You do not have much flight time left. Please land on a secure place."
            };

            List<string> startParts = new List<string> 
            {
                "You have acquired [item:",
                "You have acquired 2 [item:",
                "You have earned",
                "You are gathering", 
                "You are no longer",
                "You restored your flight time by",
                "You teleported yourself by using Blind Leap",
                "You dispelled the magic effect by using Blind Leap",
                "You changed the group",
                "Quest updated:", "Quest complete:", "You failed to share the quest with",
                "You have joined the", // ignore channels
                "You changed the connection status",
                "Legion Message:",
                "You learned",
                "You were killed",
                "Your spirit uses its skills on"
            };

            List<string> endParts = new List<string> 
            {
                "is running away.",
                "restored its movement speed.", "restored its attack speed.",
                "is no longer stunned.",
                "is no longer immobilized.", 
                "is no longer afraid.",
                "is no longer poisoned.",
                "is no longer bleeding.",
                "is no longer blind.",
                "is no longer shocked.",
                "is no longer spinning.",
                "released from the Aerial Snare.",
                "is no longer staggering.",
                "woke up.",
                "is no longer silenced.",
                "has died.",
                "gave up rolling the dice.",
                "Spirit starts to attack the enemy.",
                "Spirit is in Guard mode.",
                "Spirit is in Resting mode.",
                "Spirit has been dismissed.",
                "gives up the pursuit.",
                "MP consumption has changed because he used Lumiel's Wisdom I."
            };

            List<string> containParts = new List<string> 
            {
                "has acquired [item:",
                "rolled the dice and got a",
                "speed has decreased",
                "became stunned because",
                "became poisoned because",
                "became blinded because",
                "is spinning because",
                "became snared in mid-air because",
                "was released from the aerial snare because",
                "from the Aerial Snare by using",
                "fell down from shock because",
                "has knocked you back by using",
                "fell asleep because",
                "became silenced because",
                "is unable to fly because",
                "loot rate has increased because",
                "changed his Physical Attack by using",
                "has boosted your",
                "was affected by its own",
                "gathered successfully", "failed to gather",
                "has logged in",
                "has conquered", "is no longer vulnerable" // ignore fortress
            };

            foreach (string s in fullLines)
                if (str == s)
                    return true;

            foreach (string startPart in startParts)
                if (str.StartsWith(startPart))
                    return true;

            foreach (string endPart in endParts)
                if (str.EndsWith(endPart))
                    return true;

            foreach (string containPart in containParts)
            {
                if (str.Contains(containPart))
                    return true;
            }

            if (str.Contains("is in the") && (str.Contains("state because") || str.Contains("state as it used"))) return true;
            if (str.Contains("has weakened") && str.Contains("by using")) return true;
            if (str.Contains("casting speed by using") && str.Contains("changed")) return true;
            if (str.Contains("casting speed has changed because he used")) return true;

            if (str.EndsWith("existing skill.") && str.Contains("conflicted with")) return true;
            if (str.StartsWith("You have played for") && str.EndsWith("Please take a break.")) return true;

            return false; // unhandled string, we need to parse it or add it to the list of things to ignore
        }
    }
}