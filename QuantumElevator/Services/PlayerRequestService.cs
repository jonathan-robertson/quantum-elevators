using QuantumElevator.Utilities;
using System;
using System.Collections.Generic;

namespace QuantumElevator.Services {
    internal class PlayerRequestService {
        private static readonly ModLog log = new ModLog(typeof(PlayerRequestService));
        private static int counter = 0;
        private static readonly List<EntityPlayer> players = GameManager.Instance.World.Players.list;
        public static int QuantumBlockId { get; set; } = 0;

        internal static void OnGameUpdate() {
            try {
                if (counter < 5) {
                    counter++;
                    return;
                }
                counter = 0;
                CyclePlayers();
            } catch (Exception e) {
                log.Error("Failed OnGameUpdate", e);
            }
        }

        private static void CyclePlayers() {
            foreach (var player in players) {
                HandleViewControls(player);
                //CheckDirectionBuff(player); // TODO: it seems I cannot find a way to make actions work
            }
        }

        private static void HandleViewControls(EntityPlayer player) {
            if (player.Buffs.HasBuff("inQuantumElevator")) {
                var angle = CheckAngle(player);

                //MessagingSystem.Whisper($"{player.rotation}", player.entityId);

                var clientInfo = ConnectionManager.Instance.Clients.ForEntityId(player.entityId);
                if (clientInfo == null) {
                    return;
                }

                // TODO: get parent block now and walk down/up based on that
                // TODO: reduce frequency check and confirm player is still on block before executing teleport
                switch (angle) {
                    case 1:
                        //MessagingSystem.Whisper($"^ Check for Elevator Above", player.entityId);
                        if (TryGetFloorAbove(player, clientInfo.InternalId, out var elevatorAbovePos)) {
                            //MessagingSystem.Whisper($"++ Teleporting to floor above", player.entityId);
                            Teleport(player, clientInfo, elevatorAbovePos);
                        }
                        return;
                    case -1:
                        //MessagingSystem.Whisper($"V Check for Elevator Below", player.entityId);
                        if (TryGetFloorBelow(player, clientInfo.InternalId, out var elevatorBelowPos)) {
                            //MessagingSystem.Whisper($"++ Teleporting to floor below", player.entityId);
                            Teleport(player, clientInfo, elevatorBelowPos);
                        }
                        return;
                    case 0:
                        return;
                }
            }
        }

        private static int CheckAngle(EntityPlayer player) {
            if (player.rotation.x > 40) {
                return +1;
            } else if (player.rotation.x < -40) {
                return -1;
            }
            return 0;
        }

        private static bool HasPermission(Vector3i blockPos, Block block, PlatformUserIdentifierAbs internalId, out bool elevatorHasPassword) {
            if (!block.HasTileEntity) {
                elevatorHasPassword = false;
                return true; // blocks without tile entities (security class) will be allowed
            }
            var tileEntity = GameManager.Instance.World.GetTileEntity(GameManager.Instance.World.ChunkCache.ClusterIdx, blockPos) as TileEntitySign;
            elevatorHasPassword = tileEntity.HasPassword(); // TODO: fix Exception here!
            if (!tileEntity.IsLocked()) {
                return true;
            }
            return tileEntity.IsUserAllowed(internalId);
        }

        private static void HandleLockedOut(EntityPlayer player, bool elevatorHasPassword) {
            if (elevatorHasPassword && !player.Buffs.HasBuff("notifyQuantumElevatorLockedWithPassword")) {
                player.Buffs.AddBuff("notifyQuantumElevatorLockedWithPassword");
            }
            if (!elevatorHasPassword && !player.Buffs.HasBuff("notifyQuantumElevatorLocked")) {
                player.Buffs.AddBuff("notifyQuantumElevatorLocked");
            }
        }

        private static bool TryGetFloorAbove(EntityPlayer player, PlatformUserIdentifierAbs internalId, out Vector3i blockPos) {
            GetBaseBlockPositionAndValue(player.GetBlockPosition(), out blockPos, out var blockValue);
            if (blockValue.Block.blockID != QuantumBlockId) {
                return false;
            } else if (!HasPermission(blockPos, blockValue.Block, internalId, out var elevatorHasPassword)) {
                HandleLockedOut(player, elevatorHasPassword);
                return false;
            }
            log.Debug($"current block found to be at {blockPos}");
            // var blockValue = GameManager.Instance.World.GetBlock(playerBlockPos);

            //GameManager.Instance.World.ChunkCache.ChunkProvider.GetWorldSize()
            //GameManager.Instance.World.ChunkCache.chunks
            var clrId = GameManager.Instance.World.ChunkCache.ClusterIdx;
            // TODO: confirm if 256 is the right place to stop
            for (blockPos.y += BlockHeight(blockValue); blockPos.y < 265; blockPos.y += BlockHeight(blockValue)) {
                GetBaseBlockPositionAndValue(blockPos, out blockPos, out blockValue);
                log.Debug($"checking {blockPos}");
                if (blockValue.Block.blockID == QuantumBlockId && HasPermission(blockPos, blockValue.Block, internalId, out _)) {
                    log.Debug($"found the next accessible elevator at {blockPos}");
                    return true;
                }

                if (GameManager.Instance.World.IsOpenSkyAbove(clrId, blockPos.x, blockPos.y, blockPos.z)) {
                    //MessagingSystem.Whisper("above is open sky; aborting check early", player.entityId);
                    log.Debug($"open sky above {blockPos}; abandoning above check");
                    return false;
                }
            }

            //MessagingSystem.Whisper($"No quantum block found at {playerBlockPos}", player.entityId);
            log.Debug($"no elevator was found above");
            return false;
        }

        private static int BlockHeight(BlockValue blockValue) {
            if (blockValue.Block.isMultiBlock) {
                log.Debug($"height of focused block is {blockValue.Block.multiBlockPos.dim.y}");
                return blockValue.Block.multiBlockPos.dim.y;
            }
            log.Debug($"height of focused block is 1");
            return 1;
        }

        private static bool TryGetFloorBelow(EntityPlayer player, PlatformUserIdentifierAbs internalId, out Vector3i blockPos) {
            GetBaseBlockPositionAndValue(player.GetBlockPosition(), out blockPos, out var blockValue);
            if (blockValue.Block.blockID != QuantumBlockId) {
                return false;
            } else if (!HasPermission(blockPos, blockValue.Block, internalId, out var elevatorHasPassword)) {
                HandleLockedOut(player, elevatorHasPassword);
                return false;
            }
            log.Debug($"current block found to be at {blockPos}");
            // var blockValue = GameManager.Instance.World.GetBlock(playerBlockPos);

            //GameManager.Instance.World.ChunkCache.ChunkProvider.GetWorldSize()
            //GameManager.Instance.World.ChunkCache.chunks
            //var clrId = GameManager.Instance.World.ChunkCache.ClusterIdx;
            // TODO: confirm if -2 is the right place to start (using this to avoid top part of a block teleporting to same block on fall)
            // TODO: confirm if 0 is the right place to stop
            for (blockPos.y--; blockPos.y > 0; blockPos.y--) {
                GetBaseBlockPositionAndValue(blockPos, out blockPos, out blockValue);
                log.Debug($"checking {blockPos}");
                if (blockValue.Block.blockID == QuantumBlockId && HasPermission(blockPos, blockValue.Block, internalId, out _)) {   
                    log.Debug($"found the next accessible elevator at {blockPos}");
                    return true;
                }
            }

            //MessagingSystem.Whisper($"No quantum block found at {playerBlockPos}", player.entityId);
            log.Debug($"no elevator was found below");
            return false;
        }

        private static void GetBaseBlockPositionAndValue(Vector3i pos, out Vector3i blockPosition, out BlockValue blockValue) {
            log.Debug($"Checking block at position {pos}");
            blockValue = GameManager.Instance.World.ChunkCache.GetBlock(pos);
            if (blockValue.Block.isMultiBlock && blockValue.ischild) {
                log.Debug($"Block found is a multi-block; self: {blockValue}, isChild? {blockValue.ischild}, parent: {blockValue.parent}");
                log.Debug($">> parentY: {blockValue.parenty}");
                blockPosition = pos;
                blockPosition.y += blockValue.parenty;
            } else {
                log.Debug($"Block found is not a multi-block; self: {blockValue}, isChild? {blockValue.ischild}");
                blockPosition = pos;
            }
        }

        private static void Teleport(EntityPlayer player, ClientInfo clientInfo, Vector3i elevatorPos) {
            NetPackageTeleportPlayer netPackageTeleportPlayer = NetPackageManager.GetPackage<NetPackageTeleportPlayer>().Setup(elevatorPos.ToVector3CenterXZ(), new UnityEngine.Vector3(0, player.rotation.y, 0), false);
            player.Buffs.AddBuff("triggerQuantumJump");
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
            log.Debug("HandlePendingRequests");
            foreach (var player in players) {
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
