using System.Collections.Generic;

namespace QuantumElevators
{
    internal class TransportationServices
    {
        private static readonly ModLog<TransportationServices> _log = new ModLog<TransportationServices>();
        private static int _counter = 0;
        private static readonly List<EntityPlayer> _players = GameManager.Instance.World.Players.list;
        public static int SecureQuantumBlockId { get; set; } = 0; // TODO: reduce access, maybe move to another component
        public static int PortableQuantumBlockId { get; set; } = 0; // TODO: reduce access, maybe move to another component

        internal static void OnGameStartDone()
        {
            SecureQuantumBlockId = Block.nameIdMapping.GetIdForName("quantumElevatorBlockSecure");
            PortableQuantumBlockId = Block.nameIdMapping.GetIdForName("quantumElevatorBlockPortable");
        }

        internal static void OnGameUpdate()
        {
            if (_counter < 5)
            {
                _counter++;
                return;
            }
            _counter = 0;
            CyclePlayers();
        }

        private static void CyclePlayers()
        {
            foreach (var player in _players)
            {
                HandleViewControls(player);
                //CheckDirectionBuff(player); // TODO: it seems I cannot find a way to make actions work
            }
        }

        private static void HandleViewControls(EntityPlayer player)
        {
            if (!player.Buffs.HasBuff("inQuantumElevator"))
            {
                return;
            }

            // TODO: get parent block now and walk down/up based on that
            // TODO: reduce frequency check and confirm player is still on block before executing teleport?
            var angle = CheckAngle(player);
            if (angle == 0)
            {
                return;
            }
            if (TryGetClientInfo(player.entityId, out var clientInfo)
                && CanAccess(player, clientInfo.InternalId, out var sourcePos, out var sourceBlockValue, out var sourceTileEntity)
                && (angle > 0
                    ? TryGetFloorAbove(sourcePos, sourceBlockValue, sourceTileEntity, clientInfo.InternalId, out var destination)
                    : TryGetFloorBelow(sourcePos, sourceTileEntity, clientInfo.InternalId, out destination)))
            {
                Teleport(player, clientInfo, destination);
            }
        }

        private static bool TryGetClientInfo(int entityId, out ClientInfo clientInfo)
        {
            clientInfo = ConnectionManager.Instance.Clients.ForEntityId(entityId);
            return clientInfo != null;
        }

        private static int CheckAngle(EntityPlayer player)
        {
            if (player.rotation.x > 40)
            {
                return +1;
            }
            else if (player.rotation.x < -40)
            {
                return -1;
            }
            return 0;
        }

        // TODO: test with these debug logs enabled...
        private static bool CanAccess(EntityPlayer player, PlatformUserIdentifierAbs internalId, out Vector3i blockPos, out BlockValue blockValue, out TileEntitySign secureTileEntity)
        {
            _log.Debug($"checking if {player.GetDebugName()} CanAccess {player.GetBlockPosition()}");
            blockValue = GetBaseBlockPositionAndValue(player.GetBlockPosition(), out blockPos);

            if (PortableQuantumBlockId == blockValue.Block.blockID)
            {
                _log.Debug($"{player.GetBlockPosition()} contains a PortableQuantumBlock; premission is always granted");
                secureTileEntity = null;
                return true;
            }

            if (SecureQuantumBlockId != blockValue.Block.blockID)
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

        // TODO: delete? why was this here, again?
        private static bool HasPermission(Vector3i blockPos, Block block, PlatformUserIdentifierAbs internalId, out bool elevatorHasPassword)
        {
            if (!block.HasTileEntity)
            {
                elevatorHasPassword = false;
                return true; // blocks without tile entities (security class) will be allowed
            }
            if (!(GameManager.Instance.World.GetTileEntity(GameManager.Instance.World.ChunkCache.ClusterIdx, blockPos) is ILockable lockable))
            {
                elevatorHasPassword = false;
                return true;
            }
            elevatorHasPassword = lockable.HasPassword();
            return !lockable.IsLocked() || lockable.IsUserAllowed(internalId);
        }

        private static TileEntitySign GetTileEntitySignAt(Vector3i pos)
        {
            return GameManager.Instance.World.GetTileEntity(GameManager.Instance.World.ChunkCache.ClusterIdx, pos) as TileEntitySign;
        }

        /**
         * <summary>Confirm if player has permission to the given secure TileEntity.</summary>
         * <param name="internalId">Platform identifier representing the player trying to confirm access.</param>
         * <param name="secureTileEntity">Secure TileEntity user is trying to confirm access to.</param>
         * <param name="hasPassword">Whether the secure TileEntity has a password set.</param>
         * <returns>Whether player has permission to the given secure TileEntity.</returns>
         */
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

        /**
         * <summary>Confirm if player has permission to the given target.</summary>
         * <param name="internalId">Platform identifier representing the player trying to confirm access.</param>
         * <param name="source">Source Secure TileEntity used as an opportunistic key for target.</param>
         * <param name="target">Target Secure TileEntity user is trying to confirm access to.</param>
         * <returns>Whether player has permission to the given target.</returns>
         */
        private static bool CanAccess(PlatformUserIdentifierAbs internalId, TileEntitySign source, TileEntitySign target)
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

        private static void HandleLockedOut(EntityPlayer player, bool elevatorHasPassword)
        {
            if (elevatorHasPassword && !player.Buffs.HasBuff("notifyQuantumElevatorLockedWithPassword"))
            {
                _ = player.Buffs.AddBuff("notifyQuantumElevatorLockedWithPassword");
            }
            if (!elevatorHasPassword && !player.Buffs.HasBuff("notifyQuantumElevatorLocked"))
            {
                _ = player.Buffs.AddBuff("notifyQuantumElevatorLocked");
            }
        }

        private static bool TryGetFloorAbove(Vector3i sourcePos, BlockValue sourceBlockValue, TileEntitySign source, PlatformUserIdentifierAbs internalId, out Vector3i targetPos)
        {
            _log.Debug("calling TryGetFloorAbove");
            targetPos = sourcePos;
            var clrId = GameManager.Instance.World.ChunkCache.ClusterIdx;
            // confirm if 256 is the right place to stop

            _log.Debug($"planning to look above, starting at {targetPos}");
            BlockValue targetBlockValue;
            for (targetPos.y += BlockHeight(sourceBlockValue); targetPos.y < 256; targetPos.y += BlockHeight(targetBlockValue))
            {
                targetBlockValue = GetBaseBlockPositionAndValue(targetPos, out targetPos);
                _log.Debug($"now checking {targetPos}");

                if (PortableQuantumBlockId == targetBlockValue.Block.blockID)
                {
                    return true;
                }

                if (SecureQuantumBlockId != targetBlockValue.Block.blockID)
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
                    _log.Debug($"found the next accessible elevator at {targetPos}");
                    return true;
                }
            }

            _log.Debug($"no elevator was found below");
            return false;
        }

        private static bool TryGetFloorBelow(Vector3i sourcePos, TileEntitySign source, PlatformUserIdentifierAbs internalId, out Vector3i targetPos)
        {
            _log.Debug("calling TryGetFloorBelow");
            targetPos = sourcePos;
            // TODO: confirm if 0 is the right place to stop
            for (targetPos.y--; targetPos.y > 0; targetPos.y--)
            {
                var targetBlockValue = GetBaseBlockPositionAndValue(targetPos, out targetPos);
                _log.Debug($"checking {targetPos} ({Block.nameIdMapping.GetNameForId(targetBlockValue.Block.blockID)})");

                if (PortableQuantumBlockId == targetBlockValue.Block.blockID)
                {
                    return true;
                }

                if (SecureQuantumBlockId == targetBlockValue.Block.blockID
                    && CanAccess(internalId, source, GetTileEntitySignAt(targetPos)))
                {
                    _log.Debug($"found the next accessible elevator at {targetPos}");
                    return true;
                }
            }

            _log.Debug($"no elevator was found below");
            return false;
        }

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
                // blockValue = GameManager.Instance.World.GetBlock(blockPosition);
            }
            else
            {
                _log.Debug($"Block found is not a multi-block; self: {blockValue}, isChild? {blockValue.ischild}");
            }
            return blockValue;
        }

        private static int BlockHeight(BlockValue blockValue)
        {
            if (blockValue.Block.isMultiBlock)
            {
                _log.Debug($"height of focused block is {blockValue.Block.multiBlockPos.dim.y}");
                return blockValue.Block.multiBlockPos.dim.y;
            }
            _log.Debug($"height of focused block is 1");
            return 1;
        }

        private static void Teleport(EntityPlayer player, ClientInfo clientInfo, Vector3i elevatorPos)
        {
            //player.SetVelocity(player.position + elevatorPos); // TODO: test to see if this effects what other _players see
            _log.Debug($"attempting to move to {elevatorPos}");

            _ = player.Buffs.AddBuff("triggerQuantumJump");
            var netPackageTeleportPlayer = NetPackageManager.GetPackage<NetPackageTeleportPlayer>().Setup(elevatorPos.ToVector3CenterXZ(), new UnityEngine.Vector3(0, player.rotation.y, 0), true);
            clientInfo.SendPackage(netPackageTeleportPlayer);
        }

        /*
        // way less responsive for some reason... buffs are networked, but still. weird
        private static void CheckDirectionBuff(EntityPlayer player) {
            if (Locks.TryGetValue(player.entityId, out var cooldown)) {
                if (cooldown < 8) { // 8 = around 2 seconds?
                    Locks[player.entityId] = cooldown + 1;
                } else {
                    Locks.Remove(player.entityId);
                }
                return;
            }

            if (player.Buffs.HasBuff("triggerQuantumDrop")) {
                //player.Buffs.AddBuff("quantumCooldown");
                Locks.Add(player.entityId, 0);
                var playerBlockPos = player.GetBlockPosition();
                var blockValue = GameManager.Instance.World.GetBlock(playerBlockPos);
                if (blockValue.Block.blockID == QuantumBlockId) {
                    MessagingSystem.Whisper($"Found quantum block at {playerBlockPos}", player.entityId);
                } else {
                    MessagingSystem.Whisper($"No quantum block found at {playerBlockPos}", player.entityId);
                }
            }
        }
        */

        /*
        private static void HandlePendingRequests() {
            _log.Debug("HandlePendingRequests");
            foreach (var player in _players) {
                if (player.GetCVar("quantumJumpRequested") == 1) {
                    MessagingSystem.Whisper("handled quantum jump", player.entityId);
                    player.SetCVar("quantumJumpRequested", 0);
                }
                if (player.GetCVar("quantumDropRequested") == 1) {
                    MessagingSystem.Whisper("handled quantum drop", player.entityId);
                    player.SetCVar("quantumDropRequested", 0);
                }
            }
        }
        */
    }
}
