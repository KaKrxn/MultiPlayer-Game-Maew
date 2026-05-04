using Unity.Netcode;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public void OnClickDie()
    {
        Debug.Log("👉 [UI] 1. ปุ่ม Die ใน Canvas ถูกคลิกแล้ว!");

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            if (NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                if (NetworkManager.Singleton.LocalClient.PlayerObject.TryGetComponent(out PlayerAction player))
                {
                    Debug.Log("👉 [UI] 2. หาตัวละครของผู้เล่นเจอแล้ว! กำลังส่งคำสั่งไปที่ PlayerAction...");
                    player.LocalDie();
                }
                else
                {
                    Debug.LogError("❌ [UI] หาตัวละครเจอ แต่ไม่มีสคริปต์ PlayerAction ติดอยู่!");
                }
            }
            else
            {
                Debug.LogError("❌ [UI] ไม่มี PlayerObject (ตัวละครยังไม่ได้ Spawn)!");
            }
        }
        else
        {
            Debug.LogError("❌ [UI] ยังไม่ได้ Connect เข้า Server/Host เลยกดไม่ได้!");
        }
    }

    public void OnClickRevive()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            if (NetworkManager.Singleton.LocalClient.PlayerObject.TryGetComponent(out PlayerAction player))
            {
                player.LocalRevive();
            }
        }
    }
}