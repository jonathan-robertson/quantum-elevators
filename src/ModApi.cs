﻿using HarmonyLib;
using System;
using System.Reflection;

namespace QuantumElevators
{
    public class ModApi : IModApi
    {
        private static readonly ModLog<ModApi> _log = new ModLog<ModApi>();

        private static readonly string ModMaintainer = "@kanaverum";
        private static readonly string SupportLink = "https://discord.gg/hYa2sNHXya";

        public static bool DebugMode { get; set; } = false;
        public static bool IsServer { get; private set; } = false;
        internal static int SecureQuantumBlockId { get; set; } = 0;
        internal static int PortableQuantumBlockId { get; set; } = 0;

        public void InitMod(Mod _modInstance)
        {
            var harmony = new Harmony(GetType().ToString());
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            _log.Info("loaded");

            ModEvents.GameAwake.RegisterHandler(OnGameAwake);
            ModEvents.GameStartDone.RegisterHandler(OnGameStartDone);
        }

        private const string DLL_VERSION = "test-version";  // note: this is automatically updated to a version like 5.1.0
        private const string BUILD_TARGET = "test-target";  // note: this is automatically updated to a build version like 1.3

        private void OnGameAwake()
        {
            try
            {
                _log.Info($"Quantum Elevators DLL Version {DLL_VERSION} build for 7DTD {BUILD_TARGET}");
            }
            catch (Exception e)
            {
                _log.Error("OnGameAwake", e);
            }
        }

        private void OnGameStartDone()
        {
            try
            {
                IsServer = SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer;
                if (IsServer)
                {
                    _log.Info("QuantumElevators recognizes you as the host, so it will begin managing player positions.");

                    _log.Info("Attempting to load block IDs for QuantumElevators.");
                    var quantumElevatorBlockSecure = Block.GetBlockByName("quantumElevatorBlockSecure");
                    var quantumElevatorBlockPortable = Block.GetBlockByName("quantumElevatorBlockPortable");
                    if (quantumElevatorBlockSecure != null && quantumElevatorBlockPortable != null)
                    {
                        SecureQuantumBlockId = quantumElevatorBlockSecure.blockID;
                        PortableQuantumBlockId = quantumElevatorBlockPortable.blockID;
                        _log.Info($"PortableQuantumBlockId={PortableQuantumBlockId}; SecureQuantumBlockId={SecureQuantumBlockId}");
                    }
                    else
                    {
                        _log.Error($"PortableQuantumBlockId=FAILURE; SecureQuantumBlockId=FAILURE; restarting the server will be necessary to fix this - please reach out to the mod maintainer {ModMaintainer} via {SupportLink}");
                    }
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
