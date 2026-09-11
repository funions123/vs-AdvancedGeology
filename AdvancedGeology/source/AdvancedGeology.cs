using System.Text;
using AdvancedGeology.Patches;
using AdvancedGeology.Silver;
using AdvancedGeology.WorldGen;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;

namespace AdvancedGeology
{
    public sealed class AdvancedGeologyModSystem : ModSystem
    {
        private Harmony? harmony;
        private const string ConfigFileName = "advancedgeology.json";
        private AdvancedGeologyConfig config = new();

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);

            try
            {
                AdvancedGeologyConfig? loaded = api.LoadModConfig<AdvancedGeologyConfig>(ConfigFileName);
                if (loaded == null)
                {
                    api.StoreModConfig(config, ConfigFileName);
                }
                else
                {
                    config = loaded;
                }
            }
            catch (Exception exception)
            {
                api.Logger.Warning(
                    "[AdvancedGeology] Could not load {0}; using defaults: {1}",
                    ConfigFileName,
                    exception.Message);
                config = new AdvancedGeologyConfig();
            }

            DepositGeneratorRegistry.RegisterDepositGenerator<LayeredSurfaceDepositGenerator>("disc-layeredsurface");
            DepositGeneratorRegistry.RegisterDepositGenerator<SaltDomeDepositGenerator>("saltdome");
            DepositGeneratorRegistry.RegisterDepositGenerator<ProceduralProspectingDepositGenerator>("procedural-prospecting");
            api.Logger.Notification("[AdvancedGeology] Registered custom deposit generators: disc-layeredsurface, saltdome, procedural-prospecting");


            // Hidden silver grade system only runs when Industrial Story is installed.
            SilverGradeSystem.SetActive(api.ModLoader.IsModEnabled("industrialstory"));
            if (SilverGradeSystem.Active)
            {
                SilverGradeSystem.RegisterIgnoredAttribute();
                api.Logger.Notification("[AdvancedGeology] Industrial Story detected: hidden silver grade system enabled");
            }
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            bool canJewelryEnabled = api.ModLoader.IsModEnabled("canjewelry");
            if (!SilverGradeSystem.Active && !canJewelryEnabled) return;

            harmony = new Harmony("advancedgeology");
            if (SilverGradeSystem.Active) harmony.PatchAll();
            if (canJewelryEnabled)
            {
                CanJewelryAcquisitionPatch.Apply(harmony, api);
                CanJewelryGemCompatibility.Apply(harmony, api);
            }
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Event.InitWorldGenerator(() => VerifyBlockLayerMappings(api), "standard");

            AdvancedGeologyCommands.Register(api);

            if (api.ModLoader.IsModEnabled("canjewelry"))
            {
                CanJewelryGemCompatibility.ScheduleRuntimeVerification(api);
            }

            if (SilverGradeSystem.Active)
            {
                SilverGradeSystem.BuildNoise(api.World.Seed);
            }
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll("advancedgeology");
            base.Dispose();
        }

        public override void AssetsLoaded(ICoreAPI api)
        {
            if (api.ModLoader.IsModEnabled("canjewelry"))
            {
                CanJewelryGemCompatibility.InitializeAndReconcile(api);
            }
            if (api.Side == EnumAppSide.Server)
            {
                SuppressLegacyOreDeposits(api);
            }
        }



        /// <summary>
        /// Enforces exclusive procedural ore generation by disabling every conventional deposit
        /// except standalone gem, rock, soil, and non-generating prospecting registrations. This
        /// also suppresses externally supplied conventional ore definitions before GenDeposits finalizes them.
        /// </summary>
        private static void SuppressLegacyOreDeposits(ICoreAPI api)
        {
            byte[] emptyArray = Encoding.UTF8.GetBytes("[]");
            int suppressedFiles = 0;
            int preservedGemFiles = 0;
            int preservedRockFiles = 0;
            int preservedSoilFiles = 0;
            int preservedProspectingFiles = 0;

            foreach ((AssetLocation location, IAsset asset) in api.Assets.AllAssets.ToArray())
            {
                string path = location.Path.Replace('\\', '/').ToLowerInvariant();
                if (!path.StartsWith("worldgen/deposits/", StringComparison.Ordinal)) continue;

                if (!IsConventionalOreDepositAssetPath(path))
                {
                    string relative = path["worldgen/deposits/".Length..];
                    if (relative.StartsWith("gem/", StringComparison.Ordinal)) preservedGemFiles++;
                    else if (relative.StartsWith("rock/", StringComparison.Ordinal)) preservedRockFiles++;
                    else if (relative.StartsWith("soil/", StringComparison.Ordinal)) preservedSoilFiles++;
                    else if (relative.StartsWith("prospecting/", StringComparison.Ordinal)) preservedProspectingFiles++;
                    continue;
                }

                asset.Data = emptyArray;
                suppressedFiles++;
            }

            api.Logger.Notification(
                "[AdvancedGeology] Disabled {0} conventional ore deposit asset file(s)",
                suppressedFiles);
            api.Logger.Notification(
                "[AdvancedGeology] Preserved conventional cluster and metadata files: {0} gem, {1} rock, {2} soil, {3} prospecting",
                preservedGemFiles,
                preservedRockFiles,
                preservedSoilFiles,
                preservedProspectingFiles);
        }

        public static bool IsConventionalOreDepositAssetPath(string path)
        {
            const string prefix = "worldgen/deposits/";
            string normalized = path.Replace('\\', '/').ToLowerInvariant();
            if (!normalized.StartsWith(prefix, StringComparison.Ordinal)) return false;

            string relative = normalized[prefix.Length..];
            return !relative.StartsWith("gem/", StringComparison.Ordinal)
                && !relative.StartsWith("rock/", StringComparison.Ordinal)
                && !relative.StartsWith("soil/", StringComparison.Ordinal)
                && !relative.StartsWith("prospecting/", StringComparison.Ordinal);
        }


        /// <summary>
        /// Verifies the block layer system has correct rock-to-gravel/sand mappings for our rocks.
        /// </summary>
        private void VerifyBlockLayerMappings(ICoreServerAPI api)
        {
            string[] ourRocks = ["gneiss", "schist", "quartzite", "dolomite", "diorite", "gabbro", "dacite", "rhyolite"];

            BlockLayerConfig config = BlockLayerConfig.GetInstance(api);

            // Check rockstrata variants include our rocks
            HashSet<string> strataRocks = new();
            foreach (var variant in config.RockStrata.Variants)
            {
                strataRocks.Add(variant.BlockCode.Path);
            }
            foreach (string rock in ourRocks)
            {
                string blockcode = "rock-" + rock;
                if (!strataRocks.Contains(blockcode))
                {
                    api.Logger.Warning("[AdvancedGeology] Rock strata missing entry for {0}", blockcode);
                }
            }

            // Check block layer mappings include our rocks
            foreach (var layer in config.Blocklayers)
            {
                if (layer.BlockIdMapping == null) continue;

                foreach (string rock in ourRocks)
                {
                    Block rockBlock = api.World.GetBlock(new AssetLocation("rock-" + rock));
                    if (rockBlock == null)
                    {
                        api.Logger.Warning("[AdvancedGeology] Block 'rock-{0}' not found!", rock);
                        continue;
                    }

                    if (!layer.BlockIdMapping.ContainsKey(rockBlock.BlockId))
                    {
                        api.Logger.Warning("[AdvancedGeology] Block layer '{0}' missing mapping for rock-{1} (blockId={2})",
                            layer.Name, rock, rockBlock.BlockId);
                    }
                }

                // Only check the first {rocktype} layer as they all use the same mapping logic
                break;
            }

            api.Logger.Notification("[AdvancedGeology] Block layer mapping verification complete");
        }
    }
}
