using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandMidiControllerDDD.Infrastructure
{
    public static class GestureDetector
    {
        public static bool IsHighHand(double y) => y < 0.3;
        public static bool IsCloseFist(double z) => z < 0.2;
        public static bool IsHandFarBack(double z) => z > 0.6;
        public static bool IsCentered(double x, double y) =>
            x > 0.4 && x < 0.6 && y > 0.4 && y < 0.6;
    }
}

