using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;
using Blocks.Gameplay.Core;

namespace FileGame.Core.UI
{
    /// <summary>
    /// PEAK-Style HUD controller for the Stamina-Driven status bar.
    /// v3: Fixed positioning (debuffs right-aligned), ghost energy bar,
    /// centered icons, and Inspector-assignable sprites.
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
        public string toxicStatName = "Toxic";

        [Header("Animation")]
        [Tooltip("Speed of the smooth Lerp animation for bar width changes.")]
        public float animationSpeed = 4f;

        [Header("Custom Icons (Optional)")]
        public Texture2D painIconTexture;
        public Texture2D weightIconTexture;
        public Texture2D hungerIconTexture;
        public Texture2D toxicIconTexture;
        public Texture2D boltIconTexture;
        public Texture2D debuffStripeTexture;

        private UIDocument document;

        // Fill elements
        private VisualElement energyFill;
        private VisualElement energyGhostFill;
        private VisualElement painFill;
        private VisualElement weightFill;
        private VisualElement hungerFill;
        private VisualElement toxicFill;
        private VisualElement extraEnergyFill;

        // Icons
        private VisualElement painIcon;
        private VisualElement weightIcon;
        private VisualElement hungerIcon;
        private VisualElement toxicIcon;
        private VisualElement boltIcon;

        // Track
        private VisualElement mainBarTrack;

        // Hashes
        private int mainEnergyHash;
        private int extraEnergyHash;
        private int painHash;
        private int weightHash;
        private int hungerHash;
        private int toxicHash;

        private CoreStatsHandler localStats;

        // Animated ratios
        private float animEnergyRatio;
        private float animPainRatio;
        private float animWeightRatio;
        private float animHungerRatio;
        private float animToxicRatio;
        private float animExtraRatio;

        // Visual states
        private bool wasPainVisible;
        private bool wasWeightVisible;
        private bool wasHungerVisible;
        private bool wasToxicVisible;

        private void Start() => InitializeUI();

        private void InitializeUI()
        {
            document = GetComponent<UIDocument>();
            mainEnergyHash = Animator.StringToHash(mainEnergyStatName);
            extraEnergyHash = Animator.StringToHash(extraEnergyStatName);
            painHash = Animator.StringToHash(painStatName);
            weightHash = Animator.StringToHash(weightStatName);
            hungerHash = Animator.StringToHash(hungerStatName);
            toxicHash = Animator.StringToHash(toxicStatName);

            if (document?.rootVisualElement != null)
            {
                var root = document.rootVisualElement;
                energyFill = root.Q<VisualElement>("energy-fill");
                energyGhostFill = root.Q<VisualElement>("energy-ghost-fill");
                painFill = root.Q<VisualElement>("pain-fill");
                weightFill = root.Q<VisualElement>("weight-fill");
                hungerFill = root.Q<VisualElement>("hunger-fill");
                toxicFill = root.Q<VisualElement>("toxic-fill");
                extraEnergyFill = root.Q<VisualElement>("extra-energy-fill");

                painIcon = root.Q<VisualElement>("pain-icon");
                weightIcon = root.Q<VisualElement>("weight-icon");
                hungerIcon = root.Q<VisualElement>("hunger-icon");
                toxicIcon = root.Q<VisualElement>("toxic-icon");
                boltIcon = root.Q<VisualElement>("extra-icon-bolt");

                mainBarTrack = root.Q<VisualElement>("main-bar-track");

                SetIconOpacity(painIcon, 0f);
                SetIconOpacity(weightIcon, 0f);
                SetIconOpacity(hungerIcon, 0f);
                SetIconOpacity(toxicIcon, 0f);

                ApplyCustomIcons();
            }
        }

        private void ApplyCustomIcons()
        {
            if (painIconTexture != null && painIcon != null) painIcon.style.backgroundImage = new StyleBackground(painIconTexture);
            if (weightIconTexture != null && weightIcon != null) weightIcon.style.backgroundImage = new StyleBackground(weightIconTexture);
            if (hungerIconTexture != null && hungerIcon != null) hungerIcon.style.backgroundImage = new StyleBackground(hungerIconTexture);
            if (toxicIconTexture != null && toxicIcon != null) toxicIcon.style.backgroundImage = new StyleBackground(toxicIconTexture);
            if (boltIconTexture != null && boltIcon != null) boltIcon.style.backgroundImage = new StyleBackground(boltIconTexture);
            if (debuffStripeTexture != null)
            {
                ApplyStripe(painFill);
                ApplyStripe(weightFill);
                ApplyStripe(hungerFill);
                ApplyStripe(toxicFill);
            }
        }

        private void ApplyStripe(VisualElement e) { if (e != null) e.style.backgroundImage = new StyleBackground(debuffStripeTexture); }

        private void Update()
        {
            if (localStats == null)
            {
                if (NetworkManager.Singleton?.LocalClient?.PlayerObject != null)
                    localStats = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<CoreStatsHandler>();
                if (localStats == null) return;
            }

            if (energyFill == null) return;

            float energyMax = localStats.GetMaxValue(mainEnergyHash);
            if (energyMax <= 0) return;

            // Ratios (0-1) using silent retrieval to avoid console spam for missing optional stats
            float tEnergy = GetStatRatio(mainEnergyHash);
            float tPain = GetStatRatio(painHash);
            float tWeight = GetStatRatio(weightHash);
            float tHunger = GetStatRatio(hungerHash);
            float tToxic = GetStatRatio(toxicHash);
            float tExtra = GetStatRatio(extraEnergyHash);

            float dt = Time.deltaTime * animationSpeed;
            animEnergyRatio = SnapIfClose(Mathf.Lerp(animEnergyRatio, tEnergy, dt), tEnergy);
            animPainRatio = SnapIfClose(Mathf.Lerp(animPainRatio, tPain, dt), tPain);
            animWeightRatio = SnapIfClose(Mathf.Lerp(animWeightRatio, tWeight, dt), tWeight);
            animHungerRatio = SnapIfClose(Mathf.Lerp(animHungerRatio, tHunger, dt), tHunger);
            animToxicRatio = SnapIfClose(Mathf.Lerp(animToxicRatio, tToxic, dt), tToxic);
            animExtraRatio = SnapIfClose(Mathf.Lerp(animExtraRatio, tExtra, dt), tExtra);

            // 1. Ghost Bar (faint green) fills total available capacity (100% - debuffs)
            float totalDebuff = animPainRatio + animWeightRatio + animHungerRatio + animToxicRatio;
            float capacityRatio = Mathf.Clamp01(1f - totalDebuff);
            UpdateBar(energyGhostFill, capacityRatio, 0);

            // 2. Main Energy fills actual energy from left
            UpdateBar(energyFill, animEnergyRatio, 0);

            // 3. Debuffs anchor to the right
            // hunger is rightmost, then weight, then pain, then toxic
            UpdateBar(hungerFill, animHungerRatio, 1f - animHungerRatio);
            UpdateBar(weightFill, animWeightRatio, 1f - animHungerRatio - animWeightRatio);
            UpdateBar(painFill, animPainRatio, 1f - animHungerRatio - animWeightRatio - animPainRatio);
            UpdateBar(toxicFill, animToxicRatio, 1f - totalDebuff);

            // 4. Extra energy
            if (extraEnergyFill != null) UpdateBarWidth(extraEnergyFill, animExtraRatio);

            // 5. Icons centering & alpha
            UpdateIconVisibility(painIcon, animPainRatio, ref wasPainVisible);
            UpdateIconVisibility(weightIcon, animWeightRatio, ref wasWeightVisible);
            UpdateIconVisibility(hungerIcon, animHungerRatio, ref wasHungerVisible);
            UpdateIconVisibility(toxicIcon, animToxicRatio, ref wasToxicVisible);
            PositionIcons();
        }

        private void PositionIcons()
        {
            if (mainBarTrack == null) return;
            float tw = mainBarTrack.resolvedStyle.width;
            if (tw <= 0) return;

            float hw = 14f; // half icon width
            float padding = 4f; // track padding offset

            float totalDebuff = animHungerRatio + animWeightRatio + animPainRatio + animToxicRatio;

            // Centers are middle of each bar's range
            if (animHungerRatio > 0.001f) PositionIcon(hungerIcon, (1f - animHungerRatio * 0.5f) * tw - hw + padding);
            if (animWeightRatio > 0.001f) PositionIcon(weightIcon, (1f - animHungerRatio - animWeightRatio * 0.5f) * tw - hw + padding);
            if (animPainRatio > 0.001f) PositionIcon(painIcon, (1f - animHungerRatio - animWeightRatio - animPainRatio * 0.5f) * tw - hw + padding);
            if (animToxicRatio > 0.001f) PositionIcon(toxicIcon, (1f - totalDebuff + animToxicRatio * 0.5f) * tw - hw + padding);
        }

        private void PositionIcon(VisualElement i, float x) { if (i != null) i.style.left = Mathf.Max(0f, x); }

        private void UpdateIconVisibility(VisualElement i, float r, ref bool v)
        {
            if (i == null) return;
            bool isV = r > 0.005f;
            if (isV && !v) { SetIconOpacity(i, 1f); v = true; }
            else if (!isV && v) { SetIconOpacity(i, 0f); v = false; }
        }

        private void SetIconOpacity(VisualElement i, float o) { if (i != null) i.style.opacity = o; }

        private void UpdateBar(VisualElement e, float r, float leftRatio)
        {
            if (e == null) return;
            if (r <= 0.001f) { e.style.display = DisplayStyle.None; }
            else
            {
                e.style.display = DisplayStyle.Flex;
                e.style.width = Length.Percent(r * 100f);
                e.style.left = Length.Percent(leftRatio * 100f);
                
                // Add 2px gap between segments. Absolute positioning still obeys margins.
                bool isDebuff = e != energyFill && e != energyGhostFill;
                e.style.marginLeft = (isDebuff && leftRatio > 0.001f) ? 2f : 0f;
            }
        }

        private void UpdateBarWidth(VisualElement e, float r)
        {
            if (e == null) return;
            if (r <= 0.001f) e.style.display = DisplayStyle.None;
            else { e.style.display = DisplayStyle.Flex; e.style.width = Length.Percent(r * 100f); }
        }

        private float GetStatRatio(int statHash)
        {
            if (localStats == null) return 0f;
            float max = localStats.GetMaxValue(statHash, false);
            if (max <= 0f) return 0f;
            return Mathf.Clamp01(localStats.GetCurrentValue(statHash, false) / max);
        }

        private float SnapIfClose(float c, float t) => Mathf.Abs(c - t) < 0.002f ? t : c;
    }
}
