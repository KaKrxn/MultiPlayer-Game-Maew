using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class PlayerSpawnManager : NetworkBehaviour
{
    [Header("Player Settings")]
    [Tooltip("ลาก Player Prefab มาใส่ที่นี่")]
    public GameObject playerPrefab;

    [Header("Spawn Points")]
    [Tooltip("จุดเกิดตอนเริ่มเกม ถ้ามีจุดเดียว ระบบจะขยับตัวละครให้ยืนเรียงกันอัตโนมัติ")]
    public Transform[] startSpawnPoints;
    
    [Tooltip("จุดเกิดบนรถไฟ (ถ้าไม่ใส่ ระบบจะพยายามหารถไฟที่กำลังวิ่งอยู่ให้เอง)")]
    public Transform trainSpawnPoint;
    
    private int m_SpawnIndex = 0;
    private HashSet<ulong> m_LobbyClients = new HashSet<ulong>();
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("[PlayerSpawnManager] Player Prefab is not assigned! Please assign it in the inspector.");
                return;
            }

            FindStartSpawnPoints();

            // Store clients that transitioned with the host
            foreach (ulong id in NetworkManager.Singleton.ConnectedClientsIds)
            {
                m_LobbyClients.Add(id);
            }

            // Subscribe to scene load complete events so we only spawn players when they are ready
            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnClientSceneLoaded;
            
            // Spawn the Host immediately because the Host has already loaded the scene
            SpawnPlayerAtStartPoint(NetworkManager.ServerClientId, m_SpawnIndex++);
        }
    }
    
    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnClientSceneLoaded;
        }
    }
    
    private void OnClientSceneLoaded(ulong clientId, string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode)
    {
        if (IsServer && sceneName == gameObject.scene.name && clientId != NetworkManager.ServerClientId)
        {
            // The client has fully loaded the Game scene. Now we can safely spawn their player object!
            if (m_LobbyClients.Contains(clientId))
            {
                // This client came from the lobby, spawn at start points
                SpawnPlayerAtStartPoint(clientId, m_SpawnIndex++);
            }
            else
            {
                // This client is a late joiner, spawn on the train
                SpawnPlayerAtTrain(clientId);
            }
        }
    }

    private void FindStartSpawnPoints()
    {
        if (startSpawnPoints == null || startSpawnPoints.Length == 0)
        {
            GameObject[] points = GameObject.FindGameObjectsWithTag("Respawn");
            if (points.Length == 0) 
            {
                GameObject singlePoint = GameObject.Find("StartSpawnPoint");
                if (singlePoint != null) points = new GameObject[] { singlePoint };
            }
            
            if (points.Length > 0)
            {
                startSpawnPoints = new Transform[points.Length];
                for (int i = 0; i < points.Length; i++) startSpawnPoints[i] = points[i].transform;
                Debug.Log($"[PlayerSpawnManager] 🔍 Found {points.Length} start spawn points dynamically.");
            }
        }
    }

    private void SpawnPlayerAtStartPoint(ulong clientId, int index)
    {
        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        if (startSpawnPoints != null && startSpawnPoints.Length > 0)
        {
            Transform point = startSpawnPoints[index % startSpawnPoints.Length];
            if (point != null)
            {
                // หากจำนวนคนเยอะกว่าจุดเกิด (หรือมีจุดเกิดจุดเดียว)
                // เราจะเพิ่ม Offset ให้ยืนกระจายกันไปทางขวา คนละ 1.5 หน่วย จะได้ไม่ทับกัน
                int loop = index / startSpawnPoints.Length;
                Vector3 offset = Vector3.zero;
                
                if (loop > 0 || startSpawnPoints.Length == 1)
                {
                    // ขยับไปทางขวาของจุดเกิด (point.right) 
                    offset = point.right * (index * 1.5f);
                }

                spawnPos = point.position + offset;
                spawnRot = point.rotation;
            }
        }

        GameObject spawnedPlayer = Instantiate(playerPrefab, spawnPos, spawnRot);
        NetworkObject netObj = spawnedPlayer.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.SpawnAsPlayerObject(clientId, true);
        }
    }

    private void SpawnPlayerAtTrain(ulong clientId)
    {
        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        if (trainSpawnPoint != null)
        {
            // ถ้าลากจุดเกิดบนรถไฟไว้ใน Inspector ก็ใช้อันนั้น
            spawnPos = trainSpawnPoint.position;
            spawnRot = trainSpawnPoint.rotation;
        }
        else
        {
            // ถ้าระบบหาไม่เจอ ให้พยายามหารถไฟที่กำลังวิ่งอยู่ (TrainFuelSystem)
            var activeTrain = FindObjectOfType<TrainFuelSystem>();
            if (activeTrain != null)
            {
                // ให้เกิดกลางรถไฟ แต่อยู่สูงขึ้นมา 2 หน่วย จะได้ไม่ทะลุพื้น
                spawnPos = activeTrain.transform.position + Vector3.up * 2f;
                spawnRot = activeTrain.transform.rotation;
                Debug.Log("[PlayerSpawnManager] 🚂 พารถไฟเจอแล้ว! ให้ Late Joiner เกิดบนรถไฟ");
            }
            else
            {
                // ถ้าไม่มีอะไรเลย ก็หา GameObject ชื่อ TrainSpawnPoint
                GameObject dynamicSpawnPoint = GameObject.Find("TrainSpawnPoint");
                if (dynamicSpawnPoint != null)
                {
                    spawnPos = dynamicSpawnPoint.transform.position;
                    spawnRot = dynamicSpawnPoint.transform.rotation;
                }
                else
                {
                    Debug.LogWarning("[PlayerSpawnManager] TrainSpawnPoint not found! Spawning late-joining player at 0,0,0.");
                }
            }
        }

        GameObject spawnedPlayer = Instantiate(playerPrefab, spawnPos, spawnRot);
        NetworkObject netObj = spawnedPlayer.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.SpawnAsPlayerObject(clientId, true);
        }
    }
}
