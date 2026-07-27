using System;
using System.Timers;

namespace XIVSlothComboX.CustomComboNS.Functions
{
    internal abstract partial class CustomComboFunctions
    {
        internal bool restartCombatTimer = true;
        internal static TimeSpan combatDuration = new();
        internal  DateTime combatStart;
        internal  DateTime combatEnd;
        internal Timer? combatTimer;

        /// <summary> Called by the timer in the constructor to keep track of combat duration. </summary>
        internal void UpdateCombatTimer()
        {
            if (InCombat())
            {
                if (restartCombatTimer)
                {
                    restartCombatTimer = false;
                    combatStart = DateTime.Now;
                }

                combatEnd = DateTime.Now;
                combatDuration = combatEnd - combatStart;
            }
            else
            {
                restartCombatTimer = true;
                combatDuration = TimeSpan.Zero;
            }

            
        }

        /// <summary> Tells the elapsed time since the combat started. </summary>
        /// <returns> Combat time in seconds. </returns>
        public static TimeSpan CombatEngageDuration() => combatDuration;

        protected void StartTimer()
        {
            // combatTimer = new Timer(100); // in milliseconds
            // combatTimer.Elapsed += UpdateCombatTimer;
            // combatTimer.Start();
        }
    }
}
