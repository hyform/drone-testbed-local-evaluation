using System.Collections.Generic;
using UnityEngine;

namespace DesignerObjects
{

    /// <summary>
    /// 
    /// This is currently a low fidelity physics simulation for air vehicles.
    /// The overall approach is to create a base vehicle that has 
    /// approximately half of the capability of an Amazon Air like vehicle.
    /// Then, results such as range, velocity, and cost are scaled using constants 
    /// to get realistic values for the base vehicle. 
    /// 
    /// Future work can focus on modifying the design rules for a more dimensionally
    /// accurate simulation. Improvements would be to add lift/drag 
    /// rules for 3D structures and shapes. Also, one may want to include takeoff, travel, 
    /// and landing phases. The controller rules can be extended or rewritten to stabilize 
    /// nonsymetric vehicle configurations.
    /// 
    /// </summary>
    class UAVPhysics
    {

        /// <summary>
        /// steps of the evaluation
        /// </summary>
        public int steps { get; set; }

        /// <summary>
        /// toggle to change from hover to pitch forward
        /// </summary>
        public bool hover { get; set; }

        /// <summary>
        /// sensor of the vehicle
        /// </summary>
        private Sensors sensors;

        /// <summary>
        /// throttle value for the vehicle
        /// </summary>
        public float throttle;

        /// <summary>
        /// PID gains for pitch
        /// </summary>
        public Vector3 PID_pitch_gains = new Vector3(2, 3, 2);

        /// <summary>
        /// PID gains for roll
        /// </summary>
        public Vector3 PID_roll_gains = new Vector3(2, 0.2f, 0.5f);

        /// <summary>
        /// PID gains for yaw
        /// </summary>
        public Vector3 PID_yaw_gains = new Vector3(1, 0, 0);

        /// <summary>
        /// PID gains for throttle
        /// </summary>
        public Vector3 PID_throttle_gains = new Vector3(0.5f, 0.2f, 0.2f);

        /// <summary>
        ///  PID controller for pitch
        /// </summary>
        private PIDController PID_pitch;

        /// <summary>
        ///  PID controller for roll
        /// </summary>
        private PIDController PID_roll;

        /// <summary>
        ///  PID controller for yaw
        /// </summary>
        private PIDController PID_yaw;

        /// <summary>
        ///  PID controller for throttle
        /// </summary>
        private PIDController throttle_control;

        /// <summary>
        /// total motor energy used
        /// </summary>
        public float totalMotorEnergyUsed { get; set; }

        /// <summary>
        /// vehicle of the analysis
        /// </summary>
        private QuadrantMotorVehicleLayout vehicle;

        /// <summary>
        /// status of the analysis
        /// </summary>
        public bool analysisEnded { get; set; }

        /// <summary>
        /// range of the vehicle in miles
        /// </summary>
        public float range { get; set; }

        /// <summary>
        /// velocity of the vehicle in mph
        /// </summary>
        public float velocity { get; set; }

        /// <summary>
        /// result of the analysis
        /// </summary>
        public string resultMsg { get; set; }

        // string results from the analysis
        public static string SUCCESS = "Success";
        public static string FAILURE = "Failure";
        public static string COULDNOTSTABLIZE = "CouldNotStabilize";
        public static string HITBOUNDARY = "HitBoundary";

        /// <summary>
        /// initializes the prototype
        /// </summary>
        /// <param name="vehicle">the QuadrantMotorVehicleLayout of the vehicle</param>
        public UAVPhysics(QuadrantMotorVehicleLayout vehicle)
        {

            // set vehicle of the analysis
            this.vehicle = vehicle;

            // initialize variables
            range = -1;
            steps = 0;
            hover = true;
            totalMotorEnergyUsed = 0;
            analysisEnded = false;

            // initialize sensors
            sensors = new Sensors();

            // initialize PIDControllers
            PID_pitch = new PIDController();
            PID_roll = new PIDController();
            PID_yaw = new PIDController();
            throttle_control = new PIDController();

        }


        /// <summary>
        /// gets the position of the main structure
        /// </summary>
        /// <returns></returns>
        public Vector3 getPosition()
        {
            return vehicle.mainPrototypeStructure.transform.position;
        }

        /// <summary>
        /// gets the orientation of the main structure
        /// </summary>
        /// <returns></returns>
        public Quaternion getOrientation()
        {
            return vehicle.mainPrototypeStructure.transform.rotation;
        }

        /// <summary>
        /// adds forces to motors based on PID controllers and adds vertical forces on foils
        /// </summary>
        public void AddMotorAndFoilForce()
        {


            //Calculate the errors so we can use a PID controller to stabilize
            steps += 1;

            // check if hover has occurred
            if (System.Math.Abs(vehicle.mainPrototypeStructure.GetComponent<Rigidbody>().velocity.y) < 0.10 &&
                System.Math.Abs(vehicle.mainPrototypeStructure.GetComponent<Rigidbody>().angularVelocity.y) < 0.05 &&
                System.Math.Abs(vehicle.mainPrototypeStructure.GetComponent<Rigidbody>().angularVelocity.x) < 0.05 &&
                System.Math.Abs(vehicle.mainPrototypeStructure.GetComponent<Rigidbody>().velocity.x) < 0.10 &&
                System.Math.Abs(vehicle.mainPrototypeStructure.GetComponent<Rigidbody>().velocity.z) < 0.10 &&
                steps > 100)
                hover = false;

            // 0 pitch angle to hover and positive 16 to pitch forward
            // for a higher fidelity analyses, pitch should be dependent on vehicle design
            float pitchError = sensors.GetPitchError();
            if (!hover)
                pitchError = sensors.GetPitchErrorForward();

            // get roll and yaw errors
            float rollError = sensors.GetRollError() * -1f;
            float yawError = sensors.GetYawError();

            // Adapt the PID variables to the throttle
            Vector3 PID_pitch_gains_adapted = throttle > 100f ? PID_pitch_gains * 2f : PID_pitch_gains;

            //Get the output from the PID controllers
            float PID_pitch_output = PID_pitch.GetFactorFromPIDController(PID_pitch_gains_adapted, pitchError);
            float PID_roll_output = PID_roll.GetFactorFromPIDController(PID_roll_gains, rollError);
            float PID_yaw_output = PID_yaw.GetFactorFromPIDController(PID_yaw_gains, yawError);

            // assign forces to each motor
            Dictionary<GameObject, float> forces = new Dictionary<GameObject, float>();

            // for each motor, apply a force correctly based on its quadrant
            List<GameObject> frMotors = vehicle.getMotors(MotorData.FRONTRIGHT);
            foreach (GameObject obj in frMotors)
            {
                float propellerForceFR = throttle + PID_pitch_output + PID_roll_output + getYawIncrement(obj, PID_yaw_output);
                forces[obj] = propellerForceFR;
            }
            List<GameObject> flMotors = vehicle.getMotors(MotorData.FRONTLEFT);
            foreach (GameObject obj in flMotors)
            {
                float propellerForceFL = throttle + PID_pitch_output - PID_roll_output + getYawIncrement(obj, PID_yaw_output);
                forces[obj] = propellerForceFL;
            }
            List<GameObject> brMotors = vehicle.getMotors(MotorData.BACKRIGHT);
            foreach (GameObject obj in brMotors)
            {
                float propellerForceBR = throttle + -PID_pitch_output + PID_roll_output + getYawIncrement(obj, PID_yaw_output);
                forces[obj] = propellerForceBR;
            }
            List<GameObject> blMotors = vehicle.getMotors(MotorData.BACKLEFT);
            foreach (GameObject obj in blMotors)
            {
                float propellerForceBL = throttle + -PID_pitch_output + -PID_roll_output + getYawIncrement(obj, PID_yaw_output);
                forces[obj] = propellerForceBL;
            }
            List<GameObject> rMotors = vehicle.getMotors(MotorData.RIGHT);
            foreach (GameObject obj in rMotors)
            {
                float propellerForceR = throttle + PID_roll_output + getYawIncrement(obj, PID_yaw_output);
                forces[obj] = propellerForceR;
            }
            List<GameObject> lMotors = vehicle.getMotors(MotorData.LEFT);
            foreach (GameObject obj in lMotors)
            {
                float propellerForceL = throttle - PID_roll_output + getYawIncrement(obj, PID_yaw_output);
                forces[obj] = propellerForceL;
            }
            List<GameObject> fMotors = vehicle.getMotors(MotorData.FRONT);
            foreach (GameObject obj in fMotors)
            {
                float propellerForceF = throttle + PID_pitch_output + getYawIncrement(obj, PID_yaw_output);
                forces[obj] = propellerForceF;
            }
            List<GameObject> bMotors = vehicle.getMotors(MotorData.BACK);
            foreach (GameObject obj in bMotors)
            {
                float propellerForceB = throttle - PID_pitch_output + getYawIncrement(obj, PID_yaw_output);
                forces[obj] = propellerForceB;
            }

            // for each foil
            foreach (GameObject obj in vehicle.foils)
            {
                // gets velocity
                float v = new Vector3(vehicle.mainPrototypeStructure.GetComponent<Rigidbody>().velocity.x, 0, vehicle.mainPrototypeStructure.GetComponent<Rigidbody>().velocity.z).magnitude;

                // gets the dot product of orientation and forward velocity
                float dot = Vector3.Dot(vehicle.mainPrototypeStructure.GetComponent<Rigidbody>().transform.forward.normalized, vehicle.mainPrototypeStructure.GetComponent<Rigidbody>().velocity.normalized);
                if (dot < 0)
                    dot = 0;

                // get the area of the foil
                float area = obj.GetComponent<MeshRenderer>().bounds.size.x * obj.GetComponent<MeshRenderer>().bounds.size.z;

                RaycastHit hit;

                // check for foils and structures in front
                Vector3 original_pos = obj.GetComponent<Rigidbody>().transform.position;
                Vector3 original_scale = obj.GetComponent<Rigidbody>().transform.localScale;
                // offset to start ray to check for obstructions in front
                float offsetx = 0.75f * original_scale.z / 2f;
                float offsety = 0.0f;
                float offsetz = 0.75f * original_scale.z / 2f;
                float offsetx1 = 5.0f;
                float offsetz1 = 5.0f;
                if (original_pos.x < 0)
                    offsetx1 = -5.0f;
                if (original_pos.z < 1000)
                    offsetz1 = -5.0f;
                Vector3 pos = new Vector3(original_pos.x + offsetx + offsetx1, original_pos.y + offsety, original_pos.z + offsetz + offsetz1); // offset down a little
                Vector3 forward_dir = vehicle.mainPrototypeStructure.GetComponent<Rigidbody>().transform.forward;
                // Debug.DrawRay(pos, 100 * forward_dir, Color.green);


                // check for an obstruction in front
                bool val = Physics.Raycast(pos, forward_dir, out hit);
                if (!val)
                {
                    // lift proportional to velcity^2 and area
                    // using Vector3(0f, 1f, 0f) , foil up direction caused instability
                    // of vehicle in Unity , need to try and update with foil up direction
                    Vector3 force = new Vector3(0f, 1f, 0f) * dot * 0.0005f * v * v * area;
                    obj.GetComponent<Rigidbody>().AddRelativeForce(force);
                }
                else
                {

                    string name = "";
                    try
                    {
                        name = hit.rigidbody.gameObject.name;
                    }
                    catch (System.Exception e)
                    {
                        // Don't really care if I don't know what I hit - it doesn't matter
                    }

                    // if a foil or structure, do not apply lift to the back foil
                    if (!name.StartsWith("foil") && !name.Contains("structure"))
                    {
                        // Debug.Log("foil: " + obj + " obstructs with: " + name);
                        Vector3 force = new Vector3(0f, 1f, 0f) * dot * 0.0005f * v * v * area;
                        obj.GetComponent<Rigidbody>().AddRelativeForce(force);
                    }

                }
            }               

            // get total avaialble energy
            float totalEnergy = vehicle.getTotalBatteryEnergy();
            // still energy in the battery
            if (totalMotorEnergyUsed < totalEnergy)
            {
                foreach (GameObject obj in forces.Keys)
                {
                    // apply force or maximum amount of force
                    float availableForce = System.Math.Min(forces[obj], vehicle.motors[obj].maximumThrust);

                    // assumption : since we do not want an hour long evaluation, we will just decrement the energy
                    // by the force / 500 and scale the distance and velocity, 
                    // based on results using our base line configuration
                    totalMotorEnergyUsed += availableForce / 500;
                    AddForceToPropeller(obj, availableForce);
                }
            }
            else if (!hover) // out of energy and traveling 
            {
                // want baseline to have a velocity of 20 mph which corrsponds to 36.95 Unity units
                velocity = 0.5418f * new Vector3(vehicle.mainPrototypeStructure.GetComponent<Rigidbody>().velocity.x,
                    0, vehicle.mainPrototypeStructure.GetComponent<Rigidbody>().velocity.z).magnitude;
                getDistance();
                analysisEnded = true;
                resultMsg = UAVPhysics.SUCCESS;
            }
            else // out of energy and hovering
            {
                // want baseline to have a velocity of 20 mph which corrsponds to 36.95 Unity units
                velocity = 0.5418f * new Vector3(vehicle.mainPrototypeStructure.GetComponent<Rigidbody>().velocity.x,
                    0, vehicle.mainPrototypeStructure.GetComponent<Rigidbody>().velocity.z).magnitude;
                getDistance();
                analysisEnded = true;
                resultMsg = UAVPhysics.COULDNOTSTABLIZE;
            }

            // hit or at the same level as the ground plate
            if (vehicle.mainPrototypeStructure.transform.position.y <=
                GameObject.Find("teststand").transform.position.y)
            {
                resultMsg = UAVPhysics.HITBOUNDARY;
                UavCollision.hit = true;
            }

            // fix to limit simulation run time, should not rach these limits, but fringe cases might
            if (System.Math.Abs(vehicle.mainPrototypeStructure.transform.position.x) > 2000
                || System.Math.Abs(vehicle.mainPrototypeStructure.transform.position.x) > 4000
                || steps > 40000)
            {
                totalMotorEnergyUsed = 1000000000000000;
            }

            // some outlier configurations
            if (vehicle.mainPrototypeStructure.GetComponent<Rigidbody>().transform.position.y > 1200)
            {
                velocity = 0.5418f * new Vector3(vehicle.mainPrototypeStructure.GetComponent<Rigidbody>().velocity.x, 0, vehicle.mainPrototypeStructure.GetComponent<Rigidbody>().velocity.z).magnitude;
                getDistance();
                analysisEnded = true;
                resultMsg = UAVPhysics.COULDNOTSTABLIZE;
            }

            // vehicle hit the teststand
            if (UavCollision.hit)
            {
                velocity = new Vector3(vehicle.mainPrototypeStructure.GetComponent<Rigidbody>().velocity.x, 0, vehicle.mainPrototypeStructure.GetComponent<Rigidbody>().velocity.z).magnitude;
                analysisEnded = true;
                getDistance();
                if (!hover)
                    resultMsg = UAVPhysics.HITBOUNDARY;
                else
                    resultMsg = UAVPhysics.COULDNOTSTABLIZE;
            }

        }

        /// <summary>
        /// gets a yaw increment for the controllers
        /// </summary>
        /// <param name="obj">Unity game engine object</param>
        /// <param name="value">yaw error value</param>
        /// <returns></returns>
        public float getYawIncrement(GameObject obj, float value)
        {
            float newValue = value;
            if (obj.name.Contains("ccw"))
                newValue = -value;
            return newValue;
        }

        /// <summary>
        /// gets the distance traveled
        /// </summary>
        /// <returns></returns>
        public float getDistance()
        {
            if (!hover)
                range = Vector3.Distance(vehicle.mainPrototypeStructure.GetComponent<Rigidbody>().position, vehicle.mainPrototypeStructurePosition);
            else
                range = -System.Math.Abs(vehicle.mainPrototypeStructure.GetComponent<Rigidbody>().velocity.y);

            // base design has a unity distance travel of 382 units
            // want the base design to go 10 total miles
            // so divided distance by 38.0
            range = range / 38.0f;

            return range;
        }

        /// <summary>
        /// adds a force at a motor
        /// </summary>
        /// <param name="propellerObj">Unity GameObject of the motor</param>
        /// <param name="propellerForce">force to apply</param>
        public void AddForceToPropeller(GameObject propellerObj, float propellerForce)
        {
            Vector3 propellerUp = propellerObj.transform.up;
            Vector3 propellerPos = propellerObj.transform.position;
            propellerObj.GetComponent<Rigidbody>().AddRelativeForce(propellerUp * propellerForce);

            if (propellerObj.tag.StartsWith(DesignerAssets.UAVDesigner.PROTOTYPEMOTORCCW))
                propellerObj.GetComponent<Rigidbody>().AddRelativeTorque(propellerUp * propellerForce);
            else if (propellerObj.tag.StartsWith(DesignerAssets.UAVDesigner.PROTOTYPEMOTORCW))
                propellerObj.GetComponent<Rigidbody>().AddRelativeTorque(-propellerUp * propellerForce);
        }

        /// <summary>
        /// try to hold vehicle vertical position as constant
        /// </summary>
        public void AddAutoPilot()
        {

            float error = vehicle.mainPrototypeStructure.GetComponent<Rigidbody>().velocity.y;

            // get the output from the PID controllers
            float PID_throttle_output = throttle_control.GetFactorFromPIDController(PID_throttle_gains, error);
            throttle += -PID_throttle_output;
            throttle = Mathf.Clamp(throttle, 0f, 200f);

            // camera position and orientation 
            Vector3 prototypePos = vehicle.mainPrototypeStructure.GetComponent<Rigidbody>().position;
            Camera.main.transform.position = new Vector3(-100, 1140, 1800);
            Camera.main.transform.LookAt(prototypePos);

        }


    }
}