using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;

namespace QuantumElevators
{
    internal class CoreLogic
    {
        private static readonly ModLog<CoreLogic> _log = new ModLog<CoreLogic>();
        private static readonly int _activationAngle = 40;
        private static readonly ConcurrentDictionary<int, Coroutine> _coroutines = new ConcurrentDictionary<int, Coroutine>();

        public static string BuffInElevatorName { get; private set; } = "inQuantumElevator";
        public static string BuffTriggerJumpName { get; private set; } = "triggerQuantumJump";
        public static string BuffCooldownName { get; private set; } = "quantumElevatorCooldown";
        public static string BuffGoingUpName { get; private set; } = "quantumElevatorGoingUp";
        public static string BuffGoingDownName { get; private set; } = "quantumElevatorGoingDown";
        public static string CVarTargetElevationName { get; private set; } = "quantumElevatorTargetElevation";

        internal static void CheckPosition(EntityPlayer player)
        {
            var blockValue = player.blockValueStandingOn;
            if (blockValue.Block.blockID == ModApi.PortableQuantumBlockId)
            {
                _log.Debug($"Player {player} is standing on a portable quantum block");
            }
        }

        internal static int CheckAngle(Vector3 rotation)
        {
            if (rotation.x > _activationAngle)
            {
                return +1;
            }
            else if (rotation.x < -_activationAngle)
            {
                return -1;
            }
            return 0;
        }

        internal static void Warp(int direction, EntityPlayer player)
        {
            if (player.Buffs.HasBuff(BuffCooldownName))
            {
                return;
            }
            _ = player.Buffs.AddBuff(BuffCooldownName);

            var abs = GameManager.Instance.persistentPlayers.GetPlayerDataFromEntityID(player.entityId).UserIdentifier;

            if (TransportationServices.CanAccess(player, abs, out var sourcePos, out var sourceBlockValue, out var sourceTileEntity)
                && (direction > 0
                    ? TransportationServices.TryGetFloorAbove(sourcePos, sourceBlockValue, sourceTileEntity, abs, out var destination)
                    : TransportationServices.TryGetFloorBelow(sourcePos, sourceTileEntity, abs, out destination)))
            {
                var destinationCenter = destination.ToVector3CenterXZ();
                _log.Debug($"about to warp player to {destinationCenter}");
                if (player is EntityPlayerLocal localPlayer)
                {
                    _ = localPlayer.Buffs.AddBuff(BuffTriggerJumpName); // for visuals & sound-effects
                    localPlayer.SetCVar(CVarTargetElevationName, destinationCenter.y);
                    _ = localPlayer.Buffs.AddBuff(direction > 0 ? BuffGoingUpName : BuffGoingDownName);
                    //destinationCenter.y += 0.2f;
                    localPlayer.TeleportToPosition(destinationCenter, true, localPlayer.rotation);
                }
                else if (TransportationServices.TryGetClientInfo(player.entityId, out var clientInfo))
                {
                    _ = player.Buffs.AddBuff("triggerQuantumJump"); // for visuals & sound-effects
                    player.SetCVar(CVarTargetElevationName, destinationCenter.y);
                    _ = player.Buffs.AddBuff(direction > 0 ? BuffGoingUpName : BuffGoingDownName);
                    //destinationCenter.y += 0.2f;
                    clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageTeleportPlayer>().Setup(destinationCenter, player.rotation, true));
                }
                else
                {
                    // TODO: throw exception?
                }
                _log.Debug($"after teleport, player now at {player.position}");
                player.Buffs.RemoveBuff(BuffCooldownName);
                _log.Debug($"after remove buff, player now at {player.position}");
            }
        }

        internal static void AttemptToWarp(int direction, EntityPlayer player)
        {
            if (_coroutines.TryRemove(player.entityId, out var oldCoroutine))
            {
                ThreadManager.StopCoroutine(oldCoroutine);
            }

            var newCoroutine = ThreadManager.StartCoroutine(AttemptWarpCoroutine(direction, player));
            if (!_coroutines.TryAdd(player.entityId, newCoroutine))
            {
                ThreadManager.StopCoroutine(newCoroutine);
            }
        }

        private static IEnumerator AttemptWarpCoroutine(int direction, EntityPlayer player)
        {
            yield return null;
        }
    }
}
