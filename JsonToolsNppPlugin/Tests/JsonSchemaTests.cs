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
                            "{\"a\": {\"b\": 1, \"c\": 2}}," +
                            "{\"a\": {\"b\": 1}}" +
                        "]" +
                    "}," +
                    "{" +
                        "\"a\": 1," +
                        "\"b\": [" +
                            "{\"a\": {\"b\": 1, \"c\": 2, \"d\": 3}}" +
                        "]" +
                    "}," +
                    "{" +
                        "\"a\": 2," +
                        "\"c\": 3" +
                    "}" +
                "]", // array of objects of arrays of object with variable # of keys
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
                new string[]{
                    "[[null, 1], [null, [\"a\"]], [1, {\"a\": 3.5}]]",
                    // make sure we don't get duplicate scalar types in anyOf
                    // also make sure the scalar->array->object order of anyOf
                    // schemas is respected
                    "{\"type\": \"array\", \"items\": {" +
                        "\"items\":" +
                            "{\"anyOf\": [" +
                                    "{\"type\": [\"integer\", \"null\"]}," +
                                    "{" +
                                        "\"type\" : \"array\"," +
                                        "\"items\": {\"type\": \"string\"}" +
                                    "}," +
                                    "{" +
                                        "\"type\": \"object\"," +
                                        "\"properties\": {" +
                                            "\"a\": {\"type\": \"number\"}" +
                                        "}," +
                                        "\"required\": [\"a\"]" +
                                    "}" +
                                "]" +
                             "}," +
                        "\"type\": \"array\"}}"
                },
                // empty arrays should have no "items" keyword
                new string[]{ "[]", "{\"type\": \"array\"}" },
                // empty objects should have no "properties" or "required" keywords
                new string[]{ "{}", "{\"type\": \"object\"}" },
                new string[]{
                    // array that sometimes contains arrays
                    // and the subarrays sometimes contain objects
                    // and the subsubobjects contain variable numbers of keys
                    // including sometimes subsubsubobjects
                    "[" +
                        "[1,     {\"a\": 1, \"b\": 2}]," +
                        "[3.5,   {       \"c\": 4}]," +
                        "[\"6\", {\"a\": {\"d\": false, \"e\": []}}]," +
                        "[7,     {\"a\": {\"d\": true}}]" +
                    "]",
                    "{" +
                        "\"type\": \"array\"," +
                        "\"items\": {" +
                            "\"type\": \"array\"," +
                            "\"items\": {\"anyOf\": [" +
                                "{\"type\": [\"number\", \"string\"]}," +
                                "{" +
                                    "\"type\": \"object\"," +
                                    "\"required\": []," +
                                    "\"properties\": {" +
                                        "\"a\": {" +
                                            "\"anyOf\": [" +
                                                "{\"type\": \"integer\"}," +
                                                "{" +
                                                    "\"type\": \"object\"," +
                                                    "\"properties\": {" +
                                                        "\"d\": {\"type\": \"boolean\"}," +
                                                        "\"e\": {\"type\": \"array\"}" +
                                                    "}," +
                                                    "\"required\": [\"d\"]" +
                                                "}" +
                                            "]" +
                                        "}," +
                                        "\"b\": {\"type\": \"integer\"}," +
                                        "\"c\": {\"type\": \"integer\"}" +
                                    "}" +
                                "}" +
                            "]}" +
                        "}" +
                    "}"
                },
            };
            int ii = 0;
            int tests_failed = 0;
            JObject base_schema_j = (JObject)JsonSchemaMaker.SchemaToJNode(JsonSchemaMaker.BASE_SCHEMA);
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
                JNode schema = new JNode();
                try
                {
                    schema = JsonSchemaMaker.GetSchema(jinp);
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
                    Npp.AddLine($"Expected the schema for {inp} to be\n{desired_sch_str}\nInstead raised exception {e}");
                }
            }
            Npp.AddLine($"Failed {tests_failed} tests.");
            Npp.AddLine($"Passed {ii - tests_failed} tests.");
        }
    }
}
