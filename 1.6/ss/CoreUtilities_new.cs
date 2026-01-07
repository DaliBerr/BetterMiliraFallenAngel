using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using RimWorld.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Verse;
using Milira;
using System.Diagnostics;


namespace BetterFallenAngel
{
    public class CoreUtilities
    {
        public static void FallenAngelQuestPopup(string text, Action A = null, Action B = null, string title = null)
        {
            Dialog_MessageBox dialog = new Dialog_MessageBox(
                text,
                "Milira_FallenAngel_Action_A".Translate(),
                A,
                "Milira_FallenAngel_Action_B".Translate(),
                B,
                title,
                false,
                null);
            Find.WindowStack.Add(dialog);
        }

        static List<FactionDef> alwaysFriendlyFactionDef = new List<FactionDef>
        {
            DefDatabase<FactionDef>.GetNamedSilentFail("Kiiro_Faction"),
            DefDatabase<FactionDef>.GetNamedSilentFail("Milira_PlayerFaction"),
            DefDatabase<FactionDef>.GetNamedSilentFail("Kiiro_PlayerFaction"),
        };

        private static void UnlockGoodWill(bool isUnlocked)
        {

            var def = DefDatabase<FactionDef>.GetNamedSilentFail("Milira_Faction");
            if (def == null)
            {
                Log.Warning("[Milira] FactionDef 'Milira_Faction' not found.");
                return;
            }


            var miliraFactions = Find.FactionManager.AllFactionsListForReading
                .Where(f => f.def == def)
                .ToList();

            // 参与关系调整的阵营集合（含隐藏）
            var allFactions = Find.FactionManager.AllFactionsListForReading;
            Log.Warning("[BetterFallenAngel] UnlockGoodWill: isUnlocked=" + isUnlocked);
            // WorldComponent_BFA.Instance.isUnlocked = isUnlocked;

            if(def.permanentEnemyToEveryoneExcept == null)
                def.permanentEnemyToEveryoneExcept = new List<FactionDef>();
                
            def.permanentEnemyToEveryoneExcept.AddRange(alwaysFriendlyFactionDef);

            if (alwaysFriendlyFactionDef.Any(f => f != null && f == Find.FactionManager.OfPlayer.def))
            {
                WorldComponent_BFA.Instance.isUnlocked = ExtendBool.True;
                def.permanentEnemyToEveryoneExcept?.Add(Faction.OfPlayer.def);
                // foreach (var f in miliraFactions)
                // {
                //     f.TryAffectGoodwillWith(Faction.OfPlayer, 100, false, false, null, null);
                // }
                return;
            }


            if (isUnlocked)
            {
                WorldComponent_BFA.Instance.isUnlocked = ExtendBool.True;
                def.permanentEnemy = false;

                if (def.permanentEnemyToEveryoneExcept == null)
                    def.permanentEnemyToEveryoneExcept = new List<FactionDef> { Faction.OfPlayer.def };
                else
                {
                    def.permanentEnemyToEveryoneExcept.Add(Faction.OfPlayer.def); // 仅移除玩家阵营，保留其他白名单
                }

            foreach (var f in miliraFactions)
            {
                // 获取当前与玩家的好感度
                int currentGoodwill = f.GoodwillWith(Faction.OfPlayer);
                
                // 只有当好感度是负数（敌对）时，才重置为 0（中立）
                // 这样做是为了防止玩家已经是盟友(+75)时，读档被重置回 0
                if (currentGoodwill < 0)
                {
                    // 补正差值，让好感度正好回到 0
                    // canSendMessage: false 防止刷屏提示
                    f.TryAffectGoodwillWith(Faction.OfPlayer, -currentGoodwill, canSendMessage: false);
                }
            }
            }
            else
            {
                WorldComponent_BFA.Instance.isUnlocked = ExtendBool.False;
                def.permanentEnemy = true;
                if (def.permanentEnemyToEveryoneExcept == null)
                    def.permanentEnemyToEveryoneExcept = new List<FactionDef>();

                if (def.permanentEnemyToEveryoneExcept.Contains(Faction.OfPlayer.def))
                    def.permanentEnemyToEveryoneExcept.Remove(Faction.OfPlayer.def); // 仅追加，不覆盖原有白名单




                foreach (var f in miliraFactions)
                {
                    foreach (var other in allFactions)
                    {
                        if (other == null || other == f) continue;

                        if (other == Faction.OfPlayer)
                        {
                            // f.SetRelationDirect(other, FactionRelationKind.Hostile, false, null, null);
                            // f.SetRelation(FactionRelation.)
                            f.TryAffectGoodwillWith(other, -100, false, false, null, null);
                        }

                    }
                }
            }

            // Messages.Message("Milira goodwill has been " + (isUnlocked ? "unlocked" : "locked") + ".", MessageTypeDefOf.PositiveEvent, false);
        }
        public static void FixLegacyQuest(Quest quest)
        {
            // 如果任务不存在，或者不是进行中状态，直接忽略
            if (quest == null || quest.State != QuestState.Ongoing) return;

            // 构造这个任务原本应该监听的信号字符串
            // 注意：在 QuestNode 中使用 QuestGen 生成信号时，格式通常是 "Quest" + ID + "." + tag
            // 例如: Quest123.FA_Accept_LeaveAfter
            string expectedSignal = $"Quest{quest.id}.FA_Accept_LeaveAfter";
            string leaveAfterSignal = $"Quest{quest.id}.FA_Accept_LeaveAfter";
            string stayEndSignal = $"Quest{quest.id}.FA_Accept_StayEnd";

            bool hasFinalize = quest.PartsListForReading
                .OfType<QuestPart_FinalizePermanentStay>()
                .Any(p => p.inSignal == leaveAfterSignal);

            bool hasEnd = quest.PartsListForReading
                .OfType<QuestPart_QuestEnd>()
                .Any(p => p.inSignal == stayEndSignal);


            Pawn angel = quest.PartsListForReading
                .OfType<QuestPart_JoinPlayer>()
                .SelectMany(p => p.pawns ?? Enumerable.Empty<Pawn>())
                .FirstOrDefault();

            // 检查任务现有的零件列表 (PartsListForReading)
            // 看看是否已经存在一个监听这个信号的 QuestEnd 零件
            bool hasFix = quest.PartsListForReading.OfType<QuestPart_QuestEnd>()
                            .Any(p => p.inSignal == expectedSignal);
            
            // 如果没有修复过（也就是旧存档的情况），则进行注入

            if (!hasFinalize)
            {
                var finalize = new QuestPart_FinalizePermanentStay
                {
                    inSignal = leaveAfterSignal,
                    outSignalEnd = stayEndSignal,
                    pawn = angel
                };
                quest.AddPart(finalize);
            }

            if (!hasEnd)
            {
                var endPart = new QuestPart_QuestEnd
                {
                    inSignal = stayEndSignal,
                    outcome = QuestEndOutcome.Success
                };
                quest.AddPart(endPart);
            }

            if (!hasFix)
            {
                // 手动创建一个新的结束零件
                var endPart = new QuestPart_QuestEnd();
                endPart.inSignal = expectedSignal;       // 设置监听信号
                endPart.outcome = QuestEndOutcome.Success; // 设置结局为成功
                
                // 将零件加入到任务对象中
                quest.AddPart(endPart);

                Log.Message($"[BetterMiliraFallenAngel] 已修复旧存档任务 (ID: {quest.id})，添加了缺失的结束节点。");
            }
        }
        public static void UnlockGoodWill(ExtendBool flag)
        {
            if (flag == ExtendBool.True)
            {
                UnlockGoodWill(true);
                // WorldComponent_BFA.Instance.isUnlocked = ExtendBool.True;
            }
            else if (flag == ExtendBool.False)
            {
                UnlockGoodWill(false);
                // WorldComponent_BFA.Instance.isUnlocked = ExtendBool.False;
            }
            else if (flag == ExtendBool.Unset)
            {
                if (WorldComponent_BFA.Instance.QuestActive || WorldComponent_BFA.Instance.suppressFADialog)
                {
                    CoreUtilities.UnlockGoodWill(true);
                    Log.Message("[BetterFallenAngel] Goodwill unlocked on load due to active quest or suppressed dialog.");
                }
                else
                {
                    UnlockGoodWill(false);
                }
            }
        }
        public static void TryStartRejectQuest()
        {
            var def = DefDatabase<QuestScriptDef>.GetNamedSilentFail("Milira_FallenAngel_ToChurch");
            if (def == null) return;

            Slate slate = new Slate();


            Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(def, slate);

            if (quest != null)
            {
                QuestUtility.SendLetterQuestAvailable(quest);
            }
        }

        public static int SendQuestSignals(Quest quest, params string[] tags)
        {
            if (quest == null || tags == null) return 0;

            int count = 0;
            foreach (var tag in tags)
            {
                if (string.IsNullOrEmpty(tag)) continue;

                string namespaced = $"Quest{quest.id}.{tag}";
                // Log.Warning("this is the signal:" + namespaced);
                Find.SignalManager.SendSignal(new Signal(namespaced));
                count++;

                Find.SignalManager.SendSignal(new Signal(tag));
                count++;
            }
            return count;
        }
        public class CommunicatorDialog
        {
            public string title = "R_title".Translate();

            public string R_text = "R_text".Translate();
            public string R_OptA = "R_OptA".Translate();
            public string R_Cancel = "R_Cancel".Translate();


            public string R_OptA_S_text = "R_OptA_S_text".Translate();
            public string R_OptA_S_OptA = "R_OptA_S_OptA".Translate();
            public string R_OptA_S_OptB = "R_OptA_S_OptB".Translate();

            public string R_OptA_S_OptA_S_text = "R_OptA_S_OptA_S_text".Translate();
            public string R_OptA_S_OptA_S_OptA = "R_OptA_S_OptA_S_OptA".Translate();

            public string R_OptA_S_OptB_S_text = "R_OptA_S_OptB_S_text".Translate();
            public string R_OptA_S_OptB_S_OptA = "R_OptA_S_OptB_S_OptA".Translate();


            public string R_OptA_S_OptA_S_OptA_S_text = "R_OptA_S_OptA_S_OptA_S_text".Translate();
            public string R_OptA_S_OptA_S_OptA_S_OptA = "R_OptA_S_OptA_S_OptA_S_OptA".Translate();

            private DiaNode BuildSubNode()
            {
                // string body = string.IsNullOrEmpty(R_OptA_S_text) ? "你们对她做什么了？！" : R_OptA_S_text;
                var subNodeA = new DiaNode(R_OptA_S_text);

                // string R_OptA_S_OptA = string.IsNullOrEmpty(R_OptA_S_OptA) ? "她现在正躺在医疗室里，你们直接跟她聊吧" : R_OptA_S_OptA;
                var DiaSubOptA1 = new DiaOption(R_OptA_S_OptA)
                {
                    action = () =>
                    {
                        // TrySendSignal(Props.requestAidSignal, caster, faction, "RequestAid");
                    },
                    // resolveTree = true
                };
                DiaSubOptA1.link = BuildSubOptANode();
                subNodeA.options.Add(DiaSubOptA1);

                // string R_OptA_S_OptB = string.IsNullOrEmpty(Props.R_OptA_S_OptB) ? "如果你们想要她继续活着，就送点东西来吧！不然的话。。。" : Props.R_OptA_S_OptB;
                var DiaSubOptA2 = new DiaOption(R_OptA_S_OptB)
                {
                    action = () =>
                    {
                    },
                    // resolveTree = true
                };
                subNodeA.options.Add(DiaSubOptA2);
                DiaSubOptA2.link = BuildSubOptBNode();


                var back = new DiaOption("Back".Translate());
                back.linkLateBind = () => buildRootNode(); // 返回主菜单
                subNodeA.options.Add(back);

                return subNodeA;
            }

            private DiaNode BuildSubOptANode()
            {
                // string body = string.IsNullOrEmpty(Props.R_OptA_S_OptA_S_text) ? "你怎么回事？你还好吗？之前发生了什么？" : Props.R_OptA_S_OptA_S_text;
                var subSubNodeA = new DiaNode(R_OptA_S_OptA_S_text);

                // string R_OptA_S_OptA_S_OptA = string.IsNullOrEmpty(Props.R_OptA_S_OptA_S_OptA) ? "旅行中出了一些事故，差点死掉了，多亏有这帮地面人的帮助" : Props.R_OptA_S_OptA_S_OptA;
                var DiaSubSubOptA1 = new DiaOption(R_OptA_S_OptA_S_OptA)
                {
                    action = () =>
                    {
                        // TrySendSignal(Props.negotiateSignal, caster, faction, "Negotiate");
                    },
                    // resolveTree = true
                };
                DiaSubSubOptA1.link = BuildSubSubOptANode();
                subSubNodeA.options.Add(DiaSubSubOptA1);


                var back = new DiaOption("Back".Translate());
                back.linkLateBind = () => BuildSubNode(); // 返回上一级菜单
                subSubNodeA.options.Add(back);

                return subSubNodeA;
            }

            private DiaNode BuildSubOptBNode()
            {

                // string body = string.IsNullOrEmpty(Props.R_OptA_S_OptB_S_text) ? "我就知道，你们这些地面人都是一群贪婪的野兽！先把我们的人送回来，自然会给你们答谢" : Props.R_OptA_S_OptB_S_text;
                var subSubNodeB = new DiaNode(R_OptA_S_OptB_S_text);

                // string R_OptA_S_OptB_S_OptA = string.IsNullOrEmpty(Props.R_OptA_S_OptB_S_OptA) ? "那就这样说定了" : Props.R_OptA_S_OptB_S_OptA;
                var DiaSubSubOptB1 = new DiaOption(R_OptA_S_OptB_S_OptA)
                {
                    action = () =>
                    {
                        WorldComponent_BFA.Instance.suppressFADialog = true;
                        CoreUtilities.SendQuestSignals(WorldComponent_BFA.Instance.Quest, "QuestShuttle");
                        CoreUtilities.SendQuestSignals(WorldComponent_BFA.Instance.Quest, "FA_Accept_LeaveAfter");

                        // SendQuestSignalBare("QuestShuttle");
                        // CoreUtilities.UnlockGoodWill(false);
                        // TrySendSignal(Props.requestAidSignal, caster, faction, "RequestAid");
                    },
                    resolveTree = true
                };
                subSubNodeB.options.Add(DiaSubSubOptB1);


                var back = new DiaOption("Back".Translate());
                back.linkLateBind = () => BuildSubNode(); // 返回上一级菜单
                subSubNodeB.options.Add(back);

                return subSubNodeB;
            }

            private DiaNode BuildSubSubOptANode()
            {
                // string body = string.IsNullOrEmpty(Props.R_OptA_S_OptA_S_OptA_S_text) ? "需要我们来接你吗？" : Props.R_OptA_S_OptA_S_OptA_S_text;
                var subSubSubNodeA = new DiaNode("R_OptA_S_OptA_S_OptA_S_text".Translate());

                // string R_OptA_S_OptA_S_OptA_S_OptA = string.IsNullOrEmpty(Props.R_OptA_S_OptA_S_OptA_S_OptA) ? "我应该不回去了，他们非常友善，和传闻中的地面人不同，我决定暂时和他们待在一起。" : Props.R_OptA_S_OptA_S_OptA_S_OptA;
                var DiaSubSubSubOptA1 = new DiaOption("R_OptA_S_OptA_S_OptA_S_OptA".Translate())
                {
                    action = () =>
                    {
                        CoreUtilities.SendQuestSignals(WorldComponent_BFA.Instance.Quest, "FA_Accept_LeaveAfter");
                        WorldComponent_BFA.Instance.suppressFADialog = true;
                    },
                    resolveTree = true
                };
                subSubSubNodeA.options.Add(DiaSubSubSubOptA1);

                var back = new DiaOption("Back".Translate());
                back.linkLateBind = () => BuildSubOptANode(); // 返回上一级菜单
                subSubSubNodeA.options.Add(back);

                return subSubSubNodeA;
            }

            public DiaNode buildRootNode()
            {
                // string body = string.IsNullOrEmpty(R_text) ? "通讯接入……" : R_text;
                var root = new DiaNode("R_text".Translate());

                // string R_OptA = string.IsNullOrEmpty(Props.R_OptA) ? "你们对她做什么了？！" : Props.R_OptA;
                var DiaRootOptA = new DiaOption("R_OptA".Translate())
                {
                    action = () =>
                    {
                    },
                    // resolveTree = true
                };
                DiaRootOptA.link = BuildSubNode();
                root.options.Add(DiaRootOptA);

                var DiaRootCancel = new DiaOption(R_Cancel)
                {
                    action = () =>
                    {
                    },
                    resolveTree = true
                };
                root.options.Add(DiaRootCancel);

                return root;
            }

            // public static void createAbilityDialog(Faction targetFaction, bool hasMarkedPawn)
            // {
            //     // string text = "abcd".Translate();
            //     if (targetFaction == null) return;
            //     // string title = 
            //     if (hasMarkedPawn)
            //     {
            //         // var root = new CommunicatorDialog().buildRootNode();
            //     }
            //     else
            //     {
            //         return;
            //     }

            // }
        }




        /// <summary>
        /// 扩展版：支持 Enable / Disable / Toggle 的离开部件。
        /// 继承自原版 QuestPart_Leave，默认启用（enabledNow = true）。
        /// - 收到 inSignalEnable  => enabledNow = true
        /// - 收到 inSignalDisable => enabledNow = false
        /// - 收到 inSignalToggle  => enabledNow 取反
        /// - 收到 inSignal 且 enabledNow == true 时才执行离开
        /// 说明：Cleanup() 仍沿用父类逻辑（如果 leaveOnCleanup = true 则无条件离开）。
        /// </summary>
        public class QuestPart_Leave_Gated : QuestPart_Leave
        {
            // 额外控制信号
            public string inSignalEnable;
            public string inSignalDisable;
            public string inSignalToggle;

            // 当前是否启用响应（默认启用）
            public bool enabledNow = true;

            /// <summary>
            /// 自行处理所有信号，避免父类在禁用时误触发离开。
            /// </summary>
            public override void Notify_QuestSignalReceived(Signal signal)
            {
                // 不调用 base.Notify_QuestSignalReceived(signal)，
                // 因为父类会在 tag == inSignal 时直接执行离开，无法加门闩。

                // 处理“移除特定 pawn”
                if (signal.tag == inSignalRemovePawn
                    && signal.args.TryGetArg("SUBJECT", out Pawn toRemove)
                    && pawns.Contains(toRemove))
                {
                    pawns.Remove(toRemove);
                    // 不 return；允许同 tick 继续处理开关信号（根据需要也可以 return）
                }

                // 处理开关
                if (!string.IsNullOrEmpty(inSignalToggle) && signal.tag == inSignalToggle)
                {
                    enabledNow = !enabledNow;
                    return;
                }
                if (!string.IsNullOrEmpty(inSignalEnable) && signal.tag == inSignalEnable)
                {
                    enabledNow = true;
                    return;
                }
                if (!string.IsNullOrEmpty(inSignalDisable) && signal.tag == inSignalDisable)
                {
                    enabledNow = false;
                    return;
                }

                // 处理主触发：仅当启用时才响应
                if (signal.tag == inSignal && enabledNow)
                {
                    LeaveQuestPartUtility.MakePawnsLeave(pawns, sendStandardLetter, quest, wakeUp);
                    foreach (var p in pawns)
                    {
                        if (p != null && p.Spawned)
                        {
                            // p.ExitMap(false, CellRect.Empty);
                            p.SetFaction(Find.FactionManager.FirstFactionOfDef(MiliraDefOf.Milira_Faction));

                        }
                    }
                    CoreUtilities.SendQuestSignals(quest, "LeaveLetter");
                }
            }
        
            /// <summary>
            /// 存档/读档
            /// </summary>
            public override void ExposeData()
            {
                base.ExposeData();
                Scribe_Values.Look(ref inSignalEnable, "inSignalEnable");
                Scribe_Values.Look(ref inSignalDisable, "inSignalDisable");
                Scribe_Values.Look(ref inSignalToggle, "inSignalToggle");
                Scribe_Values.Look(ref enabledNow, "enabledNow", defaultValue: true);
            }

            /// <summary>
            /// Debug 数据：顺手给三个控制信号也生成个占位
            /// </summary>
            public override void AssignDebugData()
            {
                base.AssignDebugData();
                inSignalEnable = "DebugSignal_Enable_" + Rand.Int;
                inSignalDisable = "DebugSignal_Disable_" + Rand.Int;
                inSignalToggle = "DebugSignal_Toggle_" + Rand.Int;
            }
        }
        public class QuestPart_FinalizePermanentStay : QuestPart
        {
            public string inSignal;
            public string outSignalEnd;
            public Pawn pawn;

            /// <summary>
            /// summary: 收到信号后，将目标 Pawn 永久转为玩家阵营，并解除 JoinPlayer 的托管，然后触发 outSignalEnd。
            /// param: signal 收到的任务信号
            /// return: 无
            /// </summary>
            public override void Notify_QuestSignalReceived(Signal signal)
            {
                if (signal.tag != inSignal) 
                {
                    Log.Warning("[BetterMiliraFallenAngel] QuestPart_FinalizePermanentStay received unexpected signal: " + signal.tag);
                    return;
                }

                TryDetachFromJoinPlayerParts();
                TryMakePawnPermanentColonist();

                if (!string.IsNullOrEmpty(outSignalEnd))
                {
                    // 只发 namespaced 更安全；outSignalEnd 本身就是 QuestGen.GenerateNewSignal 生成的完整 tag
                    Find.SignalManager.SendSignal(new Signal(outSignalEnd));
                }
            }

            /// <summary>
            /// summary: 将 Pawn 从所有 QuestPart_JoinPlayer 的 pawns 列表中移除，避免 Quest 结束清理时回滚派系。
            /// param: 无
            /// return: 无
            /// </summary>
            private void TryDetachFromJoinPlayerParts()
            {
                if (quest == null || pawn == null) return;

                foreach (var jp in quest.PartsListForReading.OfType<QuestPart_JoinPlayer>())
                {
                    if(jp == null || jp.pawns == null)
                    {
                        Log.Warning("[BetterMiliraFallenAngel] QuestPart_FinalizePermanentStay found null QuestPart_JoinPlayer or null pawns list.");
                        continue;
                    }
                    jp?.pawns?.Remove(pawn);
                }
            }

            /// <summary>
            /// summary: 强制把 Pawn 设为玩家阵营，并尽量清掉访客状态（防止显示为中立/访客）。
            /// param: 无
            /// return: 无
            /// </summary>
            private void TryMakePawnPermanentColonist()
            {
                try
                {
                    pawn.guest?.SetGuestStatus(null, GuestStatus.Guest);
                }
                catch
                {
                    // 某些版本签名差异时忽略，不影响主要逻辑
                }
                if (pawn == null)
                {
                    Log.Warning("[BetterMiliraFallenAngel] QuestPart_FinalizePermanentStay found null pawn.");
                    return;
                } 

                if (pawn.Faction != Faction.OfPlayer)
                {
                    pawn.SetFaction(Faction.OfPlayer);
                }

                // 保险：如果是 Guest/Prisoner 等，尽量清回 None
                // try
                // {
                //     pawn.guest?.SetGuestStatus(null, GuestStatus.Guest);
                // }
                // catch
                // {
                //     // 某些版本签名差异时忽略，不影响主要逻辑
                // }
            }
            public override void ExposeData()
            {
                base.ExposeData();
                Scribe_Values.Look(ref inSignal, "inSignal");
                Scribe_Values.Look(ref outSignalEnd, "outSignalEnd");
                Scribe_References.Look(ref pawn, "pawn");
            }
        }

    }

}


