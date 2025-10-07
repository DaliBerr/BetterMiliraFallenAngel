
// using Milira;
// using RimWorld;
// using RimWorld.Planet;
// using RimWorld.QuestGen;
// using Verse;

// namespace BetterFallenAngel
// {
//     public static class MiliraDebugHelper
//     {
//         public static void TriggerFallenAngelNow(Map map)
//         {
//             if (map == null)
//             {
//                 Messages.Message("需要在有地图的存档中触发喵~", MessageTypeDefOf.RejectInput, false);
//                 return;
//             }

//             // 确保通过 TestRunInt 的关卡
//             var comp = Current.Game.GetComponent<MiliraGameComponent_OverallControl>();
//             if (comp != null) comp.canSendFallenMilira = true;

//             // 把当前地图塞进 Slate，让 Root 能定位到正确的 Map
//             Slate slate = new Slate();
//             slate.Set("map", map);

//             // 注意：这里的 Milira_FallenAngelQuest 要与你的 XML 中的 QuestScriptDef 对上
//             Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(MiliraDefOf.Milira_FallenAngel, slate);

//             if (quest != null)
//             {
//                 // 发一封“任务可用”的信（Root 里自己也会发事件信）
//                 QuestUtility.SendLetterQuestAvailable(quest);
//                 Messages.Message("已触发【堕天使坠落】任务喵！", MessageTypeDefOf.PositiveEvent, false);
//             }
//             else
//             {
//                 Messages.Message("触发失败：Quest 生成返回 null，可能是 TestRun 未通过或 Def 未对上。", MessageTypeDefOf.RejectInput, false);
//             }
//         }
//     }
// }
