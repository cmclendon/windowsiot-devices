using System;
using static System.Diagnostics.Debug;

namespace Bit.Windows.Devices.Diagnostics
{
    [Flags]
    public enum DEBUGLEVEL
    {
        None=0x00,
        Trace=0x01,
        Informational=0x02,
        Warning=0x04,
        Exception=0x08
    }

    static public class Debug
    {
#if(DEBUG)
        private static DEBUGLEVEL mask = DEBUGLEVEL.Warning | DEBUGLEVEL.Exception;
#else
        private static DEBUGLEVEL mask = DEBUGLEVEL.None;
#endif

        public static DEBUGLEVEL Mask
        {
            get
            {
                return mask;
            }
        }

        public static void Write(string source, DEBUGLEVEL level, string message, params object[] args)
        {
            if ( (mask & level) == level)
            {
                string formattedMessage = String.Format(message, args);
                WriteLine($"({level.ToString()} [{source}] with message '{formattedMessage}'");
            }
        }

        public static void Write(string source, System.Exception e) 
            => Write(source, DEBUGLEVEL.Exception, e.Message);

        public static void Enable(DEBUGLEVEL mask) 
            => Debug.mask |= mask;

        public static void Disable(DEBUGLEVEL mask) 
            => Debug.mask ^= mask;
    }
}
