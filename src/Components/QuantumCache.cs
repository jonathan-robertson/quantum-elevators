using QuantumElevators.Utilities;

namespace QuantumElevators.Components {
    internal class QuantumCache {
        private static readonly ModLog<QuantumCache> log = new ModLog<QuantumCache>();
        public static int QuantumBlockId { get; private set; } = 0; // TODO: reduce access, maybe move to another component
        // TODO: add private, static, readonly data structure
        // TODO: use DIRECT LOOKUPS ONLY, or this dict WILL NOT BE THREAD-SAFE!

        internal static void OnGameStartDone() {
            GameManager.Instance.World.ChunkCache.OnBlockChangedDelegates += OnBlockChanged;
        }

        private static void OnBlockChanged(Vector3i pos, BlockValue bvOld, sbyte densOld, long texOld, BlockValue bvNew) {
            log.Debug($"OnBlockChanged => pos:{pos}, bvOld:{bvOld}, densOld:{densOld}, texOld:{texOld}, bvNew:{bvNew}");
            if (BlockValue.Air.Block.blockID == bvOld.Block.blockID) {
                OnBlockPlaced(pos, bvNew);
            } else if (BlockValue.Air.Block.blockID == bvNew.Block.blockID) {
                OnBlockDestroyed(pos, bvOld);
            }
        }

        private static void OnBlockPlaced(Vector3i pos, BlockValue blockValue) {
            log.Debug($"OnBlockPlaced => {blockValue.Block.GetBlockName()} was just placed at {pos}");
            if (blockValue.Block.blockID == TransportationServices.SecureQuantumBlockId) {
                // TODO: possibly use harmony to modify BlockPlayerSign.GetBlockActivationCommands on Post (remove/shift out command at index zero
            }
        }

        private static void OnBlockDestroyed(Vector3i pos, BlockValue blockValue) {
            log.Debug($"OnBlockDestroyed => {blockValue.Block.GetBlockName()} was just destroyed at {pos}");

        }
    }
}
