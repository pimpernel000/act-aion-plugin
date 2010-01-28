using System;
using System.Collections.Generic;

namespace AionParse_Plugin
{
    public class UsingSkillRecordSetBase
    {
        const int DEFAULT_LOOKBACK_LIMIT = 30;

        public UsingSkillRecordSetBase(int lookBackLimit)
        {
            this.LookBackLimit = lookBackLimit;
        }

        public UsingSkillRecordSetBase()
            : this(DEFAULT_LOOKBACK_LIMIT)
        {
        }

        public int LookBackLimit;

        private class UsingSkillRecord
        {
            public string Actor { get; set; }
            public string Target { get; set; }
            public string Skill { get; set; }
            public DateTime Start { get; set; }
            public int Duration { get; set; }
        }

        private List<UsingSkillRecord> RecordSet = new List<UsingSkillRecord>();

        public void Add(string actor, string target, string skill, DateTime start)
        {
            Add(actor, target, skill, start, LookBackLimit);
        }

        public void Add(string actor, string target, string skill, DateTime start, int duration)
        {
            RecordSet.Insert(0, new UsingSkillRecord { Actor = actor, Target = target, Skill = skill, Start = start, Duration = duration });
        }

        virtual public void Clear()
        {
            if (RecordSet.Count == 0) return;
            DateTime lastTime = RecordSet[0].Start;
            Clear(lastTime);
        }

        virtual public void Clear(DateTime focusTime)
        {
            if (RecordSet.Count == 0) return;

            for (int i = 1; i < RecordSet.Count; i++)
            {
                double lookBackDistance = (focusTime - RecordSet[i].Start).TotalSeconds;
                if (lookBackDistance > LookBackLimit)
                {
                    RecordSet.RemoveRange(i, RecordSet.Count - i);
                    break;
                }
            }
        }
    }
}
