using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using HexBattleMage.Core.Managers;

namespace HexBattleMage.Patches
{
    [HarmonyPatch]
    internal static class ArmorAttachmentPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(VisEquipment), "AttachArmor", new[] { typeof(int), typeof(int), typeof(int) });
            yield return AccessTools.Method(typeof(VisEquipment), "AttachItem", new[] { typeof(int), typeof(int), typeof(UnityEngine.Transform), typeof(bool), typeof(bool), typeof(int) });
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var lookup = AccessTools.Method(typeof(ObjectDB), "GetItemPrefab", new[] { typeof(int) });
            var resolve = AccessTools.Method(typeof(ArmorManager), nameof(ArmorManager.ResolveVisualPrefab));
            int matches = 0;

            foreach (var instruction in instructions)
            {
                yield return instruction;

                if (instruction.Calls(lookup))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, resolve);
                    matches++;
                }
            }

            if (matches != 1)
            {
                throw new InvalidOperationException("Valheim armor attachment changed: expected one item-prefab lookup.");
            }
        }
    }
}
