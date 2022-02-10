using System.Collections.Generic;

//when mining, loot is first put to this, and then instantiated as DroppedItems
public class MiningLoot : Dictionary<uint, float>
{
    public void AddTo(uint itemTypeId, float amountToAdd)
    {
        if (ContainsKey(itemTypeId))
        {
            this[itemTypeId] += amountToAdd;
        }
        else
        {
            Add(itemTypeId, amountToAdd);
        }
    }
}