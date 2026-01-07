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
                // if()
                if (WorldComponent_BFA.Instance != null)
                {
                    CoreUtilities.FixLegacyQuest(WorldComponent_BFA.Instance.Quest);
                    CoreUtilities.UnlockGoodWill(WorldComponent_BFA.Instance.isUnlocked);
                }
            }
        }

        [HarmonyPatch(typeof(Quest), nameof(Quest.End))]
        public static class Patch_Quest_End_ForcePermanentStay
        {
            /// <summary>
            /// summary: 在 Quest.End 完成后（清理已发生），如果是“接受线留下”导致的成功结束，则把天使强制转回玩家阵营并清访客状态。
            /// param: __instance 当前结束的任务实例
            /// param: outcome 任务结局
            /// return: 无
            /// </summary>
            [HarmonyPostfix]
            public static void Postfix(Quest __instance, QuestEndOutcome outcome)
            {
                Log.Message("[BetterMiliraFallenAngel] Quest.End postfix triggered for quest " + __instance?.name + " (ID: " + __instance?.id + ")");
                if (__instance == null)
                {
                    Log.Warning("[BetterMiliraFallenAngel] Quest.End postfix found null quest instance.");
                    return;
                }
                if (outcome != QuestEndOutcome.Success){
                    Log.Message("[BetterMiliraFallenAngel] Quest.End postfix exiting because outcome is not Success: " + outcome);
                    return;
                }
                // 只处理你这个 Mod 的那条“当前注册任务”
                // if (WorldComponent_BFA.Instance == null) return;
                // if (WorldComponent_BFA.Instance.Quest != __instance) return;

                // 只在“玩家走了通讯器留下选项”后才执行，避免误伤其他成功结束
                // （你对话里选择留下会把 suppressFADialog 设为 true）:contentReference[oaicite:5]{index=5}
                // if (!WorldComponent_BFA.Instance.suppressFADialog) return;

                Pawn angel = TryFindMarkedAngelFromQuest(__instance) ?? TryFindMarkedAngelOnAnyPlayerMap();
                if (angel == null){
                    Log.Warning("[BetterMiliraFallenAngel] Could not find Fallen Angel pawn to make permanent colonist.");
                    return;
                }
                ForcePawnToBeColonist(angel);
            }

            /// <summary>
            /// summary: 优先从任务的 JoinPlayer 部件里找带 FallenAngelMark 的 Pawn（最准确）。
            /// param: quest 当前任务
            /// return: 找到则返回 Pawn，否则返回 null
            /// </summary>
            private static Pawn TryFindMarkedAngelFromQuest(Quest quest)
            {
                try
                {
                    return quest.PartsListForReading
                        .OfType<QuestPart_JoinPlayer>()
                        .SelectMany(p => p.pawns ?? Enumerable.Empty<Pawn>())
                        .FirstOrDefault(p => p?.health?.hediffSet?.HasHediff(FallenMiliraDefOf.Milira_FallenAngelMark) == true);
                }
                catch
                {
                    return null;
                }
            }

            /// <summary>
            /// summary: 兜底：从任意玩家地图上找带 FallenAngelMark 的 Pawn（适配 JoinPlayer 列表被清空的情况）。
            /// param: 无
            /// return: 找到则返回 Pawn，否则返回 null
            /// </summary>
            private static Pawn TryFindMarkedAngelOnAnyPlayerMap()
            {
                foreach (var map in Find.Maps)
                {
                    if (map == null) continue;
                    if (!map.IsPlayerHome) continue;

                    var pawns = map.mapPawns?.AllPawnsSpawned;
                    if (pawns == null) continue;

                    var found = pawns.FirstOrDefault(p =>
                        p?.health?.hediffSet?.HasHediff(FallenMiliraDefOf.Milira_FallenAngelMark) == true);
                    if(found == null)
                    {
                        Log.Warning("[BetterMiliraFallenAngel] Could not find Fallen Angel on player map: " + map);
                    }
                    if (found != null) return found;
                }
                return null;
            }

            /// <summary>
            /// summary: 强制 Pawn 成为玩家殖民者（设为玩家派系 + 清除访客/俘虏等 Guest 状态）。
            /// param: pawn 目标 Pawn
            /// return: 无
            /// </summary>
            private static void ForcePawnToBeColonist(Pawn pawn)
            {
                if (pawn == null || pawn.Dead) return;

                if (pawn.Faction != Faction.OfPlayer)
                {
                    pawn.SetFaction(Faction.OfPlayer);
                }
            }
        }

    }
}