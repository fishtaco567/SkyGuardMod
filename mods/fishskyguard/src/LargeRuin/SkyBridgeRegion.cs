using Vintagestory.API.Datastructures;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;
using progfish.WorldGen.Util;
using Newtonsoft.Json;

namespace progfish.LargeRuin {
    [JsonObject(MemberSerialization.OptIn)]
    public class SkyBridgeRegion {

        public enum SkyBridgeFeature {
            None,
            Bridge,
            BrokenBridge,
            Building,
            Tower,
            Center
        }

        protected struct PosDirLen {
            public int posX;
            public int posZ;
            public int dirX;
            public int dirZ;
            public int lenRemaining;

            public PosDirLen(int posX, int posZ, int dirX, int dirZ, int len) {
                this.posX = posX;
                this.posZ = posZ;
                this.dirX = dirX;
                this.dirZ = dirZ;
                this.lenRemaining = len;
            }
        }

        private int baseX;
        private int baseZ;

        //Things to expose
        [JsonProperty]
        private int regionSize = 1024;
        [JsonProperty]
        private float chanceOfRegion = 1;
        [JsonProperty]
        private float rangeForCenter = 0.5f;
        [JsonProperty]
        private int minLengthOfPath = 6;
        [JsonProperty]
        private int maxLengthOfPath = 14;
        [JsonProperty]
        private float straightWalkChance = 0.8f;
        [JsonProperty]
        private float towerChance = 0.05f;
        [JsonProperty]
        private float buildingChance = 0.05f;
        [JsonProperty]
        private float brokenBridgeMinChance = 0.4f;
        [JsonProperty]
        private float brokenBridgeMaxChance = 0.9f;
        [JsonProperty]
        private float maxHeightProp = 0.8f;
        [JsonProperty]
        private float minHeightProp = 0.6f;
        [JsonProperty]
        private int maxWidth = 6;
        [JsonProperty]
        private int minWidth = 4;
        [JsonProperty]
        private int branchPathDecrement = 2;

        private const int CHUNK_SIZE = 32;

        private SkyBridgeFeature[,] featureTypeMap;

        public int height;
        public int width;

        private int numCells;

        public LCGRandom random;

        public bool InitializeRegion(int chunkX, int chunkZ, long worldSeed, int worldHeight) {
            numCells = regionSize / CHUNK_SIZE;

            this.baseX = chunkX - (chunkX % numCells);
            this.baseZ = chunkZ - (chunkZ % numCells);

            this.random = new LCGRandom(worldSeed + 11887 + baseX * 11987 + baseZ * 11903);

            height = (int)((random.NextFloat() * (maxHeightProp - minHeightProp) + minHeightProp) * worldHeight);
            width = random.NextInt(maxWidth - minWidth) + minWidth;

            featureTypeMap = new SkyBridgeFeature[numCells, numCells];
        
            float initialChance = random.NextFloat();
            
            //Check if the region is a region containing a sky bridge
            if(initialChance > chanceOfRegion) {
                return false;
            }

            int centerX = (int) (random.NextInt((int) (numCells * rangeForCenter)) + (rangeForCenter * numCells) / 2);
            int centerZ = (int) (random.NextInt((int)(numCells * rangeForCenter)) + (rangeForCenter * numCells) / 2);

            featureTypeMap[centerX, centerZ] = SkyBridgeFeature.Center;

            Queue<PosDirLen> pathHeads = new Queue<PosDirLen>();

            pathHeads.Enqueue(new PosDirLen(centerX + 1, centerZ, 1, 0, random.NextInt(maxLengthOfPath - minLengthOfPath) + minLengthOfPath));
            pathHeads.Enqueue(new PosDirLen(centerX - 1, centerZ, -1, 0, random.NextInt(maxLengthOfPath - minLengthOfPath) + minLengthOfPath));
            pathHeads.Enqueue(new PosDirLen(centerX, centerZ + 1, 0, 1, random.NextInt(maxLengthOfPath - minLengthOfPath) + minLengthOfPath));
            pathHeads.Enqueue(new PosDirLen(centerX, centerZ - 1, 0, -1, random.NextInt(maxLengthOfPath - minLengthOfPath) + minLengthOfPath));

            while(pathHeads.Any()) {
                var currentHead = pathHeads.Dequeue();

                while(currentHead.lenRemaining > 0) {
                    if(currentHead.posX >= numCells || currentHead.posZ >= numCells || currentHead.posX < 0 || currentHead.posZ < 0 ||
                        featureTypeMap[currentHead.posX, currentHead.posZ] != SkyBridgeFeature.None) {
                        break;
                    }

                    var specialNodeChance = random.NextFloat();
                    //Normal bridge
                    if(specialNodeChance > buildingChance + towerChance) {
                        var bridgeBreakChance = brokenBridgeMinChance + (brokenBridgeMaxChance - brokenBridgeMinChance) *
                            Math.Sqrt((currentHead.posX - centerX) * (currentHead.posX - centerX) + (currentHead.posZ - centerZ) * (currentHead.posZ - centerZ)) / (numCells / 2);

                        if(random.NextFloat() < bridgeBreakChance) {
                            featureTypeMap[currentHead.posX, currentHead.posZ] = SkyBridgeFeature.BrokenBridge;
                        } else {
                            featureTypeMap[currentHead.posX, currentHead.posZ] = SkyBridgeFeature.Bridge;
                        }
                    } else if(specialNodeChance > buildingChance) { //Tower
                        featureTypeMap[currentHead.posX, currentHead.posZ] = SkyBridgeFeature.Tower;
                    } else { //Building
                        featureTypeMap[currentHead.posX, currentHead.posZ] = SkyBridgeFeature.Building;
                    }
                    
                    var directionChance = random.NextFloat();
                    //Straight walk
                    if(directionChance < straightWalkChance) {
                        currentHead.posX += currentHead.dirX;
                        currentHead.posZ += currentHead.dirZ;
                        currentHead.lenRemaining--;
                    } else { //Turn or Tee
                        var turnTeeChance = random.NextFloat();
                        if(turnTeeChance < 1f / 2f) { // Turn
                            if(currentHead.dirX != 0) { //Go to dirZ
                                currentHead.dirX = 0;
                                currentHead.dirZ = random.NextFloat() > 0.5f ? 1 : -1;
                            } else { //Go to dirX
                                currentHead.dirZ = 0;
                                currentHead.dirX = random.NextFloat() > 0.5f ? 1 : -1;
                            }

                            currentHead.posX += currentHead.dirX;
                            currentHead.posZ += currentHead.dirZ;
                            currentHead.lenRemaining--;
                        } else if(turnTeeChance < 3f / 4f) { //It's a tee
                            var teeLegChance = random.NextFloat();
                            if(teeLegChance < 2f / 3f) { //There is a leg going forward
                                var forwardHead = currentHead;

                                forwardHead.posX += forwardHead.dirX;
                                forwardHead.posZ += forwardHead.dirZ;
                                forwardHead.lenRemaining--;
                                pathHeads.Enqueue(forwardHead);

                                if(currentHead.dirX != 0) { //Go to dirZ
                                    currentHead.dirX = 0;
                                    currentHead.dirZ = random.NextFloat() > 0.5f ? 1 : -1;
                                } else { //Go to dirX
                                    currentHead.dirZ = 0;
                                    currentHead.dirX = random.NextFloat() > 0.5f ? 1 : -1;
                                }

                                currentHead.posX += currentHead.dirX;
                                currentHead.posZ += currentHead.dirZ;
                                currentHead.lenRemaining--;
                            } else { //Legs to either side
                                if(currentHead.dirX != 0) { //Go to dirZ
                                    var forwardHead = currentHead;
                                    forwardHead.dirX = 0;
                                    forwardHead.dirZ = 1;
                                    forwardHead.posZ += 1;
                                    forwardHead.lenRemaining -= branchPathDecrement;
                                    pathHeads.Enqueue(forwardHead);

                                    currentHead.dirX = 0;
                                    currentHead.dirZ = -1;
                                    currentHead.posZ += -1;
                                    currentHead.lenRemaining -= branchPathDecrement;
                                } else { //Go to dirX;
                                    var forwardHead = currentHead;
                                    forwardHead.dirX = 1;
                                    forwardHead.dirZ = 0;
                                    forwardHead.posX += 1;
                                    forwardHead.lenRemaining -= branchPathDecrement;
                                    pathHeads.Enqueue(forwardHead);

                                    currentHead.dirX = -1;
                                    currentHead.dirZ = 0;
                                    currentHead.posX += -1;
                                    currentHead.lenRemaining -= branchPathDecrement;
                                }
                            }
                        } else { //it's a 4 way intersection
                            if(currentHead.dirX != 0) { //Go to dirZ
                                var forwardHead1 = currentHead;
                                forwardHead1.dirX = 0;
                                forwardHead1.dirZ = 1;
                                forwardHead1.posZ += 1;
                                forwardHead1.lenRemaining -= branchPathDecrement;
                                pathHeads.Enqueue(forwardHead1);

                                var forwardHead2 = currentHead;
                                forwardHead2.dirX = 0;
                                forwardHead2.dirZ = 1;
                                forwardHead2.posZ += 1;
                                forwardHead2.lenRemaining -= branchPathDecrement;
                                pathHeads.Enqueue(forwardHead2);
                            } else { //Go to dirX;
                                var forwardHead1 = currentHead;
                                forwardHead1.dirX = 1;
                                forwardHead1.dirZ = 0;
                                forwardHead1.posX += 1;
                                forwardHead1.lenRemaining -= branchPathDecrement;
                                pathHeads.Enqueue(forwardHead1);

                                var forwardHead2 = currentHead;
                                forwardHead2.dirX = -1;
                                forwardHead2.dirZ = 0;
                                forwardHead2.posX += 1;
                                forwardHead2.lenRemaining -= branchPathDecrement;
                                pathHeads.Enqueue(forwardHead2);
                            }

                            currentHead.posX += currentHead.dirX;
                            currentHead.posZ += currentHead.dirZ;
                            currentHead.lenRemaining--;
                        }
                    }
                }
            }

            return true;
        }

        public SkyBridgeFeature GetFeature(int chunkX, int chunkZ) {
            var indexX = chunkX - baseX;
            int indexZ = chunkZ - baseZ;

            if(indexX > 0 && indexX < numCells && indexZ > 0 && indexZ < numCells) {
                return featureTypeMap[indexX, indexZ];
            } else {
                return SkyBridgeFeature.None;
            }
        }

    }
}
