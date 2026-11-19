using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using HexBattleMage.Core.Managers;

namespace HexBattleMage.Patches
{
    [HarmonyPatch(typeof(VisEquipment), "AttachArmor", new[] { typeof(int), typeof(int), typeof(int) })]
    internal static class CloakAttachmentPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var lookup = AccessTools.Method(typeof(ObjectDB), "GetItemPrefab", new[] { typeof(int) });
            var resolve = AccessTools.Method(typeof(CloakManager), nameof(CloakManager.ResolveArmorPrefab));
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
                throw new InvalidOperationException("Valheim AttachArmor changed: expected one item-prefab lookup.");
            }
        }
    }
}
