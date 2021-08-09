using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System; // For StringSplitOptions to split e.g. 3 or 4 consecutive whitespaces (tab or one whitespace did not work) (also handles our Globalization)
using System.IO; // IO: InputOutput. Used to read our input file. OLD
using System.Linq; // Adds array.where to exlude 0 from .Min(). See Step 3 for reflection in EvalSymmetry()
//using System.Diagnostics; // Provides access to local and remote processes and enables you to start and stop local system processes

public class Crystal : MonoBehaviour
{
    public GameObject crystal; // An object to be used as the parent of the unit cell. In Unity I have selected an empty parent object "Crystal" for this. In hindsight I could have skipped this entirely and made this in the script, but this works fine.
    public GameObject atom; // Atom prefab. Selected manually in Unity. This could also have been made by using GameObject.CreatePrimitive() and then setting constraints via the script
    public float eps = 0.0001f; // tolerance for comparing float numbers (abs(x)<eps => x=0). Meant to avoid rounding errors. (typically called eps or tol from MAT-IN1105)
    public float scaleChange = 0.1f; // A scale for making unit cell smaller/larger.

    private string infile; //.cif-file filename and location
    private int NumOfAtoms; // Number of UNIQUE atoms in the conventional cell (aka. number of atoms from input file, not the amount of atoms that are in the end)
    private string fileinfo; // cif2cell's info about converted file
    private Vector3 cellLength; // Length of the sides of the cell (a, b, c)
    private Vector3 cellAngle; // Angle of the lattice vectors  (alpha, beta, gamma)
    private string[] atomElement; // The element of each atom ( which can now be accessed through gameObject.name.Split(' ')[0] )
    private Vector3[] atomPos; // The position of each atom. (x,z,y), not (x,y,z)
    private float[][,] symmetryMatrices; // An array of Vector3-arrays (1st array to count operations, 2nd array is a 3x3 rotation + 3x1 translation matrix) Matrix given by normal (x,y,z,) and will be converted upon use
    private string[] symmetryMatricesType; // The type of operation for each symmetry matrix
    private float cellVolume; // Volume of the cell Old: Taken from file. New: Calculated
    private Vector3[] bravaisVectors; // Unit cell vectors NOTE: uses Unity (x,z,y)
    private float[,] bravaisMatrix; // We create a matrix for the bravais as well, so we can transform coordinates correctly NOTE: uses normal (x,y,z)
    private float[,] invBravaisMatrix; // We create an inverse matrix for the bravais as well, so we can check corners etc. correctly NOTE: uses normal (x,y,z)
    private string spaceGroup; // OLD

    private GameObject[] atomObjects; // Array of atom objects.
    private Dictionary<string, Color> atomColors = new Dictionary<string, Color>() // A Dictionary to apply colors depending on what atom it is
    {
        // Using https://en.wikipedia.org/wiki/CPK_coloring#Typical_assignments
        {"H", Color.white},
        {"C", Color.black},
        {"N", Color.blue},
        {"O", Color.red},
        {"F", Color.green}, {"Cl", Color.green},
        {"Br", new Color(153/255f, 34/255f, 0, 1)}, // "Dark red"
        {"I", new Color(102/255f, 0, 187/255f, 1)}, //"Dark violet"
        // Skipped noble gases (He, Ne, Ar, Xe, Kr use "Cyan")
        {"P", new Color(255/255f, 153/255f, 0, 1)}, // "Orange"
        {"S", Color.yellow},
        {"B", new Color(255/255f, 170/255f, 119/255f, 1)}, // "Beige
        {"Li", Color.magenta}, {"Na", Color.magenta}, {"K", Color.magenta}, {"Rb", Color.magenta}, {"Cs", Color.magenta},{"Fr", Color.magenta}, // Alkali metals (Should be "Violet")
        {"Be", new Color(0, 119/255f, 0, 1)}, {"Mg", new Color(0, 119/255f, 0, 1)}, {"Ca", new Color(0, 119/255f, 0, 1)}, {"Sr", new Color(0, 119/255f, 0, 1)}, {"Ba", new Color(0, 119/255f, 0, 1)}, {"Ra", new Color(0, 119/255f, 0, 1)}, // Alkaline earth metals ("Dark green")
        {"Ti", Color.gray},
        {"Fe", new Color(221/255f, 119/255f, 0, 1)}, // "Dark orange"
        {"other", new Color(221/255f, 119/255f, 1, 1)}, // "Pink"

        // Not from "Typical Assignments
        {"Si", Color.gray},
        {"Cu", Color.yellow}, // I'd prefer "Orange"
    };
    
    
    // Start is called before the first frame update
    void Start()
    {
        // Initialization (The thought is to have a separate game scene with buttons, sliders, etc. for setting up the crystal. This is parsed to this file and creates the appropriate crystal)

        //ReadConvert(CrystalManager.outfile); // Reads the converted file and stores needed data
        ReadConvert(@"C:\Users\erlen\AppData\LocalLow\UiO TeamVR\Crystallographic Reality\cif2cell_convert\LSMO.txt"); // Temporary, use comment above after testing, and when back in UI menu
        //ReadConvert(@"C:\Users\erlen\AppData\LocalLow\UiO TeamVR\Crystallographic Reality\cif2cell_convert\Si.txt"); // Temporary, use comment above after testing, and when back in UI menu

        // Sets up Lattice Vectors in relation to Unity's coordinate system
        // Got help from https://en.wikipedia.org/wiki/Fractional_coordinates (We use x,y,z)
        
        cellVolume = cellLength[0] * cellLength[1] * cellLength[2] // abc
            * Mathf.Sqrt(1 - (Mathf.Cos(cellAngle[0] * Mathf.Deg2Rad) * Mathf.Cos(cellAngle[0] * Mathf.Deg2Rad)) // * sqrt( 1-cos^2(alpha)
            - (Mathf.Cos(cellAngle[1] * Mathf.Deg2Rad) * Mathf.Cos(cellAngle[1] * Mathf.Deg2Rad)) // -cos^2(beta)
            - (Mathf.Cos(cellAngle[2] * Mathf.Deg2Rad) * Mathf.Cos(cellAngle[2] * Mathf.Deg2Rad)) // -cos^2(gamma)
            + (2 * Mathf.Cos(cellAngle[0] * Mathf.Deg2Rad) * Mathf.Cos(cellAngle[1] * Mathf.Deg2Rad) * Mathf.Cos(cellAngle[2] * Mathf.Deg2Rad))); // + 2*cos(alpha)*cos(beta)*cos(gamma) )
        invBravaisMatrix = new float[,] 
        { 
            {1 / cellLength[0], - (Mathf.Cos(cellAngle[2] * Mathf.Deg2Rad) / (cellLength[0] * Mathf.Sin(cellAngle[2] * Mathf.Deg2Rad))), cellLength[1] * cellLength[2] * ((Mathf.Cos(cellAngle[0] * Mathf.Deg2Rad) * Mathf.Cos(cellAngle[2] * Mathf.Deg2Rad)) - Mathf.Cos(cellAngle[1] * Mathf.Deg2Rad)) / (cellVolume * Mathf.Sin(cellAngle[2] * Mathf.Deg2Rad)) }, // m11, m12, m13
            {0, 1 / (cellLength[1] * Mathf.Sin(cellAngle[2] * Mathf.Deg2Rad)), cellLength[0] * cellLength[2] * ((Mathf.Cos(cellAngle[1] * Mathf.Deg2Rad) * Mathf.Cos(cellAngle[2] * Mathf.Deg2Rad)) - Mathf.Cos(cellAngle[0] * Mathf.Deg2Rad)) / (cellVolume * Mathf.Sin(cellAngle[2] * Mathf.Deg2Rad)) }, // m21, m23, m22
            {0, 0, (cellLength[0] * cellLength[1] * Mathf.Sin(cellAngle[2] * Mathf.Deg2Rad)) / cellVolume } // m31, m32, m33
        }; // Used for getting corner/edge/face atoms correctly

        EvalSymmetry(); // Evaluates each symmetry matrix and categorizes them
        CreateCrystal(); // Constructs the physical unit cell based on conventional atom positions, tags atoms if they match through symmetry, creates corner/edge/face atoms of cell, and adds unit cell "sticks"

        for (int i = 0; i < atomPos.Length; i++)
        {
            Debug.Log("atom: " + (i+1) + " had position "+ LinTransform(invBravaisMatrix,atomPos[i]).ToString("F2"));
        }

        //Debug.Log("x: " + (atomPos[9][0]/cellLength[0]).ToString("F3") + "y: " + 0 + "z: " + (atomPos[9][1]*invBravaisMatrix[2,2]).ToString("F3"));

        CreateUnitCellGrid();
        //CreateSymmetry();

    }

    // Update is called once per frame
    void Update()
    {
        
        //Scale unit cell down
        // USES OLD INPUT SYSTEM. DOES NOT WORK.
        /*
        if (trigger)
        {
            Debug.Log("Left Controller Trigger is held down");
            parent.transform.localScale -= new Vector3(scaleChange, scaleChange, scaleChange);
        }

        //Scale unit cell up
        if (Input.GetKey("Axis1D.SecondaryHandTrigger"))
        {
            Debug.Log("Right Controller Trigger is held down");
            parent.transform.localScale += new Vector3(scaleChange, scaleChange, scaleChange);
        }
        */
    }

    // Called in Start
    void ReadConvert(string infile)
    {
        // Reads a cif2cell run's command-line output in as .txt-file and extracts needed data
        List<string> atomElementList = new List<string>();
        List<Vector3> atomPosList = new List<Vector3>();
        List<float[,]> symmetryMatricesList = new List<float[,]>(); // A list of multi-dimensional arrays (each Vector array is a 3x3 matrix + 3x1 translation)

        string[] lines = File.ReadAllLines(infile); // Reads the file and stores each line in the array "lines"
        string[] words; // We initialize words before using it, though we also could have initialized it within each if I believe
        for (int i = 0; i < lines.Length; i++) // Initially foreach, but took for to get easier enumeration and skippable lines
        {
            if (lines[i].Contains("Lattice parameters:"))
            {
                // cellLength (the length is two lines below "Lattice parameters:". Therefore i + 2)
                words = lines[i + 2].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // the "new[]" part is just to get the correct overload of the Split-function. RemoveEmptyEntries fixes consecutive spaces in the file
                cellLength.x = StringToFloat(words[0]);
                cellLength.y = StringToFloat(words[1]);
                cellLength.z = StringToFloat(words[2]); // We use x,y,z here

                // cellAngle (the angle is four lines below "Lattice parameters:". Therefore i + 4)
                words = lines[i + 4].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                cellAngle[0] = StringToFloat(words[0]); // alpha ([0] is equivalent to .x)
                cellAngle[1] = StringToFloat(words[1]); // beta
                cellAngle[2] = StringToFloat(words[2]); // gamma

                i += 4; // Skips next lines as they've already been read
            }
            else if (lines[i].Contains("Bravais lattice vectors :"))
            {
                // cellVectors and cellMatrix
                words = lines[i + 1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // a_vec
                string[] moreWords = lines[i + 2].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // b_vec
                string[] evenMoreWords = lines[i + 3].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // c_vec

                bravaisMatrix = new float[,]
                {
                    {StringToFloat(words[0]), StringToFloat(words[1]), StringToFloat(words[2]) }, // We might need it for translational coordinates
                    {StringToFloat(moreWords[0]), StringToFloat(moreWords[1]), StringToFloat(moreWords[2]) },
                    {StringToFloat(evenMoreWords[0]), StringToFloat(evenMoreWords[1]), StringToFloat(evenMoreWords[2]) }
                };
                bravaisVectors = new Vector3[]
                {
                    new Vector3(bravaisMatrix[0, 0],bravaisMatrix[0, 2],bravaisMatrix[0, 1]), // a_vec (x,z,y)
                    new Vector3(bravaisMatrix[2, 0],bravaisMatrix[2, 2],bravaisMatrix[2, 1]), // c_vec (x,z,y)
                    new Vector3(bravaisMatrix[1, 0],bravaisMatrix[1, 2],bravaisMatrix[1, 1]), // b_vec (x,z,y)
                };

                i += 3; // Skips next lines as they've already been read
            }
            else if (lines[i].Contains("All sites")) // Looks for conventional cell atom sites
            {
                words = lines[i + 2].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // The 1st line with info is 2 lines below
                while (words.Length > 0) // After "All sites" is a blank line (len=0), so we keep going until then
                {
                    atomElementList.Add(words[0]);
                    atomPosList.Add(new Vector3(StringToFloat(words[1]),
                        StringToFloat(words[3]),
                        StringToFloat(words[2]))); // We use (x,z,y) as Unity has y be vertical, and z be horisontal like x

                    i++; // We increment i for each rep. site we read
                    words = lines[i + 2].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // Update words for next iteration
                }
            }
            else if (lines[i].Contains("Operation "))
            {
                words = lines[i + 1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // The actual operation is one line below
                symmetryMatricesList.Add(new float[,]
                {
                    { StringToFloat(words[0]), StringToFloat(words[3]), StringToFloat(words[6]), StringToFloat(words[9]) }, // (a11, a12, a13, a14) row 1 (x,y,z)
                    { StringToFloat(words[1]), StringToFloat(words[4]), StringToFloat(words[7]), StringToFloat(words[10]) }, // (a21, a22, a23, a24) row 2 (x,y,z)
                    { StringToFloat(words[2]), StringToFloat(words[5]), StringToFloat(words[8]), StringToFloat(words[11]) }  // (a31, a32, a33, a34) row 3 (x,y,z)
                });
                i++; // We increment i to avoid reading the line of the numbers, and rather skip to next "Operation "
            }
        }
        // Converts lists to arrays, for faster access (When creating cells atomElement and atomPos will be converted back to lists and filled, then over to array again)
        atomElement = atomElementList.ToArray();
        atomPos = atomPosList.ToArray();
        symmetryMatrices = symmetryMatricesList.ToArray();

        Debug.Log("Read the cif2cell-converted file and stored data");
    }

    // Called in Start
    void EvalSymmetry()
    {
        // Evaluates each Vector3[] to determine type of symmetry operation. Uses the global symmetryMatrices
        List<string> symmetryMatricesTypeList = new List<string>();

        for (int i = 0; i < symmetryMatrices.Length; i++) // .Length counts from 1, so we subtract 1 to count from 0
        {
            float[,] matrix = symmetryMatrices[i];
            string axis;

            float det = Det(matrix);
            float trace = Trace(matrix);
            float absNonDiagSum = AbsNonDiagSum(matrix);

            // Step 1: Det(ermine) determinant
            if (det > 0) // If determinant is positive
            {
                // Identity or rotation axis (Not rotoinversion)

                // Step 2: Identity has Trace(M) = 3 and absNonDiagSum = 0
                if (Mathf.Abs(trace-3) < eps & absNonDiagSum < eps) // uses < and a tolerance to avoid float number errors
                {
                    //Identity
                    symmetryMatricesTypeList.Add("Identity");
                }
                else
                {
                    // Rotation axis
                    int degOfRotation;

                    // Step 3: Determine axis of rotation
                    /*
                        1  0    0
                        0 cos -sin   x
                        0 sin  cos
                       
                        cos 0 -sin
                         0  1   0    y
                        sin 0  cos
                       
                        cos -sin 0
                        sin  cos 0   z
                         0    0  1
                        theta varies from 0.5 to -1 to 0.5 (6_1 -> 6_5) (which includes all 2, 3 and 4 within the interval) (Means only one axis will have diag = 1
                    */

                    // Find diagonal element with value = 1. This is the axis of rotation
                    if (Mathf.Abs(matrix[0, 0] - 1) < eps) // If element a11 = 1
                    {
                        // x-axis
                        axis = "(1,0,0)";
                        // Step 4: Determine degree of rotation (2=180, 3=120, 4=90, 6=60) // n=5,7 not included (molecules) (yet?)
                        degOfRotation = DegreeOfRotation(Mathf.Atan2(matrix[2, 1], matrix[1, 1]) * Mathf.Rad2Deg); // Uses atan(y/x) and converts to degrees

                    }
                    else if (Mathf.Abs(matrix[1, 1] - 1) < eps) // If element a22 = 1
                    {
                        // y-axis
                        axis = "(0,1,0)";
                        // Step 4: Determine degree of rotation (2=180, 3=120, 4=90, 6=60) // n=5,7 not included (molecules) (yet?)
                        degOfRotation = DegreeOfRotation(Mathf.Atan2(matrix[2, 0], matrix[0, 0]) * Mathf.Rad2Deg);
                    }
                    else if (Mathf.Abs(matrix[2, 2] - 1) < eps) // If element a33 = 1
                    {
                        // z-axis
                        axis = "(0,0,1)";
                        // Step 4: Determine degree of rotation (2=180, 3=120, 4=90, 6=60) // n=5,7 not included (molecules) (yet?)
                        degOfRotation = DegreeOfRotation(Mathf.Atan2(matrix[1, 0], matrix[0, 0]) * Mathf.Rad2Deg);
                    }
                    else
                    {
                        // Below only works if matrix is non-symmetric.
                        // Original taken from https://en.wikipedia.org/wiki/Rotation_matrix#Determining_the_axis
                        // See http://scipp.ucsc.edu/~haber/ph116A/rotation_11.pdf page 7 for full guide

                        float[] u = new float[] { // There was a prefactor, but it caused tricky thetas and ugly vectors, so I removed it.
                                (matrix[2, 1] - matrix[1, 2]),
                                (matrix[0, 2] - matrix[2, 0]),
                                (matrix[1, 0] - matrix[0, 1]) }; // u = (a32-a23, a13-a31, a21-a12)

                        if (Mathf.Abs(u[0]) < eps && Mathf.Abs(u[1]) < eps && Mathf.Abs(u[2]) < eps) // Should take care of symmetrycal matrices
                        {
                            Debug.LogError("NotImplemented: Find eigenvector with lambda=1 for rotation axis");
                            u = new float[] { 0, 0, 0 };
                        }


                        /* This caused issues for non-symmetrical matrices, so I changed it back to the old formula again
                        float[] u;
                        if ((Mathf.Abs(trace) + 1) < eps && (Mathf.Abs(trace) - 3) < eps) // trace != -1, 3
                        {
                            float prefactor = 1 / (Mathf.Sqrt((3 - trace) * (1 + trace)));
                            u = new float[] {
                                prefactor*(matrix[2, 1] - matrix[1, 2]),
                                prefactor*(matrix[0, 2] - matrix[2, 0]),
                                prefactor*(matrix[1, 0] - matrix[0, 1]) }; // u = prefactor * (a32-a23, a13-a31, a21-a12)
                        }
                        else
                        {
                            // We have ruled out theta=0 as that is identity. This is theta=pi rad = 180 deg
                            // However, we still do not know the axis of rotation
                            Debug.LogWarning("NotImplemented: Find eigenvector with lambda=1 for rotation axis");
                            u = new float[] { 0, 0, 0 };
                        }
                        */

                        axis = "(" + u[0] + "," + u[1] + "," + u[2] + ")";

                        // Now, to determine the angle theta for degOfRotation,
                        // we can either use that ||u||=2*sin(theta),
                        // or we can use Trace(matrix) = 1 + 2 cos(theta).
                        // Using trace seems simpler, computationally, as we have a function for that already. // Trace is might already be zero, but it wouldn't use trace instead of -1/2
                        // However, by using both methods, we can again use Atan2 for increased range of theta
                        float sin = Mathf.Sqrt((u[0] * u[0]) + (u[1] * u[1]) + (u[2] * u[2]))/2; // ||u|| = sqrt(x^2+y^2+z^2). sin(theta) = ||u||/2
                        float cos = (trace - 1f) / 2; // Trace(M) = 1 + 2*cos -> cos = (Trace(M)-1)/2
                        degOfRotation = DegreeOfRotation(Mathf.Atan2(sin,cos) * Mathf.Rad2Deg); // Atan2 only works when sin>0, for sin<0 the angle is off by 180 deg. The function takes this into account
                    }
                    
                    // Step 5: Determine rotation vs. screw
                    if (Mathf.Abs(matrix[0,3])+ Mathf.Abs(matrix[1, 3])+ Mathf.Abs(matrix[2, 3]) < eps) // If the sum of the translation vector components = 0
                    {
                        // Rotation axis
                        symmetryMatricesTypeList.Add("Rotation " + axis + " " + degOfRotation);
                    }
                    else // Screw axis
                    {
                        // n_m = rotation (n) + translation (m/n). Try different values of m to fit with translation
                        // Originally did translation * n = m and compared for different m, but I could just assign it as-is
                        if (degOfRotation != 2) // 2-fold screw axis can only be 2_1, so we skip it entirely
                        {
                            if (Mathf.Abs(matrix[0,3]) < eps) // Rotation can leave one coordinate zero and translate the other two, so we take this into account
                            {
                                symmetryMatricesTypeList.Add("Screw " + axis + " " + degOfRotation + " " + (Mathf.Abs(matrix[0, 3] * degOfRotation)));
                            }
                            else // if x = 0, then y and z should be != 0
                            {
                                symmetryMatricesTypeList.Add("Screw " + axis + " " + degOfRotation + " " + (Mathf.Abs(matrix[1, 3] * degOfRotation)));
                            }
                        }
                        else
                        {
                            symmetryMatricesTypeList.Add("Screw " + axis + " " + degOfRotation + " 1");
                        }
                    }
                }

            }
            else if (det < 0) // If determinant is negative
            {
                // Inversion or reflection (also rotoinversion)

                // Step 2: Inversion has Trace(M) = - 3 and absNonDiagSum = 0
                if (Mathf.Abs(trace + 3) < eps & absNonDiagSum < eps)
                {
                    // Inversion
                    symmetryMatricesTypeList.Add("Inversion");
                }
                else
                {
                    // Reflection (or rotoinversion)
                    // Step 3: Determine plane of reflection (Reflections only have one diag with value = -1)
                    if (Mathf.Abs(matrix[0, 0] + 1) < eps) // x-axis is plane normal
                    {
                        axis = "(1,0,0)";
                        // Step 4: Determine mirror vs. glide
                    }
                    else if (Mathf.Abs(matrix[1, 1] + 1) < eps) // y-axis is plane normal
                    {
                        axis = "(0,1,0)";
                    }
                    else if (Mathf.Abs(matrix[2, 2] + 1) < eps) // z-axis is plane normal
                    {
                        axis = "(0,0,1)";
                    }
                    else 
                    {
                        float[] v = { 1, 2, 3 }; // We define an arbitrary vector to be reflected
                        float[] u = LinTransform(matrix,v); // Mv = u
                        float[] displacement = { u[0] - v[0],
                            u[1] - v[1],
                            u[2] - v[2] }; // The displacement is the normal of the reflection plane. displacement = u-v

                        if (Mathf.Abs(displacement[0]) < eps & Mathf.Abs(displacement[1]) < eps & Mathf.Abs(displacement[2]) < eps)
                        {
                            Debug.LogError("The plane normal is (0,0,0). Either the plane normal is (1,2,3) as that was our input, or this is a rotoinversion or something else? Symmetry operation: " + (i + 1));
                            axis = "unknown";
                        }
                        else
                        {
                            float min = displacement.Where(x => !(Mathf.Abs(x) < eps)).Min(); // Finds the minimum value in displacement that is not 0.
                            displacement = new float[] { displacement[0] / min,
                            displacement[1] / min,
                            displacement[2] / min }; // Makes displacement use smaller values ( (0,-5,-5) -> (0,1,1). Since (0,-1,1)==(0,1,-1) this should not cause issues with flipping planes incorrectly
                            axis = "(" + displacement[0] + "," + displacement[1] + "," + displacement[2] + ")";
                        }
                    }

                    // Step 4: Determine mirror vs. glide
                    if (Mathf.Abs(matrix[0, 3]) + Mathf.Abs(matrix[1, 3]) + Mathf.Abs(matrix[2, 3]) < eps)
                    {
                        symmetryMatricesTypeList.Add("Mirror " + axis);
                    }
                    else
                    {
                        // Glide plane
                        // Step 5: Determine type of glide plane
                        // a-glide (0.5, 0, 0)
                        // b-glide (0, 0,5, 0)
                        // c-glide (0, 0, 0.5)
                        // n-glide (0.5, 0.5, 0.5)
                        // d-glide (0.25, 0.25, 0.25) (We ignore e-glide as that is two other glides combined)

                        if ((Mathf.Abs(matrix[0, 3]) - 0.5) < eps && (Mathf.Abs(matrix[1, 3]) + Mathf.Abs(matrix[2, 3])) < eps) // If x=0.5 and y and z = 0
                        {
                            // a-glide
                            symmetryMatricesTypeList.Add("Glide " + axis + " a");
                        }
                        else if ((Mathf.Abs(matrix[1, 3]) - 0.5) < eps && (Mathf.Abs(matrix[0, 3]) + Mathf.Abs(matrix[2, 3])) < eps) // If y=0.5 and x and z = 0
                        {
                            // b-glide
                            symmetryMatricesTypeList.Add("Glide " + axis + " b");
                        }
                        else if ((Mathf.Abs(matrix[2, 3]) - 0.5) < eps && (Mathf.Abs(matrix[0, 3]) + Mathf.Abs(matrix[2, 3])) < eps) // If z=0.5 and x and z = 0
                        {
                            // c-glide
                            symmetryMatricesTypeList.Add("Glide " + axis + " c");
                        }
                        else if (((Mathf.Abs(matrix[0, 3]) - 0.5) < eps && (Mathf.Abs(matrix[1, 3]) - 0.5) < eps)) // If x and y = 0.5
                        {
                            // n-glide
                            symmetryMatricesTypeList.Add("Glide " + axis + " n xy");
                        }
                        else if (((Mathf.Abs(matrix[0, 3]) - 0.5) < eps && (Mathf.Abs(matrix[2, 3]) - 0.5) < eps)) // If x and z = 0.5
                        {
                            // n-glide
                            symmetryMatricesTypeList.Add("Glide " + axis + " n xz");
                        }
                        else if (((Mathf.Abs(matrix[1, 3]) - 0.5) < eps && (Mathf.Abs(matrix[2, 3]) - 0.5) < eps)) // If y and z = 0.5
                        {
                            // n-glide
                            symmetryMatricesTypeList.Add("Glide " + axis + " n yz");
                        }
                        else if (((Mathf.Abs(matrix[0, 3]) - 0.25) < eps && (Mathf.Abs(matrix[1, 3]) - 0.25) < eps)) // If x and y = 0.5
                        {
                            // d-glide
                            symmetryMatricesTypeList.Add("Glide " + axis + " d xy");
                        }
                        else if (((Mathf.Abs(matrix[0, 3]) - 0.25) < eps && (Mathf.Abs(matrix[2, 3]) - 0.25) < eps)) // If x and z = 0.5
                        {
                            // d-glide
                            symmetryMatricesTypeList.Add("Glide " + axis + " d xz");
                        }
                        else if (((Mathf.Abs(matrix[1, 3]) - 0.25) < eps && (Mathf.Abs(matrix[2, 3]) - 0.25) < eps)) // If y and z = 0.5
                        {
                            // d-glide
                            symmetryMatricesTypeList.Add("Glide " + axis + " d yz");
                        }
                        else
                        {
                            // e-glide (I HOPE)
                            Debug.LogWarning("Could not determine type of glide plane. Maybe e-glide? Defaulting to e-glide. Symmetry operation: " + (i + 1));
                            symmetryMatricesTypeList.Add("Glide " + axis + " e");
                            //throw new NotImplementedException("Could not determine type of glide plane. Symmetry operation: " + (i + 1));
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("Could not evaluate symmetry based on determinant: " + det + ". Fix not implemented. Symmetry operation: " + (i + 1));
                symmetryMatricesTypeList.Add("Unknown");
                //throw new NotImplementedException("Could not evaluate symmetry based on determinant: " + det + ". Fix not implemented. Symmetry operation: " + (i + 1));
            }
            Debug.Log("Identified Symmetry Operation " + (i + 1) + " as " + symmetryMatricesTypeList[i]);
        }
        symmetryMatricesType = symmetryMatricesTypeList.ToArray();
    }

    // Called in Start
    void CreateCrystal()
    {
        // Constructs the crystal unit cell by running the representative sites through each symmetry matrix
        // Whenever a duplicate atom is made, it should delete the duplicate and rather add a tag to the original atom, binding it to it's symmetrical equivalent

        GameObject atomParent = new GameObject("Atoms"); // Creates an empty GameObject to parent all atoms (mostly for tidier structure in Unity Editor)
        atomParent.transform.parent = crystal.transform; // Sets the atomParent as a child of the Crystal
        atomParent.transform.localPosition = new Vector3(0, 0, 0); // Makes sure the atomParent is in the Crystal's (0,0,0) and not the worlds' (0,0,0)
        List<GameObject> atomObjectsList = new List<GameObject>(); // Creates a list to contain each atom's object for easier access. Will be converted to array at the end
        //List<string> atomElementsList = new List<string>(); // Creates a list to contain each atom's element for easier access. Will be converted to array at the end

        // Creates basic atom positions, and tags them with symmetries
        for (int i = 0; i < atomPos.Length; i++) // Loops over each conventional atom site
        {
            atomObjectsList.Add(Instantiate(atom, atomParent.transform, false)); // Creates a physical atom object, with atomParent as parent, and adds it to the list
            //atomElementsList.Add(atomElement[i]); // Adds the atom's element

            atomObjectsList[i].transform.localPosition = atomPos[i]; // Places the atom in its correct position
            atomObjectsList[i].name = atomElement[i] + " " + atomPos[i]; // Names the atom so they are easier to distinguish in the Unity Editor, AND to use for SetAtomColor which takes the name to find element
            SetAtomColor(atomObjectsList[i]); // Sets the atom's color based on its element

            Debug.Log("Added conventional atom: " + atomObjectsList[i].name);

            for (int j = 0; j < symmetryMatrices.Length; j++) // Loops over each symmetry matrix
            {
                Vector3 pos = PerformSymmetry(symmetryMatrices[j], atomPos[i]); // Performs symmetry operation on representative site (including translation)

                for (int k = 0; k < atomPos.Length; k++) // Loops over each conventional atom position
                {

                    // Checks if symmetry-made position is outside unit cell due to symmetry operation, and translates inside unit cell again
                    if (pos[0] > cellLength[0]) // x
                    {
                        pos[0] = pos[0] - cellLength[0];
                    }
                    if (pos[1] > cellLength[1]) // z
                    {
                        pos[1] = pos[1] - cellLength[1];
                    }
                    if (pos[2] > cellLength[0]) // y
                    {
                        pos[2] = pos[2] - cellLength[2];
                    }

                    if (atomElement[k]==atomElement[i] && // If new atom is of same element (if the new atom is in the same site but a different element, we want to check that out)
                        (Mathf.Abs(pos[0] - atomPos[k][0]) < eps) && 
                        (Mathf.Abs(pos[1] - atomPos[k][1]) < eps) && 
                        (Mathf.Abs(pos[2] - atomPos[k][2]) < eps)) // Checks if position already exists from before for this atom
                    {
                        atomObjectsList[i].GetComponent<CustomTag>().AddTag((j + 1) + " " + k); // Adds symmetry tag to atom we just made. "symmetryOperationNumber equivalentAtomPosNumber"
                        Debug.Log("Added tag: \"" + (j + 1) + " " + k + "\" to atom: " + atomObjectsList[i].name);
                    }
                }
            }
        }

        // Adds corner/edge/face atoms
        // Solves for where one coordinate is zero (face)
        for (int i = 0; i < atomPos.Length; i++)
        {
            for (int j = 0; j < 3; j++) // Iterates over the bravais lattice vectors (a -> c -> b)
            {
                if (Mathf.Abs(LinTransform(invBravaisMatrix,atomPos[i])[j]) < eps) // If atom position is approx. 0 (relative to bravais lattice)
                {
                    Vector3 newPos = atomPos[i]; // Updates the equivalent position
                    newPos += bravaisVectors[j]; // We defined cellVectors as a_vec, b_vec, c_vec so it should be fine. Uses cartesian converted bravais coordinates

                    GameObject newAtom = Instantiate(atom, atomParent.transform, false); // Instantiates new atom

                    newAtom.transform.localPosition = newPos; // Sets the equivalent position
                    newAtom.name = atomElement[i] + " " + newPos; // Names the atom so they are easier to distinguish in the Unity Editor
                    SetAtomColor(newAtom); // Sets the atom's color based on its element
                    newAtom.GetComponent<CustomTag>().tags = atomObjectsList[i].GetComponent<CustomTag>().tags; // Adds symmetry tags to atom

                    atomObjectsList.Add(newAtom); // Adds the atom to the list

                    Debug.Log("(1) Created atom: " + newPos.ToString("F2") + " from " + atomPos[i].ToString("F2"));

                    // Solves for where two coordinates are zero (edge). NOTE: This creates duplicates of atoms as (x,1,0) and (x,0,1) from previous loop are flipped to (x,1,1). Will destroy duplicates after
                    for (int k = 1; k < 3; k++) // Iterates over b and c (c -> b)
                    {
                        if (Mathf.Abs(LinTransform(invBravaisMatrix,newPos)[k]) < eps | Mathf.Abs(LinTransform(invBravaisMatrix, newPos-bravaisVectors[j])[k]) < eps) // If atom position is approx. 0
                        {
                            Vector3 newerPos = newPos;
                            newerPos += bravaisVectors[k]; // Sets start of cell to end of cell

                            GameObject newerAtom = Instantiate(atom, atomParent.transform, false);

                            newerAtom.transform.localPosition = newerPos; // Sets the equivalent position
                            newerAtom.name = atomElement[i] + " " + newerPos; // Names the atom
                            SetAtomColor(newerAtom); // Sets the atom's color based on its element
                            newerAtom.GetComponent<CustomTag>().tags = atomObjectsList[i].GetComponent<CustomTag>().tags; // Adds symmetry tags to atom

                            atomObjectsList.Add(newerAtom); // Instantiates an equivalent atom to the original

                            Debug.Log("(2) Created atom: " + newerPos.ToString("F2") + " from " + newPos.ToString("F2"));

                            // Solves for where three coordinates are zero (corner)
                            // Iterates over b
                            if (Mathf.Abs(LinTransform(invBravaisMatrix,newerPos)[2]) < eps | Mathf.Abs(LinTransform(invBravaisMatrix, newerPos - bravaisVectors[j] - bravaisVectors[k])[2]) < eps) // If atom position is approx. 0
                            {
                                Vector3 newestPos = newerPos;
                                newestPos += bravaisVectors[2]; // Sets start of cell to end of cell

                                GameObject newestAtom = Instantiate(atom, atomParent.transform, false);

                                newestAtom.transform.localPosition = newestPos; // Sets the equivalent position
                                newestAtom.name = atomElement[i] + " " + newestPos; // Names the atom
                                SetAtomColor(newestAtom); // Sets the atom's color based on its element
                                newestAtom.GetComponent<CustomTag>().tags = atomObjectsList[i].GetComponent<CustomTag>().tags; // Adds symmetry tags to atom

                                atomObjectsList.Add(newestAtom); // Instantiates an equivalent atom to the original

                                Debug.Log("(3) Created atom: " + newestPos.ToString("F2") + " from " + newerPos.ToString("F2"));
                            }
                        }
                    }
                }
            }
        }

        // Destroys duplicate atoms
        for (int i = 0; i < atomObjectsList.Count - 2; i++)
        {
            // (This should deal with all atoms, as the algorithm only makes dupes 2 indexes apart. If not, iterate again with j and j != 0)
            if (atomObjectsList[i].transform.position == atomObjectsList[i + 2].transform.position) // If position vectors are equal (Vector3 includes approximation)
            {
                Destroy(atomObjectsList[i + 2]); // Destroys atom
                atomObjectsList.RemoveAt(i + 2); // Removes the now destroyed atom from the list
                //atomElementsList.RemoveAt(i + 1); // Removes the element so we have track of it
            }
        }

        atomObjects = atomObjectsList.ToArray(); // Converts the atomObjectsList to an array (arrays are better, faster, harder, stronger)

        Debug.Log("Created Crystal");
    }

    // Called in Start
    void CreateUnitCellGrid()
    {
        // Creates the sticks for the sides of the unit cell, giving a clear picture of the unit cell's boundaries
        GameObject gridParent = new GameObject("Unit Cell Grid"); // Creates an empty GameObject to store gridLines in
        gridParent.transform.parent = crystal.transform; // Sets unitCellGrid as a child of the Crystal
        gridParent.transform.localPosition = new Vector3(0, 0, 0); // Makes sure the unitCellGrid is in the Crystal's (0,0,0)

        GameObject gridLineX = GameObject.CreatePrimitive(PrimitiveType.Cylinder); // Creates a standard Unity cylinder
        gridLineX.transform.parent = gridParent.transform; // Sets the gridLine to a child of the gridParent
        gridLineX.transform.localPosition = new Vector3(0, 0, 0); // Makes sure the gridLine is in the gridParent's (0,0,0)

        GameObject gridLineY = Instantiate(gridLineX, gridParent.transform, false); // Creates copies
        GameObject gridLineZ = Instantiate(gridLineX, gridParent.transform, false);

        gridLineX.name = "X "; // Names the gridLines
        gridLineY.name = "Y ";
        gridLineZ.name = "Z ";
        
        // Scales them to be thinner and as long enough to strech the entire unit cell
        gridLineX.transform.localScale = new Vector3(0.05f, cellLength[0] / 2, 0.05f); // a
        gridLineY.transform.localScale = new Vector3(0.05f, cellLength[1] / 2, 0.05f); // b
        gridLineZ.transform.localScale = new Vector3(0.05f, cellLength[2] / 2, 0.05f); // c


        gridLineX.transform.LookAt(bravaisVectors[0] + crystal.transform.position); // Makes the gridLine's z-component look at the coordinate point the bravais vector points to
        gridLineY.transform.LookAt(bravaisVectors[2] + crystal.transform.position); // b_vec is [2]
        gridLineZ.transform.LookAt(bravaisVectors[1] + crystal.transform.position); // c_vec is [1]

        // We need to look at the y-component instead, so we rotate the gridLines
        gridLineX.transform.Rotate(90, 0, 0); // Rotates X so it faces correctly (parallel to bravais)
        gridLineY.transform.Rotate(90, 0, 0); // Rotates Y so it faces correctly (parallel to bravais)
        gridLineZ.transform.Rotate(90, 0, 0); // Rotates Z so it faces correctly (parallel to bravais)

        gridLineX.transform.localPosition = bravaisVectors[0] / 2f; // Moves the center of the cylinder to the center of its bravais vector (cylinder has its pivot in center not on its bottom)
        gridLineY.transform.localPosition = bravaisVectors[2] / 2f;
        gridLineZ.transform.localPosition = bravaisVectors[1] / 2f;

        // Adds and places gridLines for the other unit cell edges
        GameObject gridLineXA = Instantiate(gridLineX, gridParent.transform, false);
        gridLineXA.transform.localPosition += bravaisVectors[1]; // Translates along b
        GameObject gridLineXB = Instantiate(gridLineX, gridParent.transform, false);
        gridLineXB.transform.localPosition += bravaisVectors[2]; // Translates along c
        GameObject gridLineXC = Instantiate(gridLineX, gridParent.transform, false);
        gridLineXC.transform.localPosition += bravaisVectors[1] + bravaisVectors[2]; // Translates along b and c

        GameObject gridLineYA = Instantiate(gridLineY, gridParent.transform, false);
        gridLineYA.transform.localPosition += bravaisVectors[0]; // Translates along a
        GameObject gridLineYB = Instantiate(gridLineY, gridParent.transform, false);
        gridLineYB.transform.localPosition += bravaisVectors[1]; // Translates along c
        GameObject gridLineYC = Instantiate(gridLineY, gridParent.transform, false);
        gridLineYC.transform.localPosition += bravaisVectors[0] + bravaisVectors[1]; // Translates along a and c

        GameObject gridLineZA = Instantiate(gridLineZ, gridParent.transform, false);
        gridLineZA.transform.localPosition += bravaisVectors[0]; // Translates along a
        GameObject gridLineZB = Instantiate(gridLineZ, gridParent.transform, false);
        gridLineZB.transform.localPosition += bravaisVectors[2]; // Translates along b
        GameObject gridLineZC = Instantiate(gridLineZ, gridParent.transform, false);
        gridLineZC.transform.localPosition += bravaisVectors[0] + bravaisVectors[2]; // Translates along a and b

        Debug.Log("Created Unit Cell grid");
    }

    // Called in Start
    void CreateSymmetry()
    {
        for (int i = 0; i < symmetryMatricesType.Length; i++)
        {
            string symmetry = symmetryMatricesType[i];
            string[] symmetryInfo = symmetry.Split(' '); // Here I know there's only one space, so I do it the easy way

            switch (symmetryInfo[0])
            {
                case "Identity":
                    // This always exists, so ignore it
                    break;
                case "Inversion":
                    // Instantiate Inversion element
                    break;
                case "Rotation":
                    // Instantiate Rotation axis based on axis and degree of rotation
                    break;
                case "Screw":
                    // Instantiate Screw axis based on axis, degree of rotation and subscript
                    break;
                case "Mirror":
                    // Instantiate Mirror plane based on plane normal and size of lattice (CreatePlane())
                    break;
                case "Glide":
                    // Instantiate Glide plane based on plane normal and size of lattice (CreatePlane()), with different color to indicate type of glide
                    break;
                case "Unknown":
                    // throw error message
                default:
                    break;
            }

        }
    }

    // Called in SymmetryEval
    float Det(float[,] matrix)
    {
        //Finds the determinant of a 3x3 rotation matrix (here: in a 3x4 matrix (rotation + translation))
        if(matrix.Length < 3) // Throws an error if the matrix is too small
        {
            throw new ArgumentException("Matrix does not have enough elements to find 3x3 determinant");
        }

        float a = matrix[0, 0] * (matrix[1, 1] * matrix[2, 2] - matrix[1, 2] * matrix[2, 1]); // a11 * (a22*a33 - a23*a32)
        float b = matrix[0, 1] * (matrix[1, 0] * matrix[2, 2] - matrix[1, 2] * matrix[2, 0]); // a12 * (a21*a33 - a23*a31)
        float c = matrix[0, 2] * (matrix[1, 0] * matrix[2, 1] - matrix[1, 1] * matrix[2, 0]); // a13 * (a21*a32 - a22*a31)

        return a - b + c;
    }

    // Called in SymmetryEval
    float Trace(float[,] matrix)
    {
        // Adds diagonal elements of a 3x3 matrix (Here: 3x3 rotation + 3x1 translation)

        if (matrix.Length < 3) // Throws an error if the matrix is too small
        {
            throw new ArgumentException("Matrix does not have enough elements to trace 3x3 matrix");
        }

        return matrix[0, 0] + matrix[1, 1] + matrix[2, 2];
    }

    // Called in SymmetryEval
    float AbsNonDiagSum(float[,] matrix)
    {
        // Adds together the sum of the absolute value of all non-diagonal elements in a 3x3 matrix (Here: 3x3 rotation + 3x1 translation)
        // Uses absolute value of induvidual values to avoid elements cancelling each other out
        
        if (matrix.Length < 3) // Throws an error if the matrix is too small
        {
            throw new ArgumentException("Matrix does not have enough elements to add non-diag elements in 3x3 matrix");
        }

        return Mathf.Abs(matrix[0, 1]) + Mathf.Abs(matrix[0, 2])
            + Mathf.Abs(matrix[1, 0]) + Mathf.Abs(matrix[1, 2])
            + Mathf.Abs(matrix[2, 0]) + Mathf.Abs(matrix[2, 1]); // Could have looped but is more complicated and not needed (plus this is likely faster)
    }

    // Called in SymmetryEval/Rotation axis
    int DegreeOfRotation(float theta, float eps = 0.0001f)
    {
        // This function determines the degree of rotation of a rotation axis based on its angle
        if (Mathf.Abs(theta - 180) < eps)
        {
            return 2;
        }
        else if (Mathf.Abs(theta - 120) < eps)
        {
            return 3;
        }
        else if (Mathf.Abs(theta - 90) < eps)
        {
            return 4;
        }
        else if (Mathf.Abs(theta + 90) < eps) // arctan(1/0) = error, but atan2() gives -90. However, it should give +90. We take this into account here
        {
            return 4;
        }
        else if (Mathf.Abs(theta - 60) < eps)
        {
            return 6;
        }
        else
        {
            Debug.LogError("Degree of rotation was not 2, 3, 4 or 6. theta=" + theta);
            return 0;
            //throw new NotImplementedException("Degrees not matching 2, 3, 4 and 6 not implemented");
        }
    }

    // Called in SymmetryEval
    float[] LinTransform(float[,] M, float[] v)
    {
        // This function performs a linear transformation "M" on a vector "v"

        float[] u = new float[v.Length]; // Define the output vector to fill iteratively
        for(int i = 0; i < v.Length; i++) // Loop over each element in the vector (each row in v)
        {
            for (int j = 0; j < M.GetLength(0); j++) // Loop over each row j, in column i, of M (array.GetLength(0) gives number of rows, array.GetLength(1) gives number of columns)
            {
                u[j] += v[i] * M[j,i];
            }
        }
        return u;
    }

    // Overload of LinTransform, called in CreateCrystal for corner/edge/face atoms
    Vector3 LinTransform(float[,] M, Vector3 v)
    {
        // Takes in a normal (x,y,z) matrix M, and a Unity (x,z,y) Vector v
        float[] a = new float[] { v[0], v[2], v[1] }; // Swaps y and z so that the third coordinate is the vertical axis ( (x,z,y)->(x,y,z) )
        float[] b = LinTransform(M, a); // Transforms the array-vector, a, with the matrix, M. This uses the overload of LinTransform that uses arrays, where the actual transformation is perfomed

        return new Vector3(b[0], b[2], b[1]); // Swaps y and z back again ( (x,y,z) -> (x,z,y) )
    }

    // Called in CreateCrystal. Overload to work for Vector3 using "(x,z,y)"
    Vector3 PerformSymmetry(float[,] M, Vector3 v)
    {
        // This LinTransform takes a multidimensional array as a matrix and a Unity.Vector3, and transforms the vector using the matrix
        // Unity uses y is vertical and z is horisontal, but we use z as vertical and y as horisontal.
        // We therefore need to switch the vector around before and after the transform (or switch the matrix around).

        float[] a = new float[] { v[0], v[2], v[1] }; // Swaps y and z so that the third coordinate is the vertical axis ( (x,z,y)->(x,y,z) )
        float[] b = LinTransform(M, a); // Transforms the array-vector, a, with the matrix, M. This uses the overload of LinTransform that uses arrays, where the actual transformation is perfomed
        b = new float[] { b[0] + (M[0, 3]*cellLength[0]), 
            b[1] + (M[1, 3]*cellLength[1]), 
            b[2] + (M[2, 3]*cellLength[2]) }; // Translates b according to the translational component of M, and multiplies with cellLength to handle fractional coordinates BUT NOT BRAVAISVECTORS WHICH IS BAD

        return new Vector3(b[0], b[2], b[1]); // Swaps y and z back again ( (x,y,z) -> (x,z,y) )
    }

    // Called in CreateCell OLD
    void SetAtomColor(GameObject atom, string element)
    {
        // Sets the color of an atom through the renderer's material by accessing a global dictionary "atomColors"
        try
        {
            atom.GetComponent<Renderer>().material.color =  atomColors[element]; // Changes the material color of the gameobject's renderer component
        }
        catch (KeyNotFoundException) // If atom is not in the dictonary, default to "other"
        {
            atom.GetComponent<Renderer>().material.color = atomColors["other"];
        }
    }

    // Overload of SetAtomColor, called in CreateCrystal
    void SetAtomColor(GameObject atom)
    {
        // Sets the color of an atom through the renderer's material by accessing a global dictionary "atomColors"
        // Uses the name of the GameObject to set the color
        string element = atom.name.Split(' ')[0]; // Gets the "First name" of the gameobject (e.g. "Si" from "Si (0,0,0)") and sets that as the element
        if (element.Contains('/'))
        {
            // We have multiple atoms with occurences
            element = element.Split('/')[0]; // For now, default to first atom, later maybe include Random.range(occ1, occ2)(or other if more than two atoms)
        }
        try
        {
            atom.GetComponent<Renderer>().material.color = atomColors[element];
        }
        catch (KeyNotFoundException) // If atom is not in the dictonary, default to "other"
        {
            atom.GetComponent<Renderer>().material.color = atomColors["other"];
        }

    }

    // Called in AddSymmetry
    void ToTransparentMode(Material material)
    {
        // Makes a material use the Transparent Rendering Mode
        // Taken from https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Inspector/StandardShaderGUI.cs (this is how Unity changes rendering in Client)
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    float StringToFloat(string str)
    {
        return float.Parse(str, System.Globalization.CultureInfo.InvariantCulture); // InvariantCulture makes sure commas and periods don't cause problems
    }

    // Called in AddSymmetry
    GameObject CreatePlane(Vector3[] vertices, Color color, string name = "Plane", GameObject parent = null)
    {
        // Creates a Quad GameObject for symmetry planes using four input coordinates (each corner). This should make planes work in crystals where not all angles are 90
        // Made using https://docs.unity3d.com/Manual/Example-CreatingaBillboardPlane.html
        // vertices are given as (bottomLeft, bottomRight, topLeft, topRight)

        GameObject planeBoth = new GameObject(name);
        GameObject planeFront = new GameObject("Front");
        GameObject planeBack;
        planeFront.transform.parent = planeBoth.transform;

        // Creates a mesh
        {
            MeshRenderer meshRenderer = planeFront.AddComponent<MeshRenderer>(); // Adds a meshRenderer to the planeFront, and stores it for ease of access
            meshRenderer.material = new Material(Shader.Find("Standard")); // Not sure why this is needed, but without it looks purple (guessing it is the "lack-of-material"-material) (was sharedMaterial, but changed it to material)

            MeshFilter meshFilter = planeFront.AddComponent<MeshFilter>(); // Adds a meshFilter

            Mesh mesh = new Mesh(); // Creates a mesh

            mesh.vertices = vertices; // Gives the mesh our made vertices

            int[] tris = new int[6] // triangles(?) for the mesh
            {
            // lower left triangle
            0,2,1,
            // upper right triangle
            2,3,1
            };
            mesh.triangles = tris;

            Vector3[] normals = new Vector3[4] // Normals(?) for the mesh
            {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward
            };
            mesh.normals = normals;

            Vector2[] uv = new Vector2[4]
            {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
            };
            mesh.uv = uv;

            meshFilter.mesh = mesh; // Applies our newly made mesh to the meshFilter
        }

        planeFront.GetComponent<Renderer>().material.color = color; // Sets the color of the material
        ToTransparentMode(planeFront.GetComponent<Renderer>().material); // Makes the material use the Transparent rendering mode

        planeBack = Instantiate(planeFront, planeBoth.transform, false); // Adds the backside of the plane (Unity only renders one side of the mesh we made)
        planeBack.name = "Back";

        // Pivot of this GameObject is in bottomLeft and not the center of the item, so we need to adjust for offsets
        //planeFront.transform.localPosition = -topRight / 2; // Adjusts for offset
        //planeBack.transform.localPosition = -topRight / 2; // Adjusts for offset
        planeBack.transform.RotateAround(planeBack.GetComponent<Renderer>().bounds.center, vertices[1], 180); // Rotates around the center of the plane (renderer.bounds.center gives "center of bounding box")

        planeBoth.transform.parent = parent.transform;
        planeBoth.transform.localPosition = new Vector3(0, 0, 0);

        return planeBoth;
    }

    // Old method

    // Called in Start
    void ConvertCifToXYZ(string infile)
    {
        // Converts a .cif-file to .xyz using python and cif2cell in the command line

        System.Diagnostics.Process process = new System.Diagnostics.Process(); // Creates a process to run the Command Prompt
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo(); // Defines a variable to insert our information in
        startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden; // Hides the Command prompt window from user
        startInfo.FileName = "cmd.exe"; // Calls for the command prompt
        startInfo.Arguments = "/C cd " + Path.GetDirectoryName(infile) + // cif2cell can' handle files in a named directory, so we call cif2cell through python with a named directory instead
            " & python \"" + Application.persistentDataPath + "\\cif2cell\" " + Path.GetFileName(infile) +
            " --program=xyz --no-reduce --cartesian --outputfile=\"" + Application.persistentDataPath + @"\cif2cell_convert\" +
            Path.GetFileNameWithoutExtension(infile) + ".xyz\""; // Moves to appropriate directory and calls for conversion of chosen .cif-file (\" takes care of potential spaces in directory names)
        process.StartInfo = startInfo; // Puts the information we have defined inside the process
        process.Start(); // Starts the process
        process.WaitForExit(); // Waits for the process to end before continuing

        if (File.Exists(Application.persistentDataPath + @"\cif2cell_convert\" + Path.GetFileNameWithoutExtension(infile) + ".xyz"))
        { // If the converted file exists in the correct location
            Debug.Log(".cif-file converted to .xyz!"); // Prints to the Unity Console
        }
        else // Notifies that the program could not find the converted file
        {
            Debug.Log("Cannot confirm that the conversion worked."); // Prints to the Unity Console
        }

        // .xyz does not include the lattice vectors, so we fetch those manually from the .cif-file. We also fetch the spacegroup

        Debug.Log("Fetching cell parameters manually"); // Prints to the Unity Console
        string[] lines = File.ReadAllLines(infile); // Reads the .cif file as an array of lines
        int index; // Not all .cifs use a single space. We therefore get the index by seeing how many we have.
        int i = 0; // Counter for cellLength
        int j = 0; // Counter for cellAngle
        int k = 0; // Counter for spaceGroup and cellVolume (could have combined these to a counter for all needed parameters)
        foreach (string line in lines)
        {
            if (line.Contains("_cell_length")) // Looks for the length of the sides of the cell (a, b, c)
            {
                string[] words = line.Split(' '); // Splits the line into an array of words. Splits by whitespace
                index = words.Length - 1; // Gets amount of words (aka. how many spaces between name and value)
                if (words[index].Contains("(")) // If the file has included uncertainty, remove it.
                {
                    words[index] = words[index].Remove(words[index].Length - 3);
                }
                cellLength[i] = float.Parse(words[index], System.Globalization.CultureInfo.InvariantCulture); // Sets index i to the length (.cif uses x->y->z so 0->1->2 should be fine)

                i++;
            }
            else if (line.Contains("_cell_angle")) // Looks for the angles (alpha, beta, gamma)
            {
                string[] words = line.Split(' '); // Splits the line into an array of words. Splits by whitespace
                index = words.Length - 1; // Gets amount of words (aka. how many spaces between name and value)
                if (words[index].Contains("(")) // If the file has included uncertainty, remove it. Likely not the case for angles.
                {
                    words[index] = words[1].Remove(words[1].Length - 3);
                }
                cellAngle[j] = float.Parse(words[index], System.Globalization.CultureInfo.InvariantCulture); // Sets index i to the angle (.cif uses x->y->z so 0->1->2 should be fine)

                j++;
            }
            else if (line.Contains("_symmetry_space_group_name_H-M")) // Looks for the Space Group by Hermann-Mauguin notation
            {
                string[] words = line.Split('\''); // Splits the line into an array of words. Splits by '
                spaceGroup = words[1]; // [0] = _symmetry_space_group_name_H-M , [1] = F d -3 m S, [2] = \n

                k++;
            }
            else if (line.Contains("_cell_volume"))
            {
                string[] words = line.Split(' ');
                index = words.Length - 1;
                cellVolume = float.Parse(words[index], System.Globalization.CultureInfo.InvariantCulture);

                k++;
            }
            else if (i == 2 && j == 2 && k == 2) // When we have gotten all lengths and angles, stop the loop (saves time)
            {
                break;
            }
        }

    }

    // Called in Start
    void ReadXYZ(string cellfile)
    {
        // This function will read an .xyz-file and store the data to create the cell
        if (!File.Exists(cellfile)) // If the file does not exist
        {
            // Notify that file does not exist
            Debug.Log("Could not find the converted .xyz-file. This could get funky."); // Prints to the Unity Console
        }

        // Open the file to read from
        using (StreamReader filestream = File.OpenText(cellfile)) // Declares filestream as our "StreamRead" which reads the input file. "using" handles closing and resources, like with open in python (I believe)
        {
            NumOfAtoms = int.Parse(filestream.ReadLine()); // Reads 1st line and converts to int (1st line is number of atoms)
            fileinfo = filestream.ReadLine(); // Reads 2nd line (which is fileinfo from cif2cell)

            atomElement = new string[NumOfAtoms]; // Creates an empty string array of length equaling the number of atoms
            atomPos = new Vector3[NumOfAtoms];
            string line;

            int i = 0;
            while ((line = filestream.ReadLine()) != null) // While filestream is not empty. Think I could have done this with for-loop and without FileStream
            {
                string[] words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // Splits the line on each tab into words
                atomElement[i] = words[0];
                atomPos[i] = new Vector3(float.Parse(words[1], System.Globalization.CultureInfo.InvariantCulture), // X
                    float.Parse(words[3], System.Globalization.CultureInfo.InvariantCulture), // Z (We use (x,y,z), but Unity has y be vertical instead of z)
                    float.Parse(words[2], System.Globalization.CultureInfo.InvariantCulture)); // Y (We use (x,y,z), but Unity has y be vertical instead of z)

                i++;
            }
        }

        /*
        string[] atomElement = new string[NumOfAtoms]; // Creates an empty string array of length equaling the number of atoms
        Transform[] atomPos = new Transform[NumOfAtoms];


        string[] lines = File.ReadAllLines(cellfile); // Reads all lines and saves it to an array of lines
        
        int i = 0;

        foreach (string line in lines)
        {
            string[] words = line.Split('\t'); // Splits the line on each tab into words
            
            foreach (string word in words)
            {
                Debug.Log(word);
            }

            atomElement[i] = words[0];
            atomPos[i].position = new Vector3(float.Parse(words[1]), float.Parse(words[2]), float.Parse(words[3]));

            i++;
        }
        */
    }

    // Called in Start
    void CreateCell()
    {
        // Constructs the cell that is being investigated.

        /* Inverts celllength and normalizes positions. Works, but unless we shrink the atoms, they overlap and when scaling crystal, they become way too large.
        for (int i = 0; i < 3; i++)
        {
            cellLength[i] = 1.0f / cellLength[i]; // Inverts cellLength so we can normalize each atomPos with Vector3.Scale
        }

        for (int i = 0; i < NumOfAtoms; i++)
        {
            atomPos[i] = Vector3.Scale(atomPos[i], cellLength); // Normalizes atomPos so everything is fractional in the crystal
        }
        */

        GameObject atomParent = new GameObject("Atoms"); // Creates an empty GameObject to contain all atoms
        atomParent.transform.parent = crystal.transform; // Sets the atomParent as a child of the Crystal
        atomParent.transform.localPosition = new Vector3(0, 0, 0); // Makes sure the atomParent is in the Crystal's (0,0,0) and not the worlds' (0,0,0)
        List<GameObject> atomObjectsList = new List<GameObject>();
        List<string> atomElementsList = new List<string>();

        Vector3 equivalentPosition; // Initializes position for equivalent atoms (atoms on edges or corners will be duplicated in other edges and corners)
        List<GameObject> equivalentAtomObjects = new List<GameObject>(); // Creates a list for the new atoms
        List<string> equivalentAtomElements = new List<string>(); // Creates a list for the new atomElements
        int equivalentCount = 0;

        //NOTE: This will set y as z and z as y, due to Unity being x,z,y but we using x, y, z. Fix1: Convert to cif x, z, y. Fix2: rotate cell to fit(but rotation will be a mirrored cell due to right hand rule)
        for (int i = 0; i < NumOfAtoms; i++)
        {
            atomObjectsList.Add(Instantiate(atom, atomParent.transform, false)); // Creates an atom with its position relative to the parent
            atomElementsList.Add(atomElement[i]); // Adds the atoms element to the list we will be using
            atomObjectsList[i].transform.localPosition = atomPos[i]; // Sets the atom position to match that of the .xyz-file (can do scaling in Update() )
            SetAtomColor(atomObjectsList[i], atomElement[i]); // Sets the atom's color based on its element
            atomObjectsList[i].name = atomElement[i] + " " + atomPos[i]; // Names the atom so they are easier to distinguish in the Unity Editor

            // Creates atoms not in the .xyz-file, but still useful or necessary
            for (int j = 0; j < 3; j++) // Iterates over x, y and z
            {
                if (Mathf.Abs(atomPos[i][j]) < 0.0001f) // If atom position is approx. 0
                {
                    equivalentPosition = atomPos[i]; // Updates the equivalent position
                    equivalentPosition[j] = cellLength[j]; // Sets start of cell to end of cell to duplicate other end of cell for equivalent atom

                    equivalentAtomObjects.Add(Instantiate(atom, atomParent.transform, false)); // Instantiates an equivalent atom to the original
                    equivalentAtomElements.Add(atomElement[i]); // Adds the atomElement for the equivalent atom
                    equivalentAtomObjects[equivalentCount].transform.localPosition = equivalentPosition; // Sets the equivalent position
                    SetAtomColor(equivalentAtomObjects[equivalentCount], equivalentAtomElements[equivalentCount]); // Sets the atom's color based on its element
                    equivalentAtomObjects[equivalentCount].name = equivalentAtomElements[equivalentCount] + " " + equivalentPosition; // Names the atom so they are easier to distinguish in the Unity Editor

                    Debug.Log("1st created " + equivalentPosition + " from " + atomPos[i]);
                    equivalentCount++;
                }
            }
        }


        // Solves the cases where two coordinates are zero. NOTE: This creates duplicates of atoms as (x,1,0) and (x,0,1) from previous loop are flipped to (x,1,1). Will destroy duplicates.
        List<GameObject> moreEquivalentAtomObjects = new List<GameObject>();
        List<string> moreEquivalentAtomElements = new List<string>();
        equivalentCount = 0;

        //NOTE: This will set y as z and z as y, due to Unity being x,z,y but we using x, y, z. Fix1: Convert to cif x, z, y. Fix2: rotate cell to fit(but rotation will be a mirrored cell due to right hand rule)
        for (int i = 0; i < equivalentAtomObjects.Count; i++)
        {
            for (int j = 1; j < 3; j++) // Iterates over y and z 
            {
                if (Mathf.Abs(equivalentAtomObjects[i].transform.localPosition[j]) < 0.0001f) // If atom position is approx. 0
                {
                    equivalentPosition = equivalentAtomObjects[i].transform.localPosition;
                    equivalentPosition[j] = cellLength[j]; // Sets start of cell to end of cell to duplicate other end of cell for equivalent atom

                    moreEquivalentAtomObjects.Add(Instantiate(atom, atomParent.transform, false)); // Instantiates an equivalent atom to the original
                    moreEquivalentAtomElements.Add(equivalentAtomElements[i]); // Adds the atomElement for the equivalent atom
                    moreEquivalentAtomObjects[equivalentCount].transform.localPosition = equivalentPosition; // Sets the equivalent position
                    SetAtomColor(moreEquivalentAtomObjects[equivalentCount], moreEquivalentAtomElements[equivalentCount]); // Sets the atom's color based on its element
                    moreEquivalentAtomObjects[equivalentCount].name = moreEquivalentAtomElements[equivalentCount] + " " + equivalentPosition; // Names the atom so they are easier to distinguish in the Unity Editor

                    Debug.Log("2nd created " + equivalentPosition + " from " + equivalentAtomObjects[i].transform.localPosition);
                    equivalentCount++;
                }
            }
        }

        // Solves the case where three coordinates are zero (origin)
        List<GameObject> evenMoreEquivalentAtomObjects = new List<GameObject>();
        List<string> evenMoreEquivalentAtomElements = new List<string>();
        equivalentCount = 0;

        for (int i = 0; i < moreEquivalentAtomObjects.Count; i++)
        {

            // Iterates over z
            if (Mathf.Abs(moreEquivalentAtomObjects[i].transform.localPosition[2]) < 0.0001f) // If atom position is approx. 0
            {
                equivalentPosition = moreEquivalentAtomObjects[i].transform.localPosition;
                equivalentPosition[2] = cellLength[2]; // Sets start of cell to end of cell to duplicate other end of cell for equivalent atom

                evenMoreEquivalentAtomObjects.Add(Instantiate(atom, atomParent.transform, false)); // Instantiates an equivalent atom to the original
                evenMoreEquivalentAtomElements.Add(moreEquivalentAtomElements[i]); // Adds the atomElement for the equivalent atom
                evenMoreEquivalentAtomObjects[equivalentCount].transform.localPosition = equivalentPosition; // Sets the equivalent position
                SetAtomColor(evenMoreEquivalentAtomObjects[equivalentCount], evenMoreEquivalentAtomElements[equivalentCount]); // Sets the atom's color based on its element
                evenMoreEquivalentAtomObjects[equivalentCount].name = evenMoreEquivalentAtomElements[equivalentCount] + " " + equivalentPosition; // Names the atom so they are easier to distinguish in the Unity Editor

                Debug.Log("3rd created " + equivalentPosition + " from " + moreEquivalentAtomObjects[i].transform.localPosition);
                equivalentCount++;
            }

        }

        // Adds lists together
        atomObjectsList.AddRange(equivalentAtomObjects);
        atomObjectsList.AddRange(moreEquivalentAtomObjects);
        atomObjectsList.AddRange(evenMoreEquivalentAtomObjects);

        atomElementsList.AddRange(equivalentAtomElements);
        atomElementsList.AddRange(moreEquivalentAtomElements);
        atomElementsList.AddRange(evenMoreEquivalentAtomElements);


        // Destroys duplicate atoms
        for (int i = 0; i < atomObjectsList.Count - 1; i++)
        {
            if (atomObjectsList[i].transform.position == atomObjectsList[i + 1].transform.position) // If position vectors are equal (Vector3 includes approximation)
            {
                Destroy(atomObjectsList[i + 1]); // Destroys atom
                atomObjectsList.RemoveAt(i + 1); // Removes the now destroyed atom from the list
                atomElementsList.RemoveAt(i + 1); // Removes the element so we have track of it
            }
        }

        atomObjects = atomObjectsList.ToArray(); // Converts the atomObjectsList to an array (arrays are better, faster, harder, stronger)
        atomElement = atomElementsList.ToArray(); // Updates the atomElement array to match all our atoms


        // Adds grid lines for the unit cell (Done really dirty, but quicker than thinking out an algorithm)
        {
            GameObject unitCellGrid = new GameObject("Unit Cell Grid"); // Creates an empty GameObject to store gridLines in
            unitCellGrid.transform.parent = crystal.transform; // Sets unitCellGrid as a child of the Crystal
            unitCellGrid.transform.localPosition = new Vector3(0, 0, 0); // Makes sure the unitCellGrid is in the Crystal's (0,0,0)

            GameObject gridLineX = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            gridLineX.transform.parent = unitCellGrid.transform; // Sets the gridLine to a child of the Unit Cell Grid
            gridLineX.transform.localPosition = new Vector3(0, 0, 0); // Makes sure the gridLine is in the unitCellGrid's (0,0,0)
            gridLineX.transform.localScale = new Vector3(0.05f, cellLength[0] / 2, 0.05f);

            GameObject gridLineY = Instantiate(gridLineX, unitCellGrid.transform, false); // Uses gridLineX as a template for Y and Z
            GameObject gridLineZ = Instantiate(gridLineX, unitCellGrid.transform, false);

            gridLineX.name = "X ";
            gridLineY.name = "Y ";
            gridLineZ.name = "Z ";
            gridLineX.transform.Translate(cellLength[0] / 2f, 0, 0);
            gridLineY.transform.Translate(0, 0, cellLength[1] / 2f); // y in crystallography is z in Unity
            gridLineZ.transform.Translate(0, cellLength[2] / 2f, 0); // z in crystallography is y in Unity
            gridLineX.transform.Rotate(0, 0, 90); // NOTE: Rotate() rotates along (z, x, y) not (x, y, z)
            gridLineY.transform.Rotate(90, 0, 0);
            gridLineZ.transform.Rotate(0, 0, 0); // Standard is along vertical axis

            // Rotates gridLines to match cell_angle
            gridLineX.transform.RotateAround(unitCellGrid.transform.position, new Vector3(0, cellLength[2], 0), (cellAngle[2] - 90) / 2); // gamma half
            gridLineY.transform.RotateAround(unitCellGrid.transform.position, new Vector3(0, cellLength[2], 0), (90 - cellAngle[2]) / 2); // gamma half
            gridLineZ.transform.RotateAround(unitCellGrid.transform.position, gridLineX.transform.localPosition, 90 - cellAngle[0]); // alpha. Rotate the z-axis from origin along the new x-axis by "90-alpha" degrees
            gridLineZ.transform.RotateAround(unitCellGrid.transform.position, gridLineY.transform.localPosition, cellAngle[1] - 90); // beta. Rotate the z-axis from origin along the new y-axis by "beta-90" degrees


            // Adds and places gridLines for the other unit cell edges
            GameObject gridLineXA = Instantiate(gridLineX, unitCellGrid.transform, false);
            gridLineXA.transform.localPosition = new Vector3(cellLength[0] / 2f, cellLength[2], 0);
            GameObject gridLineXB = Instantiate(gridLineX, unitCellGrid.transform, false);
            gridLineXB.transform.localPosition = new Vector3(cellLength[0] / 2f, 0, cellLength[1]);
            GameObject gridLineXC = Instantiate(gridLineX, unitCellGrid.transform, false);
            gridLineXC.transform.localPosition = new Vector3(cellLength[0] / 2f, cellLength[2], cellLength[1]);

            GameObject gridLineYA = Instantiate(gridLineY, unitCellGrid.transform, false);
            gridLineYA.transform.localPosition = new Vector3(cellLength[0], 0, cellLength[1] / 2f);
            GameObject gridLineYB = Instantiate(gridLineY, unitCellGrid.transform, false);
            gridLineYB.transform.localPosition = new Vector3(0, cellLength[2], cellLength[1] / 2f);
            GameObject gridLineYC = Instantiate(gridLineY, unitCellGrid.transform, false);
            gridLineYC.transform.localPosition = new Vector3(cellLength[0], cellLength[2], cellLength[1] / 2f);

            GameObject gridLineZA = Instantiate(gridLineZ, unitCellGrid.transform, false);
            gridLineZA.transform.localPosition = new Vector3(cellLength[0], cellLength[2] / 2f, 0);
            GameObject gridLineZB = Instantiate(gridLineZ, unitCellGrid.transform, false);
            gridLineZB.transform.localPosition = new Vector3(0, cellLength[2] / 2f, cellLength[1]);
            GameObject gridLineZC = Instantiate(gridLineZ, unitCellGrid.transform, false);
            gridLineZC.transform.localPosition = new Vector3(cellLength[0], cellLength[2] / 2f, cellLength[1]);
        }
    }

    // Called in Start
    void AddSymmetry()
    {
        // Adds symmetry to the cell.  F d -3 m S

        string[] spaceGroupSymbols = spaceGroup.Split(' '); // Splits SpaceGroup into each symbol
        GameObject symmetryParent = new GameObject("Symmetry"); // Creates an emprty GameObject to contain symmetries
        symmetryParent.transform.parent = crystal.transform; // Sets the symmetryParent as a child of the Crystal
        symmetryParent.transform.localPosition = new Vector3(0, 0, 0); // Sets the symmetryParent to (0,0,0)

        // Lattice Type (Could also compare this with a dictionary)
        switch (spaceGroupSymbols[0])
        {
            case "P":
                // Primitive

                // Atoms in corners

                break;

            case "I":
                // Body centered

                // Atoms in all corners
                // atom in center. (a/2, b/2, c/2)

                break;

            case "F":
                // Face centered

                // Atoms in all corners
                // Atoms on all faces. (a/2, b/2, 0) and all equivalent

                break;

            case "A":
                // Base centered on A faces only

                // Atoms in all corners
                // Atoms on all A faces

                break;

            case "B":
                // Base centered on B faces only

                // Atoms in all corners
                // Atoms on all B faces

                break;

            case "C":
                // Base centered on C faces only

                // Atoms in all corners
                // Atoms on all C faces

                break;

            case "R":
                // Rhombohedral

                // Place atoms in all corners
                // If there is any Rhombohedral stuff, do it (don't think there is)

                break;
            default:
                Debug.Log("Could not determine Lattice type");
                break;
        }

        // WILL NEED TO DETERMINE CRYSTAL SYSTEM TO KNOW WHICH SYMBOL IS WHICH AXIS. Below is for POINT groups. (I hope it is the same for SPACE groups?)
        // By convention the following rules have been adopted to describe point groups.
        // When a rotation axis is followed by a slash and an m, then this mirror is perpendicular to the rotation axis.
        // For orthorhombic systems the three characters describe the symmetry along the three axes, a, b, and c, respectively.
        // For tetragonal, trigonal, and hexagonal type cells, the c axis is unique, and the first symbol in the point group shows the symmetry along the unique axis.
        // In tetragonal systems, the second symbol shows the symmetry along the[100] and[010] directions and the third symbol shows the symmetry along the[110] and[110] directions.
        // In trigonal and hexagonal cells, the second symbol shows the symmetry along[100], [010] and[110], and the third symbol shows symmetry along[210], [120], and[120].
        // In rhombohedral systems on rhombohedral axes, the first symbol shows symmetry along[111], and the second symbol shows symmetry along[110], [011], and[101].
        // Cubic symbols show[100], [010], [001] in the first symbol, [111], [111], [111], [111] in the second symbol and[110], [110], [011], [011], [101], and[101] in the third symbol.

        // Screw axes and Glide planes

        List<GameObject> symmetryElement = new List<GameObject>();

        for (int i = 1; i < spaceGroupSymbols.Length; i++) // Iterates over the symbols, but skips the Lattice symbol
        {
            try
            {
                int axisSymbol = int.Parse(spaceGroupSymbols[i]); // If this works, the element is a screw axis. If it fails, the element is a glide plane
                Debug.Log("This is a screw/rotation axis!: " + spaceGroupSymbols[i]);

                // Determines type of axis
                switch (axisSymbol)
                {
                    case -3:
                        Debug.Log("Oh boy, an inverse three-fold rotation axis?!");
                        break;
                    default:

                        break;
                }

            }
            catch (FormatException)
            {
                if (spaceGroupSymbols[i].Contains("/")) // Finds symmetries such as 4/m
                {
                    Debug.Log("This is a screw/rotation axis perpendicular to a plane!: " + spaceGroupSymbols[i]);
                }
                else if (spaceGroupSymbols[i].Contains("a") || spaceGroupSymbols[i].Contains("b") || spaceGroupSymbols[i].Contains("c") || spaceGroupSymbols[i].Contains("n") || spaceGroupSymbols[i].Contains("d") || spaceGroupSymbols[i].Contains("e") || spaceGroupSymbols[i].Contains("m")) // Finds planes
                {
                    Debug.Log("This is a glide/mirror plane!: " + spaceGroupSymbols[i]);


                    GameObject planeOriginal;
                    GameObject planeTranslated;
                    Vector3[] planeVertices = new Vector3[4] // No size (all vertices in origin) (Default)
                            {
                            new Vector3(0, 0, 0),
                            new Vector3(0, 0, 0),
                            new Vector3(0, 0, 0),
                            new Vector3(0, 0, 0)
                            };
                    Color planeColor = new Color(1, 1, 0, 0.5f); // Transparent Yellow (Default)
                    string planeName = "Symmetry plane"; // Default
                    Vector3 planeNormal = new Vector3(0, 0, 0); // Default

                    //Determintes type of plane
                    switch (spaceGroupSymbols[i])
                    {
                        case "a":
                            // Glide translation along half a
                            {
                                planeName = "a-glide";
                                planeColor = new Color(0, 0, 1, 0.5f); // Transparent blue

                                // AddAnimation(a-glide);
                            }
                            break;
                        case "b":
                            // Glide translation along half b
                            {
                                planeName = "b-glide";
                                planeColor = new Color(0, 0, 1, 0.5f); // Transparent blue

                                // AddAnimation(b-glide);
                            }
                            break;
                        case "c":
                            // Glide translation along half c
                            {
                                planeName = "c-glide";
                                planeColor = new Color(0, 0, 1, 0.5f); // Transparent blue

                                // AddAnimation(c-glide);
                            }
                            break;
                        case "n":
                            // Glide translation along half of a face diagonal
                            // (if plane is perpendicular to x, slide along y and z by 1/2)
                            {
                                planeName = "n-glide";
                                planeColor = new Color(0, 1, 0, 0.5f); // Transparent green

                                // AddAnimation(n-glide);
                            }
                            break;
                        case "d":
                            // Glide translation along quarter of a face diagonal
                            // (if plane is perpendicular to x, slide along y and z by 1/4)
                            {
                                planeName = "d-glide";
                                planeColor = new Color(112 / 255f, 209 / 255f, 244 / 255f, 0.5f); // Transparent "Ford Diamond Blue"

                                // AddAnimation(d-glide);

                            }
                            break;
                        case "e":
                            // Two glides with the same glide plane and translation along two (different) half lattice-vectors (e.g. a and b)
                            {
                                planeName = "e-glide";
                                planeColor = new Color(1, 0, 1, 0.5f); // Transparent magenta

                                // AddAnimation(e-glide);
                            }
                            break;
                        case "m":
                            // Normal Mirror plane
                            // along axis corresponding to i (i=1 -> x, i=2 -> y, i=3 -> z) I THINK. Could also be others maybe, depending on higher-order axes and stuff..?
                            {
                                planeName = "Mirror";
                                planeColor = new Color(1, 1, 0, 0.5f); // Transparent Yellow

                                // AddAnimation(mirror);
                            }
                            break;
                        default:
                            Debug.Log(spaceGroupSymbols[i] + " has been filtered to be a plane, so how I didn't recognize this is a mystery...");
                            planeName = ("Not recognized: " + spaceGroupSymbols[i]);
                            break;
                    }

                    // Constructs the vertices of the plane depending on if it is perpendicular to x, y or z
                    switch (i)
                    {
                        case 1: // X
                            planeVertices[0] = new Vector3(0, 0, 0); // Origin
                            planeVertices[1] = bravaisVectors[1]; // vec(b)
                            planeVertices[2] = bravaisVectors[2]; // vec(c)
                            planeVertices[3] = bravaisVectors[1] + bravaisVectors[2]; // vec(b)+vec(c)
                            planeName += " X";
                            planeNormal = bravaisVectors[0];
                            break;
                        case 2: // Y
                            planeVertices[0] = new Vector3(0, 0, 0); // Origin
                            planeVertices[1] = bravaisVectors[0]; // vec(a)
                            planeVertices[2] = bravaisVectors[2]; // vec(c)
                            planeVertices[3] = bravaisVectors[0] + bravaisVectors[2]; // vec(a)+vec(c)
                            planeName += " Y";
                            planeNormal = bravaisVectors[1];
                            break;
                        case 3: // Z
                            planeVertices[0] = new Vector3(0, 0, 0); // Origin
                            planeVertices[1] = bravaisVectors[0]; // vec(a)
                            planeVertices[2] = bravaisVectors[1]; // vec(b)
                            planeVertices[3] = bravaisVectors[0] + bravaisVectors[1]; // vec(a)+vec(b)
                            planeName += " Z";
                            planeNormal = bravaisVectors[2];
                            break;
                        default:
                            Debug.Log("Could not determine the direction of the plane: " + spaceGroupSymbols[i]);
                            break;
                    }

                    planeOriginal = CreatePlane(planeVertices, planeColor, planeName, symmetryParent);

                    planeTranslated = Instantiate(planeOriginal, symmetryParent.transform, false); // Creates a mirror for the other end of the cell
                    planeTranslated.transform.localPosition += planeNormal; // Moves the copy to the other end of the cell
                    planeTranslated.name = planeName + " (Translated)"; // Adds name to distinguish original and translated plane
                }
                else // If the symmetry was not recognized, print it to the log (could be an extra symbol from the cif that is not part of H-M)
                {
                    Debug.Log("Could not recognize " + spaceGroupSymbols[i] + " as a symmetry element)");
                }

            }
        }

        // After iterating over the symmetries

    }

    // Old Setup, using ConvertCiftoXYZ, ReadXYZ, CreateCell and AddSymmetry as base
    /*
    infile = CrystalManager.infile;
    if (infile == null)
    {
        infile = @"C:\Users\erlen\Documents\Github\Crystallographic-Reality\files\Si.cif";
    }
    bool convertFile = true; // Specifies that the user wishes to convert their .cif to a .xyz automatically by the program

    if (!Directory.Exists(Application.persistentDataPath + @"\cif2cell_convert\")) // If the cif2cell_convert folder does not exist in the persistentDataPath
    {
        Directory.CreateDirectory(Application.persistentDataPath + @"\cif2cell_convert"); // Create cif2cell_convert folder
    }
    if (!File.Exists(Application.persistentDataPath + @"\cif2cell")) // If cif2cell does not exist in the persistentDataPath
    {
        File.Copy(Application.dataPath + @"\Scripts\cif2cell", Application.persistentDataPath + @"\cif2cell"); // Copy cif2cell from Assets/Scipts to the persistentDataPath in AppData
    }


    if (convertFile)
    {
        ConvertCifToXYZ(infile); // Converts .cif-file to .xyz-file using cif2cell (uses --no-reduce to get the conventional cell and not the primitive cell. This could maybe be changed by the user later)
    }
    ReadXYZ(Application.persistentDataPath + @"\cif2cell_convert\" + Path.GetFileNameWithoutExtension(infile) + ".xyz"); // Reads converted .xyz-file (Could've had Convert_cif return file path to have this cleaner)

    // Sets up Lattice Vectors in relation to Unity's coordinate system
    cellVectors = new Vector3[3] // Got help from https://en.wikipedia.org/wiki/Fractional_coordinates (Remember Unity uses (x,z,y), but we use (x,y,z) )
    {
        new Vector3(cellLength[0], cellLength[2] * Mathf.Cos(cellAngle[1] * Mathf.Deg2Rad), cellLength[1] * Mathf.Cos(cellAngle[2] * Mathf.Deg2Rad)), // a_vec
        new Vector3(0, cellLength[2] * ( ( Mathf.Cos(cellAngle[0] * Mathf.Deg2Rad) - Mathf.Cos(cellAngle[1] * Mathf.Deg2Rad)*Mathf.Cos(cellAngle[2] * Mathf.Deg2Rad) ) / Mathf.Sin(cellAngle[2] * Mathf.Deg2Rad)), cellLength[1] * Mathf.Sin(cellAngle[2] * Mathf.Deg2Rad)), // b_vec
        new Vector3(0, ( cellVolume / ( cellLength[0] * cellLength[1] * Mathf.Sin(cellAngle[2] * Mathf.Deg2Rad) ) ), 0) // c_vec
    };

    CreateCell();
    AddSymmetry();
    */
}
