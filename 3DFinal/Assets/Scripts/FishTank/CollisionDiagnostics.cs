using UnityEngine;

/// <summary>
/// 碰撞診斷工具 - 檢查手部和魚的碰撞設置
/// 將此腳本掛載到場景中任意 GameObject 上即可自動診斷
/// </summary>
public class CollisionDiagnostics : MonoBehaviour
{
    [Header("診斷設定")]
    [SerializeField] private bool runDiagnosticsOnStart = true;
    [SerializeField] private bool showDetailedInfo = true;

    private void Start()
    {
        if (runDiagnosticsOnStart)
        {
            DiagnoseCollisionSetup();
        }
    }

    [ContextMenu("執行碰撞診斷")]
    public void DiagnoseCollisionSetup()
    {
        Debug.Log("═══════════════════════════════════════");
        Debug.Log("🔍 開始碰撞診斷...");
        Debug.Log("═══════════════════════════════════════");

        // 1. 檢查所有魚
        Fish[] allFish = FindObjectsOfType<Fish>();
        Debug.Log($"\n📊 找到 {allFish.Length} 條魚");
        
        int fishWithTrigger = 0;
        int fishWithoutCollider = 0;
        
        foreach (Fish fish in allFish)
        {
            Collider col = fish.GetComponent<Collider>();
            if (col == null)
            {
                fishWithoutCollider++;
                Debug.LogError($"❌ {fish.name} 沒有 Collider 組件！");
            }
            else if (!col.isTrigger)
            {
                Debug.LogWarning($"⚠️ {fish.name} 的 Collider 沒有勾選 Is Trigger");
            }
            else
            {
                fishWithTrigger++;
                if (showDetailedInfo)
                    Debug.Log($"✅ {fish.name} Collider 設置正確（Is Trigger = true）");
            }
        }

        Debug.Log($"\n📈 統計：{fishWithTrigger}/{allFish.Length} 條魚有正確的 Trigger Collider");

        // 2. 檢查所有標記為 "Hand" 的物件
        GameObject[] handObjects = GameObject.FindGameObjectsWithTag("Hand");
        Debug.Log($"\n📊 找到 {handObjects.Length} 個標記為 'Hand' 的物件");

        int handWithCollider = 0;
        int handWithTrigger = 0;
        int handWithRigidbody = 0;

        foreach (GameObject handObj in handObjects)
        {
            Collider col = handObj.GetComponent<Collider>();
            Rigidbody rb = handObj.GetComponent<Rigidbody>();

            if (col == null)
            {
                Debug.LogWarning($"⚠️ {handObj.name} 標記為 'Hand' 但沒有 Collider！");
            }
            else
            {
                handWithCollider++;
                if (col.isTrigger)
                {
                    handWithTrigger++;
                    if (showDetailedInfo)
                        Debug.Log($"✅ {handObj.name} 有 Trigger Collider");
                }
                else
                {
                    if (showDetailedInfo)
                        Debug.Log($"ℹ️ {handObj.name} 有 Collider（非 Trigger）");
                }
            }

            if (rb != null)
            {
                handWithRigidbody++;
                if (showDetailedInfo)
                    Debug.Log($"ℹ️ {handObj.name} 有 Rigidbody（Is Kinematic: {rb.isKinematic}）");
            }
        }

        Debug.Log($"\n📈 Hand 物件統計：");
        Debug.Log($"   - 有 Collider: {handWithCollider}/{handObjects.Length}");
        Debug.Log($"   - 是 Trigger: {handWithTrigger}/{handObjects.Length}");
        Debug.Log($"   - 有 Rigidbody: {handWithRigidbody}/{handObjects.Length}");

        // 3. 檢查未標記的 HandZone/HandActiveZone
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int unmarkedHandZones = 0;
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Hand") && (obj.name.Contains("Zone") || obj.name.Contains("Active")))
            {
                if (obj.tag != "Hand")
                {
                    unmarkedHandZones++;
                    Collider col = obj.GetComponent<Collider>();
                    if (col != null)
                    {
                        Debug.LogWarning($"⚠️ 發現未標記的 Hand 區域：{obj.name} (Tag: {obj.tag}, 有 Collider:{col.isTrigger} ? 'Trigger' : '非Trigger')");
                        Debug.LogWarning($"   💡 建議：將 {obj.name} 的 Tag 設置為 'Hand'");
                    }
                }
            }
        }

        if (unmarkedHandZones > 0)
        {
            Debug.LogWarning($"\n⚠️ 發現 {unmarkedHandZones} 個未標記為 'Hand' 的手部區域物件");
        }

        // 4. 檢查 GameManager
        if (GameManager_fish.Instance == null)
        {
            Debug.LogError("\n❌ 場景中沒有 GameManager_fish 實例！");
            Debug.LogError("   💡 請在場景中創建一個 GameObject 並添加 GameManager_fish 腳本");
        }
        else
        {
            Debug.Log($"\n✅ GameManager_fish 存在：{GameManager_fish.Instance.gameObject.name}");
        }

        // 5. 物理設置建議
        Debug.Log("\n═══════════════════════════════════════");
        Debug.Log("💡 碰撞設置建議：");
        Debug.Log("═══════════════════════════════════════");
        Debug.Log("1. 魚的 Collider 必須勾選 'Is Trigger'");
        Debug.Log("2. 手部物件的 Tag 必須設置為 'Hand'");
        Debug.Log("3. 手部物件需要有 Collider（Trigger 或非 Trigger 都可以）");
        Debug.Log("4. 至少一方需要有 Rigidbody（建議手部有，且設置為 Is Kinematic）");
        Debug.Log("5. 確保手部 Collider 會跟隨手部移動（檢查 HandCollisionDetector 設置）");
        Debug.Log("═══════════════════════════════════════");
    }

    private void OnDrawGizmos()
    {
        // 繪製所有標記為 Hand 的物件位置
        GameObject[] handObjects = GameObject.FindGameObjectsWithTag("Hand");
        Gizmos.color = Color.green;
        foreach (GameObject handObj in handObjects)
        {
            Collider col = handObj.GetComponent<Collider>();
            if (col != null)
            {
                if (col is BoxCollider boxCol)
                {
                    Gizmos.DrawWireCube(handObj.transform.position + boxCol.center, boxCol.size);
                }
                else if (col is SphereCollider sphereCol)
                {
                    Gizmos.DrawWireSphere(handObj.transform.position + sphereCol.center, sphereCol.radius);
                }
            }
        }
    }
}

