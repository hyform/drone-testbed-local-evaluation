using UnityEngine;

/// <summary>
/// PID Controller for the vehicle
/// </summary>
public class PIDController
{

    // PID parameters

    /// <summary>
    /// proportional gain
    /// </summary>
    public float gain_P = 0f;

    /// <summary>
    /// integral gain
    /// </summary>
    public float gain_I = 0f;

    /// <summary>
    /// derivative gain
    /// </summary>
    public float gain_D = 0f;

    /// <summary>
    /// stores the previous error
    /// </summary>
    float error_old = 0f;

    /// <summary>
    /// stores the sum of previous errors
    /// </summary>
    float error_sum = 0f;

    /// <summary>
    /// limit the total sum of all errors used in the I
    /// </summary>
    private float error_sumMax = 20f;

    /// <summary>
    /// gets the PID output from the controller
    /// </summary>
    /// <param name="gains">vector of gains for P, I, and D</param>
    /// <param name="error">incoming error</param>
    /// <returns></returns>
    public float GetFactorFromPIDController(Vector3 gains, float error)
    {

        // set the gains
        this.gain_P = gains.x;
        this.gain_I = gains.y;
        this.gain_D = gains.z;

        //The output from PID
        float output = 0f;

        //P
        output += gain_P * error;

        //I
        error_sum += Time.fixedDeltaTime * error;

        //Clamp the sum 
        this.error_sum = Mathf.Clamp(error_sum, -error_sumMax, error_sumMax);
        output += gain_I * error_sum;

        //D
        float d_dt_error = (error - error_old) / Time.fixedDeltaTime;

        //Save the last errors
        this.error_old = error;

        output += gain_D * d_dt_error;

        return output;

    }

}
