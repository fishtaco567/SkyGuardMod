using Vintagestory.API.Datastructures;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;
using System.Collections.Generic;
using System;

namespace progfish.WorldGen.Util {
    public class ChestItemRandomizer {

        public List<string> items;
        private ICoreServerAPI api;

        public ChestItemRandomizer(string[] items, int[] weights, ICoreServerAPI api) {
            if(items.Length == weights.Length) {
                this.items = new List<string>();

                for(int i = 0; i < items.Length; i++) {
                    int weight = weights[i];
                    for(int j = 0; j < weight; j++) {
                        this.items.Add(items[i]);
                    }
                } 
            }

            this.api = api;
        }

        public string RandomItem(LCGRandom random) {
            return items[random.NextInt(items.Count)];
        }

        public void PlaceItemsInChest(IBlockEntityContainer chest, int minItems, int maxItems, LCGRandom random) {
            Dictionary<string, ItemStack> stacks = new Dictionary<string, ItemStack>();

            int numItems = random.NextInt(maxItems - minItems) + minItems;
            
            for(int i = 0; i < numItems; i++) {
                var randomItem = RandomItem(random);
                
                if(stacks.ContainsKey(randomItem)) {
                    stacks[randomItem].StackSize++;
                } else {
                    var item = api.World.GetItem(new AssetLocation(randomItem));

                    if(item != null) {
                        stacks.Add(randomItem, new ItemStack(item));
                    }
                }
            }

            int curSlot = 0;
            foreach(var stack in stacks.Values) {
                curSlot = Math.Min(curSlot, chest.Inventory.Count - 1);

                var itemSlot = chest.Inventory[curSlot];
                itemSlot.Itemstack = stack;
                chest.Inventory.MarkSlotDirty(curSlot);
                curSlot++;
            }
        }

    }
}
