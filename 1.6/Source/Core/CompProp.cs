using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using System.Linq;

namespace BetterFallenAngel
{
    public class CompProperties_GiveAbilityOnEquip : CompProperties
    {
        public AbilityDef abilityDef;

        public CompProperties_GiveAbilityOnEquip()
        {
            this.compClass = typeof(CompGiveAbilityOnEquip);
        }
    }
    public class CompGiveAbilityOnEquip : ThingComp
    {
        public CompProperties_GiveAbilityOnEquip Props => (CompProperties_GiveAbilityOnEquip)props;

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            if (pawn?.abilities == null || Props.abilityDef == null) return;
            
            if (!pawn.abilities.abilities.Any(a => a.def == Props.abilityDef))
            {
                pawn.abilities.GainAbility(Props.abilityDef);
            }
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            if (pawn?.abilities == null || Props.abilityDef == null) return;
            if (pawn.abilities.abilities.Any(a => a.def == Props.abilityDef))
            {
                pawn.abilities.RemoveAbility(Props.abilityDef);
            }
        }
    }

    public class CompProperties_AbilityEffect_Communicator : CompProperties_AbilityEffect
    {

        public string factionDefName = "Milira_Faction"; // 目标派系DefName





        // public string requestAidSignal;
        // public string negotiateSignal;

        public CompProperties_AbilityEffect_Communicator()
        {
            compClass = typeof(CompAbilityEffect_Communicator);
        }
    }

    public class CompAbilityEffect_Communicator : CompAbilityEffect
    {
        public new CompProperties_AbilityEffect_Communicator Props
            => (CompProperties_AbilityEffect_Communicator)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            var caster = parent.pawn;
            if (caster?.Map == null) return;


            Faction targetFaction = null;
            

            if (!string.IsNullOrEmpty(Props.factionDefName))
            {
                var def = DefDatabase<FactionDef>.GetNamedSilentFail(Props.factionDefName);
                if (def != null) targetFaction = Find.FactionManager.FirstFactionOfDef(def);
            }

            // 没找到派系就给出提示
            if (targetFaction == null)
            {
                Messages.Message("[Milira通讯器] 未找到指定派系：" + Props.factionDefName,
                    new LookTargets(caster), MessageTypeDefOf.RejectInput);
                return;
            }

            // string title = string.IsNullOrEmpty(Props.title) ? "Milira通讯器" : Props.title;
            // string body = string.IsNullOrEmpty(Props.R_text) ? "通讯接入……" : Props.R_text;

            var root = new DiaNode("Conneting");


            var map = caster?.Map ?? Find.AnyPlayerHomeMap;
            var playerPawns = map?.mapPawns?.FreeColonistsAndPrisonersSpawned ?? new List<Pawn>();
            bool hasMarked = playerPawns.Any(p => p.health?.hediffSet?.HasHediff(FallenMiliraDefOf.Milira_FallenAngelMark) == true);

            if (hasMarked && !WorldComponent_BFA.Instance.suppressFADialog)
            {
                
                root = new CoreUtilities.CommunicatorDialog().buildRootNode();


                // root = buildRootNode();
                // root = CoreUtilities.CommunicatorDialog.createAbilityDialog(targetFaction, hasMarked);
            }
            else
            {
                root = new DiaNode("CommunicatorDisconnected".Translate());
                var DiaRootCancel = new DiaOption("Close".Translate())
                {
                    action = () =>
                    {
                    },
                    resolveTree = true
                };
                root.options.Add(DiaRootCancel);
                // root = new DiaNode("通讯器无法连接到任何频道");
                // var DiaRootCancel = new DiaOption("关闭")
                // {
                //     action = () =>
                //     {
                //     },
                //     resolveTree = true
                // };
                // root.options.Add(DiaRootCancel);
            }
            Find.WindowStack.Add(new Dialog_NodeTree(root, true, false, "Milira_Communicator".Translate()));
            // Find.WindowStack.Add(new Dialog_NodeTreeWithFactionInfo(root, targetFaction, true, false, title));
            // SoundDefOf.CommsConsole_Open.PlayOneShotOnCamera();

        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            var targetFaction = Find.FactionManager.FirstFactionOfDef(DefDatabase<FactionDef>.GetNamedSilentFail("Milira_Faction"));
            return parent?.pawn != null && parent.pawn.Spawned && targetFaction != null && !parent.pawn.Faction.HostileTo(targetFaction);
        }

        // private DiaNode BuildSubNode()
        // {
        //     string body = string.IsNullOrEmpty(Props.R_OptA_S_text) ? "你们对她做什么了？！" : Props.R_OptA_S_text;
        //     var subNodeA = new DiaNode(body);

        //     string R_OptA_S_OptA = string.IsNullOrEmpty(Props.R_OptA_S_OptA) ? "她现在正躺在医疗室里，你们直接跟她聊吧" : Props.R_OptA_S_OptA;
        //     var DiaSubOptA1 = new DiaOption(R_OptA_S_OptA)
        //     {
        //         action = () =>
        //         {
        //             // TrySendSignal(Props.requestAidSignal, caster, faction, "RequestAid");
        //         },
        //         // resolveTree = true
        //     };
        //     DiaSubOptA1.link = BuildSubOptANode();
        //     subNodeA.options.Add(DiaSubOptA1);

        //     string R_OptA_S_OptB = string.IsNullOrEmpty(Props.R_OptA_S_OptB) ? "如果你们想要她继续活着，就送点东西来吧！不然的话。。。" : Props.R_OptA_S_OptB;
        //     var DiaSubOptA2 = new DiaOption(R_OptA_S_OptB)
        //     {
        //         action = () =>
        //         {
        //         },
        //         // resolveTree = true
        //     };
        //     subNodeA.options.Add(DiaSubOptA2);
        //     DiaSubOptA2.link = BuildSubOptBNode();


        //     var back = new DiaOption("Back".Translate());
        //     back.linkLateBind = () => buildRootNode(); // 返回主菜单
        //     subNodeA.options.Add(back);

        //     return subNodeA;
        // }

        // private DiaNode BuildSubOptANode()
        // {
        //     string body = string.IsNullOrEmpty(Props.R_OptA_S_OptA_S_text) ? "你怎么回事？你还好吗？之前发生了什么？" : Props.R_OptA_S_OptA_S_text;
        //     var subSubNodeA = new DiaNode(body);

        //     string R_OptA_S_OptA_S_OptA = string.IsNullOrEmpty(Props.R_OptA_S_OptA_S_OptA) ? "旅行中出了一些事故，差点死掉了，多亏有这帮地面人的帮助" : Props.R_OptA_S_OptA_S_OptA;
        //     var DiaSubSubOptA1 = new DiaOption(R_OptA_S_OptA_S_OptA)
        //     {
        //         action = () =>
        //         {
        //             // TrySendSignal(Props.negotiateSignal, caster, faction, "Negotiate");
        //         },
        //         // resolveTree = true
        //     };
        //     DiaSubSubOptA1.link = BuildSubSubOptANode();
        //     subSubNodeA.options.Add(DiaSubSubOptA1);


        //     var back = new DiaOption("Back".Translate());
        //     back.linkLateBind = () => BuildSubNode(); // 返回上一级菜单
        //     subSubNodeA.options.Add(back);

        //     return subSubNodeA;
        // }

        // private DiaNode BuildSubOptBNode()
        // {

        //     string body = string.IsNullOrEmpty(Props.R_OptA_S_OptB_S_text) ? "我就知道，你们这些地面人都是一群贪婪的野兽！先把我们的人送回来，自然会给你们答谢" : Props.R_OptA_S_OptB_S_text;
        //     var subSubNodeB = new DiaNode(body);

        //     string R_OptA_S_OptB_S_OptA = string.IsNullOrEmpty(Props.R_OptA_S_OptB_S_OptA) ? "那就这样说定了" : Props.R_OptA_S_OptB_S_OptA;
        //     var DiaSubSubOptB1 = new DiaOption(R_OptA_S_OptB_S_OptA)
        //     {
        //         action = () =>
        //         {
        //             CoreUtilities.SendQuestSignals(WorldComponent_BFA.Instance.Quest, "QuestShuttle");
        //             // SendQuestSignalBare("QuestShuttle");
        //             // CoreUtilities.UnlockGoodWill(false);
        //             // TrySendSignal(Props.requestAidSignal, caster, faction, "RequestAid");
        //         },
        //         resolveTree = true
        //     };
        //     subSubNodeB.options.Add(DiaSubSubOptB1);


        //     var back = new DiaOption("Back".Translate());
        //     back.linkLateBind = () => BuildSubNode(); // 返回上一级菜单
        //     subSubNodeB.options.Add(back);

        //     return subSubNodeB;
        // }

        // private DiaNode BuildSubSubOptANode()
        // {
        //     string body = string.IsNullOrEmpty(Props.R_OptA_S_OptA_S_OptA_S_text) ? "需要我们来接你吗？" : Props.R_OptA_S_OptA_S_OptA_S_text;
        //     var subSubSubNodeA = new DiaNode(body);

        //     string R_OptA_S_OptA_S_OptA_S_OptA = string.IsNullOrEmpty(Props.R_OptA_S_OptA_S_OptA_S_OptA) ? "我应该不回去了，他们非常友善，和传闻中的地面人不同，我决定暂时和他们待在一起。" : Props.R_OptA_S_OptA_S_OptA_S_OptA;
        //     var DiaSubSubSubOptA1 = new DiaOption(R_OptA_S_OptA_S_OptA_S_OptA)
        //     {
        //         action = () =>
        //         {
        //             // TrySendSignal(Props.negotiateSignal, caster, faction, "Negotiate");
        //         },
        //         resolveTree = true
        //     };
        //     subSubSubNodeA.options.Add(DiaSubSubSubOptA1);

        //     var back = new DiaOption("Back".Translate());
        //     back.linkLateBind = () => BuildSubOptANode(); // 返回上一级菜单
        //     subSubSubNodeA.options.Add(back);

        //     return subSubSubNodeA;
        // }
        
    }

    

}



