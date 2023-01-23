using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixPSDImporter : MonoBehaviour
{
    [UnityEditor.InitializeOnLoadMethod]
    public static void ResetPSDImporterFoldout()
    {
        UnityEditor.EditorPrefs.DeleteKey("PSDImporterEditor.m_PlatformSettingsFoldout");
    }
}
