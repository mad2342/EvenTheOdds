using System;
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


                Fields.GlobalDifficulty = Utilities.GetNormalizedGlobalDifficulty(simGameState);
                Logger.Debug("[Contract_Begin_PREFIX] Fields.GlobalDifficulty: " + Fields.GlobalDifficulty);

                // Reset counter
                Fields.CurrentContractEliteUnits = 0;

                Fields.MaxThreatLevelByProgression = Utilities.GetMaxThreatLevelByProgression(Fields.GlobalDifficulty);
                Fields.MaxCustomUnitsPerLance = Utilities.GetMaxCustomUnitsPerLance(Fields.GlobalDifficulty);
                //Fields.MaxEliteUnitsPerContract = Utilities.GetMaxEliteUnitsPerContract(Fields.GlobalDifficulty);
                Logger.Debug("[Contract_Begin_PREFIX] Fields.MaxThreatLevelByProgression: " + Fields.MaxThreatLevelByProgression);
                Logger.Debug("[Contract_Begin_PREFIX] Fields.MaxCustomUnitsPerLance: " + Fields.MaxCustomUnitsPerLance);
                Logger.Debug("[Contract_Begin_PREFIX] Fields.MaxEliteUnitsPerContract: " + Fields.MaxEliteUnitsPerContract);

                /*
                Logger.Info("[Contract_Begin_PREFIX] simGameState.IsCampaign: " + simGameState.IsCampaign);
                Logger.Info("[Contract_Begin_PREFIX] simGameState.isCareerMode(): " + simGameState.IsCareerMode());
                Logger.Info("[Contract_Begin_PREFIX] simGameState.TargetSystem.Name: " + simGameState.TargetSystem.Name);
                Logger.Info("[Contract_Begin_PREFIX] simGameState.TargetSystem.Stats: " + simGameState.TargetSystem.Stats);
                Logger.Info("[Contract_Begin_PREFIX] simGameState.TargetSystem.Tags: " + simGameState.TargetSystem.Tags);

                Logger.Info("[Contract_Begin_PREFIX] contract.Name: " + __instance.Name);
                Logger.Info("[Contract_Begin_PREFIX] contract.Difficulty: " + __instance.Difficulty);
                Logger.Info("[Contract_Begin_PREFIX] contract.IsStoryContract: " + __instance.IsStoryContract);
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
                    //Logger.Info("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] UnitSpawnPointOverride.IsUnitDefTagged: " + __instance.IsUnitDefTagged);
                    //Logger.Info("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] UnitSpawnPointOverride.IsPilotDefTagged: " + __instance.IsPilotDefTagged);
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
                int mechThreatLevel = 0;

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
                    Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] selectedMechDefId(" + selectedMechDefId + ") is a custom unit and thus needs adjustment");

                    // Count
                    Fields.CurrentLanceCustomUnitCount++;

                    // Collect constraints
                    Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] Fields.CurrentLanceCustomUnitCount: " + Fields.CurrentLanceCustomUnitCount);
                    Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] Fields.MaxCustomUnitsPerLance: " + Fields.MaxCustomUnitsPerLance);
                    Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] Fields.CurrentContractEliteUnits: " + Fields.CurrentContractEliteUnits);
                    Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] Fields.MaxEliteUnitsPerContract: " + Fields.MaxEliteUnitsPerContract);
                    Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] Fields.MaxThreatLevelByProgression: " + Fields.MaxThreatLevelByProgression);

                    int selectedMechsThreatLevel = Utilities.GetThreatLevelByTag(selectedMechDef.MechTags);
                    Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] selectedMechsThreatLevel: " + selectedMechsThreatLevel);

                    int maxThreatLevel = Fields.MaxThreatLevelByProgression;
                    Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] maxThreatLevel: " + maxThreatLevel);
                    
                    // Limit elite units per contract
                    if (selectedMechsThreatLevel == 3 && maxThreatLevel == 3)
                    {
                        Fields.CurrentContractEliteUnits++;

                        if (Fields.CurrentContractEliteUnits > Fields.MaxEliteUnitsPerContract)
                        {
                            Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] Already pulled an elite unit, reducing maxThreatLevel to 2");
                            maxThreatLevel = 2;
                        }
                    }
                    
                    // Limit custom units per lance
                    if (Fields.CurrentLanceCustomUnitCount > Fields.MaxCustomUnitsPerLance)
                    {
                        Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] Already "+ Fields.MaxCustomUnitsPerLance + " custom units in this lance, reducing maxThreatLevel to 0");
                        maxThreatLevel = 0;
                    }
                    Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] maxThreatLevel: " + maxThreatLevel);



                    // Replace if necessary
                    if (selectedMechsThreatLevel > maxThreatLevel)
                    {
                        // Replace with less powerful version of the same Mech (Fallback to STOCK included)
                        replacementMechDefId = Utilities.GetMechDefIdBasedOnSameChassis(selectedMechDef.ChassisID, maxThreatLevel, ___dataManager);
                        __instance.selectedUnitDefId = replacementMechDefId;

                        mechThreatLevel = maxThreatLevel;

                        // Add to load request
                        loadRequest.AddBlindLoadRequest(BattleTechResourceType.MechDef, __instance.selectedUnitDefId, new bool?(false));
                    }
                    else
                    {
                        mechThreatLevel = selectedMechsThreatLevel;
                    }
                }
                else
                {
                    Logger.Debug("[UnitSpawnPointOverride_GenerateUnit_POSTFIX] selectedMechDefId(" + selectedMechDefId + ") is no custom Unit. Let it pass unchanged...");
                }



                // Pilot handling (will also potentially place better pilots in stock and light mechs in late game)
                replacementPilotDefId = Utilities.GetPilotIdForMechDef(selectedMechDef, __instance.selectedPilotDefId, __instance.pilotTagSet, mechThreatLevel, Fields.GlobalDifficulty);
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
