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
    public sealed class SpiderBrain : PetEffect
    {
        public int lifePool = 0;
        public float lifePoolMaxPerc = 0.3f;
        public int cdToAddToPool = 120;
        public float lifestealAmount = 0.035f;

        public override PetClasses PetClassPrimary => PetClasses.Defensive;
        public override void PreUpdate()
        {
            if (Pet.PetInUse(ItemID.BrainOfCthulhuPetItem))
            {
                Pet.SetPetAbilityTimer(cdToAddToPool);
            }
        }
        public override void PreUpdateBuffs()
        {
            if (Pet.PetInUse(ItemID.BrainOfCthulhuPetItem) && Pet.timer <= 0 && lifePool <= Player.statLifeMax2 * lifePoolMaxPerc)
            {
                lifePool++;
                Pet.timer = Pet.timerMax;
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Pet.PetInUseWithSwapCd(ItemID.BrainOfCthulhuPetItem) && GlobalPet.LifestealCheck(target))
            {
                int decreaseFromPool = Pet.PetRecovery(damageDone, lifestealAmount, doHeal: false);
                if (decreaseFromPool >= lifePool)
                {
                    Pet.PetRecovery(lifePool, 1f);
                    lifePool = 0;
                }
                else
                {
                    lifePool -= decreaseFromPool;
                    Pet.PetRecovery(decreaseFromPool, 1f);
                }
            }
        }
    }
    public sealed class BrainOfCthulhuPetItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.BrainOfCthulhuPetItem;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (ModContent.GetInstance<PetPersonalization>().EnableTooltipToggle && !PetKeybinds.PetTooltipHide.Current)
            {
                return;
            }

            SpiderBrain spiderBrain = Main.LocalPlayer.GetModPlayer<SpiderBrain>();
            tooltips.Add(new(Mod, "Tooltip0", Language.GetTextValue("Mods.PetsOverhaul.PetItemTooltips.BrainOfCthulhuPetItem")
                .Replace("<class>", PetTextsColors.ClassText(spiderBrain.PetClassPrimary, spiderBrain.PetClassSecondary))
                        .Replace("<lifesteal>", Math.Round(spiderBrain.lifestealAmount * 100, 2).ToString())
                        .Replace("<maxPool>", Math.Round(spiderBrain.lifePoolMaxPerc * 100, 2).ToString())
                        .Replace("<healthRecovery>", Math.Round(spiderBrain.cdToAddToPool / 60f, 2).ToString())
                        .Replace("<pool>", spiderBrain.lifePool.ToString())
                        ));
        }
    }
}
