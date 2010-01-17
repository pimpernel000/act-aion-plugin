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
        }

        Dictionary<string, List<BlockedRecord>> attackerHistory = new Dictionary<string, List<BlockedRecord>>();

        public void Add(string attacker, string defender, DateTime time)
        {
            if (String.IsNullOrEmpty(attacker)) return;

            if (!attackerHistory.ContainsKey(attacker))
            {
                attackerHistory.Add(attacker, new List<BlockedRecord>());
            }

            attackerHistory[attacker].Insert(0, new BlockedRecord { Defender = defender, BlockedTime = time });
        }

        public bool IsBlocked(string attacker, string defender, DateTime time)
        {
            return IsBlocked(attacker, defender, time, true);
        }

        public bool IsBlocked(string attacker, string defender, DateTime time, bool consume)
        {
            if (!attackerHistory.ContainsKey(attacker)) return false;

            List<BlockedRecord> blockedRecordList = attackerHistory[attacker];
            foreach (BlockedRecord record in blockedRecordList)
            {
                if (record.BlockedTime == DateTime.MinValue || (time - record.BlockedTime).TotalSeconds > 2)
                    return false;

                if (record.Defender == defender)
                {
                    if (consume) record.BlockedTime = DateTime.MinValue; // consume the block record
                    return true;
                }
            }

            return false;
        }

        public void Clear()
        {
            attackerHistory.Clear();
        }
	}
}
