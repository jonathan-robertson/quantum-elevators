using QuantumElevator.Components;
using QuantumElevator.Utilities;
using System;

namespace QuantumElevator {
    public class ModAPI : IModApi {
        private static readonly ModLog<ModAPI> log = new ModLog<ModAPI>();
        public void InitMod(Mod _modInstance) {
            try {
                ModEvents.GameStartDone.RegisterHandler(OnGameStartDone);
                ModEvents.GameUpdate.RegisterHandler(OnGameUpdate);
            } catch (Exception e) {
                log.Error("Error on InitMod", e);
            }
        }

        private void OnGameStartDone() {
            try {
                //QuantumCache.OnGameStartDone();
                TransportationServices.OnGameStartDone();
            } catch (Exception e) {
                log.Error("Error OnGameStartDone", e);
            }
        }

        private void OnGameUpdate() {
            try {
                TransportationServices.OnGameUpdate();
            } catch (Exception e) {
                log.Error("Error OnGameUpdate", e);
            }
        }
    }
}
