using System;
using System.Collections.Generic;
using System.IO;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;

namespace JSON_Tools.Tests
{
    public class JsonSchemaValidatorTester
    {
        struct SchemaValidatesJson
        {
            public string json;
            public string schema;
            public bool should_validate;

            public SchemaValidatesJson(string json, string schema, bool should_validate)
            {
                this.json = json;
                this.schema = schema;
                this.should_validate = should_validate;
            }
        }

        public static void Test()
        {
            JsonParser jsonParser = new JsonParser();
            string[] basic_jsons = new string[]{ "[]", "{}", "1", "0.5", "null", "false", "\"a\"" };
            string[] basic_type_schemas = new string[]
            {
                "{\"type\": \"array\"}",
                "{\"type\": \"boolean\"}",
                "{\"type\": \"integer\"}",
                "{\"type\": \"null\"}",
                "{\"type\": \"number\"}",
                "{\"type\": \"object\"}",
                "{\"type\": \"string\"}",
            };
            int ii = 0;
            int tests_failed = 0;
            foreach (string basic_json_str in basic_jsons)
            {
                JNode basic_json = jsonParser.Parse(basic_json_str);
                ii += 2;
                if (!JsonSchemaValidator.Validates(basic_json, new JObject(), out var _))
                {
                    tests_failed++;
                    Npp.AddLine($"Expected {basic_json_str} to validate under empty schema {{}}, but it didn't");
                }
                if (!JsonSchemaValidator.Validates(basic_json, new JNode(true, Dtype.BOOL, 0), out var _))
                {
                    tests_failed++;
                    Npp.AddLine($"Expected {basic_json_str} to validate under the trivial schema `true`, but it didn't");
                }
                foreach (string basic_schema in basic_type_schemas)
                {
                    ii++;
                    JObject schema = (JObject)jsonParser.Parse(basic_schema);
                    string schema_type = (string)schema["type"].value;
                    string json_type = JsonSchemaMaker.TypeName(basic_json.type);
                    bool should_validate = JsonSchemaValidator.TypeValidates(
                        json_type, schema_type
                    );
                    bool validates = JsonSchemaValidator.Validates(basic_json, schema, out var _);
                    if (should_validate != validates)
                    {
                        tests_failed++;
                        Npp.AddLine($"Expected {basic_json_str} validation under schema {basic_schema} to return {should_validate}, but it returned {validates}");
                    }

                }
            }
            string object_anyof_schema = "{\"type\": \"object\", \"properties\": " +
                "{\"a\":" +
                    "{\"anyOf\": [" +
                        "{\"type\": [\"string\", \"integer\"]}," +
                        "{\"type\": \"object\", " +
                            "\"properties\": {\"b\": {\"type\": \"integer\"}}" +
                        "}," +
                        "{\"type\": \"array\", \"items\": {\"type\": \"number\"}}" +
                    "]}" +
                "}" +
            "}"; // matches an object with key a and a value that's an int array, a string, an integer, or an object with key b and int value
            var testcases = new List<SchemaValidatesJson>
            {
                new SchemaValidatesJson(
                    "[1, 2]", "{\"type\": \"array\", \"items\": {\"type\": \"integer\"}}",
                    true // test that int array validates under int array schema 
                ),
                new SchemaValidatesJson(
                    "[0.5]", "{\"type\": \"array\", \"items\": {\"type\": \"integer\"}}",
                    false // test that float array doesn't validate under int array schema
                ),
                new SchemaValidatesJson(
                    "[-0.5, 0, 1]", "{\"type\": \"array\", \"items\": {\"type\": \"number\"}}",
                    true // test that mixed number array validates under number array schema
                ),
                new SchemaValidatesJson(
                    "[-0.5, 0, 1]", "{\"type\": \"array\", \"items\": {\"type\": \"integer\"}}",
                    false // test that mixed number array doesn't validate under int array schema
                ),
                new SchemaValidatesJson(
                    "[\"a\", 1, 1.5]",
                    "{\"type\": \"array\", \"items\": {\"type\": [\"number\", \"string\"]}}",
                    true // test that mixed-scalar-type array validates against matching array of scalar types in schema
                ),
                new SchemaValidatesJson(
                    "[\"a\", null, 1.5]",
                    "{\"type\": \"array\", \"items\": {\"type\": [\"number\", \"string\"]}}",
                    false // test that mixed-scalar-type array DOES NOT validate against matching array of scalar types in schema
                ),
                new SchemaValidatesJson(
                    "[0, 1, 2]",
                    "{\"type\": \"array\", \"items\": {\"type\": \"integer\", \"enum\": [0, 1, 2]}}",
                    true // test integer enum with valid values
                ),
                new SchemaValidatesJson(
                    "[0, 1, 2, 3]",
                    "{\"type\": \"array\", \"items\": {\"type\": \"integer\", \"enum\": [0, 1, 2]}}",
                    false // test integer enum with invalid values
                ),
                new SchemaValidatesJson(
                    "[\"one\", \"two\"]",
                    "{\"type\": \"array\", \"items\": {\"type\": \"string\", \"enum\": [\"one\", \"two\"]}}",
                    true // test string enum with valid values
                ),
                new SchemaValidatesJson(
                    "[\"one\", \"a\"]",
                    "{\"type\": \"array\", \"items\": {\"type\": \"string\", \"enum\": [\"one\", \"two\"]}}",
                    false // test string enum with invalid values
                ),
                new SchemaValidatesJson(
                    "[\"one\", 1]",
                    "{\"type\": \"array\", \"items\": {\"type\": \"string\", \"enum\": [\"one\", \"two\"]}}",
                    false // test string enum with non-string values
                ),
                new SchemaValidatesJson(
                    "[1, 2, 3]",
                    "{\"type\": \"array\", \"items\": {\"type\": \"integer\"}, \"minItems\": 1}",
                    true // test minItems with valid length
                ),
                new SchemaValidatesJson(
                    "[]",
                    "{\"type\": \"array\", \"items\": {\"type\": \"integer\"}, \"minItems\": 1}",
                    false // test minItems with invalid length
                ),
                new SchemaValidatesJson(
                    "[1]",
                    "{\"type\": \"array\", \"items\": {\"type\": \"integer\"}, \"minItems\": 1}",
                    true // test minItems with exactly valid length
                ),
                new SchemaValidatesJson(
                    "[1, 2, 3]",
                    "{\"type\": \"array\", \"items\": {\"type\": \"integer\"}, \"maxItems\": 4}",
                    true // test maxItems with valid length
                ),
                new SchemaValidatesJson(
                    "[1, 2, 3, 4, 5]",
                    "{\"type\": \"array\", \"items\": {\"type\": \"integer\"}, \"maxItems\": 4}",
                    false // test maxItems with invalid length
                ),
                new SchemaValidatesJson(
                    "[1, 2, 3, 4]",
                    "{\"type\": \"array\", \"items\": {\"type\": \"integer\"}, \"maxItems\": 4}",
                    true // test maxItems with exactly valid length
                ),
                new SchemaValidatesJson(
                    "[1, 2, [3, 4.0], \"5\"]",
                    "{\"type\": \"array\", \"items\": {\"anyOf\": [{\"type\": [\"string\", \"integer\"]}, {\"type\": \"array\", \"items\": {\"type\": \"number\"}}]}}",
                    true // anyOf where all values validate
                ),
                new SchemaValidatesJson(
                    "[1, null, [3, 4.0], \"5\"]",
                    "{\"type\": \"array\", \"items\": {\"anyOf\": [{\"type\": [\"string\", \"integer\"]}, {\"type\": \"array\", \"items\": {\"type\": \"number\"}}]}}",
                    false // anyOf where an item does not validate
                ),
                new SchemaValidatesJson(
                    "{\"d\": \"baz\", \"e\": 1.5}",
                    "{\"type\": \"object\", \"properties\": {\"d\": {\"type\": \"string\"}, \"e\": {\"type\": \"number\"}}}",
                    true // simple object properties with valid stuff
                ),
                new SchemaValidatesJson(
                    "{\"d\": \"baz\", \"e\": \"blah\"}",
                    "{\"type\": \"object\", \"properties\": {\"d\": {\"type\": \"string\"}, \"e\": {\"type\": \"number\"}}}",
                    false // simple object properties with invalid stuff
                ),
                new SchemaValidatesJson(
                    "{\"d\": \"baz\", \"e\": 1.5}",
                    "{\"type\": \"object\", " +
                    "\"properties\": {" +
                        "\"d\": {\"type\": \"string\", \"enum\": [\"foo\", \"bar\", \"baz\"]}, " +
                        "\"e\": {\"type\": \"number\"}" +
                    "}}",
                    true // simple object with enum value
                ),
                new SchemaValidatesJson(
                    "{\"d\": \"rerno\", \"e\": 1.5}",
                    "{\"type\": \"object\", " +
                    "\"properties\": {" +
                        "\"d\": {\"type\": \"string\", \"enum\": [\"foo\", \"bar\", \"baz\"]}, " +
                        "\"e\": {\"type\": \"number\"}" +
                    "}}",
                    false // simple object with bad enum value
                ),
                new SchemaValidatesJson(
                    "{\"d\": \"baz\", \"e\": 1.5}",
                    "{\"type\": \"object\", " +
                    "\"properties\": {" +
                        "\"d\": {\"type\": \"string\"}, " +
                        "\"e\": {\"type\": \"number\"}," +
                        "\"f\": {\"type\": \"integer\"}" +
                    "}}",
                    true // simple object missing non-required property in schema
                ),
                new SchemaValidatesJson(
                    "{\"d\": \"baz\", \"e\": 1.5, \"f\": 3}",
                    "{\"type\": \"object\", \"properties\": {\"d\": {\"type\": \"string\"}, \"e\": {\"type\": \"number\"}}}",
                    true // simple object with properties not validated by schema
                ),
                new SchemaValidatesJson(
                    "{\"d\": \"baz\", \"e\": 1.5}",
                    "{\"type\": \"object\", " +
                    "\"properties\": {" +
                        "\"d\": {\"type\": \"string\"}, " +
                        "\"e\": {\"type\": \"number\"}," +
                        "\"f\": {\"type\": \"integer\"}" +
                    "}," +
                    "\"required\": [\"d\", \"e\", \"f\"]}",
                    false // simple object missing required property in schema
                ),
                new SchemaValidatesJson(
                    "{\"d\": \"baz\", \"e\": 1.5}",
                    "{\"type\": \"object\", " +
                    "\"properties\": {" +
                        "\"d\": {\"type\": \"string\"}, " +
                        "\"e\": {\"type\": \"number\"}," +
                        "\"f\": {\"type\": \"integer\"}" +
                    "}," +
                    "\"required\": [\"d\", \"e\"]}",
                    true // simple object with all required properties in schema
                ),
                new SchemaValidatesJson(
                    "{\"d\": \"baz\", \"e\": \"blah\"}",
                    "{\"type\": \"object\", " +
                    "\"properties\": {" +
                        "\"d\": {\"type\": \"string\"}, " +
                        "\"e\": {\"type\": \"number\"}," +
                        "\"f\": {\"type\": \"integer\"}" +
                    "}," +
                    "\"required\": [\"d\", \"e\"]}",
                    false // simple object with all required properties in schema, but some required property has the wrong type
                ),
                new SchemaValidatesJson(
                    "{\"d\": \"baz\", \"e\": 1.5}",
                    "{\"type\": \"object\", " +
                    "\"properties\": {" +
                        "\"d\": {\"type\": \"string\"}, " +
                        "\"f\": {\"type\": \"integer\"}" +
                    "}," +
                    "\"required\": [\"d\"]}",
                    true // simple object with all required properties in schema and some non-validated properties
                ),
                new SchemaValidatesJson(
                    "{\"a\": 1}",
                    object_anyof_schema,
                    true // object with anyOf key where value matches first option
                ),
                new SchemaValidatesJson(
                    "{\"a\": {\"b\": 1}}",
                    object_anyof_schema,
                    true // object with anyOf key where value matches middle option
                ),
                new SchemaValidatesJson(
                    "{\"a\": [1, 2.55]}",
                    object_anyof_schema,
                    true // object with anyOf key where value matches last option
                ),
                new SchemaValidatesJson(
                    "{\"a\": 1.5}",
                    object_anyof_schema,
                    false // object with anyOf key where value does not match any option
                ),
                new SchemaValidatesJson(
                    "{\"a\": 1, \"b\": [\"foo\", \"bar\"], \"c\": {\"d\": \"baz\", \"e\": 1.5}}",
                    "{" +
                        "\"type\": \"object\", " +
                        "\"properties\": {" +
                            "\"a\": {\"type\": \"integer\"}, " +
                            "\"b\": {\"type\": \"array\", \"items\": {\"type\": \"string\"}}, " +
                            "\"c\": {" +
                                "\"type\": \"object\", \"properties\": {" +
                                    "\"d\": {\"type\": \"string\"}, \"e\": {\"type\": \"number\"}" +
                                "}" +
                            "}" +
                        "}" +
                    "}",
                    true // objects and arrays inside object
                ),
                new SchemaValidatesJson(
                    "{\"a\": 1, \"b\": [\"foo\", \"bar\"], \"c\": 1}",
                    "{" +
                        "\"type\": \"object\", " +
                        "\"properties\": {" +
                            "\"a\": {\"type\": \"integer\"}, " +
                            "\"b\": {\"type\": \"array\", \"items\": {\"type\": \"string\"}}, " +
                            "\"c\": {" +
                                "\"type\": \"object\", \"properties\": {" +
                                    "\"d\": {\"type\": \"string\"}, \"e\": {\"type\": \"number\"}" +
                                "}" +
                            "}" +
                        "}" +
                    "}",
                    false // objects expected inside object, but got number
                ),
                new SchemaValidatesJson(
                    "[{\"a\": 1}, {\"a\": 1, \"b\": 2}]",
                    "{" +
                        "\"type\": \"array\", " +
                        "\"items\": {" +
                            "\"type\": \"object\", " +
                            "\"properties\": {" +
                                "\"a\": {\"type\": \"number\"}, " +
                                "\"b\": {\"type\": \"number\"}" +
                            "}, " +
                            "\"required\": [\"a\"]" +
                        "}" +
                    "}",
                    true // objects in array
                ),
                new SchemaValidatesJson(
                    "[{\"a\": 1}, null]",
                    "{" +
                        "\"type\": \"array\", " +
                        "\"items\": {" +
                            "\"type\": \"object\", " +
                            "\"properties\": {" +
                                "\"a\": {\"type\": \"number\"}, " +
                                "\"b\": {\"type\": \"number\"}" +
                            "}, " +
                            "\"required\": [\"a\"]" +
                        "}" +
                    "}",
                    false // object expected in array, but got number
                ),
            };
            string random_tweet_text = null, tweet_schema_text = null;
            string testfiles_path = @"plugins\JsonTools\testfiles\";
            try
            {
                random_tweet_text = File.ReadAllText(testfiles_path + "random_tweet.json");
            }
            catch
            {
                Npp.AddLine($"File not found at {testfiles_path}random_tweet.json");
            }
            try
            {
                tweet_schema_text = File.ReadAllText(testfiles_path + "tweet_schema.json");
            }
            catch
            {
                Npp.AddLine($"File not found at {testfiles_path}tweet_schema.json");
            }
            if (tweet_schema_text != null && random_tweet_text != null)
            {
                testcases.Add(new SchemaValidatesJson(random_tweet_text, tweet_schema_text, true));
            }
            foreach (SchemaValidatesJson test in testcases)
            {
                JNode json = new JNode(), schema = new JNode();
                try
                {
                    json = jsonParser.Parse(test.json);
                }
                catch (Exception ex)
                {
                    Npp.AddLine($"Failed to parse testing JSON {test.json}, got error {ex}");
                    continue;
                }
                try
                {
                    schema = jsonParser.Parse(test.schema);
                }
                catch (Exception ex)
                {
                    Npp.AddLine($"Failed to parse schema {test.schema}, got error {ex}");
                    continue;
                }
                ii++;
                try
                {
                    bool validates = JsonSchemaValidator.Validates(json, schema, out JsonSchemaValidator.ValidationProblem? vp);
                    if (!validates)
                    {
                        ii++;
                        if (vp == null)
                        {
                            tests_failed++;
                            Npp.AddLine($"Validation of {test.json} failed under schema {test.schema}, but the ValidationProblem was null");
                        }
                        else
                        {
                            try
                            {
                                vp.ToString();
                            }
                            catch (Exception ex_ToString)
                            {
                                tests_failed++;
                                Npp.AddLine($"Validation of {test.json} failed under schema {test.schema}, but calling the ToString() method of its ValidationProblem's raised exception {ex_ToString}");
                            }
                        }
                    }
                    if (validates != test.should_validate)
                    {
                        tests_failed++;
                        Npp.AddLine($"Expected {test.json} validation under schema {test.schema} to return {test.should_validate}, but instead returned {validates} with ValidationProblem\n{vp}");
                    }
                }
                catch (Exception e)
                {
                    tests_failed++;
                    Npp.AddLine($"Expected {test.json} validation under schema {test.schema} to return {test.should_validate}, but it raised exception {e}");
                }
            }
            Npp.AddLine($"Failed {tests_failed} tests.");
            Npp.AddLine($"Passed {ii - tests_failed} tests.");
        }
    }
}
