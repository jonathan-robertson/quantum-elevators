using System;

namespace QuantumElevators
{
    //[HarmonyPatch(typeof(EntityBuffs), "AddBuff")]
    internal class EntityBuffs_AddBuff_Patches
    {
        private static readonly ModLog<EntityBuffs_AddBuff_Patches> _log = new ModLog<EntityBuffs_AddBuff_Patches>();

        public static void Prefix(EntityBuffs __instance, string _name, int _instigatorId = -1, bool _netSync = true, bool _fromElectrical = false, bool _fromNetwork = false)
        {
            try
            {
                if (_name == CoreLogic.BuffInElevatorName
                    && __instance.parent is EntityPlayerLocal localPlayer
                    && localPlayer.IsAlive()
                    && !localPlayer.Buffs.HasBuff(CoreLogic.BuffInElevatorName))
                {
                    var direction = CoreLogic.CheckAngle(localPlayer.rotation);
                    switch (direction)
                    {
                        case +1:
                            _log.Debug($"{__instance.parent} was looking up on enter");
                            CoreLogic.Warp(direction, localPlayer);
                            break;
                        case -1:
                            _log.Debug($"{__instance.parent} was looking down on enter");
                            CoreLogic.Warp(direction, localPlayer);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                _log.Error($"Failed to handle EntityPlayerLocal_updateTransform_Patches Prefix ({__instance.parent.rotation}) for {__instance}", e);
            }
        }
    }
}
