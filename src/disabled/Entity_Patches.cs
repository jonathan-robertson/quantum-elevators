using System;
using UnityEngine;

namespace QuantumElevators
{
    /// <summary>
    /// Triggers when *remote* players are repositioning
    /// </summary>
    //[HarmonyPatch(typeof(Entity), "SetPosition")]
    internal class EntityPlayer_SetPosition_Patches
    {
        private static readonly ModLog<EntityPlayer_SetPosition_Patches> _log = new ModLog<EntityPlayer_SetPosition_Patches>();

        private static Vector3i current = Vector3i.zero;
        private static Vector3i future = Vector3i.zero;

        public static void Prefix(Entity __instance, Vector3 _pos)
        {
            try
            {
                current = World.worldToBlockPos(__instance.position);
                future = World.worldToBlockPos(_pos);

                /*
                if (current != future)
                {
                    (__instance as EntityPlayer).blockValueStandingOn
                }

                __instance.GetBlockPosition();
                future = _pos.GetBlockPosition();


                if ( != _pos.GetBlockPosition())
                {

                }
                */
            }
            catch (Exception e)
            {
                _log.Error($"Failed EntityPlayer_SetPosition_Patches Prefix: check position for entity {__instance}", e);
            }
        }

        public static void Postfix(Entity __instance, Vector3i __state)
        {
            try
            {
                if (__state != Vector3i.zero && __instance is EntityPlayer player)
                {
                    CoreLogic.CheckPosition(player);
                }
            }
            catch (Exception e)
            {
                _log.Error($"Failed EntityPlayer_SetPosition_Patches Postfix: check position for entity {__instance}", e);
            }
        }
    }
}
