using UnityEngine;
using DesignerObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DesignerAssets
{

    /// <summary>
    /// 
    /// The below is a general description for the uav string configuration and layout.
    /// 
    ///              J K L M N O P 
    ///                    z
    /// 
    ///                    |        forward     P
    ///                    |                    O
    ///                    |                    N
    ///              - - - - - - -         x    M
    ///                    |                    L
    ///                    |                    K
    ///                    |                    J
    ///                    
    /// 
    /// example string : *aMM0+++++*bNM2+++*cMN1+++*dLM2+++*eML1+++^ab^ac^ad^ae,5,3
    /// 
    /// component : *bNM2+++ : b:node id, N : x position, M : z position, 2 component type, +++ size
    ///             component types = 0 : structure, 1 Motor CW, 2 : Motor CCW, 3 : Foil, 4 : Empty  
    /// ^ab : edge : first char is the starting node id and the second char is the ending node id
    /// ,5,3 : capacity in pounds and the controler index
    /// 
    /// </summary>
    public class UAVDesigner : MonoBehaviour
    {


        // input and output settings
        // will clean this up once final softwrae requirments are set
        private string defaultConfig = "*aMM0+++++*bNM2+++*cMN1+++*dLM2+++*eML1+++^ab^ac^ad^ae,5,3";
        public string CurrentConfig;
        private bool runAsService = false; 

        // variables to support analysis
        public string result = UAVPhysics.FAILURE;
        public string payload = "2";
        private string oldpayload = "2";
        public float cost = 0;
        public float velocity = 0;
        public float distance = 0;
        public float sampleInterval = 0.2f;
        private float simTime = 0;

        // map stores the characteristics of a joint to a graphical object
        private Dictionary<GameObject, JointInfo> jointGraph = new Dictionary<GameObject, JointInfo>();

        // map stores the characteristics of a connection to a graphical object
        private Dictionary<GameObject, ConnectorInfo> connectionGraph = new Dictionary<GameObject, ConnectorInfo>();

        // maps the handle used to create a connection (multiple handles can create the same connection, if the connection is deleted the appropriate handle should be visible
        private Dictionary<GameObject, GameObject> jointHandleToConnection = new Dictionary<GameObject, GameObject>();

        // sizes of the objects
        private float connectionSize = 10f;
        private float jointSize = 2.0f;

        // list object to store handles for assembly mode
        private List<GameObject> inactivehandles = new List<GameObject>();

        // store intersections to restrict overlapping geometries
        private List<Vector3> intersections = new List<Vector3>();

        /// <summary>
        /// store the local evaluation physics calculation class
        /// </summary>
        private UAVPhysics physics = null;


        // run an an executable for batch processing
        // bool executable = false;

        // flag to evaluate a design
        public bool prototypeMode = false;
        public bool evaluating = false;

        public List<float[]> trajectory = new List<float[]>();

        // string to display to a user
        private string lastRun = "";

        private bool valid = true;

        private string designtag = "tag";

        private Dictionary<string, int> vehicleselection = new Dictionary<string, int>();

        // string constants
        public static string VEHICLECOMPONENT = "rb";
        public static string PROTOTYPESTRUCTURE = "protostructure";
        public static string PROTOTYPEWIDESTRUCTURE = "protowidestructure";
        public static string PROTOTYPENARROWSTRUCTURE = "protonarrowstructure";
        public static string PROTOTYPEMOTORCCW = "protomotorccw";
        public static string PROTOTYPEMOTORCW = "protomotorcw";
        public static string PROTOTYPECONNECTION = "protoconnection";
        public static string PROTOTYPEFOIL = "protofoil";
        public static string JOINT = "joint";
        public static string SIZELABEL = "sizelabel";
        public static string CONNECTION = "connection";
        public static string STRUCTURE = "structure";
        public static string WIDESTRUCTURE = "widestructure";
        public static string NARROWSTRUCTURE = "narrowstructure";
        public static string MOTORCCW = "motorccw";
        public static string MOTORCW = "motorcw";
        public static string FOIL = "foil";
        public static string SPINNERCW = "spinnercw";
        public static string SPINNERCCW = "spinnerccw";
        public static string POSITIVEX = "posx";
        public static string NEGATIVEX = "negx";
        public static string POSITIVEZ = "posz";
        public static string NEGATIVEZ = "negz";
        public static string POSITIVEY = "posy";
        public static string NEGATIVEY = "negy";
        public static string AI = "ai";

        public static Boolean returnTrajectory = false;

        // Use this for initialization
        void Start()
        {

            //Debug.Log("start of UAVDesigner");

            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                Debug.Log("ARG = " + args[i]);
                if (args[i] == "-configuration")
                {
                    defaultConfig = args[i + 1];
                }
                else if (args[i] == "-trajectory")
                {                    
                    returnTrajectory = true;
                }
            }

            Application.targetFrameRate = 30;

            Screen.fullScreen = false;

            // ResetView();
            Initialize();
            // This will simulate the default configuration

            CurrentConfig = defaultConfig;

            if(!runAsService)
                fromstring(CurrentConfig);

        }

        /// <summary>
        /// 
        /// Initializes the scene
        /// 
        /// </summary>
        void Initialize()
        {

            JointInfo.counter = 0;

            // reset and destroy all joint, components, and component labels
            foreach (GameObject joint in jointGraph.Keys)
            {
                if (jointGraph[joint] != null)
                    if (jointGraph[joint].gameObj != null)
                        Destroy(jointGraph[joint].gameObj);
                if (jointGraph[joint].textLabel != null)
                    Destroy(jointGraph[joint].textLabel);
                Destroy(joint);
            }
            foreach (GameObject connection in connectionGraph.Keys)
            {
                if (connection != null)
                    Destroy(connection);
            }

            // set the initial base joint
            addStartingJoint();

        }

        /// <summary>
        /// 
        /// Resets the componet and connection dictionaries and adds a base joint
        /// 
        /// </summary>
        private void addStartingJoint()
        {
            // reset all dictionaries
            jointGraph = new Dictionary<GameObject, JointInfo>();
            connectionGraph = new Dictionary<GameObject, ConnectorInfo>();
            jointHandleToConnection = new Dictionary<GameObject, GameObject>();
            intersections = new List<Vector3>();

            // reset and destroy all existing graph objects used to create a UAV
            JointInfo.counter = 0;

            // add initital joint
            Vector3 pos = new Vector3(0f, 0f, 0f);
            intersections.Add(pos);
            GameObject basejoint = Instantiate(GameObject.Find(JOINT), pos, Quaternion.identity) as GameObject;
            jointGraph.Add(basejoint, new JointInfo(JointInfo.UAVComponentType.None, 0, 0, 0, null, null));
            jointGraph[basejoint].locked = true;

        }

        /// <summary>
        /// 
        /// Ends the operational capabilities analysis
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="distance"></param>
        /// <param name="velocity"></param>
        /// <param name="cost"></param>
        public void endPrototype(string msg, float distance, float velocity, float cost)
        {

            result = msg;
            this.distance = distance;
            this.velocity = velocity;
            this.cost = cost;
            // payload will have already been set

            removePrototype();

            // this should not be necessary
            // prototypeMode = false;
            // but this is - it will let the rpc impl know that we are done
            evaluating = false;
            // ResetView();
            // GUIAssets.PopupButton.ok = false;
            // GUIAssets.PopupButton.msg = "";
            int capacity = getCapacity(defaultConfig);

            //System.IO.File.Delete("results.txt");
            //System.IO.File.WriteAllText("results.txt", defaultConfig + ";" + distance + ";" + capacity + ";" + cost + ";" + velocity);
            Application.Quit();

        }

        /// <summary>
        /// 
        /// Physics calculations occur in the FixedUpdate method
        /// 
        /// </summary>checkPopupCache
        void FixedUpdate()
        {
            if (evaluating)
                FixedUpdatePrototype();
        }


        // physics calculations should occur in the Fixed Update methods
        void FixedUpdatePrototype()
        {

            simTime += Time.fixedDeltaTime;
            // if physics is not set , no structure in the uav
            if (physics == null)
            {
                endPrototype("nostructure", -1, -1, -1);
            }

            physics.AddAutoPilot();
            physics.AddMotorAndFoilForce();

            if ((simTime % sampleInterval)<= Time.fixedDeltaTime)
            {
                Vector3 pos = physics.getPosition();
                Quaternion ori = physics.getOrientation();
                trajectory.Add(new float[] { simTime, pos.x, pos.y, pos.z, ori.x, ori.y, ori.z, ori.w });
                //Debug.Log("Sample time: " + simTime + " pos: " + pos + " ori: " + ori);
            }

            if (physics.analysisEnded)
            {
                endPrototype(physics.resultMsg, physics.range, physics.velocity, cost);
                Debug.Log("RESULTS");
                Debug.Log(result + " " + physics.range + " " + physics.velocity + " " + cost);
                if(returnTrajectory)
                {
                    foreach (float[] tr in trajectory)
                    {
                        Debug.Log(String.Join(" ", tr));
                    }
                }
            }
            

        }


        /// <summary>
        /// 
        /// Handle all key and mouse listeners
        /// 
        /// </summary>
        void Update(){
            if (prototypeMode && !evaluating && runAsService)
            {
                fromstring(CurrentConfig);
            }
        }

        /// <summary>
        /// 
        /// Changes the component at a joint to a specific type
        /// 
        /// </summary>
        /// <param name="joint">Unity game object of a joint</param>
        /// <param name="compType">component type</param>
        private string changeJointToComponent(GameObject joint, JointInfo.UAVComponentType compType)
        {

            string typeAction = "";

            // get previous size
            int previousSize = jointGraph[joint].sizedata;
            jointGraph[joint].sizedata = 0;

            // remove component at the joint
            if (jointGraph[joint].gameObj != null)
                Destroy(jointGraph[joint].gameObj);

            // get new position
            Vector3 pos = joint.transform.position;
            Vector3 newPos = new Vector3(pos.x, pos.y, pos.z);


            // add component at the joint position
            // steps : clone the gameobject in unity, rotate if needed, and update the jointgraph object
            if (compType.Equals(JointInfo.UAVComponentType.Structure))
            {

                GameObject pickedObject = GameObject.Find(STRUCTURE);
                GameObject childObject = Instantiate(pickedObject.gameObject, newPos, Quaternion.identity) as GameObject;
                childObject.transform.Rotate(new Vector3(0, 1, 0), 45f);
                childObject.tag = VEHICLECOMPONENT;
                jointGraph[joint].gameObj = childObject;
                jointGraph[joint].componentType = JointInfo.UAVComponentType.Structure;
                sizeComponent(joint, previousSize);
                jointGraph[joint].setTextLabel();
                typeAction = "ToggleStructure";

            }
            else if (compType.Equals(JointInfo.UAVComponentType.MotorCCW))
            {

                GameObject pickedObject = GameObject.Find(MOTORCCW);
                GameObject childObject = Instantiate(pickedObject.gameObject, newPos, Quaternion.identity) as GameObject;
                childObject.tag = VEHICLECOMPONENT;
                jointGraph[joint].gameObj = childObject;
                jointGraph[joint].componentType = JointInfo.UAVComponentType.MotorCCW;
                sizeComponent(joint, previousSize);
                jointGraph[joint].setTextLabel();
                typeAction = "ToggleCCWMotor";

            }
            else if (compType.Equals(JointInfo.UAVComponentType.MotorCW))
            {

                GameObject pickedObject = GameObject.Find(MOTORCW);
                GameObject childObject = Instantiate(pickedObject.gameObject, newPos, Quaternion.identity) as GameObject;
                childObject.tag = VEHICLECOMPONENT;
                jointGraph[joint].gameObj = childObject;
                jointGraph[joint].componentType = JointInfo.UAVComponentType.MotorCW;
                sizeComponent(joint, previousSize);
                jointGraph[joint].setTextLabel();
                typeAction = "ToggleCWMotor";

            }
            else if (compType.Equals(JointInfo.UAVComponentType.Foil))
            {

                GameObject pickedObject = GameObject.Find(FOIL);
                GameObject childObject = Instantiate(pickedObject.gameObject, newPos, Quaternion.identity) as GameObject;
                childObject.transform.Rotate(new Vector3(0, 1, 0), 45);
                childObject.transform.Rotate(new Vector3(1, 0, 0), -10);
                childObject.tag = VEHICLECOMPONENT;
                jointGraph[joint].gameObj = childObject;
                jointGraph[joint].componentType = JointInfo.UAVComponentType.Foil;
                sizeComponent(joint, previousSize);
                jointGraph[joint].setTextLabel();
                typeAction = "ToggleFoil";

            }
            else if (compType.Equals(JointInfo.UAVComponentType.None))
            {

                jointGraph[joint].gameObj = null;
                jointGraph[joint].componentType = JointInfo.UAVComponentType.None;
                jointGraph[joint].sizedata = previousSize;
                jointGraph[joint].setTextLabel();
                typeAction = "ToggleEmpty";

            }

            return typeAction;

        }

        /// <summary>
        /// 
        /// Adds a connector at the selected handle
        /// 
        /// </summary>
        /// <param name="selected"></param>
        /// <returns></returns>
        private string addConnectorAtHandle(GameObject selected)
        {
            Vector3 pos = selected.transform.position;

            if (selected.name.Equals(POSITIVEZ))
            {
                Vector3 endPoint = new Vector3(pos.x, pos.y, -jointSize / 2.0f + pos.z + connectionSize);
                Vector3 startPoint = new Vector3(pos.x, pos.y, -jointSize / 2.0f + pos.z);
                addConnector(selected, endPoint, startPoint, 0f, 0f, 1f);
            }

            if (selected.name.Equals(POSITIVEX))
            {
                Vector3 endPoint = new Vector3(-jointSize / 2.0f + pos.x + connectionSize, pos.y, pos.z);
                Vector3 startPoint = new Vector3(-jointSize / 2.0f + pos.x, pos.y, pos.z);
                addConnector(selected, endPoint, startPoint, 90f, 0f, 1f);
            }

            if (selected.name.Equals(NEGATIVEZ))
            {
                Vector3 endPoint = new Vector3(pos.x, pos.y, jointSize / 2.0f + pos.z - connectionSize);
                Vector3 startPoint = new Vector3(pos.x, pos.y, jointSize / 2.0f + pos.z);
                addConnector(selected, endPoint, startPoint, 180f, 0f, 1f);
            }

            if (selected.name.Equals(NEGATIVEX))
            {
                Vector3 endPoint = new Vector3(jointSize / 2.0f + pos.x - connectionSize, pos.y, pos.z);
                Vector3 startPoint = new Vector3(jointSize / 2.0f + pos.x, pos.y, pos.z);
                addConnector(selected, endPoint, startPoint, -90f, 0f, 1f);
            }

            return "AssemblyHandle";

        }

        /// <summary>
        /// 
        /// Code for left click selection on joints (toggle structure, foil, motors at joints) 
        /// and joint handles for connections (assembly process)
        /// 
        /// </summary>
        /// <param name="selected"></param>
        private string leftClickSelected(GameObject selected)
        {

            string typeAction = "NoEvent";

            // check for left click on joint handle
            if (selected.name.StartsWith(JOINT))
            {
                JointInfo.UAVComponentType componentType = JointInfo.getNextComponentType(jointGraph[selected].componentType);
                changeJointToComponent(selected, componentType);
            }
            // check for left click on assembly handle
            else if (selected.name.Equals(POSITIVEZ) ||
                selected.name.Equals(NEGATIVEZ) ||
                selected.name.Equals(POSITIVEX) ||
                selected.name.Equals(NEGATIVEX))
            {
                typeAction = addConnectorAtHandle(selected);
            }

            return typeAction;


        }

        /// <summary>
        /// 
        /// Scale up the component at the selected joint
        /// 
        /// </summary>
        /// <param name="selected"></param>
        private void scaleUpComponentAtJoint(GameObject selected)
        {

            if (selected.name.StartsWith(JOINT) && jointGraph[selected].gameObj != null)
            {

                Vector3 scale = jointGraph[selected].gameObj.transform.gameObject.transform.localScale;
                float increment = (scale.x + 0.25f) / scale.x;
                jointGraph[selected].gameObj.transform.gameObject.transform.localScale = new Vector3(increment * scale.x, increment * scale.y, increment * scale.z);
                jointGraph[selected].sizedata = jointGraph[selected].sizedata + 1;
                jointGraph[selected].setTextLabel();

            }

        }

        /// <summary>
        /// 
        /// Scale down the component at the selected joint
        /// 
        /// </summary>
        /// <param name="selected"></param>
        private void scaleDownComponentAtJoint(GameObject selected)
        {
            if (selected.name.StartsWith(JOINT) && jointGraph[selected].gameObj != null)
            {

                Vector3 scale = jointGraph[selected].gameObj.transform.gameObject.transform.localScale;
                if (scale.x > 0.8)
                {
                    float increment = (scale.x - 0.25f) / scale.x;
                    jointGraph[selected].gameObj.transform.gameObject.transform.localScale = new Vector3(increment * scale.x, increment * scale.y, increment * scale.z);
                    jointGraph[selected].sizedata = jointGraph[selected].sizedata - 1;
                    jointGraph[selected].setTextLabel();

                }
            }

        }

        /// <summary>
        /// 
        /// Adds connector at the handle connection point
        /// 
        /// </summary>
        /// <param name="selected"></param>
        /// <param name="endPoint"></param>
        /// <param name="startPoint"></param>
        /// <param name="rotateAngle"></param>
        private void addConnector(GameObject selected, Vector3 endPoint, Vector3 startPoint, float rotateAngleY, float rotateAngleX, float connectorScale)
        {


            Vector3 pos = selected.transform.position;
            Vector3 center = new Vector3((startPoint.x + endPoint.x) / 2.0f, (startPoint.y + endPoint.y) / 2.0f, (startPoint.z + endPoint.z) / 2.0f);

            // check for existing joint at the endpoint for cyclic assembly
            bool jointAtEndingLocation = false;
            foreach (GameObject joint in jointGraph.Keys)
            {
                //Vector3 v = jointGraph[joint].endPoint;
                //Debug.Log(v.x + " " + v.z + " " + jointGraph[joint].x + " " + jointGraph[joint].z);
                jointAtEndingLocation = jointAtEndingLocation
                    || (System.Math.Abs(connectionSize * jointGraph[joint].x - endPoint.x) < 0.1
                    && System.Math.Abs(connectionSize * jointGraph[joint].z - endPoint.z) < 0.1);
            }

            // add joint if no existing joint is at the end point
            // this is where joints are added to the assembly
            if (!jointAtEndingLocation)
            {

                GameObject joint = GameObject.Find(JOINT);
                GameObject component = Instantiate(joint, endPoint, Quaternion.identity) as GameObject;

                JointInfo jointInfo = new JointInfo(JointInfo.UAVComponentType.None, 0, endPoint.x / 10.0f, endPoint.z / 10.0f, null, null);
                jointGraph.Add(component, jointInfo);
                jointInfo.sizedata = 0;

            }

            // check for existing connections at the same location
            bool connectionIntersection = false;
            foreach (Vector3 v in intersections)
                connectionIntersection = connectionIntersection
                    || (System.Math.Abs(v.x - center.x) < 0.1
                    && System.Math.Abs(v.y - center.y) < 0.1
                    && System.Math.Abs(v.z - center.z) < 0.1);


            // if no existing connections at this location (this should always be true) 
            // but just used as a double check
            if (!connectionIntersection)
            {

                // add new connector and rotate connector
                GameObject connection = GameObject.Find(CONNECTION);
                GameObject connectionObject = Instantiate(connection, center, Quaternion.identity) as GameObject;
                connectionObject.tag = VEHICLECOMPONENT;
                connectionObject.transform.Rotate(new Vector3(0, 1, 0), rotateAngleY);
                connectionObject.transform.Rotate(new Vector3(1, 0, 0), rotateAngleX);

                // scale the connector
                Vector3 lScale = connectionObject.transform.localScale;
                lScale.z = connectorScale * lScale.z;
                connectionObject.transform.localScale = lScale;

                // hide gui handle and attach it to the connection object in the dictionary
                jointHandleToConnection[selected] = connectionObject;
                selected.SetActive(false);
                intersections.Add(center);

                int indexa = -1;
                int indexb = -1;

                // find starting and ending joint indices nd create a connection
                foreach (GameObject key in jointGraph.Keys)
                {
                    if (System.Math.Abs(jointGraph[key].x - (startPoint.x / 10f)) < 0.01
                        && System.Math.Abs(jointGraph[key].z - (startPoint.z / 10f)) < 0.01)
                    {
                        indexa = jointGraph[key].index;
                    }
                    if (System.Math.Abs(jointGraph[key].x - (endPoint.x / 10f)) < 0.01
                        && System.Math.Abs(jointGraph[key].z - (endPoint.z / 10f)) < 0.01)
                    {
                        indexb = jointGraph[key].index;
                    }
                }
                connectionGraph.Add(connectionObject, new ConnectorInfo(indexa, indexb, !jointAtEndingLocation));

            }

        }


        /// <summary>
        /// 
        /// Gets the Unity game object by char , to support fromstring where 
        /// edges are identify by starting char and ending char
        /// 
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private GameObject getJointByChar(char c)
        {
            GameObject selected = null;
            int index = JointInfo.getNodeIndexByChar(c);
            foreach (GameObject key in jointGraph.Keys)
            {
                if (jointGraph[key].index == index)
                    selected = key;
            }
            return selected;
        }

        /// <summary>
        /// 
        /// for a node, a connection is added to that node based on a positive or negative x or z dir flag
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="type"></param>
        private void assemble(char jointid, string type)
        {

            GameObject selected = getJointByChar(jointid);
            if (selected != null)
                for (int j = 0; j < selected.transform.childCount; j++)
                    // handle names are posx, negx, posz, negz
                    if (selected.transform.GetChild(j).gameObject.name.Equals(type))
                        if (selected.transform.GetChild(j).gameObject.activeSelf)
                        {
                            leftClickSelected(selected.transform.GetChild(j).gameObject);
                            return;
                        }

            // assembly operation failed
            //Debug.Log("Assembly operation failed for " + jointid + " " + type);

        }


        /// <summary>
        /// 
        /// Sizes the component at a joint
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="type"></param>
        private void sizeComponent(GameObject selected, int size)
        {

            // this should never happen, but a check
            if (jointGraph[selected].gameObj == null)
                return;

            // add a counter to prevent an infinite look if there is any issue
            int counter = 0;
            while ((int)jointGraph[selected].sizedata < size
                && counter < 100)
            {
                scaleUpComponentAtJoint(selected);
                counter++;
            }
            while ((int)jointGraph[selected].sizedata > size
                && counter < 100)
            {
                scaleDownComponentAtJoint(selected);
                counter++;
            }
        }


        /// <summary>
        /// 
        /// makes physical prototype to evaluate its operational capabilitiy
        /// 
        /// </summary>
        private void makePrototype()
        {

            // increase the time scale to speed up the anlysis
            Time.timeScale = 20.0f;
            Time.fixedDeltaTime = 0.02f;
            //Time.timeScale = 1.0f;
            //Time.timeScale = 10.0f;

            // update GUI variables
            // prototypeMode = true;
            // evaluating = true;
            UavCollision.hit = false;

            // create physical prototype
            // make the prototype
            QuadrantMotorVehicleLayout prototype = new QuadrantMotorVehicleLayout();
            bool valid = prototype.makePrototype(payload);
            // check for a valid design
            if (!valid)
            {
                endPrototype("nostructure", -1, -1, -1);
                return;
            }
            else
            {
                physics = new UAVPhysics(prototype);
                cost = prototype.getCost();
            }
            // allow the prototype to be evaluated when it hits FixedUpdate (the physics clock tick)
            evaluating = true;
            prototypeMode = false;

        }

        /// <summary>
        /// 
        /// Removes the real physical uav object by removing all prototype objects from the Unity scene
        /// 
        /// </summary>
        private void removePrototype()
        {
            Time.timeScale = 1.0f;

            GameObject[] objects = GameObject.FindGameObjectsWithTag(PROTOTYPESTRUCTURE);
            foreach (GameObject obj in objects)
            {
                Destroy(obj);
            }

            objects = GameObject.FindGameObjectsWithTag(PROTOTYPEWIDESTRUCTURE);
            foreach (GameObject obj in objects)
            {
                Destroy(obj);
            }

            objects = GameObject.FindGameObjectsWithTag(PROTOTYPENARROWSTRUCTURE);
            foreach (GameObject obj in objects)
            {
                Destroy(obj);
            }

            objects = GameObject.FindGameObjectsWithTag(PROTOTYPEMOTORCCW);
            foreach (GameObject obj in objects)
            {
                Destroy(obj);
            }

            objects = GameObject.FindGameObjectsWithTag(PROTOTYPEMOTORCW);
            foreach (GameObject obj in objects)
            {
                Destroy(obj);
            }

            objects = GameObject.FindGameObjectsWithTag(PROTOTYPECONNECTION);
            foreach (GameObject obj in objects)
            {
                Destroy(obj);
            }

            objects = GameObject.FindGameObjectsWithTag(PROTOTYPEFOIL);
            foreach (GameObject obj in objects)
            {
                Destroy(obj);
            }

            // shouldn't need to do this, but none of the previous objects match what is in the simulation
            objects = GameObject.FindGameObjectsWithTag(VEHICLECOMPONENT);
            foreach (GameObject obj in objects)
            {
                Destroy(obj);
            }

        }

        /// <summary>
        /// 
        /// Generates a UAV design from a string. The entire vehicle is basically rotated 45 degrees,
        /// since existing drones have 45 degree connections. This way increments of 1 in the
        /// x and z connection correspond to a rotated frame of reference by 45 degress, where a vector
        /// of x,z = 1,1 represents the forward direction. This allowed for shorter string representations
        /// for the x and z positions for components.
        /// 
        ///              J K L M N O P 
        ///                    z
        /// 
        ///                    |        forward     P
        ///                    |                    O
        ///                    |                    N
        ///              - - - - - - -         x    M
        ///                    |                    L
        ///                    |                    K
        ///                    |                    J
        ///                    
        /// 
        /// example string : *aMM0+++++*bNM2+++*cMN1+++*dLM2+++*eML1+++^ab^ac^ad^ae,5,3
        /// 
        /// component : *bNM2+++ : b:node id, N : x position, M : z position, 2 component type, +++ size
        ///             component types = 0 : structure, 1 Motor CW, 2 : Motor CCW, 3 : Foil, 4 : Empty  
        /// ^ab : edge : first char is the starting node id and the second char is the endind node id
        /// ,5,3 : capacity in poinds and the controler index
        /// 
        /// 
        /// </summary>
        /// <param name="str"></param>
        public void fromstring(string str)
        {

            try
            {

                // get node and edge tokens
                // ex. *aMM0+++++*bNM2+++*cMN1+++*dLM2+++*eML1+++^ab^ac^ad^ae,5,3
                // node tokens : split string by ^, get first token, then split by *
                // edge tokens : split string by , and get first token, then split by ^ and remove first token 
                string[] nodetokens = str.Split('^')[0].Split(new char[] { '*' }).Skip(1).ToArray();
                string[] edgetokens = str.Split(',')[0].Split('^').Skip(1).ToArray();

                // get capacity
                payload = getCapacity(str) + "";

                // store information about nodes in a dictionary
                // the dictionary is the node name that corresponds to the edge chars
                // and the Joint infor object that included the component type and size
                Dictionary<string, JointInfo> nodes = new Dictionary<string, JointInfo>();
                foreach (string t in nodetokens)
                {
                    JointInfo jointInfo = new JointInfo(t);
                    nodes.Add(jointInfo.getNodeIDAsString(), new JointInfo(t));
                }

                // create the assembly sequence

                // maximum assembly connection index
                int maxConnectionStepIndex = 0;

                // Dictionary that stores the ending node index of an edge with the string repesentation
                // of the edge, want to assemble the vehicle in edge ascending order
                Dictionary<int, string> sortedEdgeConnectionSteps = new Dictionary<int, string>();

                // Since the assembly can be cyclic, these connections will be added to a separate list
                // and appended
                List<string> edgesEndAtExistingComponent = new List<string>();

                // for each edge token
                foreach (string edgetoken in edgetokens)
                {

                    // get the index of the nodes at each end
                    //
                    // example ^ac : firstIndex = 0; secondIndex = 2
                    //
                    int secondIndex = JointInfo.getNodeIndexByChar(edgetoken[1]);
                    int firstIndex = JointInfo.getNodeIndexByChar(edgetoken[0]);

                    // store edge by second index
                    if (!sortedEdgeConnectionSteps.ContainsKey(secondIndex))
                        sortedEdgeConnectionSteps[secondIndex] = edgetoken;
                    else // ending index already exists, so must be a cycle
                        edgesEndAtExistingComponent.Add(edgetoken);

                    // set the maximum second index
                    maxConnectionStepIndex = System.Math.Max(maxConnectionStepIndex, secondIndex);

                }

                // if there are cycles, add to the end of the Dictionary
                foreach (string edgeAtCylce in edgesEndAtExistingComponent)
                {
                    maxConnectionStepIndex += 1;
                    sortedEdgeConnectionSteps[maxConnectionStepIndex] = edgeAtCylce;
                }

                // destroy all joint and component Unity game objects
                foreach (GameObject joint in jointGraph.Keys)
                {
                    try
                    {
                        // destroy the component
                        if (jointGraph[joint].gameObj != null)
                            Destroy(jointGraph[joint].gameObj);
                        // destroy the above text label
                        if (jointGraph[joint].textLabel != null)
                            Destroy(jointGraph[joint].textLabel);
                        // destroy the joint
                        Destroy(joint);
                    }
                    catch (Exception e)
                    {
                        //Debug.Log(e);
                    }
                }

                // destroy all connection Unity game objects
                foreach (GameObject connection in connectionGraph.Keys)
                {
                    try
                    {
                        Destroy(connection);
                    }
                    catch (Exception e)
                    {
                        //Debug.Log(e);
                    }
                }

                // set the base joint
                addStartingJoint();

                // add all connections
                for (int ii = 0; ii < maxConnectionStepIndex + 1; ii++)
                {
                    if (sortedEdgeConnectionSteps.ContainsKey(ii))
                    {

                        // get the edge token, ex "ad"
                        string t = sortedEdgeConnectionSteps[ii];

                        // the first character is the starting joint
                        // the second character is the ending joint
                        string start = "" + t[0];
                        string end = "" + t[1];

                        // get the joint information based on the input configuration string
                        JointInfo startnode = nodes[start];
                        JointInfo endnode = nodes[end];

                        // get the x and z positions for the starting and ending node
                        int xstart = (int)startnode.x;
                        int zstart = (int)startnode.z;
                        int xend = (int)endnode.x;
                        int zend = (int)endnode.z;

                        // get the starting node 
                        char startChar = start[0];

                        // if the starting node has a lower x and the same z , then we have a positive X
                        // connection, te remaining if follow this same algorithm
                        if ((xstart - xend) == -1 && (zstart - zend) == 0)
                        {
                            assemble(startChar, POSITIVEX);
                        }
                        else if ((xstart - xend) == 1 && (zstart - zend) == 0)
                        {
                            assemble(startChar, NEGATIVEX);
                        }
                        else if ((xstart - xend) == 0 && (zstart - zend) == -1)
                        {
                            assemble(startChar, POSITIVEZ);
                        }
                        else if ((xstart - xend) == 0 && (zstart - zend) == 1)
                        {
                            assemble(startChar, NEGATIVEZ);
                        }
                        else
                        {
                            //Debug.Log("Error in file " + t);
                        }

                        // check that the new joint index matches the configuration file, 
                        // deleting and adding of nodes changes the default ordering
                        foreach (GameObject joint in jointGraph.Keys)
                        {

                            // if the joint is at the new connection ending point and it is not locked
                            if ((int)jointGraph[joint].x == xend
                                && (int)jointGraph[joint].z == zend
                                && !jointGraph[joint].locked)
                            {
                                // check that the string representing the index in the configuration is the same joint indx
                                if (!end.Equals("" + JointInfo.nodeIdChars[jointGraph[joint].index]))
                                {
                                    int index = JointInfo.getNodeIndexByChar(end[0]);
                                    jointGraph[joint].index = index;
                                    JointInfo.counter = System.Math.Max(index + 1, JointInfo.counter);
                                }
                                jointGraph[joint].locked = true;
                            }

                        }

                    }

                }

                // add components at each joint
                foreach (string obj in nodes.Keys)
                {
                    JointInfo value = nodes[obj];
                    changeJointToComponent(getJointByChar(obj[0]), value.componentType);
                }

                // size components at each joint
                foreach (string obj in nodes.Keys)
                {
                    JointInfo value = nodes[obj];
                    sizeComponent(getJointByChar(obj[0]), value.sizedata);
                }

                // next step is to make the prototype
                makePrototype();

            }
            catch (Exception e)
            {
                //Debug.Log(e);
            }

        }


        /// <summary>
        /// 
        /// parses the vehicle configuration to get the capacity value
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private int getCapacity(string config)
        {
            try
            {
                return (int)double.Parse(config.Split(',')[1]);
            }
            catch (Exception e)
            {
                //Debug.Log(e);
                return -1;
            }
        }

    }


}