using System;
using System.Collections.Generic;

namespace AionParse_Plugin
{
	public class ContinuousDamageSet
	{
        const int LOOKBACK_LIMIT = 30;

        class ContinuousDamageRecord
        {
            public string Actor { get; set; }
            public string Target { get; set; }
            public string Skill { get; set; }
            public DateTime Start { get; set; }
        }

        private List<ContinuousDamageRecord> list = new List<ContinuousDamageRecord>();

        public void Add(string actor, string target, string skill, DateTime start)
        {
            list.Insert(0, new ContinuousDamageRecord { Actor = actor, Target = target, Skill = skill, Start = start });
        }

        public string GetActor(string target, string skill, DateTime now)
        {
            foreach (var record in list)
            {
                if ((now - record.Start).TotalSeconds > LOOKBACK_LIMIT) 
                    return null;

                if (record.Target == target && record.Skill == skill)
                    return record.Actor;
            }

            return null;
        }

        public void Clear()
        {
            list.Clear();
        }
	}
}
