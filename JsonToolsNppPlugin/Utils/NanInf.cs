namespace JSON_Tools.Utils
{
    public class NanInf
    {
        /// <summary>
        /// a/b<br></br>
        /// may be necessary to generate infinity or nan at runtime
        /// to avoid the compiler pre-computing things<br></br>
        /// since if the compiler sees literal 1d/0d in the code
        /// it just pre-computes it at compile time
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double Divide(double a, double b) { return a / b; }

        public static readonly double inf = Divide(1d, 0d);
        public static readonly double neginf = Divide(-1d, 0d);
        public static readonly double nan = Divide(0d, 0d);
    }
}
