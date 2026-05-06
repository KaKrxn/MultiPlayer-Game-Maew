using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LootTable_", menuName = "Loot/Loot Table")]
public class LootTableData : ScriptableObject
{
    public string tableName;
    public List<LootItemData> possibleLoot = new List<LootItemData>();
}
