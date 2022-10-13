using QuantumElevator.Components;
using QuantumElevator.Utilities;
using System;

namespace QuantumElevator {
    public class ModAPI : IModApi {
        private static readonly ModLog log = new ModLog(typeof(ModAPI));
        public void InitMod(Mod _modInstance) {
            try {
                ModEvents.GameStartDone.RegisterHandler(QuantumCache.OnGameStartDone);
                ModEvents.GameStartDone.RegisterHandler(TransportationServices.OnGameStartDone);
                ModEvents.GameUpdate.RegisterHandler(TransportationServices.OnGameUpdate);
            } catch (Exception e) {
                log.Error("Error on InitMod", e);
            }
        }
    }
}
