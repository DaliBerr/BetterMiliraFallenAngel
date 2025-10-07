using Verse;
using HarmonyLib;
using RimWorld.Utility;
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
            harmony.PatchAll();
        }

    }
    

}
