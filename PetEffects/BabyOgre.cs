﻿using Terraria;
using Terraria.ID;
using PetsOverhaul.Systems;
using Terraria.ModLoader;

namespace PetsOverhaul.PetEffects
{
    sealed public class BabyOgre : ModPlayer
    {
        GlobalPet Pet { get => Player.GetModPlayer<GlobalPet>(); }
        public float dr = 0.1f;
        public float nonMeleedmg = 0.2f;
        public int crit = 20;
        public float defMult = 1.25f;
        public float movespdNerf = 0.5f;
        public float healthIncrease = 0.25f;
        public float atkSpdMult = 0.3f;
        public float horizontalMult = 0.8f;
        public float verticalMult = 0.7f;
        public float trueMeleeMultipliers = 3.5f;
        public override void PostUpdateEquips()
        {
            if (Pet.PetInUseWithSwapCd(ItemID.DD2OgrePetItem) && Player.mount.Active == false)
            {
                Player.endurance += dr;
                Player.GetDamage<GenericDamageClass>() -= nonMeleedmg;
                Player.statDefense *= defMult;
                Player.GetDamage<MeleeDamageClass>() += nonMeleedmg;
                Player.wingTimeMax /= 2;
                Player.moveSpeed *= movespdNerf;
                Player.maxRunSpeed *= movespdNerf;
                Player.noKnockback = true;
                if (Main.masterMode == true)
                {
                    healthIncrease = Player.statLifeMax2 * 0.75f;
                }
                else if (Main.expertMode == true)
                {
                    healthIncrease = Player.statLifeMax2 * 0.50f;
                }
                else
                {
                    healthIncrease = Player.statLifeMax2 * 0.25f;
                }

                Player.statLifeMax2 += (int)healthIncrease;
            }
        }
        public override float UseSpeedMultiplier(Item item)
        {
            if (Pet.PetInUseWithSwapCd(ItemID.DD2OgrePetItem) && item.noMelee == false && Player.mount.Active == false && item.CountsAsClass<MeleeDamageClass>())
            {
                return atkSpdMult;
            }
            else
            {
                return 1f;
            }
        }
        public override void ModifyItemScale(Item item,  ref float scale)
        {
            if (Pet.PetInUseWithSwapCd(ItemID.DD2OgrePetItem) && item.noMelee == false && Player.mount.Active == false && item.CountsAsClass<MeleeDamageClass>())
            {
                scale *= trueMeleeMultipliers;
                item.useTurn = false;
            }
        }
        public override void ModifyWeaponDamage(Item item,  ref StatModifier damage)
        {

            if (Pet.PetInUseWithSwapCd(ItemID.DD2OgrePetItem) && item.noMelee == false && Player.mount.Active == false && item.CountsAsClass<MeleeDamageClass>())
            {
                damage *= trueMeleeMultipliers;
            }
        }
        public override void ModifyWeaponKnockback(Item item,  ref StatModifier knockback)
        {
            if (Pet.PetInUseWithSwapCd(ItemID.DD2OgrePetItem) && item.noMelee == false && Player.mount.Active == false && item.CountsAsClass<MeleeDamageClass>())
            {
                knockback *= trueMeleeMultipliers;
            }
        }
        public override void ModifyWeaponCrit(Item item, ref float crit)
        {
            if (Pet.PetInUseWithSwapCd(ItemID.DD2OgrePetItem) && item.noMelee == false && Player.mount.Active == false && item.CountsAsClass<MeleeDamageClass>())
            {
                crit += crit;
            }
        }
    }
    sealed public class BabyOgreWing : GlobalItem
    {
        public override bool InstancePerEntity => true;
        public override void HorizontalWingSpeeds(Item item, Player player, ref float speed, ref float acceleration)
        {
            if (player.GetModPlayer<GlobalPet>().PetInUseWithSwapCd(ItemID.DD2OgrePetItem))
            {
                speed *= player.GetModPlayer<BabyOgre>().horizontalMult;
                acceleration *= player.GetModPlayer<BabyOgre>().horizontalMult;
            }
        }
        public override void VerticalWingSpeeds(Item item, Player player, ref float ascentWhenFalling, ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
        {
            if (player.GetModPlayer<GlobalPet>().PetInUseWithSwapCd(ItemID.DD2OgrePetItem))
            {
                maxAscentMultiplier *= player.GetModPlayer<BabyOgre>().verticalMult;
                maxCanAscendMultiplier *= player.GetModPlayer<BabyOgre>().verticalMult;
                ascentWhenRising *= player.GetModPlayer<BabyOgre>().verticalMult;
            }
        }
    }
}
