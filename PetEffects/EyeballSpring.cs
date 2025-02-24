﻿using PetsOverhaul.Config;
using PetsOverhaul.Systems;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PetsOverhaul.PetEffects
{
    public sealed class EyeballSpring : PetEffect
    {
        public override PetClasses PetClassPrimary => PetClasses.Mobility;
        public float acceleration = 0.15f;
        public float jumpBoost = 4.50f;
        public float ascentPenaltyMult = 0.55f;
        public override void PostUpdateMiscEffects()
        {
            if (Pet.PetInUseWithSwapCd(ItemID.EyeSpring))
            {
                Player.jumpSpeedBoost += jumpBoost;
            }
        }
        public override void PostUpdateRunSpeeds()
        {
            if (Pet.PetInUseWithSwapCd(ItemID.EyeSpring))
            {
                if (Player.jump > 0 && Pet.jumpRegistered == false)
                {
                    if (ModContent.GetInstance<PetPersonalization>().AbilitySoundEnabled)
                    {
                        SoundEngine.PlaySound(SoundID.Item56 with { Volume = 0.5f, Pitch = -0.3f, PitchVariance = 0.1f }, Player.Center);
                    }

                    Pet.jumpRegistered = true;
                }
                Player.runAcceleration *= acceleration + 1f;
                Player.jumpSpeedBoost += jumpBoost;
            }
        }
    }
    public sealed class EyeballSpringWing : GlobalItem
    {
        public override bool InstancePerEntity => true;
        public override void VerticalWingSpeeds(Item item, Player player, ref float ascentWhenFalling, ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
        {
            if (player.TryGetModPlayer(out EyeballSpring eyeballs) && player.GetModPlayer<GlobalPet>().PetInUseWithSwapCd(ItemID.EyeSpring))
            {
                maxAscentMultiplier *= eyeballs.ascentPenaltyMult;
            }

        }
    }
    public sealed class EyeSpring : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.EyeSpring;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (ModContent.GetInstance<PetPersonalization>().EnableTooltipToggle && !PetKeybinds.PetTooltipHide.Current)
            {
                return;
            }

            EyeballSpring eyeballSpring = Main.LocalPlayer.GetModPlayer<EyeballSpring>();
            tooltips.Add(new(Mod, "Tooltip0", Language.GetTextValue("Mods.PetsOverhaul.PetItemTooltips.EyeSpring")
                .Replace("<class>", PetTextsColors.ClassText(eyeballSpring.PetClassPrimary, eyeballSpring.PetClassSecondary))
                        .Replace("<jumpBoost>", Math.Round(eyeballSpring.jumpBoost * 100, 2).ToString())
                        .Replace("<acceleration>", Math.Round(eyeballSpring.acceleration * 100, 2).ToString())
                        .Replace("<ascNerf>", eyeballSpring.ascentPenaltyMult.ToString())
                        ));
        }
    }
}
