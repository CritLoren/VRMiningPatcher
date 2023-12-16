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

        private static FormLink<ISoundGetter> PickaxeSound =
            new(
                ModKey
                    .FromNameAndExtension("RealisticMiningAndChoppingVR.esp")
                    .MakeFormKey(0x001857)
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

                // Deep copy existing record to edit
                var modifiedACTI = Activator.DeepCopy();
                if (modifiedACTI.VirtualMachineAdapter == null)
                    continue;

                Console.WriteLine($"Found {Activator.EditorID} ...");

                // Get index of script with MineOreScript name
                int index = modifiedACTI
                    .VirtualMachineAdapter
                    .Scripts
                    .FindIndex<IScriptEntryGetter, string>(e => e.Name == "MineOreScript");
                var Script = modifiedACTI.VirtualMachineAdapter.Scripts[index];

                // Swap the script name and add the necessary properties
                Script.Name = "VR_MineOreScript";
                Script
                    .Properties
                    .Add(
                        new ScriptObjectProperty
                        {
                            Name = "PlayerPickAxeStrikeSND",
                            Flags = ScriptProperty.Flag.Edited,
                            Object = PickaxeSound
                        }
                    );
                Script
                    .Properties
                    .Add(
                        new ScriptIntProperty
                        {
                            Name = "AttackStrikesBeforeCollection",
                            Flags = ScriptProperty.Flag.Edited,
                            Data = 3
                        }
                    );

                // Swap existing script with the modified script
                modifiedACTI.VirtualMachineAdapter.Scripts[index] = Script;

                // Apply changes
                state.PatchMod.Activators.Set(modifiedACTI);
            }
        }
    }
}
