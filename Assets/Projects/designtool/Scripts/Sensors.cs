using UnityEngine;

namespace DesignerObjects
{
    /// <summary>
    /// Returns roll, pitch, and yaw errors of the vehicle
    /// </summary>
    class Sensors
    {

        public Sensors() { }

        /// <summary>
        /// gets the transform based on the a structure in the vehicle assembly
        /// </summary>
        /// <returns></returns>
        private Transform getTransform()
        {
            GameObject[] objects = GameObject.FindGameObjectsWithTag("protostructure");
            return objects[0].transform;
        }

        /// <summary>
        /// gets the pitch error in hover mode (error from 0 degrees pitch forward)
        /// </summary>
        /// <returns>pitch error in degrees</returns>
        public float GetPitchError()
        {
            return WrapAngle(getTransform().eulerAngles.x);
        }

        /// <summary>
        /// gets the pitch error in forward mode (error from 16 degrees pitch forward)
        /// </summary>
        /// <returns>pitch error in degrees</returns>
        public float GetPitchErrorForward()
        {
            return WrapAngle(getTransform().eulerAngles.x) - 16;
        }


        /// <summary>
        /// gets the roll error 
        /// </summary>
        /// <returns>roll error in degrees</returns>
        public float GetRollError()
        {
            return WrapAngle(getTransform().eulerAngles.z);
        }

        /// <summary>
        /// gets the yaw error (error from 45 degrees since the vehicle is oriented at
        /// 45 degrees in the scene in the positive x and z direction)
        /// </summary>
        /// <returns>yaw error in degrees</returns>
        public float GetYawError()
        {
            return WrapAngle(getTransform().eulerAngles.y) - 45;
        }

        /// <summary>
        /// wraps the angle to between -180 and 180
        /// </summary>
        /// <param name="inputAngle">ange in degrees</param>
        /// <returns>wrapped angle in degrees</returns>
        float WrapAngle(float inputAngle)
        {
            float angle = ((inputAngle % 360f) + 360f) % 360f;
            if (angle > 180f && angle <= 360f)
                angle = angle - 360f;
            return angle;
        }


    }
}
