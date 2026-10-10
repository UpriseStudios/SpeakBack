using System;

namespace Uprise.SpeakBack.Core
{
    /// <summary>
    /// Argument checks shared by Core types. Each check returns the value it validated so it can be
    /// used inline in constructors and factories.
    /// </summary>
    public static class Guard
    {
        /// <summary>Returns <paramref name="value"/> when it is greater than zero; otherwise throws.</summary>
        public static int Positive(int value, string paramName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(paramName, value, "Must be greater than zero.");
            }

            return value;
        }

        /// <summary>Returns <paramref name="value"/> when it is neither NaN nor infinite; otherwise throws.</summary>
        public static float Finite(float value, string paramName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(paramName, value, "Must be a finite number.");
            }

            return value;
        }
    }
}
