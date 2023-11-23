﻿using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using PetsOverhaul.Systems;

namespace PetsOverhaul.Items
{
    public class PrismaticOptic : ModItem
    {
        public override void SetStaticDefaults()
        {
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(15, 4));
            ItemID.Sets.AnimatesAsSoul[Item.type] = true;
        }
        public override void SetDefaults()
        {
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Master;
            Item.value = 0;
            Item.master = true;
            Item.width = 42;
            Item.height = 42;
            Item.alpha = 100;
        }
        public override void PostUpdate()
        {
            Lighting.AddLight(Item.Center,GlobalPet.maxQuality.ToVector3() *0.7f);
        }
    }
}
