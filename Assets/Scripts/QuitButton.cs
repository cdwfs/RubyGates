using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class QuitButton : MonoBehaviour
{
    public static void OnClick()
    {
        if (Application.isEditor)
        {
            Debug.Log("In standalone builds, that would quit the game.");
#if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
#endif
        }
        else
        {
            Application.Quit();
        }
    }
}
