using HarmonyLib;
using System;

namespace QuantumElevators
{
    [HarmonyPatch(typeof(WorldStaticData), "LoadBlocks")]
    internal class GameManager_StartAsServer_Patches
    {
        private static readonly ModLog<GameManager_StartAsServer_Patches> _log = new ModLog<GameManager_StartAsServer_Patches>();

        public static void Postfix()
        {
            try
            {
                _log.Info("Attempting to load block IDs for QuantumElevators.");
                ModApi.SecureQuantumBlockId = Block.nameIdMapping.GetIdForName("quantumElevatorBlockSecure");
                ModApi.PortableQuantumBlockId = Block.nameIdMapping.GetIdForName("quantumElevatorBlockPortable");
                _log.Info($"PortableQuantumBlockId={ModApi.PortableQuantumBlockId}; SecureQuantumBlockId={ModApi.SecureQuantumBlockId}");
            }
            catch (Exception e)
            {
                _log.Error("Failure on GameManager_StartAsServer_Patches.Postfix", e);
            }
        }
    }
}
