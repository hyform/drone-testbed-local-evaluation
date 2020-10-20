using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace DesignerObjects
{

    /// <summary>
    /// Creates a physical prototype of the vehicle
    /// </summary>
    class QuadrantMotorVehicleLayout : MonoBehaviour
    {

        /// <summary>
        /// main structure in the middle of the vehicle
        /// </summary>
        public GameObject mainPrototypeStructure { get; set; }

        /// <summary>
        /// main structure position
        /// </summary>
        public Vector3 mainPrototypeStructurePosition { get; set; }

        /// <summary>
        /// dictionary to key a Unity game object with motor metrics 
        /// </summary>
        public Dictionary<GameObject, MotorData> motors = new Dictionary<GameObject, MotorData>();

        /// <summary>
        /// list of foils in the vehicle
        /// </summary>
        public List<GameObject> foils { get; set; }

        /// <summary>
        /// total structure weight in lb
        /// </summary>
        public float totalStructureWeight { get; set; }

        /// <summary>
        /// total motor weight in lb
        /// </summary>
        public float totalMotorWeight { get; set; }

        /// <summary>
        /// total connection weight in lb
        /// </summary>
        public float totalConnectionWeight { get; set; }

        /// <summary>
        /// total foil weight in lb
        /// </summary>
        public float totalFoilWeight { get; set; }

        /// <summary>
        /// scaling factor of the vehicle
        /// </summary>
        public float scale { get; set; }

        /// <summary>
        /// main constructor to initialize the vehicle
        /// </summary>
        public QuadrantMotorVehicleLayout()
        {
            motors = new Dictionary<GameObject, MotorData>();
            foils = new List<GameObject>();
        }

        /// <summary>
        /// creates a physical prototype
        /// </summary>
        /// <param name="capacity"></param>
        /// <returns></returns>
        public bool makePrototype(string capacity)
        {

            // initialize weights
            totalStructureWeight = 0;
            totalMotorWeight = 0;
            totalConnectionWeight = 0;
            totalFoilWeight = 0;

            float minsize = 1.0f;

            float xMin = 1000;
            float yMin = 1000;
            float zMin = 1000;

            float xMax = -1000;
            float yMax = -1000;
            float zMax = -1000;

            // clear dictionaries and lists
            motors.Clear();
            foils.Clear();

            GameObject[] objects = GameObject.FindGameObjectsWithTag(DesignerAssets.UAVDesigner.VEHICLECOMPONENT);

            // get bounds and sizing
            // this code can be cleaned up using Unity bounds methods
            foreach (GameObject obj in objects)
            {
                Vector3 size = obj.GetComponent<MeshRenderer>().bounds.size;
                minsize = System.Math.Min(minsize, size.x);
                minsize = System.Math.Min(minsize, size.y);
                minsize = System.Math.Min(minsize, size.z);

                xMin = System.Math.Min(xMin, obj.GetComponent<MeshRenderer>().bounds.min.x);
                yMin = System.Math.Min(yMin, obj.GetComponent<MeshRenderer>().bounds.min.y);
                zMin = System.Math.Min(zMin, obj.GetComponent<MeshRenderer>().bounds.min.z);

                xMax = System.Math.Max(xMax, obj.GetComponent<MeshRenderer>().bounds.max.x);
                yMax = System.Math.Max(yMax, obj.GetComponent<MeshRenderer>().bounds.max.y);
                zMax = System.Math.Max(zMax, obj.GetComponent<MeshRenderer>().bounds.max.z);

            }
            scale = 2;

            float xCenter = (xMin + xMax) / 2.0f;
            float yCenter = (yMin + yMax) / 2.0f;
            float zCenter = (zMin + zMax) / 2.0f;

            // center vehicle above the test stand
            float prototypey = 1000f;
            float prototypez = 2000f;

            // set main structure to null
            mainPrototypeStructure = null;

            // copy all rigidbody components
            foreach (GameObject obj in objects)
            {
                Vector3 pos = obj.transform.position;
                Vector3 newPos = new Vector3(scale * pos.x, scale * pos.y + prototypey, scale * pos.z + prototypez);

                GameObject vehicleObject = Instantiate(obj, newPos, obj.transform.rotation) as GameObject;
                Vector3 localScale = vehicleObject.transform.localScale;
                vehicleObject.transform.localScale = new Vector3(scale * localScale.x, scale * localScale.y, scale * localScale.z);
                vehicleObject.AddComponent<Rigidbody>();

                if (obj.name.StartsWith("motorcw"))
                    vehicleObject.tag = DesignerAssets.UAVDesigner.PROTOTYPEMOTORCW;
                if (obj.name.StartsWith("motorccw"))
                    vehicleObject.tag = DesignerAssets.UAVDesigner.PROTOTYPEMOTORCCW;
                if (obj.name.StartsWith("structure"))
                    vehicleObject.tag = DesignerAssets.UAVDesigner.PROTOTYPESTRUCTURE;
                if (obj.name.StartsWith("connection"))
                    vehicleObject.tag = DesignerAssets.UAVDesigner.PROTOTYPECONNECTION;
                if (obj.name.StartsWith("foil"))
                {
                    vehicleObject.tag = DesignerAssets.UAVDesigner.PROTOTYPEFOIL;
                    foils.Add(vehicleObject);
                }

                // assign motors metrics to Unity game objects
                float tolerance = 0.01f;
                if (obj.name.StartsWith("m"))
                {

                    // quadrant
                    MotorData motordata = new MotorData();
                    Vector3 mvector = Quaternion.AngleAxis(45, Vector3.up) * new Vector3(pos.x - xCenter, pos.y - yCenter, pos.z - zCenter);
                    if (mvector.x > tolerance && mvector.z > tolerance)
                        motordata.quadrant = MotorData.FRONTLEFT;
                    else if (mvector.x > tolerance && mvector.z < -tolerance)
                        motordata.quadrant = MotorData.FRONTRIGHT;
                    else if (mvector.x < -tolerance && mvector.z < -tolerance)
                        motordata.quadrant = MotorData.BACKRIGHT;
                    else if (mvector.x < -tolerance && mvector.z > tolerance)
                        motordata.quadrant = MotorData.BACKLEFT;
                    else if (mvector.x > tolerance && mvector.z > -tolerance && mvector.z < tolerance)
                        motordata.quadrant = MotorData.FRONT;
                    else if (mvector.x < -tolerance && mvector.z > -tolerance && mvector.z < tolerance)
                        motordata.quadrant = MotorData.BACK;
                    else if (mvector.z > tolerance && mvector.x > -tolerance && mvector.x < tolerance)
                        motordata.quadrant = MotorData.LEFT;
                    else if (mvector.z < -tolerance && mvector.x > -tolerance && mvector.x < tolerance)
                        motordata.quadrant = MotorData.RIGHT;

                    // motor metrics
                    motordata.distance = (float)System.Math.Sqrt(pos.x * pos.x + pos.z * pos.z);
                    motordata.maximumThrust = getMotorThrust(obj);
                    motors[vehicleObject] = motordata;

                }

                try
                {
                    // assign mass properties for a structure
                    if (obj.name.StartsWith("structure"))
                    {

                        Vector3 size = obj.GetComponent<MeshRenderer>().bounds.size;
                        float vol = size.x * size.y * size.z;

                        // approximate base mass to be near 17.7 lb
                        float structuremass = vol / 20f;

                        vehicleObject.GetComponent<Rigidbody>().mass = structuremass;
                        vehicleObject.GetComponent<Rigidbody>().drag = structuremass / 100;
                        totalStructureWeight += structuremass;

                        bool mainStructure = true;
                        if (mainPrototypeStructure != null)
                        {
                            float distanceA = Vector3.Distance(mainPrototypeStructure.transform.position, new Vector3(0 + xCenter, prototypey + yCenter, prototypez + zCenter));
                            float distanceB = Vector3.Distance(vehicleObject.transform.position, new Vector3(0 + xCenter, prototypey + yCenter, prototypez + zCenter));
                            mainStructure = distanceB < distanceA;
                        }

                        if (mainStructure)
                        {
                            mainPrototypeStructure = vehicleObject;
                            mainPrototypeStructurePosition = mainPrototypeStructure.transform.position;
                        }

                    }

                    // assign mass properties for a motor
                    if (obj.name.StartsWith("m"))
                    {
                        Vector3 size = obj.GetComponent<MeshRenderer>().bounds.size;
                        float vol = size.x * size.y * size.z;

                        // approximate base mass to be near 0.2 kg
                        float motormass = vol / 10f;

                        vehicleObject.GetComponent<Rigidbody>().mass = motormass;
                        vehicleObject.GetComponent<Rigidbody>().drag = motormass / 400;
                        totalMotorWeight += motormass;

                    }

                    // assign mass properties for a connector
                    if (obj.name.StartsWith("c"))
                        vehicleObject.GetComponent<Rigidbody>().mass = 0;

                    // assign mass properties for a foil
                    if (obj.name.StartsWith("f"))
                    {
                        Vector3 size = obj.GetComponent<MeshRenderer>().bounds.size;
                        float vol = size.x * size.y * size.z;

                        // assume base foil of about 2 lb per foot
                        float foilmass = vol / 71.0f;

                        vehicleObject.GetComponent<Rigidbody>().mass = foilmass;
                        vehicleObject.GetComponent<Rigidbody>().drag = foilmass / 400;
                        totalFoilWeight += foilmass;
                    }


                }
                catch (System.Exception e)
                {
                    //Debug.Log(e);
                }


            }

            // no main structure, return error
            if (mainPrototypeStructure == null)
                return false;

            // add capacity
            mainPrototypeStructure.GetComponent<Rigidbody>().mass += float.Parse(capacity);

            // create the assembly
            List<GameObject> connectedObjects = new List<GameObject>();
            connectedObjects.Add(mainPrototypeStructure);

            // add vehicle components to all objects
            List<GameObject> allObjects = new List<GameObject>();
            addAllObjects(allObjects, GameObject.FindGameObjectsWithTag(DesignerAssets.UAVDesigner.PROTOTYPESTRUCTURE));
            addAllObjects(allObjects, GameObject.FindGameObjectsWithTag(DesignerAssets.UAVDesigner.PROTOTYPEMOTORCCW));
            addAllObjects(allObjects, GameObject.FindGameObjectsWithTag(DesignerAssets.UAVDesigner.PROTOTYPEMOTORCW));
            addAllObjects(allObjects, GameObject.FindGameObjectsWithTag(DesignerAssets.UAVDesigner.PROTOTYPECONNECTION));
            addAllObjects(allObjects, GameObject.FindGameObjectsWithTag(DesignerAssets.UAVDesigner.PROTOTYPEFOIL));

            // connect all objects and use the main structure as the base connection
            allObjects.Remove(mainPrototypeStructure);
            attachedObjects(connectedObjects, allObjects);

            return true;

        }

        /// <summary>
        /// adds all objects to the allObjects list
        /// </summary>
        /// <param name="allObjects">allObjects list</param>
        /// <param name="objects">objects to add</param>
        private void addAllObjects(List<GameObject> allObjects, GameObject[] objects)
        {
            foreach (GameObject obj in objects)
                allObjects.Add(obj);
        }

        /// <summary>
        /// connects objects of the vehicle using fixed joints
        /// 
        /// noticed that Unity physics analysis behaves better when the connections
        /// occur with close proximity components, so each component is connected to a close
        /// neighbor
        /// 
        /// </summary>
        /// <param name="connectedObjects">list of connected objects</param>
        /// <param name="allObjects">list of remaining unconnected objects</param>
        private void attachedObjects(List<GameObject> connectedObjects, List<GameObject> allObjects)
        {


            // counter to prevent infinite loop, should never occur
            int counter = 0;

            // while objects are still unconnected
            while (allObjects.Count > 0 && counter < 100)
            {

                GameObject closestUnconnected = null;
                GameObject closestConnected = null;
                float minDistance = 10000000000;

                // get closest object to any other connected object
                foreach (GameObject obj in allObjects)
                {
                    // for each connected object
                    foreach (GameObject objConnect in connectedObjects)
                    {
                        float distanceB = Vector3.Distance(obj.transform.position, objConnect.transform.position);
                        if (distanceB < minDistance && objConnect.GetComponent<Rigidbody>().mass > 0.9f)
                        {
                            closestUnconnected = obj;
                            closestConnected = objConnect;
                            minDistance = distanceB;
                        }
                    }
                }

                // add a fixed joint to connect the closestUnconnected and closestConnected
                FixedJoint joint = closestUnconnected.AddComponent<FixedJoint>();
                joint.connectedBody = closestConnected.GetComponent<Rigidbody>();
                joint.enableCollision = false;
                joint.enablePreprocessing = false;

                // update connected and unconnected lists
                allObjects.Remove(closestUnconnected);
                connectedObjects.Add(closestUnconnected);

                // increment counter
                counter++;

            }

        }

        /// <summary>
        /// gets the maximimum motor power based on game object size 
        /// (low fidelity calculation)
        /// </summary>
        /// <param name="obj">Unity game object of the motor</param>
        /// <returns>maximum thrust</returns>
        private float getMotorThrust(GameObject obj)
        {

            float motorPower = 10f;
            float motorProp = 0.075f;

            Vector3 size = obj.GetComponent<MeshRenderer>().bounds.size;
            float vol = size.x * 1.4f * size.y * size.z;

            return (motorPower + vol / motorProp);

        }

        /// <summary>
        /// gets all motors by quadrant position
        /// </summary>
        /// <param name="quadrant">string representation of the quadrant</param>
        /// <returns>list of Unity game object motors</returns>
        public List<GameObject> getMotors(string quadrant)
        {
            List<GameObject> motorList = new List<GameObject>();
            foreach (GameObject obj in motors.Keys)
                if (motors[obj].quadrant.Equals(quadrant))
                    motorList.Add(obj);
            return motorList;
        }

        /// <summary>
        /// low fidelity energy rules for the vehicle
        ///
        /// https:// pdfs.semanticscholar.org/5d42/ccdc6b5afd0ff8407a389789e1055de84fef.pdf
        /// Amazon Air has with a delivery radius of about 10 miles or range or 20.
        /// Assume our baseline is half of this , looking for a radius range of 5 miles (10 miles total) with 5 lb payload.
        /// Amazon Air baseline assumes 1.5 kWh for the delivery, redesigned to 8 mi would be 0.4kWh
        /// our baseline design is 17.7 lb, assume 5 battery lb is a about a 570 Wh battery
        /// added power of 2 to get a more of an improvement as one increases the size
        /// </summary>
        /// <returns>total battery energy</returns>
        public float getTotalBatteryEnergy()
        {
            return (float)System.Math.Pow(totalStructureWeight / 17.7, 2.0) * 570f;
        }

        /// <summary>
        /// calculates the cost.
        /// assumption : scaled cost around the base design to be above 3000, 
        /// assumption : foils $40 per pound 
        /// assumption : each base foil would be around $80
        /// assumption : controller costs 200 + 50*motorCount
        /// </summary>
        /// <returns></returns>
        public float getCost()
        {
            return (totalStructureWeight + totalMotorWeight + totalConnectionWeight) * 140f + totalFoilWeight * 40 + (200f + 50 * motors.Count);
        }


    }

    /// <summary>
    /// Store motor metrics
    /// </summary>
    public class MotorData
    {
        /// <summary>
        /// distance from the center of the vehicle
        /// </summary>
        public float distance = 0;

        /// <summary>
        /// quadrant location of the vehicle
        /// </summary>
        public string quadrant = "";

        /// <summary>
        /// maximum thrust
        /// </summary>
        public float maximumThrust = 0;

        // quadrant variables
        public static string FRONTRIGHT = "FRONTRIGHT";
        public static string FRONTLEFT = "FRONTLEFT";
        public static string FRONT = "FRONT";
        public static string BACKRIGHT = "BACKRIGHT";
        public static string BACKLEFT = "BACKLEFT";
        public static string BACK = "BACK";
        public static string RIGHT = "RIGHT";
        public static string LEFT = "LEFT";

    }

}
