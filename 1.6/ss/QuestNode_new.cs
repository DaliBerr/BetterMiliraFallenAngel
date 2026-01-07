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

            string stayEndSignal = QuestGen.GenerateNewSignal("FA_Accept_StayEnd");
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

            var finalizeStay = new CoreUtilities.QuestPart_FinalizePermanentStay
            {
                inSignal = leaveAfterSignal,
                outSignalEnd = stayEndSignal,
                pawn = fallenAngel
            };
            quest.AddPart(finalizeStay);

            var endQuestOnStay = new QuestPart_QuestEnd
            {
                inSignal = stayEndSignal,      // 监听“留下”的信号
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
