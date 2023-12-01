using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Utils
{
    public class Utils
    {
        public static float GetAngleFromVectorFloat(Vector3 dir)
        {
            dir = dir.normalized;
            float deg = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
            if (deg < 0)
            {
                deg += 360;
            }
            return deg;
        }

        /// <summary>
        /// Use Box-Muller transform method
        /// https://www.alanzucconi.com/2015/09/16/how-to-sample-from-a-gaussian-distribution/
        /// </summary>
        /// <returns></returns>
        public static Vector2 NextGaussian2D()
        {
            float u1, u2, r, theta;
            u1 = UnityEngine.Random.Range(0f, 1f);
            u2 = UnityEngine.Random.Range(0f, 1f);

            r = Mathf.Sqrt(-2.0f * Mathf.Log(u1));
            theta = 2 * Mathf.PI * u2;
            float x = r * Mathf.Cos(theta);
            float y = r * Mathf.Sin(theta);

            return new Vector2(x, y);
        }
        /// <summary>
        /// Return standard gaussian
        /// Use Marsaglia polar method
        /// https://www.alanzucconi.com/2015/09/16/how-to-sample-from-a-gaussian-distribution/
        /// </summary>
        /// <returns></returns>
        public static float NextGaussian()
        {
            float v1, v2, s;
            do
            {
                v1 = 2.0f * UnityEngine.Random.Range(0f, 1f) - 1.0f;
                v2 = 2.0f * UnityEngine.Random.Range(0f, 1f) - 1.0f;
                s = v1 * v1 + v2 * v2;
            } while (s >= 1.0f || s == 0f);
            s = Mathf.Sqrt((-2.0f * Mathf.Log(s)) / s);

            return v1 * s;
        }
        /// <summary>
        /// Return general gaussian
        /// </summary>
        /// <param name="mean"></param>
        /// <param name="standard_deviation"></param>
        /// <returns></returns>
        public static float NextGaussian(float mean, float standard_deviation)
        {
            return mean + NextGaussian() * standard_deviation;
        }
        /// <summary>
        /// Return Gaussian damped between min and max
        /// </summary>
        /// <param name="mean"></param>
        /// <param name="standard_deviation"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static float NextGaussian(float mean, float standard_deviation, float min, float max)
        {
            float x;
            do
            {
                x = NextGaussian(mean, standard_deviation);
            } while (x < min || x > max);
            return x;
        }

    }
}
