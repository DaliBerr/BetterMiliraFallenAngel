using HarmonyLib;
using Milira;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace BetterFallenAngel
{
    public static class Patches
    {
        [HarmonyPatch(typeof(QuestNode_Root_FallenAngel), nameof(QuestNode_Root_FallenAngel.GeneratePawn_NewTemp))]
        public static class Patch_FallenAngel_GeneratePawnNewTemp
        {
            [HarmonyPostfix]
            public static void Postfix(Pawn __result)
            {
                CoreUtilities.PrepareInitialFallenAngel(__result);
            }
        }

        [HarmonyPatch(typeof(Game), nameof(Game.LoadGame))]
        public static class Patch_Game_LoadGame
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (WorldComponent_BFA.Instance == null) return;

                CoreUtilities.UnlockGoodWill(WorldComponent_BFA.Instance.isUnlocked);
                CoreUtilities.ReconcileAfterLoad();
            }
        }

        [HarmonyPatch(typeof(Quest), nameof(Quest.End))]
        public static class Patch_Quest_End
        {
            [HarmonyPostfix]
            public static void Postfix(Quest __instance, QuestEndOutcome outcome)
            {
                WorldComponent_BFA component = WorldComponent_BFA.Instance;
                if (__instance == null || component == null || component.Quest != __instance) return;

                CoreUtilities.QuestPart_FinalizePermanentStay finalize = __instance.PartsListForReading
                    .Find(part => part is CoreUtilities.QuestPart_FinalizePermanentStay)
                    as CoreUtilities.QuestPart_FinalizePermanentStay;

                if (outcome == QuestEndOutcome.Success && finalize?.completed == true)
                {
                    CoreUtilities.SyncPermanentAngel(finalize.pawn ?? component.managedAngel);
                    return;
                }

                if (outcome != QuestEndOutcome.Success)
                {
                    CoreUtilities.MarkAngelLeft(component.managedAngel);
                }
            }
        }

        [HarmonyPatch(typeof(QuestNode_Root_WandererJoin), nameof(QuestNode_Root_WandererJoin.GeneratePawn_NewTemp))]
        public static class Patch_FallenMiliraReturn_GeneratePawnNewTemp
        {
            [HarmonyPrefix]
            public static bool Prefix(QuestNode_Root_WandererJoin __instance, ref Pawn __result)
            {
                QuestNode_Root_FallenMiliraJoin_WalkIn returnNode = __instance as QuestNode_Root_FallenMiliraJoin_WalkIn;
                if (returnNode == null) return true;

                __result = returnNode.GeneratePawn();
                return false;
            }
        }

        [HarmonyPatch(typeof(QuestNode_Root_WandererJoin), "TestRunInt")]
        public static class Patch_FallenMiliraReturn_TestRun
        {
            [HarmonyPostfix]
            public static void Postfix(QuestNode_Root_WandererJoin __instance, ref bool __result)
            {
                if (!(__instance is QuestNode_Root_FallenMiliraJoin_WalkIn)) return;

                MiliraGameComponent_OverallControl control = CoreUtilities.MiliraControl;
                Pawn pawn = control?.pawn;
                Faction miliraFaction = Find.FactionManager.FirstFactionOfDef(MiliraDefOf.Milira_Faction);
                __result = __result
                    && miliraFaction != null
                    && !miliraFaction.HostileTo(Faction.OfPlayer)
                    && pawn != null
                    && !pawn.Dead
                    && pawn.Faction?.def == MiliraDefOf.Milira_Faction;
            }
        }

        [HarmonyPatch(typeof(IncidentWorker_GiveQuestExceptMiliraScenario), "CanFireNowSub")]
        public static class Patch_MiliraChurchIncident_CanFire
        {
            [HarmonyPostfix]
            public static void Postfix(IncidentWorker_GiveQuestExceptMiliraScenario __instance, ref bool __result)
            {
                FallenAngelStoryState state = WorldComponent_BFA.Instance?.storyState ?? FallenAngelStoryState.None;
                if (state != FallenAngelStoryState.Active && state != FallenAngelStoryState.Permanent) return;

                if (__instance?.def?.defName == "Milira_FallenAngel_ToChurch")
                {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
        public static class Patch_ManagedAngel_Kill
        {
            [HarmonyPostfix]
            public static void Postfix(Pawn __instance)
            {
                WorldComponent_BFA component = WorldComponent_BFA.Instance;
                if (component?.managedAngel != __instance) return;

                Quest quest = component.Quest;
                if (quest != null
                    && (quest.State == QuestState.NotYetAccepted || quest.State == QuestState.Ongoing))
                {
                    quest.End(QuestEndOutcome.Fail, false, false);
                }
                else
                {
                    CoreUtilities.MarkAngelLeft(__instance);
                }
            }
        }
    }
}
