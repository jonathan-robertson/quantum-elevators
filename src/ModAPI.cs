using HarmonyLib;
using System;
using System.Reflection;

namespace QuantumElevators
{
    public class ModApi : IModApi
    {
        private static readonly ModLog<ModApi> _log = new ModLog<ModApi>();

        public static bool DebugMode { get; set; } = false;
        internal static int SecureQuantumBlockId { get; set; } = 0;
        internal static int PortableQuantumBlockId { get; set; } = 0;

        public void InitMod(Mod _modInstance)
        {
            ModEvents.GameStartDone.RegisterHandler(OnGameStartDone);
        }

        private void OnGameStartDone()
        {
            try
            {
                if (GameManager.Instance == null
                    || GameManager.Instance.World == null
                    || GameManager.Instance.World.IsRemote())
                {
                    // avoid loading mod if connecting to a remote world (let this mod on the host's side control this functionality)
                    _log.Warn("QuantumElevators is a host-side mod and is disabled when connecting to another player or dedicated server's world. To enjoy these features in remote worlds, the host will need to have this mod installed.");
                    return;
                }

                SecureQuantumBlockId = Block.nameIdMapping.GetIdForName("quantumElevatorBlockSecure");
                PortableQuantumBlockId = Block.nameIdMapping.GetIdForName("quantumElevatorBlockPortable");

                var harmony = new Harmony(GetType().ToString());
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                _log.Info("loaded");
            }
            catch (Exception e)
            {
                _log.Error("Error OnGameStartDone", e);
            }
        }
    }
}
