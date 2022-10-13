using QuantumElevator.Services;
using QuantumElevator.Utilities;

namespace QuantumElevator {
    public class ModAPI : IModApi {
        private static readonly ModLog log = new ModLog(typeof(ModAPI));

        public void InitMod(Mod _modInstance) {
            ModEvents.GameStartDone.RegisterHandler(OnGameStartDone);
            ModEvents.GameUpdate.RegisterHandler(PlayerRequestService.OnGameUpdate);
        }

        private void OnGameStartDone() {
            PlayerRequestService.QuantumBlockId = Block.nameIdMapping.GetIdForName("quantumElevatorBlock");
        }
    }
}
