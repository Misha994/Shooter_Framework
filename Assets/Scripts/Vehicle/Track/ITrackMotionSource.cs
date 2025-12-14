// Assets/Scripts/Vehicle/TracksWC/ITrackMotionSourceWC.cs
// SPDX-License-Identifier: MIT
using UnityEngine;

namespace TracksWC
{
    /// <summary>
    /// Джерело швидкості (м/с) для лівої/правої гусениці.
    /// </summary>
    public interface ITrackMotionSource
    {
        /// <param name="leftSide">true = ліва, false = права гусениця</param>
        float GetMetersPerSecond(bool leftSide);

        /// <summary>Середня поступальна швидкість уздовж "носа" танка (м/с).</summary>
        float GetForwardSpeedMS();
    }
}
