using Verse;
using HarmonyLib;
using Milira;
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
            }
        } 
    }
}