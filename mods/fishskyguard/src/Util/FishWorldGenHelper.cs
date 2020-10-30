using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace progfish.WorldGen.Util {
    public class FishWorldGenHelper {

        private static int[] otherCoordPairs = { 2, 0, 0, 1, 2, 1 };

        public static void PlaceBlockColumn(IBlockAccessor blockAccessor, int xPos, int zPos, int startY, int endY, int id, bool replaceBlocks) {
            BlockPos pos = new BlockPos(xPos, startY, zPos);

            for(int j = startY; j <= endY; j++) {
                pos.Y = j;

                if(replaceBlocks || blockAccessor.GetBlock(pos).IsReplacableBy(blockAccessor.GetBlock(id))) {
                    blockAccessor.SetBlock(id, pos);
                }
            }
        }

        public static int PlaceBlockColumnToGround(IBlockAccessor blockAccessor, int xPos, int yStart, int zPos, int id) {
            BlockPos pos = new BlockPos(xPos, yStart, zPos);

            while(pos.Y > 0 && blockAccessor.GetBlock(pos).IsReplacableBy(blockAccessor.GetBlock(id))) {
                blockAccessor.SetBlock(id, pos);

                pos.Y--;
            }

            return pos.Y + 1;
        }

        public static int GetGroundHeight(IBlockAccessor blockAccessor, int xPos, int yStart, int zPos) {
            BlockPos pos = new BlockPos(xPos, yStart, zPos);

            while(pos.Y > 0 && blockAccessor.GetBlockId(pos) == 0) {
                pos.Y--;
            }

            return pos.Y + 1;
        }

        public static void PlaceArea(IBlockAccessor blockAccessor, int xStart, int yStart, int zStart, int xEnd, int yEnd, int zEnd, int id, bool replaceBlocks) {
            BlockPos pos = new BlockPos(xStart, yStart, zStart);

            for(int i = xStart; i <= xEnd; i++) {
                for(int j = yStart; j <= yEnd; j++) {
                    for(int k = zStart; k <= zEnd; k++) {
                        pos.X = i;
                        pos.Y = j;
                        pos.Z = k;

                        if(replaceBlocks || blockAccessor.GetBlock(pos).IsReplacableBy(blockAccessor.GetBlock(id))) {
                            blockAccessor.SetBlock(id, pos);
                        }
                    }
                }
            }
        }

        public static void PlaceSpheroid(IBlockAccessor blockAccessor, int xPos, int yPos, int zPos, int xRadius, int yRadius, int zRadius, int id, bool replaceBlocks) {
            BlockPos pos = new BlockPos(xPos, yPos, zPos);

            //Calculate the bounding box of the spheroid
            int xMin = xPos - xRadius;
            int xMax = xPos + xRadius;
            int yMin = yPos - yRadius;
            int yMax = yPos + yRadius;
            int zMin = zPos - zRadius;
            int zMax = zPos + zRadius;

            //Square the radii first to save computational time
            float xRadiusSq = xRadius * xRadius;
            float yRadiusSq = yRadius * yRadius;
            float zRadiusSq = zRadius * zRadius;

            //Iterate the bounding box of the spheroid
            for(int i = xMin; i <= xMax; i++) {
                for(int j = yMin; j <= yMax; j++) {
                    for(int k = zMin; k <= zMax; k++) {
                        //Get the distance from the "origin" of the spheroid"
                        int iDist = xPos - i;
                        int jDist = yPos - j;
                        int kDist = zPos - k;

                        //Formula for a spheroid x^2 / A^2 + y^2 / B^2 + z^2 / C^2 <= 1
                        if((((iDist * iDist) / xRadiusSq) + ((jDist * jDist) / yRadiusSq) + ((kDist * kDist) / zRadiusSq)) <= 1) {
                            pos.X = i;
                            pos.Y = j;
                            pos.Z = k;

                            if(replaceBlocks || blockAccessor.GetBlock(pos).IsReplacableBy(blockAccessor.GetBlock(id))) {
                                blockAccessor.SetBlock(id, pos);
                            }
                        }
                    }
                }
            }
        }

        public static void PlaceLine(IBlockAccessor blockAccessor, int xStart, int yStart, int zStart, int xEnd, int yEnd, int zEnd, int id, bool replaceBlocks) {
            BlockPos pos = new BlockPos(xStart, yStart, zStart);

            var initialPosition = new int[3];
            var finalPosition = new int[3];

            initialPosition[0] = xStart;
            initialPosition[1] = yStart;
            initialPosition[2] = zStart;

            finalPosition[0] = xEnd;
            finalPosition[1] = yEnd;
            finalPosition[2] = zEnd;

            int[] direction = {
                0, 0, 0
            };

            int maxGradDir = 0;
            for(int i = 0; i < 3; i++) {
                direction[i] = finalPosition[i] - initialPosition[i];
                if(Math.Abs(direction[i]) > Math.Abs(direction[maxGradDir])) {
                    maxGradDir = i;
                }
            }

            if(direction[maxGradDir] == 0) {
                return;
            }

            int secondCoord = otherCoordPairs[maxGradDir];
            int thirdCoord = otherCoordPairs[maxGradDir + 3];
            int stepFirstCoord;

            if(direction[maxGradDir] > 0) {
                stepFirstCoord = 1;
            } else {
                stepFirstCoord = -1;
            }

            float secondCoordGradient = (float)direction[secondCoord] / (float)direction[maxGradDir];
            float thirdCoordGradient = (float)direction[thirdCoord] / (float)direction[maxGradDir];

            int[] curPosInLine = {
                initialPosition[0], initialPosition[1], initialPosition[2]
            };

            int endCoord = direction[maxGradDir] + stepFirstCoord;

            for(int firstCoord = 0; firstCoord != endCoord; firstCoord += stepFirstCoord) {
                curPosInLine[maxGradDir] = (int)Math.Floor((float)(initialPosition[maxGradDir] + firstCoord) + 0.5f);
                curPosInLine[secondCoord] = (int)Math.Floor((float)initialPosition[secondCoord] + (float)firstCoord * secondCoordGradient + 0.5f);
                curPosInLine[thirdCoord] = (int)Math.Floor((float)initialPosition[thirdCoord] + (float)firstCoord * thirdCoordGradient + 0.5f);

                pos.X = curPosInLine[0];
                pos.Y = curPosInLine[1];
                pos.Z = curPosInLine[2];

                if(replaceBlocks || blockAccessor.GetBlock(pos).IsReplacableBy(blockAccessor.GetBlock(id))) {
                    blockAccessor.SetBlock(id, pos);
                }
            }

        }
    }
}
