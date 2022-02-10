using UnityEngine;

namespace Item
{
    [CreateAssetMenu(fileName = "ItemTypeHandler", menuName = "Items/ItemTypeHandler")]
    public class ItemTypeHandler : ScriptableObject
    {
        [SerializeField] public ItemType[] itemTypes;

        public static ItemTypeHandler instance { get; private set; }

        void OnEnable()
        {
            if (instance != null)
                Debug.LogError("can't have multiple instances of ItemTypeHandler");

            instance = this;
        }

        void OnDestroy() =>
            instance = null;
    }
}