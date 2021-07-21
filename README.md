# Crystallographic Reality
 A VR program designed to visualize and manipulate crystral structures.

## The Dream
An environment that lets you "live" in the unit cell, seeing symmetries as you look around.
The follwing list includes wanted features, some might be doable during summer, some might be done later:
- Show mirrors / inversion points etc.
  - Turn off and on induvidual symmetry elements
  - Point at connected atoms to visualize their symmetry connection
- Symmetry "sticks" on the atom
- Surfaces, polyhedrals, etc.


## Some specific things to do
**Always document the process**
- [ ] Read and convert .cif-files to a physical unit(y) cell
  - [x] Cell_length
    - Read manually
  - [x] Cell_angle
    - Read manually
  - [x] atom_positions
    - Handled by converting .cif to .xyz
  - [ ] Ball-and-Stick model
    - [x] Ball
      - Use Dictionary to change color of the atoms
        - Might need to re-structure so atomElement and atomPos functions together
        No, just instantiate with color change immediately and apply same color for equivalentAtom as the other atom
          - Use more/evenMore then remove the same elements
          - Add a tag to the GameObject when Instantiating (if possible?)
    - [ ] Stick
      - Algorithm to create sticks (heavy work)
      - pdb has sticks. Use pdb instead of xyz?? (easier, but requires making a ConvertCiftoPDB and ReadPDB function)
  - [x] Unit Cell grid
    - Can scale down cylinders to rods (0.05,1,0.05) and place them according to Cell_length (and angle eventually)
    Create a prefab with scaling (and shader?), and Instantiate in CreateCell)() function
- [ ] Create a functioning player, with working hands
  - [ ] Hand assets
    - [ ] Search the web
  - [ ] Scale crystal up/down
    - Figure out XR Interaction Toolkit
    - Scale XR Rig. Gives same effect, but likely easier (and can keep crystal in Å)
  - [ ] Teleport movement
  - [ ] Turn with joystick
- [ ] Add symmetries
  - Create Algorithm
    - [ ] Hermann-Mauguin Space Group
      - [x] Lattice Type (P, I, F) etc.
      - [ ] Screw axes
      - [ ] Glide planes
    - [ ] Convert Space Group to Point Group ??
  - Read directly from cif2cell output and convert symmetry matrices into mirrors and axes
