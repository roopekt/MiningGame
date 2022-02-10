using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Item
{
    //holds a Slot, and handles it's gui
    public class ContainerSlotGUI : SlotGUI, IPointerClickHandler
    {
        [SerializeField] public SlotGUI CursorSlot;

        public void OnPointerClick(PointerEventData eventData)
        {
            switch (eventData.button)
            {
                case PointerEventData.InputButton.Left:
                    LeftClick();
                    break;
                case PointerEventData.InputButton.Right:
                    RightClick();
                    break;
            }
        }

        void LeftClick()
        {
            Slot cursorSlot = CursorSlot.slot;
            if (cursorSlot.amount > 0)
            {
                if (Slot.TypesMatch(slot, cursorSlot))
                    cursorSlot.MoveAllTo(slot);
                else
                    Slot.SwapContents(slot, cursorSlot);
            }
            else
            {
                slot.MoveAllTo(cursorSlot);
            }
        }

        void RightClick()
        {
            Slot cursorSlot = CursorSlot.slot;
            if (cursorSlot.amount > 0)
            {
                cursorSlot.MoveOneTo(slot);
            }
            else
            {
                slot.MoveTo(cursorSlot, slot.amount / 2);//move half to cursor
            }
        }
    }
}