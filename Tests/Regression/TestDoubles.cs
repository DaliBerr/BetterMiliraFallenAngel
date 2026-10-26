// Headless doubles for the specific engine side effects exercised by these
// regressions. Their behavior follows Pawn_GuestTracker, Pawn_Ownership,
// Faction and GoodwillSituationManager in the supplied decompiled game code.
using System.Xml.Linq;
using BetterFallenAngel;

namespace RimWorld
{
    public class FactionDef
    {
        public string defName;
        public string categoryTag;
        public bool permanentEnemy;
        public List<FactionDef> permanentEnemyToEveryoneExcept;
    }

    public class Faction
    {
        public static Faction OfPlayer;
        public static Faction OfPlayerSilentFail => OfPlayer;
        public FactionDef def;
        public int BaseGoodwill;
        public bool Hostile;
        public int GoodwillWrites;
        public bool IsPlayer => this == OfPlayer;

        public int BaseGoodwillWith(Faction other) => BaseGoodwill;
        public int GoodwillWith(Faction other) => Math.Min(BaseGoodwill, Verse.Find.GoodwillSituationManager.GetCap(this));
        public bool TryAffectGoodwillWith(Faction other, int amount, bool canSendMessage = true, bool canSendHostilityLetter = true)
        {
            GoodwillWrites++;
            BaseGoodwill = Math.Clamp(BaseGoodwill + amount, -100, 100);
            Hostile = BaseGoodwill <= -75;
            return true;
        }
    }

    public class FactionManager
    {
        public List<Faction> AllFactionsListForReading = new();
    }

    public class GoodwillSituationManager
    {
        private readonly Dictionary<Faction, int> cachedData = new();
        public void SeedStaleCap(Faction faction) => cachedData[faction] = -100;
        public int GetCap(Faction faction)
        {
            if (!cachedData.TryGetValue(faction, out int cap))
            {
                cap = faction.def.permanentEnemy
                    || !faction.def.permanentEnemyToEveryoneExcept.Contains(Faction.OfPlayer.def) ? -100 : 100;
                cachedData[faction] = cap;
            }
            return cap;
        }
    }

    public enum QuestState { NotYetAccepted, Ongoing, EndedSuccess, EndedFailed }
    public class Quest { public int id; public QuestState State; }
    public class QuestManager { public List<Quest> QuestsListForReading = new(); }

    public class Pawn_GuestTracker
    {
        public Verse.Pawn pawn;
        public Faction HostFaction;
        public bool IsPrisoner;
        public bool IsSlave;
        public int SetStatusCalls;
        public void SetGuestStatus(Faction host)
        {
            SetStatusCalls++;
            HostFaction = host;
            IsPrisoner = IsSlave = false;
            pawn.SurgeryBills = 0;
            // Ordinary colonist bed + null HostFaction triggers UnclaimBed in
            // the original Notify_ChangedGuestStatus(), including on no-ops.
            if (host == null) pawn.OwnedBed = null;
        }
    }

    public static class RecruitUtility
    {
        public static void Recruit(Verse.Pawn pawn, Faction faction)
        {
            pawn.RecruitCalls++;
            pawn.SetFaction(faction);
        }
    }
}

namespace RimWorld.Planet
{
    public class World { }
    public class WorldComponent
    {
        public WorldComponent(World world) { }
        public virtual void ExposeData() { }
        public virtual void FinalizeInit(bool fromLoad) { }
    }
}

namespace Verse
{
    public class Pawn
    {
        public string Id;
        public bool Dead;
        public RimWorld.Faction Faction;
        public RimWorld.Pawn_GuestTracker guest;
        public object OwnedBed;
        public int SurgeryBills;
        public int SetFactionCalls;
        public int RecruitCalls;
        public bool IsPrisonerOfColony => guest?.IsPrisoner == true && guest.HostFaction == RimWorld.Faction.OfPlayer;
        public bool IsSlaveOfColony => guest?.IsSlave == true;
        public void SetFaction(RimWorld.Faction faction)
        {
            SetFactionCalls++;
            guest?.SetGuestStatus(null);
            Faction = faction;
        }
    }

    public static class Find
    {
        public static RimWorld.FactionManager FactionManager;
        public static RimWorld.GoodwillSituationManager GoodwillSituationManager;
        public static RimWorld.QuestManager QuestManager;
    }

    public static class DefDatabase<T> where T : RimWorld.FactionDef
    {
        public static Dictionary<string, T> Values = new();
        public static T GetNamedSilentFail(string name) => Values.GetValueOrDefault(name);
    }

    public enum LoadSaveMode { Inactive, Saving, LoadingVars, ResolvingCrossRefs, PostLoadInit }
    public static class Scribe
    {
        public static LoadSaveMode mode;
        public static XElement Data;
        public static Dictionary<string, Pawn> Pawns = new();
    }

    public static class Scribe_Values
    {
        public static void Look<T>(ref T value, string label, T defaultValue = default, bool forceSave = false)
        {
            if (Scribe.mode == LoadSaveMode.Saving)
                Scribe.Data.Add(new XElement(label, value));
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                string text = Scribe.Data.Element(label)?.Value;
                value = text == null ? defaultValue : typeof(T).IsEnum
                    ? (T)Enum.Parse(typeof(T), text) : (T)Convert.ChangeType(text, typeof(T));
            }
        }
    }

    public static class Scribe_References
    {
        public static void Look(ref Pawn pawn, string label)
        {
            if (Scribe.mode == LoadSaveMode.Saving)
                Scribe.Data.Add(new XElement(label, pawn?.Id ?? "null"));
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
                pawn = Scribe.Pawns.GetValueOrDefault(Scribe.Data.Element(label)?.Value ?? "null");
        }
    }
}
