﻿using System.Collections.Generic;
using System.Linq;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PetsOverhaul.Systems
{
    public class PlayerPlacedBlockList : ModSystem
    {
        public static List<Point16> placedBlocksByPlayer = new();
        public override void SaveWorldData(TagCompound tag)
        {
            tag.Add("placedBlocksByPlayer", placedBlocksByPlayer);
        }
        public override void LoadWorldData(TagCompound tag)
        {
            if (tag.TryGet("placedBlocksByPlayer", out List<Point16> listOfPlacedBlocks))
            {
                placedBlocksByPlayer = listOfPlacedBlocks;
                placedBlocksByPlayer = placedBlocksByPlayer.Distinct().ToList(); //Removes duplicate entries
            }
        }
    }
}
