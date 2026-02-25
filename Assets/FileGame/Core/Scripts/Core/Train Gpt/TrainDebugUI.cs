using Unity.Netcode;
using UnityEngine;

namespace Game.Train
{
    public class TrainDebugUI : NetworkBehaviour
    {
        [SerializeField] private TrainState train;

        // NEW: toggle cursor by Right Ctrl
        [Header("Cursor Toggle")]
        [SerializeField] private bool cursorVisibleOnStart = false;
        private bool cursorVisible;

        private void Awake()
        {
            if (!train) train = GetComponent<TrainState>();
        }

        private void Start()
        {
            cursorVisible = cursorVisibleOnStart;
            ApplyCursorState(cursorVisible);
        }

        private void Update()
        {
            // Right Ctrl toggles cursor visibility & lock state
            if (Input.GetKeyDown(KeyCode.RightControl))
            {
                cursorVisible = !cursorVisible;
                ApplyCursorState(cursorVisible);
            }
        }

        private void ApplyCursorState(bool show)
        {
            Cursor.visible = show;
            Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
        }

        private void OnGUI()
        {
            if (!train) return;

            GUILayout.BeginArea(new Rect(10, 10, 340, 260), GUI.skin.box);

            GUILayout.Label($"Network: {(NetworkManager.Singleton != null ? (NetworkManager.Singleton.IsServer ? "Server/Host" : "Client") : "No NM")}");
            GUILayout.Label($"Cursor: {(cursorVisible ? "Visible/Unlocked" : "Hidden/Locked")}  (Right Ctrl toggle)");
            GUILayout.Space(6);

            GUILayout.Label($"IsMoving: {train.IsMoving.Value}");
            GUILayout.Label($"Speed: {train.Speed.Value:0.00}");
            GUILayout.Label($"Fuel: {train.Fuel.Value:0.0}/{train.FuelCapacity:0.0}");
            GUILayout.Label($"HP: {train.Hp.Value:0.0}/{train.MaxHp:0.0}");

            GUILayout.Space(10);

            // Test buttons: only for server/host
            GUI.enabled = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

            if (GUILayout.Button("Toggle Move (Server)"))
                train.IsMoving.Value = !train.IsMoving.Value;

            if (GUILayout.Button("Set Speed = 5 (Server)"))
                train.Speed.Value = 5f;

            if (GUILayout.Button("Fuel -10 (Server)"))
                train.Fuel.Value = Mathf.Max(0f, train.Fuel.Value - 10f);

            if (GUILayout.Button("HP -25 (Server)"))
                train.Hp.Value = Mathf.Max(0f, train.Hp.Value - 25f);

            GUI.enabled = true;

            GUILayout.EndArea();
        }
    }
}