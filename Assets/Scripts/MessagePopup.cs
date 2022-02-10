using UnityEngine;

public class MessagePopup : MonoBehaviour
{
    [SerializeField] private bool InEditor = false;

    void OnEnable()
    {
        Application.logMessageReceived += Log;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= Log;
    }

    public void Log(string logString, string callStack, LogType type)
    {
        if (Application.isEditor && !InEditor)
            return;


    }
}
