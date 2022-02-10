using UnityEngine;
using UnityEditor;

namespace Item
{
    [RequireComponent(typeof(RectTransform))]
    public class ContainerGUI : MonoBehaviour
    {
        [SerializeField] public Container Container;
        [SerializeField] float SlotWidth = 20f;
        [SerializeField] GameObject SlotGUIPrefab;
        [SerializeField] SlotGUI CursorSlot;
        [Header("Highlight")]
        [SerializeField] bool EnableHighlightDefault = false;
        [SerializeField] float HighlightStrength = 20f;

        RectTransform[,] slotsAsRectTransforms;

        public Vector2Int containerSize { get => Container.size; }

        bool _enableHighlight;
        public bool enableHighlight
        {
            get => _enableHighlight;
            set
            {
                _enableHighlight = value;

                //update highlight
                highlightIndex = highlightIndex;
            }
        }

        uint _highlightIndex = 0;
        public uint highlightIndex
        {
            get => _highlightIndex;
            set
            {
                //unhighlight old slot
                RectTransform oldSlot = slotsAsRectTransforms[_highlightIndex, 0];
                oldSlot.offsetMin = oldSlot.offsetMax = Vector2.zero;

                _highlightIndex = value;
                
                if (_enableHighlight)
                {
                    //highlight new slot
                    RectTransform newSlot = slotsAsRectTransforms[_highlightIndex, 0];
                    newSlot.offsetMin = newSlot.offsetMax = new Vector2(0f, HighlightStrength);
                }
            }
        }

        public Slot highlightedSlot { get => Container[_highlightIndex]; }

        void Start()
        {
            slotsAsRectTransforms = new RectTransform[Container.size.x, Container.size.y];

            //instantiate SlotGUI objects
            for (uint x = 0; x < Container.size.x; x++)
                for (uint y = 0; y < Container.size.y; y++)
                {
                    //instantiate
                    GameObject slot = Instantiate(SlotGUIPrefab, transform);
                    RectTransform rectTransform = slot.GetComponent<RectTransform>();
                    slotsAsRectTransforms[x, y] = rectTransform;

                    //place
                    Vector2 scale = new Vector2(1f / Container.size.x, 1f / Container.size.y);
                    Vector2 xy = new Vector2(x, y);
                    rectTransform.anchorMin = Vector2.Scale(xy, scale);
                    rectTransform.anchorMax = Vector2.Scale(xy + Vector2.one, scale);
                    rectTransform.offsetMin = rectTransform.offsetMax = Vector2.zero;

                    //set references
                    ContainerSlotGUI slotGUI = slot.GetComponent<ContainerSlotGUI>();
                    slotGUI.CursorSlot = CursorSlot;
                    slotGUI.slot = Container[x, y];
                }

            enableHighlight = EnableHighlightDefault;

            //temp
            Container[0, 0].Set(0, 5000);
        }

        private void OnValidate() =>
            Invoke("Resize", 0f);

        void Resize()//update width and height of RectTransform
        {
            Vector2 newSize = (Vector2)Container.size * SlotWidth;

            RectTransform rectTransform = GetComponent<RectTransform>();
            rectTransform.sizeDelta = newSize;//width and height
        }
    }
}