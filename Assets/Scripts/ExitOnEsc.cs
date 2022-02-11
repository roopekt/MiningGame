using UnityEngine;

public class ExitOnEsc : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKey(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Escape))
            Quit();
    }

    private static void Quit()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
