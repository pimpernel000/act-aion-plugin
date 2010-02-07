using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AionParsePlugin
{
    public class UsingSkillRecordSetBase : System.ComponentModel.BindingList<UsingSkillRecord>
    {
        public const int DefaultLookBackLimit = 30;

        private int lookBackLimit;

        public UsingSkillRecordSetBase(int lookBackLimit)
        {
            this.lookBackLimit = lookBackLimit;
            this.SummonedPets = new List<string>();
        }

        public UsingSkillRecordSetBase()
            : this(DefaultLookBackLimit)
        {
        }

        private List<string> SummonedPets { get; set; }

        public void Add(string actor, string target, string skill, DateTime start)
        {
            Add(actor, target, skill, start, lookBackLimit);  // TODO: instead of using lookBackLimit, do a lookup on the skill/pet durations
        }

        public void Add(string actor, string target, string skill, DateTime start, int duration)
        {
            this.Insert(0, new UsingSkillRecord { Actor = actor, Target = target, Skill = skill, Start = start, Duration = duration });
        }

        public void Add(string actor, string target, string skill, string pet, DateTime summonTime, int petDuration)
        {
            this.Insert(0, new UsingSkillRecord { Actor = actor, Target = target, Skill = skill, Pet = pet, Start = summonTime, Duration = petDuration });
            if (!string.IsNullOrEmpty(pet))
                SummonedPets.Add(pet);
        }

        public bool IsSummonedPet(string pet)
        {
            return SummonedPets.Contains(pet);
        }

        public string GetActor(string target, string skill, DateTime now)
        {
            foreach (var record in this.Items)
            {
                if (record.Match(target, skill, now))
                    return record.Actor;
            }

            return null;
        }

        public string GetPartyCaster(string skill, DateTime now)
        {
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
            foreach (var record in this.Items)
            {
                if (record.MatchPet(target, pet, now))
                    return record;
            }

            return null;
        }

        public new void Clear()
        {
            if (this.Items.Count == 0) return;
            DateTime lastTime = this.Items[0].Start;
            Clear(lastTime);
        }

        public void Clear(DateTime focusTime)
        {
            if (this.Items.Count == 0) return;

            for (int i = this.Items.Count - 1; i >= 0; i--)
            {
                if (this.Items[i].Duration == 0) continue; // Duration 0 = does not expire
                double elapsedTime = (focusTime - this.Items[i].Start).TotalSeconds;
                if (elapsedTime > this.Items[i].Duration)
                    this.Items.RemoveAt(i);
            }
        }
    }

    public class UsingSkillRecord
    {
        public UsingSkillRecord() 
        { 
        }

        public UsingSkillRecord(string actor, string skill) : this()
        {
            Actor = actor;
            Skill = skill;
            Target = null;
            Duration = 0;
        }

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
                ((target == Target &&
                    skill == Skill) ||
                 (target == null &&
                    (skill == Skill || Regex.IsMatch(skill, Skill + " (I(X|V)?|(X|V)?I{0,3})")))) &&
                (time <= End || Duration == 0);
        }

        public bool MatchPet(string target, string pet, DateTime time)
        {
            return
                pet == Pet &&
                (target == Target || !AionData.Pet.IsTargettedPet(pet)) &&
                (time <= End || Duration == 0);
        }
    }
}
