using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//using UnityEditor; // For File Dialog
using SimpleFileBrowser;
using UnityEngine.UI; // For getting Text Component
using System; // For exceptions
using System.IO; // For GetFileName (aka. removing path)
using UnityEngine.SceneManagement; // For changing scene

public class ButtonHandler : MonoBehaviour
{
    /* Used UnityEditor, which is disabled for builds.
    public void GetInfile() // Public allows us to select it in the Unity Editor
    {
        string infile = EditorUtility.OpenFilePanel("Load your crystal file", "", "cif"); // Opens a file panel to find the .cif-file
        if (!File.Exists(infile))
        {
            transform.Find("Text").GetComponent<Text>().text = "File not found"; // Updates Button Text
            throw new FileNotFoundException("Could not find chosen .cif-file"); 
        }
        transform.Find("Text").GetComponent<Text>().text = Path.GetFileName(infile); // Updates the text of the button to match the selected file
        CrystalManager.infile = infile; // Stores our infile in a static class so it can be fetched in another scene

        Debug.Log("User clicked the File Button and selected " + infile);
    }
    */
    public void GetInfile() // Public allows us to select it in the Unity Editor
    {
        string infile = ("hey"); // Opens a file panel to find the .cif-file
        if (!File.Exists(infile))
        {
            transform.Find("Text").GetComponent<Text>().text = "File not found"; // Updates Button Text
            throw new FileNotFoundException("Could not find chosen .cif-file");
        }
        transform.Find("Text").GetComponent<Text>().text = Path.GetFileName(infile); // Updates the text of the button to match the selected file
        CrystalManager.infile = infile; // Stores our infile in a static class so it can be fetched in another scene

        Debug.Log("User clicked the File Button and selected " + infile);
    }

    public void LaunchGame()
    {
        CrystalManager.TestSetup();
        if (CrystalManager.infile==null) // If the file has not been chosen
        {
            throw new NullReferenceException(".cif-file not chosen");
        }
        CrystalManager.ConvertCIF();
        SceneManager.LoadScene("MainGameScene");

        Debug.Log("Loaded Scene for Crystal");
    }
}
