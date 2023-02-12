using HarmonyLib;
using System;
using System.Collections.Concurrent;

namespace QuantumElevators
{
    internal enum PlayerState
    {
        Neutral, Crouching, Jumping
    }

    [HarmonyPatch(typeof(EntityAlive), "updateCurrentBlockPosAndValue")]
    internal class EntityAlive_updateCurrentBlockPosAndValue_Patches
    {
        private static readonly ModLog<EntityAlive_updateCurrentBlockPosAndValue_Patches> _log = new ModLog<EntityAlive_updateCurrentBlockPosAndValue_Patches>();

        /// <summary>
        /// Concurrency-safe dictionary for recording what the last state was.
        /// Used to identify "the moment" player switches from neutral stance to jumping or crouching to simulate single-button reaction
        /// and avoid multiple triggers when player holds key down.
        /// </summary>
        private static readonly ConcurrentDictionary<int, PlayerState> _prevStates = new ConcurrentDictionary<int, PlayerState>();

        /// <summary>
        /// Patch responsible for 'intercepting' crouch/jump controls if the given player is standing on a quantum block. 
        /// </summary>
        /// <param name="__instance">EntityAlive instance to check from.</param>
        /// <param name="___blockPosStandingOn">Block Position this entity is standing on.</param>
        /// <param name="___blockValueStandingOn">BlockValue this entity is standing on.</param>
        /// <param name="___bCrouching">Whether the client behind this entity has the Crouch key held down (or CrouchLock is enabled).</param>
        /// <param name="___bJumping">Whether the client behind this entity has the Jump key held down.</param>
        public static void Postfix(EntityAlive __instance, Vector3i ___blockPosStandingOn, BlockValue ___blockValueStandingOn, bool ___bCrouching, bool ___bJumping)
        {
            try
            {
                if (CoreLogic.IsQuantumBlock(___blockValueStandingOn.Block.blockID) && __instance is EntityPlayer player)
                {
                    // update elevation for buff shown while player standing on block (informative, only)
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
                        if (currentPlayerState == PlayerState.Crouching)
                        {
                            CoreLogic.Warp(Direction.Down, player, ___blockPosStandingOn, ___blockValueStandingOn);
                        }
                        else
                        {
                            CoreLogic.Warp(Direction.Up, player, ___blockPosStandingOn, ___blockValueStandingOn);
                        }
                        return;
                    }
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
}
