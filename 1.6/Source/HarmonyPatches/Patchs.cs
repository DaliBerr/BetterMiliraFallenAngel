using HarmonyLib;
using Milira;
using RimWorld;
using RimWorld.QuestGen;
using System.Reflection;
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

                MiliraDiplomacy.Restore(WorldComponent_BFA.Instance);
                CoreUtilities.ReconcileAfterLoad();
            }
        }

        [HarmonyPatch(typeof(MiliraGameComponent_OverallControl), nameof(MiliraGameComponent_OverallControl.CheckMiliraPermanentEnemyStatus))]
        public static class Patch_MiliraPermanentEnemyStatus
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                // Milira also rewrites its whitelist in LoadedGame(). Apply
                // BFA's persisted permission after that independent callback.
                MiliraDiplomacy.Restore(WorldComponent_BFA.Instance);
            }
        }

        [HarmonyPatch(typeof(GoodwillSituationWorker_PermanentEnemy), nameof(GoodwillSituationWorker_PermanentEnemy.ArePermanentEnemies))]
        public static class Patch_MiliraPermanentEnemies
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            [HarmonyAfter("Ancot.MiliraRaceHarmonyPatch")]
            public static void Postfix(Faction a, Faction b, ref bool __result)
            {
                bool miliraAndPlayer = a?.def == MiliraDefOf.Milira_Faction && b?.IsPlayer == true
                    || b?.def == MiliraDefOf.Milira_Faction && a?.IsPlayer == true;
                bool unlocked;
                if (miliraAndPlayer && MiliraDiplomacy.TryGetUnlocked(WorldComponent_BFA.Instance, out unlocked))
                    __result = !unlocked;
            }
        }

        [HarmonyPatch(typeof(GoodwillSituationWorker_SlightThaw), nameof(GoodwillSituationWorker_SlightThaw.Active))]
        public static class Patch_MiliraSlightThaw
        {
            [HarmonyPostfix]
            public static void Postfix(Faction other, ref bool __result)
            {
                bool unlocked;
                if (other?.def == MiliraDefOf.Milira_Faction
                    && MiliraDiplomacy.TryGetUnlocked(WorldComponent_BFA.Instance, out unlocked) && unlocked)
                    __result = false;
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

        [HarmonyPatch]
        public static class Patch_FallenMiliraReturn_GeneratePawnNewTemp
        {
            [HarmonyTargetMethod]
            public static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(QuestNode_Root_WandererJoin), "GeneratePawn_NewTemp")
                    ?? AccessTools.Method(typeof(QuestNode_Root_WandererJoin), "GeneratePawn");
            }

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
