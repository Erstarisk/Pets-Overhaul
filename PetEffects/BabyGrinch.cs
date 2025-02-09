﻿using PetsOverhaul.Config;
using PetsOverhaul.NPCs;
using PetsOverhaul.Systems;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PetsOverhaul.PetEffects
{
    public sealed class BabyGrinch : PetEffect
    {
        public float winterDmg = 0.15f;
        public int winterCrit = 10;
        public float grinchSlow = 1f;
        public int grinchRange = 480;

        public override PetClasses PetClassPrimary => PetClasses.Utility;
        public override PetClasses PetClassSecondary => PetClasses.Offensive;
        public override void ModifyWeaponDamage(Item item, ref StatModifier damage)
        {
            if (Pet.PetInUseWithSwapCd(ItemID.BabyGrinchMischiefWhistle))
            {
                if (item.netID == ItemID.ChristmasTreeSword || item.netID == ItemID.Razorpine || item.netID == ItemID.ElfMelter || item.netID == ItemID.ChainGun || item.netID == ItemID.BlizzardStaff || item.netID == ItemID.SnowmanCannon || item.netID == ItemID.NorthPole)
                {
                    damage += winterDmg;
                }
            }
        }
        public override void ModifyWeaponCrit(Item item, ref float crit)
        {
            if (Pet.PetInUseWithSwapCd(ItemID.BabyGrinchMischiefWhistle))
            {
                if (item.netID == ItemID.ChristmasTreeSword || item.netID == ItemID.Razorpine || item.netID == ItemID.ElfMelter || item.netID == ItemID.ChainGun || item.netID == ItemID.BlizzardStaff || item.netID == ItemID.SnowmanCannon || item.netID == ItemID.NorthPole)
                {
                    crit += winterCrit;
                }
            }
        }
        public override void PostUpdateMiscEffects()
        {
            if (Pet.PetInUseWithSwapCd(ItemID.BabyGrinchMischiefWhistle))
            {
                Player.resistCold = true;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active && Player.Distance(npc.Center) < grinchRange)
                    {
                        NpcPet.AddSlow(new NpcPet.PetSlow(grinchSlow, 1, PetSlowIDs.Grinch), npc);
                    }
                }
            }
        }
    }
    public sealed class BabyGrinchMischiefWhistle : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.BabyGrinchMischiefWhistle;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (ModContent.GetInstance<PetPersonalization>().EnableTooltipToggle && !PetKeybinds.PetTooltipHide.Current)
            {
                return;
            }

            BabyGrinch babyGrinch = Main.LocalPlayer.GetModPlayer<BabyGrinch>();
            tooltips.Add(new(Mod, "Tooltip0", Language.GetTextValue("Mods.PetsOverhaul.PetItemTooltips.BabyGrinchMischiefWhistle")
                .Replace("<class>", PetTextsColors.ClassText(babyGrinch.PetClassPrimary, babyGrinch.PetClassSecondary))
                .Replace("<slowAmount>", Math.Round(babyGrinch.grinchSlow * 100, 2).ToString())
                .Replace("<slowRange>", Math.Round(babyGrinch.grinchRange / 16f, 2).ToString())
                .Replace("<dmg>", Math.Round(babyGrinch.winterDmg * 100, 2).ToString())
                .Replace("<crit>", babyGrinch.winterCrit.ToString())
            ));
        }
    }
}

