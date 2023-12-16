using DynamicData.Kernel;
using Mutagen.Bethesda;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Assets;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Skyrim.Assets;
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

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            SoundDescriptor PickaxeSound = state.PatchMod.SoundDescriptors.AddNew();
            SoundMarker PickaxeMarker = state.PatchMod.SoundMarkers.AddNew();

            PickaxeSound.EditorID = "PlayerPickAxeSD";
            PickaxeSound.Type = SoundDescriptor.DescriptorType.Standard;
            PickaxeSound.Category = new FormLinkNullable<ISoundCategoryGetter>(
                Skyrim.SoundCategory.AudioCategorySFX.FormKey
            );
            PickaxeSound
                .SoundFiles
                .Add(
                    new AssetLink<SkyrimSoundAssetType>
                    {
                        RawPath = "Data\\Sound\\FX\\NPC\\Human\\PickAxe\\NPC_Human_PickAxe_01.wav"
                    }
                );
            PickaxeSound
                .SoundFiles
                .Add(
                    new AssetLink<SkyrimSoundAssetType>
                    {
                        RawPath = "Data\\Sound\\FX\\NPC\\Human\\PickAxe\\NPC_Human_PickAxe_02.wav"
                    }
                );
            PickaxeSound
                .SoundFiles
                .Add(
                    new AssetLink<SkyrimSoundAssetType>
                    {
                        RawPath = "Data\\Sound\\FX\\NPC\\Human\\PickAxe\\NPC_Human_PickAxe_03.wav"
                    }
                );
            PickaxeSound
                .SoundFiles
                .Add(
                    new AssetLink<SkyrimSoundAssetType>
                    {
                        RawPath = "Data\\Sound\\FX\\NPC\\Human\\PickAxe\\NPC_Human_PickAxe_05.wav"
                    }
                );
            PickaxeSound.OutputModel = new FormLinkNullable<ISoundOutputModelGetter>(
                Skyrim.SoundOutputModel.SOMStereoRad02100_verb.FormKey
            );
            ;
            PickaxeSound.PercentFrequencyVariance = new Percent(-1);
            PickaxeSound.Priority = 127;
            PickaxeSound.Variance = 3;
            PickaxeSound.StaticAttenuation = 5.29f;
            PickaxeSound.LoopAndRumble = new SoundLoopAndRumble
            {
                Loop = SoundDescriptor.LoopType.None,
                RumbleValues = 0,
                Unknown = 01
            };

            PickaxeMarker.EditorID = "PlayerPickaxeMarker";
            PickaxeMarker.SoundDescriptor = new FormLinkNullable<ISoundDescriptorGetter>(
                PickaxeSound
            );

            Console.WriteLine("Looking for activators to patch...");

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
                            Name = "PlayerPickaxeMarkerSND",
                            Flags = ScriptProperty.Flag.Edited,
                            Object = new FormLinkNullable<ISoundMarkerGetter>(PickaxeMarker)
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

            Console.WriteLine("Looking for furniture to patch...");

            foreach (
                IFurnitureGetter Activator in state
                    .LoadOrder
                    .PriorityOrder
                    .Furniture()
                    .WinningOverrides()
            )
            {
                // Sanity checks to skip unnecessary processing
                if (
                    Activator.VirtualMachineAdapter == null
                    || !Activator
                        .VirtualMachineAdapter
                        .Scripts
                        .Any(e => e.Name == "MineOreFurnitureScript")
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
                    .FindIndex<IScriptEntryGetter, string>(e => e.Name == "MineOreFurnitureScript");
                var Script = modifiedACTI.VirtualMachineAdapter.Scripts[index];

                // Swap the script name and add the necessary properties
                Script.Name = "VR_MineOreFurnitureScript";

                // The mod added some defaults so we'll add defaults too
                if (!Script.Properties.Any(e => e.Name == "CurrentFollowerFaction"))
                    Script
                        .Properties
                        .Add(
                            new ScriptObjectProperty
                            {
                                Name = "CurrentFollowerFaction",
                                Flags = ScriptProperty.Flag.Edited,
                                Object = Skyrim.Faction.CurrentFollowerFaction
                            }
                        );

                if (!Script.Properties.Any(e => e.Name == "MiningSkillIncrement"))
                    Script
                        .Properties
                        .Add(
                            new ScriptObjectProperty
                            {
                                Name = "MiningSkillIncrement",
                                Flags = ScriptProperty.Flag.Edited,
                                Object = Skyrim.Global.MiningSkillIncrement
                            }
                        );

                // Swap existing script with the modified script
                modifiedACTI.VirtualMachineAdapter.Scripts[index] = Script;

                // Apply changes
                state.PatchMod.Furniture.Set(modifiedACTI);
            }
        }
    }
}
