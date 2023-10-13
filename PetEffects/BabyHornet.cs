﻿using Terraria;
using Terraria.ID;
using PetsOverhaul.Systems;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.DataStructures;
using PetsOverhaul.Config;

namespace PetsOverhaul.PetEffects
{
    sealed public class BabyHornet : ModPlayer
    { 
        GlobalPet Pet { get => Player.GetModPlayer<GlobalPet>(); }
        public int nectarCooldown = 360;
        public int beeDmg = 6;
        public float beeKb = 0.1f;
        public float dmgReduction = 0.03f;
        public int defReduction = 3;
        public int maxMinion = 1;
        public int critReduction = 3;
        public float moveSpdIncr = 0.03f;
        public float healthRecovery = 0.05f;
        public int beeChance = 5;
        public override void PreUpdate()
        {
            if (Pet.PetInUse(ItemID.Nectar))
                Player.GetModPlayer<GlobalPet>().timerMax = nectarCooldown;
        }
        public override void PreUpdateBuffs()
        {
            if (Pet.PetInUseWithSwapCd(ItemID.Nectar))
            {
                Player.buffImmune[BuffID.Poisoned] = false;
                Player.buffImmune[BuffID.Venom] = false;
            }
        }
        public override void PostUpdateEquips()
        {
            if (Pet.PetInUseWithSwapCd(ItemID.Nectar))
            {

                if (Player.HasBuff(BuffID.Poisoned) == true || Player.HasBuff(BuffID.Venom) == true)
                {
                    if (Pet.timer <= 0)
                    {
                        Pet.Lifesteal(Player.statLifeMax2, healthRecovery, respectLifeStealCap: false);
                        if (ModContent.GetInstance<Personalization>().AbilitySoundDisabled == false)
                            SoundEngine.PlaySound(SoundID.Item97 with { Pitch = 0.4f, MaxInstances = 10 });
                        Pet.timer = Pet.timerMax;
                    }
                    Player.ClearBuff(BuffID.Poisoned);
                    Player.ClearBuff(BuffID.Venom);
                }
                Player.statDefense -= defReduction;
                Player.GetDamage<GenericDamageClass>() -= dmgReduction;
                Player.GetCritChance<GenericDamageClass>() -= critReduction;
                Player.moveSpeed += moveSpdIncr;
                Player.maxMinions += maxMinion;
            }
        }
        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {

            int summonMult = 1;
            if (hit.DamageType == DamageClass.Summon || hit.DamageType == DamageClass.SummonMeleeSpeed || hit.DamageType == DamageClass.MagicSummonHybrid)
                summonMult = 2;
            if (Pet.PetInUseWithSwapCd(ItemID.Nectar))
            {
                for (int i = 0; i < ItemPet.Randomizer(beeChance * summonMult); i++)
                {
                    if (Player.strongBees == true && Main.rand.NextBool(1, 3))
                        Projectile.NewProjectileDirect(Player.GetSource_Misc("BabyHornet"), target.Center, Main.rand.NextVector2Circular(10f, 10f), ProjectileID.GiantBee, beeDmg * 2, beeKb * 2, Player.whoAmI);
                    else
                        Projectile.NewProjectileDirect(Player.GetSource_Misc("BabyHornet"), target.Center, Main.rand.NextVector2Circular(10f, 10f), ProjectileID.Bee, beeDmg, beeKb, Player.whoAmI);
                }
            }
        }
        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            int summonMult = 1;
            if (hit.DamageType == DamageClass.Summon)
                summonMult = 2;
            if (Pet.PetInUseWithSwapCd(ItemID.Nectar) && proj.GetGlobalProjectile<ProjectileSourceChecks>().beeProj == false)
            {
                for (int i = 0; i < ItemPet.Randomizer(beeChance * summonMult); i++)
                {
                    if (Player.strongBees == true && Main.rand.NextBool(1, 3))
                        Projectile.NewProjectileDirect(Player.GetSource_Misc("BabyHornet"), target.Center, Main.rand.NextVector2Circular(10f, 10f), ProjectileID.GiantBee, beeDmg * 2, beeKb * 2, Player.whoAmI);
                    else
                        Projectile.NewProjectileDirect(Player.GetSource_Misc("BabyHornet"), target.Center, Main.rand.NextVector2Circular(10f, 10f), ProjectileID.Bee, beeDmg, beeKb, Player.whoAmI);
                }
            }
        }
    }
}
