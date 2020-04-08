using System;
using Harmony;
using System.Linq;
using System.Collections.Generic;
using BattleTech;
using BattleTech.Data;

namespace EvenTheOdds.Patches
{
    // Try to limit the amount of plus weapons in salvage
    // In Vanilla the OpFor almost *never* fields Mechs with rare weapons, these get generated on salvage creation
    [HarmonyPatch(typeof(Contract), "AddMechComponentToSalvage")]
    public static class Contract_AddMechComponentToSalvage_Patch
    {
        public static bool Prefix(Contract __instance, ref MechComponentDef def, DataManager ___dataManager)
        {
            try
            {
                Logger.Debug("----------------------------------------------------------------------------------------------------");
                Logger.Debug("[Contract_AddMechComponentToSalvage_PREFIX] Handling def: " + def.Description.Id + "(ComponentType: " + def.ComponentType + ")");

                // If rare upgrades are found, just cancel original method completely -> Skip adding anything to salvage for this component
                // Rare upgrades will still be added to salvage by Vanillas Replacement-RNG on anything non-weapon
                if (def.ComponentType == ComponentType.Upgrade)
                {
                    Logger.Debug("[Contract_AddMechComponentToSalvage_PREFIX] Component is an upgrade, skipping original method");
                    return false;
                }
                // Ignore blacklisted components
                if (def.ComponentTags.Contains("BLACKLISTED"))
                {
                    return true;
                }
                // Only touch weapons for now
                if (def.ComponentType != ComponentType.Weapon)
                {
                    return true;
                }
                // Don't touch stock weapons either
                if (def.Description.Rarity <= 0)
                {
                    return true;
                }

                // At this point only RARE WEAPONS should be left to handle
                // Adding in a random chance to keep exactly the item that will be passed into the original method.
                // Default behaviour is to replace any rare weapon with its stock version and let vanillas algorithms decide if some of them will be replaced by rare counterparts
                // Note that even if the current component is rare and it passes the test to be kept, the original method will still probably replace it with *another* rare component.
                SimGameState simGameState = __instance.BattleTechGame.Simulation;
                SimGameConstants simGameConstants = simGameState.Constants;
                int contractDifficulty = __instance.Override.finalDifficulty;
                int globalDifficulty = Utilities.GetNormalizedGlobalDifficulty(simGameState);
                Logger.Info($"[Contract_AddMechComponentToSalvage_PREFIX] contractDifficulty: {contractDifficulty}, globalDifficulty: {globalDifficulty}");


                // Respect difficulty setting "No Rare Salvage" in here too...
                //Logger.Info($"[Contract_AddMechComponentToSalvage_PREFIX] simGameConstants.Salvage.RareWeaponChance: {simGameConstants.Salvage.RareWeaponChance}");
                //Logger.Info($"[Contract_AddMechComponentToSalvage_PREFIX] simGameConstants.Salvage.VeryRareWeaponChance: {simGameConstants.Salvage.VeryRareWeaponChance}");
                bool rareSalvageEnabled = simGameConstants.Salvage.RareWeaponChance > -10f;
                Logger.Debug($"[Contract_AddMechComponentToSalvage_PREFIX] rareSalvageEnabled: {rareSalvageEnabled}");

                if (rareSalvageEnabled)
                {
                    // Minimum chance to keep this rare weapon by settings + (contract difficulty OR global difficulty (whatever is higher) multiplied by mod-difficulty), max 80%
                    int bonus = Math.Max(contractDifficulty, globalDifficulty) * EvenTheOdds.Settings.Difficulty;
                    float chance = Math.Min(EvenTheOdds.Settings.SalvageHighQualityWeaponsBaseChancePercent + bonus, 80) / 100f;
                    float roll = simGameState.NetworkRandom.Float(0f, 1f);
                    bool keep = roll < chance;
                    Logger.Info($"[Contract_AddMechComponentToSalvage_PREFIX] ({def.Description.Id}) chance({chance}) against roll({roll}) to keep: {keep}");

                    // Currently handled rare weapons should pass into original method as is
                    if (keep)
                    {
                        Logger.Debug("[Contract_AddMechComponentToSalvage_PREFIX] (" + def.Description.Id + ") passes into original method");
                        return true;
                    }
                }


                // Resetting rare weapon to its stock version if above checks to potentially keep it fails
                WeaponDef weaponDef = def as WeaponDef;
                string weaponOriginalId = def.Description.Id;

                // Note that the "Heavy Metal" weapons don't follow this scheme. But as they're all blacklisted they should never end up down here
                string weaponStockId = def.ComponentType.ToString() + "_" + weaponDef.Type.ToString() + "_" + weaponDef.WeaponSubType.ToString() + "_0-STOCK";

                // Set to stock
                def = ___dataManager.WeaponDefs.Get(weaponStockId);
                Logger.Debug("[Contract_AddMechComponentToSalvage_PREFIX] (" + weaponOriginalId + ") was replaced with stock version (" + def.Description.Id + ")");

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return true;
            }
        }

        // Check
        public static void Postfix(Contract __instance, MechComponentDef def, List<SalvageDef> ___finalPotentialSalvage)
        {
            try
            {
                // If prefix returns false this is empty on first call. Need to check first
                if (___finalPotentialSalvage != null && ___finalPotentialSalvage.Count > 0)
                {
                    // This item was just added by original method
                    SalvageDef lastAddedSalvageItem = ___finalPotentialSalvage.Last();
                    MechComponentDef lastAddedMechComponent = lastAddedSalvageItem.MechComponentDef ?? null;
                    
                    // Only check components (ignore MechParts), and ignore upgrades/blacklisted components
                    if (lastAddedMechComponent == null || def.ComponentType == ComponentType.Upgrade || def.ComponentTags.Contains("BLACKLISTED"))
                    {
                        return;
                    }

                    // Check for rare weapons
                    if (def.ComponentType == ComponentType.Weapon && def.Description.Rarity > 0)
                    {
                        Logger.Info("[Contract_AddMechComponentToSalvage_POSTFIX] (" + def.Description.Id + ") passed into original method");
                    }

                    // Check if vanilla replacement rng DID transform components
                    if (def.Description.Id != lastAddedMechComponent.Description.Id)
                    {
                        Logger.Info("[Contract_AddMechComponentToSalvage_POSTFIX] (" + def.Description.Id + ") was changed to (" + lastAddedMechComponent.Description.Id + ")");
                    }

                    // Check final salvage for components
                    Logger.Info("[Contract_AddMechComponentToSalvage_POSTFIX] (" + lastAddedMechComponent.Description.Id + ") was added to salvage");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }



    // Normalize MechParts for Salvage (Replacing custom MechDefs with Stock) 
    // Otherwise each different MechDef gets their own MechPart-Stack in the Mechbay...
    [HarmonyPatch(typeof(Contract), "CreateAndAddMechPart")]
    public static class Contract_CreateAndAddMechPart_Patch
    {
        public static void Prefix(Contract __instance, ref MechDef m, DataManager ___dataManager)
        {
            try
            {
                if(!m.MechTags.Contains("unit_madlabs"))
                {
                    return;
                }
                else
                {
                    Logger.Debug("----------------------------------------------------------------------------------------------------");
                    Logger.Debug("[Contract_CreateAndAddMechPart_PREFIX] Handling MechPart of MechDef (" + m.Description.Id + ") which belongs to a custom unit. Normalizing back to STOCK...");

                    //SimGameState simGameState = __instance.BattleTechGame.Simulation;
                    string currentMechDefId = m.Description.Id;
                    string stockMechDefId = m.ChassisID.Replace("chassisdef", "mechdef");

                    // Replace
                    m = ___dataManager.MechDefs.Get(stockMechDefId);
                    Logger.Debug("[Contract_CreateAndAddMechPart_PREFIX] MechDef (" + currentMechDefId + ") was replaced with stock version (" + m.Description.Id + ")");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}

