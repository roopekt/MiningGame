using UnityEngine;

namespace Item
{
    public class DroppedItem : SlotGUI
    {
        [SerializeField] float RotationSpeed = 180f;
        [SerializeField] SphereCollider TriggerCollider;
        [SerializeField] float TriggerRadius = 0.5f;
        [SerializeField] SphereCollider PhysicsCollider;
        [SerializeField] float PhysicsRadius = 0.25f;

        public float triggerRadius { get => TriggerRadius; }
        public float physicsRadius { get => PhysicsRadius; }

        public bool isDestroyed { get; private set; } = false;

        //timer to when this can be picked up again. Instantiate might set this
        float pickupCooldown = 0f;

        protected override void Awake()
        {
            base.Awake();

            //set the event camera of the canvas
            Canvas canvas = GetComponent<Canvas>();
            if (canvas)
                canvas.worldCamera = Camera.main;

            //set radiuses of the colliders
            TriggerCollider.radius = TriggerRadius;
            PhysicsCollider.radius = PhysicsRadius;

            //set random rotation
            transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }

        void Update()
        {
            //rotate
            Quaternion rotation = transform.rotation;
            rotation *= Quaternion.Euler(Vector3.up * RotationSpeed * Time.deltaTime);
            transform.rotation = rotation;

            //destroy if empty
            if (slot.amount == 0)
            {
                Destroy(gameObject);
                isDestroyed = true;
            }

            //update pickup cooldown
            if (pickupCooldown < 0f) {
                TriggerCollider.enabled = true;
            }
            else {
                pickupCooldown -= Time.deltaTime;
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (isDestroyed)
            {
                Debug.Log("TriggerEnter when destroyed");
                return;
            }

            DroppedItem otherDroppedItem = other.GetComponent<DroppedItem>();
            if (otherDroppedItem//if other is DroppedItem
                && otherDroppedItem.GetInstanceID() < this.GetInstanceID()//this way only one of two DroppedItems can pick the other up
                && !otherDroppedItem.isDestroyed)//the other must not have been destroyed
            {
                //try to take items from otherDroppedItem
                otherDroppedItem.slot.MoveAllTo(this.slot);
            }
        }

        public static DroppedItem Drop(Vector3 position, float pickupCooldown = -1f)
        {
             var gameObject = Object.Instantiate(GameManager.instance.DroppedItemPrefab,
                 position, Quaternion.identity,
                 GameManager.instance.DroppedItemsParent.transform);
            var droppedItem = gameObject.GetComponent<DroppedItem>();

            //pickup cooldown
            droppedItem.pickupCooldown = pickupCooldown;
            if (pickupCooldown > 0f)
                droppedItem.TriggerCollider.enabled = false;

            return droppedItem;
        }

        public static DroppedItem[] Drop(Vector3 position, uint itemTypeId, uint amount, float pickupCooldown = -1f)
        {
            int maxAmountPerSlot = (int)ItemTypeHandler.instance.itemTypes[itemTypeId].maxAmountPerSlot;
            int amountToDrop = (int)amount;
            DroppedItem[] droppedItems = new DroppedItem[Utility.DivIntCeil(amountToDrop, maxAmountPerSlot)];
            for (uint i = 0; amountToDrop > 0; ++i)
            {
                int slotAmount = System.Math.Min(amountToDrop, maxAmountPerSlot);
                amountToDrop -= slotAmount;

                droppedItems[i] = Drop(position, pickupCooldown);
                droppedItems[i].slot.Set(itemTypeId, (uint)slotAmount);
            }
            return droppedItems;
        }
    }
}