using System.Collections;
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

            var userIdentifier = GameManager.Instance.persistentPlayers.GetPlayerDataFromEntityID(player.entityId).PrimaryId;
            Vector3i destination = default; // note: do not inline this; mono/msbuild in linux pipeline returns error if we do: error CS0165: Use of unassigned local variable 'destination'
            if (CanAccess(player, userIdentifier, sourceBlockPos, sourceBlockValue, out var sourceTileEntity)
                && (direction == Direction.Up
                    ? !player.Buffs.HasBuff(BuffAtTopFloorName) && TryGetFloorAbove(userIdentifier, sourceBlockPos, sourceBlockValue, sourceTileEntity, out destination)
                    : !player.Buffs.HasBuff(BuffAtBottomFloorName) && TryGetFloorBelow(userIdentifier, sourceBlockPos, sourceTileEntity, out destination)))
            {
                // clear above/below locks early
                player.Buffs.RemoveBuff(BuffAtTopFloorName);
                player.Buffs.RemoveBuff(BuffAtBottomFloorName);

                var crowdWasAtPanel = Push(destination);

                var destinationCenter = destination.ToVector3CenterXZ();
                _log.Debug($"about to warp {player} to {destinationCenter}");
                if (player is EntityPlayerLocal localPlayer)
                {
                    player.CrouchingLocked = false; // auto unlock crouching for ease of use
                    _ = localPlayer.Buffs.AddBuff(BuffTriggerJumpName); // for visuals & sound-effects
                    localPlayer.TeleportToPosition(destinationCenter, true, localPlayer.rotation);
                    _log.Debug($"after teleport, {player} now at {player.position}");
                    player.Buffs.RemoveBuff(BuffCooldownName);
                    _log.Debug($"after remove buff, {player} now at {player.position}");
                    return;
                }
                else if (TryGetClientInfo(player.entityId, out var clientInfo))
                {
                    _log.Debug($"planning to teleport {player} from {player.position} to to {destinationCenter} soon");
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

            if (crowd.Count == 0)
            {
                return false;
            }

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
            return true;
        }

        /// <summary>
        /// Warp the given player to the provided coordinates.
        /// </summary>
        /// <param name="clientInfo">Client to send the warp package to.</param>
        /// <param name="player">Player to warp.</param>
        /// <param name="destinationCenter">Vector3 representing the destination to warp to.</param>
        private static void WarpPlayer(ClientInfo clientInfo, EntityPlayer player, Vector3 destinationCenter)
        {
            var rotation = new Vector3(0, player.rotation.y % 360, 0);
            _log.Debug($"sending NetPackage to remote player {player} at {player.position}: teleport to {destinationCenter} w/ rotation {rotation}");
            clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageTeleportPlayer>().Setup(destinationCenter, rotation, true));
            player.Buffs.RemoveBuff(BuffCooldownName);
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
        /// <param name="blockPos">Block position of TileEntityComposite to confirm access to.</param>
        /// <param name="tileEntityComposite">TileEntityComposite identified during the confirmation process.</param>
        /// <returns>Whether access is allowed for this entityPlayer.</returns>
        /// <remarks>This method assumes the provided entityBlockPos value is referring to the parent sourceBlockPos in the event of a multi-dim block.</remarks>
        private static bool CanAccess(EntityPlayer player, PlatformUserIdentifierAbs internalId, Vector3i blockPos, BlockValue blockValue, out TEFeatureLockable lockableTileEntity)
        {
            if (blockValue.Block.blockID == ModApi.PortableQuantumBlockId)
            {
                _log.Debug($"{player.GetBlockPosition()} contains a PortableQuantumBlock; permission is always granted");
                lockableTileEntity = null;
                return true;
            }

            if (blockValue.Block.blockID != ModApi.SecureQuantumBlockId)
            {
                _log.Debug($"{player.GetBlockPosition()} does not contain a PortableQuantumBlock or SecureQuantumBlock");
                lockableTileEntity = null;
                return false;
            }

            lockableTileEntity = GetLockableTileEntityAt(blockPos);
            if (!CanAccess(internalId, lockableTileEntity, out var elevatorHasPassword))
            {
                HandleLockedOut(player, elevatorHasPassword);
                return false;
            }

            _log.Debug($"confirmed that {player.GetDebugName()} CanAccess {player.GetBlockPosition()}");
            return true;
        }

        /// <summary>
        /// Confirm if entityPlayer has permission to the given secure TEFeatureLockable.
        /// </summary>
        /// <param name="internalId">Platform identifier representing the entityPlayer trying to confirm access.</param>
        /// <param name="tileEntityComposite">Secure TEFeatureLockable user is trying to confirm access to.</param>
        /// <param name="hasPassword">Whether the secure TEFeatureLockable has a password set.</param>
        /// <returns>Whether entityPlayer has permission to the given secure TEFeatureLockable.</returns>
        private static bool CanAccess(PlatformUserIdentifierAbs internalId, TEFeatureLockable lockableTileEntity, out bool hasPassword)
        {
            if (lockableTileEntity == null)
            {
                hasPassword = false;
                return true;
            }
            hasPassword = lockableTileEntity.HasPassword();
            return !lockableTileEntity.IsLocked() || lockableTileEntity.IsUserAllowed(internalId);
        }

        /// <summary>
        /// Confirm if entityPlayer has permission to the given targetBlockPos.
        /// </summary>
        /// <param name="internalId">Platform identifier representing the entityPlayer trying to confirm access.</param>
        /// <param name="source">Source Secure TEFeatureLockable moveable as an opportunistic key for targetBlockPos.</param>
        /// <param name="target">Target Secure TEFeatureLockable user is trying to confirm access to.</param>
        /// <returns>Whether entityPlayer has permission to the given targetBlockPos.</returns>
        private static bool CanAccess(PlatformUserIdentifierAbs internalId, TEFeatureLockable source, TEFeatureLockable target)
        {
            if (CanAccess(internalId, target, out var targetHasPassword))
            {
                _log.Debug($"CAN ACCESS because: player {internalId} already has permission to the target panel.");
                return true;
            } // so target is locked and entityPlayer isn't already registered as an authorized user
            if (!targetHasPassword)
            {
                _log.Debug("CANNOT ACCESS because: target does not have a password.");
                return false;
            } // so target does have a password set
            if (source == null)
            {
                _log.Debug("CANNOT ACCESS because: source is a portable panel, which we cannot apply a password from.");
                return false;
            } // so source is not a portable panel and might have a password for us to apply to target
            if (!source.IsLocked())
            {
                _log.Debug("CANNOT ACCESS because: source is not locked, so we know it does NOT have a password we could try applying to target.");
                return false;
            } // so source is locked
            if (!source.HasPassword())
            {
                _log.Debug("CANNOT ACCESS because: source is locked but has no password, so we have no password to try applying to target.");
                return false;
            } // so source and target each have passwords and we might be able to apply the password from source to target
            if (!source.GetOwner().Equals(target.GetOwner()))
            {
                _log.Debug("CANNOT ACCESS because: source owner does not match target owner, so a password cannot be applied to target even if it matches.");
                return false;
            } // so source and target have the same owners...
            if (!source.GetPassword().Equals(target.GetPassword()))
            {
                _log.Debug("CANNOT ACCESS because: source and target do not share the same password.");
                return false;
            } // so source and target share the same password...
            _log.Debug("CAN ACCESS because: source panel shares owner and password with target panel.");
            target.GetUsers().Add(internalId); // dynamically register this user to targetBlockPos
            target.SetModified();
            return true;
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
        private static bool TryGetFloorAbove(PlatformUserIdentifierAbs internalId, Vector3i sourcePos, BlockValue sourceBlockValue, TEFeatureLockable source, out Vector3i targetPos)
        {
            _log.Trace("calling TryGetFloorAbove");
            var crawlerPos = sourcePos;
            BlockValue targetBlockValue;
            var clrId = GameManager.Instance.World.ChunkCache.ClusterIdx;
            _log.Debug($"planning to look above, starting from {crawlerPos}");
            for (crawlerPos.y += GetBlockHeight(sourceBlockValue); crawlerPos.y < 253; crawlerPos.y += GetBlockHeight(targetBlockValue))
            {
                if (GameManager.Instance.World.IsOpenSkyAbove(clrId, crawlerPos.x, crawlerPos.y, crawlerPos.z))
                {
                    _log.Debug($"open sky above {crawlerPos}; abandoning above check");
                    targetPos = default;
                    return false;
                }

                targetBlockValue = GetBaseBlockPositionAndValue(crawlerPos, out targetPos);
                if (ModApi.PortableQuantumBlockId == targetBlockValue.Block.blockID)
                {
                    _log.Debug($"found the next accessible (portable) elevator at {targetPos} can be accessed");
                    return true;
                }

                if (ModApi.SecureQuantumBlockId == targetBlockValue.Block.blockID
                    && CanAccess(internalId, source, GetLockableTileEntityAt(targetPos)))
                {
                    _log.Debug($"found the next accessible (secured) elevator at {targetPos} can be accessed");
                    return true;
                }
            }

            _log.Trace($"no elevator was found below");
            targetPos = default;
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
        private static bool TryGetFloorBelow(PlatformUserIdentifierAbs internalId, Vector3i sourcePos, TEFeatureLockable source, out Vector3i targetPos)
        {
            _log.Trace("calling TryGetFloorBelow");
            var crawlerPos = sourcePos;
            BlockValue targetBlockValue;
            _log.Debug($"planning to look below, starting from {crawlerPos}");
            for (crawlerPos.y--; crawlerPos.y > 1; crawlerPos.y -= GetBlockHeight(targetBlockValue))
            {
                targetBlockValue = GetBaseBlockPositionAndValue(crawlerPos, out targetPos);
                if (ModApi.PortableQuantumBlockId == targetBlockValue.Block.blockID)
                {
                    _log.Debug($"found the next accessible (portable) elevator at {targetPos} can be accessed");
                    return true;
                }

                if (ModApi.SecureQuantumBlockId == targetBlockValue.Block.blockID
                    && CanAccess(internalId, source, GetLockableTileEntityAt(targetPos)))
                {
                    _log.Debug($"found the next accessible (secured) elevator at {targetPos} can be accessed");
                    return true;
                }
            }

            _log.Trace($"no elevator was found below");
            targetPos = default;
            return false;
        }

        /// <summary>
        /// Calculate and return the given BlockValue's height.
        /// </summary>
        /// <param name="blockValue">The BlockValue to determine height for; if multidim, this value is expected to be the parent.</param>
        /// <returns>The height of the BlockValue provided.</returns>
        private static int GetBlockHeight(BlockValue blockValue)
        {
            var height = 1;
            if (blockValue.Block.isMultiBlock)
            {
                switch (blockValue.rotation) // note: if multidim, we can only pull rotation properly via the parent blockValue
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                        height = blockValue.Block.multiBlockPos.dim.y;
                        break;
                    case 8:
                    case 10:
                    case 13:
                    case 15:
                    case 16:
                    case 18:
                    case 21:
                    case 23:
                        height = blockValue.Block.multiBlockPos.dim.z;
                        break;
                    case 9:
                    case 11:
                    case 12:
                    case 14:
                    case 17:
                    case 19:
                    case 20:
                    case 22:
                        height = blockValue.Block.multiBlockPos.dim.x;
                        break;
                }

                _log.Debug($"calculatedHeight=[{height}] for multidim blockValue=[{blockValue}], multiBlockPos.dim=[{blockValue.Block.multiBlockPos.dim}]");
            }
            else
            {
                _log.Debug($"calculatedHeight=[{height}] for non-multidim blockValue=[{blockValue}]");
            }
            return height;
        }

        /// <summary>
        /// Fetch and return the BlockValue found at the given position.
        /// </summary>
        /// <param name="pos">The given Block Position to retrieve a block value from.</param>
        /// <param name="blockPosition">Parent block position of the retrieved BlockValue found at the given position.</param>
        /// <returns>The block value found at the given position or its parent position.</returns>
        private static BlockValue GetBaseBlockPositionAndValue(Vector3i pos, out Vector3i blockPosition)
        {
            _log.Trace($"GetBaseBlockPositionAndValue at position {pos}");

            var blockValue = GameManager.Instance.World.ChunkCache.GetBlock(pos);

            var blockName = blockValue.Block.blockID == 0
                ? "air"
                : Block.nameIdMapping != null
                    ? Block.nameIdMapping.GetNameForId(blockValue.Block.blockID)
                    : "no name mapping";

            if (blockValue.ischild)
            {
                blockPosition = pos + blockValue.parent;
                blockValue = GameManager.Instance.World.GetBlock(blockPosition);
                _log.Debug($"Identified block: id=[{blockValue.Block.blockID}], name=[{blockName}], blockValue=[{blockValue}], originalPos=[{pos}], parentPos=[{blockPosition}].");
            }
            else
            {
                blockPosition = pos;
                _log.Debug($"Identified block: id=[{blockValue.Block.blockID}], name=[{blockName}], blockValue=[{blockValue}], position=[{pos}].");
            }
            return blockValue;
        }

        /// <summary>
        /// Retrieve and return the TEFeatureLockable from the given position.
        /// </summary>
        /// <param name="pos">The position to retrieve a TEFeatureLockable from (this must be the parent position of a multi-dim block).</param>
        /// <returns>The TEFeatureLockable retrieved from the given position.</returns>
        private static TEFeatureLockable GetLockableTileEntityAt(Vector3i pos)
        {
            return GameManager.Instance.World.GetTileEntity(GameManager.Instance.World.ChunkCache.ClusterIdx, pos) is TileEntityComposite te
                ? te.GetFeature<TEFeatureLockable>()
                : null;
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

        private static bool TryGetParentBlock(Vector3i blockPos, BlockValue blockValue, out Vector3i parentPos, out BlockValue parentBlockValue)
        {
            if (!blockValue.Block.isMultiBlock || !blockValue.ischild)
            {
                parentPos = default;
                parentBlockValue = default;
                return false;
            }

            parentPos = blockPos + blockValue.parent;
            parentBlockValue = GameManager.Instance.World.ChunkCache.GetBlock(parentPos);
            return true;
        }
    }
}
