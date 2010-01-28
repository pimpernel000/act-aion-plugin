using System;
using System.Collections.Generic;

namespace AionParse_Plugin
{
	public class BlockedSet
	{
        class BlockedRecord
        {
            public string Defender { get; set; }
            public DateTime BlockedTime { get; set; }
            public String BlockType { get; set; }
        }

        Dictionary<string, List<BlockedRecord>> attackerHistory = new Dictionary<string, List<BlockedRecord>>();

        public void Add(string attacker, string defender, DateTime time, string blockString)
        {
            if (String.IsNullOrEmpty(attacker)) return;

            if (!attackerHistory.ContainsKey(attacker))
            {
                attackerHistory.Add(attacker, new List<BlockedRecord>());
            }

            attackerHistory[attacker].Insert(0, new BlockedRecord { Defender = defender, BlockedTime = time, BlockType = blockString });
        }

        public string IsBlocked(string attacker, string defender, DateTime time)
        {
            return IsBlocked(attacker, defender, time, true);
        }

        public string IsBlocked(string attacker, string defender, DateTime time, bool consume)
        {
            if (!attackerHistory.ContainsKey(attacker)) return "";

            List<BlockedRecord> blockedRecordList = attackerHistory[attacker];
            foreach (BlockedRecord record in blockedRecordList)
            {
                if (record.BlockedTime == DateTime.MinValue || (time - record.BlockedTime).TotalSeconds > 1)
                    return "";

                if (record.Defender == defender)
                {
                    if (consume) record.BlockedTime = DateTime.MinValue; // consume the block record
                    return record.BlockType;
                }
            }

            return "";
        }

        public void Clear()
        {
            attackerHistory.Clear();
        }
	}
}
