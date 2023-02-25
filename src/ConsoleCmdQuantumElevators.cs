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
                { "push <x> <y> <z>", "recursively push entities 1 block at a time to make room; use float values from 'lp' command to let system calculate correct block coordinates for you" },
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
            return "Admin commands for working with Quantum Elevators";
        }

        public override string GetHelp()
        {
            return _help;
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            if (_params.Count == 0)
            {
                ModApi.DebugMode = !ModApi.DebugMode;
                SdtdConsole.Instance.Output($"Debug logging is now {(ModApi.DebugMode ? "enabled" : "disabled")}.");
                return;
            }
            switch (_params[0])
            {
                case "push":
                    if (_params.Count != 4)
                    {
                        SdtdConsole.Instance.Output("Wrong number of arguments, expected 4, found " + _params.Count.ToString() + ".");
                        return;
                    }
                    if (!float.TryParse(_params[1], out var x)
                        || !float.TryParse(_params[2], out var y)
                        || !float.TryParse(_params[3], out var z))
                    {
                        SdtdConsole.Instance.Output($"Wrong type of argument, provided string could not be converted to float: {_params[1]}, {_params[2]}, {_params[3]}.");
                        return;
                    }
                    SdtdConsole.Instance.Output($"Pushing...");
                    // note: one can use integer conversion to get the bottom-left of a positive coordinate,
                    // but not for the bottom-left of a negative coordinate... these have to be rounded "down" to the *next* nearest number.
                    // ... yes, this is how block coords are identified lol
                    _ = CoreLogic.Push(new Vector3i(
                        x % 1 < 0 ? x - 1 : x,
                        y % 1 < 0 ? y - 1 : y,
                        z % 1 < 0 ? z - 1 : z));
                    SdtdConsole.Instance.Output($"Pushing completed.");
                    return;
            }

            SdtdConsole.Instance.Output($"Invalid number of parameters; use 'help {_commands[0]}' to verify valid options.");
        }
    }
}
