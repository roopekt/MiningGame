using UnityEngine;

namespace Item
{
    public class ItemPickupper : MonoBehaviour
    {
        [SerializeField] Container[] destContainers;

        void OnTriggerEnter(Collider collider)
        {
            var droppedItem = collider.GetComponent<DroppedItem>();
            if (droppedItem && !droppedItem.isDestroyed)//if collider is dropped item
            {
                Slot slot = droppedItem.slot;
                foreach (var container in destContainers)
                {
                    slot.MoveAllTo(container);
                }
            }
        }
    }
}
