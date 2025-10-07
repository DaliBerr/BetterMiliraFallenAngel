using RimWorld;
using Verse;
using System;
using RimWorld.Planet;


namespace BetterFallenAngel
{
    public class WorldComponent_BFA : WorldComponent
    {

        public static WorldComponent_BFA Instance;

        public bool QuestActive => Quest != null && Quest.State == QuestState.Ongoing;

        public bool suppressFADialog = false;
        private Quest _quest;

        public int questId = -1;
        public WorldComponent_BFA(World world) : base(world)
        {
            Instance = this;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            // Scribe_Values.Look(ref isUnlocked, "isUnlocked", false);
            // Scribe_References.Look(ref quest, "quest");
            Scribe_Values.Look(ref questId, "questId", -1, true);
            Scribe_Values.Look(ref suppressFADialog, "BFA_suppressFADialog", false, true);
        }
        public Quest Quest
        {
            get
            {
                if (_quest == null && questId >= 0)
                    _quest = GetQuest(questId);
                return _quest;
            }
            set
            {
                _quest = value;
                questId = value?.id ?? -1;

            }
        }

        public Quest GetQuest(int id)
        {
            var quests = Find.QuestManager.QuestsListForReading;
            foreach (var q in quests)
            {
                if (q.id == id) return q;
            }
            return null;
        }

        public void RegisterQuest(Quest quest, string uniqueSignal = null)
        {
            this.Quest = quest;
            // Log.Message($"[BFA] Registered quest #{quest?.id}, signal='{uniqueSignal}'");
        }
    }
}