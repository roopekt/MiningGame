using UnityEngine;
using UnityEngine.UI;

namespace Item
{
    //holds a Slot, and handles it's gui
    public class SlotGUI : MonoBehaviour
    {
        [SerializeField] Image ItemImage;
        [SerializeField] Text AmountText;

        private Slot _slot;
        public Slot slot
        {
            get => _slot;
            set
            {
                ReplaceSlot(value);
                UpdateGUI();
            }
        }

        void ReplaceSlot(Slot newSlot)
        {
            //unsub to old Slot's events
            if (_slot != null)
                _slot.OnChange -= UpdateGUI;

            //sub to new Slot's events
            if (newSlot != null)
                newSlot.OnChange += UpdateGUI;

            //write new value
            _slot = newSlot;
        }

        protected virtual void Awake()
        {
            if (_slot == null)
            {
                ReplaceSlot(new Slot());
                ItemImage.enabled = false;
                AmountText.enabled = false;
            }
        }

        void UpdateGUI()
        {
            ItemType itemType = _slot.itemType;
            uint amount = _slot.amount;

            //update sprite
            if (amount > 0)
            {
                ItemImage.enabled = true;
                ItemImage.sprite = itemType.sprite;
                ItemImage.color = itemType.color;
            }
            else
            {
                ItemImage.enabled = false;
            }

            //update amount text
            AmountText.enabled = amount > 1;
            AmountText.text = amount.ToString();
        }
    }
}