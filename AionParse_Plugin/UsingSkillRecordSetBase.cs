using System;
using System.Collections.Generic;

namespace AionParsePlugin
{
    public class UsingSkillRecordSetBase
    {
        public const int DefaultLookBackLimit = 30;

        private int lookBackLimit;

        private List<UsingSkillRecord> recordSet = new List<UsingSkillRecord>();

        private List<string> summonedPets = new List<string>();

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
            Add(actor, target, skill, start, lookBackLimit);  // TODO: instead of using lookBackLimit, do a lookup on the skill/pet durations
        }

        public void Add(string actor, string target, string skill, DateTime start, int duration)
        {
            recordSet.Insert(0, new UsingSkillRecord { Actor = actor, Target = target, Skill = skill, Start = start, Duration = duration });
        }

        public void Add(string actor, string target, string skill, string pet, DateTime summonTime, int petDuration)
        {
            recordSet.Insert(0, new UsingSkillRecord { Actor = actor, Target = target, Skill = skill, Pet = pet, Start = summonTime, Duration = petDuration });
            if (!string.IsNullOrEmpty(pet))
                summonedPets.Add(pet);
        }

        public bool IsSummonedPet(string pet)
        {
            return summonedPets.Contains(pet);
        }

        public string GetActor(string target, string skill, DateTime now)
        {
            foreach (var record in recordSet)
            {
                if (record.Match(target, skill, now))
                    return record.Actor;
            }

            return null;
        }

        public string GetPartyCaster(string skill, DateTime now) {
            return GetActor(null, skill, now);
        }

        public string GetAnyActor(string target, string skill, DateTime now)
        {
            string actor = GetActor(target, skill, now);
            if (String.IsNullOrEmpty(actor))
                actor = GetPartyCaster(skill, now);
            return actor;
        }

        public UsingSkillRecord GetSummonerRecord(string target, string pet, DateTime now)
        {
            foreach (var record in recordSet)
            {
                if (record.MatchPet(target, pet, now))
                    return record;
            }

            return null;
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

            for (int i = recordSet.Count - 1; i >= 0; i--)
            {
                double elapsedTime = (focusTime - recordSet[i].Start).TotalSeconds;
                if (elapsedTime > recordSet[i].Duration)
                    recordSet.RemoveAt(i);
            }
        }

        public class UsingSkillRecord
        {
            public string Actor { get; set; }

            public string Target { get; set; }

            public string Skill { get; set; }

            public string Pet { get; set; }

            public int Duration { get; set; }

            public DateTime Start { get; set; }

            public DateTime End
            {
                get
                {
                    return Start.AddSeconds(Duration);
                }
            }

            public bool Match(string target, string skill, DateTime time)
            {
                return
                    target == Target &&
                    skill == Skill &&
                    time <= End;
            }

            public bool MatchPet(string target, string pet, DateTime time)
            {
                return
                    (target == Target || !AionData.Pet.IsTargettedPet(pet)) &&
                    pet == Pet &&
                    time <= End;
            }
        }
    }
}
