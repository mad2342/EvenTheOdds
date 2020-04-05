using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.Data;
using HBS.Collections;
using UnityEngine;

namespace EvenTheOdds
{
    class Utilities
    {
        public static int GetNormalizedGlobalDifficulty(SimGameState simGameState)
        {
            int result = 0;
            int settingsModifier = simGameState.Constants.Story.ContractDifficultyMod;
            Logger.Info("[Utilities.GetNormalizedGlobalDifficulty] settingsModifier: " + settingsModifier);

            if (simGameState.SimGameMode == SimGameState.SimGameType.KAMEA_CAMPAIGN)
            {
                Logger.Info("[Utilities.GetNormalizedGlobalDifficulty] SimGameState.GlobalDifficulty: " + simGameState.GlobalDifficulty);
                result = Mathf.FloorToInt(simGameState.GlobalDifficulty);
                result = Mathf.Clamp(result, 0, 8);
                result += settingsModifier;
            }
            else if (simGameState.SimGameMode == SimGameState.SimGameType.CAREER)
            {
                int daysPassed = simGameState.DaysPassed;
                Logger.Info("[Utilities.GetNormalizedGlobalDifficulty] daysPassed: " + daysPassed);
                int difficultyByDaysPassed = Mathf.FloorToInt(daysPassed / 120);
                Logger.Info("[Utilities.GetNormalizedGlobalDifficulty] difficultyByDaysPassed: " + difficultyByDaysPassed);

                result = Mathf.Clamp(difficultyByDaysPassed, 0, 8);
                result += settingsModifier;
            }
            else
            {
                Logger.Debug("[Utilities.GetNormalizedGlobalDifficulty] WARNING: Couldn't determine normalized difficulty!");
            }

            return result;
        }



        public static int[] GetContractDifficultyVariances(SimGameState.SimGameType gameMode, TagSet companyTags)
        {
            //Logger.Info("[Utilities.GetMaxAllowedContractDifficultyVariance] companyTags: " + companyTags);

            // For CAREER mode ContractDifficulty is based on starsystem difficulty atm...
            if (gameMode == SimGameState.SimGameType.CAREER)
            {
                return new int[] { 1, 1 };
            }

            // To be able to play low difficulty contracts during/after KAMEA_CAMPAIGN the lower variance value slowly rises...
            if (companyTags.Contains("story_complete"))
            {
                return new int[] { 5, 2 };
            }
            else if (companyTags.Contains("oc09_post_damage_report"))
            {
                return new int[] { 3, 1 };
            }
            else if (companyTags.Contains("oc04_post_argo"))
            {
                return new int[] { 2, 1 };
            }
            else
            {
                return new int[] { 1, 1 };
            }
        }



        public static int GetMaxThreatLevelByProgression(int globalDifficulty)
        {
            int result = 0;

            // A global difficulty of >= 9 is only possible with setting "Enemy Force Strength" to "Hard" in Game Options
            if (globalDifficulty >= 9)
            {
                result = 3;
            }
            else if (globalDifficulty >= 7)
            {
                result = 2;
            }
            else if (globalDifficulty >= 4)
            {
                result = 1;
            }
            // Additional limit by difficulty setting (Easy: 1, Normal: 2, Hard: 3)
            result = Mathf.Clamp(result, 0, EvenTheOdds.Settings.Difficulty);

            return result;
        }



        public static int GetMaxCustomUnitsPerLance(int globalDifficulty)
        {
            int result = 0;

            // A global difficulty of >= 9 is only possible with setting "Enemy Force Strength" to "Hard" in Game Options
            if (globalDifficulty >= 9)
            {
                result = 3;
            }
            else if (globalDifficulty >= 7)
            {
                result = 2;
            }
            else if (globalDifficulty >= 4)
            {
                result = 1;
            }
            // Additional limit by difficulty setting (Easy: 1, Normal: 2, Hard: 3)
            result = Mathf.Clamp(result, 0, EvenTheOdds.Settings.Difficulty);

            return result;
        }



        public static int GetMaxEliteUnitsPerContract(int globalDifficulty)
        {
            int result = 0;

            // A global difficulty of >= 9 is only possible with setting "Enemy Force Strength" to "Hard" in Game Options
            if (globalDifficulty >= 9)
            {
                result = 3;
            }
            else if (globalDifficulty >= 8)
            {
                result = 2;
            }
            else if (globalDifficulty >= 7)
            {
                result = 1;
            }

            return result;
        }



        public static string GetMechDefIdBasedOnSameChassis(string chassisID, int threatLevel, DataManager dm)
        {
            string replacementMechDefId = "";

            Logger.Info("[Utilities.GetMechDefIdBasedOnSameChassis] Get replacement for chassisID: " + chassisID + " and threatLevel " + threatLevel);

            // Shortcut for STOCK
            if (threatLevel == 0)
            {
                Logger.Info("[Utilities.GetMechDefIdBasedOnSameChassis] Requested threatlevel is 0. Returning STOCK variant...");
                return chassisID.Replace("chassisdef", "mechdef");
            }

            string mechTagForThreatLevel = Utilities.GetTagByThreatLevel(threatLevel);
            Logger.Info("[Utilities.GetMechDefIdBasedOnSameChassis] mechTagForThreatLevel: " + mechTagForThreatLevel);

            List<MechDef> allMechDefs = new List<MechDef>();
            foreach (string key in dm.MechDefs.Keys)
            {
                MechDef mechDef = dm.MechDefs.Get(key);
                allMechDefs.Add(mechDef);
            }

            List<string> mechDefIdsBasedOnSameChassis = allMechDefs
                .Where(mechDef => mechDef.ChassisID == chassisID)
                .Where(mechDef => mechDef.MechTags.Contains(mechTagForThreatLevel))
                .Select(mechDef => mechDef.Description.Id)
                .ToList();

            foreach (string Id in mechDefIdsBasedOnSameChassis)
            {
                Logger.Info("[Utilities.GetMechDefIdBasedOnSameChassis] mechDefIdsBasedOnSameChassis(" + chassisID + "): " + Id);
            }

            if (mechDefIdsBasedOnSameChassis.Count > 0)
            {
                mechDefIdsBasedOnSameChassis.Shuffle<string>();
                replacementMechDefId = mechDefIdsBasedOnSameChassis[0];
                Logger.Info("[Utilities.GetMechDefIdBasedOnSameChassis] replacementMechDefId: " + replacementMechDefId);
            }
            else
            {
                Logger.Info("[Utilities.GetMechDefIdBasedOnSameChassis] Couldn't find a replacement. Falling back to STOCK...");
                replacementMechDefId = chassisID.Replace("chassisdef", "mechdef");
            }

            return replacementMechDefId;
        }



        public static string GetTagByThreatLevel(int threatLevel)
        {
            switch(threatLevel)
            {
                case 1:
                    return "unit_components_plus";
                case 2:
                    return "unit_components_plusplus";
                case 3:
                    return "unit_components_plusplusplus";
                default:
                    return "unit_components_neutral";
            }
        }



        public static int GetThreatLevelByTag(TagSet mechTags)
        {
            if (mechTags.Contains("unit_components_plusplusplus"))
            {
                return 3;
            }
            else if (mechTags.Contains("unit_components_plusplus"))
            {
                return 2;
            }
            else if (mechTags.Contains("unit_components_plus"))
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }



        public static string GetPilotTypeForMechDef(MechDef mechDef, bool random = false)
        {
            string pilotType = "lancer";
            TagSet mechTags = mechDef.MechTags;
            List<string> availablePilotTypes = new List<string>() { "skirmisher", "lancer", "sharpshooter", "flanker", "outrider", "recon", "gladiator", "brawler", "sentinel", "striker", "scout", "vanguard" };
            List<string> appropiatePilotTypes = new List<string>();

            if(random)
            {
                System.Random rnd = new System.Random();
                int r = rnd.Next(availablePilotTypes.Count);

                Logger.Info("[Utilities.GetPilotTypeForMechDef] Returning random pilotType: " + availablePilotTypes[r]);
                return availablePilotTypes[r];
            }

            if (!mechTags.IsEmpty)
            {
                Logger.Info("[Utilities.GetPilotTypeForMechDef] mechTags: " + mechTags);

                // unit_lance_support, unit_lance_tank, unit_lance_assassin, unit_lance_vanguard
                // unit_role_brawler, unit_role_sniper, unit_role_scout
                // skirmisher, lancer, sharpshooter, flanker, outrider, recon, gladiator, brawler, sentinel, striker, scout, vanguard

                // By logical combination
                if (mechTags.Contains("unit_lance_support") && mechTags.Contains("unit_role_brawler"))
                {
                    appropiatePilotTypes.Add("vanguard");
                }
                if (mechTags.Contains("unit_lance_support") && mechTags.Contains("unit_role_sniper"))
                {
                    appropiatePilotTypes.Add("sharpshooter");
                }
                if (mechTags.Contains("unit_lance_support") && mechTags.Contains("unit_role_scout"))
                {
                    appropiatePilotTypes.Add("recon");
                }

                if (mechTags.Contains("unit_lance_tank") && mechTags.Contains("unit_role_brawler"))
                {
                    appropiatePilotTypes.Add("gladiator");
                }
                if (mechTags.Contains("unit_lance_tank") && mechTags.Contains("unit_role_sniper"))
                {
                    appropiatePilotTypes.Add("lancer");
                }
                if (mechTags.Contains("unit_lance_tank") && mechTags.Contains("unit_role_scout"))
                {
                    appropiatePilotTypes.Add("outrider");
                }

                if (mechTags.Contains("unit_lance_assassin") && mechTags.Contains("unit_role_brawler"))
                {
                    appropiatePilotTypes.Add("brawler");
                }
                if (mechTags.Contains("unit_lance_assassin") && mechTags.Contains("unit_role_sniper"))
                {
                    appropiatePilotTypes.Add("skirmisher");
                }
                if (mechTags.Contains("unit_lance_assassin") && mechTags.Contains("unit_role_scout"))
                {
                    appropiatePilotTypes.Add("flanker");
                }

                if (mechTags.Contains("unit_lance_vanguard") && mechTags.Contains("unit_role_brawler"))
                {
                    appropiatePilotTypes.Add("scout");
                }
                if (mechTags.Contains("unit_lance_vanguard") && mechTags.Contains("unit_role_sniper"))
                {
                    appropiatePilotTypes.Add("striker");
                }
                if (mechTags.Contains("unit_lance_vanguard") && mechTags.Contains("unit_role_scout"))
                {
                    appropiatePilotTypes.Add("sentinel");
                }

                // Add variety by single roles
                if (mechTags.Contains("unit_role_brawler"))
                {
                    appropiatePilotTypes.Add("lancer");
                    appropiatePilotTypes.Add("skirmisher");
                }
                if (mechTags.Contains("unit_role_sniper"))
                {
                    appropiatePilotTypes.Add("sharpshooter");
                }
                if (mechTags.Contains("unit_role_scout"))
                {
                    appropiatePilotTypes.Add("recon");
                    appropiatePilotTypes.Add("scout");
                }

                // Add variety by special tags
                if (mechTags.Contains("unit_indirectFire"))
                {
                    appropiatePilotTypes.Add("vanguard");
                    appropiatePilotTypes.Add("striker");
                }

                // Add variety by Chassis
                if (mechDef.ChassisID.Contains("hatchetman") || mechDef.ChassisID.Contains("dragon") || mechDef.ChassisID.Contains("banshee"))
                {
                    appropiatePilotTypes.Add("brawler");
                    appropiatePilotTypes.Add("gladiator");
                }
            }

            if (appropiatePilotTypes.Count > 0)
            {
                foreach (string Type in appropiatePilotTypes)
                {
                    Logger.Info("[Utilities.GetPilotTypeForMechDef] appropiatePilotTypes: " + Type);
                }
                appropiatePilotTypes.Shuffle<string>();
                pilotType = appropiatePilotTypes[0];
            }
            Logger.Info("[Utilities.GetPilotTypeForMechDef] Selected pilotType: " + pilotType);

            return pilotType;
        }



        public static int GetPilotSkillLevel(TagSet pilotTagSet)
        {
            if (pilotTagSet.Contains("pilot_npc_d10"))
            {
                return 10;
            }
            if (pilotTagSet.Contains("pilot_npc_d9"))
            {
                return 9;
            }
            if (pilotTagSet.Contains("pilot_npc_d8"))
            {
                return 8;
            }
            if (pilotTagSet.Contains("pilot_npc_d7"))
            {
                return 7;
            }
            if (pilotTagSet.Contains("pilot_npc_d6"))
            {
                return 6;
            }
            if (pilotTagSet.Contains("pilot_npc_d5"))
            {
                return 5;
            }
            if (pilotTagSet.Contains("pilot_npc_d4"))
            {
                return 4;
            }
            if (pilotTagSet.Contains("pilot_npc_d3"))
            {
                return 3;
            }
            if (pilotTagSet.Contains("pilot_npc_d2"))
            {
                return 2;
            }
            if (pilotTagSet.Contains("pilot_npc_d1"))
            {
                return 1;
            }
            // Default
            return 7;
        }



        public static string BuildPilotDefIdFromSkillAndSpec(int skillLevel, string pilotSpecialization)
        {
            return "pilot_d" + skillLevel + "_" + pilotSpecialization;
        }



        public static string GetPilotIdForMechDef(MechDef mechDef, string currentPilotDefId, TagSet currentPilotTagSet, int threatLevel, int minimumSkillLevel)
        {
            // If no replacement is appropiate fall back to original PilotDef
            string replacementPilotDefId = currentPilotDefId;
            int currentSkillLevel = Utilities.GetPilotSkillLevel(currentPilotTagSet);
            Logger.Info("[Utilities.GetPilotIdForMechDef] currentSkillLevel: " + currentSkillLevel);

            int requestedSkillLevel = 0;
            switch (threatLevel)
            {
                case 0:
                    requestedSkillLevel = minimumSkillLevel;
                    break;
                case 1:
                    requestedSkillLevel = 10;
                    break;
                case 2:
                    requestedSkillLevel = 11;
                    break;
                case 3:
                    requestedSkillLevel = 12;
                    break;
            }
            Logger.Info("[Utilities.GetPilotIdForMechDef] requestedSkillLevel: " + requestedSkillLevel);

            // Specialization starts at difficulty of 7
            if (requestedSkillLevel > 7 && requestedSkillLevel > currentSkillLevel)
            {
                string pilotSpecialization = Utilities.GetPilotTypeForMechDef(mechDef);
                Logger.Info("[Utilities.GetPilotIdForMechDef] pilotSpecialization: " + pilotSpecialization);
                replacementPilotDefId = Utilities.BuildPilotDefIdFromSkillAndSpec(requestedSkillLevel, pilotSpecialization);
            }

            Logger.Info("[Utilities.GetPilotIdForMechDef] replacementPilotDefId: " + replacementPilotDefId);
            return replacementPilotDefId;
        }
    }
}
