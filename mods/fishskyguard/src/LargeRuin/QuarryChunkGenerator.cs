using Vintagestory.API.Datastructures;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using progfish.WorldGen.Util;

namespace progfish.LargeRuin {

    [JsonObject(MemberSerialization.OptIn)]
    public class QuarryChunkGenerator {

        [JsonProperty]
        private float chanceOfMetalPartsInChunk;

        [JsonProperty]
        private int minMetalParts;
        [JsonProperty]
        private int maxMetalParts;

        [JsonProperty]
        private float chanceOfGearsInChunk;

        [JsonProperty]
        private int minGears;
        [JsonProperty]
        private int maxGears;

        [JsonProperty]
        private float chanceOfSmallGear;

        [JsonProperty]
        private float chanceOfSmallParts;

        [JsonProperty]
        private float pathFadeLength;

        [JsonProperty]
        private double[] localNoiseAmp;
        [JsonProperty]
        private double[] localNoiseFreq;

        [JsonProperty]
        private double[] erosionMapNoiseAmp;
        [JsonProperty]
        private double[] erosionMapNoiseFreq;

        [JsonProperty]
        private float erosionStrength;

        [JsonProperty]
        private float erosionNegativeMod;

        private int waterID;
        private int metalPartsSmallID;
        private int metalPartsMediumID;
        private int gearPileSmallID;
        private int gearPileMediumID;
        private int pathNSID;
        private int pathEWID;

        private ICoreServerAPI api;
        private IBlockAccessor blockAccessor;
        private QuarryRegion region;

        private int worldHeight;
        private int estSeaLevel;

        private int baseX;
        private int baseZ;

        private LCGRandom random;

        private const int CHUNK_SIZE = 32;

        public void Generate(ICoreServerAPI api, QuarryRegion region, IWorldGenBlockAccessor blockAccessor, int chunkX, int chunkZ) {
            this.api = api;
            this.region = region;

            this.blockAccessor = blockAccessor;

            SimplexNoise localNoise = new SimplexNoise(localNoiseAmp, localNoiseFreq, region.regionSeed);
            SimplexNoise erosionMapNoise = new SimplexNoise(erosionMapNoiseAmp, erosionMapNoiseFreq, region.regionSeed + 1);

            waterID = api.WorldManager.GetBlockId(new AssetLocation("water-still-7"));
            metalPartsSmallID = api.WorldManager.GetBlockId(new AssetLocation("metalpartpile-small"));
            metalPartsMediumID = api.WorldManager.GetBlockId(new AssetLocation("metalpartpile-medium"));
            gearPileSmallID = api.WorldManager.GetBlockId(new AssetLocation("loosegears-1"));
            gearPileMediumID = api.WorldManager.GetBlockId(new AssetLocation("loosegears-3"));
            pathNSID = api.WorldManager.GetBlockId(new AssetLocation("woodenpath-ns"));
            pathEWID = api.WorldManager.GetBlockId(new AssetLocation("woodenpath-we"));

            random = new LCGRandom(api.World.Seed + 10607 + chunkX * 10613 + chunkZ * 10627);

            baseX = chunkX * CHUNK_SIZE;
            baseZ = chunkZ * CHUNK_SIZE;

            //Bail out if nothing's generating this chunk
            if(!region.hasQuarry || baseX > region.maxX || baseZ > region.maxZ || baseX + CHUNK_SIZE < region.minX || baseZ + CHUNK_SIZE < region.minZ) {
                return;
            }

            worldHeight = api.World.BlockAccessor.MapSizeY;

            estSeaLevel = (int) (worldHeight * 0.43f);

            foreach(QuarryRegion.PathInfo path in region.paths) {
                PlacePath(path);
            }

            for(int i = baseX; i < baseX + CHUNK_SIZE; i++) {
                for(int k = baseZ; k < baseZ + CHUNK_SIZE; k++) {
                    int distFromCenterX = 0;
                    int distFromCenterZ = 0;

                    double erosionMapSignal = GameMath.Clamp(Math.Abs(erosionMapNoise.Noise(i, k) - (1 - erosionStrength)), 0, 1);
                    double erosion = erosionMapSignal * (localNoise.Noise(i, k) - erosionNegativeMod);

                    if(i < region.baseX) {
                        distFromCenterX = region.baseX - i;
                    } else if(i > region.baseX + region.widthX) {
                        distFromCenterX = i - (region.baseX + region.widthX);
                    }

                    if(k < region.baseZ) {
                        distFromCenterZ = region.baseZ - k;
                    } else if(k > region.baseZ + region.widthZ) {
                        distFromCenterZ = k - (region.baseZ + region.widthZ);
                    }

                    var curDepth = region.depth - Math.Max(distFromCenterX, distFromCenterZ) * region.stepDepth + (int) erosion;

                    if(curDepth >= worldHeight) {
                        continue;
                    }

                    //Cut out quarry
                    FishWorldGenHelper.PlaceBlockColumn(blockAccessor, i, k, estSeaLevel - curDepth, worldHeight - 1, 0, true, false);

                    //Update edges for lakes
                    FishWorldGenHelper.UpdateBlockColumn(blockAccessor, i, k, estSeaLevel - curDepth - region.stepDepth, estSeaLevel - curDepth);

                    //Flooded quarry
                    if(region.isFlooded && distFromCenterX == 0 && distFromCenterZ == 0) {
                        FishWorldGenHelper.PlaceBlockColumn(blockAccessor, i, k, estSeaLevel - curDepth, estSeaLevel - region.depth + region.floodingDepth, waterID, true, true);
                    }
                }
            }

            TryPlaceItems(chanceOfGearsInChunk, minGears, maxGears, gearPileSmallID, gearPileMediumID, chanceOfSmallGear);
            TryPlaceItems(chanceOfMetalPartsInChunk, minMetalParts, maxMetalParts, metalPartsSmallID, metalPartsMediumID, chanceOfSmallParts);
        }

        private void TryPlaceItems(float chance, int minItems, int maxItems, int id1, int id2, float chanceID1) {
            if(random.NextFloat() < chance) {
                int numItems = minItems + random.NextInt(maxItems - minItems);

                for(int i = 0; i < numItems; i++) {
                    var pos = new BlockPos();
                    pos.X = baseX + random.NextInt(CHUNK_SIZE);
                    pos.Z = baseZ + random.NextInt(CHUNK_SIZE);

                    pos.Y = FishWorldGenHelper.GetGroundHeight(blockAccessor, pos.X, worldHeight, pos.Z);

                    if(random.NextFloat() < chanceID1) {
                        blockAccessor.SetBlock(id1, pos);
                    } else {
                        blockAccessor.SetBlock(id2, pos);
                    }
                }
            }
        }

        private void PlacePath(QuarryRegion.PathInfo path) {
            if(baseX > path.MaxX || baseZ > path.MaxZ || baseX + CHUNK_SIZE < path.MinX || baseZ + CHUNK_SIZE < path.MinZ) {
                return;
            }

            int currentID = path.dirX == 0 ? pathNSID : pathEWID;

            int startX = Math.Max(baseX, path.MinX);
            int stopX = Math.Min(baseX + CHUNK_SIZE - 1, path.MaxX);
            int startZ = Math.Max(baseZ, path.MinZ);
            int stopZ = Math.Min(baseZ + CHUNK_SIZE - 1, path.MaxZ);

            BlockPos pos = new BlockPos();

            for(int i = startX; i <= stopX; i++) {
                for(int k = startZ; k <= stopZ; k++) {
                    int distanceToEnd = int.MaxValue;

                    if(path.dirX == 1) {
                        distanceToEnd = path.MaxX - i;
                    } else if(path.dirX == -1) {
                        distanceToEnd = i - path.MinX;
                    } else if(path.dirZ == 1) {
                        distanceToEnd = path.MaxZ - k;
                    } else if(path.dirZ == -1) {
                        distanceToEnd = k - path.MaxZ;
                    }

                    if(distanceToEnd < pathFadeLength) {
                        float pathMissingChance = 1 - ((float)distanceToEnd) / pathFadeLength;
                        if(random.NextFloat() > pathMissingChance) {
                            pos.X = i;
                            pos.Z = k;
                            pos.Y = FishWorldGenHelper.GetGroundHeight(blockAccessor, i, worldHeight, k);
                            if(!blockAccessor.GetBlock(pos).IsLiquid()) {
                                blockAccessor.SetBlock(currentID, pos);
                            }
                        }
                    } else {
                        pos.X = i;
                        pos.Z = k;
                        pos.Y = FishWorldGenHelper.GetGroundHeight(blockAccessor, i, worldHeight, k);
                        if(!blockAccessor.GetBlock(pos).IsLiquid()) {
                            blockAccessor.SetBlock(currentID, pos);
                        }
                    }
                }
            }
        }

    }
}
