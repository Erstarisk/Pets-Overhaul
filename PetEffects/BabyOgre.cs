﻿using PetsOverhaul.Config;
using PetsOverhaul.Systems;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PetsOverhaul.PetEffects
{
    public sealed class BabyOgre : PetEffect
    {
        public override PetClasses PetClassPrimary => PetClasses.Melee;
        public override PetClasses PetClassSecondary => PetClasses.Defensive;
        public float dr = 0.02f;
        public float nonMeleedmg = 0.2f;
        public int crit = 20;
        public float defMult = 1.15f;
        public float movespdNerf = 0.6f;
        public float healthIncrease = 0.05f;
        public float atkSpdMult = 0.33f;
        public float horizontalMult = 0.8f;
        public float verticalMult = 0.7f;
        public float trueMeleeMultipliers = 2.75f;

        public override void PostUpdateMiscEffects()
        {
            if (Main.masterMode == true)
            {
                healthIncrease = 0.80f;
                dr = 0.08f;
                defMult = 1.35f;
            }
            else if (Main.expertMode == true)
            {
                healthIncrease = 0.20f;
                dr = 0.04f;
                defMult = 1.25f;
            }
            else
            {
                healthIncrease = 0.05f;
                dr = 0.02f;
                defMult = 1.15f;
            }

            if (Pet.PetInUseWithSwapCd(ItemID.DD2OgrePetItem) && Player.mount.Active == false)
            {
                Player.endurance += dr;
                Player.GetDamage<GenericDamageClass>() -= nonMeleedmg;
                Player.statDefense *= defMult;
                Player.GetDamage<MeleeDamageClass>() += nonMeleedmg;
                Player.wingTimeMax /= 2;
                Player.noKnockback = true;
                Player.statLifeMax2 += (int)(healthIncrease * Player.statLifeMax2);
                Player.moveSpeed *= movespdNerf;
            }
        }
        public override float UseSpeedMultiplier(Item item)
        {
            if (Pet.PetInUseWithSwapCd(ItemID.DD2OgrePetItem) && item.noMelee == false && Player.mount.Active == false && item.CountsAsClass<MeleeDamageClass>())
            {
                return atkSpdMult;
            }
            return base.UseSpeedMultiplier(item);
        }
        public override void ModifyItemScale(Item item, ref float scale)
        {
            if (Pet.PetInUseWithSwapCd(ItemID.DD2OgrePetItem) && item.noMelee == false && Player.mount.Active == false && item.CountsAsClass<MeleeDamageClass>())
            {
                scale *= trueMeleeMultipliers;
                item.useTurn = false;
            }
        }
        public override void ModifyWeaponDamage(Item item, ref StatModifier damage)
        {

            if (Pet.PetInUseWithSwapCd(ItemID.DD2OgrePetItem) && item.noMelee == false && Player.mount.Active == false && item.CountsAsClass<MeleeDamageClass>())
            {
                damage *= trueMeleeMultipliers;
            }
        }
        public override void ModifyWeaponKnockback(Item item, ref StatModifier knockback)
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
    public sealed class BabyOgreWing : GlobalItem
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
    public sealed class DD2OgrePetItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.DD2OgrePetItem;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (ModContent.GetInstance<PetPersonalization>().EnableTooltipToggle && !PetKeybinds.PetTooltipHide.Current)
            {
                return;
            }

            BabyOgre babyOgre = Main.LocalPlayer.GetModPlayer<BabyOgre>();
            tooltips.Add(new(Mod, "Tooltip0", Language.GetTextValue("Mods.PetsOverhaul.PetItemTooltips.DD2OgrePetItem")
                .Replace("<class>", PetTextsColors.ClassText(babyOgre.PetClassPrimary, babyOgre.PetClassSecondary))
                .Replace("<moveSpdNerf>", babyOgre.movespdNerf.ToString())
                .Replace("<atkSpdNerf>", babyOgre.atkSpdMult.ToString())
                .Replace("<dmgNerf>", Math.Round(babyOgre.nonMeleedmg * 100, 2).ToString())
                .Replace("<horizontalNerf>", babyOgre.horizontalMult.ToString())
                .Replace("<verticalNerf>", babyOgre.verticalMult.ToString())
                .Replace("<trueMeleeMults>", babyOgre.trueMeleeMultipliers.ToString())
                .Replace("<trueMeleeCrit>", babyOgre.crit.ToString())
                .Replace("<healthIncrease>", Math.Round(babyOgre.healthIncrease * 100, 2).ToString())
                .Replace("<defMult>", babyOgre.defMult.ToString())
                .Replace("<damageReduction>", Math.Round(babyOgre.dr * 100, 2).ToString())
            ));
        }

    }
}
