using HarmonyLib;
using System;
using System.Collections.Concurrent;

namespace QuantumElevators
{
    [HarmonyPatch(typeof(EntityAlive), "updateCurrentBlockPosAndValue")]
    internal class EntityAlive_updateCurrentBlockPosAndValue_Patches
    {
        private static readonly ModLog<EntityAlive_updateCurrentBlockPosAndValue_Patches> _log = new ModLog<EntityAlive_updateCurrentBlockPosAndValue_Patches>();

        private static readonly ConcurrentDictionary<int, PlayerState> _prevStates = new ConcurrentDictionary<int, PlayerState>();

        public static void Postfix(EntityAlive __instance, Vector3i ___blockPosStandingOn, BlockValue ___blockValueStandingOn, bool ___bCrouching, bool ___bJumping/*, JumpState ___jumpState, bool ___CrouchingLocked, int ___walkTypeBeforeCrouch*/)
        {
            try
            {
                if ((___blockValueStandingOn.Block.blockID == ModApi.PortableQuantumBlockId
                        || ___blockValueStandingOn.Block.blockID == ModApi.SecureQuantumBlockId)
                    && __instance is EntityPlayer player)
                {
                    player.CrouchingLocked = false; // TODO: this works for local, but not for remote
                    // TODO: add a method (probably netPackage) for remote players


                    //_log.Info($"player {player}: jump state: {___jumpState}, walkTypeBeforeCrouch: {___walkTypeBeforeCrouch}");
                    player.SetCVar(CoreLogic.CVarTargetElevationName, Utils.Fastfloor(player.position.y));

                    var currentPlayerState = GetCurrentPlayerState(___bCrouching, ___bJumping);
                    if (!_prevStates.TryGetValue(player.entityId, out var prevPlayerState))
                    {
                        _prevStates[player.entityId] = currentPlayerState;
                        return;
                    }
                    _prevStates[player.entityId] = currentPlayerState;

                    if (prevPlayerState == PlayerState.Neutral && currentPlayerState != PlayerState.Neutral)
                    {
                        _log.Info($"{player} was {prevPlayerState} and is now {currentPlayerState}");

                        // TODO: pass ___blockPosStandingOn and ___blockValueStandingOn into Warp method to save calls
                        if (currentPlayerState == PlayerState.Crouching)
                        {
                            CoreLogic.Warp(-1, player);
                        }
                        else
                        {
                            CoreLogic.Warp(+1, player);
                        }
                        return;
                    }

                    //_log.Debug($"player {__instance} on {___blockPosStandingOn}; jumping: {player.Jumping}; crouching: {player.Crouching}");
                }
            }
            catch (Exception e)
            {
                _log.Error("EntityAlive_updateCurrentBlockPosAndValue_Patches Postfix failed: handle block pos change for {__instance}.", e);
            }
        }

        internal static PlayerState GetCurrentPlayerState(bool crouching, bool jumping)
        {
            return !crouching && !jumping ? PlayerState.Neutral : crouching ? PlayerState.Crouching : PlayerState.Jumping;
        }
    }

    internal enum PlayerState
    {
        Neutral, Crouching, Jumping
    }
}
