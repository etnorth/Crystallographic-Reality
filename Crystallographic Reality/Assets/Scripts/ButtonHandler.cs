using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor; // For File Dialog
using UnityEngine.UI; // For getting Text Component
using System.IO; // For GetFileName (aka. removing path)
using UnityEngine.SceneManagement; // For changing scene

public class ButtonHandler : MonoBehaviour
{
    public void GetInfile() // Public allows us to select it in the Unity Editor
    {
        string infile = EditorUtility.OpenFilePanel("Load your crystal file", "", "cif"); // Opens a file panel to find the .cif-file
        string filename = Path.GetFileName(infile); // Gets the filename without the path
        transform.Find("Text").GetComponent<Text>().text = filename; // Updates the text of the button to match the selected file

        CrystalManager.infile = infile; // Stores our infile in a static class so it can be fetched in another scene
    }
    public void LaunchGame()
    {
        SceneManager.LoadScene("MainGameScene");
    }
}
