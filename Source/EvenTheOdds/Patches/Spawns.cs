﻿using System;
using Harmony;
using BattleTech;
using BattleTech.Data;
using HBS.Collections;
using BattleTech.Framework;

namespace EvenTheOdds.Patches
{
    // Get and Save some Contract/Progression information
    [HarmonyPatch(typeof(Contract), "Begin")]
    public static class Contract_Begin_Patch
    {
        public static void Prefix(Contract __instance)
        {
            try
            {
                Logger.Debug("----------------------------------------------------------------------------------------------------");
                Logger.Debug("-------------------------------------------------");
                Logger.Debug("-------------------------------");
                Logger.Debug("[Contract_Begin_PREFIX] Contract.Name: " + __instance.Name);
                Logger.Debug("[Contract_Begin_PREFIX] Contract.Difficulty: " + __instance.Difficulty);

                SimGameState simGameState = __instance.BattleTechGame.Simulation;
                Logger.Debug("[Contract_Begin_PREFIX] simGameState.CompanyTags: " + simGameState.CompanyTags);
                Logger.Debug("[Contract_Begin_PREFIX] simGameState.DaysPassed: " + simGameState.DaysPassed);
                Logger.Debug("[Contract_Begin_PREFIX] simGameState.GlobalDifficulty: " + simGameState.GlobalDifficulty);

                //Fields.GlobalDifficulty = (int)simGameState.GlobalDifficulty;
                Fields.GlobalDifficulty = Utilities.GetNormalizedGlobalDifficulty(simGameState);
                Logger.Debug("[Contract_Begin_PREFIX] Fields.GlobalDifficulty: " + Fields.GlobalDifficulty);

                Fields.CurrentContractPlusPlusPlusUnits = 0;

                Fields.MaxAllowedExtraThreatLevelByProgression = Utilities.GetMaxAllowedExtraThreatLevelByProgression(simGameState.DaysPassed, simGameState.CompanyTags);
                Fields.MaxAllowedCustomUnitsPerLance = Utilities.GetMaxAllowedCustomUnitsByProgression(Fields.GlobalDifficulty);
                Logger.Debug("[Contract_Begin_PREFIX] Fields.MaxAllowedExtraThreatLevelByProgression: " + Fields.MaxAllowedExtraThreatLevelByProgression);
                Logger.Debug("[Contract_Begin_PREFIX] Fields.MaxAllowedCustomUnitsPerLance: " + Fields.MaxAllowedCustomUnitsPerLance);

                /*
                Logger.Debug("[Contract_Begin_PREFIX] simGameState.IsCampaign: " + simGameState.IsCampaign);
                Logger.Debug("[Contract_Begin_PREFIX] simGameState.isCareerMode(): " + simGameState.IsCareerMode());
                Logger.Debug("[Contract_Begin_PREFIX] simGameState.TargetSystem.Name: " + simGameState.TargetSystem.Name);
                Logger.Debug("[Contract_Begin_PREFIX] simGameState.TargetSystem.Stats: " + simGameState.TargetSystem.Stats);
                Logger.Debug("[Contract_Begin_PREFIX] simGameState.TargetSystem.Tags: " + simGameState.TargetSystem.Tags);

                Logger.Debug("[Contract_Begin_PREFIX] contract.Name: " + __instance.Name);
                Logger.Debug("[Contract_Begin_PREFIX] contract.Difficulty: " + __instance.Difficulty);
                Logger.Debug("[Contract_Begin_PREFIX] contract.IsStoryContract: " + __instance.IsStoryContract);
                */
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }


    // Mess around with unit spawns
    [HarmonyPatch(typeof(UnitSpawnPointOverride), "GenerateUnit")]
    public static class UnitSpawnPointOverride_GenerateUnit_Patch
    {
        public static void Postfix(UnitSpawnPointOverride __instance, DataManager ___dataManager, string lanceName)
        {
            try
            {
                Logger.Debug("----------------------------------------------------------------------------------------------------");

                // Ignore untagged (players lance, empty spawnpoints, or manually defined in the contracts json) units
                if (!__instance.IsUnitDefTagged || !__instance.IsPilotDefTagged)
                {
                    //Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] UnitSpawnPointOverride.IsUnitDefTagged: " + __instance.IsUnitDefTagged);
                    //Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] UnitSpawnPointOverride.IsPilotDefTagged: " + __instance.IsPilotDefTagged);
                    Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] Unit or Pilot was either specified exactly via configuration or excluded from spawning. Aborting.");
                    return;
                }

                // Ignore all units that are NOT Mechs
                if (__instance.selectedUnitType != UnitType.Mech)
                {
                    Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] Unit is not a Mech. Aborting.");
                    return;
                }



                // Info
                Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] ---");
                Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] UnitSpawnPointOverride.selectedUnitDefId: " + __instance.selectedUnitDefId);
                Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] UnitSpawnPointOverride.selectedUnitType: " + __instance.selectedUnitType);
                Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] UnitSpawnPointOverride.selectedPilotDefId: " + __instance.selectedPilotDefId);
                Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] ---");

                // Prepare vars
                string selectedMechDefId = __instance.selectedUnitDefId;
                MechDef selectedMechDef = null;
                string replacementMechDefId = "";
                string replacementPilotDefId = "";
                int finalThreatLevel = 0;

                // Get data
                Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] selectedMechDefId(" + selectedMechDefId + ") is requested from DataManager...");
                if (!___dataManager.MechDefs.TryGet(selectedMechDefId, out selectedMechDef))
                {
                    Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] selectedMechDefId(" + selectedMechDefId + ") couldn't get fetched. Aborting.");
                    return;
                }
                else
                {
                    Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] selectedMechDefId(" + selectedMechDefId + ") successfully requested. Continuing.");
                }

                // Check lance info
                if (Fields.CurrentLanceName != lanceName)
                {
                    Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] Lance (" + lanceName + ") is a new lance, resetting counters");
                    Fields.CurrentLanceName = lanceName;
                    Fields.CurrentLanceCustomUnitCount = 0;
                }



                // Prepare load requests
                LoadRequest loadRequest = ___dataManager.CreateLoadRequest(null, false);



                // Check for custom units
                if (selectedMechDef.MechTags.Contains("unit_madlabs"))
                {
                    Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] selectedMechDefId(" + selectedMechDefId + ") is a custom unit. Adjusting.");

                    // Count
                    Fields.CurrentLanceCustomUnitCount++;

                    // Collect constraints
                    Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] Fields.CurrentLanceCustomUnitCount: " + Fields.CurrentLanceCustomUnitCount);
                    Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] Fields.MaxAllowedCustomUnitsPerLance: " + Fields.MaxAllowedCustomUnitsPerLance);

                    int selectedMechsExtraThreatLevel = Utilities.GetExtraThreatLevelByTag(selectedMechDef.MechTags);
                    Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] selectedMechsExtraThreatLevel: " + selectedMechsExtraThreatLevel);

                    Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] Fields.MaxAllowedExtraThreatLevelByProgression: " + Fields.MaxAllowedExtraThreatLevelByProgression);
                    int allowedExtraThreatLevel = Fields.MaxAllowedExtraThreatLevelByProgression;
                    Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] allowedExtraThreatLevel: " + allowedExtraThreatLevel);
                    
                    // Limit triple PPP Units to X per contract
                    if (selectedMechsExtraThreatLevel == 3 && allowedExtraThreatLevel == 3)
                    {
                        Fields.CurrentContractPlusPlusPlusUnits++;

                        if (Fields.CurrentContractPlusPlusPlusUnits > Fields.MaxAllowedPlusPlusPlusUnitsPerContract)
                        {
                            Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] Already pulled a PlusPusPlus Unit, reducing allowedExtraThreatLevel to 2");
                            allowedExtraThreatLevel = 2;
                        }
                    }
                    
                    // Limit custom units per lance
                    if (Fields.CurrentLanceCustomUnitCount > Fields.MaxAllowedCustomUnitsPerLance)
                    {
                        Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] Already "+ Fields.MaxAllowedCustomUnitsPerLance + " custom units in this lance, reducing allowedExtraThreatLevel to 0");
                        allowedExtraThreatLevel = 0;
                    }
                    Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] allowedExtraThreatLevel: " + allowedExtraThreatLevel);



                    // Replace if necessary
                    if (selectedMechsExtraThreatLevel > allowedExtraThreatLevel)
                    {
                        // Replace with less powerful version of the same Mech (Fallback to STOCK included)
                        replacementMechDefId = Utilities.GetMechDefIdBasedOnSameChassis(selectedMechDef.ChassisID, allowedExtraThreatLevel, ___dataManager);
                        __instance.selectedUnitDefId = replacementMechDefId;

                        finalThreatLevel = allowedExtraThreatLevel;

                        // Add to load request
                        loadRequest.AddBlindLoadRequest(BattleTechResourceType.MechDef, __instance.selectedUnitDefId, new bool?(false));
                    }
                    else
                    {
                        finalThreatLevel = selectedMechsExtraThreatLevel;
                    }
                }
                else
                {
                    Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] selectedMechDefId(" + selectedMechDefId + ") is no custom Unit. Let it pass unchanged...");
                }



                // Pilot handling
                replacementPilotDefId = Utilities.GetPilotIdForMechDef(selectedMechDef, __instance.selectedPilotDefId, __instance.pilotTagSet, finalThreatLevel, Fields.GlobalDifficulty);
                __instance.selectedPilotDefId = replacementPilotDefId;

                // Add new pilot to load request
                loadRequest.AddBlindLoadRequest(BattleTechResourceType.PilotDef, __instance.selectedPilotDefId, new bool?(false));



                // Fire load requests
                loadRequest.ProcessRequests(1000u);

                Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] ---");
                Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] CHECK UnitSpawnPointOverride.selectedUnitDefId: " + __instance.selectedUnitDefId);
                Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] CHECK UnitSpawnPointOverride.selectedUnitType: " + __instance.selectedUnitType);
                Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] CHECK UnitSpawnPointOverride.selectedPilotDefId: " + __instance.selectedPilotDefId);
                Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] ---");
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }

    // Info
    [HarmonyPatch(typeof(UnitSpawnPointOverride), "SelectTaggedUnitDef")]
    public static class UnitSpawnPointOverride_SelectTaggedUnitDef_Patch
    {
        public static void Postfix(UnitSpawnPointOverride __instance, UnitDef_MDD __result, MetadataDatabase mdd, TagSet unitTagSet, TagSet unitExcludedTagSet, string lanceName)
        {
            try
            {
                Logger.Debug("----------------------------------------------------------------------------------------------------");
                Logger.Debug("[UnitSpawnPointOverride_SelectTaggedUnitDef_POSTFIX] lanceName: " + lanceName);
                Logger.Debug("[UnitSpawnPointOverride_SelectTaggedUnitDef_POSTFIX] __result.UnitDefID: " + __result.UnitDefID);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }

    // Info
    [HarmonyPatch(typeof(UnitSpawnPointGameLogic), "SpawnUnit")]
    public static class UnitSpawnPointGameLogic_SpawnUnit_Patch
    {
        public static void Prefix(UnitSpawnPointGameLogic __instance, MechDef ___mechDefOverride)
        {
            try
            {
                Logger.Debug("[UnitSpawnPointGameLogic_SpawnUnit_PREFIX] UnitDefId: " + __instance.UnitDefId);
                if (___mechDefOverride != null)
                {
                    Logger.Debug("[UnitSpawnPointGameLogic_SpawnUnit_PREFIX] Overridden with: " + ___mechDefOverride.Description.Id);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}
