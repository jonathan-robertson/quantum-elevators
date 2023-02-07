using System;

namespace QuantumElevators
{
    //[HarmonyPatch(typeof(EntityPlayerLocal), "updateTransform")]
    internal class EntityPlayerLocal_updateTransform_Patches
    {
        private static readonly ModLog<EntityPlayerLocal_updateTransform_Patches> _log = new ModLog<EntityPlayerLocal_updateTransform_Patches>();

        public static void Prefix(EntityPlayerLocal __instance, ref int __state)
        {
            try
            {
                if (__instance.IsAlive()
                    && __instance.Buffs.HasBuff(CoreLogic.BuffInElevatorName))
                {
                    __state = CoreLogic.CheckAngle(__instance.rotation);
                }
            }
            catch (Exception e)
            {
                _log.Error($"Failed to handle EntityPlayerLocal_updateTransform_Patches Prefix ({__instance.rotation}) for {__instance}", e);
            }
        }

        public static void Postfix(EntityPlayerLocal __instance, ref int __state)
        {
            try
            {
                if (__instance.IsAlive()
                    && __instance.Buffs.HasBuff(CoreLogic.BuffInElevatorName)
                    && __state == 0)
                {
                    var direction = CoreLogic.CheckAngle(__instance.rotation);
                    switch (direction)
                    {
                        case +1:
                            _log.Debug($"{__instance} is now looking up");
                            CoreLogic.Warp(direction, __instance);
                            break;
                        case -1:
                            _log.Debug($"{__instance} is now looking down");
                            CoreLogic.Warp(direction, __instance);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                _log.Error($"Failed to handle EntityPlayerLocal_updateTransform_Patches Postfix ({__instance.rotation}) for {__instance}", e);
            }
        }
    }
}
