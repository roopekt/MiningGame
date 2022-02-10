using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class DebugLabel : MonoBehaviour
{
    void Awake()
    {
        Text text = GetComponent<Text>();
        text.enabled = false;

        #if DEBUG
        if (!Application.isEditor)
            text.enabled = true;
        #endif

        if (Debug.isDebugBuild && !Application.isEditor)
            text.enabled = true;
    }
}
