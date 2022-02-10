using System;

namespace Item
{
    //"contains" some number of items of some type
    public class Slot
    {
        public event Action OnChange;

        //itemTypeId
        private uint _itemTypeId;//index to ItemTypeHandler.instance.types
        public uint itemTypeId {
            get => _itemTypeId;
            //set {
            //    _itemTypeId = value;
            //    OnChange?.Invoke();
            //}
        }

        //amount
        private uint _amount;
        public uint amount {
            get => _amount;
            //set {
            //    _amount = value;
            //    OnChange?.Invoke();

            //    #if DEBUG
            //    if (amount > itemType.maxAmountPerSlot)
            //        UnityEngine.Debug.LogError("Item.Slot.amount.set: too big value");
            //    #endif
            //}
        }

        public ItemType itemType { get => ItemTypeHandler.instance.itemTypes[_itemTypeId]; }
        private uint freeSpace { get => itemType.maxAmountPerSlot - _amount; }

        public Slot(uint itemTypeId, uint amount)
        {
            _itemTypeId = itemTypeId;
            _amount = amount;
        }
        public Slot()//constructs an empty slot
            : this(0, 0) { }

        public void Set(uint itemTypeId, uint amount)
        {
            _itemTypeId = itemTypeId;
            _amount = amount;

            #if DEBUG
            if (_itemTypeId >= ItemTypeHandler.instance.itemTypes.Length)
                UnityEngine.Debug.LogError("Item.Slot.Set: invalid itemTypeId");
            if (amount > itemType.maxAmountPerSlot)
                UnityEngine.Debug.LogError("Item.Slot.Set: amount is too big");
            #endif

            OnChange?.Invoke();
        }

        //subtracts safely. returns amount subtracted
        public uint Subtract(uint amountToSubtract)
        {
            if (amountToSubtract <= _amount)
            {
                _amount -= amountToSubtract;
                OnChange?.Invoke();
                return amountToSubtract;
            }
            else
            {
                uint originalAmount = _amount;
                _amount = 0;
                OnChange?.Invoke();
                return originalAmount;
            }
        }

        //move up to amountToMove items to otherSlot.
        //returns number of items moved
        public uint MoveTo(Slot otherSlot, uint amountToMove)
        {
            if (otherSlot._amount == 0)
                otherSlot._itemTypeId = _itemTypeId;
            
            if (itemType == otherSlot.itemType)//to move items, types must match
            {
                //move items
                amountToMove = Math.Min(Math.Min(amountToMove, _amount), otherSlot.freeSpace);
                _amount -= amountToMove;
                otherSlot._amount += amountToMove;
            }
            else
            {
                amountToMove = 0;
            }

            //invoke OnChange events
            OnChange?.Invoke();
            otherSlot.OnChange?.Invoke();

            #if DEBUG
            if (amount > itemType.maxAmountPerSlot
                || otherSlot.amount > otherSlot.itemType.maxAmountPerSlot)
                UnityEngine.Debug.LogError("Item.Slot: amount is too big");
            #endif

            return amountToMove;
        }

        //move one item to otherSlot, if possible.
        //returns number of items moved (0 or 1)
        public uint MoveOneTo(Slot otherSlot) =>
            MoveTo(otherSlot, 1);

        //move as many items as possible to otherSlot.
        //returns number of items moved
        public uint MoveAllTo(Slot otherSlot) =>
            MoveTo(otherSlot, uint.MaxValue);

        //move as many items as possible to container
        //returns number of items moved
        public uint MoveAllTo(Container container)
        {
            uint originalAmount = _amount;
            for (uint x = 0; x < container.size.x; x++)
                for (uint y = 0; y < container.size.y; y++)
                {
                    MoveAllTo(container[x, y]);

                    if (_amount < 1)
                        return originalAmount;
                }
            return originalAmount - _amount;
        }

        //returns true, if types are same or at least one slot is empty
        public static bool TypesMatch(Slot A, Slot B)
        {
            return A._itemTypeId == B._itemTypeId || A._amount == 0 || B._amount == 0;
        }

        //swap amount and itemTypeId
        public static void SwapContents(Slot A, Slot B)
        {
            uint A_type = A._itemTypeId;
            uint A_amount = A._amount;

            A._itemTypeId = B._itemTypeId;
            A._amount = B._amount;

            B._itemTypeId = A_type;
            B._amount = A_amount;

            //invoke OnChange events
            A.OnChange?.Invoke();
            B.OnChange?.Invoke();
        }
    }
}