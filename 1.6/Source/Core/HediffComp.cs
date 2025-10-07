using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace BetterFallenAngel
{
    public enum FactionFilter { Any, Player, MiliraFaction }


    public class Hediff_Hidden : HediffWithComps
    {
        public override bool Visible => false;
        public override string LabelBase => string.Empty;
        public override string LabelInBrackets => null;
        public override string TipStringExtra => string.Empty;
    }
    public class HediffCompProperties_FriendlyProximityTrigger : HediffCompProperties
    {
        public float radius = 8f;
        public int tickInterval = 60;
        public bool oneShot = true;
        public int cooldownTicks = 0;
        public FactionFilter factionFilter = FactionFilter.Player;

        // 触发效果（可选项）
        public HediffDef applyHediffDef;
        public string sendSignalTagA = "Accept";         // 用于 Quest/其他监听：Signal 参数里会带 SUBJECT=目标pawn, SOURCE=堕天使
        public string sendSignalTagB = "Reject";        // 用于 Quest/其他监听：Signal 参数里会带 SUBJECT=目标pawn, SOURCE=堕天使

        public string AcceptLabelKey;
        public string AcceptTextKey;
        public string RejectLabelKey;
        public string RejectTextKey;

        public HediffCompProperties_FriendlyProximityTrigger()
        {
            this.compClass = typeof(HediffComp_FriendlyProximityTrigger);
        }
    }
    public class HediffComp_FriendlyProximityTrigger : HediffComp
    {
        private HediffCompProperties_FriendlyProximityTrigger Props => (HediffCompProperties_FriendlyProximityTrigger)props;

        private int lastTick;
        private int cooldownUntil;
        private HashSet<int> affectedPawnIds = new HashSet<int>();

        public override void CompExposeData()
        {
            Scribe_Values.Look(ref lastTick, "lastTick", 0);
            Scribe_Values.Look(ref cooldownUntil, "cooldownUntil", 0);
            Scribe_Collections.Look(ref affectedPawnIds, "affectedPawnIds", LookMode.Value);
            if (affectedPawnIds == null)
            {
                affectedPawnIds = new HashSet<int>();
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            var pawn = parent.pawn;
            if (pawn == null || !pawn.Spawned || pawn.Map == null) return;

            int cur = Find.TickManager.TicksGame;
            if (cur - lastTick < Props.tickInterval) return;
            lastTick = cur;

            if (cur < cooldownUntil) return;

            // 只在 FallenAngel 自身活着且非下地图时生效
            if (pawn.Dead || pawn.Downed && pawn.MapHeld == null) return;

            // 获取候选目标
            var candidates = GetCandidates(pawn);
            if (candidates == null || candidates.Count == 0) return;

            float r2 = Props.radius * Props.radius;
            foreach (var p in candidates)
            {
                if (p == null || p == pawn || p.Dead || !p.Spawned) continue;
                if ((p.Position - pawn.Position).LengthHorizontalSquared > r2) continue;

                if (!affectedPawnIds.Contains(p.thingIDNumber))
                {
                    // 命中！
                    ApplyEffects(pawn, p);

                    affectedPawnIds.Add(p.thingIDNumber);

                    if (Props.oneShot)
                    {
                        pawn.health.RemoveHediff(parent);
                        return;
                    }
                    else if (Props.cooldownTicks > 0)
                    {
                        cooldownUntil = cur + Props.cooldownTicks;
                        break; // 这次扫描只触发一次
                    }
                }
            }
        }

        private List<Pawn> GetCandidates(Pawn self)
        {
            var mapPawns = self.Map.mapPawns;
            return mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
        }

        private void ApplyEffects(Pawn sourceFallenAngel, Pawn target)
        {
            // 1) 施加 hediff（示例）
            if (Props.applyHediffDef != null)
            {
                var h = HediffMaker.MakeHediff(Props.applyHediffDef, target);
                target.health.AddHediff(h);
            }
            
            CoreUtilities.FallenAngelQuestPopup("Milira_FallenAngel_Action_Desc".Translate(target.LabelShortCap),
                A: () =>
                {
                    if (!string.IsNullOrEmpty(Props.sendSignalTagA))
                    {
                        var sig = new Signal(Props.sendSignalTagA);
                        sig.args.Add("SUBJECT", target);
                        sig.args.Add("SOURCE", sourceFallenAngel);
                        Find.SignalManager.SendSignal(sig);
                    }
                    StartFallenAngelQuest("BFA_FallenAngel_Accept", sourceFallenAngel, target);

                    var label = !string.IsNullOrEmpty(Props.AcceptLabelKey) ? Props.AcceptLabelKey.Translate() : "Milira_FallenAngel_Action_A".Translate();
                    var text = !string.IsNullOrEmpty(Props.AcceptTextKey) ? Props.AcceptTextKey.Translate(target.Named("PAWN"), sourceFallenAngel.Named("SOURCE")) : "Milira_FallenAngel_Action_A_Desc".Translate(target.LabelShortCap);
                    Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent, new LookTargets(target, sourceFallenAngel));


                },
                B: () =>
                {
                    if (!string.IsNullOrEmpty(Props.sendSignalTagB))
                    {
                        var sig = new Signal(Props.sendSignalTagB);
                        sig.args.Add("SUBJECT", target);
                        sig.args.Add("SOURCE", sourceFallenAngel);
                        Find.SignalManager.SendSignal(sig);
                    }

                    // StartFallenAngelQuest("BFA_FallenAngel_Reject", sourceFallenAngel, target);
                    CoreUtilities.TryStartRejectQuest();
                    var label = !string.IsNullOrEmpty(Props.RejectLabelKey) ? Props.RejectLabelKey.Translate() : "Milira_FallenAngel_Action_B".Translate();
                    var text = !string.IsNullOrEmpty(Props.RejectTextKey) ? Props.RejectTextKey.Translate(target.Named("PAWN"), sourceFallenAngel.Named("SOURCE")) : "Milira_FallenAngel_Action_B_Desc".Translate(target.LabelShortCap);
                    Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NegativeEvent, new LookTargets(target, sourceFallenAngel));
                },
                title: "BetterFallenAngel".Translate()
            );

        }


        private static void StartFallenAngelQuest(string questDefName, Pawn fallenAngel, Pawn subject)
        {
            var def = DefDatabase<QuestScriptDef>.GetNamedSilentFail(questDefName);
            if (def == null) return;

            var map = fallenAngel?.Map ?? Find.CurrentMap;
            var slate = new Slate();
            slate.Set("fallenAngel", fallenAngel);
            slate.Set("subject", subject);
            slate.Set("map", map);
            slate.Set("points", StorytellerUtility.DefaultThreatPointsNow(map)); // 给战斗事件用
            slate.Set("playerFaction", Faction.OfPlayer);

            var quest = QuestUtility.GenerateQuestAndMakeAvailable(def, slate);
            if (quest != null)
            {
                QuestUtility.SendLetterQuestAvailable(quest);
            }
            
        }

    }
}