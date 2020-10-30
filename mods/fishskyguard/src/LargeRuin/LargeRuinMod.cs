using Vintagestory.API.Datastructures;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;
using progfish.WorldGen.Util;

namespace progfish.LargeRuin {
    public class LargeRuinMod : ModSystem {

        private ICoreServerAPI api;
        private IBlockAccessor chunkGenBlockAccessor;
        private IBlockAccessor worldBlockAccessor;
        private int chunkSize;

        public override void StartServerSide(ICoreServerAPI api) {
            base.StartServerSide(api);
            this.api = api;
            this.api.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);
            this.api.Event.ChunkColumnGeneration(OnChunkColumnGeneration, EnumWorldGenPass.TerrainFeatures, "standard");
            this.worldBlockAccessor = api.World.BlockAccessor;
            this.chunkSize = worldBlockAccessor.ChunkSize;
        }

        public override bool ShouldLoad(EnumAppSide side) {
            return side == EnumAppSide.Server;
        }

        public override double ExecuteOrder() {
            return 0.39;
        }

        private void OnChunkColumnGeneration(IServerChunk[] chunks, int chunkX, int chunkZ, ITreeAttribute chunkGenParams) {
            var asset = api.Assets.Get(new AssetLocation("fishskyguard", "worldgen/skybridgeregion.json"));
            var skyBridgeRegion = asset.ToObject<SkyBridgeRegion>();
            skyBridgeRegion.InitializeRegion(chunkX, chunkZ, api.World.Seed, worldBlockAccessor.MapSizeY);

            asset = api.Assets.Get(new AssetLocation("fishskyguard", "worldgen/skybridgegen.json"));
            var skyBridgeGenerator = asset.ToObject<SkyBridgeChunkGenerator>();
            skyBridgeGenerator.Init(skyBridgeRegion);
            skyBridgeGenerator.GenerateChunk(api, chunkGenBlockAccessor, chunkX, chunkZ, api.World.Seed);
        }

        private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider) {
            this.chunkGenBlockAccessor = chunkProvider.GetBlockAccessor(true);
        }

    }
}
