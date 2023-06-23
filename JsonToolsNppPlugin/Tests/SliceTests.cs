using System;
using System.Collections.Generic;
using System.Text;
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
                new object[]{ onetofive, 2, null, null, new int[]{1, 2} },
                new object[]{ onetofive, null, null, null, onetofive },
                new object[]{ onetofive, null, 1, null, new int[]{1} },
                new object[]{ onetofive, null, null, -1, new int[] {5, 4, 3, 2, 1} },
                new object[]{ onetofive, 1, 3, null, new int[]{2, 3} },
                new object[]{ onetofive, 1, 4, 2, new int[]{2, 4} },
                new object[]{ onetofive, 2, null, -1, new int[]{3, 2, 1} },
                new object[]{ onetofive, 4, 1, -2, new int[]{5, 3} },
                new object[]{ onetofive, 1, null, 3, new int[]{2, 5} },
                new object[]{ onetofive, 4, 2, -1, new int[]{5, 4} },
                new object[]{ onetofive, -3, null, null, new int[]{1,2} },
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
            //(int[] input, int start, int stop, int stride, int[] desired)
            int tests_failed = 0;
            int ii = 0;
            foreach (object[] stuff in testcases)
            {
                int[] input = (int[])stuff[0], desired = (int[])stuff[4];
                int? start = (int?)stuff[1], stop = (int?)stuff[2], stride = (int?)stuff[3];
                int[] output = (int[])input.Slice(start, stop, stride);
                // verify that it works for both arrays and Lists, because both implement IList
                List<int> list_output = (List<int>)(new List<int>(input)).Slice(start, stop, stride);
                var sb_desired = new StringBuilder();
                sb_desired.Append('{');
                foreach (int desired_value in desired)
                {
                    sb_desired.Append(desired_value.ToString());
                    sb_desired.Append(", ");
                }
                sb_desired.Append('}');
                string str_desired = sb_desired.ToString();
                var sb_output = new StringBuilder();
                sb_output.Append('{');
                foreach (int value in output)
                {
                    sb_output.Append(value.ToString());
                    sb_output.Append(", ");
                }
                sb_output.Append('}');
                string str_output = sb_output.ToString();
                if (str_output != str_desired)
                {

                    tests_failed++;
                    Npp.AddLine(String.Format("Test {0} ({1}.Slice({2}, {3}, {4})) failed:\n" +
                                                    "Expected\n{5}\nGot\n{6}",
                                                    ii + 1, input, start, stop, stride, str_desired, str_output));
                }
                ii++;
                var sb_list_output = new StringBuilder();
                sb_list_output.Append('{');
                foreach (int value in list_output)
                {
                    sb_list_output.Append(value.ToString());
                    sb_list_output.Append(", ");
                }
                sb_list_output.Append('}');
                string str_list_output = sb_list_output.ToString();
                if (str_list_output != str_desired)
                {

                    tests_failed++;
                    Npp.AddLine(String.Format("Test {0} ({1}.Slice({2}, {3}, {4})) failed:\n" +
                                                    "Expected\n{5}\nGot\n{6}",
                                                    ii + 1, input, start, stop, stride, str_desired, str_list_output));
                }
                ii++;
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
                new object[]{ onetofive, "-2:15", new int[]{4,5} }
            };
            // test string slicer
            foreach (object[] inp_sli_desired in str_testcases)
            {
                int[] inp = (int[])inp_sli_desired[0];
                string slicer = (string)inp_sli_desired[1];
                int[] desired = (int[])inp_sli_desired[2];
                int[] output = (int[])inp.Slice(slicer);
                var sb_desired = new StringBuilder();
                sb_desired.Append('{');
                foreach (int desired_value in desired)
                {
                    sb_desired.Append(desired_value.ToString());
                    sb_desired.Append(", ");
                }
                sb_desired.Append('}');
                string str_desired = sb_desired.ToString();
                var sb_output = new StringBuilder();
                sb_output.Append('{');
                foreach (int value in output)
                {
                    sb_output.Append(value.ToString());
                    sb_output.Append(", ");
                }
                sb_output.Append('}');
                string str_output = sb_output.ToString();
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
