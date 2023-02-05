using System.Collections.Generic;
using System.Linq;

namespace QuantumElevators
{
    internal class ConsoleCmdQuantumElevators : ConsoleCmdAbstract
    {
        private static readonly string[] _commands = new string[] {
            "quantumelevators",
            "qe"
        };
        private readonly string _help;

        public ConsoleCmdQuantumElevators()
        {
            var dict = new Dictionary<string, string>() {
                { "", "enable/disable debug logging for this mod" },
            };

            var i = 1; var j = 1;
            _help = $"Usage:\n  {string.Join("\n  ", dict.Keys.Select(command => $"{i++}. {GetCommands()[0]} {command}").ToList())}\nDescription Overview\n{string.Join("\n", dict.Values.Select(description => $"{j++}. {description}").ToList())}";
        }

        public override string[] GetCommands()
        {
            return _commands;
        }

        public override string GetDescription()
        {
            return "Enable/Disable debug logging for Quantum Elevators";
        }

        public override string GetHelp()
        {
            return _help;
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            if (_params.Count > 0)
            {
                SdtdConsole.Instance.Output("This command doesn't receive any parameters.");
                return;
            }

            ModApi.DebugMode = !ModApi.DebugMode;
            SdtdConsole.Instance.Output($"Debug logging is now {(ModApi.DebugMode ? "enabled" : "disabled")}.");
        }
    }
}
