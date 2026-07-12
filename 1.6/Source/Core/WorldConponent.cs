using RimWorld;
using Verse;
using RimWorld.Planet;


namespace BetterFallenAngel
{
    public class WorldComponent_BFA : WorldComponent
    {

        public static WorldComponent_BFA Instance;

        public bool QuestActive => Quest != null
            && (Quest.State == QuestState.NotYetAccepted || Quest.State == QuestState.Ongoing);

        public bool suppressFADialog = false;
        // public bool isUnlocked = false;

        public ExtendBool isUnlocked = ExtendBool.Unset;
        public Pawn managedAngel;
        public FallenAngelStoryState storyState = FallenAngelStoryState.None;
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
            Scribe_Values.Look(ref isUnlocked, "BFA_isUnlocked", ExtendBool.Unset, true);
            Scribe_References.Look(ref managedAngel, "BFA_managedAngel");
            Scribe_Values.Look(ref storyState, "BFA_storyState", FallenAngelStoryState.None, true);
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
            storyState = FallenAngelStoryState.Active;
        }

        public bool RegisterInitialAngel(Pawn pawn)
        {
            if (pawn == null) return false;

            bool isNewAngel = managedAngel != pawn;
            managedAngel = pawn;
            if (isNewAngel || storyState == FallenAngelStoryState.Left)
            {
                storyState = FallenAngelStoryState.None;
                suppressFADialog = false;
                Quest = null;
            }
            return isNewAngel;
        }

        public void MarkPermanent(Pawn pawn)
        {
            managedAngel = pawn;
            storyState = FallenAngelStoryState.Permanent;
            suppressFADialog = true;
        }

        public void MarkRejected(Pawn pawn)
        {
            managedAngel = pawn;
            storyState = FallenAngelStoryState.Rejected;
        }

        public void MarkLeft(Pawn pawn)
        {
            if (pawn == null || managedAngel == pawn)
            {
                managedAngel = null;
            }
            storyState = FallenAngelStoryState.Left;
            Quest = null;
        }



    }
    public enum FallenAngelStoryState
    {
        None,
        Active,
        Permanent,
        Rejected,
        Left
    }

    public enum ExtendBool
    {
        True,
        False,
        Unset
    }
}
