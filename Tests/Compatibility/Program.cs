using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;

string repo = args.Length > 0 ? Path.GetFullPath(args[0]) : Directory.GetCurrentDirectory();
string game = args.Length > 1 ? args[1] : @"C:\Program Files (x86)\Steam\steamapps\common\RimWorld";
int checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    checks++;
}

using AssemblyInfo milira = new(Path.Combine(repo, "../Rimworld_Assmb/Milira/3256974620/1.6/Assemblies/Milira.dll"));
foreach (var required in new[] {
    ("Milira.QuestNode_Root_FallenAngel", "GeneratePawn_NewTemp"),
    ("Milira.QuestNode_Root_FallenMiliraJoin_WalkIn", "GeneratePawn"),
    ("Milira.IncidentWorker_GiveQuestExceptMiliraScenario", "CanFireNowSub"),
    ("Milira.MiliraGameComponent_OverallControl", "CheckMiliraPermanentEnemyStatus"),
    ("Milira.GoodwillSituationWorker_SlightThaw", "Active") })
    Check(milira.HasMethod(required.Item1, required.Item2), $"Milira patch target missing: {required}");

foreach (string version in new[] { "1.6", "1.7" })
{
    string managed = version == "1.6" ? Path.Combine(repo, "1.6/ExternalMods") : game;
    using AssemblyInfo core = new(Path.Combine(managed, "Assembly-CSharp.dll"));
    using AssemblyInfo mod = new(Path.Combine(repo, version, "Assemblies/BetterMiliraFallenAngel.dll"));
    Check(core.Version.Major == 1 && core.Version.Minor == (version == "1.6" ? 6 : 7), "Wrong game reference version");
    foreach (var required in new[] {
        ("Verse.Game", "LoadGame"), ("Verse.Pawn", "Kill"),
        ("RimWorld.Quest", "End"), ("RimWorld.QuestGen.QuestNode_Root_WandererJoin", "TestRunInt"),
        ("RimWorld.GoodwillSituationWorker_PermanentEnemy", "ArePermanentEnemies"),
        ("RimWorld.Faction", "BaseGoodwillWith"), ("RimWorld.Faction", "TryAffectGoodwillWith"),
        ("RimWorld.Pawn_GuestTracker", "get_HostFaction"), ("RimWorld.Pawn_GuestTracker", "get_IsPrisoner"),
        ("RimWorld.Pawn_GuestTracker", "get_IsSlave"), ("RimWorld.Pawn_GuestTracker", "SetGuestStatus"),
        ("RimWorld.Planet.WorldComponent", "FinalizeInit") })
        Check(core.HasMethod(required.Item1, required.Item2), $"{version} game API missing: {required}");
    Check(core.HasMethod("RimWorld.QuestGen.QuestNode_Root_WandererJoin", "GeneratePawn_NewTemp")
        || core.HasMethod("RimWorld.QuestGen.QuestNode_Root_WandererJoin", "GeneratePawn"), "No supported return-quest target");
    Check(core.HasField("RimWorld.GoodwillSituationManager", "cachedData"), "Goodwill cap cache field missing");
    Check(mod.HasMethod("BetterFallenAngel.WorldComponent_BFA", "FinalizeInit"), "Early diplomacy restoration missing");
    Check(mod.HasMethod("BetterFallenAngel.MiliraDiplomacy", "Restore"), "Diplomacy repair missing");
    Check(mod.HasMethod("BetterFallenAngel.PawnMembership", "EnsurePlayerMembership"), "Bed repair missing");
    var references = mod.References();
    Check(references.ContainsKey(version == "1.6" ? "UnityEngine.CoreModule" : "UnityStubs"), "Wrong engine reference");
    Check(references["0Harmony"].Major == (version == "1.6" ? 2 : 3), "Wrong Harmony major version");

    XElement loadFolders = XDocument.Load(Path.Combine(repo, "LoadFolders.xml")).Root.Element("v" + version);
    var dlls = loadFolders.Elements("li").SelectMany(folder => {
        string directory = Path.Combine(repo, folder.Value == "/" ? "" : folder.Value, "Assemblies");
        return Directory.Exists(directory) ? Directory.EnumerateFiles(directory, "*.dll", SearchOption.AllDirectories) : Array.Empty<string>();
    }).ToArray();
    Check(dlls.Length == 1 && Path.GetFullPath(dlls[0]) == Path.GetFullPath(Path.Combine(repo, version, "Assemblies/BetterMiliraFallenAngel.dll")),
        $"{version} loads duplicate or foreign DLLs");
    Check(loadFolders.Elements("li").Any(folder => folder.Value == "Common"), "Shared definitions are not loaded");
    Console.WriteLine($"PASS: {version}; reference {core.Version}; Harmony {references["0Harmony"]}; one matching mod DLL.");
}

foreach (string xml in Directory.EnumerateFiles(Path.Combine(repo, "Common"), "*.xml", SearchOption.AllDirectories))
{
    XDocument.Load(xml);
    checks++;
}
Console.WriteLine($"PASS: {checks} API, artifact, XML and load-folder checks.");

sealed class AssemblyInfo : IDisposable
{
    private readonly FileStream stream;
    private readonly PEReader pe;
    private readonly MetadataReader metadata;
    private readonly Dictionary<string, TypeDefinition> types;
    public Version Version => metadata.GetAssemblyDefinition().Version;
    public AssemblyInfo(string path)
    {
        stream = File.OpenRead(path);
        pe = new PEReader(stream);
        metadata = pe.GetMetadataReader();
        types = metadata.TypeDefinitions.Select(metadata.GetTypeDefinition)
            .Where(type => !type.IsNested).ToDictionary(type => metadata.GetString(type.Namespace) + "." + metadata.GetString(type.Name));
    }
    public bool HasMethod(string type, string method) => types.TryGetValue(type, out var definition)
        && definition.GetMethods().Select(metadata.GetMethodDefinition).Any(member => metadata.GetString(member.Name) == method);
    public bool HasField(string type, string field) => types.TryGetValue(type, out var definition)
        && definition.GetFields().Select(metadata.GetFieldDefinition).Any(member => metadata.GetString(member.Name) == field);
    public Dictionary<string, Version> References() => metadata.AssemblyReferences.Select(metadata.GetAssemblyReference)
        .ToDictionary(reference => metadata.GetString(reference.Name), reference => reference.Version);
    public void Dispose() { pe.Dispose(); stream.Dispose(); }
}
