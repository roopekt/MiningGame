using UnityEngine;

namespace Item
{
    //descripes an item type
    [System.Serializable]
    public class ItemType
    {
        public string name = "";
        public uint maxAmountPerSlot = 10;//max amount per one ItemSlot
        public Sprite sprite;//sprite for UI
        public Color color = Color.white;
    }
}