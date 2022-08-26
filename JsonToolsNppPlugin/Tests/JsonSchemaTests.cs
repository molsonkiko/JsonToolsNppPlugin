using System;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;
using Kbg.NppPluginNET.PluginInfrastructure;

namespace JSON_Tools.Tests
{
    public class JsonSchemaMakerTester
    {
        public static void Test()
        {
            JsonParser jsonParser = new JsonParser();
            JsonSchemaMaker sch_maker = new JsonSchemaMaker();
            string[][] testcases = new string[][]
            {
                new string[]{ "[1, \"1\"]", "{\"type\": \"array\", \"items\": {\"type\": [\"integer\", \"string\"]}}" },
                new string[]{ "{\"a\": 1}", "{\"type\": \"object\", \"properties\": {\"a\": {\"type\": \"integer\"}}, \"required\": [\"a\"]}" },
                new string[]{ "[{\"a\": 1, \"b\": \"w\", \"c\": 1.0}, " +
                "{\"a\": \"2\", \"b\": \"v\"}]",
                    "{\"type\": \"array\", " +
                    "\"items\": " +
                        "{\"type\": \"object\", " +
                        "\"properties\": {" +
                                "\"a\": {\"type\": [\"integer\", \"string\"]}," +
                                "\"b\": {\"type\": \"string\"}," +
                                "\"c\": {\"type\": \"number\"}" +
                            "}," +
                         "\"required\": [\"a\", \"b\"]" +
                         "}" +
                     "}"
                },
                new string[]{"[[1, 2.0, {\"a\": 1}]]",
                    "{" +
                    "\"type\": \"array\"," +
                    "\"items\": " +
                        "{" +
                        "\"type\":\"array\"," +
                        "\"items\": " +
                            "{" +
                            "\"anyOf\": " +
                                "[" +
                                    "{\"type\": \"integer\"}," +
                                    "{\"type\": \"number\"}," +
                                    "{" +
                                        "\"type\": \"object\", " +
                                        "\"properties\": " +
                                            "{\"a\": {\"type\": \"integer\"}}, " +
                                        "\"required\": [\"a\"]" +
                                     "}" +
                                "]" +
                            "}" +
                        "}" +
                    "}"
                },
                new string[]{"[" +
                    "{" +
                        "\"a\": 3" +
                    "}," +
                    "{" +
                        "\"a\": 1," +
                        "\"b\": [" +
                            "{" +
                                "\"a\": {" +
                                    "\"b\": 1," +
                                    "\"c\": 2" +
                                "}" +
                            "}," +
                            "{" +
                                "\"a\": {" +
                                    "\"b\": 1" +
                                "}" +
                            "}" +
                        "]" +
                    "}," +
                    "{" +
                        "\"a\": 1," +
                        "\"b\": [" +
                            "{" +
                                "\"a\": {" +
                                    "\"b\": 1," +
                                    "\"c\": 2," +
                                    "\"d\": 3" +
                                "}" +
                            "}" +
                        "]" +
                    "}," +
                    "{" +
                        "\"a\": 2," +
                        "\"c\": 3" +
                    "}" +
                "]", // nested JSON object
                "{" +
                    "\"$schema\": \"http://json-schema.org/schema#\"," +
                    "\"type\": \"array\"," +
                    "\"items\": {" +
                        "\"type\": \"object\"," +
                        "\"properties\": {" +
                            "\"a\": {" +
                                "\"type\": \"integer\"" +
                            "}," +
                            "\"b\": {" +
                                "\"type\": \"array\"," +
                                "\"items\": {" +
                                    "\"type\": \"object\"," +
                                    "\"properties\": {" +
                                        "\"a\": {" +
                                            "\"type\": \"object\"," +
                                            "\"properties\": {" +
                                                "\"b\": {" +
                                                    "\"type\": \"integer\"" +
                                                "}," +
                                                "\"c\": {" +
                                                    "\"type\": \"integer\"" +
                                                "}," +
                                                "\"d\": {" +
                                                    "\"type\": \"integer\"" +
                                                "}" +
                                            "}," +
                                            "\"required\": [" +
                                                "\"b\"" +
                                            "]" +
                                        "}" +
                                    "}," +
                                    "\"required\": [" +
                                        "\"a\"" +
                                    "]" +
                                "}" +
                            "}," +
                            "\"c\": {" +
                                "\"type\": \"integer\"" +
                            "}" +
                        "}," +
                        "\"required\": [" +
                            "\"a\"" +
                        "]" +
                    "}" +
                "}"
                }, // nested JSON object schema
            };
            int ii = 0;
            int tests_failed = 0;
            JObject base_schema_j = (JObject)sch_maker.SchemaToJNode(JsonSchemaMaker.BASE_SCHEMA);
            foreach (string[] test in testcases)
            {
                string inp = test[0];
                string desired_out = test[1];
                ii++;
                JNode jinp = jsonParser.Parse(inp);
                JObject desired_schema = (JObject)jsonParser.Parse(desired_out);
                foreach (string k in base_schema_j.children.Keys)
                {
                    desired_schema[k] = base_schema_j[k];
                }
                string desired_sch_str = desired_schema.ToString();
                JNode schema = new JNode(null, Dtype.NULL, 0);
                try
                {
                    schema = sch_maker.GetSchema(jinp);
                    try
                    {
                        if (!schema.Equals(desired_schema))
                        {
                            tests_failed++;
                            Npp.AddLine($"Expected the schema for {inp} to be\n{desired_sch_str}\nInstead got\n{schema.ToString()}");
                        }
                    }
                    catch
                    {
                        // probably because of something like trying to compare an array to a non-array
                        tests_failed++;
                        Npp.AddLine($"Expected the schema for {inp} to be\n{desired_sch_str}\nInstead got {schema.ToString()}");
                    }
                }
                catch (Exception e)
                {
                    tests_failed++;
                    Npp.AddLine($"Expected the schema for {jinp} to be\n{desired_sch_str}\nInstead raised exception {e}");
                }
            }
            Npp.AddLine($"Failed {tests_failed} tests.");
            Npp.AddLine($"Passed {ii - tests_failed} tests.");
        }
    }
}
