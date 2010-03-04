using System;
using System.Collections.Generic;
using System.Text;
using AionData;

namespace AionParsePlugin
{
    class PartyRecordSet : System.ComponentModel.BindingList<Player>
    {
        private Dictionary<string, Player> hash = new Dictionary<string, Player>();

        public void Add(string name)
        {
            this.Add(new AionData.Player() { Name = name, Class = Player.Classes.Unknown });
        }

        public bool Contains(string name)
        {
            return hash.ContainsKey(name);
        }

        public Player Find(string name)
        {
            if (!this.Contains(name)) return null;
            return hash[name];
        }

        public void Replace(string oldGuy, string newGuy)
        {
            int oldIndex = this.FindCore(oldGuy);
            this.RemoveAt(oldIndex);
            this.Add(newGuy);
        }

        public List<Player> FindByClass(Player.Classes playerClass)
        {
            List<Player> list = new List<Player>();
            foreach (Player player in Items)
            {
                if (player.Class == playerClass)
                    list.Add(player);
            }

            return list;
        }

        public void SetClass(string actor, string skill)
        {
            Player player = Find(actor);
            if (player == null || Player.IsTier2Class(player.Class)) return;
            player.Class = Skill.GuessClass(skill);
        }

        public void SetClass(string actor, Player.Classes playerClass)
        {
            Player player = Find(actor);
            if (player == null || Player.IsTier2Class(player.Class)) return;
            player.Class = playerClass;
        }

        protected override int FindCore(System.ComponentModel.PropertyDescriptor prop, object key)
        {
            return FindCore(prop, key);
        }

        protected int FindCore(string key)
        {
            for (int i = 0; i < Count; ++i)
                if (Items[i].Name.ToLower() == key.ToLower())
                    return i;

            return -1;
        }

        protected override void OnAddingNew(System.ComponentModel.AddingNewEventArgs e)
        {
            if (e.NewObject != null)
            {
                Player newCore = (Player)e.NewObject;
                hash.Add(newCore.Name, newCore);
            }

            base.OnAddingNew(e);
        }
    }
}
