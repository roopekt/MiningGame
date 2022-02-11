using UnityEngine;

namespace Item
{
    [CreateAssetMenu(fileName = "ItemTypeHandler", menuName = "Items/ItemTypeHandler")]
    public class ItemTypeHandler : ScriptableObject
    {
        [SerializeField] public ItemType[] itemTypes;

        public static ItemTypeHandler instance { get => GetInstance(); }

        private static ItemTypeHandler _instance;

        private static ItemTypeHandler GetInstance()
        {
            if (_instance == null)
                _instance = LoadInstanceFromResources();

            return _instance;
        }

        private static ItemTypeHandler LoadInstanceFromResources()
        {
            ItemTypeHandler instance = Resources.Load<ItemTypeHandler>(nameof(ItemTypeHandler));

            if (instance == null)
                throw new System.Exception("Could not find a ItemTypeHandler");

            return instance;
        }
    }
}