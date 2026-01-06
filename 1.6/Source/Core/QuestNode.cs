using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Milira;
using UnityEngine;

namespace BetterFallenAngel
{
    // ============= 拒绝线 =============
    // public class QuestNode_Root_FallenAngel_Reject : QuestNode
    // {
    //     protected override void RunInt()
    //     {
    //         var quest = QuestGen.quest;
    //         var slate = QuestGen.slate;

    //         Pawn fallenAngel = slate.Get<Pawn>("fallenAngel");
    //         Pawn subject = slate.Get<Pawn>("subject");
    //         Map map = slate.Get<Map>("map");
    //         float points = slate.Get<float>("points");

    //         // 1) 警告信
    //         var startLetter = new QuestPart_Letter
    //         {
    //             inSignal = QuestGen.GenerateNewSignal("FA_Reject_Start"),
    //             letter = LetterMaker.MakeLetter(
    //                 "Milira_FallenAngel_Reject_Title".Translate(),
    //                 "Milira_FallenAngel_Reject_Desc".Translate(subject.Named("PAWN"), fallenAngel.Named("SOURCE")),
    //                 LetterDefOf.NegativeEvent,
    //                 new LookTargets(new TargetInfo(subject.Position, map)))
    //         };

    //         var startSignal = startLetter.inSignal;
    //         quest.AddPart(startLetter);
    //         var rel = new QuestPart_FactionGoodwillChange
    //         {
    
    //             inSignal = startSignal,
    //             faction = fallenAngel.Faction,
    //             change = -100,
    //             canSendMessage = true,            
    //         };
    //         quest.AddPart(rel);
    //         CoreUtilities.UnlockGoodWill(false);

    //         // 2) 1~2 天后触发一场小规模威胁（例：袭击）
    //         var delay = new QuestPart_Delay
    //         {
    //             inSignalEnable = startSignal,
    //             delayTicks = Rand.RangeInclusive(GenDate.TicksPerDay, GenDate.TicksPerDay * 2)

    //         };
    //         string afterDelaySignal = QuestGen.GenerateNewSignal("FA_Reject_AfterDelay");
    //         delay.outSignalsCompleted.Add(afterDelaySignal);
    //         quest.AddPart(delay);

    //         //part 3 incident
    //         var incident = new QuestPart_Incident
    //         {
    //             inSignal = afterDelaySignal,
    //             incident = IncidentDefOf.RaidEnemy,
    //         };
    //         var incidentParms = new IncidentParms
    //         {
    //             target = map, // 目标地图
    //             points = points * 0.6f, // 威胁点数
    //             forced = true, // 强制触发
    //             faction = fallenAngel.Faction, // 可选：指定派系
    //             spawnCenter = map.Center // 可选：指定生成中心
    //         };
    //         incident.SetIncidentParmsAndRemoveTarget(incidentParms);
    //         quest.AddPart(incident);

    //         // 4) 结束（失败/被拒剧情完成）
    //         var end = new QuestPart_QuestEnd
    //         {
    //             inSignal = afterDelaySignal,
    //             outcome = QuestEndOutcome.Fail
    //         };
    //         quest.AddPart(end);
    //     }

    //     protected override bool TestRunInt(Slate slate) => true;
    // }
    public class QuestNode_Root_FallenAngel_Accept : QuestNode
    {
        public Quest quest;


        protected override void RunInt()
        {
            quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            
            
            WorldComponent_BFA.Instance.RegisterQuest(quest, null);
            int id = quest.id;
            Pawn fallenAngel = slate.Get<Pawn>("fallenAngel");
            slate.Set("fallenAngel", fallenAngel);

            Pawn subject = slate.Get<Pawn>("subject"); 
            Map map = slate.Get<Map>("map");

            if (fallenAngel == null || map == null)
            {
                quest.End(QuestEndOutcome.Fail, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, false, false);
                return;
            }

            // int defaultStay = fallenAngel.health.hediffSet.HasHediff(MiliraDefOf.Abasia)
            //     ? (fallenAngel.health.hediffSet.hediffs.Find(x => x.def == MiliraDefOf.Abasia).ageTicks + GenDate.TicksPerDay * 3)
            //     : GenDate.TicksPerDay * 30; // Default to 30 days if the condition is false
            int leaveAfterTicks = GenDate.TicksPerDay * Mathf.Min(Rand.Range(40,50)+Rand.Range(15,20),65);
            // Log.Warning($"[BFA] Fallen Angel will stay for {leaveAfterTicks / GenDate.TicksPerDay} days ({leaveAfterTicks} ticks)");
            // 关键信号
            string startSignal = QuestGen.GenerateNewSignal("FA_Accept_Start");

            string leaveSignal = QuestGen.GenerateNewSignal("FA_Accept_Leave");

            string leaveAfterSignal = QuestGen.GenerateNewSignal("FA_Accept_LeaveAfter");

            string leaveLetterSignal = QuestGen.GenerateNewSignal("LeaveLetter");
            quest.Letter(
                LetterDefOf.PositiveEvent,
                startSignal,
                null, null, null,
                false, QuestPart.SignalListenMode.OngoingOnly,
                null, false,
                "Milira_FallenAngel_Accept_Desc".Translate(),
                null,
                "Milira_FallenAngel_Title".Translate(),
                null, // lookTargets
                null
            );

            CoreUtilities.UnlockGoodWill(ExtendBool.True);

            var rel = new QuestPart_FactionGoodwillChange
            {
                inSignal = startSignal,
                faction = fallenAngel.Faction,
                change = 15,
                canSendMessage = true,


            };
            quest.AddPart(rel);
            // }
            var join = new QuestPart_JoinPlayer
            {
                inSignal = startSignal,
                joinPlayer = true,
                mapParent = map.Parent
            };
            join.pawns.Add(fallenAngel);
            quest.AddPart(join);

            var delay = new QuestPart_Delay
            {
                inSignalEnable = startSignal,

                delayTicks = leaveAfterTicks,
            };
            delay.outSignalsCompleted.Add(leaveSignal);
            quest.AddPart(delay);



            var leave = new CoreUtilities.QuestPart_Leave_Gated
            {
                inSignal = leaveSignal,

                inSignalEnable = startSignal,
                inSignalDisable = leaveAfterSignal,

                pawns = new List<Pawn> { fallenAngel },
                sendStandardLetter = true,
            };

            var leaveLetter = new QuestPart_Letter
            {
                inSignal = leaveLetterSignal,
                letter = LetterMaker.MakeLetter(
                    "Milira_FallenAngel_Leave_Title".Translate(),
                    "Milira_FallenAngel_Leave_Desc".Translate(fallenAngel.Named("PAWN")),
                    LetterDefOf.PositiveEvent,
                    new LookTargets(new TargetInfo(fallenAngel.Position, map)))
            };
            var leaveSignalActual = leaveLetter.inSignal;
            quest.AddPart(leaveLetter);

            quest.AddPart(leave);

            var endQuestOnStay = new QuestPart_QuestEnd
            {
                inSignal = leaveAfterSignal,      // 监听“留下”的信号
                outcome = QuestEndOutcome.Success // 判定任务成功
            };
            quest.AddPart(endQuestOnStay);

            // —— 立即启动整个链条 —— 
            quest.SignalPass(null, null, startSignal);
        }

        protected override bool TestRunInt(Slate slate)
        {
            return slate.Get<Pawn>("fallenAngel") != null && slate.Get<Map>("map") != null;
        }
    }

    // public class QuestNode_Leave : QuestNode
    // {
    //     protected override void RunInt()
    //     {
    //         Quest quest = QuestGen.quest;
    //         Slate slate = QuestGen.slate;

    //         Pawn fallenAngel = slate.Get<Pawn>("fallenAngel");
    //         Map map = slate.Get<Map>("map");
    //         if (fallenAngel == null || map == null) return;

    //         // 关键信号
    //         string leaveSignal = QuestGen.GenerateNewSignal("FA_Accept_Leave");

    //         // 1) 离开信
    //         var leaveLetter = new QuestPart_Letter
    //         {
    //             inSignal = leaveSignal,
    //             letter = LetterMaker.MakeLetter(
    //                 "Milira_FallenAngel_Leave_Title".Translate(),
    //                 "Milira_FallenAngel_Leave_Desc".Translate(fallenAngel.Named("PAWN")),
    //                 LetterDefOf.PositiveEvent,
    //                 new LookTargets(new TargetInfo(fallenAngel.Position, map)))
    //         };
    //         var leaveSignalActual = leaveLetter.inSignal;
    //         quest.AddPart(leaveLetter);

    //         // 2) 离开
    //         var leave = new QuestPart_Leave
    //         {
    //             inSignal = leaveSignalActual,
    //             pawns = new List<Pawn> { fallenAngel },
    //             // mapParent = map.Parent,
    //             // removeFromWorldPawns = true,
    //             sendStandardLetter = false
    //         };
    //         quest.AddPart(leave);

    //         // 3) 结束（成功/剧情完成）
    //         var end = new QuestPart_QuestEnd
    //         {
    //             inSignal = leaveSignalActual,
    //             outcome = QuestEndOutcome.Success
    //         };
    //         quest.AddPart(end);
    //     }

    //     protected override bool TestRunInt(Slate slate)
    //     {
    //         return slate.Get<Pawn>("fallenAngel") != null && slate.Get<Map>("map") != null;
    //     }
    // }

    public class QuestNode_DropPresetRewards : QuestNode
    {
        public string mapSlateKey = "map";

        private List<ThingDefCountClass> rewards = new List<ThingDefCountClass>()
        {
            new ThingDefCountClass(ThingDefOf.Silver, 400),
            new ThingDefCountClass(ThingDefOf.MedicineIndustrial, 10),
            new ThingDefCountClass(MiliraDefOf.Milira_SunPlateSteel,30),
        };
        protected override void RunInt()
        {
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;

            Map map = slate.Get<Map>(mapSlateKey);

            var dropPods = new QuestPart_DropPods
            {
                inSignal = $"Quest{quest.id}.pickupShipThing.SentSatisfied", // 何时空投
                // outSignalResult = QuestGen.GenerateNewSignal("FA_Accept_Reward_Dropped"), // 空投完成
                mapParent = slate.Get<Map>("map")?.Parent, 
                dropSpot = IntVec3.Invalid,       
                useTradeDropSpot = true,          
                joinPlayer = false,               
                makePrisoners = false,
                dropAllInSamePod = true,          
                allowFogged = false,
                sendStandardLetter = true,       
                customLetterLabel = "RewardsDelivered".Translate(),
                faction = Faction.OfPlayer
            };
            // 组装要空投的物品堆
            dropPods.thingDefs = rewards;

            quest.AddPart(dropPods);
        }

        protected override bool TestRunInt(Slate slate)
        {
            // 生成器预检：有地图且有奖励就通过
            Map map = slate.Get<Map>(mapSlateKey);
            if (map == null) return false;
            return true;
        }
    }


}
