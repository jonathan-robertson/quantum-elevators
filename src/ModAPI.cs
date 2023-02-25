using HarmonyLib;
using System;
using System.Reflection;

namespace QuantumElevators
{
    public class ModApi : IModApi
    {
        private static readonly ModLog<ModApi> _log = new ModLog<ModApi>();

        public static bool DebugMode { get; set; } = false;
        public static bool IsServer { get; private set; } = false;
        internal static int SecureQuantumBlockId { get; set; } = 0;
        internal static int PortableQuantumBlockId { get; set; } = 0;

        public void InitMod(Mod _modInstance)
        {
            var harmony = new Harmony(GetType().ToString());
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            _log.Info("loaded");

            ModEvents.GameStartDone.RegisterHandler(OnGameStartDone);
        }

        private void OnGameStartDone()
        {
            try
            {
                IsServer = SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer;
                if (IsServer)
                {
                    _log.Info("QuantumElevators recognizes you as the host, so it will begin managing player positions.");
                }
                else
                {
                    _log.Warn("QuantumElevators recognizes you as a client, so this locally installed mod will be inactive until you host a game.");
                }
            }
            catch (Exception e)
            {
                _log.Error("Error OnGameStartDone", e);
            }
        }
    }
}
