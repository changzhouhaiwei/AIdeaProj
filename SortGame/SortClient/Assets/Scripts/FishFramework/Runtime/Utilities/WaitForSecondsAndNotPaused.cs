using System;
using UnityEngine;

namespace FishFramework
{
    public class WaitForSecondsAndNotPaused : CustomYieldInstruction
    {
        private float seconds;
        private readonly Func<bool> isPaused;
        private float initialTime;

        private bool paused
        {
            get
            {
                if (isPaused())
                {
                    seconds -= Mathf.Max(deltaTime, 0);
                    initialTime = Time.time;
                }

                return isPaused();
            }
        }

        private float deltaTime => Time.time - initialTime;

        public override bool keepWaiting => paused || (deltaTime < seconds);

        public WaitForSecondsAndNotPaused(float seconds, Func<bool> isPaused)
        {
            this.seconds = seconds;
            this.isPaused = isPaused;
            initialTime = Time.time;
        }
    }
}