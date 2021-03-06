using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {

    [StaticConstructorOnStartup]
    internal class HarmonyPatches {
        static HarmonyPatches() {
            try {
                Type pageConfigureStartingPawnsType = ReflectionUtil.TypeByName("RimWorld.Page_ConfigureStartingPawns");
                Type gameType = ReflectionUtil.TypeByName("Verse.Game");
                HarmonyInstance harmony = HarmonyInstance.Create("EdB.PrepareCarefully");
                if (pageConfigureStartingPawnsType != null) {
                    if (harmony.Patch(pageConfigureStartingPawnsType.GetMethod("PreOpen"),
                        new HarmonyMethod(null),
                        new HarmonyMethod(typeof(HarmonyPatches).GetMethod("PreOpenPostfix"))) == null) {
                        Log.Warning("Prepare Carefully did not successfully patch the Page_ConfigureStartingPawns.PreOpen method. The Prepare Carefully button may not appear properly.");
                    }
                    if (harmony.Patch(pageConfigureStartingPawnsType.GetMethod("DoWindowContents"),
                        new HarmonyMethod(null),
                        new HarmonyMethod(typeof(HarmonyPatches).GetMethod("DoWindowContentsPostfix"))) == null) {
                        Log.Warning("Prepare Carefully did not successfully patch the Page_ConfigureStartingPawns.DoWindowContentsPostfix method. The Prepare Carefully button may not appear properly.");
                    }
                }
                else {
                    Log.Warning("Could not add the Prepare Carefully button to the configure pawns page.  Could not find the required type.");
                }
                if (gameType != null) {
                    if (harmony.Patch(gameType.GetMethod("InitNewGame"),
                        new HarmonyMethod(null),
                        new HarmonyMethod(typeof(HarmonyPatches).GetMethod("InitNewGamePostfix"))) == null) {
                        Log.Warning("Prepare Carefully did not successfully patch the Game.InitNewGame method. Prepare Carefully may not properly spawn pawns and items onto the map.");
                    }
                }
                else {
                    Log.Warning("Could not modify the game initialization routine as needed for Prepare Carefully.  Could not find the required type.");
                }
            }
            catch (Exception e) {
                Log.Warning("Failed to patch the game code as needed for Prepare Carefully.  There was an unexpected exception. \n" + e.StackTrace);
            }
        }

        // Clear the original scenario when opening the Configure Starting Pawns page.  This makes
        // sure that the workaround static variable gets cleared if you quit to the main menu from
        // gameplay and then start a new game.
        public static void PreOpenPostfix() {
            PrepareCarefully.ClearOriginalScenario();
        }

        // Removes the customized scenario (with PrepareCarefully-specific scenario parts) and replaces
        // it with a vanilla-friendly version that was prepared earlier.  This is a workaround to avoid
        // creating a dependency between a saved game and the mod.  See Controller.PrepareGame() for 
        // more details.
        public static void InitNewGamePostfix() {
            if (PrepareCarefully.OriginalScenario != null) {
                Current.Game.Scenario = PrepareCarefully.OriginalScenario;
                PrepareCarefully.ClearOriginalScenario();
            }
        }

        // Draw the "Prepare Carefully" button at the bottom of the Configure Starting Pawns page.
        public static void DoWindowContentsPostfix(Rect rect, Page_ConfigureStartingPawns __instance) {
            Vector2 BottomButSize = new Vector2(150f, 38f);
            float num = rect.height + 45f;
            Rect rect4 = new Rect(rect.x + rect.width / 2f - BottomButSize.x / 2f, num, BottomButSize.x, BottomButSize.y);
            if (Widgets.ButtonText(rect4, "EdB.PC.Page.Button.PrepareCarefully".Translate(), true, false, true)) {
                try {
                    PrepareCarefully.Instance.Initialize();
                    PrepareCarefully.Instance.OriginalPage = __instance;
                    Page_PrepareCarefully page = new Page_PrepareCarefully();
                    PrepareCarefully.Instance.State.Page = page;
                    Find.WindowStack.Add(page);
                }
                catch (Exception e) {
                    Find.WindowStack.Add(new DialogInitializationError());
                    SoundDefOf.ClickReject.PlayOneShot(null);
                    throw e;
                }
            }
        }
    }
}
