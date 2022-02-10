using UnityEngine;

namespace Item
{
    public class PlayerInventory : MonoBehaviour
    {
        [SerializeField] float PickupCooldownOnDrop = 1.5f;
        [SerializeField] ContainerGUI Hotbar;
        [SerializeField] ContainerGUI Backpack;
        [SerializeField] SlotGUI CursorSlot;
        [SerializeField] GameObject CenterIcon;
        [SerializeField] Transform PlayerCamera;

        public Slot handSlot { get => Hotbar.Container[handPos]; }

        //inventory mode active? (backpack visible, cursor free, etc.)
        bool invModeActive = false;

        uint handPos {
            get => Hotbar.highlightIndex;
            set => Hotbar.highlightIndex = value;
        }

        void Start()
        {
            SetInvModeActive(GameManager.inventoryModeActive);
        }

        void Update()
        {
            //toggle inventory mode
            if (Input.GetKeyDown(KeyCode.E))
                SetInvModeActive(!invModeActive);

            HandleDropping();

            UpdateHandPos();
        }

        //activate or deactivate inventory mode
        void SetInvModeActive(bool active)
        {
            invModeActive = active;

            GameManager.SetInvModeActive(invModeActive);
            Backpack.gameObject.SetActive(invModeActive);
            CursorSlot.gameObject.SetActive(invModeActive);
            CenterIcon.SetActive(!invModeActive);
            Hotbar.enableHighlight = !invModeActive;
            Hotbar.gameObject.SetActive(true);
        }

        void UpdateHandPos()
        {
            int delta = -Mathf.RoundToInt(Input.mouseScrollDelta.y);
            handPos = (uint)Utility.Mod((int)handPos + delta, Hotbar.containerSize.x);
        }

        void HandleDropping()
        {
            if (Input.GetKeyDown(KeyCode.Q) && !GameManager.inventoryModeActive)
            {
                Slot hand = Hotbar.highlightedSlot;
                Slot droppedSlot = DroppedItem.Drop(PlayerCamera.position, PickupCooldownOnDrop).slot;

                //if control pressed (Ctrl + Q)
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                {
                    hand.MoveAllTo(droppedSlot);
                }
                else
                {
                    hand.MoveOneTo(droppedSlot);
                }
            }
        }
    }
}
