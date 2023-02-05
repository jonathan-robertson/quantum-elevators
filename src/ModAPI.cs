using QuantumElevators.Components;
using QuantumElevators.Utilities;
using System;

namespace QuantumElevators
{
    public class ModApi : IModApi
    {
        private static readonly ModLog<ModApi> _log = new ModLog<ModApi>();

        public static bool DebugMode { get; set; } = false;

        public void InitMod(Mod _modInstance)
        {
            try
            {
                ModEvents.GameStartDone.RegisterHandler(OnGameStartDone);
                ModEvents.GameUpdate.RegisterHandler(OnGameUpdate);
            }
            catch (Exception e)
            {
                _log.Error("Error on InitMod", e);
            }
        }

        private void OnGameStartDone()
        {
            try
            {
                //QuantumCache.OnGameStartDone();
                TransportationServices.OnGameStartDone();
            }
            catch (Exception e)
            {
                _log.Error("Error OnGameStartDone", e);
            }
        }

        private void OnGameUpdate()
        {
            try
            {
                TransportationServices.OnGameUpdate();
            }
            catch (Exception e)
            {
                _log.Error("Error OnGameUpdate", e);
            }
        }
    }
}
