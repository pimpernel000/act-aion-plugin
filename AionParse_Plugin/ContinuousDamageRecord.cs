using System;
using System.Collections.Generic;

namespace AionParse_Plugin
{
    public class ContinuousDamageSet
    {
        const int LookBackLimit = 30;

        private List<ContinuousDamageRecord> list = new List<ContinuousDamageRecord>();

        public void Add(string actor, string target, string skill, DateTime start)
        {
            list.Insert(0, new ContinuousDamageRecord { Actor = actor, Target = target, Skill = skill, Start = start });
        }

        public string GetActor(string target, string skill, DateTime now)
        {
            foreach (var record in list)
            {
                if ((now - record.Start).TotalSeconds > LookBackLimit)
                    return null;

                if (record.Target == target && record.Skill == skill)
                    return record.Actor;
            }

            return null;
        }

        public void Clear()
        {
            if (list.Count == 0) return;

            DateTime lastTime = list[0].Start;
            bool listStale = false;
            int staleIndex = -1;
            for (int i = 1; i < list.Count; i++)
            {
                if ((lastTime - list[i].Start).TotalSeconds > LookBackLimit)
                {
                    listStale = true;
                    staleIndex = i;
                    break;
                }
            }

            if (listStale)
                list.RemoveRange(staleIndex, list.Count - staleIndex);
        }
    
        class ContinuousDamageRecord
        {
            public string Actor { get; set; }

            public string Target { get; set; }

            public string Skill { get; set; }

            public DateTime Start { get; set; }
        }
    }
}