using System;
using UnityEngine;

namespace DesignerObjects
{

    /// <summary>
    /// 
    /// Stores the component type, size, and underlying data for joint information
    /// 
    /// </summary>
    class JointInfo
    {

        /// <summary>
        /// the Unity Game Object that represents the joint
        /// </summary>
        public GameObject gameObj { get; set; }

        /// <summary>
        /// joint identified uniquely using an integer index
        /// </summary>
        public int index { get; set; }

        /// <summary>
        /// static counter that increments with new instances to assign a unique index 
        /// </summary>
        public static int counter = 0;

        /// <summary>
        /// x position of the joint
        /// </summary>
        public float x { get; set; }

        /// <summary>
        /// z position of the joint
        /// </summary>
        public float z { get; set; }

        /// <summary>
        /// enumeration with the available types of components at each joint
        /// </summary>
        public enum UAVComponentType { Structure, MotorCW, MotorCCW, Foil, None };

        /// <summary>
        /// stores the type of component at the joint
        /// </summary>
        public UAVComponentType componentType { get; set; }

        /// <summary>
        /// array of characters that identify node indices in the grammar string 
        /// integers are used for components in the grammar and it would also limit 
        /// the number of total components in an assembly using 1 char space 
        /// with 10 total components in the assembly (0-9). 
        /// 
        /// the length of the current list of characters should be a safe upper bound to possible
        /// number of components added to an assembly (42). if this application does expand to larger
        /// assemblies, this will need to be modified.
        /// </summary>
        public static char[] nodeIdChars = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '!', '@', '#', '$', '%', '&', '(', ')', '_', '=', '[', ']', '{', '}', '<', '>' };

        /// <summary>
        /// array of characters that identify the positions of nodes in the grammar string, M is the 
        /// central location in both the x and z direction, so users can go about +- 12 connection
        /// lengths in either direction from the central point
        /// </summary>
        public static char[] positionChars = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };

        /// <summary>
        /// locks the index, this is probably removable, but kept it since deleting
        /// and ordering indices is a complex task 
        /// </summary>
        public bool locked { get; set; }

        /// <summary>
        /// stores the size of the object
        /// </summary>
        public int sizedata = 0;

        /// <summary>
        /// stores the text label GameObject above each joint
        /// </summary>
        public GameObject textLabel { get; set; }

        /// <summary>
        /// 
        /// Main constructor
        /// 
        /// </summary>
        /// <param name="type">type of component</param>
        /// <param name="sizedata">size of the component</param>
        /// <param name="x">x position of the component</param>
        /// <param name="z">z position of the component</param>
        /// <param name="obj">Unity game object that represents the joint</param>
        /// <param name="textLabel">Unity Game object that represents the text label above the joint</param>
        public JointInfo(UAVComponentType type, int sizedata,
            float x, float z,
            GameObject obj, GameObject textLabel)
        {
            componentType = type;
            this.sizedata = sizedata;
            this.x = x;
            this.z = z;
            this.gameObj = obj;
            this.textLabel = textLabel;
            this.index = counter;
            setTextLabel();
            counter++;
        }

        /// <summary>
        /// constructor that sets the properties using a string
        /// </summary>
        /// <param name="s">string configuration of the joint (see grammar())</param>
        public JointInfo(string s)
        {
            fromString(s);
            setTextLabel();
        }

        /// <summary>
        /// gets the node identification as a string
        /// </summary>
        /// <returns>one character node string</returns>
        public string getNodeIDAsString()
        {
            return nodeIdChars[index] + "";
        }

        /// <summary>
        /// sets the text of the size label above the component
        /// </summary>
        public void setTextLabel()
        {

            if (textLabel != null)
            {
                if (componentType.Equals(UAVComponentType.None))
                    textLabel.GetComponent<TextMesh>().text = "";
                else if (sizedata == 0)
                    textLabel.GetComponent<TextMesh>().text = "0";
                else if (sizedata > 0)
                    textLabel.GetComponent<TextMesh>().text = "+" + sizedata;
                else
                    textLabel.GetComponent<TextMesh>().text = "-" + sizedata;

            }

        }

        /// <summary>
        /// gets the node index of a character
        /// </summary>
        /// <param name="s">character of the node</param>
        /// <returns></returns>
        public static int getNodeIndexByChar(char s)
        {
            for (int i = 0; i < nodeIdChars.Length; i++)
                if (nodeIdChars[i] == s)
                    return i;
            return -1;
        }

        /// <summary>
        /// gets the string representing the component size. 4 would be ++++ and -2 would be --
        /// </summary>
        /// <param name="sizecomp"></param>
        /// <returns></returns>
        public static string getSizeString(int sizecomp)
        {
            string str = "";
            for (int i = 0; i < Math.Abs(sizecomp); i++)
                str += (sizecomp > 0) ? "+" : "-";
            return str;
        }

        /// <summary>
        /// gets the position character based on x and z indices
        /// </summary>
        /// <param name="positionIndex">position index</param>
        /// <returns></returns>
        public static char getPositionChar(int positionIndex)
        {
            return positionChars[positionIndex + 12];
        }

        /// <summary>
        /// gets the position index by character
        /// </summary>
        /// <param name="c">character representing the position</param>
        /// <returns>position index</returns>
        public static int getPositionIndex(char c)
        {
            int index = -100000;
            for (int i = 0; i < positionChars.Length; i++)
                if (c == positionChars[i])
                    index = i;
            return index - 12;
        }

        /// <summary>
        /// gets the next component type
        /// </summary>
        /// <param name="comptype">current component type</param>
        /// <returns>next component type</returns>
        public static UAVComponentType getNextComponentType(UAVComponentType comptype)
        {
            if (comptype.Equals(UAVComponentType.None))
                return UAVComponentType.Structure;
            else if (comptype.Equals(UAVComponentType.Structure))
                return UAVComponentType.MotorCW;
            else if (comptype.Equals(UAVComponentType.MotorCW))
                return UAVComponentType.MotorCCW;
            else if (comptype.Equals(UAVComponentType.MotorCCW))
                return UAVComponentType.Foil;
            else
                return UAVComponentType.None;
        }

        /// <summary>
        /// gets a string representation of the node to include in the vehicle configuration string
        /// </summary>
        /// <returns>string of format (nodeIdStr)(xpositionChar)(zpositionChar)(componentTypeInt)(sizeStr) 
        /// ex. *bNM2+++</returns>
        public String grammar()
        {
            return "*" + nodeIdChars[index]
                + "" + getPositionChar((int)System.Math.Round(x))
                + "" + getPositionChar((int)System.Math.Round(z))
                + "" + (int)componentType + ""
                + getSizeString(sizedata);
        }

        /// <summary>
        /// sets the joint information using a string
        /// </summary>
        /// <param name="str">string configuration of the joint (see grammar())</param>
        public void fromString(string str)
        {
            try
            {
                // set the index, x, z, and component type
                index = getNodeIndexByChar(str[0]);
                x = getPositionIndex(str[1]);
                z = getPositionIndex(str[2]);
                componentType = (UAVComponentType)System.Int32.Parse("" + str[3]);

                // gets the size data and set the text label
                string sizeStr = str.Substring(4);
                int l = sizeStr.Length;
                if (l > 0)
                    if (sizeStr[0] == '-')
                        l = -l;
                sizedata = l;
                setTextLabel();

            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }


    }
}
