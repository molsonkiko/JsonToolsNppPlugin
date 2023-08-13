using System;
using JSON_Tools.Utils;

namespace JSON_Tools.Tests
{
    public class SliceTester
    {
        public static readonly int[] onetofive = new int[] { 1, 2, 3, 4, 5 };

        // terminology below:
        // + -> 0 <= num <= 4
        // - -> -1 >= num >= -5
        // big -> num >= 5
        // negbig -> num <= -6
        // <descriptor>1, <descriptor>2 -> 1 is less than 2 (after doing negative number wraparound)
        //     e.g., 3:-1 becomes +1:-2 because -1 becomes 4 and 3 < 4
        //     but -3:4 becomes -2:+1 because -3 becomes 2 and 2 < 4
        //     and 2:4 becomes +1:+2 because 2 < 4
        public static readonly (string slicer, int[] desired)[] str_testcases = new (string slicer, int[] desired)[]
        {
            (":", onetofive),                       // : 
            ("2:", new int[]{3, 4, 5}),             // +:
            ("-3:", new int[] { 3, 4, 5 }),         // -:
            ("5:", new int[] { }),                  // big:
            ("-24:", onetofive),                   // negbig:
            (":1", new int[]{1}),                   // :+
            (":-3", new int[] { 1, 2 }),            // :-
            (":85", onetofive),                    // :big
            (":-24", new int[]{}),                 // :negbig
            ("1:3", new int[] { 2, 3 }),            // +1:+2
            ("3:1", new int[]{ }),                  // +2:+1
            ("3:3", new int[]{}),                   // +=:+=
            ("1:-1", new int[] { 2, 3, 4 }),        // +1:-2
            ("4:-3", new int[]{ }),                 // +2:-1
            ("3:-2", new int[]{ }),                 // +=:-=
            ("4:85", new int[]{5}),                 // +:big
            ("3:-8", new int[]{ }),                 // +:negbig
            ("-4:4", new int[] { 2, 3, 4 }),        // -1:+2
            ("-3:1", new int[]{}),                  // -2:+1
            ("-3:2", new int[]{}),                  // -=:+=
            ("-3:-1", new int[]{3, 4}),             // -1:-2
            ("-1:-3", new int[]{}),                 // -2:-1
            ("-1:-1", new int[]{}),                 // -=:-=
            ("-2:85", new int[] { 4, 5 }),          // -:big
            ("-4:-24", new int[]{}),                // -:negbig
            ("85:1", new int[]{}),                  // big:+
            ("85:-3", new int[]{}),                 // big:-
            ("85:5", new int[]{}),                  // big:=
            ("85:85", new int[] { }),               // big:big
            ("85:-24", new int[]{}),                // big:negbig
            ("-24:85", onetofive),                  // negbig:big
            ("-24:-24", new int[] { }),             // negbig:negbig
            ("-24:-5", new int[]{}),                // negbig:=
            ("-24:-4", new int[] { 1 }),            // negbig:-
            ("-24:3", new int[]{1,2,3}),            // negbig:+
            ("1::3", new int[] { 2, 5 }),           // +::+
            ("::-1", new int[] { 5, 4, 3, 2, 1 }),  // ::-1
            ("1:4:2", new int[] { 2, 4 }),
            ("2::-1", new int[] { 3, 2, 1 }),
            ("4:1:-2", new int[] { 5, 3 }),
            ("4:2:-1", new int[] { 5, 4 }),
            ("-4::2", new int[] { 2, 4 }),
            ("-3::1", new int[] { 3, 4, 5 }),
            ("-3::-1", new int[] { 3, 2, 1 }),
            ("-700:-54:-3", new int[] { }),
            ("-1:1:-2", new int[] { 5, 3 }),
            ("-4:4:2", new int[] { 2, 4 }),
            ("2:-2:2", new int[] { 3 }),
            ("3::5", new int[] { 4 }),
            ("13434:4343:8", new int[] { }),
            ("13434:43:-8", new int[] { }),
            ("13434:-4:-1", new int[] { 5, 4, 3 }),
            ("178::-6", new int[] { 5 }),
            ("9343::-2", new int[] { 5, 3, 1 }),
            ("86:1:-3", new int[] { 5 }),
            ("-700:54:9", new int[] { 1 }),
            ("-24:-5:-1", new int[] { }),
            ("5:-24:-1", new int[] { 5, 4, 3, 2, 1 }),
            ("3:-7:-3", new int[] { 4, 1 }),
        };

        public static bool Test()
        {
            string input_str = onetofive.ArrayToString();
            var testcases = new (int?[] slicer, int[] output)[]
            {
                (new int?[]{null, null, null}, onetofive),
                (new int?[]{null, 1, null}, new int[]{1}),
                (new int?[]{null, null, -1}, new int[] {5, 4, 3, 2, 1}),
                (new int?[]{1, 3, null}, new int[]{2, 3}),
                (new int?[]{1, 4, 2}, new int[]{2, 4}),
                (new int?[]{2, null, -1}, new int[]{3, 2, 1}),
                (new int?[]{4, 1, -2}, new int[]{5, 3}),
                (new int?[]{1, null, 3}, new int[]{2, 5}),
                (new int?[]{4, 2, -1}, new int[]{5, 4}),
                (new int?[]{-4, -1, null}, new int[]{2,3,4}),
                (new int?[]{-4, null, 2}, new int[]{2, 4}),
                (new int?[]{null, -3, null}, new int[]{1,2}),
                (new int?[]{-3, null, 1}, new int[]{3,4,5}),
                (new int?[]{-3, null, -1}, new int[]{3,2,1}),
                (new int?[]{-1, 1, -2}, new int[]{5, 3}),
                (new int?[]{1, -1, null}, new int[]{2,3,4}),
                (new int?[]{-4, 4, null}, new int[]{2,3,4}),
                (new int?[]{-4, 4, 2}, new int[]{2, 4}),
                (new int?[]{2, -2, 2}, new int[]{3}),
                (new int?[]{-4, null, -2}, new int[]{2}),
                (new int?[]{2, 1, null}, new int[]{ }),
            };

            int tests_failed = 0;
            int ii = 0;
            foreach ((int?[] slicer, int[] desired) in testcases)
            {
                int[] input = onetofive;
                string str_desired = desired.ArrayToString();
                int[] output;
                ii++;
                try
                {
                    output = (int[])input.Slice(slicer);
                }
                catch (Exception ex)
                {
                    tests_failed++;
                    Npp.AddLine(String.Format("Test {0} ({1}.Slice({2})) failed:\n" +
                                                    "Expected\n{3}\nGot exception\n{6}",
                                                    ii + 1, input_str, slicer.ArrayToString(), str_desired, ex));
                    continue;
                }
                string str_output = output.ArrayToString();
                if (str_output != str_desired)
                {

                    tests_failed++;
                    Npp.AddLine(String.Format("Test {0} ({1}.Slice({2})) failed:\n" +
                                                    "Expected\n{3}\nGot\n{6}",
                                                    ii + 1, input_str, slicer.ArrayToString(), str_desired, str_output));
                }
            }
            // test string slicer
            foreach ((string slicer, int[] desired) in str_testcases)
            {
                int[] inp = onetofive;
                string str_desired = desired.ArrayToString();
                int[] output;
                try
                {
                    output = (int[])inp.Slice(slicer);
                }
                catch (Exception ex)
                {
                    tests_failed++;
                    Npp.AddLine(String.Format("Test {0} ({1}.Slice(\"{2}\")) failed:\n" +
                                                    "Expected\n{3}\nGot exception\n{4}",
                                                    ii + 1, input_str, slicer, str_desired, ex));
                    continue;
                }
                string str_output = output.ArrayToString();
                if (str_output != str_desired)
                {

                    tests_failed++;
                    Npp.AddLine(String.Format("Test {0} ({1}.Slice(\"{2}\")) failed:\n" +
                                                    "Expected\n{3}\nGot\n{4}",
                                                    ii + 1, input_str, slicer, str_desired, str_output));
                }
                ii++;
            }

            Npp.AddLine($"Failed {tests_failed} tests.");
            Npp.AddLine($"Passed {ii - tests_failed} tests.");
            return tests_failed > 0;
        }
    }
}
