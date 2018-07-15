﻿using System.Collections.Generic;
using System;
using BDArmory.Core.Services;
using BDArmory.Core.Utils;
using UniLinq;
using UnityEngine;

namespace BDArmory.Core.Extension
{
    public static class PartExtensions
    {
        public static void AddDamage(this Part p, float damage)
        {
            //////////////////////////////////////////////////////////
            // Basic Add Hitpoints for compatibility
            //////////////////////////////////////////////////////////
            damage = (float)Math.Round(damage, 2);

            if (p.GetComponent<KerbalEVA>() != null)
            {
                ApplyHitPoints(p.GetComponent<KerbalEVA>(), damage);
            }
            else
            {
                Dependencies.Get<DamageService>().AddDamageToPart_svc(p, damage);
                if (BDArmorySettings.DRAW_DEBUG_LABELS)
                    Debug.Log("[BDArmory]: Standard Hitpoints Applied : " + damage);
            }

        }

        public static void AddExplosiveDamage(this Part p,
                                               float explosiveDamage,                                               
                                               float caliber,
                                               bool isMissile)
        {
            float damage_ = 0f;

            //////////////////////////////////////////////////////////
            // Explosive Hitpoints
            //////////////////////////////////////////////////////////

            if (isMissile)
            {
                damage_ = (BDArmorySettings.DMG_MULTIPLIER / 100) * BDArmorySettings.EXP_DMG_MOD_MISSILE * explosiveDamage;
            }
            else
            {
                damage_ = (BDArmorySettings.DMG_MULTIPLIER / 100) * BDArmorySettings.EXP_DMG_MOD_BALLISTIC * explosiveDamage;
            }

            //////////////////////////////////////////////////////////
            //   Armor Reduction factors
            //////////////////////////////////////////////////////////

            if (p.HasArmor())
            {
                float armorMass_ = p.GetArmorThickness();
                float damageReduction = DamageReduction(armorMass_, damage_, isMissile, caliber);

                damage_ = damageReduction;
            }

            //////////////////////////////////////////////////////////
            //   Apply Hitpoints
            //////////////////////////////////////////////////////////

            if (p.GetComponent<KerbalEVA>() != null)
            {
                ApplyHitPoints(p.GetComponent<KerbalEVA>(), (float)damage_);
            }
            else
            {
                ApplyHitPoints(p, damage_);
            }

        }

        public static void AddBallisticDamage(this Part p,
                                               float mass,
                                               float caliber,
                                               float multiplier,
                                               float penetrationfactor,                                               
                                               float bulletDmgMult,
                                               float impactVelocity)
        {          

            //////////////////////////////////////////////////////////
            // Basic Kinetic Formula
            //////////////////////////////////////////////////////////
            //Hitpoints mult for scaling in settings
            //1e-4 constant for adjusting MegaJoules for gameplay

            float damage_ = ((0.5f * (mass * Mathf.Pow(impactVelocity, 2)))
                            * (BDArmorySettings.DMG_MULTIPLIER / 100) * bulletDmgMult
                            * 1e-4f);
            
            //////////////////////////////////////////////////////////
            //   Armor Reduction factors
            //////////////////////////////////////////////////////////

            if (p.HasArmor())
            {
                float armorMass_ =  p.GetArmorThickness();                
                float damageReduction = DamageReduction(armorMass_, damage_, false, caliber,penetrationfactor);

                damage_ = damageReduction;
            }
            
            //////////////////////////////////////////////////////////
            //   Apply Hitpoints
            //////////////////////////////////////////////////////////

            if (p.GetComponent<KerbalEVA>() != null)
            {
                ApplyHitPoints(p.GetComponent<KerbalEVA>(), (float)damage_);
            }
            else
            {
                ApplyHitPoints(p, damage_, caliber, mass, mass, impactVelocity, penetrationfactor);
            }       
            
                
        }

        /// <summary>
        /// Ballistic Hitpoint Damage
        /// </summary>
        public static void ApplyHitPoints(Part p, float damage_ ,float caliber,float mass, float multiplier, float impactVelocity,float penetrationfactor)
        {

            //////////////////////////////////////////////////////////
            // Apply HitPoints Ballistic
            //////////////////////////////////////////////////////////
            Dependencies.Get<DamageService>().AddDamageToPart_svc(p, damage_);
            if (BDArmorySettings.DRAW_DEBUG_LABELS)
            {
                Debug.Log("[BDArmory]: mass: " + mass + " caliber: " + caliber + " multiplier: " + multiplier + " velocity: " + impactVelocity + " penetrationfactor: " + penetrationfactor);
                Debug.Log("[BDArmory]: Ballistic Hitpoints Applied : " + Math.Round(damage_, 2));
            }

            //CheckDamageFX(p);
        }

        /// <summary>
        /// Explosive Hitpoint Damage
        /// </summary>
        public static void ApplyHitPoints(Part p, float damage)
        {
            //////////////////////////////////////////////////////////
            // Apply Hitpoints / Explosive
            //////////////////////////////////////////////////////////

            Dependencies.Get<DamageService>().AddDamageToPart_svc(p, damage);
            if (BDArmorySettings.DRAW_DEBUG_LABELS)
                Debug.Log("[BDArmory]: Explosive Hitpoints Applied to " + p.name + ": " + Math.Round(damage, 2));

            //CheckDamageFX(p);
        }

        /// <summary>
        /// Kerbal Hitpoint Damage
        /// </summary>
        public static void ApplyHitPoints(KerbalEVA kerbal, float damage)
        {
            //////////////////////////////////////////////////////////
            // Apply Hitpoints / Kerbal
            //////////////////////////////////////////////////////////

            Dependencies.Get<DamageService>().AddDamageToKerbal_svc(kerbal, damage);
            if (BDArmorySettings.DRAW_DEBUG_LABELS)
                Debug.Log("[BDArmory]: Hitpoints Applied to " + kerbal.name + ": " + Math.Round(damage, 2));

        }

        public static void AddForceToPart(Rigidbody rb, Vector3 force, Vector3 position,ForceMode mode)
        {

            //////////////////////////////////////////////////////////
            // Add The force to part
            //////////////////////////////////////////////////////////

            rb.AddForceAtPosition(force, position, mode);
            Debug.Log("[BDArmory]: Force Applied : " + Math.Round(force.magnitude,2));
        }

        public static void Destroy(this Part p)
        {
            Dependencies.Get<DamageService>().SetDamageToPart_svc(p,-1);
        }

        public static bool HasArmor(this Part p)
        {
            return p.GetArmorThickness() > 15f;
        }

        public static bool GetFireFX(this Part p)
        {
            return Dependencies.Get<DamageService>().HasFireFX_svc(p);
        }

        public static float GetFireFXTimeOut(this Part p)
        {
            return Dependencies.Get<DamageService>().GetFireFXTimeOut(p);
        }

        public static float Damage(this Part p)
         {		
             return Dependencies.Get<DamageService>().GetPartDamage_svc(p);		
         }		
 		
        public static float MaxDamage(this Part p)
         {		
             return Dependencies.Get<DamageService>().GetMaxPartDamage_svc(p);		
         }

        public static void ReduceArmor(this Part p, double massToReduce)
        {
            if (!p.HasArmor()) return;
            massToReduce = Math.Max(0.10, Math.Round(massToReduce, 2));
            Dependencies.Get<DamageService>().ReduceArmor_svc(p, (float) massToReduce );

            if (BDArmorySettings.DRAW_DEBUG_LABELS)
            {
                  Debug.Log("[BDArmory]: Armor Removed : " + massToReduce);
            }       
        }
        
        public static float GetArmorThickness(this Part p)
        {
            if (p == null) return 0f;        
            return Dependencies.Get<DamageService>().GetPartArmor_svc(p);
        }

        public static float GetArmorPercentage(this Part p)
        {
            if (p == null) return 0;
            float armor_ = Dependencies.Get<DamageService>().GetPartArmor_svc(p);
            float maxArmor_ = Dependencies.Get<DamageService>().GetMaxArmor_svc(p);

            return armor_ / maxArmor_;
        }

        public static float GetDamagePercentatge(this Part p)
        {
            if (p == null) return 0;

            float damage_ = p.Damage();
            float maxDamage_ = p.MaxDamage();

            return damage_ / maxDamage_;
        }

        public static void RefreshAssociatedWindows(this Part part)
        {
            //Thanks FlowerChild
            //refreshes part action window

            IEnumerator<UIPartActionWindow> window = UnityEngine.Object.FindObjectsOfType(typeof(UIPartActionWindow)).Cast<UIPartActionWindow>().GetEnumerator();
            while (window.MoveNext())
            {
                if (window.Current == null) continue;
                if (window.Current.part == part)
                {
                    window.Current.displayDirty = true;
                }
            }
            window.Dispose();
        }

        public static bool IsMissile(this Part part)
        {
            return part.Modules.Contains("MissileBase") || part.Modules.Contains("MissileLauncher") ||
                   part.Modules.Contains("BDModularGuidance");
        }

        public static float GetArea(this Part part)
        {
            float sfcAreaCalc = 2f * (part.prefabSize.x * part.prefabSize.y) + 2f * (part.prefabSize.y * part.prefabSize.z) + 2f * (part.prefabSize.x * part.prefabSize.z);

            if (sfcAreaCalc == 0)
            {
                sfcAreaCalc = 2f * (part.HighlightRenderer[0].bounds.size.x * part.HighlightRenderer[0].bounds.size.y) + 2f * (part.HighlightRenderer[0].bounds.size.y * part.HighlightRenderer[0].bounds.size.z) + 2f * (part.HighlightRenderer[0].bounds.size.x * part.HighlightRenderer[0].bounds.size.z);
            }

            Debug.Log("Part name = " + part.name + "Area ="+ sfcAreaCalc);
            return sfcAreaCalc;
        }

        public static float GetAverageBoundSize(this Part part)
        {
            return (part.prefabSize.x + part.prefabSize.y + part.prefabSize.z) / 3f;
        }

        public static float GetVolume(this Part part)
        {
            var volume = part.prefabSize.x * part.prefabSize.y * part.prefabSize.z;
            if (volume == 0)
            {
                volume = part.HighlightRenderer[0].bounds.size.x * part.HighlightRenderer[0].bounds.size.y * part.HighlightRenderer[0].bounds.size.z;
            }
            Debug.Log("Part name = " + part.name + "volume =" + volume);
            return volume;
        }

        public static float GetDensity (this Part part)
        {
            return (part.mass * 1000) / part.GetVolume();
        }

        public static bool IsAero(this Part part)
        {
            return part.Modules.Contains("ModuleControlSurface") ||
                   part.Modules.Contains("ModuleLiftingSurface");
        }

        public static string GetExplodeMode(this Part part)
        {
            return Dependencies.Get<DamageService>().GetExplodeMode_svc(part);
        }

        public static bool IgnoreDecal(this Part part)
        {
            if (
                part.Modules.Contains("FSplanePropellerSpinner") ||
                part.Modules.Contains("ModuleWheelBase") ||
                part.Modules.Contains("KSPWheelBase") ||
                part.gameObject.GetComponentUpwards<KerbalEVA>()||
                part.Modules.Contains("ModuleDCKShields")
                )
            {
                return true;
            }
            else
            {
                return false;
            }            
        }

        public static bool HasFuel(this Part part)
        {
            bool hasFuel = false;
            IEnumerator<PartResource> resources = part.Resources.GetEnumerator();
            while (resources.MoveNext())
            {
                if (resources.Current == null) continue;
                switch (resources.Current.resourceName)
                {
                    case "LiquidFuel":
                        if(resources.Current.amount > 1d) hasFuel = true;
                        break;               
                }
            }
            return hasFuel;

        }

        public static float DamageReduction(float armor, float damage,bool isMissile,float caliber = 0, float penetrationfactor = 0)
        {           

            if (isMissile)
            {
                if (BDAMath.Between(armor, 100f, 200f))
                {
                    damage *= 0.95f;
                }
                else if (BDAMath.Between(armor, 200f, 400f))
                {
                    damage *= 0.875f;
                }
                else if (BDAMath.Between(armor, 400f, 500f))
                {
                    damage *= 0.80f;
                }

            }

            if (!isMissile && !(penetrationfactor >= 1f))
            {
                if (BDAMath.Between(armor, 100f, 200f))
                {
                    damage *= 0.300f;
                }
                else if (BDAMath.Between(armor, 200f, 400f))
                {
                    damage *= 0.250f;
                }
                else if (BDAMath.Between(armor, 400f, 500f))
                {
                    damage *= 0.200f;
                }
            }

            return damage;
        }

        public static void CheckDamageFX(Part part)
        {
            if (part.GetComponent<ModuleEngines>() != null && part.GetDamagePercentatge() <= 0.35f)
            {
                part.gameObject.AddOrGetComponent<DamageFX>();
                DamageFX.engineDamaged = true;
            }

            if (part.GetComponent<ModuleLiftingSurface>() != null && part.GetDamagePercentatge() <= 0.35f)
            {
                //part.gameObject.AddOrGetComponent<DamageFX>();
            }

        }
    }
}