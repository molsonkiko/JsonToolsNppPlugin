using System;
using System.Collections.Generic;
using JSON_Tools.Utils;

namespace JSON_Tools.Tests
{
    public class SliceTester
    {
        public static void Test()
        {
            int[] onetofive = new int[] { 1, 2, 3, 4, 5 };
            var testcases = new object[][]
            {
                new object[]{ onetofive, null, null, null, onetofive },
                new object[]{ onetofive, null, 1, null, new int[]{1} },
                new object[]{ onetofive, null, null, -1, new int[] {5, 4, 3, 2, 1} },
                new object[]{ onetofive, 1, 3, null, new int[]{2, 3} },
                new object[]{ onetofive, 1, 4, 2, new int[]{2, 4} },
                new object[]{ onetofive, 2, null, -1, new int[]{3, 2, 1} },
                new object[]{ onetofive, 4, 1, -2, new int[]{5, 3} },
                new object[]{ onetofive, 1, null, 3, new int[]{2, 5} },
                new object[]{ onetofive, 4, 2, -1, new int[]{5, 4} },
                new object[]{ onetofive, -4, -1, null, new int[]{2,3,4} },
                new object[]{ onetofive, -4, null, 2, new int[]{2, 4} },
                new object[]{ onetofive, null, -3, null, new int[]{1,2} },
                new object[]{ onetofive, -3, null, 1, new int[]{3,4,5} },
                new object[]{ onetofive, -3, null, -1, new int[]{3,2,1} },
                new object[]{ onetofive, -1, 1, -2, new int[]{5, 3} },
                new object[]{ onetofive, 1, -1, null, new int[]{2,3,4} },
                new object[]{ onetofive, -4, 4, null, new int[]{2,3,4} },
                new object[]{ onetofive, -4, 4, 2, new int[]{2, 4} },
                new object[]{ onetofive, 2, -2, 2, new int[]{3} },
                new object[]{ onetofive, -4, null, -2, new int[]{2} },
                new object[]{ onetofive, 2, 1, null, new int[]{ } }
            };

            int tests_failed = 0;
            int ii = 0;
            foreach (object[] stuff in testcases)
            {
                int[] input = (int[])stuff[0], desired = (int[])stuff[4];
                string str_desired = desired.ArrayToString();
                int? start = (int?)stuff[1], stop = (int?)stuff[2], stride = (int?)stuff[3];
                int[] output;
                ii++;
                try
                {
                    output = (int[])input.Slice(start, stop, stride);
                }
                catch (Exception ex)
                {
                    tests_failed++;
                    Npp.AddLine(String.Format("Test {0} ({1}.Slice({2}, {3}, {4})) failed:\n" +
                                                    "Expected\n{5}\nGot exception\n{6}",
                                                    ii + 1, input, start, stop, stride, str_desired, ex));
                    continue;
                }
                string str_output = output.ArrayToString();
                if (str_output != str_desired)
                {

                    tests_failed++;
                    Npp.AddLine(String.Format("Test {0} ({1}.Slice({2}, {3}, {4})) failed:\n" +
                                                    "Expected\n{5}\nGot\n{6}",
                                                    ii + 1, input, start, stop, stride, str_desired, str_output));
                }
            }
            var str_testcases = new object[][]
            {
                new object[]{ onetofive, "2", new int[]{1, 2} },
                new object[]{ onetofive, ":", onetofive },
                new object[]{ onetofive, ":1", new int[]{1} },
                new object[]{ onetofive, "::-1", new int[] {5, 4, 3, 2, 1} },
                new object[]{ onetofive, "1:3", new int[]{2, 3} },
                new object[]{ onetofive, "1::3", new int[]{2, 5} },
                new object[]{ onetofive, "1:4:2", new int[]{2, 4} },
                new object[]{ onetofive, "2::-1", new int[]{3, 2, 1} },
                new object[]{ onetofive, "4:1:-2", new int[]{5, 3} },
                new object[]{ onetofive, "1::3", new int[]{2, 5} },
                new object[]{ onetofive, "4:2:-1", new int[]{5, 4} },
                new object[]{ onetofive, "-3", new int[]{1,2} },
                new object[]{ onetofive, "-4:-1", new int[]{2,3,4} },
                new object[]{ onetofive, "-4::2", new int[]{2, 4} },
                new object[]{ onetofive, ":-3", new int[]{1,2} },
                new object[]{ onetofive, "-3::1", new int[]{3,4,5} },
                new object[]{ onetofive, "-3:", new int[]{3,4,5} },
                new object[]{ onetofive, "-3::-1", new int[]{3,2,1} },
                new object[]{ onetofive, "-1:1:-2", new int[]{5, 3} },
                new object[]{ onetofive, "1:-1", new int[]{2,3,4} },
                new object[]{ onetofive, "-4:4", new int[]{2,3,4} },
                new object[]{ onetofive, "-4:4:2", new int[]{2, 4} },
                new object[]{ onetofive, "2:-2:2", new int[]{3} },
                new object[]{ onetofive, "3::5", new int[]{4} },
                new object[]{ onetofive, "5:", new int[]{ } },
                new object[]{ onetofive, "3:8", new int[]{4, 5} },
                new object[]{ onetofive, "-2:15", new int[]{4,5} },
                new object[]{ onetofive, "13434:4343", new int[] { } },
                new object[]{ onetofive, "13434:4343:8", new int[] { } },
                new object[]{ onetofive, "13434:43:-8", new int[] { } },
                new object[]{ onetofive, "13434:-4:-1", new int[] {5, 4, 3} },
                new object[]{ onetofive, "178::-6", new int[] { 5 } },
                new object[]{ onetofive, "9343::-2", new int[] { 5, 3, 1} },
                new object[]{ onetofive, "86:1:-3", new int[] { 5 } },
                new object[]{ onetofive, "-700:-54:-3", new int[] { } },
                new object[]{ onetofive, "-700:-54", new int[] { } },
                new object[]{ onetofive, "-700:54", new int[] { 1, 2, 3, 4, 5 } },
                new object[]{ onetofive, "-700:54:9", new int[] { 1 } },
                new object[]{ onetofive, "-24:-5", new int[] { } },
                new object[]{ onetofive, "-24:-5:-1", new int[] { } },
                new object[]{ onetofive, "5:-24:-1", new int[] { 5, 4, 3, 2, 1 } },
                new object[]{ onetofive, "3:-7:-3", new int[] { 4, 1 } },
            };
            // test string slicer
            foreach (object[] inp_sli_desired in str_testcases)
            {
                int[] inp = (int[])inp_sli_desired[0];
                string slicer = (string)inp_sli_desired[1];
                int[] desired = (int[])inp_sli_desired[2];
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
                                                    ii + 1, inp, slicer, str_desired, ex));
                    continue;
                }
                string str_output = output.ArrayToString();
                if (str_output != str_desired)
                {

                    tests_failed++;
                    Npp.AddLine(String.Format("Test {0} ({1}.Slice(\"{2}\")) failed:\n" +
                                                    "Expected\n{3}\nGot\n{4}",
                                                    ii + 1, inp, slicer, str_desired, str_output));
                }
                ii++;
            }

            Npp.AddLine($"Failed {tests_failed} tests.");
            Npp.AddLine($"Passed {ii - tests_failed} tests.");
        }
    }
}
