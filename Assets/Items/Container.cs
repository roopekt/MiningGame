
using UnityEngine;

namespace Item
{
    //a collection of Slots in grid formation
    public class Container : MonoBehaviour
    {
        [SerializeField] Vector2Int Size;

        private Slot[,] slots;

        #region old constructors
        //construct empty grid
        //public Container(uint width, uint height)
        //{
        //    //allocate slots array
        //    slots = new Slot[width, height];

        //    //fill with empty slots
        //    for (uint x = 0; x < width; x++)
        //        for (uint y = 0; y < height; y++)
        //        {
        //            slots[x, y] = new Slot();
        //        }
        //}

        ////construct empty grid
        //public Container(UnityEngine.Vector2Int size)
        //    : this((uint)size.x, (uint)size.y) { }

        ////construct empty row
        //public Container(uint size)
        //    : this(size, 1) { }
        #endregion

        public Vector2Int size { get => Size; }

        void Awake()
        {
            //allocate slots array
            slots = new Slot[Size.x, Size.y];

            //fill with empty slots
            for (uint x = 0; x < Size.x; x++)
                for (uint y = 0; y < Size.y; y++)
                {
                    slots[x, y] = new Slot();
                }
        }

        //indexer. allows for Container[x, y] syntax
        public Slot this[uint x, uint y] {
            get => slots[x, y];
            //set => slots[x, y] = value;
        }

        //indexer. allows for Container[i] syntax
        public Slot this[uint i]
        {
            get => slots[i, 0];
            //set => slots[i, 0] = value;
        }
    }
}
