﻿using Microsoft.Xna.Framework;
using PetsOverhaul.Buffs;
using PetsOverhaul.Config;
using PetsOverhaul.Items;
using PetsOverhaul.Projectiles;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PetsOverhaul.Systems
{
    /// <summary>
    /// ModPlayer class that contains useful Methods and fields for Pet implementation.
    /// </summary>
    sealed public class GlobalPet : ModPlayer
    {
        public bool[] burnDebuffs = BuffID.Sets.Factory.CreateBoolSet(false, BuffID.Burning, BuffID.OnFire, BuffID.OnFire3, BuffID.Frostburn, BuffID.CursedInferno, BuffID.ShadowFlame, BuffID.Frostburn2);
        public Color skin;
        public bool skinColorChanged = false;
        public static List<int> pool = new(100000);
        public int shieldAmount = 0;
        public int shieldTimer = -1;
        public bool jumpRegistered = false;

        /// <summary>
        /// Adds the specified ItemID to the 'pool' list with amount of specified weight. Capacity is 100000.
        /// </summary>
        public int petSwapCooldown = 600;
        internal int previousPetItem = 0;
        /// <summary>
        /// This field ticks down every frame in PreUpdate() hook. Does not go below -1. Plays 'cooldown refreshment' sound effect upon reaching 0 amd displays Timer while higher than 0. Usually is recommended to use Mod's timer mechanic for timers that the Player should be aware of.
        /// </summary>
        public int timer = -1;
        /// <summary>
        /// Use this field to set how long the Pet's cooldown will be.
        /// </summary>
        public int timerMax = 0;
        /// <summary>
        /// Increase this value to reduce ability cooldowns. Eg. 0.1f increases how fast ability will return by 10%. Negative values will increase the cooldowns. Negative is capped at -0.9f. Do not use this to directly reduce a cooldown, use the timer field instead. Ability Haste reduces timerMax with a more balanced calculation.
        /// </summary>
        public float abilityHaste = 0;
        public static void ItemWeight(int itemId, int weight)
        {
            for (int i = 0; i < weight; i++)
            {
                pool.Add(itemId);
            }
        }
        /// <summary>
        /// Runs all of the standart OnPickup's checks for the Pet to work with no problems.
        /// </summary>
        public bool PickupChecks(Item item, int petitemid, ItemPet itemPet)
        {
                if (itemPet.pickedUpBefore == false && Player.CanPullItem(item, Player.ItemSpace(item)) && PetInUse(petitemid) && item.maxStack != 1)
                    return true;
                else
            return false;
        }
        /// <summary>
        /// Checks if the given Pet Item is in use and checks if pet has been lately swapped or not.
        /// </summary>
        public bool PetInUseWithSwapCd(int petItemID)
        {

            if (Player.miscEquips[0].type == petItemID && Player.HasBuff(ModContent.BuffType<ObliviousPet>()) == false)
                return true;
            else
                return false;
        }
        /// <summary>
        /// Checks if the given Pet Item is in use without being affected by swapping cooldown.
        /// </summary>
        public bool PetInUse(int petItemID)
        {
            if (Player.miscEquips[0].type == petItemID)
                return true;
            else
                return false;
        }
        public override void SaveData(TagCompound tag)
        {
            tag.Add("SkinColor", skin);
            tag.Add("SkinColorChanged", skinColorChanged);
        }
        public override void LoadData(TagCompound tag)
        {
            skin = tag.Get<Color>("SkinColor");
            skinColorChanged = tag.GetBool("SkinColorChanged");
        }
        public bool LifestealCheck(NPC npc)
        {
            if (npc.friendly || npc.SpawnedFromStatue || npc.type == NPCID.TargetDummy)
                return false;
            else
                return true;
        }
        /// <summary>
        /// percentageAmount% of baseAmount is converted to healing, non converted amount can still grant +1, depending on a roll. If manaSteal is set to true, 
        /// everything same will be done for Mana instead of Health. Example: Lifesteal(215, 0.05f) will heal you for 10 health and 75% chance to heal 11 health 
        /// instead. It returns its actual amount before MaxHealth/LifeSteal limitations, meaning you can combine it with another Lifesteal method to set the flatIncrease to something else. Lihzahrd uses 
        /// this with its %heallth healing alongside its regular Lifesteal. flatIncrease does not go into any calculations. Will show its actual value as recovered, 
        /// however, it will not increase the lifeSteal field or health/mana itself more than Maximum Health/mana cap or the lifeSteal field itself if respectLifeStealCap is true.
        /// Does not actually lifesteal if doLifesteal is set to false, so it can safely return an integer value.
        /// </summary>
        public int Lifesteal(int baseAmount, float percentageAmount, int flatIncrease = 0, bool manaSteal = false, bool respectLifeStealCap = true, bool doLifesteal = true)
        {
            float num = baseAmount * percentageAmount;
            int calculatedAmount = (int)num;
            if (Main.rand.NextFloat(0, 1) < num % 1)
                calculatedAmount++;
            calculatedAmount += flatIncrease;
            num = calculatedAmount;
            if (calculatedAmount > 0 && doLifesteal == true)
            {
                if (manaSteal == false)
                {
                    Player.HealEffect(calculatedAmount);
                    if (calculatedAmount > Player.statLifeMax2 - Player.statLife)
                        calculatedAmount = Player.statLifeMax2 - Player.statLife;
                    if (respectLifeStealCap == true)
                    {
                        if (calculatedAmount > Player.lifeSteal)
                            calculatedAmount = (int)Player.lifeSteal;

                        if (Player.lifeSteal > 0)
                        {
                            Player.statLife += calculatedAmount;
                            Player.lifeSteal -= calculatedAmount;
                        }
                    }
                    else
                    {
                        Player.statLife += calculatedAmount;
                    }
                }
                else
                {
                    Player.ManaEffect(calculatedAmount);
                    if (calculatedAmount > Player.statManaMax2 - Player.statMana)
                        calculatedAmount = Player.statManaMax2 - Player.statMana;
                    Player.statMana += calculatedAmount;
                }
            }

            return (int)num;
        }
        public static bool KingSlimePetActive(out Player owner)
        {
            bool anyPlayerHasSlimePet = false;
            owner = null;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player plr = Main.player[i];
                if (plr.active && plr.whoAmI != 255 && plr.miscEquips[0].type == ItemID.KingSlimePetItem)
                {
                    anyPlayerHasSlimePet = true;
                    owner = plr;
                    break;
                }
            }
            if (anyPlayerHasSlimePet == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool QueenSlimePetActive(out Player owner)
        {
            bool anyPlayerHasSlimePet = false;
            owner = null;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player plr = Main.player[i];
                if (plr.active && plr.whoAmI != 255 && plr.miscEquips[0].type == ItemID.QueenSlimePetItem)
                {
                    anyPlayerHasSlimePet = true;
                    owner = plr;
                    break;
                }
            }
            if (anyPlayerHasSlimePet == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool DualSlimePetActive(out Player owner)
        {
            bool anyPlayerHasSlimePet = false;
            owner = null;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player plr = Main.player[i];
                if (plr.active && plr.whoAmI != 255 && plr.miscEquips[0].type == ItemID.ResplendentDessert)
                {
                    anyPlayerHasSlimePet = true;
                    owner = plr;
                    break;
                }
            }
            if (anyPlayerHasSlimePet == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (ModContent.GetInstance<Personalization>().MoreDifficult == false)
                modifiers.FinalDamage *= 0.95f;
        }
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (ModContent.GetInstance<Personalization>().MoreDifficult == false)
                modifiers.FinalDamage *= 1.05f;
            modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) =>
        {
            if (info.Damage > shieldAmount && shieldAmount <= 0 == false)
            {
                CombatText.NewText(Player.Hitbox, Color.Cyan, -shieldAmount, true);
                if (ModContent.GetInstance<Personalization>().AbilitySoundDisabled == false)
                    SoundEngine.PlaySound(SoundID.NPCDeath43 with { PitchVariance = 0.4f, Pitch = -0.8f, Volume = 0.2f }, Player.position);
                info.Damage -= shieldAmount;
                shieldAmount = 0;
            }
        };
        }
        public override bool ConsumableDodge(Player.HurtInfo info)
        {
            if (info.Damage <= shieldAmount && shieldAmount <= 0 == false)
            {
                CombatText.NewText(Player.Hitbox, Color.Cyan, -info.Damage, true);
                if (ModContent.GetInstance<Personalization>().AbilitySoundDisabled == false)
                    SoundEngine.PlaySound(SoundID.NPCDeath43 with { PitchVariance = 0.4f, Pitch = -0.8f, Volume = 0.2f }, Player.position);
                info.SoundDisabled = true;
                shieldAmount -= info.Damage;
                if (info.Damage <= 1)
                    Player.SetImmuneTimeForAllTypes(Player.longInvince ? 40 : 20);
                else
                    Player.SetImmuneTimeForAllTypes(Player.longInvince ? 80 : 40);
                return true;
            }
            return false;
        }
        public override void PreUpdate()
        {
            if (Player.jump == 0)
            {
                jumpRegistered = false;
            }
            timer--;
            if (timer < -1)
                timer = -1;
            if (timer == 0)
            {
                if (ModContent.GetInstance<Personalization>().AbilitySoundDisabled == false && ((ModContent.GetInstance<Personalization>().LowCooldownSoundDisabled && timerMax > 90) || ModContent.GetInstance<Personalization>().LowCooldownSoundDisabled == false))
                    SoundEngine.PlaySound(SoundID.MaxMana with { PitchVariance = 0.3f, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew }, Player.position);
            }
            if (abilityHaste < -0.9f)
                abilityHaste = -0.9f;
            timerMax = (int)(timerMax * (1 / (1 + abilityHaste)));
            petSwapCooldown = 600;
            abilityHaste = 0;
            if (shieldTimer <= 0)
                shieldAmount = 0;
            if (shieldTimer <= -1)
                shieldTimer = -1;
            shieldTimer--;
        }
        public override void OnEnterWorld()
        {
            previousPetItem = Player.miscEquips[0].type;
            if (ModContent.GetInstance<Personalization>().DisableNotice == false)
                Main.NewText("Pets Overhaul latest changes: Tooltips have been enhanced. Junimo Maximum level cap is fixed.\nIf you happen to come across any bugs, we encourage you to join our Discord\ncommunity and report them immediately. It is recommended that you do\nso even if you have not encountered any bugs, as the mod is constantly\nupdated with new features. This will keep you informed of all the latest\ndevelopments. You're welcome to give any sort of opinion or anything you want!");
        }
        public override void OnHurt(Player.HurtInfo info)
        {

            if (ModContent.GetInstance<Personalization>().HurtSoundDisabled == false)
            {
                info.SoundDisabled = true;
                if (PetInUse(ItemID.Seaweed))
                {
                    SoundEngine.PlaySound(SoundID.NPCHit24 with { PitchVariance = 0.4f }, Player.position);
                }
                if (PetInUse(ItemID.LunaticCultistPetItem))
                {
                    SoundEngine.PlaySound(SoundID.NPCHit55 with { PitchVariance = 0.6f }, Player.position);
                }
                else if (PetInUse(ItemID.LizardEgg))
                {
                    SoundEngine.PlaySound(SoundID.NPCHit26 with { PitchVariance = 0.6f }, Player.position);
                }
                else if (PetInUse(ItemID.BoneKey) || PetInUse(ItemID.SkeletronPetItem))
                {
                    SoundEngine.PlaySound(SoundID.NPCHit2 with { PitchVariance = 0.05f, Pitch = 0.1f }, Player.position);
                }
                else if (PetInUse(ItemID.ToySled))
                {
                    SoundEngine.PlaySound(SoundID.NPCHit11 with { Pitch = -0.5f, PitchVariance = 0.2f }, Player.position);
                }
                else if (PetInUse(ItemID.FullMoonSqueakyToy))
                {
                    SoundEngine.PlaySound(SoundID.NPCHit6 with { PitchVariance = 0.4f }, Player.position);
                }
                else if (PetInUse(ItemID.BerniePetItem))
                {
                    if (Player.Male == true)
                    {
                        SoundEngine.PlaySound(SoundID.DSTMaleHurt with { PitchVariance = 0.2f }, Player.position);
                    }
                    if (Player.Male == false)
                    {
                        SoundEngine.PlaySound(SoundID.DSTFemaleHurt with { PitchVariance = 0.2f }, Player.position);
                    }
                }
                else if (PetInUse(ItemID.CursedSapling))
                {
                    SoundEngine.PlaySound(SoundID.NPCHit7 with { PitchVariance = 0.4f }, Player.position);
                }
                else if (PetInUse(ItemID.CelestialWand))
                {
                    SoundEngine.PlaySound(SoundID.NPCHit5 with { PitchVariance = 0.2f, Pitch = 0.5f }, Player.position);
                }
                else if (PetInUse(ItemID.UnluckyYarn))
                {
                    SoundEngine.PlaySound(SoundID.Meowmere with { PitchVariance = 0.4f, Pitch = 0.6f }, Player.position);
                }
                else if (PetInUse(ItemID.ChesterPetItem))
                {
                    if (Main.rand.NextBool(2))
                    {
                        SoundEngine.PlaySound(SoundID.ChesterOpen with { PitchVariance = 0.2f, Pitch = -0.6f }, Player.position);
                    }
                    else
                    {
                        SoundEngine.PlaySound(SoundID.ChesterClose with { PitchVariance = 0.2f, Pitch = -0.6f }, Player.position);
                    }

                }
                else if (PetInUse(ItemID.CompanionCube))
                {
                    SoundEngine.PlaySound(SoundID.NPCHit55 with { Pitch = -0.3f, PitchVariance = 0.5f }, Player.position);
                }
                else if (PetInUse(ItemID.ParrotCracker))
                {
                    SoundEngine.PlaySound(SoundID.NPCHit46 with { PitchVariance = 0.5f }, Player.position);
                }
                else if (PetInUse(ItemID.GlommerPetItem))
                {
                    SoundEngine.PlaySound(SoundID.NPCHit35 with { PitchVariance = 0.2f, Pitch = -0.5f }, Player.position);
                }
                else if (PetInUse(ItemID.SpiderEgg))
                {
                    SoundEngine.PlaySound(SoundID.NPCHit29 with { PitchVariance = 0.3f }, Player.position);
                }
                else if (PetInUse(ItemID.OrnateShadowKey) || PetInUse(ItemID.DestroyerPetItem) || PetInUse(ItemID.SkeletronPetItem) || PetInUse(ItemID.TwinsPetItem))
                {
                    SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.5f }, Player.position);
                }
                else if (PetInUse(ItemID.PigPetItem))
                {
                    SoundEngine.PlaySound(SoundID.Zombie39 with { PitchVariance = 0.3f }, Player.position);
                }
                else if (PetInUse(ItemID.LightningCarrot))
                {
                    SoundEngine.PlaySound(SoundID.NPCHit34 with { PitchVariance = 0.5f }, Player.position);
                }
                else if (PetInUse(ItemID.BrainOfCthulhuPetItem))
                {
                    SoundEngine.PlaySound(SoundID.NPCHit9 with { Pitch = 0.1f, PitchVariance = 0.4f }, Player.position);
                }
                else if (PetInUse(ItemID.DD2OgrePetItem))
                {
                    SoundEngine.PlaySound(SoundID.DD2_OgreHurt with { PitchVariance = 0.7f, Volume = 0.7f }, Player.position);
                }
                else if (PetInUse(ItemID.MartianPetItem))
                {
                    SoundEngine.PlaySound(SoundID.NPCHit39 with { Pitch = 0.2f, PitchVariance = 0.5f }, Player.position);
                }
                else if (PetInUse(ItemID.DD2BetsyPetItem))
                {
                    SoundEngine.PlaySound(SoundID.DD2_BetsyHurt with { Pitch = 0.3f, PitchVariance = 0.5f }, Player.position);
                }
                else if (PetInUse(ItemID.DukeFishronPetItem))
                {
                    SoundEngine.PlaySound(SoundID.Zombie39 with { PitchVariance = 0.8f }, Player.position);
                }
                else if (PetInUse(ItemID.MoonLordPetItem))
                {
                    switch (Main.rand.Next(3))
                    {
                        case 0:
                            SoundEngine.PlaySound(SoundID.Zombie100 with { PitchVariance = 0.5f, Volume = 0.5f }, Player.position);
                            break;
                        case 1:
                            SoundEngine.PlaySound(SoundID.Zombie101 with { PitchVariance = 0.5f, Volume = 0.5f }, Player.position);
                            break;
                        case 2:
                            SoundEngine.PlaySound(SoundID.Zombie102 with { PitchVariance = 0.5f, Volume = 0.5f }, Player.position);
                            break;
                    }
                }
                else
                {
                    info.SoundDisabled = false;
                }
            }
        }
        public override void UpdateEquips()
        {
            if (Player.miscEquips[0].type == ItemID.None)
            {

            }
            else if (previousPetItem != Player.miscEquips[0].type)
            {
                if (ModContent.GetInstance<Personalization>().SwapCooldown == false)
                    Player.AddBuff(ModContent.BuffType<ObliviousPet>(), petSwapCooldown);
            }
            previousPetItem = Player.miscEquips[0].type;
            if (ModContent.GetInstance<Personalization>().PassiveSoundDisabled == false && Main.rand.NextBool(3600))
            {
                if (PetInUse(ItemID.LizardEgg))
                {
                    if (Main.rand.NextBool(2))
                    {
                        SoundEngine.PlaySound(SoundID.Zombie37 with { PitchVariance = 0.5f, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew, Volume = 0.5f }, Player.position);
                    }
                    else
                    {
                        SoundEngine.PlaySound(SoundID.Zombie36 with { PitchVariance = 0.5f, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew, Volume = 0.5f }, Player.position);
                    }
                }
                if (PetInUse(ItemID.ParrotCracker))
                {
                    if (Main.rand.NextBool(2))
                    {

                        SoundEngine.PlaySound(SoundID.Cockatiel with { PitchVariance = 0.5f, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew, Volume = 0.8f }, Player.position);
                    }
                    else
                    {

                        SoundEngine.PlaySound(SoundID.Macaw with { PitchVariance = 0.5f, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew, Volume = 0.8f }, Player.position);
                    }
                }
                if (PetInUse(ItemID.BoneRattle))
                {
                    SoundEngine.PlaySound(SoundID.Zombie8 with { PitchVariance = 0.5f, Pitch = 0.5f, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew, Volume = 0.5f }, Player.position);
                }
                if (PetInUse(ItemID.DukeFishronPetItem))
                {
                    SoundEngine.PlaySound(SoundID.Zombie20 with { PitchVariance = 0.5f, Pitch = 0.5f, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew, Volume = 0.5f }, Player.position);
                }
                if (PetInUse(ItemID.MartianPetItem))
                {
                    if (Main.rand.NextBool(2))
                    {
                        SoundEngine.PlaySound(SoundID.Zombie59 with { PitchVariance = 0.5f, Pitch = 0.5f, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew, Volume = 0.5f }, Player.position);
                    }
                    else
                    {
                        SoundEngine.PlaySound(SoundID.Zombie60 with { PitchVariance = 0.5f, Pitch = 0.5f, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew, Volume = 0.5f }, Player.position);
                    }
                }
                if (PetInUse(ItemID.QueenBeePetItem))
                {
                    switch (Main.rand.Next(3))
                    {
                        case 0:
                            SoundEngine.PlaySound(SoundID.Zombie50 with { PitchVariance = 0.2f, Pitch = 0.9f, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew, Volume = 0.5f }, Player.position);
                            break;
                        case 1:
                            SoundEngine.PlaySound(SoundID.Zombie51 with { PitchVariance = 0.2f, Pitch = 0.9f, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew, Volume = 0.5f }, Player.position);
                            break;
                        case 2:
                            SoundEngine.PlaySound(SoundID.Zombie52 with { PitchVariance = 0.2f, Pitch = 0.9f, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew, Volume = 0.5f }, Player.position);
                            break;
                    }
                }

            }
        }
        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            timer = -1;
            shieldTimer = -1;
        }
        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            if (ModContent.GetInstance<Personalization>().DeathSoundDisabled == false)
            {
                playSound = false;
                if (PetInUse(ItemID.Seaweed))
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath27 with { PitchVariance = 0.4f }, Player.position);
                }
                if (PetInUse(ItemID.LunaticCultistPetItem))
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath59 with { Pitch = -0.2f, PitchVariance = 0.2f }, Player.position);
                }
                else if (PetInUse(ItemID.CompanionCube))
                {
                    if (Main.rand.NextBool(25))
                    {
                        SoundEngine.PlaySound(SoundID.NPCDeath61 with { PitchVariance = 0.5f, Volume = 0.7f }, Player.position);
                    }
                    else
                    {
                        SoundEngine.PlaySound(SoundID.NPCDeath59 with { PitchVariance = 0.5f }, Player.position);
                    }
                }
                else if (PetInUse(ItemID.SpiderEgg))
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath47 with { PitchVariance = 0.5f }, Player.position);
                }
                else if (PetInUse(ItemID.LightningCarrot))
                {
                    SoundEngine.PlaySound(SoundID.Item94 with { PitchVariance = 0.3f }, Player.position);
                }
                else if (PetInUse(ItemID.LizardEgg))
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath29 with { PitchVariance = 0.3f }, Player.position);
                }
                else if (PetInUse(ItemID.CursedSapling) || PetInUse(ItemID.EverscreamPetItem))
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath5 with { PitchVariance = 0.5f }, Player.position);
                }
                else if (PetInUse(ItemID.PigPetItem))
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath20 with { Pitch = 0.5f, PitchVariance = 0.3f }, Player.position);
                }
                else if (PetInUse(ItemID.ParrotCracker))
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath48 with { PitchVariance = 0.5f }, Player.position);
                }
                else if (PetInUse(ItemID.BrainOfCthulhuPetItem))
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath11 with { Pitch = -0.2f, PitchVariance = 0.2f }, Player.position);
                }
                else if (PetInUse(ItemID.DD2OgrePetItem))
                {
                    SoundEngine.PlaySound(SoundID.DD2_OgreDeath with { PitchVariance = 0.7f, Volume = 0.7f }, Player.position);
                }
                else if (PetInUse(ItemID.MartianPetItem))
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath57 with { Pitch = -0.3f, PitchVariance = 0.5f }, Player.position);
                }
                else if (PetInUse(ItemID.DD2BetsyPetItem))
                {
                    SoundEngine.PlaySound(SoundID.DD2_BetsyScream with { Pitch = -0.5f, PitchVariance = 0.2f }, Player.position);
                }
                else if (PetInUse(ItemID.DukeFishronPetItem))
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath20 with { Pitch = -0.2f, PitchVariance = 0.3f }, Player.position);
                }
                else if (PetInUse(ItemID.MoonLordPetItem))
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath62 with { PitchVariance = 0.5f, Volume = 0.8f }, Player.position);
                }
                else
                {
                    playSound = true;
                }
            }

            return true;
        }
    }
    /// <summary>
    /// GlobalItem class that contains many useful booleans and methods for mainly gathering and item randomizing purposes
    /// </summary>
    sealed public class ItemPet : GlobalItem
    {
        public override bool InstancePerEntity => true;
        public bool itemFromNpc = false;
        public bool sharkTooltipChangeRegular = false;
        public bool sharkTooltipChangeButMecha = false;
        public bool herbBoost = false;
        public bool rareHerbBoost = false;
        public bool oreBoost = false;
        public bool bambooBoost = false;
        public bool fishron = false;
        public bool dirt = false;
        public bool commonBlock = false;
        public bool blockNotByPlayer = false;
        public bool tree = false;
        public bool gemTree = false;
        public bool pickedUpBefore = false;
        public bool itemFromBag = false;
        public bool itemFromBoss = false;
        /// <summary>
        /// Randomizes the given number. numToBeRandomized / randomizeTo returns how many times its 100% chance and rolls if the leftover, non-100% amount is true. item.stack += Randomizer(250) returns +2 and +1 more with 50% chance.
        /// </summary>
        public static int Randomizer(int numToBeRandomized, int randomizeTo = 100)
        {
            int a = 0;
            if (numToBeRandomized >= randomizeTo)
            {
                a = numToBeRandomized / randomizeTo;
                numToBeRandomized %= randomizeTo;
            }
            if (Main.rand.NextBool(numToBeRandomized, randomizeTo))
                a++;

            return a;

        }
        public override void UpdateInventory(Item item, Player player)
        {
            if (pickedUpBefore == false)
            {
                pickedUpBefore = true;
            }
        }
        public override void OnSpawn(Item item, IEntitySource source)
        {
            if (source is EntitySource_Loot { Entity: NPC { boss: false } })
            {
                itemFromNpc = true;
            }
            else
            {

                itemFromNpc = false;
            }
            if (TileChecks.commonPlant == true && source is EntitySource_TileBreak)
            {
                herbBoost = true;
            }
            else
            {
                herbBoost = false;
            }
            if (TileChecks.rarePlant == true && source is EntitySource_TileBreak && TileChecks.tileIsPlacedByPlayer == false)
            {
                rareHerbBoost = true;
            }
            else
            {
                rareHerbBoost = false;
            }
            if (TileChecks.choppable == true && source is EntitySource_TileBreak)
            {
                tree = true;
            }
            else
            {
                tree = false;
            }
            if (source is EntitySource_TileBreak && TileChecks.tileIsOreGemOrExtractable == true && TileChecks.tileIsPlacedByPlayer == false)
            {
                oreBoost = true;
            }
            else
            {
                oreBoost = false;
            }
            if (source is EntitySource_TileBreak && TileChecks.bamboo == true && TileChecks.tileIsPlacedByPlayer == false)
            {
                bambooBoost = true;
            }
            else
            {
                bambooBoost = false;
            }
            if (source is EntitySource_TileBreak && TileChecks.dirt == true && TileChecks.tileIsPlacedByPlayer == false)
            {
                dirt = true;
            }
            else
            {
                dirt = false;
            }
            if (source is EntitySource_TileBreak && TileChecks.commonBlock == true && TileChecks.tileIsPlacedByPlayer == false)
            {
                commonBlock = true;
            }
            else
            {
                commonBlock = false;
            }
            if (source is EntitySource_TileBreak && TileChecks.tileIsPlacedByPlayer == false)
            {
                blockNotByPlayer = true;
            }
            else
            {
                blockNotByPlayer = false;
            }
            if (source is EntitySource_TileBreak && TileChecks.gemTree == true)
            {
                gemTree = true;
            }
            else
            {
                gemTree = false;
            }
            if (source is EntitySource_Loot lootSource && lootSource.Entity is NPC npc && (npc.boss == true || npc.GetGlobalNPC<NpcPet>().nonBossTrueBosses[npc.type]))
            {
                itemFromBoss = true;
            }
            else
            {

                itemFromBoss = false;
            }
            if (source is EntitySource_ItemOpen)
            {
                itemFromBag = true;
            }
            else
            {
                itemFromBag = false;
            }
        }
        public override void NetSend(Item item, BinaryWriter writer)
        {
            BitsByte sources1 = new(itemFromNpc, herbBoost, rareHerbBoost, tree, oreBoost, bambooBoost, dirt, commonBlock);
            BitsByte sources2 = new(blockNotByPlayer, gemTree, pickedUpBefore, itemFromBoss, itemFromBag);
            writer.Write(sources1);
            writer.Write(sources2);
        }
        public override void NetReceive(Item item, BinaryReader reader)
        {
            BitsByte sources1 = reader.ReadByte();
            sources1.Retrieve(ref itemFromNpc, ref herbBoost, ref rareHerbBoost, ref tree, ref oreBoost, ref bambooBoost, ref dirt, ref commonBlock);
            BitsByte sources2 = reader.ReadByte();
            sources2.Retrieve(ref blockNotByPlayer, ref gemTree, ref pickedUpBefore, ref itemFromBoss, ref itemFromBag);
        }

    }
    /// <summary>
    /// GlobalNPC class that contains useful booleans and methods such as Slow() and seaCreature
    /// </summary>
    sealed public class NpcPet : GlobalNPC
    {
        public enum SlowId
        {
            Grinch = 0, Snowman = 1, QueenSlime = 2, Deerclops = 3, IceQueen = 4
        }
        public List<(SlowId, float slowAmount, int slowTime)> SlowList = new(100);
        /// <summary>
        /// Increase this to slow down an enemy.
        /// </summary>
        public float slowAmount = 0f;
        public bool seaCreature;
        public int maulCounter;
        public int curseCounter;
        /// <summary>
        /// Contains all Vanilla bosses that does not return npc.boss = true
        /// </summary>
        public bool[] nonBossTrueBosses = NPCID.Sets.Factory.CreateBoolSet(false, NPCID.TheDestroyer, NPCID.TheDestroyerBody, NPCID.TheDestroyerTail, NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail, NPCID.EaterofWorldsHead, NPCID.LunarTowerSolar, NPCID.LunarTowerNebula, NPCID.LunarTowerStardust, NPCID.LunarTowerVortex, NPCID.TorchGod, NPCID.Retinazer, NPCID.Spazmatism);
        public Vector2 FlyingVelo { get; internal set; }
        public float GroundVelo { get; internal set; }
        public bool VeloChangedFlying { get; internal set; }
        public bool VeloChangedFlying2 { get; internal set; }
        public bool VeloChangedGround { get; internal set; }
        public bool VeloChangedGround2 { get; internal set; }

        public override bool InstancePerEntity => true;
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            if (npc.type == NPCID.EyeofCthulhu && MasteryShardCheck.masteryShardObtained1 == false)
            {
                npcLoot.Add(ItemDropRule.ByCondition(new FirstKillEoC(), ModContent.ItemType<MasteryShard>()));
            }
            if (npc.type == NPCID.WallofFlesh && MasteryShardCheck.masteryShardObtained2 == false)
            {
                npcLoot.Add(ItemDropRule.ByCondition(new FirstKillWoF(), ModContent.ItemType<MasteryShard>()));
            }
            if (npc.type == NPCID.Golem && MasteryShardCheck.masteryShardObtained3 == false)
            {
                npcLoot.Add(ItemDropRule.ByCondition(new FirstKillGolem(), ModContent.ItemType<MasteryShard>()));

            }
        }
        public override void OnKill(NPC npc)
        {
            if (npc.type == NPCID.EyeofCthulhu)
            {
                MasteryShardCheck.masteryShardObtained1 = true;
            }
            if (npc.type == NPCID.WallofFlesh)
            {
                MasteryShardCheck.masteryShardObtained2 = true;
            }
            if (npc.type == NPCID.Golem)
            {
                MasteryShardCheck.masteryShardObtained3 = true;
            }
        }
        public override bool PreAI(NPC npc)
        {
            if (npc.active)
            {
                if (VeloChangedGround == true)
                {
                    npc.velocity.X = GroundVelo;
                    VeloChangedGround2 = true;
                }
                else
                    VeloChangedGround2 = false;
                if (VeloChangedGround2 == false)
                {
                    GroundVelo = npc.velocity.X;
                }
                if (VeloChangedFlying == true)
                {
                    npc.velocity = FlyingVelo;
                    VeloChangedFlying2 = true;
                }
                else
                    VeloChangedFlying2 = false;
                if (VeloChangedFlying2 == false)
                {
                    FlyingVelo = npc.velocity;
                }
            }

            return true;
        }
        public override void PostAI(NPC npc)
        {
            if (npc.active && (npc.townNPC == false || npc.isLikeATownNPC == false || npc.friendly == false) && (npc.boss == false || nonBossTrueBosses[npc.type] == false))
            {
                if (SlowList.Count > 0)
                {
                    SlowList.ForEach(x => slowAmount += x.slowAmount);
                    for (int i = 0; i < SlowList.Count; i++) //List'lerde struct'lar bir nevi readonly olarak çalıştığından, değeri alıp tekrar atıyoruz
                    {
                        var slow = SlowList[i];
                        slow.slowTime--;
                        SlowList[i] = slow;
                    }
                    int indexToRemove = SlowList.FindIndex(x => x.slowTime <= 0);
                    if (indexToRemove != -1)
                    {
                        SlowList.RemoveAt(indexToRemove);
                    }
                }

                if (slowAmount != 0)
                    Slow(npc, slowAmount);
                slowAmount = 0;
            }
        }
        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            if (source is EntitySource_FishedOut)
                seaCreature = true;
            else
                seaCreature = false;
        }
        /// <summary>
        /// Slows if enemy is not a boss or a friendly npc. Also does not slow an npc's vertical speed if they are affected by gravity, but does so if they arent. Due to the formula, you may use a positive number for slowAmount freely and as much as you want, it almost will never completely stop an enemy. Negative values however, easily can get out of hand and cause unwanted effects. Due to that, a cap of -0.9f exists for negative values, which 10x's the speed.
        /// </summary>
        private void Slow(NPC npc, float slow)
        {
            if (slow < -0.9f)
                slow = -0.9f;
            FlyingVelo = npc.velocity;
            GroundVelo = npc.velocity.X;
            if (npc.noGravity == false)
            {
                npc.velocity.X *= 1 / (1 + slow);
                VeloChangedGround = true;
            }
            else
                VeloChangedGround = false;
            if (npc.noGravity)
            {
                npc.velocity *= 1 / (1 + slow);
                VeloChangedFlying = true;
            }
            else
                VeloChangedFlying = false;

        }
        public void AddSlow(SlowId slowType, float slowValue, int slowTimer)
        {
            int indexToReplace;
            if (SlowList.Exists(x => x.Item1 == slowType && x.slowAmount < slowValue))
            {
                indexToReplace = SlowList.FindIndex(x => x.Item1 == slowType && x.slowAmount < slowValue);
                SlowList[indexToReplace] = (slowType, slowValue, slowTimer);
            }
            else if (SlowList.Exists(x => x.Item1 == slowType && x.slowAmount == slowValue && x.slowTime < slowTimer))
            {
                indexToReplace = SlowList.FindIndex(x => x.Item1 == slowType && x.slowAmount == slowValue && x.slowTime < slowTimer);
                SlowList[indexToReplace] = (slowType, slowValue, slowTimer);
            }
            else if (SlowList.Exists(x => x.Item1 == slowType) == false)
            {
                SlowList.Add((slowType, slowValue, slowTimer));
            }
        }
        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (npc.HasBuff(ModContent.BuffType<Mauled>()))
            {
                for (int i = 0; i < maulCounter; i++)
                {
                    Dust dust = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Blood, Main.rand.NextFloat(0f, 1f), Main.rand.NextFloat(0f, 3f), 75, default, Main.rand.NextFloat(0.5f, 0.8f));
                    dust.velocity *= 0.8f;
                }

            }
            if (npc.HasBuff(ModContent.BuffType<QueensDamnation>()))
            {
                if (curseCounter >= 20)
                {
                    Dust dust = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Wraith, Main.rand.NextFloat(0f, 0.6f), Main.rand.NextFloat(-1f, 2f), 75, default, Main.rand.NextFloat(0.7f, 1f));
                    dust.velocity *= 0.2f;
                }
                else
                {
                    Dust dust = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Pixie, Main.rand.NextFloat(0f, 0.6f), Main.rand.NextFloat(-1f, 2f), 150, default, Main.rand.NextFloat(0.5f, 0.7f));
                    dust.velocity *= 0.2f;
                }
            }
        }
    }
    sealed public class ProjectileSourceChecks : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public bool isPlanteraProjectile = false;
        public bool planteroProj = false;
        public bool isFromSentry = false;
        public bool phantasmProjectile = false;
        public bool beeProj = false;
        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            if (source is EntitySource_ItemUse item && (item.Item.type == ItemID.VenusMagnum || item.Item.type == ItemID.NettleBurst || item.Item.type == ItemID.LeafBlower || item.Item.type == ItemID.FlowerPow || item.Item.type == ItemID.WaspGun || item.Item.type == ItemID.Seedler))
            {
                isPlanteraProjectile = true;
            }
            else if (source is EntitySource_Parent parent && parent.Entity is Projectile proj && (proj.type == ProjectileID.Pygmy || proj.type == ProjectileID.Pygmy2 || proj.type == ProjectileID.Pygmy3 || proj.type == ProjectileID.Pygmy4))
            {
                isPlanteraProjectile = true;
            }
            else
            {
                isPlanteraProjectile = false;
            }
            if (source is EntitySource_Misc { Context: "Plantero" })
            {
                planteroProj = true;
            }
            else
            {
                planteroProj = false;
            }
            if (source is EntitySource_Parent parent2 && parent2.Entity is Projectile proj2 && proj2.sentry)
            {
                isFromSentry = true;
            }
            else
            {
                isFromSentry = false;
            }
            if (source is EntitySource_Misc { Context: "PhantasmalDragon" })
            {
                phantasmProjectile = true;
            }
            else
            {
                phantasmProjectile = false;
            }
            if (source is EntitySource_Misc { Context: "BabyHornet" })
            {
                beeProj = true;
            }
            else
            {
                beeProj = false;
            }
        }
    }
}
