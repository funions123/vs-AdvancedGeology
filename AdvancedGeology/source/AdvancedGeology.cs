using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdvancedGeology.Patches;
using AdvancedGeology.Silver;
using AdvancedGeology.WorldGen;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;

namespace AdvancedGeology
{
    public sealed class AdvancedGeologyModSystem : ModSystem
    {
        private Harmony? harmony;

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);

            DepositGeneratorRegistry.RegisterDepositGenerator<LayeredSurfaceDepositGenerator>("disc-layeredsurface");
            DepositGeneratorRegistry.RegisterDepositGenerator<SaltDomeDepositGenerator>("saltdome");
            api.Logger.Notification("[AdvancedGeology] Registered custom deposit generators: disc-layeredsurface, saltdome");

            if (api.ModLoader.IsModEnabled("interestingoregen"))
            {
                IogChildDepositPatch.Apply(api);
            }

            // Hidden silver grade system only runs when AdvancedMetallurgy is installed.
            SilverGradeSystem.SetActive(api.ModLoader.IsModEnabled("advancedmetallurgy"));
            if (SilverGradeSystem.Active)
            {
                SilverGradeSystem.RegisterIgnoredAttribute();
                api.Logger.Notification("[AdvancedGeology] AdvancedMetallurgy detected: hidden silver grade system enabled");
            }
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            if (SilverGradeSystem.Active)
            {
                harmony = new Harmony("advancedgeology.silvergrade");
                harmony.PatchAll();
            }
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Event.InitWorldGenerator(() => VerifyBlockLayerMappings(api), "standard");

            if (SilverGradeSystem.Active)
            {
                SilverGradeSystem.BuildNoise(api.World.Seed);
            }
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll("advancedgeology.silvergrade");
            base.Dispose();
        }

        public override void AssetsLoaded(ICoreAPI api)
        {
            if (api.ModLoader.IsModEnabled("interestingoregen"))
            {
                ClearIogDeposits(api);

                ZeroDepositTries(api, "game:worldgen/deposits/metalore/chalcopyrite.json");
            }

            if (api.ModLoader.IsModEnabled("geoaddons"))
            {
                RemoveGeoAddonsRockOverlaps(api);
                RemoveGeoAddonsOreOverlaps(api);
            }

            if (SilverGradeSystem.Active)
            {
                DisableNativeSilverOreGen(api);
            }
        }

        /// Below: unhinged load order manipulation shenanigans

        /// <summary>
        /// Strips native-silver and freibergite childDeposits from galena and tetrahedrite so
        /// silver only comes out via cupellation.
        /// </summary>
        private void DisableNativeSilverOreGen(ICoreAPI api)
        {
            string[] depositFiles = [
                "game:worldgen/deposits/metalore/galena.json",
                "game:worldgen/deposits/metalore/iog-galena.json",
                "game:worldgen/deposits/metalore/tetrahedrite.json",
                "game:worldgen/deposits/metalore/iog-tetrahedrite.json"
            ];
            string[] silverMarkers = ["nativesilver", "freibergite"];
            int removed = 0;

            foreach (string path in depositFiles)
            {
                IAsset? asset = api.Assets.TryGet(new AssetLocation(path));
                if (asset == null) continue;

                JArray root;
                try { root = JArray.Parse(asset.ToText()); }
                catch { continue; }

                bool changed = false;
                foreach (JObject deposit in root.OfType<JObject>())
                {
                    if (deposit["childDeposits"] is not JArray children) continue;

                    for (int i = children.Count - 1; i >= 0; i--)
                    {
                        string entry = children[i].ToString();
                        if (silverMarkers.Any(m => entry.Contains(m)))
                        {
                            children.RemoveAt(i);
                            removed++;
                            changed = true;
                        }
                    }
                }

                if (changed) asset.Data = Encoding.UTF8.GetBytes(root.ToString());
            }

            api.Logger.Notification("[AdvancedGeology] AdvancedMetallurgy detected: removed {0} native-silver childDeposits", removed);
        }

        /// <summary>
        /// Removes GeoAddons' overlapping rock entries from worldproperties.
        /// </summary>
        private void RemoveGeoAddonsRockOverlaps(ICoreAPI api)
        {
            string[] removeEntirely = ["dolostone", "Gabbro"];
            string[] deduplicateCodes = ["quartzite", "gneiss", "schist", "diorite", "rhyolite"];

            int removed = 0;
            removed += DeduplicateWorldPropertyVariants(api, "game:worldproperties/block/rock.json", "Code", removeEntirely, deduplicateCodes);
            removed += DeduplicateWorldPropertyVariants(api, "game:worldproperties/block/rockwithdeposit.json", "Code", removeEntirely, deduplicateCodes);

            string[] removeEntirelyStrata = ["rock-dolostone"];
            string[] deduplicateStrata = ["rock-quartzite", "rock-gneiss", "rock-schist", "rock-diorite", "rock-gabbro", "rock-rhyolite"];
            removed += DeduplicateWorldPropertyVariants(api, "game:worldgen/rockstrata.json", "blockcode", removeEntirelyStrata, deduplicateStrata);

            // Fanned cobblestone: geoaddons adds "dolostone" and duplicates of our rocks
            removed += CleanupFannedCobblestoneVariants(api);

            // Recipes: geoaddons adds dolostone to allowedVariants and as fanned cobblestone recipe
            removed += CleanupDolostonRecipes(api);

            // Quern: geoaddons replaces the entire states array, dropping our rocks
            removed += RestoreQuernVariants(api);

            api.Logger.Notification("[AdvancedGeology] GeoAddons compat: removed {0} overlapping rock entries", removed);
        }

        /// <summary>
        /// Removes GeoAddons' chalcopyrite/tetrahedrite ore overlaps; deduplicates worldproperty
        /// variants and allowedVariants, restores our chalcopyrite deposit (geoaddons overrides
        /// it via same filename), and removes geoaddons' double-spawning tetrahedritewithsilver.
        /// </summary>
        private void RemoveGeoAddonsOreOverlaps(ICoreAPI api)
        {
            int removed = 0;

            string[] oreDedup = ["chalcopyrite", "tetrahedrite"];
            removed += DeduplicateWorldPropertyVariants(api, "game:worldproperties/block/ore-graded.json", "Code", [], oreDedup);
            removed += DeduplicateWorldPropertyVariants(api, "game:worldproperties/block/ore-nugget.json", "Code", [], oreDedup);

            // Clean allowedVariants: deduplicate overlapping entries and remove geoaddons-only rock entries
            // Only keep ore variants for rocks that our deposits actually use
            string[] chalcopyriteRocks = ["sandstone", "shale", "limestone", "andesite", "granite", "peridotite", "gneiss", "schist", "diorite", "gabbro", "dacite", "rhyolite"];
            string[] tetrahedriteRocks = ["phyllite", "andesite", "quartzite", "schist", "dacite"];

            string[] allowedVariantFiles = [
                "game:blocktypes/stone/ore-graded.json",
                "game:blocktypes/stone/looseores.json",
                "game:itemtypes/resource/ore-graded.json",
                "game:itemtypes/resource/crystalizedore-graded.json"
            ];
            foreach (string file in allowedVariantFiles)
            {
                removed += CleanupOreAllowedVariants(api, file, "chalcopyrite", chalcopyriteRocks);
                removed += CleanupOreAllowedVariants(api, file, "tetrahedrite", tetrahedriteRocks);
            }

            // Restore our chalcopyrite deposit (geoaddons overrides it since same filename, they load second)
            if (RestoreAssetFromModOrigin(api, "game:worldgen/deposits/metalore/chalcopyrite.json"))
            {
                removed++;

                // The restore undoes the zeroing done in AssetsLoaded, so re-apply it here when IOG is installed
                if (api.ModLoader.IsModEnabled("interestingoregen"))
                {
                    ZeroDepositTries(api, "game:worldgen/deposits/metalore/chalcopyrite.json");
                }
            }

            // Remove geoaddons' tetrahedritewithsilver deposit (different filename causes double spawning)
            if (api.Assets.AllAssets.Remove(new AssetLocation("game", "worldgen/deposits/metalore/tetrahedritewithsilver.json")))
            {
                removed++;
            }

            if (api.ModLoader.IsModEnabled("interestingoregen"))
            {
                InjectChildDepositsFromConfig(api, "advancedgeology:config/iog-geoaddons-childdeposits.json");
            }
            else
            {
                InjectChildDepositsFromConfig(api, "advancedgeology:config/geoaddons-childdeposits.json");
            }

            api.Logger.Notification("[AdvancedGeology] GeoAddons compat: removed {0} overlapping ore entries", removed);
        }

        /// <summary>
        /// Empties IOG deposit files that we change. Emptying avoids block-code validation errors from nonexistent ore-rock combinations.
        /// </summary>
        private void ClearIogDeposits(ICoreAPI api)
        {
            // IOG base deposit files in question
            string[] depositPaths = [
                "interestingoregen:worldgen/deposits/metalore/galena.json",
                "interestingoregen:worldgen/deposits/metalore/sphalerite.json",
                "interestingoregen:worldgen/deposits/metalore/bismuthinite.json",
                "interestingoregen:worldgen/deposits/metalore/malachite.json",
                "interestingoregen:worldgen/deposits/metalore/cassiterite.json",
                "interestingoregen:worldgen/deposits/metalore/nativecopper.json",
                "interestingoregen:worldgen/deposits/metalore/pentlandite.json",
                "interestingoregen:worldgen/deposits/metalore/chromite.json",
                "interestingoregen:worldgen/deposits/metalore/ilmenite.json",
                "interestingoregen:worldgen/deposits/metalore/limonite.json",
                "interestingoregen:worldgen/deposits/metalore/hematite.json",
                "interestingoregen:worldgen/deposits/metalore/magnetite.json",
                "interestingoregen:worldgen/deposits/metalore/chalcopyrite.json",
                "interestingoregen:worldgen/deposits/metalore/tetrahedrite.json",
                "interestingoregen:worldgen/deposits/mineralore/sulfur.json",
                "interestingoregen:worldgen/deposits/mineralore/borax.json",
                "interestingoregen:worldgen/deposits/mineralore/coal.json"
            ];

            // GeoAddons-only IOG deposits
            if (api.ModLoader.IsModEnabled("geoaddons"))
            {
                depositPaths = [..depositPaths,
                    "interestingoregen:worldgen/deposits/metalore/azurite.json",
                    "interestingoregen:worldgen/deposits/metalore/cerussite.json",
                    "interestingoregen:worldgen/deposits/metalore/chalcocite.json",
                    "interestingoregen:worldgen/deposits/metalore/franckeite.json",
                    "interestingoregen:worldgen/deposits/metalore/hemimorphite.json",
                    "interestingoregen:worldgen/deposits/metalore/nativeplatinum.json",
                    "interestingoregen:worldgen/deposits/metalore/pyrite.json",
                    "interestingoregen:worldgen/deposits/metalore/smithsonite.json",
                    "interestingoregen:worldgen/deposits/metalore/sperrylite.json",
                    "interestingoregen:worldgen/deposits/metalore/teallite.json",
                    "interestingoregen:worldgen/deposits/metalore/vanadinite.json",
                    "interestingoregen:worldgen/deposits/metalore/wulfenite.json"
                ];
            }

            byte[] emptyArray = Encoding.UTF8.GetBytes("[]");
            int cleared = 0;

            foreach (string path in depositPaths)
            {
                IAsset? asset = api.Assets.TryGet(new AssetLocation(path));
                if (asset == null) continue;

                asset.Data = emptyArray;
                cleared++;
            }

            api.Logger.Notification("[AdvancedGeology] IOG compat: cleared {0} interestingoregen: deposit files", cleared);
        }

        /// <summary>
        /// Sets triesPerChunk to 0 on every entry of a deposit file, disabling it without removing it (keeps handbook/ore variant resolution intact).
        /// </summary>
        private void ZeroDepositTries(ICoreAPI api, string assetPath)
        {
            IAsset? asset = api.Assets.TryGet(new AssetLocation(assetPath));
            if (asset == null) return;

            JArray arr;
            try { arr = JArray.Parse(asset.ToText()); }
            catch { return; }

            foreach (JObject entry in arr.OfType<JObject>())
            {
                entry["triesPerChunk"] = 0;
            }
            asset.Data = Encoding.UTF8.GetBytes(arr.ToString());
        }

        /// <summary>
        /// Removes dolostone and deduplicates rock entries from fanned cobblestone's hardcoded
        /// states list (it's not loaded from worldproperties).
        /// </summary>
        private int CleanupFannedCobblestoneVariants(ICoreAPI api)
        {
            IAsset? asset = api.Assets.TryGet(new AssetLocation("game:blocktypes/stone/cobble/fanned.json"));
            if (asset == null) return 0;

            JObject root;
            try { root = JObject.Parse(asset.ToText()); }
            catch { return 0; }

            JArray? variantGroups = root["variantgroups"] as JArray;
            if (variantGroups == null || variantGroups.Count == 0) return 0;

            JArray? states = variantGroups[0]?["states"] as JArray;
            if (states == null) return 0;

            HashSet<string> toRemove = new(["dolostone"]);
            HashSet<string> toDedup = new(["quartzite", "gneiss", "schist", "diorite", "gabbro", "rhyolite"]);
            HashSet<string> seen = new();
            int removed = 0;

            for (int i = states.Count - 1; i >= 0; i--)
            {
                string? val = states[i]?.ToString();
                if (val != null && toRemove.Contains(val))
                {
                    states.RemoveAt(i);
                    removed++;
                }
            }

            // Deduplicate overlapping rocks (keep first occurrence = ours)
            for (int i = 0; i < states.Count; i++)
            {
                string? val = states[i]?.ToString();
                if (val == null) continue;

                if (toDedup.Contains(val) && !seen.Add(val))
                {
                    states.RemoveAt(i);
                    i--;
                    removed++;
                }
            }

            if (removed > 0)
            {
                asset.Data = Encoding.UTF8.GetBytes(root.ToString());
            }

            return removed;
        }

        /// <summary>
        /// Re-adds our rocks to the quern block and recipe, which GeoAddons drops by replacing
        /// the states array entirely.
        /// </summary>
        private int RestoreQuernVariants(ICoreAPI api)
        {
            string[] ourRocks = ["rhyolite", "diorite", "dacite", "gabbro", "gneiss", "schist", "quartzite"];
            int added = 0;

            added += EnsureValuesInArray(api, "game:blocktypes/stone/quern.json",
                token => (token as JObject)?["variantgroups"]?[0]?["states"] as JArray, ourRocks);

            added += EnsureValuesInArray(api, "game:recipes/grid/quern.json",
                token => (token as JObject)?["ingredients"]?["R"]?["allowedVariants"] as JArray, ourRocks);

            return added;
        }

        /// <summary>
        /// Deduplicates a JArray and adds any missing required values.
        /// </summary>
        private int EnsureValuesInArray(ICoreAPI api, string assetPath, System.Func<JToken, JArray?> arraySelector, string[] values)
        {
            IAsset? asset = api.Assets.TryGet(new AssetLocation(assetPath));
            if (asset == null) return 0;

            JToken root;
            try { root = JToken.Parse(asset.ToText()); }
            catch { return 0; }

            JArray? arr = arraySelector(root);
            if (arr == null) return 0;

            int changed = 0;

            HashSet<string> seen = new();
            for (int i = arr.Count - 1; i >= 0; i--)
            {
                if (!seen.Add(arr[i].ToString()))
                {
                    arr.RemoveAt(i);
                    changed++;
                }
            }

            foreach (string val in values)
            {
                if (seen.Add(val))
                {
                    arr.Add(val);
                    changed++;
                }
            }

            if (changed > 0)
            {
                asset.Data = Encoding.UTF8.GetBytes(root.ToString());
            }

            return changed;
        }

        /// <summary>
        /// Removes dolostone from recipe files: drops recipes that output dolostone blocks and
        /// cleans "dolostone" from ingredient allowedVariants arrays.
        /// </summary>
        private int CleanupDolostonRecipes(ICoreAPI api)
        {
            string[] recipeFiles = [
                "game:recipes/grid/cobblestone.json",
                "game:recipes/grid/drystone.json",
                "game:recipes/grid/drystonefence.json",
                "game:recipes/grid/polishedrock.json",
                "game:recipes/grid/stonebrick.json",
                "game:recipes/grid/stonebricks.json",
                "game:recipes/grid/slabs/cobbleslabs.json",
                "game:recipes/grid/slabs/stonebrickslab.json",
                "game:recipes/grid/slabs/polishedrockslab.json",
                "game:recipes/grid/stairs/cobblestairs.json",
                "game:recipes/grid/stairs/stonebrickstairs.json",
                "game:recipes/barrel/mortar.json"
            ];

            int removed = 0;
            foreach (string file in recipeFiles)
            {
                removed += CleanupDolostonFromRecipeFile(api, file);
            }
            return removed;
        }

        private int CleanupDolostonFromRecipeFile(ICoreAPI api, string assetPath)
        {
            IAsset? asset = api.Assets.TryGet(new AssetLocation(assetPath));
            if (asset == null) return 0;

            JToken root;
            try { root = JToken.Parse(asset.ToText()); }
            catch { return 0; }

            int removed = 0;

            // Recipe files are JSON arrays of recipe objects
            if (root is JArray recipes)
            {
                for (int i = recipes.Count - 1; i >= 0; i--)
                {
                    string? outputCode = recipes[i]?["output"]?["code"]?.ToString();
                    if (outputCode != null && outputCode.Contains("dolostone"))
                    {
                        recipes.RemoveAt(i);
                        removed++;
                    }
                }
            }

            removed += RemoveValueFromArrays(root, "allowedVariants", "dolostone");

            if (removed > 0)
            {
                asset.Data = Encoding.UTF8.GetBytes(root.ToString());
            }

            return removed;
        }

        /// <summary>
        /// Recursively removes a value from all JArrays with the given property name.
        /// </summary>
        private int RemoveValueFromArrays(JToken token, string arrayName, string value)
        {
            int removed = 0;

            if (token is JObject obj)
            {
                foreach (var prop in obj.Properties().ToList())
                {
                    if (prop.Name == arrayName && prop.Value is JArray arr)
                    {
                        for (int i = arr.Count - 1; i >= 0; i--)
                        {
                            if (arr[i]?.ToString() == value)
                            {
                                arr.RemoveAt(i);
                                removed++;
                            }
                        }
                    }
                    else
                    {
                        removed += RemoveValueFromArrays(prop.Value, arrayName, value);
                    }
                }
            }
            else if (token is JArray array)
            {
                foreach (var item in array)
                {
                    removed += RemoveValueFromArrays(item, arrayName, value);
                }
            }

            return removed;
        }

        /// <summary>
        /// Removes and deduplicates allowedVariants entries for an ore whose rock isn't whitelisted,
        /// clearing geoaddons-only variants that would bloat the handbook/creative menu.
        /// </summary>
        private int CleanupOreAllowedVariants(ICoreAPI api, string assetPath, string oreName, string[] validRocks)
        {
            IAsset? asset = api.Assets.TryGet(new AssetLocation(assetPath));
            if (asset == null) return 0;

            JObject root;
            try { root = JObject.Parse(asset.ToText()); }
            catch { return 0; }

            JArray? variants = root["allowedVariants"] as JArray;
            if (variants == null) return 0;

            HashSet<string> validRockSet = new(validRocks);
            HashSet<string> seen = new();
            string oreSplitter = "-" + oreName + "-";
            int removed = 0;

            for (int i = 0; i < variants.Count; i++)
            {
                string? val = variants[i]?.ToString();
                if (val == null || !val.Contains(oreName)) continue;

                // Extract rock name from variant string
                // Formats: ore-poor-chalcopyrite-{rock}, looseores-chalcopyrite-{rock}-free,
                //          crystalizedore-poor-chalcopyrite-{rock}
                int splitIdx = val.IndexOf(oreSplitter);
                if (splitIdx < 0) continue;

                string rockPart = val[(splitIdx + oreSplitter.Length)..];
                if (rockPart.EndsWith("-free"))
                    rockPart = rockPart[..^5];

                if (!validRockSet.Contains(rockPart) || !seen.Add(val))
                {
                    variants.RemoveAt(i);
                    i--;
                    removed++;
                }
            }

            if (removed > 0)
            {
                asset.Data = Encoding.UTF8.GetBytes(root.ToString());
            }

            return removed;
        }

        /// <summary>
        /// Reloads a game: domain asset from our mod's asset origin, undoing geoaddons overrides that use the same path.
        /// </summary>
        private bool RestoreAssetFromModOrigin(ICoreAPI api, string targetPath)
        {
            IAsset? target = api.Assets.TryGet(new AssetLocation(targetPath));
            if (target == null) return false;

            // Find our mod's asset origin via any advancedgeology: domain asset
            IAssetOrigin? myOrigin = null;
            foreach (var kvp in api.Assets.AllAssets)
            {
                if (kvp.Key.Domain == "advancedgeology")
                {
                    myOrigin = kvp.Value.Origin;
                    break;
                }
            }

            if (myOrigin == null)
            {
                api.Logger.Warning("[AdvancedGeology] Could not find mod asset origin for restore");
                return false;
            }

            return myOrigin.TryLoadAsset(target);
        }

        /// <summary>
        /// Reads childDeposit definitions from a config file and injects them into the target
        /// deposit assets, appending to existing childDeposits arrays or creating new ones.
        /// </summary>
        private void InjectChildDepositsFromConfig(ICoreAPI api, string configPath)
        {
            IAsset? configAsset = api.Assets.TryGet(new AssetLocation(configPath));
            if (configAsset == null)
            {
                api.Logger.Warning("[AdvancedGeology] Could not find childDeposits config: {0}", configPath);
                return;
            }

            JObject config;
            try { config = JObject.Parse(configAsset.ToText()); }
            catch { return; }

            int injected = 0;
            foreach (var entry in config.Properties())
            {
                string depositPath = entry.Name;
                JObject? depositConfig = entry.Value as JObject;
                if (depositConfig == null) continue;

                JArray? childDeposits = depositConfig["childDeposits"] as JArray;
                if (childDeposits == null || childDeposits.Count == 0) continue;

                // Support depositIndex as single int or array of ints
                JToken? indexToken = depositConfig["depositIndex"];
                int[] depositIndices = indexToken is JArray indexArray
                    ? indexArray.Select(t => t.Value<int>()).ToArray()
                    : new[] { indexToken?.Value<int>() ?? 0 };

                IAsset? depositAsset = api.Assets.TryGet(new AssetLocation(depositPath));
                if (depositAsset == null) continue;

                JArray root;
                try { root = JArray.Parse(depositAsset.ToText()); }
                catch { continue; }

                foreach (int depositIndex in depositIndices)
                {
                    if (depositIndex >= root.Count) continue;
                    JObject? targetDeposit = root[depositIndex] as JObject;
                    if (targetDeposit == null) continue;

                    JArray? existing = targetDeposit["childDeposits"] as JArray;
                    if (existing != null)
                    {
                        foreach (var child in childDeposits)
                        {
                            existing.Add(child.DeepClone());
                        }
                    }
                    else
                    {
                        targetDeposit["childDeposits"] = childDeposits.DeepClone();
                    }

                    injected += childDeposits.Count;
                }

                depositAsset.Data = Encoding.UTF8.GetBytes(root.ToString());
            }

            api.Logger.Notification("[AdvancedGeology] GeoAddons compat: injected {0} childDeposits", injected);
        }

        private int DeduplicateWorldPropertyVariants(ICoreAPI api, string assetPath, string codeField, string[] removeEntirely, string[] deduplicateCodes)
        {
            IAsset? asset = api.Assets.TryGet(new AssetLocation(assetPath));
            if (asset == null) return 0;

            JObject root;
            try
            {
                root = JObject.Parse(asset.ToText());
            }
            catch
            {
                api.Logger.Warning("[AdvancedGeology] Failed to parse {0} for GeoAddons compat", assetPath);
                return 0;
            }

            JArray? variants = root["variants"] as JArray;
            if (variants == null) return 0;

            HashSet<string> toRemove = new(removeEntirely);
            HashSet<string> toDedup = new(deduplicateCodes);
            HashSet<string> seen = new();
            int removed = 0;

            for (int i = variants.Count - 1; i >= 0; i--)
            {
                string? code = variants[i][codeField]?.ToString();
                if (code == null) continue;

                if (toRemove.Contains(code))
                {
                    variants.RemoveAt(i);
                    removed++;
                }
            }

            // Deduplicate: iterate forward, keep first occurrence, remove subsequent
            seen.Clear();
            for (int i = 0; i < variants.Count; i++)
            {
                string? code = variants[i][codeField]?.ToString();
                if (code == null) continue;

                if (toDedup.Contains(code))
                {
                    if (!seen.Add(code))
                    {
                        variants.RemoveAt(i);
                        i--;
                        removed++;
                    }
                }
            }

            if (removed > 0)
            {
                asset.Data = Encoding.UTF8.GetBytes(root.ToString());
            }

            return removed;
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
