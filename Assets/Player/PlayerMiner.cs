using UnityEngine;

public class PlayerMiner : MonoBehaviour
{
    [SerializeField] float SphereRadius = .5f;
    [SerializeField] float Strength = 1f;
    [SerializeField] float Reach = 5f;
    [SerializeField] LayerMask RayLayerMask;
    [SerializeField] ChunkLoader ChunkLoader;
    [SerializeField] Item.PlayerInventory PlayerInvetory;
    [Tooltip("Mining loot will be dropped every this many seconds.")]
    [SerializeField] float LootDropDeltaTime = 0.2f;

    MiningLoot miningLoot;
    float lootDropTimer;
    uint latestTypeTaken = uint.MaxValue;//see placing
    float extraAmountTaken = 0f;//see placing
    Vector3 latestRayHitPos;

    void Awake()
    {
        miningLoot = new MiningLoot();
        lootDropTimer = LootDropDeltaTime;
    }

    void DropMiningLoot()
    {
        //find a location to drop items
        float droppedItemRadius = GameManager.DroppedItemPrefab_DroppedItem.physicsRadius;
        RaycastHit hitInfo;
        Physics.SphereCast(transform.position, droppedItemRadius, transform.forward, out hitInfo, Reach, RayLayerMask);
        Vector3 dropPos = (hitInfo.distance) * transform.forward + transform.position;

        //drop items
        float groundItemDensity = GameManager.instance.GroundItemDensity;
        foreach (var pair in miningLoot)
        {
            uint itemTypeId = pair.Key;
            uint amount = (uint)Mathf.CeilToInt(pair.Value * groundItemDensity);
            Item.DroppedItem[] temp = Item.DroppedItem.Drop(dropPos, itemTypeId, amount);
        }

        //reset MiningLoot
        miningLoot.Clear();
    }
    void Update()//SHOULD BE FIXEDUPDATE
    {
        lootDropTimer -= Time.fixedDeltaTime;
        if (lootDropTimer < 0f)
        {
            DropMiningLoot();
            lootDropTimer = LootDropDeltaTime;
        }

        //test
        #if false
        if (Time.time > .5f)
        {
            Vector3 pos = Vector3.one * 2f;

            miningLoot.Clear();
            float totalDelta = ChunkLoader.PlaceSphere(pos, SphereRadius, .3f, 0);

            //figure out how much to take from hand
            Item.Slot handSlot = new Item.Slot(0, 1000);
            extraAmountTaken = 0f;//reset
            float amountPlaced = totalDelta * GameManager.instance.GroundItemDensity;
            int amountTaken = Mathf.CeilToInt(amountPlaced - extraAmountTaken);
            extraAmountTaken = (float)amountTaken - amountPlaced;
            latestTypeTaken = handSlot.itemTypeId;
            handSlot.Subtract((uint)amountTaken);

            ChunkLoader.MineSphere(pos, SphereRadius, -.3f, out miningLoot);
            float minedAmount = miningLoot[0];
            DropMiningLoot();

            Debug.Log("test executed");
            Debug.Break();//to prevent freezing
        }
        else
        {
            Debug.Log("waiting");
        }
        #endif

        //mining and placing
        {
            if (GameManager.inventoryModeActive)
                return;

            float delta = 0f;
            if (Input.GetMouseButtonDown(0))
                delta -= Strength * Time.fixedDeltaTime;
            if (Input.GetMouseButtonDown(1))
                delta += Strength * Time.fixedDeltaTime;

            //temp
            delta /= Time.fixedDeltaTime;

            //if no mining or placing
            if (Mathf.Approximately(delta, 0f))
                return;

            RaycastHit hitInfo;
            if (Physics.Raycast(transform.position, transform.forward, out hitInfo, Reach, RayLayerMask))
            {
                Vector3 hitPos = hitInfo.point;
                latestRayHitPos = hitPos;

                if (delta > 0f)//if placing
                {
                    Item.Slot handSlot = PlayerInvetory.handSlot;
                    if (handSlot.amount > 0)
                    {
                        //place a sphere
                        float totalDelta = ChunkLoader.PlaceSphere(hitPos, SphereRadius, delta, handSlot.itemTypeId);

                        //figure out how much to take from hand
                        if (latestTypeTaken != handSlot.itemTypeId)
                            extraAmountTaken = 0f;//reset
                        float amountPlaced = totalDelta * GameManager.instance.GroundItemDensity;
                        int amountTaken = Mathf.CeilToInt(amountPlaced - extraAmountTaken);
                        extraAmountTaken = (float)amountTaken - amountPlaced;
                        latestTypeTaken = handSlot.itemTypeId;

                        handSlot.Subtract((uint)amountTaken);
                    }
                }
                else//if mining
                {
                    ChunkLoader.MineSphere(hitPos, SphereRadius, delta, out miningLoot);
                }
            }
        }
    }
}