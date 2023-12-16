using DynamicData.Kernel;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;

namespace VRMiningPatcher
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline
                .Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "VRMiningPatcher.esp")
                .Run(args);
        }

        private static FormLink<ISoundGetter> PickaxeSound = new FormLink<ISoundGetter>(
            ModKey.FromNameAndExtension("RealisticMiningAndChoppingVR.esp").MakeFormKey(0x001856)
        );

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            foreach (
                IActivatorGetter Activator in state
                    .LoadOrder
                    .PriorityOrder
                    .Activator()
                    .WinningOverrides()
            )
            {
                // Sanity checks to skip unnecessary processing
                if (
                    Activator.VirtualMachineAdapter == null
                    || !Activator.VirtualMachineAdapter.Scripts.Any(e => e.Name == "MineOreScript")
                )
                    continue;

                Console.WriteLine($"Found {Activator.EditorID} ...");

                // Create a new COBJ record with the modified conditions
                IActivatorGetter modifiedACTI = state
                    .PatchMod
                    .Activators
                    .GetOrAddAsOverride(Activator);

                // Sanity checks to skip unnecessary processing
                if (modifiedACTI.VirtualMachineAdapter == null)
                    continue;

                var Script = modifiedACTI.VirtualMachineAdapter.Scripts[
                    modifiedACTI
                        .VirtualMachineAdapter
                        .Scripts
                        .FindIndex<IScriptEntryGetter, string>(e => e.Name == "MineOreScript")
                ];

                var NewScript = Script.DeepCopy();
                NewScript.Name = "VR_MineOreScript";

                NewScript
                    .Properties
                    .Add(
                        new ScriptObjectProperty
                        {
                            Name = "PlayerPickAxeStrikeSND",
                            Flags = ScriptProperty.Flag.Edited,
                            Object = PickaxeSound
                        }
                    );

                NewScript
                    .Properties
                    .Add(
                        new ScriptIntProperty
                        {
                            Name = "AttackStrikesBeforeCollection",
                            Flags = ScriptProperty.Flag.Edited,
                            Data = 3
                        }
                    );

                // This doesn't actually work
                modifiedACTI.VirtualMachineAdapter.Scripts.AsList().Remove(Script);
                modifiedACTI.VirtualMachineAdapter.Scripts.Append(NewScript);
            }
        }
    }
}
