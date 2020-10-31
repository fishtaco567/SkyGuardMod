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
    public class QuarryRegion {

        public struct PathInfo {
            public int posX;
            public int posZ;

            public int dirX;
            public int dirZ;

            public int length;
            public int width;

            public int MinX {
                get {
                    if(dirX == -1) {
                        return posX - length;
                    } else {
                        return posX;
                    }
                }
            }

            public int MinZ {
                get {
                    if(dirZ == -1) {
                        return posZ - length;
                    } else {
                        return posZ;
                    }
                }
            }

            public int MaxX {
                get {
                    if(dirX == -1) {
                        return posX;
                    } else if(dirX == 1) {
                        return posX + length;
                    } else {
                        return posX + width;
                    }
                }
            }

            public int MaxZ {
                get {
                    if(dirZ == -1) {
                        return posZ;
                    } else if(dirZ == 1) {
                        return posZ + length;
                    } else {
                        return posZ + width;
                    }
                }
            }

            public PathInfo(int posX, int posZ, int dirX, int dirZ, int length, int width) {
                this.posX = posX;
                this.posZ = posZ;
                this.dirX = dirX;
                this.dirZ = dirZ;
                this.length = length;
                this.width = width;
            }

        }

        [JsonProperty]
        private int regionSize;

        [JsonProperty]
        private float chanceOfQuarry;

        [JsonProperty]
        private int minWidthX;
        [JsonProperty]
        private int maxWidthX;

        [JsonProperty]
        private int minWidthZ;
        [JsonProperty]
        private int maxWidthZ;

        [JsonProperty]
        private int minDepth;
        [JsonProperty]
        private int maxDepth;

        [JsonProperty]
        private int minStepDepth;
        [JsonProperty]
        private int maxStepDepth;

        [JsonProperty]
        private int minPathStubLength;
        [JsonProperty]
        private int maxPathStubLength;

        [JsonProperty]
        private int minPathStubs;
        [JsonProperty]
        private int maxPathStubs;

        [JsonProperty]
        private int minPathStubWidth;
        [JsonProperty]
        private int maxPathStubWidth;

        [JsonProperty]
        private float chanceOfFlooding;

        [JsonProperty]
        private int minFloodingDepth;
        [JsonProperty]
        private int maxFloodingDepth;

        public int widthX;
        public int widthZ;
        public int depth;
        public int stepDepth;

        public int baseX;
        public int baseZ;

        public List<PathInfo> paths;

        public bool hasQuarry;
        public bool isFlooded;

        public int floodingDepth;

        public int minX;
        public int minZ;
        public int maxX;
        public int maxZ;

        public int regionSeed;

        public LCGRandom random;

        private ICoreServerAPI api;

        private const int CHUNK_SIZE = 32;

        public void InitializeRegion(ICoreServerAPI api, int chunkX, int chunkZ) {
            this.api = api;

            var numCells = regionSize / CHUNK_SIZE;

            var baseChunkX = chunkX - (chunkX % numCells);
            var baseChunkZ = chunkZ - (chunkZ % numCells);

            regionSeed = api.World.Seed + 11887 + baseChunkX * 11987 + baseChunkZ * 11903;
            this.random = new LCGRandom(regionSeed);

            this.hasQuarry = random.NextFloat() < chanceOfQuarry;

            this.widthX = minWidthX + random.NextInt(maxWidthX - minWidthX);
            this.widthZ = minWidthZ + random.NextInt(maxWidthZ - minWidthZ);

            var minimumOffsetBack = maxPathStubLength;
            var maximumOffsetBackX = minimumOffsetBack + widthX;
            var maximumOffsetBackZ = minimumOffsetBack + widthZ;

            this.baseX = baseChunkX * CHUNK_SIZE + minimumOffsetBack + random.NextInt(maximumOffsetBackX - minimumOffsetBack);
            this.baseZ = baseChunkZ * CHUNK_SIZE + minimumOffsetBack + random.NextInt(maximumOffsetBackZ - minimumOffsetBack);

            this.depth = minDepth + random.NextInt(maxDepth - minDepth);

            this.stepDepth = minStepDepth + random.NextInt(maxStepDepth - minStepDepth);

            this.isFlooded = random.NextFloat() < chanceOfFlooding;
            if(this.isFlooded) {
                this.floodingDepth = minFloodingDepth + random.NextInt(maxFloodingDepth - minFloodingDepth);
            }

            int numPaths = minPathStubs + random.NextInt(maxPathStubs - minPathStubs);

            int estSeaLevel = (int) (api.World.BlockAccessor.MapSizeY * 0.43f);
            int lengthOfSlope = estSeaLevel / this.stepDepth;

            this.minX = baseX - lengthOfSlope;
            this.minZ = baseZ - lengthOfSlope;
            this.maxX = baseX + widthX + lengthOfSlope;
            this.maxZ = baseZ + widthZ + lengthOfSlope;

            paths = new List<PathInfo>();

            for(int i = 0; i < numPaths; i++) {
                int direction = random.NextInt(4);

                int dirX = direction >= 2 ? 0 : direction == 0 ? 1 : -1;
                int dirZ = direction < 2 ? 0 : direction == 2 ? 1 : -1;

                int length = minPathStubLength + random.NextInt(maxPathStubLength - minPathStubLength);
                int width = minPathStubWidth + random.NextInt(maxPathStubWidth - minPathStubWidth);

                int posX;
                int posZ;

                if(dirX != 0) {
                    posX = dirX == 1 ? baseX + widthX : baseX;
                    posZ = random.NextInt(widthZ) + baseZ;

                    if(dirX == -1) {
                        minX = Math.Min(minX, baseX - length);
                    } else {
                        maxX = Math.Max(maxX, baseX + widthX + length);
                    }
                } else {
                    posX = random.NextInt(widthX) + baseX;
                    posZ = dirZ == 1 ? baseZ + widthZ : baseZ;

                    if(dirZ == -1) {
                        minZ = Math.Min(minZ, baseZ - length);
                    } else {
                        maxZ = Math.Max(maxZ, baseZ + widthZ + length);
                    }
                }

                paths.Add(new PathInfo(posX, posZ, dirX, dirZ, length, width));
            }
        }

    }
}
