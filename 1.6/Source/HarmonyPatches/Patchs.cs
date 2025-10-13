using Verse;
using HarmonyLib;
using Milira;
using RimWorld;
using System.Linq;
namespace BetterFallenAngel
{
    public static class Patches
    {
        [HarmonyPatch(typeof(QuestNode_Root_FallenAngel), "GeneratePawn")]
        public static class Postfix_GeneratePawn
        {

            [HarmonyPostfix]
            public static void GeneratePawn_Postfix(ref Pawn __result)
            {
                if (__result != null)
                {
                    __result.health.AddHediff(FallenMiliraDefOf.Milira_FallenAngelMark);
                    if (!__result.health.hediffSet.HasHediff(FallenMiliraDefOf.Milira_FallenAngelAura))
                    {
                        var h = HediffMaker.MakeHediff(FallenMiliraDefOf.Milira_FallenAngelAura, __result);
                        __result.health.AddHediff(h);
                    }
                }
                CoreUtilities.UnlockGoodWill(ExtendBool.False);
            }
        }

        [HarmonyPatch(typeof(Game), nameof(Game.LoadGame))]
        public static class Patch_Game_LoadGame
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                // var miliraPlayerFactionDef = DefDatabase<FactionDef>.GetNamedSilentFail("Milira_PlayerFaction");
                // var kiiroPlayerFactionDef = DefDatabase<FactionDef>.GetNamedSilentFail("Kiiro_PlayerFaction");
                // var kiiroFaction = DefDatabase<FactionDef>.GetNamedSilentFail("Kiiro_PlayerFaction");


                // if (Find.FactionManager.OfPlayer.def == miliraPlayerFactionDef || Find.FactionManager.OfPlayer.def == kiiroPlayerFactionDef || Find.FactionManager.OfPlayer.def == kiiroFaction)
                // {
                //     if (WorldComponent_BFA.Instance != null)
                //     {
                //         WorldComponent_BFA.Instance.isUnlocked = ExtendBool.True;
                //         CoreUtilities.UnlockGoodWill(WorldComponent_BFA.Instance.isUnlocked);

                //     }
                // }
                if (WorldComponent_BFA.Instance != null)
                {
                    CoreUtilities.UnlockGoodWill(WorldComponent_BFA.Instance.isUnlocked);
                }
            }
        }
    }
}