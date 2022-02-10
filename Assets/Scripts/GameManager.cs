using UnityEngine;

//Singleton handling global scene things
public class GameManager : MonoBehaviour
{
    [SerializeField] public GameObject DroppedItemPrefab;
    [SerializeField] public GameObject DroppedItemsParent;
    [Tooltip("Scales the amount of items that is dropped when ground is mined.")]
    [SerializeField] public float GroundItemDensity = 5f;

    public static GameManager instance { get; private set; }

    public static bool inventoryModeActive { get; private set; } = false;
    public static Item.DroppedItem DroppedItemPrefab_DroppedItem;

    //activate or deactivate inventory mode (backpack visible, cursor free, etc.)
    public static void SetInvModeActive(bool active)
    {
        inventoryModeActive = active;
        Cursor.lockState = active ? CursorLockMode.None : CursorLockMode.Locked;
    }

    void OnEnable()
    {
        if (instance != null)
            Debug.LogError("can't have multiple instances of GameManager");

        instance = this;

        DroppedItemPrefab_DroppedItem = DroppedItemPrefab.GetComponent<Item.DroppedItem>();
    }

    void OnDisable() =>
        instance = null;

    void Update()
    {
        //quit application if esc pressed
        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
}