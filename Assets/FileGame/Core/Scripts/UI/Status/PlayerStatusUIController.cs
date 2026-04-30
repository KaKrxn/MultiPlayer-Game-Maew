using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;
using Blocks.Gameplay.Core;

namespace FileGame.Core.UI
{
    /// <summary>
    /// Global HUD controller for the Stamina-Driven status bar.
    /// This should be placed in the scene (on a global UI object) and will automatically
    /// find and bind to the local player's stats.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class PlayerStatusUIController : MonoBehaviour
    {
        [Header("Stat Names")]
        public string mainEnergyStatName = "Health";
        public string extraEnergyStatName = "Energy";
        public string painStatName = "Pain";
        public string weightStatName = "Weight";
        public string hungerStatName = "Hunger";

        private UIDocument document;
        private VisualElement energyFill;
        private VisualElement painFill;
        private VisualElement weightFill;
        private VisualElement hungerFill;
        private VisualElement extraEnergyFill;

        private int mainEnergyHash;
        private int extraEnergyHash;
        private int painHash;
        private int weightHash;
        private int hungerHash;

        private CoreStatsHandler localStats;

        private void Start()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            document = GetComponent<UIDocument>();
            mainEnergyHash = Animator.StringToHash(mainEnergyStatName);
            extraEnergyHash = Animator.StringToHash(extraEnergyStatName);
            painHash = Animator.StringToHash(painStatName);
            weightHash = Animator.StringToHash(weightStatName);
            hungerHash = Animator.StringToHash(hungerStatName);

            if (document != null && document.rootVisualElement != null)
            {
                var root = document.rootVisualElement;
                energyFill = root.Q<VisualElement>("energy-fill");
                painFill = root.Q<VisualElement>("pain-fill");
                weightFill = root.Q<VisualElement>("weight-fill");
                hungerFill = root.Q<VisualElement>("hunger-fill");
                extraEnergyFill = root.Q<VisualElement>("extra-energy-fill");
            }
        }

        private void Update()
        {
            // Find local player stats if not already found
            if (localStats == null)
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null)
                {
                    var playerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
                    if (playerObj != null)
                    {
                        localStats = playerObj.GetComponent<CoreStatsHandler>();
                    }
                }

                if (localStats == null) return; // Still waiting for player spawn
            }

            if (energyFill == null) return;

            float energyMax = localStats.GetMaxValue(mainEnergyHash);
            if (energyMax <= 0) return;

            float energy = localStats.GetCurrentValue(mainEnergyHash);
            float pain = localStats.GetCurrentValue(painHash);
            float weight = localStats.GetCurrentValue(weightHash);
            float hunger = localStats.GetCurrentValue(hungerHash);
            float extraEnergy = localStats.GetCurrentValue(extraEnergyHash);

            float painMax = localStats.GetMaxValue(painHash);
            float weightMax = localStats.GetMaxValue(weightHash);
            float hungerMax = localStats.GetMaxValue(hungerHash);
            float extraEnergyMax = localStats.GetMaxValue(extraEnergyHash);
            
            float hungerRatio = hungerMax > 0 ? hunger / hungerMax : 0f;
            float weightRatio = weightMax > 0 ? weight / weightMax : 0f;
            float painRatio = painMax > 0 ? pain / painMax : 0f;

            float totalDebuffRatio = hungerRatio + weightRatio + painRatio;
            totalDebuffRatio = Mathf.Clamp01(totalDebuffRatio);
            
            // The green bar simply uses the ratio of current energy to its asset max.
            // Since the server already caps 'energy' to (100 - debuffs), 
            // a 'full' energy bar will naturally touch the debuff bars on the right.
            float visualEnergyRatio = Mathf.Clamp01(energy / energyMax);

            UpdateElementWidth(energyFill, visualEnergyRatio);
            UpdateElementWidth(painFill, painRatio);
            UpdateElementWidth(weightFill, weightRatio);
            UpdateElementWidth(hungerFill, hungerRatio);
            
            if (extraEnergyFill != null)
            {
                float extraRatio = extraEnergyMax > 0 ? extraEnergy / extraEnergyMax : 0f;
                UpdateElementWidth(extraEnergyFill, extraRatio);
            }
        }

        private void UpdateElementWidth(VisualElement element, float ratio)
        {
            if (element == null) return;
            if (ratio <= 0.001f)
            {
                element.style.display = DisplayStyle.None;
            }
            else
            {
                element.style.display = DisplayStyle.Flex;
                element.style.width = Length.Percent(ratio * 100f);
            }
        }
    }
}
