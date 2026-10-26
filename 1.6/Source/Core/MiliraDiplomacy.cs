using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace BetterFallenAngel
{
    internal static class MiliraDiplomacy
    {
        private static readonly string[] FriendlyFactionNames =
        {
            "Kiiro_Faction", "Milira_PlayerFaction", "Kiiro_PlayerFaction"
        };

        // Both supported game versions keep the calculated goodwill caps here.
        private static readonly FieldInfo CachedGoodwill = typeof(GoodwillSituationManager)
            .GetField("cachedData", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static bool TryGetUnlocked(WorldComponent_BFA component, out bool unlocked)
        {
            unlocked = false;
            FactionDef playerDef = Faction.OfPlayerSilentFail?.def;
            if (component == null || playerDef == null
                || playerDef.defName == "Milira_Imperium_PlayerFaction") return false;

            if (IsAlwaysFriendly(playerDef))
            {
                unlocked = true;
                return true;
            }

            if (component.isUnlocked != ExtendBool.Unset)
            {
                unlocked = component.isUnlocked == ExtendBool.True;
                return true;
            }

            // Older saves did not always persist the unlock flag. Migrate the
            // permission from their story state without replaying its reward.
            if (component.suppressFADialog || component.QuestActive
                || component.storyState == FallenAngelStoryState.Active
                || component.storyState == FallenAngelStoryState.Permanent)
            {
                unlocked = true;
                return true;
            }

            return false;
        }

        internal static void Restore(WorldComponent_BFA component)
        {
            bool unlocked;
            if (!TryGetUnlocked(component, out unlocked)) return;

            FactionDef miliraDef = DefDatabase<FactionDef>.GetNamedSilentFail("Milira_Faction");
            if (miliraDef == null || Find.FactionManager == null) return;

            component.isUnlocked = unlocked ? ExtendBool.True : ExtendBool.False;
            if (miliraDef.permanentEnemyToEveryoneExcept == null)
                miliraDef.permanentEnemyToEveryoneExcept = new List<FactionDef>();

            foreach (string name in FriendlyFactionNames)
            {
                FactionDef friendly = DefDatabase<FactionDef>.GetNamedSilentFail(name);
                if (friendly != null && !miliraDef.permanentEnemyToEveryoneExcept.Contains(friendly))
                    miliraDef.permanentEnemyToEveryoneExcept.Add(friendly);
            }

            // Use Milira's whitelist to lock only the relevant player faction.
            // A global permanentEnemy flag would override its friendly factions.
            miliraDef.permanentEnemy = false;
            FactionDef playerDef = Faction.OfPlayer.def;
            if (unlocked)
            {
                if (!miliraDef.permanentEnemyToEveryoneExcept.Contains(playerDef))
                    miliraDef.permanentEnemyToEveryoneExcept.Add(playerDef);
            }
            else
            {
                miliraDef.permanentEnemyToEveryoneExcept.RemoveAll(def => def == playerDef);
            }

            // Invalidate only calculated caps. Never alter saved base goodwill
            // or relation kinds during loading, even if the faction is hostile.
            IDictionary cache = CachedGoodwill?.GetValue(Find.GoodwillSituationManager) as IDictionary;
            if (cache != null)
            {
                foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
                    if (faction.def == miliraDef) cache.Remove(faction);
            }
        }

        internal static void SetUnlocked(WorldComponent_BFA component, ExtendBool flag)
        {
            if (component == null || flag == ExtendBool.Unset)
            {
                Restore(component);
                return;
            }

            Faction player = Faction.OfPlayerSilentFail;
            if (player == null || player.def.defName == "Milira_Imperium_PlayerFaction") return;

            bool wasUnlocked = component.isUnlocked == ExtendBool.True || IsAlwaysFriendly(player.def);
            component.isUnlocked = flag;
            Restore(component);

            // This is a story transition, not a load callback. Grant neutrality
            // once, using the saved base value rather than a cached -100 cap.
            if (flag != ExtendBool.True || wasUnlocked) return;
            FactionDef miliraDef = DefDatabase<FactionDef>.GetNamedSilentFail("Milira_Faction");
            foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
            {
                if (faction.def != miliraDef) continue;
                int goodwill = faction.BaseGoodwillWith(player);
                if (goodwill < 0)
                    faction.TryAffectGoodwillWith(player, -goodwill, false, false);
            }
        }

        private static bool IsAlwaysFriendly(FactionDef playerDef)
        {
            return FriendlyFactionNames.Contains(playerDef.defName)
                || playerDef.categoryTag == "Kiiro_PlayerFaction";
        }
    }
}
