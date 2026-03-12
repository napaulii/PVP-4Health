using System;
using UnityEngine;

namespace FortressBuilder.Core
{
    public class Fortress : MonoBehaviour
    {
        [Header("Current Status")]
        [SerializeField] private int level = 1;
        [SerializeField] private int currentXp = 0;

        [Header("Leveling Logic")]
        [Tooltip("Total Cumulative XP required to REACH the next level. Index 0 = XP for Lv2, Index 1 = XP for Lv3, etc.")]
        [SerializeField]
        private int[] xpThresholds = new int[]
        {
            100,  // Required to reach Level 2
            300,  // Required to reach Level 3
            600,  // Required to reach Level 4
            1000, // Required to reach Level 5
            2000  // Required to reach Level 6
        }; [Header("Visual Appearance")]
        [Tooltip("Drag the GameObjects (3D models/sprites) for each level here. Index 0 = Lv1, Index 1 = Lv2, etc.")]
        [SerializeField] private GameObject[] levelModels;

        // ==========================================
        // EVENTS (For UI Progress Bars and Particles)
        // ==========================================

        // Fired when XP changes (Passes: Current XP, Target XP for next level)
        // Target XP will be -1 if max level is reached.
        public event Action<int, int> OnXpUpdated;

        // Fired when the fortress levels up (Passes: New Level)
        public event Action<int> OnLevelUp;

        private void Start()
        {
            UpdateAppearance();
        }


        /// <summary>
        /// Call this when fetching the user's base data from the server.
        /// </summary>
        public void SyncWithServer(int serverLevel, int serverXp)
        {
            currentXp = serverXp;

            if (level != serverLevel)
            {
                level = serverLevel;
                UpdateAppearance();
                OnLevelUp?.Invoke(level);
            }

            // Notify UI
            OnXpUpdated?.Invoke(currentXp, GetXpRequiredForNextLevel());
        }

        public void AddXP(int xpEarned)
        {
            if (xpEarned <= 0) return;

            currentXp += xpEarned;
            Debug.Log($"<color=orange>Fortress</color>: Gained {xpEarned} XP. Total XP: {currentXp}");

            CheckForLevelUp();

            // Notify UI to update the progress bar
            OnXpUpdated?.Invoke(currentXp, GetXpRequiredForNextLevel());
        }

        /// <summary>
        /// Checks if current XP meets or exceeds the threshold for the next level.
        /// </summary>
        private void CheckForLevelUp()
        {
            int requiredXp = GetXpRequiredForNextLevel();

            // If requiredXp is -1, we are at the maximum level
            if (requiredXp == -1) return;

            bool leveledUpThisCheck = false;

            // 'While' loop allows skipping multiple levels if a massive amount of XP was gained
            while (currentXp >= requiredXp && requiredXp != -1)
            {
                level++;
                leveledUpThisCheck = true;
                Debug.Log($"<color=yellow>Fortress Leveled Up!</color> Now Level {level}");

                requiredXp = GetXpRequiredForNextLevel();
            }

            if (leveledUpThisCheck)
            {
                UpdateAppearance();
                OnLevelUp?.Invoke(level);
            }
        }

        // ==========================================
        // APPEARANCE & HELPERS
        // ==========================================

        /// <summary>
        /// Swaps the active GameObject to match the current level.
        /// </summary>
        private void UpdateAppearance()
        {
            if (levelModels == null || levelModels.Length == 0) return;

            // Arrays are 0-indexed, Levels are 1-indexed. (Level 1 is index 0)
            int targetIndex = level - 1;

            // Safety check: clamp the index so we don't get an array error 
            // if the level exceeds the number of models we have assigned.
            targetIndex = Mathf.Clamp(targetIndex, 0, levelModels.Length - 1);

            for (int i = 0; i < levelModels.Length; i++)
            {
                if (levelModels[i] != null)
                {
                    // Turn ON the matching model, turn OFF all others
                    levelModels[i].SetActive(i == targetIndex);
                }
            }
        }

        /// <summary>
        /// Returns the target XP for the next level based on the xpThresholds array.
        /// Returns -1 if max level is reached.
        /// </summary>
        public int GetXpRequiredForNextLevel()
        {
            // Level 1 looks at index 0 (which holds the requirement for Level 2)
            int index = level - 1;

            // Check if we have surpassed the max defined levels
            if (index >= xpThresholds.Length)
            {
                return -1; // Max Level
            }

            return xpThresholds[index];
        }

        // Getters for other scripts
        public int GetCurrentLevel() => level;
        public int GetCurrentXp() => currentXp;
    }
}