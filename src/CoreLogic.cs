namespace QuantumElevators
{
    internal enum Direction
    {
        Up, Down
    }

    internal class CoreLogic
    {
        private static readonly ModLog<CoreLogic> _log = new ModLog<CoreLogic>();

        #region buff name references
        internal static string BuffInElevatorName { get; private set; } = "inQuantumElevator";
        internal static string BuffTriggerJumpName { get; private set; } = "triggerQuantumJump";
        internal static string BuffCooldownName { get; private set; } = "quantumElevatorCooldown";
        internal static string BuffGoingUpName { get; private set; } = "quantumElevatorGoingUp";
        internal static string BuffGoingDownName { get; private set; } = "quantumElevatorGoingDown";
        internal static string BuffNotifyLockedWithPasswordName { get; private set; } = "notifyQuantumElevatorLockedWithPassword";
        internal static string BuffNotifyLockedName { get; private set; } = "notifyQuantumElevatorLocked";
        #endregion

        #region cvar name references
        internal static string CVarTargetElevationName { get; private set; } = "quantumElevatorTargetElevation";
        #endregion

        /// <summary>
        /// Attempt to warp the given player either above or below the provided Quantum Elevator Block at the provided position.
        /// </summary>
        /// <param name="direction">The direction to attempt travel in, either up or down.</param>
        /// <param name="player">The player attempting to warp.</param>
        /// <param name="sourceBlockPos">The block position of the panel this player is attempting to warp from.</param>
        /// <param name="sourceBlockValue">The block value of the panel this player is attempting to warp from.</param>
        internal static void Warp(Direction direction, EntityPlayer player, Vector3i sourceBlockPos, BlockValue sourceBlockValue)
        {
            if (player.Buffs.HasBuff(BuffCooldownName))
            {
                return;
            }
            _ = player.Buffs.AddBuff(BuffCooldownName);

            var userIdentifier = GameManager.Instance.persistentPlayers.GetPlayerDataFromEntityID(player.entityId).UserIdentifier;

            if (CanAccess(player, userIdentifier, sourceBlockPos, sourceBlockValue, out var sourceTileEntity)
                && (direction == Direction.Up
                    ? TryGetFloorAbove(userIdentifier, sourceBlockPos, sourceBlockValue, sourceTileEntity, out var destination)
                    : TryGetFloorBelow(userIdentifier, sourceBlockPos, sourceTileEntity, out destination)))
            {
                var destinationCenter = destination.ToVector3CenterXZ();
                _log.Debug($"about to warp player to {destinationCenter}");
                if (player is EntityPlayerLocal localPlayer)
                {
                    player.CrouchingLocked = false; // auto unlock crouching for ease of use
                    _ = localPlayer.Buffs.AddBuff(BuffTriggerJumpName); // for visuals & sound-effects
                    localPlayer.TeleportToPosition(destinationCenter, true, localPlayer.rotation);
                }
                else if (TryGetClientInfo(player.entityId, out var clientInfo))
                {
                    _ = player.Buffs.AddBuff(BuffTriggerJumpName); // for visuals & sound-effects
                    clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageTeleportPlayer>().Setup(destinationCenter, player.rotation, true));
                }
                else
                {
                    return;
                }
                _log.Debug($"after teleport, player now at {player.position}");
                player.Buffs.RemoveBuff(BuffCooldownName);
                _log.Debug($"after remove buff, player now at {player.position}");
            }
        }

        /// <summary>
        /// Confirm the given player's access to the secureTileEntity found at blockPos.
        /// </summary>
        /// <param name="player">Player to confirm access for.</param>
        /// <param name="internalId">Player ABS to confirm access with.</param>
        /// <param name="blockPos">Block position of TileEntitySign to confirm access to.</param>
        /// <param name="secureTileEntity">TileEntitySign identified during the confirmation process.</param>
        /// <returns>Whether access is allowed for this player.</returns>
        /// <remarks>This method assumes the provided blockPos value is referring to the parent pos in the event of a multi-dim block.</remarks>
        internal static bool CanAccess(EntityPlayer player, PlatformUserIdentifierAbs internalId, Vector3i blockPos, BlockValue blockValue, out TileEntitySign secureTileEntity)
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
        /// Confirm if player has permission to the given secure TileEntity.
        /// </summary>
        /// <param name="internalId">Platform identifier representing the player trying to confirm access.</param>
        /// <param name="secureTileEntity">Secure TileEntity user is trying to confirm access to.</param>
        /// <param name="hasPassword">Whether the secure TileEntity has a password set.</param>
        /// <returns>Whether player has permission to the given secure TileEntity.</returns>
        internal static bool CanAccess(PlatformUserIdentifierAbs internalId, TileEntitySign secureTileEntity, out bool hasPassword)
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
        /// Confirm if player has permission to the given target.
        /// </summary>
        /// <param name="internalId">Platform identifier representing the player trying to confirm access.</param>
        /// <param name="source">Source Secure TileEntity used as an opportunistic key for target.</param>
        /// <param name="target">Target Secure TileEntity user is trying to confirm access to.</param>
        /// <returns>Whether player has permission to the given target.</returns>
        internal static bool CanAccess(PlatformUserIdentifierAbs internalId, TileEntitySign source, TileEntitySign target)
        {
            if (CanAccess(internalId, target, out var targetHasPassword))
            {
                return true;
            } // target is locked and player doesn't have access
            if (!targetHasPassword
                || source == null
                || !source.IsLocked()
                || !source.HasPassword())
            {
                _log.Debug($"CANNOT ACCESS because: !targetHasPassword ({!targetHasPassword}) || source == null({source == null}) || !source.IsLocked()({!source.IsLocked()}) || !source.HasPassword()({!source.HasPassword()})");
                return false;
            } // source and target have passwords, source is locked
            if (source.GetOwner().Equals(target.GetOwner())
                && source.GetPassword().Equals(target.GetPassword()))
            {
                _log.Debug($"CAN ACCESS because: source.GetOwner() == target.GetOwner()({source.GetOwner() == target.GetOwner()}) && source.GetPassword() == target.GetPassword()({source.GetPassword() == target.GetPassword()})");
                target.GetUsers().Add(internalId); // dynamically register this user to target!
                target.SetModified();
                return true;
            } // source/target owners or passwords don't match=
            _log.Debug($@"CANNOT ACCESS because: source.GetOwner() != target.GetOwner() || source.GetPassword() != target.GetPassword()
source.owner:
    CombinedString:{source.GetOwner().CombinedString}
    PlatformIdentifierString:{source.GetOwner().PlatformIdentifierString}
    ReadablePlatformUserIdentifier:{source.GetOwner().ReadablePlatformUserIdentifier}
target.owner:
    CombinedString:{target.GetOwner().CombinedString}
    PlatformIdentifierString:{target.GetOwner().PlatformIdentifierString}
    ReadablePlatformUserIdentifier:{target.GetOwner().ReadablePlatformUserIdentifier}
source.pass:{source.GetPassword()}
target.pass:{target.GetPassword()}");
            return false;
        }

        /// <summary>
        /// Attempt to retrieve ClientInfo for the given entityId.
        /// </summary>
        /// <param name="entityId">The entityId to attempt to retrieve ClientInfo with.</param>
        /// <param name="clientInfo">The ClientInfo retrieved with the given entityId.</param>
        /// <returns>Whether ClientInfo could be retrieved.</returns>
        internal static bool TryGetClientInfo(int entityId, out ClientInfo clientInfo)
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
        /// <param name="targetPos">The next position of the elevator block the player represented by the provided internal id can warp to.</param>
        /// <returns>Whether a quantum elevator block above the sourcePos can be warped to.</returns>
        internal static bool TryGetFloorAbove(PlatformUserIdentifierAbs internalId, Vector3i sourcePos, BlockValue sourceBlockValue, TileEntitySign source, out Vector3i targetPos)
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
        /// <param name="targetPos">The next position of the elevator block the player represented by the provided internal id can warp to.</param>
        /// <returns>Whether a quantum elevator block above the sourcePos can be warped to.</returns>
        internal static bool TryGetFloorBelow(PlatformUserIdentifierAbs internalId, Vector3i sourcePos, TileEntitySign source, out Vector3i targetPos)
        {
            _log.Debug("calling TryGetFloorBelow");
            targetPos = sourcePos;
            // TODO: confirm if 0 is the right place to stop
            for (targetPos.y--; targetPos.y > 0; targetPos.y--)
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
        internal static int GetBlockHeight(BlockValue blockValue)
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
        internal static BlockValue GetBaseBlockPositionAndValue(Vector3i pos, out Vector3i blockPosition)
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
                // blockValue = GameManager.Instance.World.GetBlock(blockPosition);
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
        internal static TileEntitySign GetTileEntitySignAt(Vector3i pos)
        {
            return GameManager.Instance.World.GetTileEntity(GameManager.Instance.World.ChunkCache.ClusterIdx, pos) as TileEntitySign;
        }

        /// <summary>
        /// React to the situation where a player cannot warp to an elevator above or below due to not having access to the source panel. 
        /// </summary>
        /// <param name="player">The player who was not able to access the source panel.</param>
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
    }
}
