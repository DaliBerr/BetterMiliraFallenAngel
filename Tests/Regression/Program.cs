using System.Xml.Linq;
using BetterFallenAngel;
using RimWorld;
using RimWorld.Planet;
using Verse;

int passed = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    passed++;
}

(WorldComponent_BFA Component, Faction Milira) Setup(int goodwill, ExtendBool flag = ExtendBool.True,
    string playerName = "PlayerColony")
{
    DefDatabase<FactionDef>.Values.Clear();
    FactionDef playerDef = new() { defName = playerName };
    FactionDef miliraDef = new() { defName = "Milira_Faction", permanentEnemyToEveryoneExcept = new() };
    foreach (FactionDef def in new[] { playerDef, miliraDef,
        new FactionDef { defName = "Milira_PlayerFaction" }, new FactionDef { defName = "Kiiro_Faction" } })
        DefDatabase<FactionDef>.Values[def.defName] = def;
    Faction.OfPlayer = new Faction { def = playerDef };
    Faction milira = new() { def = miliraDef, BaseGoodwill = goodwill, Hostile = goodwill <= -75 };
    Find.FactionManager = new() { AllFactionsListForReading = new() { Faction.OfPlayer, milira } };
    Find.GoodwillSituationManager = new();
    Find.QuestManager = new();
    Scribe.mode = LoadSaveMode.Inactive;
    return (new WorldComponent_BFA(new World()) { isUnlocked = flag }, milira);
}

string Save(WorldComponent_BFA component)
{
    Scribe.Data = new XElement("li", new XAttribute("Class", "BetterFallenAngel.WorldComponent_BFA"));
    Scribe.mode = LoadSaveMode.Saving;
    component.ExposeData();
    Scribe.mode = LoadSaveMode.Inactive;
    return Scribe.Data.ToString();
}

WorldComponent_BFA Load(string xml)
{
    WorldComponent_BFA component = new(new World());
    Scribe.Data = XElement.Parse(xml);
    Scribe.mode = LoadSaveMode.LoadingVars;
    component.ExposeData();
    component.FinalizeInit(true);
    Scribe.mode = LoadSaveMode.Inactive;
    return component;
}

// Exercise the actual serialized WorldComponent and both early load callbacks.
// Preserve hostile relations, neutral relations and alliances over 50 reloads.
foreach (int savedGoodwill in new[] { -100, -85, -20, 0, 35, 80, 100 })
{
    var fixture = Setup(savedGoodwill);
    string xml = Save(fixture.Component);
    for (int reload = 0; reload < 50; reload++)
    {
        fixture.Milira.def.permanentEnemy = true;
        fixture.Milira.def.permanentEnemyToEveryoneExcept.RemoveAll(def => def == Faction.OfPlayer.def);
        Find.GoodwillSituationManager.SeedStaleCap(fixture.Milira);
        WorldComponent_BFA loaded = Load(xml);
        Check(fixture.Milira.GoodwillWith(Faction.OfPlayer) == savedGoodwill, "Reload changed effective goodwill");
        Check(fixture.Milira.BaseGoodwill == savedGoodwill, "Reload changed serialized base goodwill");
        Check(fixture.Milira.Hostile == (savedGoodwill <= -75), "Reload stopped a hostile raid");
        Check(fixture.Milira.GoodwillWrites == 0, "Reload replayed a diplomatic reward");
        Check(loaded.isUnlocked == ExtendBool.True, "Unlock permission did not survive XML roundtrip");
        xml = Save(loaded);
    }
}

// An actual unlock event grants neutrality once; subsequent insults survive
// both repeated events and Milira's later whitelist rewrite in LoadedGame().
{
    var fixture = Setup(-85, ExtendBool.False);
    Find.GoodwillSituationManager.SeedStaleCap(fixture.Milira);
    MiliraDiplomacy.SetUnlocked(fixture.Component, ExtendBool.True);
    Check(fixture.Milira.BaseGoodwill == 0 && fixture.Milira.GoodwillWrites == 1, "Initial unlock reward failed");
    fixture.Milira.BaseGoodwill = -90;
    fixture.Milira.Hostile = true;
    MiliraDiplomacy.SetUnlocked(fixture.Component, ExtendBool.True);
    Check(fixture.Milira.BaseGoodwill == -90 && fixture.Milira.Hostile, "Repeated unlock erased player hostility");
    fixture.Milira.def.permanentEnemyToEveryoneExcept.RemoveAll(def => def == Faction.OfPlayer.def);
    Find.GoodwillSituationManager.SeedStaleCap(fixture.Milira);
    MiliraDiplomacy.Restore(fixture.Component);
    Check(fixture.Milira.GoodwillWith(Faction.OfPlayer) == -90, "Milira callback reinstated a stale cap");
}

// Legacy migration must restore permission without granting neutrality.
foreach (FallenAngelStoryState state in new[] { FallenAngelStoryState.Active, FallenAngelStoryState.Permanent })
{
    var fixture = Setup(-85, ExtendBool.Unset);
    fixture.Component.storyState = state;
    var loaded = Load(Save(fixture.Component));
    Check(loaded.isUnlocked == ExtendBool.True, "Legacy story state was not migrated");
    Check(fixture.Milira.BaseGoodwill == -85 && fixture.Milira.GoodwillWrites == 0, "Migration replayed a reward");
}
{
    var fixture = Setup(35, ExtendBool.Unset);
    fixture.Component.suppressFADialog = true;
    Check(Load(Save(fixture.Component)).isUnlocked == ExtendBool.True, "Legacy dialog flag was not migrated");
}
{
    var fixture = Setup(-85, ExtendBool.Unset);
    fixture.Component.questId = 123;
    Find.QuestManager.QuestsListForReading.Add(new Quest { id = 123, State = QuestState.Ongoing });
    Check(Load(Save(fixture.Component)).isUnlocked == ExtendBool.True, "Legacy active quest was not migrated");
}
{
    var fixture = Setup(35, ExtendBool.Unset);
    var loaded = Load(Save(fixture.Component));
    Check(loaded.isUnlocked == ExtendBool.Unset, "Unmanaged diplomacy was claimed by BFA");
    Check(fixture.Milira.GoodwillWrites == 0, "Unmanaged diplomacy was modified");
}

// Explicit locks preserve base goodwill and keep native friendly exemptions.
{
    var fixture = Setup(35, ExtendBool.False);
    Load(Save(fixture.Component));
    Check(fixture.Milira.BaseGoodwill == 35 && fixture.Milira.GoodwillWith(Faction.OfPlayer) == -100,
        "Locked diplomacy changed saved goodwill");
    Check(fixture.Milira.def.permanentEnemyToEveryoneExcept.All(def => def != null), "Null whitelist entry");
    for (int i = 0; i < 50; i++) MiliraDiplomacy.Restore(fixture.Component);
    Check(fixture.Milira.def.permanentEnemyToEveryoneExcept.Count == 2, "Whitelist grows on each reload");
}
{
    var fixture = Setup(35, ExtendBool.False, "Milira_PlayerFaction");
    MiliraDiplomacy.Restore(fixture.Component);
    Check(fixture.Component.isUnlocked == ExtendBool.True && fixture.Milira.BaseGoodwill == 35,
        "Native friendly player faction was locked or rewarded again");
}
{
    var fixture = Setup(-85, ExtendBool.False, "Milira_Imperium_PlayerFaction");
    MiliraDiplomacy.SetUnlocked(fixture.Component, ExtendBool.True);
    Check(fixture.Component.isUnlocked == ExtendBool.False && fixture.Milira.GoodwillWrites == 0,
        "Imperium compatibility exemption was changed");
}

// Reconciliation of an existing colonist must not clear beds or surgery bills.
{
    Setup(0);
    object bed = new();
    Pawn pawn = new() { Faction = Faction.OfPlayer, OwnedBed = bed, SurgeryBills = 3 };
    pawn.guest = new Pawn_GuestTracker { pawn = pawn };
    for (int i = 0; i < 50; i++) PawnMembership.EnsurePlayerMembership(pawn);
    Check(ReferenceEquals(pawn.OwnedBed, bed), "Colonist bed was unclaimed on reload");
    Check(pawn.SurgeryBills == 3, "Surgery bills were cleared on reload");
    Check(pawn.guest.SetStatusCalls == 0 && pawn.SetFactionCalls == 0 && pawn.RecruitCalls == 0,
        "Reload repeated an already completed recruitment");
}
foreach (bool slave in new[] { false, true })
{
    Setup(0);
    Pawn pawn = new() { Faction = new Faction() };
    pawn.guest = new Pawn_GuestTracker { pawn = pawn, HostFaction = Faction.OfPlayer,
        IsPrisoner = !slave, IsSlave = slave };
    PawnMembership.EnsurePlayerMembership(pawn);
    object bed = pawn.OwnedBed = new();
    int statusCalls = pawn.guest.SetStatusCalls;
    for (int i = 0; i < 50; i++) PawnMembership.EnsurePlayerMembership(pawn);
    Check(pawn.Faction == Faction.OfPlayer && pawn.RecruitCalls == 1, "Captive was not recruited exactly once");
    Check(ReferenceEquals(pawn.OwnedBed, bed) && pawn.guest.SetStatusCalls == statusCalls,
        "Converted captive lost a newly assigned bed on reload");
}

Console.WriteLine($"PASS: {passed} assertions; repeated XML load, diplomacy, cache, migration and bed regressions.");
