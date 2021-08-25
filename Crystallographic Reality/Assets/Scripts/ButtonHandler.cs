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
    /* OLD METHOD Used UnityEditor, which is disabled for builds.
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

    private void Start()
    {
        //GameObject.Find("SimpleFileBrowserCanvas").SetActive(false); // De-activates the File Browser objects as the game starts
        FileBrowser.HideDialog(); // Hides the FileBrowser as the game starts
    }

    public void GetInfile() // Public allows us to select it in the Unity Editor
    {
        Debug.Log("User clicked the File Button");

        FileBrowser.SetFilters(true, new FileBrowser.Filter("Crystal files", ".cif")); // Sets filters (optional)
        FileBrowser.SetDefaultFilter(".cif"); // Sets .cif to be the default filter (optional)
        FileBrowser.AddQuickLink("Users", "C:\\Users", null); // Adds a Quick Link to the Users folder (optional)

        // Opens Load Dialog
        // OnSuccess: store file chosen as infile
        // OnCancelled: print cancelled
        // Pick only files
        // Do not allow multi-file selection
        // Initial path: Default (Documents)
        // Initial filename: empty
        //FileBrowser.ShowLoadDialog((paths)=> { infile = paths[0]; }, ()=> { Debug.Log("Cancelled"); }, FileBrowser.PickMode.Files, false, null, null, "Load file", "Load");
        //if (FileBrowser.Success) // If the user selected a file
        //{
        //    Debug.Log("LoadDialog was successful. File chosen: " + infile);
        //}
        
        StartCoroutine(ShowLoadDialogCoroutine()); // Starts a Coroutine (whatever that is) which opens and handles the File Browser
        

        /* Moved into ShowLoadDialogCoroutine()
        if (!File.Exists(infile))
        {
            transform.Find("Text").GetComponent<Text>().text = "File not found"; // Updates Button Text
            throw new FileNotFoundException("Could not find chosen .cif-file");
        }
        transform.Find("Text").GetComponent<Text>().text = Path.GetFileName(infile); // Updates the text of the button to match the selected file
        CrystalManager.infile = infile; // Stores our infile in a static class so it can be fetched in another scene

        Debug.Log("User clicked the File Button and selected " + infile);
        */
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

    // Called in GetInfile()
    IEnumerator ShowLoadDialogCoroutine()
    {
        // Taken from https://github.com/yasirkula/UnitySimpleFileBrowser -> Example code
        // and modified a bit

        // Show a load file dialog and wait for a response from user
        // Load file/folder: only files, Allow multiple selection: false
        // Initial path: default (Documents), Initial filename: empty
        // Title: "Load File", Submit button text: "Load"
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, "Load File", "Load");

        // Dialog is closed
        // Print whether the user has selected some files/folders or cancelled the operation (FileBrowser.Success)
        Debug.Log("Did user select file: " + FileBrowser.Success);

        if (FileBrowser.Success)
        {
            // Print paths of the selected file (FileBrowser.Result) (null, if FileBrowser.Success is false)
            string infile = FileBrowser.Result[0]; // Updates infile
            Debug.Log("File chosen by user:" + infile); // Reports the file choice


            if (!File.Exists(infile))
            {
                transform.Find("Text").GetComponent<Text>().text = "File not found"; // Updates Button Text
                throw new FileNotFoundException("Could not find chosen .cif-file");
            }
            transform.Find("Text").GetComponent<Text>().text = Path.GetFileName(infile); // Updates the text of the button to match the selected file
            CrystalManager.infile = infile; // Stores our infile in a static class so it can be fetched in another scene

            Debug.Log("User clicked the File Button and selected " + infile);
        }
    }
}
