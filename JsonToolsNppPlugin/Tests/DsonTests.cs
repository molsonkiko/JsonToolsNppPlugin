using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;

namespace JSON_Tools.Tests
{
    public class DsonTester
    {
        public static void TestDump()
        {
            (string inp, string correctOut)[] testcases = new (string inp, string correctOut)[]
            {
                ("[]", "so many"),
                ("{}", "such wow"),
                ("[1]", "so 1 many"),
                ("{\"a\": 13}", "such \"a\" is 15 wow"),
                ("null", "empty"),
                ("[1.5, [2e31], -3.5E40]", "so 1.5 and so 2VERY37 many also -3.5VERY50 many"),
                ("{\"aჿ\":[true,false,{\"c\": 3},\"оa\",[1, -2]], \"b\\\"\": \"z\", \"c\": {\"d\": 1.13}, \"d\": 50, \"e\": 0.76, \"f\": null}",
                    "such \"aჿ\" is so yes and no also such \"c\" is 3 wow and \"оa\" also so 1 and -2 many many, \"b\\\"\" is \"z\". " +
                    "\"c\" is such \"d\" is 1.15 wow! \"d\" is 62? \"e\" is 0.114, \"f\" is empty wow"),
                ("[-9223372036854775808, 9223372036854775807]", "so 1000000000000000000000 and 777777777777777777777 many"),
            };
            int tests_failed = 0;
            int ii = 0;
            JsonParser parser = new JsonParser();
            foreach ((string input, string correctOut) in testcases)
            {
                JNode json = JsonParserTester.TryParse(input, parser);
                if (json == null) continue;
                ii++;
                string dson;
                try
                {
                    dson = Dson.Dump(json);
                }
                catch (Exception ex)
                {
                    tests_failed++;
                    string msg = $"Tried to parse json\r\n{input}\r\nas dson\r\n{correctOut}\r\nbut got error:\r\n{ex.ToString()}\r\n";
                    Npp.editor.AppendText(Encoding.UTF8.GetByteCount(msg), msg);
                    continue;
                }
                if (dson != correctOut)
                {
                    tests_failed++;
                    string msg = $"Expected json\r\n{input}\r\nto be emitted\r\nas dson\r\n{correctOut}\r\nbut instead got \r\n{dson}\r\n";
                    Npp.editor.AppendText(Encoding.UTF8.GetByteCount(msg), msg);
                }
            }
            Npp.AddLine($"Failed {tests_failed} tests.");
            Npp.AddLine($"Passed {ii - tests_failed} tests.");
        }
    }
}
