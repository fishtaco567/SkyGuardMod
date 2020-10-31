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
    public class SkyBridgeChunkGenerator {

        public SkyBridgeRegion region;

        private const int CHUNK_SIZE = 32;

        public int chunkX;
        public int chunkZ;

        private int cobbleID;
        private int stairDN;
        private int stairDS;
        private int stairDE;
        private int stairDW;
        private int ladderNID;
        private int agedPlankID;
        private int bedHeadEastID;
        private int bedFootEastID;
        private int bedHeadWestID;
        private int bedFootWestID;
        private int crateID;
        private int torchBurnedOutEastID;
        private int torchBurnedOutWestID;
        private int torchBurnedOutSouthID;
        private int tableID;
        private int chairID;
        private int chestNorthID;

        private LCGRandom random;

        [JsonProperty]
        private int residualLengthMin = 3;
        [JsonProperty]
        private int residualLengthMax = 7;

        [JsonProperty]
        private int minRubblePiles = 1;
        [JsonProperty]
        private int maxRubblePiles = 2;

        [JsonProperty]
        private int minBridgeRubblePileSize = 3;
        [JsonProperty]
        private int maxBridgeRubblePileSize = 6;

        [JsonProperty]
        private int minRubbleHeight = 1;
        [JsonProperty]
        private int maxRubbleHeight = 3;

        [JsonProperty]
        private int minTowerStubSize = 4;
        [JsonProperty]
        private int maxTowerStubSize = 8;

        [JsonProperty]
        private int storeRoomHeightMin = 4;
        [JsonProperty]
        private int storeRoomHeightMax = 6;

        [JsonProperty]
        private int barracksHeightMin = 4;
        [JsonProperty]
        private int barracksHeightMax = 5;

        [JsonProperty]
        private int barracksMinCrates = 3;
        [JsonProperty]
        private int barracksMaxCrates = 8;

        [JsonProperty]
        private float barracksBedChance = 0.75f;

        [JsonProperty]
        private float barracksChestChance = 0.25f;

        [JsonProperty]
        private float centerChestChance = 0.5f;

        [JsonProperty]
        private int storeRoomPlatformExtraWidth = 2;

        [JsonProperty]
        private int barracksPlatformExtraWidth = 5;

        [JsonProperty]
        private int centerPlatformExtraWidth = 4;

        [JsonProperty]
        private int endPlatformExtraWidth = 1;

        [JsonProperty]
        private float rubbleBaseChance = .75f;

        [JsonProperty]
        private string[] itemsBarracksChest;
        [JsonProperty]
        private int[] itemsBarracksChestWeights;
        [JsonProperty]
        private int minBarracksChestItems;
        [JsonProperty]
        private int maxBarracksChestItems;

        [JsonProperty]
        private string[] itemsTowerChest;
        [JsonProperty]
        private int[] itemsTowerChestWeights;
        [JsonProperty]
        private int minTowerChestItems;
        [JsonProperty]
        private int maxTowerChestItems;

        [JsonProperty]
        private string[] itemsCenterChest;
        [JsonProperty]
        private int[] itemsCenterChestWeights;
        [JsonProperty]
        private int minCenterChestItems;
        [JsonProperty]
        private int maxCenterChestItems;

        public void Init(SkyBridgeRegion region) {
            this.region = region;
        }

        public void GenerateChunk(ICoreServerAPI api, IWorldGenBlockAccessor blockAccessor, int chunkX, int chunkZ, long seed) {
            var chunkFeature = region.GetFeature(chunkX, chunkZ);
            if(chunkFeature == SkyBridgeRegion.SkyBridgeFeature.None) {
                return;
            }

            cobbleID = api.WorldManager.GetBlockId(new AssetLocation("cobblestone-granite"));
            stairDN = api.WorldManager.GetBlockId(new AssetLocation("cobblestonestairs-granite-down-north-free"));
            stairDS = api.WorldManager.GetBlockId(new AssetLocation("cobblestonestairs-granite-down-south-free"));
            stairDE = api.WorldManager.GetBlockId(new AssetLocation("cobblestonestairs-granite-down-west-free"));
            stairDW = api.WorldManager.GetBlockId(new AssetLocation("cobblestonestairs-granite-down-east-free"));
            agedPlankID = api.WorldManager.GetBlockId(new AssetLocation("planks-aged"));
            ladderNID = api.WorldManager.GetBlockId(new AssetLocation("ladder-north"));
            bedHeadEastID = api.WorldManager.GetBlockId(new AssetLocation("bed-wood-head-east"));
            bedFootEastID = api.WorldManager.GetBlockId(new AssetLocation("bed-wood-feet-east"));
            bedHeadWestID = api.WorldManager.GetBlockId(new AssetLocation("bed-wood-head-west"));
            bedFootWestID = api.WorldManager.GetBlockId(new AssetLocation("bed-wood-feet-west"));
            crateID = api.WorldManager.GetBlockId(new AssetLocation("woodencrate-opened"));
            torchBurnedOutEastID = api.WorldManager.GetBlockId(new AssetLocation("torch-burnedout-east"));
            torchBurnedOutWestID = api.WorldManager.GetBlockId(new AssetLocation("torch-burnedout-west"));
            torchBurnedOutSouthID = api.WorldManager.GetBlockId(new AssetLocation("torch-burnedout-south"));
            tableID = api.WorldManager.GetBlockId(new AssetLocation("table-aged"));
            chairID = api.WorldManager.GetBlockId(new AssetLocation("chair-aged"));
            chestNorthID = api.WorldManager.GetBlockId(new AssetLocation("chest-north"));

            random = new LCGRandom(seed + 10607 + chunkX * 10613 + chunkZ * 10627);

            var northFeature = region.GetFeature(chunkX, chunkZ + 1);
            var southFeature = region.GetFeature(chunkX, chunkZ - 1);
            var eastFeature = region.GetFeature(chunkX + 1, chunkZ);
            var westFeature = region.GetFeature(chunkX - 1, chunkZ);

            bool connectsNorth = northFeature != SkyBridgeRegion.SkyBridgeFeature.None;
            bool connectsSouth = southFeature != SkyBridgeRegion.SkyBridgeFeature.None;
            bool connectsEast = eastFeature != SkyBridgeRegion.SkyBridgeFeature.None;
            bool connectsWest = westFeature != SkyBridgeRegion.SkyBridgeFeature.None;

            if(chunkFeature == SkyBridgeRegion.SkyBridgeFeature.BrokenBridge) {
                if(northFeature == SkyBridgeRegion.SkyBridgeFeature.BrokenBridge) {
                    connectsNorth = false;
                }

                if(southFeature == SkyBridgeRegion.SkyBridgeFeature.BrokenBridge) {
                    connectsSouth = false;
                }

                if(eastFeature == SkyBridgeRegion.SkyBridgeFeature.BrokenBridge) {
                    connectsEast = false;
                }

                if(westFeature == SkyBridgeRegion.SkyBridgeFeature.BrokenBridge) {
                    connectsWest = false;
                }
            }

            int numConnecting = (connectsNorth ? 1 : 0) + (connectsSouth ? 1 : 0) + (connectsEast ? 1 : 0) + (connectsWest ? 1 : 0);

            int baseX = chunkX * CHUNK_SIZE;
            int baseZ = chunkZ * CHUNK_SIZE;

            int towerCornerX = baseX + CHUNK_SIZE / 2 - region.width / 2;
            int towerCornerZ = baseZ + CHUNK_SIZE / 2 - region.width / 2;

            int towerPlatformSizeX = region.width - 1;
            int towerPlatformSizeZ = region.width - 1;

            int towerPlatformMinX = towerCornerX;
            int towerPlatformMinZ = towerCornerZ;
            int towerPlatformMaxX = towerCornerX + towerPlatformSizeX;
            int towerPlatformMaxZ = towerCornerZ + towerPlatformSizeZ;

            BlockPos pos = new BlockPos();

            if(chunkFeature != SkyBridgeRegion.SkyBridgeFeature.BrokenBridge) {
                //Build Tower to height - 1
                for(int i = 0; i < region.width; i++) {
                    for(int k = 0; k < region.width; k++) {
                        if(i == 0 || i == region.width - 1 || k == 0 || k == region.width - 1) {
                            int groundHeight = FishWorldGenHelper.PlaceBlockColumnToGround(blockAccessor, i + towerCornerX, region.height - 1, k + towerCornerZ, cobbleID);
                            
                            //Generate "door" openings
                            if(chunkFeature == SkyBridgeRegion.SkyBridgeFeature.Tower && k == region.width / 2) {
                                FishWorldGenHelper.PlaceBlockColumn(blockAccessor, i + towerCornerX, k + towerCornerZ, groundHeight, groundHeight + 2, 0, true);
                            }
                        }
                    }
                }

                if(chunkFeature == SkyBridgeRegion.SkyBridgeFeature.Bridge && numConnecting == 1) { // End
                    towerPlatformMinX -= endPlatformExtraWidth;
                    towerPlatformMinZ -= endPlatformExtraWidth;
                    towerPlatformMaxX += endPlatformExtraWidth;
                    towerPlatformMaxZ += endPlatformExtraWidth;
                    towerPlatformSizeX += endPlatformExtraWidth * 2;
                    towerPlatformSizeZ += endPlatformExtraWidth * 2;

                    //Create platform and railing
                    for(int i = towerPlatformMinX; i <= towerPlatformMaxX; i++) {
                        for(int k = towerPlatformMinZ; k <= towerPlatformMaxZ; k++) {
                            pos.X = i;
                            pos.Y = region.height;
                            pos.Z = k;

                            //Create Railing
                            if(i == towerPlatformMinX || i == towerPlatformMaxX || k == towerPlatformMinZ || k == towerPlatformMaxZ) {
                                blockAccessor.SetBlock(cobbleID, pos);
                                pos.Y += 1;
                                blockAccessor.SetBlock(cobbleID, pos);
                            } else {
                                blockAccessor.SetBlock(agedPlankID, pos);
                            }
                        }
                    }

                    pos.X = towerPlatformMinX + towerPlatformSizeX / 2;
                    pos.Z = towerPlatformMinZ + towerPlatformSizeZ / 2;
                    pos.Y = region.height + 1;
                    blockAccessor.SetBlock(tableID, pos);
                    blockAccessor.SetBlock(chairID, pos.EastCopy());
                    blockAccessor.SetBlock(chairID, pos.WestCopy());
                    blockAccessor.SetBlock(chairID, pos.NorthCopy());
                    blockAccessor.SetBlock(chairID, pos.SouthCopy());
                } else if(chunkFeature == SkyBridgeRegion.SkyBridgeFeature.Bridge) { //Normal
                    //Create platform and railing
                    for(int i = towerPlatformMinX; i <= towerPlatformMaxX; i++) {
                        for(int k = towerPlatformMinZ; k <= towerPlatformMaxZ; k++) {
                            pos.X = i;
                            pos.Y = region.height;
                            pos.Z = k;


                            //Create Railing
                            if(i == towerPlatformMinX || i == towerPlatformMaxX || k == towerPlatformMinZ || k == towerPlatformMaxZ) {
                                blockAccessor.SetBlock(cobbleID, pos);
                                pos.Y += 1;
                                blockAccessor.SetBlock(cobbleID, pos);
                            } else {
                                blockAccessor.SetBlock(agedPlankID, pos);
                            }
                        }
                    }
                } else if(chunkFeature == SkyBridgeRegion.SkyBridgeFeature.Tower) {
                    towerPlatformMinX -= storeRoomPlatformExtraWidth;
                    towerPlatformMinZ -= storeRoomPlatformExtraWidth;
                    towerPlatformMaxX += storeRoomPlatformExtraWidth;
                    towerPlatformMaxZ += storeRoomPlatformExtraWidth;
                    towerPlatformSizeX += storeRoomPlatformExtraWidth * 2;
                    towerPlatformSizeZ += storeRoomPlatformExtraWidth * 2;

                    //Create platform and railing
                    for(int i = towerPlatformMinX; i <= towerPlatformMaxX; i++) {
                        for(int k = towerPlatformMinZ; k <= towerPlatformMaxZ; k++) {
                            pos.X = i;
                            pos.Y = region.height;
                            pos.Z = k;


                            //Create Railing
                            if(i == towerPlatformMinX || i == towerPlatformMaxX || k == towerPlatformMinZ || k == towerPlatformMaxZ) {
                                blockAccessor.SetBlock(cobbleID, pos);
                                pos.Y += 1;
                                blockAccessor.SetBlock(cobbleID, pos);
                            } else {
                                blockAccessor.SetBlock(cobbleID, pos);
                            }
                        }
                    }

                    for(int j = 1; j < storeRoomPlatformExtraWidth; j++) {
                        for(int i = towerPlatformMinX + j; i <= towerPlatformMaxX - j; i++) {
                            for(int k = towerPlatformMinZ + j; k <= towerPlatformMaxZ - j; k++) {
                                if(i == towerPlatformMinX + j || i == towerPlatformMaxX - j || k == towerPlatformMinZ + j || k == towerPlatformMaxZ - j) {
                                    pos.X = i;
                                    pos.Y = region.height - j;
                                    pos.Z = k;
                                    blockAccessor.SetBlock(cobbleID, pos);
                                }
                            }
                        }
                    }

                    var storeRoomHeight = storeRoomHeightMin + random.NextInt(storeRoomHeightMax - storeRoomHeightMin);

                    //Create small storehouse
                    for(int i = 0; i < region.width; i++) {
                        for(int k = 0; k < region.width; k++) {
                            if((i == 0 || i == region.width - 1 || k == 0 || k == region.width - 1) && k != region.width - 2) {
                                int heightMod = (k == region.width - 1) ? 1 : 0;
                                FishWorldGenHelper.PlaceBlockColumnToGround(blockAccessor, i + towerCornerX, region.height + storeRoomHeight + heightMod, k + towerCornerZ, cobbleID);
                            } else {
                                pos.Set(i + towerCornerX, region.height + storeRoomHeight - 1, k + towerCornerZ);
                                blockAccessor.SetBlock(agedPlankID, pos);
                            }
                        }
                    }

                    //place ladder
                    pos.Set(towerCornerX + 1, region.height, towerCornerZ + 1);
                    blockAccessor.SetBlock(0, pos);
                    pos.Y += 2;
                    blockAccessor.SetBlock(torchBurnedOutSouthID, pos);
                    FishWorldGenHelper.PlaceBlockColumnToGround(blockAccessor, towerCornerX + 1, region.height, towerCornerZ + 1, ladderNID);

                    //place chest
                    pos.Set(towerCornerX + region.width - 2, region.height + 1, towerCornerZ + 1);
                    var chestBlock = blockAccessor.GetBlock(chestNorthID);
                    chestBlock.TryPlaceBlockForWorldGen(blockAccessor, pos, BlockFacing.DOWN, random);

                    var chestEntity = blockAccessor.GetBlockEntity(pos) as BlockEntityGenericTypedContainer;
                    if(chestEntity != null) {
                        chestEntity.Initialize(api);

                        var itemRandomizer = new ChestItemRandomizer(itemsTowerChest, itemsTowerChestWeights, api);
                        itemRandomizer.PlaceItemsInChest(chestEntity, minTowerChestItems, maxTowerChestItems, random);
                    }
                } else if(chunkFeature == SkyBridgeRegion.SkyBridgeFeature.Building) {
                    towerPlatformMinX -= barracksPlatformExtraWidth;
                    towerPlatformMinZ -= barracksPlatformExtraWidth;
                    towerPlatformMaxX += barracksPlatformExtraWidth;
                    towerPlatformMaxZ += barracksPlatformExtraWidth;
                    towerPlatformSizeX += barracksPlatformExtraWidth * 2;
                    towerPlatformSizeZ += barracksPlatformExtraWidth * 2;

                    //Create platform and railing
                    for(int i = towerPlatformMinX; i <= towerPlatformMaxX; i++) {
                        for(int k = towerPlatformMinZ; k <= towerPlatformMaxZ; k++) {
                            pos.X = i;
                            pos.Y = region.height;
                            pos.Z = k;


                            //Create Railing
                            if(i == towerPlatformMinX || i == towerPlatformMaxX || k == towerPlatformMinZ || k == towerPlatformMaxZ) {
                                blockAccessor.SetBlock(cobbleID, pos);
                                pos.Y += 1;
                                blockAccessor.SetBlock(cobbleID, pos);
                            } else {
                                blockAccessor.SetBlock(cobbleID, pos);
                            }
                        }
                    }

                    for(int j = 1; j < barracksPlatformExtraWidth; j++) {
                        for(int i = towerPlatformMinX + j; i <= towerPlatformMaxX - j; i++) {
                            for(int k = towerPlatformMinZ + j; k <= towerPlatformMaxZ - j; k++) {
                                if(i == towerPlatformMinX + j || i == towerPlatformMaxX - j || k == towerPlatformMinZ + j || k == towerPlatformMaxZ - j) {
                                    pos.X = i;
                                    pos.Y = region.height - j;
                                    pos.Z = k;
                                    blockAccessor.SetBlock(cobbleID, pos);
                                }
                            }
                        }
                    }

                    //place crates
                    var numCrates = barracksMinCrates + random.NextInt(barracksMaxCrates - barracksMinCrates);
                    pos.Y = region.height + 1;
                    for(int i = 0; i < numCrates; i++) {
                        pos.X = random.NextInt(towerPlatformSizeX - 6) + towerPlatformMinX + 3;
                        pos.Z = random.NextInt(towerPlatformSizeZ - 6) + towerPlatformMinZ + 3;
                        blockAccessor.SetBlock(crateID, pos);
                    }

                    //try place chest
                    if(random.NextFloat() < barracksChestChance) {
                        pos.Set(random.NextInt(towerPlatformSizeX - 6) + towerPlatformMinX + 3, region.height + 1, towerPlatformMinZ + 3);
                        var chestBlock = blockAccessor.GetBlock(chestNorthID);
                        chestBlock.TryPlaceBlockForWorldGen(blockAccessor, pos, BlockFacing.DOWN, random);
                        var chestEntity = blockAccessor.GetBlockEntity(pos) as BlockEntityGenericTypedContainer;
                        if(chestEntity != null) {
                            chestEntity.Initialize(api);

                            var itemRandomizer = new ChestItemRandomizer(itemsBarracksChest, itemsBarracksChestWeights, api);
                            itemRandomizer.PlaceItemsInChest(chestEntity, minBarracksChestItems, maxBarracksChestItems, random);
                        }
                    }

                    var barracksHeight = barracksHeightMin + random.NextInt(barracksHeightMax - barracksHeightMin);
                    //Create barracks
                    for(int i = 2; i <= towerPlatformSizeX - 2; i++) {
                        for(int k = 2; k <= towerPlatformSizeZ - 2; k++) {
                            if((i == 2 || i == towerPlatformSizeX - 2 || k == 2 || k == towerPlatformSizeZ - 2) && k != towerPlatformSizeZ - 3) {
                                FishWorldGenHelper.PlaceBlockColumnToGround(blockAccessor, i + towerPlatformMinX, region.height + barracksHeight, k + towerPlatformMinZ, cobbleID);

                                //On a east edge
                                if(i == 2 && k % 2 == 0 && k > 2 && k < towerPlatformSizeZ - 2 && random.NextFloat() < barracksBedChance) {
                                    pos.X = i + 1 + towerPlatformMinX;
                                    pos.Y = region.height + 2;
                                    pos.Z = k + towerPlatformMinZ;
                                    blockAccessor.SetBlock(torchBurnedOutEastID, pos);
                                    pos.Y -= 1;
                                    blockAccessor.SetBlock(bedHeadEastID, pos);
                                    pos.X += 1;
                                    blockAccessor.SetBlock(bedFootEastID, pos);
                                }

                                //On a west edge
                                if(i == towerPlatformSizeX - 2 && k % 2 == 0 && k > 2 && k < towerPlatformSizeZ - 2 && random.NextFloat() < barracksBedChance) {
                                    pos.X = i - 1 + towerPlatformMinX;
                                    pos.Y = region.height + 2;
                                    pos.Z = k + towerPlatformMinZ;
                                    blockAccessor.SetBlock(torchBurnedOutWestID, pos);
                                    pos.Y -= 1;
                                    blockAccessor.SetBlock(bedHeadWestID, pos);
                                    pos.X -= 1;
                                    blockAccessor.SetBlock(bedFootWestID, pos);
                                }
                            } else {
                                pos.Set(i + towerPlatformMinX, region.height + barracksHeight - 1, k + towerPlatformMinZ);
                                blockAccessor.SetBlock(agedPlankID, pos);
                            }
                        }
                    }

                } else if(chunkFeature == SkyBridgeRegion.SkyBridgeFeature.Center) {
                    towerPlatformMinX -= centerPlatformExtraWidth;
                    towerPlatformMinZ -= centerPlatformExtraWidth;
                    towerPlatformMaxX += centerPlatformExtraWidth;
                    towerPlatformMaxZ += centerPlatformExtraWidth;
                    towerPlatformSizeX += centerPlatformExtraWidth * 2;
                    towerPlatformSizeZ += centerPlatformExtraWidth * 2;

                    //Create platform and railing
                    for(int i = towerPlatformMinX; i <= towerPlatformMaxX; i++) {
                        for(int k = towerPlatformMinZ; k <= towerPlatformMaxZ; k++) {
                            pos.X = i;
                            pos.Y = region.height;
                            pos.Z = k;


                            //Create Railing
                            if(i == towerPlatformMinX || i == towerPlatformMaxX || k == towerPlatformMinZ || k == towerPlatformMaxZ) {
                                blockAccessor.SetBlock(cobbleID, pos);
                                pos.Y += 1;
                                blockAccessor.SetBlock(cobbleID, pos);
                            } else {
                                blockAccessor.SetBlock(cobbleID, pos);
                            }
                        }
                    }

                    for(int j = 1; j < centerPlatformExtraWidth; j++) {
                        for(int i = towerPlatformMinX + j; i <= towerPlatformMaxX - j; i++) {
                            for(int k = towerPlatformMinZ + j; k <= towerPlatformMaxZ - j; k++) {
                                if(i == towerPlatformMinX + j || i == towerPlatformMaxX - j || k == towerPlatformMinZ + j || k == towerPlatformMaxZ - j) {
                                    pos.X = i;
                                    pos.Y = region.height - j;
                                    pos.Z = k;
                                    blockAccessor.SetBlock(cobbleID, pos);
                                }
                            }
                        }
                    }

                    //try place chest
                    if(random.NextFloat() < centerChestChance) {
                        pos.Set(random.NextInt(towerPlatformSizeX - 6) + towerPlatformMinX + 3, region.height + 1, towerPlatformMinZ + 1);
                        var chestBlock = blockAccessor.GetBlock(chestNorthID);
                        chestBlock.TryPlaceBlockForWorldGen(blockAccessor, pos, BlockFacing.DOWN, random);
                        var chestEntity = blockAccessor.GetBlockEntity(pos) as BlockEntityGenericTypedContainer;
                        if(chestEntity != null) {
                            chestEntity.Initialize(api);

                            var itemRandomizer = new ChestItemRandomizer(itemsCenterChest, itemsCenterChestWeights, api);
                            itemRandomizer.PlaceItemsInChest(chestEntity, minCenterChestItems, maxCenterChestItems, random);
                        }
                    }

                    //place crates
                    var numCrates = barracksMinCrates + random.NextInt(barracksMaxCrates - barracksMinCrates);
                    pos.Y = region.height + 1;
                    for(int i = 0; i < numCrates; i++) {
                        pos.X = random.NextInt(towerPlatformSizeX - 2) + towerPlatformMinX + 1;
                        pos.Z = random.NextInt(towerPlatformSizeZ - 2) + towerPlatformMinZ + 1;
                        blockAccessor.SetBlock(crateID, pos);
                    }

                    pos.X = towerPlatformMinX + towerPlatformSizeX / 2;
                    pos.Z = towerPlatformMinZ + towerPlatformSizeZ / 2;
                    pos.Y = region.height + 1;
                    blockAccessor.SetBlock(tableID, pos);
                    blockAccessor.SetBlock(chairID, pos.EastCopy());
                    blockAccessor.SetBlock(chairID, pos.WestCopy());
                    blockAccessor.SetBlock(chairID, pos.NorthCopy());
                    blockAccessor.SetBlock(chairID, pos.SouthCopy());
                }

                if(connectsNorth) {
                    //Place bridge surface
                    FishWorldGenHelper.PlaceArea(blockAccessor, towerCornerX + 1, region.height, towerPlatformMaxZ + 1,
                        towerCornerX + region.width - 2, region.height, baseZ + CHUNK_SIZE - 1, agedPlankID, false);
                    //Cut out tower wall for path
                    FishWorldGenHelper.PlaceArea(blockAccessor, towerCornerX + 1, region.height + 1, towerPlatformMaxZ,
                        towerCornerX + region.width - 2, region.height + 1, towerPlatformMaxZ, 0, true);
                    //Place bridge rails
                    FishWorldGenHelper.PlaceArea(blockAccessor, towerCornerX, region.height - 2, towerPlatformMaxZ + 1,
                        towerCornerX, region.height + 1, baseZ + CHUNK_SIZE - 1, cobbleID, false);
                    FishWorldGenHelper.PlaceArea(blockAccessor, towerCornerX + region.width - 1, region.height - 2, towerPlatformMaxZ + 1,
                        towerCornerX + region.width - 1, region.height + 1, baseZ + CHUNK_SIZE - 1, cobbleID, false);

                    //Create arch
                    int currentPieceDepth = 3;
                    int currentPieceRun = 1;
                    int currentRun = 0;
                    int bottomOfCurrentArchPiece = region.height - 3 - (currentPieceDepth * (currentPieceDepth + 1)) / 2 - currentPieceDepth / 2;

                    for(int k = towerCornerZ + region.width; k < baseZ + CHUNK_SIZE; k++) {
                        FishWorldGenHelper.PlaceBlockColumn(blockAccessor, towerCornerX, k, bottomOfCurrentArchPiece, region.height, cobbleID, false);
                        FishWorldGenHelper.PlaceBlockColumn(blockAccessor, towerCornerX + region.width - 1, k, bottomOfCurrentArchPiece, region.height, cobbleID, false);

                        if(currentPieceDepth != 1) {
                            pos.X = towerCornerX;
                            pos.Y = bottomOfCurrentArchPiece - 1;
                            pos.Z = k;
                            blockAccessor.SetBlock(stairDN, pos);
                            pos.X = towerCornerX + region.width - 1;
                            blockAccessor.SetBlock(stairDN, pos);

                            bottomOfCurrentArchPiece += currentPieceDepth;
                            currentPieceDepth -= 1;
                        } else {
                            if(currentRun == 0) {
                                pos.X = towerCornerX;
                                pos.Y = bottomOfCurrentArchPiece - 1;
                                pos.Z = k;
                                blockAccessor.SetBlock(stairDN, pos);
                                pos.X = towerCornerX + region.width - 1;
                                blockAccessor.SetBlock(stairDN, pos);
                            }

                            currentRun++;
                            if(currentRun == currentPieceRun) {
                                bottomOfCurrentArchPiece += currentPieceDepth;
                                currentPieceRun++;
                                currentRun = 0;
                            }
                        }

                        if(currentPieceDepth >= region.height - 3) {
                            break;
                        }
                    }
                }

                if(connectsSouth) {
                    //Place bridge surface
                    FishWorldGenHelper.PlaceArea(blockAccessor, towerCornerX + 1, region.height, baseZ,
                        towerCornerX + region.width - 2, region.height, towerCornerZ - 1, agedPlankID, false);
                    //Cut out tower wall for path
                    FishWorldGenHelper.PlaceArea(blockAccessor, towerCornerX + 1, region.height + 1, towerPlatformMinZ,
                        towerCornerX + region.width - 2, region.height + 1, towerPlatformMinZ, 0, true);
                    //Place bridge rails
                    FishWorldGenHelper.PlaceArea(blockAccessor, towerCornerX, region.height - 2, baseZ,
                        towerCornerX, region.height + 1, towerPlatformMinZ - 1, cobbleID, false);
                    FishWorldGenHelper.PlaceArea(blockAccessor, towerCornerX + region.width - 1, region.height - 2, baseZ,
                        towerCornerX + region.width - 1, region.height + 1, towerPlatformMinZ - 1, cobbleID, false);

                    //Create arch
                    int currentPieceDepth = 3;
                    int currentPieceRun = 1;
                    int currentRun = 0;
                    int bottomOfCurrentArchPiece = region.height - 3 - (currentPieceDepth * (currentPieceDepth + 1)) / 2 - currentPieceDepth / 2;

                    for(int k = towerCornerZ - 1; k > baseZ; k--) {
                        FishWorldGenHelper.PlaceBlockColumn(blockAccessor, towerCornerX, k, bottomOfCurrentArchPiece, region.height, cobbleID, false);
                        FishWorldGenHelper.PlaceBlockColumn(blockAccessor, towerCornerX + region.width - 1, k, bottomOfCurrentArchPiece, region.height, cobbleID, false);

                        if(currentPieceDepth != 1) {
                            pos.X = towerCornerX;
                            pos.Y = bottomOfCurrentArchPiece - 1;
                            pos.Z = k;
                            blockAccessor.SetBlock(stairDS, pos);
                            pos.X = towerCornerX + region.width - 1;
                            blockAccessor.SetBlock(stairDS, pos);

                            bottomOfCurrentArchPiece += currentPieceDepth;
                            currentPieceDepth -= 1;
                        } else {
                            if(currentRun == 0) {
                                pos.X = towerCornerX;
                                pos.Y = bottomOfCurrentArchPiece - 1;
                                pos.Z = k;
                                blockAccessor.SetBlock(stairDS, pos);
                                pos.X = towerCornerX + region.width - 1;
                                blockAccessor.SetBlock(stairDS, pos);
                            }

                            currentRun++;
                            if(currentRun == currentPieceRun) {
                                bottomOfCurrentArchPiece += currentPieceDepth;
                                currentPieceRun++;
                                currentRun = 0;
                            }
                        }

                        if(currentPieceDepth >= region.height - 3) {
                            break;
                        }
                    }
                }

                if(connectsEast) {
                    //Place bridge surface
                    FishWorldGenHelper.PlaceArea(blockAccessor, towerCornerX + region.width, region.height, towerCornerZ + 1,
                        baseX + CHUNK_SIZE - 1, region.height, towerCornerZ + region.width - 2, agedPlankID, false);
                    //Cut out tower wall for path
                    FishWorldGenHelper.PlaceArea(blockAccessor, towerPlatformMaxX, region.height + 1, towerCornerZ + 1,
                        towerPlatformMaxX, region.height + 1, towerCornerZ + region.width - 2, 0, true);
                    //Place bridge rails
                    FishWorldGenHelper.PlaceArea(blockAccessor, towerPlatformMaxX + 1, region.height - 2, towerCornerZ,
                        baseX + CHUNK_SIZE - 1, region.height + 1, towerCornerZ, cobbleID, false);
                    FishWorldGenHelper.PlaceArea(blockAccessor, towerPlatformMaxX + 1, region.height - 2, towerCornerZ + region.width - 1,
                        baseX + CHUNK_SIZE - 1, region.height + 1, towerCornerZ + region.width - 1, cobbleID, false);

                    //Create arch
                    int currentPieceDepth = 3;
                    int currentPieceRun = 1;
                    int currentRun = 0;
                    int bottomOfCurrentArchPiece = region.height - 3 - (currentPieceDepth * (currentPieceDepth + 1)) / 2 - currentPieceDepth / 2;

                    for(int i = towerCornerX + region.width; i < baseX + CHUNK_SIZE; i++) {
                        FishWorldGenHelper.PlaceBlockColumn(blockAccessor, i, towerCornerZ, bottomOfCurrentArchPiece, region.height, cobbleID, false);
                        FishWorldGenHelper.PlaceBlockColumn(blockAccessor, i, towerCornerZ + region.width - 1, bottomOfCurrentArchPiece, region.height, cobbleID, false);

                        if(currentPieceDepth != 1) {
                            pos.X = i;
                            pos.Y = bottomOfCurrentArchPiece - 1;
                            pos.Z = towerCornerZ;
                            blockAccessor.SetBlock(stairDE, pos);
                            pos.Z = towerCornerZ + region.width - 1;
                            blockAccessor.SetBlock(stairDE, pos);

                            bottomOfCurrentArchPiece += currentPieceDepth;
                            currentPieceDepth -= 1;
                        } else {
                            if(currentRun == 0) {
                                pos.X = i;
                                pos.Y = bottomOfCurrentArchPiece - 1;
                                pos.Z = towerCornerZ;
                                blockAccessor.SetBlock(stairDE, pos);
                                pos.Z = towerCornerZ + region.width - 1;
                                blockAccessor.SetBlock(stairDE, pos);
                            }

                            currentRun++;
                            if(currentRun == currentPieceRun) {
                                bottomOfCurrentArchPiece += currentPieceDepth;
                                currentPieceRun++;
                                currentRun = 0;
                            }
                        }

                        if(currentPieceDepth >= region.height - 3) {
                            break;
                        }
                    }
                }

                if(connectsWest) {
                    //Place bridge surface
                    FishWorldGenHelper.PlaceArea(blockAccessor, baseX, region.height, towerCornerZ + 1,
                        towerCornerX - 1, region.height, towerCornerZ + region.width - 2, agedPlankID, false);
                    //Cut out tower wall for path
                    FishWorldGenHelper.PlaceArea(blockAccessor, towerPlatformMinX, region.height + 1, towerCornerZ + 1,
                        towerPlatformMinX, region.height + 1, towerCornerZ + region.width - 2, 0, true);
                    //Place bridge rails
                    FishWorldGenHelper.PlaceArea(blockAccessor, baseX, region.height - 2, towerCornerZ,
                        towerPlatformMinX - 1, region.height + 1, towerCornerZ, cobbleID, false);
                    FishWorldGenHelper.PlaceArea(blockAccessor, baseX, region.height - 2, towerCornerZ + region.width - 1,
                        towerPlatformMinX - 1, region.height + 1, towerCornerZ + region.width - 1, cobbleID, false);

                    //Create arch
                    int currentPieceDepth = 3;
                    int currentPieceRun = 1;
                    int currentRun = 0;
                    int bottomOfCurrentArchPiece = region.height - 3 - (currentPieceDepth * (currentPieceDepth + 1)) / 2 - currentPieceDepth / 2;

                    for(int i = towerCornerX - 1; i > baseX; i--) {
                        FishWorldGenHelper.PlaceBlockColumn(blockAccessor, i, towerCornerZ, bottomOfCurrentArchPiece, region.height, cobbleID, false);
                        FishWorldGenHelper.PlaceBlockColumn(blockAccessor, i, towerCornerZ + region.width - 1, bottomOfCurrentArchPiece, region.height, cobbleID, false);

                        if(currentPieceDepth != 1) {
                            pos.X = i;
                            pos.Y = bottomOfCurrentArchPiece - 1;
                            pos.Z = towerCornerZ;
                            blockAccessor.SetBlock(stairDW, pos);
                            pos.Z = towerCornerZ + region.width - 1;
                            blockAccessor.SetBlock(stairDW, pos);

                            bottomOfCurrentArchPiece += currentPieceDepth;
                            currentPieceDepth -= 1;
                        } else {
                            if(currentRun == 0) {
                                pos.X = i;
                                pos.Y = bottomOfCurrentArchPiece - 1;
                                pos.Z = towerCornerZ;
                                blockAccessor.SetBlock(stairDW, pos);
                                pos.Z = towerCornerZ + region.width - 1;
                                blockAccessor.SetBlock(stairDW, pos);
                            }

                            currentRun++;
                            if(currentRun == currentPieceRun) {
                                bottomOfCurrentArchPiece += currentPieceDepth;
                                currentPieceRun++;
                                currentRun = 0;
                            }
                        }

                        if(currentPieceDepth >= region.height - 3) {
                            break;
                        }
                    }
                }
            } else { //Build broken bridge ends and fallen rubble
                //Build Broken Tower
                for(int i = 0; i < region.width; i++) {
                    for(int k = 0; k < region.width; k++) {
                        if(i == 0 || i == region.width - 1 || k == 0 || k == region.width - 1) {
                            int towerHeight = random.NextInt(maxTowerStubSize - minTowerStubSize) + minTowerStubSize;
                            int yHeight = FishWorldGenHelper.GetGroundHeight(blockAccessor, i + towerCornerX, region.height, k + towerCornerZ);
                            FishWorldGenHelper.PlaceBlockColumnToGround(blockAccessor, i + towerCornerX, yHeight + towerHeight, k + towerCornerZ, cobbleID);
                        }
                    }
                }

                //Bridge Ends
                if(connectsNorth) {
                    //Fallen ruins
                    int rubbleAmount = random.NextInt(maxRubblePiles - minRubblePiles) + minRubblePiles;

                    for(int i = 0; i < rubbleAmount; i++) {
                        int lengthAlong = random.NextInt(CHUNK_SIZE / 2 - region.width / 2);
                        int size = random.NextInt(maxBridgeRubblePileSize - minBridgeRubblePileSize) + minBridgeRubblePileSize;

                        PlaceRandomPile(blockAccessor, random, towerCornerX + region.width / 2, region.height, baseZ + CHUNK_SIZE - 1 - lengthAlong, region.width, size, cobbleID);
                    }

                    //Bridge End
                    int residualLength1 = random.NextInt(residualLengthMax - residualLengthMin) + residualLengthMin;
                    int residualLength2 = random.NextInt(residualLengthMax - residualLengthMin) + residualLengthMin;

                    int sideDepth = 4;

                    //Create residual ends of bridge
                    for(int k = 0; k < Math.Max(residualLength1, residualLength2); k++) {
                        int depthLeft1 = (int) ((1 - Math.Max(0, k / (float) residualLength1)) * sideDepth);
                        int depthLeft2 = (int) ((1 - Math.Max(0, k / (float) residualLength2)) * sideDepth);

                        int currentZPos = baseZ + CHUNK_SIZE - 1 - k;

                        for(int j = 0; j < depthLeft1; j++) {
                            pos.X = towerCornerX;
                            pos.Y = region.height + 1 - j;
                            pos.Z = currentZPos;
                            blockAccessor.SetBlock(cobbleID, pos);
                        }

                        for(int j = 0; j < depthLeft2; j++) {
                            pos.X = towerCornerX + region.width - 1;
                            pos.Y = region.height + 1 - j;
                            pos.Z = currentZPos;
                            blockAccessor.SetBlock(cobbleID, pos);
                        }

                        if(depthLeft1 > 1) {
                            int woodAmount = random.NextInt(region.width / 2);
                            for(int i = 0; i < woodAmount; i++) {
                                pos.X = towerCornerX + 1 + woodAmount;
                                pos.Y = region.height;
                                pos.Z = currentZPos;
                                blockAccessor.SetBlock(agedPlankID, pos);
                            }
                        }

                        if(depthLeft2 > 1) {
                            int woodAmount = random.NextInt(region.width / 2);
                            for(int i = 0; i < woodAmount; i++) {
                                pos.X = towerCornerX + region.width - 1 - woodAmount;
                                pos.Y = region.height;
                                pos.Z = currentZPos;
                                blockAccessor.SetBlock(agedPlankID, pos);
                            }
                        }
                    }
                }

                if(connectsSouth) {
                    //Fallen ruins
                    int rubbleAmount = random.NextInt(maxRubblePiles - minRubblePiles) + minRubblePiles;

                    for(int i = 0; i < rubbleAmount; i++) {
                        int lengthAlong = random.NextInt(CHUNK_SIZE / 2 - region.width / 2);
                        int size = random.NextInt(maxBridgeRubblePileSize - minBridgeRubblePileSize) + minBridgeRubblePileSize;

                        PlaceRandomPile(blockAccessor, random, towerCornerX + region.width / 2, region.height, baseZ + lengthAlong, region.width, size, cobbleID);
                    }

                    int residualLength1 = random.NextInt(residualLengthMax - residualLengthMin) + residualLengthMin;
                    int residualLength2 = random.NextInt(residualLengthMax - residualLengthMin) + residualLengthMin;

                    int sideDepth = 4;

                    //Create residual ends of bridge
                    for(int k = 0; k < Math.Max(residualLength1, residualLength2); k++) {
                        int depthLeft1 = (int)((1 - Math.Max(0, k / (float)residualLength1)) * sideDepth);
                        int depthLeft2 = (int)((1 - Math.Max(0, k / (float)residualLength2)) * sideDepth);

                        int currentZPos = baseZ + k;

                        for(int j = 0; j < depthLeft1; j++) {
                            pos.X = towerCornerX;
                            pos.Y = region.height + 1 - j;
                            pos.Z = currentZPos;
                            blockAccessor.SetBlock(cobbleID, pos);
                        }

                        for(int j = 0; j < depthLeft2; j++) {
                            pos.X = towerCornerX + region.width - 1;
                            pos.Y = region.height + 1 - j;
                            pos.Z = currentZPos;
                            blockAccessor.SetBlock(cobbleID, pos);
                        }

                        if(depthLeft1 > 1) {
                            int woodAmount = random.NextInt(region.width / 2);
                            for(int i = 0; i < woodAmount; i++) {
                                pos.X = towerCornerX + 1 + woodAmount;
                                pos.Y = region.height;
                                pos.Z = currentZPos;
                                blockAccessor.SetBlock(agedPlankID, pos);
                            }
                        }

                        if(depthLeft2 > 1) {
                            int woodAmount = random.NextInt(region.width / 2);
                            for(int i = 0; i < woodAmount; i++) {
                                pos.X = towerCornerX + region.width - 1 - woodAmount;
                                pos.Y = region.height;
                                pos.Z = currentZPos;
                                blockAccessor.SetBlock(agedPlankID, pos);
                            }
                        }
                    }
                }

                if(connectsEast) {
                    //Fallen ruins
                    int rubbleAmount = random.NextInt(maxRubblePiles - minRubblePiles) + minRubblePiles;

                    for(int i = 0; i < rubbleAmount; i++) {
                        int lengthAlong = random.NextInt(CHUNK_SIZE / 2 - region.width / 2);
                        int size = random.NextInt(maxBridgeRubblePileSize - minBridgeRubblePileSize) + minBridgeRubblePileSize;

                        PlaceRandomPile(blockAccessor, random, baseX + CHUNK_SIZE - 1 - lengthAlong, region.height, towerCornerZ + region.width / 2, region.width, size, cobbleID);
                    }

                    int residualLength1 = random.NextInt(residualLengthMax - residualLengthMin) + residualLengthMin;
                    int residualLength2 = random.NextInt(residualLengthMax - residualLengthMin) + residualLengthMin;

                    int sideDepth = 4;

                    //Create residual ends of bridge
                    for(int i = 0; i < Math.Max(residualLength1, residualLength2); i++) {
                        int depthLeft1 = (int)((1 - Math.Max(0, i / (float)residualLength1)) * sideDepth);
                        int depthLeft2 = (int)((1 - Math.Max(0, i / (float)residualLength2)) * sideDepth);

                        int currentXPos = baseX + CHUNK_SIZE - 1 - i;

                        for(int j = 0; j < depthLeft1; j++) {
                            pos.X = currentXPos;
                            pos.Y = region.height + 1 - j;
                            pos.Z = towerCornerZ;
                            blockAccessor.SetBlock(cobbleID, pos);
                        }

                        for(int j = 0; j < depthLeft2; j++) {
                            pos.X = currentXPos;
                            pos.Y = region.height + 1 - j;
                            pos.Z = towerCornerZ + region.width - 1;
                            blockAccessor.SetBlock(cobbleID, pos);
                        }

                        if(depthLeft1 > 1) {
                            int woodAmount = random.NextInt(region.width / 2);
                            for(int k = 0; k < woodAmount; k++) {
                                pos.X = currentXPos;
                                pos.Y = region.height;
                                pos.Z = towerCornerZ + 1 + woodAmount;
                                blockAccessor.SetBlock(agedPlankID, pos);
                            }
                        }

                        if(depthLeft2 > 1) {
                            int woodAmount = random.NextInt(region.width / 2);
                            for(int k = 0; k < woodAmount; k++) {
                                pos.X = currentXPos;
                                pos.Y = region.height;
                                pos.Z = towerCornerZ + region.width - 1 - woodAmount;
                                blockAccessor.SetBlock(agedPlankID, pos);
                            }
                        }
                    }
                }

                if(connectsWest) {
                    //Fallen ruins
                    int rubbleAmount = random.NextInt(maxRubblePiles - minRubblePiles) + minRubblePiles;

                    for(int i = 0; i < rubbleAmount; i++) {
                        int lengthAlong = random.NextInt(CHUNK_SIZE / 2 - region.width / 2);
                        int size = random.NextInt(maxBridgeRubblePileSize - minBridgeRubblePileSize) + minBridgeRubblePileSize;

                        PlaceRandomPile(blockAccessor, random, baseX + lengthAlong, region.height, towerCornerZ + region.width / 2, region.width, size, cobbleID);
                    }

                    int residualLength1 = random.NextInt(residualLengthMax - residualLengthMin) + residualLengthMin;
                    int residualLength2 = random.NextInt(residualLengthMax - residualLengthMin) + residualLengthMin;

                    int sideDepth = 4;

                    //Create residual ends of bridge
                    for(int i = 0; i < Math.Max(residualLength1, residualLength2); i++) {
                        int depthLeft1 = (int)((1 - Math.Max(0, i / (float)residualLength1)) * sideDepth);
                        int depthLeft2 = (int)((1 - Math.Max(0, i / (float)residualLength2)) * sideDepth);

                        int currentXPos = baseX + i;

                        for(int j = 0; j < depthLeft1; j++) {
                            pos.X = currentXPos;
                            pos.Y = region.height + 1 - j;
                            pos.Z = towerCornerZ;
                            blockAccessor.SetBlock(cobbleID, pos);
                        }

                        for(int j = 0; j < depthLeft2; j++) {
                            pos.X = currentXPos;
                            pos.Y = region.height + 1 - j;
                            pos.Z = towerCornerZ + region.width - 1;
                            blockAccessor.SetBlock(cobbleID, pos);
                        }

                        if(depthLeft1 > 1) {
                            int woodAmount = random.NextInt(region.width / 2);
                            for(int k = 0; k < woodAmount; k++) {
                                pos.X = currentXPos;
                                pos.Y = region.height;
                                pos.Z = towerCornerZ + 1 + woodAmount;
                                blockAccessor.SetBlock(agedPlankID, pos);
                            }
                        }

                        if(depthLeft2 > 1) {
                            int woodAmount = random.NextInt(region.width / 2);
                            for(int k = 0; k < woodAmount; k++) {
                                pos.X = currentXPos;
                                pos.Y = region.height;
                                pos.Z = towerCornerZ + region.width - 1 - woodAmount;
                                blockAccessor.SetBlock(agedPlankID, pos);
                            }
                        }
                    }
                }
            }
        }

        private void PlaceRandomPile(IBlockAccessor blockAccessor, LCGRandom rand, int xCenter, int yBase, int zCenter, int xSize, int zSize, int id) {
            float xSizeSq = xSize * xSize;
            float zSizeSq = zSize * zSize;
            for(int i = xCenter - xSize / 2; i < xCenter + xSize / 2; i++) {
                for(int k = zCenter - zSize / 2; k < zCenter + zSize / 2; k++) {
                    float distanceFromCenter = ((i - xCenter) * (i - xCenter)) / xSizeSq + ((k - zCenter) * (k - zCenter)) / zSizeSq;

                    float chanceToBeat = (1 - distanceFromCenter) * rubbleBaseChance;

                    if(rand.NextFloat() < chanceToBeat) {
                        int pileSize = rand.NextInt(maxRubbleHeight - minRubbleHeight) + minRubbleHeight;

                        int yHeight = FishWorldGenHelper.GetGroundHeight(blockAccessor, i, yBase, k);
                        FishWorldGenHelper.PlaceBlockColumnToGround(blockAccessor, i, yHeight + pileSize, k, id);
                    }
                }
            }
        }

    }
}
