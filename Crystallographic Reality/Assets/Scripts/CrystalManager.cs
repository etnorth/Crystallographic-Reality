using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System; // For exceptions
using System.IO; // IO: InputOutput. Used to read files and handle e.g. Paths

public static class CrystalManager
{
    public static string infile; // Input .cif-file (alt. name = ciffile)
    public static string outfile; // Output .txt-file for constructing Crystal (alt. name = txtfile)

    // Called in ButtonHandler.cs -> LaunchGame()
    public static void TestSetup()
    {
        // Test that the setup required for the program to run in fact works

        // Checks that the persistentDataPath has required files and folders
        if (!Directory.Exists(Application.persistentDataPath + @"\cif2cell_convert\")) // If the cif2cell_convert folder does not exist in the persistentDataPath
        {
            Directory.CreateDirectory(Application.persistentDataPath + @"\cif2cell_convert"); // Create cif2cell_convert folder
        }
        if (!File.Exists(Application.persistentDataPath + @"\cif2cell")) // If cif2cell does not exist in the persistentDataPath
        {
            File.Copy(Application.dataPath + @"\Scripts\cif2cell", Application.persistentDataPath + @"\cif2cell"); // Copy cif2cell from Assets/Scipts to the persistentDataPath in AppData
        }

        // Tests Python
        System.Diagnostics.Process process = new System.Diagnostics.Process(); // Creates a process to run the Command Prompt
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo(); // Defines a variable to insert our information in
        startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden; // Hides the Command prompt window from user
        startInfo.FileName = "cmd.exe"; // Calls for the command prompt
        startInfo.Arguments = "/C python --version"; // Asks for its version (/C Carries out the command specified by string and then terminates)
        process.StartInfo = startInfo;
        process.Start();
        process.WaitForExit();
        if (process.ExitCode == 9009)
        {
            throw new FileNotFoundException("Could not find Python");
            // Error: File not Found (aka. Python not installed)
        }
    }

    // Called in ButtonHandler.cs -> LaunchGame()
    public static void ConvertCIF()
    {
        // Converts a .cif-file through cif2cell to generate cell information and store it to a file
        outfile = Application.persistentDataPath + @"\cif2cell_convert\" + Path.GetFileNameWithoutExtension(infile) + ".txt"; // Sets a name for the output-file

        System.Diagnostics.Process process = new System.Diagnostics.Process(); // Creates a process to run
        process.StartInfo.WorkingDirectory = Path.GetDirectoryName(infile); // Sets the working directory to the folder where the .cif-file is located (cif2cell can't read the file from a different directory
        process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden; // Hides the Command prompt window from user
        process.StartInfo.FileName = "cmd.exe"; // Sets Command Prompt as our executable
        process.StartInfo.Arguments = "/C python \"" + Application.persistentDataPath + "\\cif2cell\" " + // Calls for cif2cell in the location cif2cell is stored
            Path.GetFileName(infile) + " --no-reduce --cartesian --print-symmetry-operations " + // Arguments for cif2cell (file is first, but can also do --file=FILE)
            "> \"" + outfile + "\""; // Moves to appropriate directory and puts cif2cell output in a file
        process.Start(); // Starts the process
        process.WaitForExit(); // Waits for the process to end before continuing
        if (process.ExitCode!=0 | !File.Exists(outfile)) // If the process failed or the output file does not exist
        {
            throw new Exception(".cif-conversion failed with Exit Code: " + process.ExitCode + " from the command prompt");
            //Debug.Log("ConvertCIF failed with Exit code: " + process.ExitCode);
        }

        Debug.Log("Ran " + infile + " through cif2cell and generated " + outfile);
    }
}
