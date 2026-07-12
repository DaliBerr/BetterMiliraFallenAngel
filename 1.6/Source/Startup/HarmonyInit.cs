using Verse;
using HarmonyLib;
using System;

namespace BetterFallenAngel.Startup
{
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            if (!ModsConfig.IsActive("ancot.milirarace"))
            {
                return;
            }

            var harmony = new Harmony("Aquin.BetterMiliraFallenAngel");
            PatchSafely(harmony, typeof(Patches.Patch_FallenAngel_GeneratePawnNewTemp));
            PatchSafely(harmony, typeof(Patches.Patch_Game_LoadGame));
            PatchSafely(harmony, typeof(Patches.Patch_Quest_End));
            PatchSafely(harmony, typeof(Patches.Patch_FallenMiliraReturn_GeneratePawnNewTemp));
            PatchSafely(harmony, typeof(Patches.Patch_FallenMiliraReturn_TestRun));
            PatchSafely(harmony, typeof(Patches.Patch_MiliraChurchIncident_CanFire));
            PatchSafely(harmony, typeof(Patches.Patch_ManagedAngel_Kill));
        }

        private static void PatchSafely(Harmony harmony, Type patchType)
        {
            try
            {
                harmony.CreateClassProcessor(patchType).Patch();
            }
            catch (Exception ex)
            {
                Log.Error($"[BetterMiliraFallenAngel] Failed to apply patch {patchType.FullName}: {ex}");
            }
        }
    }
    

}
