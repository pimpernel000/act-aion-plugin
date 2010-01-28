using System;
using System.Collections.Generic;

namespace AionParse_Plugin
{
    public class UsingSkillRecordSetBase
    {
        public const int DefaultLookBackLimit = 30;

        private int lookBackLimit;

        private List<UsingSkillRecord> recordSet = new List<UsingSkillRecord>();

        public UsingSkillRecordSetBase(int lookBackLimit)
        {
            this.lookBackLimit = lookBackLimit;
        }

        public UsingSkillRecordSetBase()
            : this(DefaultLookBackLimit)
        {
        }

        public void Add(string actor, string target, string skill, DateTime start)
        {
            Add(actor, target, skill, start, lookBackLimit);
        }

        public void Add(string actor, string target, string skill, DateTime start, int duration)
        {
            recordSet.Insert(0, new UsingSkillRecord { Actor = actor, Target = target, Skill = skill, Start = start, Duration = duration });
        }

        public virtual void Clear()
        {
            if (recordSet.Count == 0) return;
            DateTime lastTime = recordSet[0].Start;
            Clear(lastTime);
        }

        public virtual void Clear(DateTime focusTime)
        {
            if (recordSet.Count == 0) return;

            for (int i = 1; i < recordSet.Count; i++)
            {
                double lookBackDistance = (focusTime - recordSet[i].Start).TotalSeconds;
                if (lookBackDistance > lookBackLimit)
                {
                    recordSet.RemoveRange(i, recordSet.Count - i);
                    break;
                }
            }
        }

        private class UsingSkillRecord
        {
            public string Actor { get; set; }

            public string Target { get; set; }

            public string Skill { get; set; }

            public DateTime Start { get; set; }

            public int Duration { get; set; }
        }
    }
}
