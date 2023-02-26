﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuantumElevators
{
    internal enum Direction
    {
        Up, Down
    }

    internal class CoreLogic
    {
        private static readonly ModLog<CoreLogic> _log = new ModLog<CoreLogic>();
        private static readonly Vector3 _single = new Vector3(1, 2, 1);
        private static readonly List<Vector3i> _possibleDirections = new List<Vector3i> { Vector3i.left, Vector3i.right, Vector3i.forward, Vector3i.back };

        #region buff name references
        internal static string BuffTriggerJumpName { get; private set; } = "triggerQuantumJump";
        internal static string BuffCooldownName { get; private set; } = "quantumElevatorCooldown";
        internal static string BuffAtTopFloorName { get; private set; } = "quantumElevatorTopFloor";
        internal static string BuffAtBottomFloorName { get; private set; } = "quantumElevatorBottomFloor";
        internal static string BuffNotifyLockedWithPasswordName { get; private set; } = "notifyQuantumElevatorLockedWithPassword";
        internal static string BuffNotifyLockedName { get; private set; } = "notifyQuantumElevatorLocked";
        #endregion

        #region cvar name references
        internal static string CVarTargetElevationName { get; private set; } = "quantumElevatorTargetElevation";
        #endregion

        /// <summary>
        /// Attempt to warp the given entityPlayer either above or below the provided Quantum Elevator Block at the provided position.
        /// </summary>
        /// <param name="direction">The direction to attempt travel in, either up or down.</param>
        /// <param name="player">The entityPlayer attempting to warp.</param>
        /// <param name="sourceBlockPos">The block position of the panel this entityPlayer is attempting to warp from.</param>
        /// <param name="sourceBlockValue">The block value of the panel this entityPlayer is attempting to warp from.</param>
        internal static void Warp(Direction direction, EntityPlayer player, Vector3i sourceBlockPos, BlockValue sourceBlockValue)
        {
            if (player.Buffs.HasBuff(BuffCooldownName))
            {
                return;
            }
            // Adding the buff cooldown prevents multiple hits from triggering warps;
            //  crouch/jump fires tons of them; we just want the first one.
            _ = player.Buffs.AddBuff(BuffCooldownName);

            var userIdentifier = GameManager.Instance.persistentPlayers.GetPlayerDataFromEntityID(player.entityId).UserIdentifier;
            if (CanAccess(player, userIdentifier, sourceBlockPos, sourceBlockValue, out var sourceTileEntity)
                && (direction == Direction.Up
                    ? !player.Buffs.HasBuff(BuffAtTopFloorName) && TryGetFloorAbove(userIdentifier, sourceBlockPos, sourceBlockValue, sourceTileEntity, out var destination)
                    : !player.Buffs.HasBuff(BuffAtBottomFloorName) && TryGetFloorBelow(userIdentifier, sourceBlockPos, sourceTileEntity, out destination)))
            {
                // clear above/below locks early
                player.Buffs.RemoveBuff(BuffAtTopFloorName);
                player.Buffs.RemoveBuff(BuffAtBottomFloorName);

                var crowdWasAtPanel = Push(destination);

                var destinationCenter = destination.ToVector3CenterXZ();
                _log.Debug($"about to warp entityPlayer to {destinationCenter}");
                if (player is EntityPlayerLocal localPlayer)
                {
                    player.CrouchingLocked = false; // auto unlock crouching for ease of use
                    _ = localPlayer.Buffs.AddBuff(BuffTriggerJumpName); // for visuals & sound-effects
                    localPlayer.TeleportToPosition(destinationCenter, true, localPlayer.rotation);
                    _log.Debug($"after teleport, entityPlayer now at {player.position}");
                    player.Buffs.RemoveBuff(BuffCooldownName);
                    _log.Debug($"after remove buff, entityPlayer now at {player.position}");
                    return;
                }
                else if (TryGetClientInfo(player.entityId, out var clientInfo))
                {
                    _log.Debug($"planning to teleport {player} to {player.position} soon");
                    var buffStatus = player.Buffs.AddBuff(BuffTriggerJumpName); // for visuals & sound-effects
                    if (buffStatus != EntityBuffs.BuffStatus.Added)
                    {
                        _log.Warn($"Failed to apply buff {BuffTriggerJumpName} to player {player} due to buffStatus response of {buffStatus}");
                    }
                    if (crowdWasAtPanel)
                    {
                        _ = ThreadManager.StartCoroutine(WarpRemotePlayerLater(clientInfo, player, destinationCenter));
                    }
                    else
                    {
                        WarpPlayer(clientInfo, player, destinationCenter);
                    }
                }
                return;
            }

            // If we could not find a panel above/below that player has access to, we indicate this to the player
            //  and this also locks future checks in the same direction for a brief period of time.
            _ = direction == Direction.Up ? player.Buffs.AddBuff(BuffAtTopFloorName) : player.Buffs.AddBuff(BuffAtBottomFloorName);
        }

        /// <summary>
        /// Push players and zombies out of the way to make room for another incoming entity.
        /// </summary>
        /// <param name="blockPos">Vector3i of the block to move entities out of.</param>
        /// <returns>Whether any entities were identified within this block (and moved).</returns>
        internal static bool Push(Vector3i blockPos)
        {
            var crowd = GetEntitiesAt(blockPos);
            _log.Debug($"found {crowd.Count} entities at position");

            var offsets = FindOffsets(blockPos);
            _log.Debug($"found {offsets.Count} possible offsets");

            var rand = GameManager.Instance.World.GetGameRandom();

            var blockCenter = blockPos.ToVector3Center();
            for (var i = 0; i < crowd.Count; i++)
            {
                Vector3 newPos;
                switch (offsets.Count)
                {
                    case 0:
                        newPos = blockCenter + FindRandomPositionOnCircle(20);
                        newPos.y = GameManager.Instance.World.GetHeightAt(newPos.x, newPos.z) + 1; // determine surface height 
                        _log.Debug($"NO VALID TARGETS: WARPING FROM {blockPos} TO {newPos}");
                        break;
                    case 1:
                        newPos = blockCenter + offsets[0];
                        _log.Debug($"ONLY ONE VALID TARGET: PUSHING FROM {blockPos} TO {newPos}");
                        break;
                    default:
                        newPos = blockCenter + offsets[rand.RandomRange(offsets.Count)];
                        _log.Debug($"MANY TARGETS: PUSHING FROM {blockPos} TO {newPos}");
                        break;
                }

                if (crowd[i].entityType == EntityType.Player && crowd[i].isEntityRemote)
                {
                    _log.Debug("remote player");

                    if (TryGetClientInfo(crowd[i].entityId, out var clientInfo))
                    {
                        clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageTeleportPlayer>().Setup(newPos, crowd[i].rotation, true));
                    }
                    else
                    {
                        _log.Debug($"server thinks entity {crowd[i].entityId} being pushed is a player, but couldn't find a client connection for it... could've been due to player disconnection at *just* the right time... still strange.");
                    }
                }
                else
                {
                    _log.Debug("non-player or non-remote player");
                    crowd[i].SetPosition(newPos);
                }
            }
            return crowd.Count > 0;
        }

        /// <summary>
        /// Warp the given player to the provided coordinates.
        /// </summary>
        /// <param name="clientInfo">Client to send the warp package to.</param>
        /// <param name="player">Player to warp.</param>
        /// <param name="destinationCenter">Vector3 representing the destination to warp to.</param>
        private static void WarpPlayer(ClientInfo clientInfo, EntityPlayer player, Vector3 destinationCenter)
        {
            clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageTeleportPlayer>().Setup(destinationCenter, player.rotation, true));
            _log.Debug($"after teleport, entityPlayer now at {player.position}");
            player.Buffs.RemoveBuff(BuffCooldownName);
            _log.Debug($"after remove buff, entityPlayer now at {player.position}");
        }

        /// <summary>
        /// Warp the given player to the provided coordinates after a short delay.
        /// </summary>
        /// <param name="clientInfo">Client to send the warp package to.</param>
        /// <param name="player">Player to warp.</param>
        /// <param name="destinationCenter">Vector3 representing the destination to warp to.</param>
        /// <returns></returns>
        private static IEnumerator WarpRemotePlayerLater(ClientInfo clientInfo, EntityPlayer player, Vector3 destinationCenter)
        {
            yield return new WaitForSeconds(0.2f);
            WarpPlayer(clientInfo, player, destinationCenter);
        }

        /// <summary>
        /// Confirm the given entityPlayer's access to the secureTileEntity found at entityBlockPos.
        /// </summary>
        /// <param name="player">Player to confirm access for.</param>
        /// <param name="internalId">Player ABS to confirm access with.</param>
        /// <param name="blockPos">Block position of TileEntitySign to confirm access to.</param>
        /// <param name="secureTileEntity">TileEntitySign identified during the confirmation process.</param>
        /// <returns>Whether access is allowed for this entityPlayer.</returns>
        /// <remarks>This method assumes the provided entityBlockPos value is referring to the parent sourceBlockPos in the event of a multi-dim block.</remarks>
        private static bool CanAccess(EntityPlayer player, PlatformUserIdentifierAbs internalId, Vector3i blockPos, BlockValue blockValue, out TileEntitySign secureTileEntity)
        {
            if (blockValue.Block.blockID == ModApi.PortableQuantumBlockId)
            {
                _log.Debug($"{player.GetBlockPosition()} contains a PortableQuantumBlock; premission is always granted");
                secureTileEntity = null;
                return true;
            }

            if (blockValue.Block.blockID != ModApi.SecureQuantumBlockId)
            {
                _log.Debug($"{player.GetBlockPosition()} does not contain a PortableQuantumBlock or SecureQuantumBlock");
                secureTileEntity = null;
                return false;
            }

            secureTileEntity = GetTileEntitySignAt(blockPos);
            if (!CanAccess(internalId, secureTileEntity, out var elevatorHasPassword))
            {
                HandleLockedOut(player, elevatorHasPassword);
                return false;
            }

            _log.Debug($"confirmed that {player.GetDebugName()} CanAccess {player.GetBlockPosition()}");
            return true;
        }

        /// <summary>
        /// Confirm if entityPlayer has permission to the given secure TileEntity.
        /// </summary>
        /// <param name="internalId">Platform identifier representing the entityPlayer trying to confirm access.</param>
        /// <param name="secureTileEntity">Secure TileEntity user is trying to confirm access to.</param>
        /// <param name="hasPassword">Whether the secure TileEntity has a password set.</param>
        /// <returns>Whether entityPlayer has permission to the given secure TileEntity.</returns>
        private static bool CanAccess(PlatformUserIdentifierAbs internalId, TileEntitySign secureTileEntity, out bool hasPassword)
        {
            if (secureTileEntity == null)
            {
                hasPassword = false;
                return true;
            }
            hasPassword = secureTileEntity.HasPassword();
            return !secureTileEntity.IsLocked() || secureTileEntity.IsUserAllowed(internalId);
        }

        /// <summary>
        /// Confirm if entityPlayer has permission to the given targetBlockPos.
        /// </summary>
        /// <param name="internalId">Platform identifier representing the entityPlayer trying to confirm access.</param>
        /// <param name="source">Source Secure TileEntity moveable as an opportunistic key for targetBlockPos.</param>
        /// <param name="target">Target Secure TileEntity user is trying to confirm access to.</param>
        /// <returns>Whether entityPlayer has permission to the given targetBlockPos.</returns>
        private static bool CanAccess(PlatformUserIdentifierAbs internalId, TileEntitySign source, TileEntitySign target)
        {
            if (CanAccess(internalId, target, out var targetHasPassword))
            {
                return true;
            } // targetBlockPos is locked and entityPlayer doesn't have access
            if (!targetHasPassword
                || source == null
                || !source.IsLocked()
                || !source.HasPassword())
            {
                _log.Debug($"CANNOT ACCESS because: !targetHasPassword ({!targetHasPassword}) || source == null({source == null}) || !source.IsLocked()({!source.IsLocked()}) || !source.HasPassword()({!source.HasPassword()})");
                return false;
            } // source and targetBlockPos have passwords, source is locked
            if (source.GetOwner().Equals(target.GetOwner())
                && source.GetPassword().Equals(target.GetPassword()))
            {
                _log.Debug($"CAN ACCESS because: source.GetOwner() == targetBlockPos.GetOwner()({source.GetOwner() == target.GetOwner()}) && source.GetPassword() == targetBlockPos.GetPassword()({source.GetPassword() == target.GetPassword()})");
                target.GetUsers().Add(internalId); // dynamically register this user to targetBlockPos!
                target.SetModified();
                return true;
            } // source/targetBlockPos owners or passwords don't match=
            _log.Debug($@"CANNOT ACCESS because: source.GetOwner() != targetBlockPos.GetOwner() || source.GetPassword() != targetBlockPos.GetPassword()
source.owner:
    CombinedString:{source.GetOwner().CombinedString}
    PlatformIdentifierString:{source.GetOwner().PlatformIdentifierString}
    ReadablePlatformUserIdentifier:{source.GetOwner().ReadablePlatformUserIdentifier}
targetBlockPos.owner:
    CombinedString:{target.GetOwner().CombinedString}
    PlatformIdentifierString:{target.GetOwner().PlatformIdentifierString}
    ReadablePlatformUserIdentifier:{target.GetOwner().ReadablePlatformUserIdentifier}
source.pass:{source.GetPassword()}
targetBlockPos.pass:{target.GetPassword()}");
            return false;
        }

        /// <summary>
        /// Attempt to retrieve ClientInfo for the given entityId.
        /// </summary>
        /// <param name="entityId">The entityId to attempt to retrieve ClientInfo with.</param>
        /// <param name="clientInfo">The ClientInfo retrieved with the given entityId.</param>
        /// <returns>Whether ClientInfo could be retrieved.</returns>
        private static bool TryGetClientInfo(int entityId, out ClientInfo clientInfo)
        {
            clientInfo = ConnectionManager.Instance.Clients.ForEntityId(entityId);
            return clientInfo != null;
        }

        /// <summary>
        /// Try fetching the next Quantum Elevator block above the provided source block that the given internal id has permission to access.
        /// </summary>
        /// <param name="internalId">The provided internal id to check for access permissions for.</param>
        /// <param name="sourcePos">The source block position to start scanning from.</param>
        /// <param name="sourceBlockValue">The source block value to check when confirming whether this block </param>
        /// <param name="source">The Tile Entity to reference when leveraging passthrough access.</param>
        /// <param name="targetPos">The next position of the elevator block the entityPlayer represented by the provided internal id can warp to.</param>
        /// <returns>Whether a quantum elevator block above the sourcePos can be warped to.</returns>
        private static bool TryGetFloorAbove(PlatformUserIdentifierAbs internalId, Vector3i sourcePos, BlockValue sourceBlockValue, TileEntitySign source, out Vector3i targetPos)
        {
            _log.Debug("calling TryGetFloorAbove");
            targetPos = sourcePos;
            var clrId = GameManager.Instance.World.ChunkCache.ClusterIdx;
            // confirm if 256 is the right place to stop

            _log.Debug($"planning to look above, starting at {targetPos}");
            BlockValue targetBlockValue;
            for (targetPos.y += GetBlockHeight(sourceBlockValue); targetPos.y < 256; targetPos.y += GetBlockHeight(targetBlockValue))
            {
                targetBlockValue = GetBaseBlockPositionAndValue(targetPos, out targetPos);
                _log.Debug($"now checking {targetPos}");

                if (targetBlockValue.Block.blockID == ModApi.PortableQuantumBlockId)
                {
                    _log.Debug($"found the next accessible (portable) elevator at {targetPos} can be accessed");
                    return true;
                }

                if (targetBlockValue.Block.blockID != ModApi.SecureQuantumBlockId)
                {
                    if (GameManager.Instance.World.IsOpenSkyAbove(clrId, targetPos.x, targetPos.y, targetPos.z))
                    {
                        _log.Debug($"open sky above {targetPos}; abandoning above check");
                        return false;
                    }
                    continue;
                }

                if (CanAccess(internalId, source, GetTileEntitySignAt(targetPos)))
                {
                    _log.Debug($"found the next accessible (secured) elevator at {targetPos} can be accessed");
                    return true;
                }
            }

            _log.Debug($"no elevator was found below");
            return false;
        }

        /// <summary>
        /// Try fetching the next Quantum Elevator block above the provided source block that the given internal id has permission to access.
        /// </summary>
        /// <param name="internalId">The provided internal id to check for access permissions for.</param>
        /// <param name="sourcePos">The source block position to start scanning from.</param>
        /// <param name="source">The Tile Entity to reference when leveraging passthrough access.</param>
        /// <param name="targetPos">The next position of the elevator block the entityPlayer represented by the provided internal id can warp to.</param>
        /// <returns>Whether a quantum elevator block above the sourcePos can be warped to.</returns>
        private static bool TryGetFloorBelow(PlatformUserIdentifierAbs internalId, Vector3i sourcePos, TileEntitySign source, out Vector3i targetPos)
        {
            _log.Debug("calling TryGetFloorBelow");
            targetPos = sourcePos;
            for (targetPos.y--; targetPos.y > 1; targetPos.y--)
            {
                var targetBlockValue = GetBaseBlockPositionAndValue(targetPos, out targetPos);
                _log.Debug($"checking {targetPos} ({Block.nameIdMapping.GetNameForId(targetBlockValue.Block.blockID)})");

                if (ModApi.PortableQuantumBlockId == targetBlockValue.Block.blockID)
                {
                    _log.Debug($"found the next accessible (portable) elevator at {targetPos} can be accessed");
                    return true;
                }

                if (ModApi.SecureQuantumBlockId == targetBlockValue.Block.blockID
                    && CanAccess(internalId, source, GetTileEntitySignAt(targetPos)))
                {
                    _log.Debug($"found the next accessible (secured) elevator at {targetPos} can be accessed");
                    return true;
                }
            }

            _log.Debug($"no elevator was found below");
            return false;
        }

        /// <summary>
        /// Calculate and return the given BlockValue's height.
        /// </summary>
        /// <param name="blockValue">The BlockValue to determine height for.</param>
        /// <returns>The height of the BlockValue provided.</returns>
        private static int GetBlockHeight(BlockValue blockValue)
        {
            if (blockValue.Block.isMultiBlock)
            {
                _log.Debug($"height of focused block is {blockValue.Block.multiBlockPos.dim.y}");
                return blockValue.Block.multiBlockPos.dim.y;
            }
            _log.Debug($"height of focused block is 1");
            return 1;
        }

        /// <summary>
        /// Fetch and return the BlockValue found at the given position.
        /// </summary>
        /// <param name="pos">The given Block Position to retrieve a block value from.</param>
        /// <param name="blockPosition">Parent block position of the retrieved BlockValue found at the given position.</param>
        /// <returns>The block value found at the given position.</returns>
        private static BlockValue GetBaseBlockPositionAndValue(Vector3i pos, out Vector3i blockPosition)
        {
            blockPosition = pos;
            _log.Debug($"GetBaseBlockPositionAndValue at position {pos}");
            var blockValue = GameManager.Instance.World.ChunkCache.GetBlock(pos);
            if (blockValue.ischild)
            {
                _log.Debug($"Block found is a multi-block; self: {blockValue}, isChild? {blockValue.ischild}, parent: {blockValue.parent}");
                _log.Debug($">> parentY: {blockValue.parenty}");
                blockPosition.y += blockValue.parenty;
                // NOTE: block value will be the same; leaving note here in case we ever find a way to make compound blocks
                // blockValueBody = GameManager.Instance.World.GetBlock(blockPosition);
            }
            else
            {
                _log.Debug($"Block found is not a multi-block; self: {blockValue}, isChild? {blockValue.ischild}");
            }
            return blockValue;
        }

        /// <summary>
        /// Retrieve and return the TileEntitySign from the given position.
        /// </summary>
        /// <param name="pos">The position to retrieve a TileEntitySign from (this must be the parent position of a multi-dim block).</param>
        /// <returns>The TileEntitySign retrieved from the given position.</returns>
        private static TileEntitySign GetTileEntitySignAt(Vector3i pos)
        {
            return GameManager.Instance.World.GetTileEntity(GameManager.Instance.World.ChunkCache.ClusterIdx, pos) as TileEntitySign;
        }

        /// <summary>
        /// React to the situation where a entityPlayer cannot warp to an elevator above or below due to not having access to the source panel. 
        /// </summary>
        /// <param name="player">The entityPlayer who was not able to access the source panel.</param>
        /// <param name="elevatorHasPassword">Whether the source panel has a password.</param>
        private static void HandleLockedOut(EntityPlayer player, bool elevatorHasPassword)
        {
            if (elevatorHasPassword && !player.Buffs.HasBuff(BuffNotifyLockedWithPasswordName))
            {
                _ = player.Buffs.AddBuff(BuffNotifyLockedWithPasswordName);
            }
            if (!elevatorHasPassword && !player.Buffs.HasBuff(BuffNotifyLockedName))
            {
                _ = player.Buffs.AddBuff(BuffNotifyLockedName);
            }
        }

        /// <summary>
        /// Determine and return the possible offsets to push entities into from a provided block position.
        /// </summary>
        /// <param name="position">Vector3i block position to check for offsets around.</param>
        /// <returns>Possible offsets to push entities into from the provided block position.</returns>
        private static List<Vector3i> FindOffsets(Vector3i position)
        {
            var validOffsets = new List<Vector3i>();
            for (var i = 0; i < _possibleDirections.Count; i++)
            {
                var targetPos = position + _possibleDirections[i];
                _log.Debug($"Checking {targetPos}");
                if (CanMoveTo(targetPos))
                {
                    _log.Debug($"Can move to {targetPos}");
                    validOffsets.Add(_possibleDirections[i]);
                }
                else
                {
                    _log.Debug($"Cannot move to {targetPos}");
                }
            }
            return validOffsets;
        }

        /// <summary>
        /// Return a random position on a circle with the provided distance multiplier.
        /// </summary>
        /// <param name="multiplier">In essence, how far a position should be from the circle's center point.</param>
        /// <returns></returns>
        private static Vector3 FindRandomPositionOnCircle(float multiplier)
        {
            var circle = GameManager.Instance.World.GetGameRandom().RandomOnUnitCircle;
            _log.Debug($"circle: {circle}");
            var around = new Vector3(circle.x, 0, circle.y);
            _log.Debug($"around: {around}");
            around *= multiplier;
            _log.Debug($"around: {around}");
            return around;
        }

        /// <summary>
        /// Retrieve all entities currently within the given block position.
        /// </summary>
        /// <param name="blockPos">Vector3i block position to check for entities within.</param>
        /// <returns>List of entities currently within the provided block position.</returns>
        private static List<EntityAlive> GetEntitiesAt(Vector3i blockPos)
        {
            var adjustedCenter = new Vector3(blockPos.x + 0.5f, blockPos.y + 1f, blockPos.z + 0.5f);
            var bounds = new Bounds(adjustedCenter, _single);
            _log.Debug($"BOUNDS SET: {bounds.min}, {bounds.max}");

            return GameManager.Instance.World.GetLivingEntitiesInBounds(null, bounds);
        }

        /// <summary>
        /// Determine whether the given block position is safe to move to.
        /// </summary>
        /// <param name="blockPos">The position to check move safety for.</param>
        /// <returns>Whether the provided block position can be moved to.</returns>
        /// <remarks>This method is based off of chunk.CanPlayersSpawnAtPos.</remarks>
        private static bool CanMoveTo(Vector3i blockPos, bool _bAllowToSpawnOnAirPos = false, bool _bAllowToSpawnOnWaterPos = false)
        {
            if (blockPos.y < 2 || blockPos.y > 251)
            {
                return false; // protect against moving out of bounds
            }

            var block = GameManager.Instance.World.GetBlock(blockPos.x, blockPos.y - 1, blockPos.z).Block;
            _log.Debug($"block1: {block?.GetLocalizedBlockName()}");
            if (!block.CanPlayersSpawnOn)
            {
                return false; // protect against moving onto a mine
            }

            var block2 = GameManager.Instance.World.GetBlock(blockPos).Block;
            _log.Debug($"block2: {(block2 != null ? block2.GetLocalizedBlockName() : "NULL")}");
            var block3 = GameManager.Instance.World.GetBlock(blockPos.x, blockPos.y + 1, blockPos.z).Block;
            _log.Debug($"block3: {block3?.GetLocalizedBlockName()}");
            return (block.IsCollideMovement
                    || (_bAllowToSpawnOnAirPos && block.blockID == 0)
                    || (_bAllowToSpawnOnWaterPos && Block.list[block.blockID].blockMaterial.IsLiquid))
                && !block2.IsCollideMovement && !block2.shape.IsSolidSpace
                && !block3.IsCollideMovement && !block3.shape.IsSolidSpace;
        }
    }
}
