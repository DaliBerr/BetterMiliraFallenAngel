using RimWorld;
using Verse;

namespace BetterFallenAngel
{
    internal static class PawnMembership
    {
        internal static void EnsurePlayerMembership(Pawn pawn)
        {
            if (pawn == null || pawn.Dead) return;

            if (pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony)
                RecruitUtility.Recruit(pawn, Faction.OfPlayer);
            else if (pawn.Faction != Faction.OfPlayer)
                pawn.SetFaction(Faction.OfPlayer);

            // SetGuestStatus has side effects even when its value is unchanged:
            // it clears beds and surgery bills. An ordinary colonist needs no
            // guest-status transition when its save is reconciled.
            if (pawn.guest != null && (pawn.guest.HostFaction != null
                || pawn.guest.IsPrisoner || pawn.guest.IsSlave))
                pawn.guest.SetGuestStatus(null);
        }
    }
}
