﻿using Microsoft.Xna.Framework;
using PetsOverhaul.Config;
using PetsOverhaul.Systems;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PetsOverhaul.PetEffects
{
    public sealed class TinyFishron : PetEffect
    {
        public float fishingPowerPenalty = 0.5f;
        public float fpPerQuest = 0.002f;
        public float maxQuestPower = 0.5f;
        public int bobberChance = 105;
        public int stackChance = 10;
        public float multiplier = 1f;

        public override PetClasses PetClassPrimary => PetClasses.Fishing;
        public override void GetFishingLevel(Item fishingRod, Item bait, ref float fishingLevel)
        {
            if (Pet.PetInUse(ItemID.DukeFishronPetItem))
            {
                float fishingPowerMult = 0;
                fishingPowerMult += fishingPowerPenalty;
                if (Player.anglerQuestsFinished * fpPerQuest > maxQuestPower)
                {
                    fishingPowerMult += maxQuestPower;
                }
                else
                {
                    fishingPowerMult += Player.anglerQuestsFinished * fpPerQuest;
                }

                fishingLevel += fishingPowerMult;
            }
        }
        public override void ModifyCaughtFish(Item fish)
        {
            if (Pet.PetInUse(ItemID.DukeFishronPetItem) && fish.maxStack != 1)
            {
                for (int i = 0; i < GlobalPet.Randomizer(stackChance + (int)(Player.fishingSkill * multiplier) * fish.stack); i++)
                {
                    Player.QuickSpawnItem(GlobalPet.GetSource_Pet(EntitySourcePetIDs.FishingItem), fish.type, 1);
                }
            }
        }
        public override bool Shoot(Item item, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (Pet.PetInUse(ItemID.DukeFishronPetItem) && item.fishingPole > 0)
            {
                for (int i = 0; i < GlobalPet.Randomizer(bobberChance); i++)
                {
                    Vector2 bobberSpeed = velocity + new Vector2(Main.rand.NextFloat(-50f, 50f) * 0.05f, Main.rand.NextFloat(-50f, 50f) * 0.05f);
                    Projectile petProjectile = Projectile.NewProjectileDirect(source, position, bobberSpeed, ProjectileID.FishingBobber, 0, 0f, Player.whoAmI);
                }
            }
            return base.Shoot(item, source, position, velocity, type, damage, knockback);
        }
    }
    public sealed class DukeFishronPetItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.DukeFishronPetItem;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (ModContent.GetInstance<PetPersonalization>().EnableTooltipToggle && !PetKeybinds.PetTooltipHide.Current)
            {
                return;
            }

            TinyFishron tinyFishron = Main.LocalPlayer.GetModPlayer<TinyFishron>();
            tooltips.Add(new(Mod, "Tooltip0", Language.GetTextValue("Mods.PetsOverhaul.PetItemTooltips.DukeFishronPetItem")
                .Replace("<class>", PetTextsColors.ClassText(tinyFishron.PetClassPrimary, tinyFishron.PetClassSecondary))
                        .Replace("<baseMult>", tinyFishron.fishingPowerPenalty.ToString())
                        .Replace("<anglerFishingPower>", tinyFishron.fpPerQuest.ToString())
                        .Replace("<flatChance>", tinyFishron.stackChance.ToString())
                        .Replace("<fishingPowerChance>", Math.Round(tinyFishron.multiplier * 100, 2).ToString())
                        .Replace("<bobberChance>", tinyFishron.bobberChance.ToString())
                        .Replace("<anglerQuests>", Main.LocalPlayer.anglerQuestsFinished.ToString())
                        .Replace("<currentAnglerWithBaseMult>", Math.Round(Main.LocalPlayer.anglerQuestsFinished * tinyFishron.fpPerQuest + tinyFishron.fishingPowerPenalty, 2).ToString())
                        ));
        }
    }
}
