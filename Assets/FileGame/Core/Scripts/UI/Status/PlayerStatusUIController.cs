using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;
using Blocks.Gameplay.Core;

namespace FileGame.Core.UI
{
    /// <summary>
    /// PEAK-Style HUD controller for the Stamina-Driven status bar.
    /// v4: Added UV-tiling for debuff stripes to prevent stretching (Infinite Stretch),
    /// fixed right-anchored positioning, ghost bar, and centered icons.
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

        [Header("Animation")]
        public float animationSpeed = 4f;

        [Header("Custom Icons (Optional)")]
        public Texture2D painIconTexture;
        public Texture2D weightIconTexture;
        public Texture2D hungerIconTexture;
        public Texture2D boltIconTexture;
        public Texture2D debuffStripeTexture;

        private UIDocument document;
        private VisualElement energyFill, energyGhostFill, painFill, weightFill, hungerFill, extraEnergyFill;
        private VisualElement painIcon, weightIcon, hungerIcon, boltIcon;
        private VisualElement mainBarTrack;

        private int mainEnergyHash, extraEnergyHash, painHash, weightHash, hungerHash;
        private CoreStatsHandler localStats;
        private float animEnergyRatio, animPainRatio, animWeightRatio, animHungerRatio, animExtraRatio;
        private bool wasPainVisible, wasWeightVisible, wasHungerVisible;

        // Texture for tiling (cached from Resources or Inspector)
        private Texture2D stripeTex;

        private void Start() => InitializeUI();

        private void InitializeUI()
        {
            document = GetComponent<UIDocument>();
            mainEnergyHash = Animator.StringToHash(mainEnergyStatName);
            extraEnergyHash = Animator.StringToHash(extraEnergyStatName);
            painHash = Animator.StringToHash(painStatName);
            weightHash = Animator.StringToHash(weightStatName);
            hungerHash = Animator.StringToHash(hungerStatName);

            if (document?.rootVisualElement != null)
            {
                var root = document.rootVisualElement;
                energyFill = root.Q<VisualElement>("energy-fill");
                energyGhostFill = root.Q<VisualElement>("energy-ghost-fill");
                painFill = root.Q<VisualElement>("pain-fill");
                weightFill = root.Q<VisualElement>("weight-fill");
                hungerFill = root.Q<VisualElement>("hunger-fill");
                extraEnergyFill = root.Q<VisualElement>("extra-energy-fill");

                painIcon = root.Q<VisualElement>("pain-icon");
                weightIcon = root.Q<VisualElement>("weight-icon");
                hungerIcon = root.Q<VisualElement>("hunger-icon");
                boltIcon = root.Q<VisualElement>("extra-icon-bolt");

                mainBarTrack = root.Q<VisualElement>("main-bar-track");

                // Setup UV-tiling for debuff bars
                stripeTex = debuffStripeTexture != null ? debuffStripeTexture : Resources.Load<Texture2D>("UI/DebuffStripePattern");
                
                // We draw the stripes manually to ensure they tile perfectly without stretching
                SetupTiledBackground(painFill);
                SetupTiledBackground(weightFill);
                SetupTiledBackground(hungerFill);

                SetIconOpacity(painIcon, 0f);
                SetIconOpacity(weightIcon, 0f);
                SetIconOpacity(hungerIcon, 0f);

                ApplyCustomIcons();
            }
        }

        private void SetupTiledBackground(VisualElement e)
        {
            if (e == null) return;
            // Register callback to draw custom tiled mesh
            e.generateVisualContent += OnGenerateTiledBackground;
            // Remove the default background-image to avoid overlapping
            e.style.backgroundImage = null;
        }

        /// <summary>
        /// Draws a tiled background for the debuff bars to prevent stretching.
        /// This ensures the diagonal lines always stay the same size.
        /// </summary>
        private void OnGenerateTiledBackground(MeshGenerationContext mgc)
        {
            if (stripeTex == null) return;

            var ve = mgc.visualElement;
            var rect = ve.contentRect;
            if (rect.width <= 0 || rect.height <= 0) return;

            // Get the color from the USS (background-image-tint-color)
            Color tint = ve.resolvedStyle.unityBackgroundImageTintColor;

            // Calculate UVs for tiling. Texture should be 64x64 or similar.
            // We want it to tile based on height to keep aspect ratio.
            float uRepeat = rect.width / rect.height;

            // Texture is passed directly to Allocate in UI Toolkit
            var mesh = mgc.Allocate(4, 6, stripeTex);
            
            // Vertices
            mesh.SetNextVertex(new Vertex { position = new Vector3(0, 0, 0), uv = new Vector2(0, 1), tint = tint });
            mesh.SetNextVertex(new Vertex { position = new Vector3(rect.width, 0, 0), uv = new Vector2(uRepeat, 1), tint = tint });
            mesh.SetNextVertex(new Vertex { position = new Vector3(rect.width, rect.height, 0), uv = new Vector2(uRepeat, 0), tint = tint });
            mesh.SetNextVertex(new Vertex { position = new Vector3(0, rect.height, 0), uv = new Vector2(0, 0), tint = tint });

            // Triangles
            mesh.SetNextIndex(0); mesh.SetNextIndex(1); mesh.SetNextIndex(2);
            mesh.SetNextIndex(2); mesh.SetNextIndex(3); mesh.SetNextIndex(0);
        }

        private void ApplyCustomIcons()
        {
            if (painIconTexture != null && painIcon != null) painIcon.style.backgroundImage = new StyleBackground(painIconTexture);
            if (weightIconTexture != null && weightIcon != null) weightIcon.style.backgroundImage = new StyleBackground(weightIconTexture);
            if (hungerIconTexture != null && hungerIcon != null) hungerIcon.style.backgroundImage = new StyleBackground(hungerIconTexture);
            if (boltIconTexture != null && boltIcon != null) boltIcon.style.backgroundImage = new StyleBackground(boltIconTexture);
            
            // If stripe texture changed via Inspector, trigger repaint
            if (debuffStripeTexture != null)
            {
                stripeTex = debuffStripeTexture;
                painFill?.MarkDirtyRepaint();
                weightFill?.MarkDirtyRepaint();
                hungerFill?.MarkDirtyRepaint();
            }
        }

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

            float tEnergy = Mathf.Clamp01(localStats.GetCurrentValue(mainEnergyHash) / energyMax);
            float tPain = Mathf.Clamp01(localStats.GetCurrentValue(painHash) / localStats.GetMaxValue(painHash));
            float tWeight = Mathf.Clamp01(localStats.GetCurrentValue(weightHash) / localStats.GetMaxValue(weightHash));
            float tHunger = Mathf.Clamp01(localStats.GetCurrentValue(hungerHash) / localStats.GetMaxValue(hungerHash));
            float tExtra = Mathf.Clamp01(localStats.GetCurrentValue(extraEnergyHash) / localStats.GetMaxValue(extraEnergyHash));

            float dt = Time.deltaTime * animationSpeed;
            animEnergyRatio = SnapIfClose(Mathf.Lerp(animEnergyRatio, tEnergy, dt), tEnergy);
            animPainRatio = SnapIfClose(Mathf.Lerp(animPainRatio, tPain, dt), tPain);
            animWeightRatio = SnapIfClose(Mathf.Lerp(animWeightRatio, tWeight, dt), tWeight);
            animHungerRatio = SnapIfClose(Mathf.Lerp(animHungerRatio, tHunger, dt), tHunger);
            animExtraRatio = SnapIfClose(Mathf.Lerp(animExtraRatio, tExtra, dt), tExtra);

            float totalDebuff = animPainRatio + animWeightRatio + animHungerRatio;
            UpdateBar(energyGhostFill, Mathf.Clamp01(1f - totalDebuff), 0);
            UpdateBar(energyFill, animEnergyRatio, 0);

            UpdateBar(hungerFill, animHungerRatio, 1f - animHungerRatio);
            UpdateBar(weightFill, animWeightRatio, 1f - animHungerRatio - animWeightRatio);
            UpdateBar(painFill, animPainRatio, 1f - totalDebuff);

            if (extraEnergyFill != null) UpdateBarWidth(extraEnergyFill, animExtraRatio);

            UpdateIconVisibility(painIcon, animPainRatio, ref wasPainVisible);
            UpdateIconVisibility(weightIcon, animWeightRatio, ref wasWeightVisible);
            UpdateIconVisibility(hungerIcon, animHungerRatio, ref wasHungerVisible);
            PositionIcons();
        }

        private void PositionIcons()
        {
            if (mainBarTrack == null) return;
            float tw = mainBarTrack.resolvedStyle.width;
            if (tw <= 0) return;
            float hw = 14f, padding = 4f;
            if (animHungerRatio > 0.001f) PositionIcon(hungerIcon, (1f - animHungerRatio * 0.5f) * tw - hw + padding);
            if (animWeightRatio > 0.001f) PositionIcon(weightIcon, (1f - animHungerRatio - animWeightRatio * 0.5f) * tw - hw + padding);
            if (animPainRatio > 0.001f) PositionIcon(painIcon, (1f - animHungerRatio - animWeightRatio - animPainRatio * 0.5f) * tw - hw + padding);
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
                bool isDebuff = e != energyFill && e != energyGhostFill;
                e.style.marginLeft = (isDebuff && leftRatio > 0.001f) ? 2f : 0f;
                // Important: Trigger repaint if width changed to update tiling UVs
                e.MarkDirtyRepaint();
            }
        }

        private void UpdateBarWidth(VisualElement e, float r)
        {
            if (e == null) return;
            if (r <= 0.001f) e.style.display = DisplayStyle.None;
            else { e.style.display = DisplayStyle.Flex; e.style.width = Length.Percent(r * 100f); }
        }

        private float SnapIfClose(float c, float t) => Mathf.Abs(c - t) < 0.002f ? t : c;
    }
}
