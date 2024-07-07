using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
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
            public bool shouldValidate;

            public SchemaValidatesJson(string json, string schema, bool shouldValidate)
            {
                this.json = json;
                this.schema = schema;
                this.shouldValidate = shouldValidate;
            }
        }

        public static bool Test()
        {
            JsonParser jsonParser = new JsonParser();
            string[] basicJsons = new string[]{ "[]", "{}", "1", "0.5", "null", "false", "\"a\"" };
            string[] basicTypeSchemas = new string[]
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
            int testsFailed = 0;
            foreach (string basicJsonStr in basicJsons)
            {
                JNode basicJson = jsonParser.Parse(basicJsonStr);
                ii += 2;
                bool validates = JsonSchemaValidator.Validates(basicJson, new JObject(), 0, true, out List<JsonLint> lints);
                if (!validates && lints.Count > 0)
                {
                    testsFailed++;
                    Npp.AddLine($"Expected {basicJsonStr} to validate under empty schema {{}}, but instead got validation problem {lints[0]}");
                }
                validates = JsonSchemaValidator.Validates(basicJson, new JNode(true, Dtype.BOOL, 0), 0, true, out lints);
                if (!validates && lints.Count > 0)
                {
                    testsFailed++;
                    Npp.AddLine($"Expected {basicJsonStr} to validate under the trivial schema `true`, but instead got validation problem {lints[0]}");
                }
                foreach (string basicSchema in basicTypeSchemas)
                {
                    ii++;
                    JObject schema = (JObject)jsonParser.Parse(basicSchema);
                    var schemaType = JsonSchemaMaker.typeNameToDtype[(string)schema["type"].value];
                    bool shouldValidate = JsonSchemaValidator.TypeValidates(
                        basicJson.type, schemaType
                    );
                    validates = JsonSchemaValidator.Validates(basicJson, schema, 0, true, out lints);
                    if (shouldValidate && (!validates && lints.Count > 0))
                    {
                        testsFailed++;
                        Npp.AddLine($"Expected {basicJsonStr} validation under schema {basicSchema} to validate, but it returned {lints[0]}");
                    }
                    else if (!shouldValidate && (validates && lints.Count == 0))
                    {
                        testsFailed++;
                        Npp.AddLine($"Expected {basicJsonStr} validation under schema {basicSchema} to NOT validate, but it did validate");
                    }
                }
            }
            string objectAnyofSchema = "{\"type\": \"object\", \"properties\": " +
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
            var defsRefExampleSchema = "{" +
                "\"type\": \"object\"," +
                "\"definitions\": {" +
                    "\"foo\": {\"type\": \"array\", \"items\": {\"type\": \"number\"}}," +
                    "\"bar\": {\"type\": \"array\", \"items\": {\"type\": \"string\"}}" +
                "}," +
                "\"properties\": {}," +
                "\"patternProperties\": {" +
                    "\"foo\": {\"$ref\": \"#/definitions/foo\"}," +
                    "\"bar\": {\"$ref\": \"bar\"}" +
                // the "#/definitions/foo" syntax is probably more correct
                // but we should be prepared for both
                "}" +
            "}";
            var recursiveDefsRefSchema = "{" +
                "\"$defs\": {" +
                    "\"foo\": {" +
                        "\"type\": \"object\"," +
                        "\"properties\": {" +
                            "\"bar\": {\"type\": \"integer\"}, " +
                            "\"foo\": {\"anyOf\": [" +
                                "{\"type\": \"null\"}," +
                                "{\"$ref\": \"#/$defs/foo\"}" +
                            "]}" +
                        "}," +
                        "\"required\": [\"foo\", \"bar\"]" +
                    "}" +
                "}," +
                "\"$ref\": \"#/$defs/foo\"" +
            "}";
            var containsSchemaMinAndMax = "{" +
                "\"type\": \"array\"," +
                "\"items\": {\"type\": \"number\"}," +
                "\"contains\": {\"type\": \"string\"}," +
                "\"minContains\": 2, \"maxContains\": 4" +
            "}";
            var testcases = new List<SchemaValidatesJson>
            {
                /********
                 * maximum, minimum, exclusiveMaximum, exclusiveMinimum for numbers and integers
                ********/
                new SchemaValidatesJson("5", "{\"type\": \"number\", \"minimum\": 5, \"maximum\": 7, \"exclusiveMinimum\": -100, \"exclusiveMaximum\": 100}",
                    true // check that integer is validated as subset of number with min and max
                ),
                new SchemaValidatesJson("5.5", "{\"type\": \"integer\", \"minimum\": 5}",
                    false // check that schema type is still validated when minimum is in place
                ),
                new SchemaValidatesJson("5.5", "{\"type\": \"integer\", \"maximum\": 6}",
                    false // check that schema type is still validated when maximum is in place
                ),
                new SchemaValidatesJson("5.5", "{\"type\": \"integer\", \"minimum\": 4, \"maximum\": 6}",
                    false // check that schema type is still validated with minimum and maximum
                ),
                new SchemaValidatesJson("5", "{\"type\": \"integer\", \"minimum\": 5}",
                    true
                ),
                new SchemaValidatesJson("5", "{\"type\": \"integer\", \"maximum\": 6}",
                    true
                ),
                new SchemaValidatesJson("5", "{\"type\": \"integer\", \"minimum\": 4, \"maximum\": 6}",
                    true
                ),
                new SchemaValidatesJson("4", "{\"type\": \"integer\", \"minimum\": 5}",
                    false
                ),
                new SchemaValidatesJson("7", "{\"type\": \"integer\", \"maximum\": 6}",
                    false
                ),
                new SchemaValidatesJson("3", "{\"type\": \"integer\", \"minimum\": 4, \"maximum\": 6}",
                    false
                ),
                new SchemaValidatesJson("7", "{\"type\": \"integer\", \"minimum\": 4, \"maximum\": 6}",
                    false
                ),
                new SchemaValidatesJson("5.5", "{\"type\": \"integer\", \"exclusiveMinimum\": 5}",
                    false // check that schema type is still validated when exclusiveMinimum is in place
                ),
                new SchemaValidatesJson("5.5", "{\"type\": \"integer\", \"exclusiveMaximum\": 6}",
                    false // check that schema type is still validated when exclusiveMaximum is in place
                ),
                new SchemaValidatesJson("5.5", "{\"type\": \"integer\", \"exclusiveMinimum\": 4, \"exclusiveMaximum\": 6}",
                    false // check that schema type is still validated with exclusiveMinimum and exclusiveMaximum
                ),
                new SchemaValidatesJson("5", "{\"type\": \"integer\", \"exclusiveMinimum\": 4}",
                    true
                ),
                new SchemaValidatesJson("5", "{\"type\": \"integer\", \"exclusiveMaximum\": 6}",
                    true
                ),
                new SchemaValidatesJson("5", "{\"type\": \"integer\", \"exclusiveMinimum\": 4, \"exclusiveMaximum\": 6}",
                    true
                ),
                new SchemaValidatesJson("5", "{\"type\": \"integer\", \"exclusiveMinimum\": 5}",
                    false
                ),
                new SchemaValidatesJson("6", "{\"type\": \"integer\", \"exclusiveMaximum\": 6}",
                    false
                ),
                new SchemaValidatesJson("4", "{\"type\": \"integer\", \"exclusiveMinimum\": 4, \"exclusiveMaximum\": 6}",
                    false
                ),
                new SchemaValidatesJson("7", "{\"type\": \"integer\", \"exclusiveMinimum\": 4, \"exclusiveMaximum\": 6}",
                    false
                ),
                new SchemaValidatesJson("4", "{\"type\": \"integer\", \"exclusiveMinimum\": 5}",
                    false
                ),
                new SchemaValidatesJson("7", "{\"type\": \"integer\", \"exclusiveMaximum\": 6}",
                    false
                ),
                new SchemaValidatesJson("4", "{\"type\": \"integer\", \"minimum\": 4, \"exclusiveMaximum\": 6}",
                    true // mix and match minimum/maximum and exclusiveMinimum/exclusiveMaximum
                ),
                new SchemaValidatesJson("6", "{\"type\": \"integer\", \"exclusiveMinimum\": 4, \"maximum\": 6}",
                    true
                ),
                new SchemaValidatesJson("3", "{\"type\": \"integer\", \"minimum\": 4, \"exclusiveMaximum\": 6}",
                    false
                ),
                new SchemaValidatesJson("7", "{\"type\": \"integer\", \"exclusiveMinimum\": 4, \"maximum\": 6}",
                    false
                ),
                new SchemaValidatesJson("6", "{\"type\": \"integer\", \"minimum\": 4, \"exclusiveMaximum\": 6}",
                    false
                ),
                new SchemaValidatesJson("4", "{\"type\": \"integer\", \"exclusiveMinimum\": 4, \"maximum\": 6}",
                    false
                ),
                /***********
                 * arrays with mixed types
                 ***********/
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
                /***********
                 * enums
                 ***********/
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
                    "[\"one\", 1.0, \"two\", 2]",
                    "{\"type\": \"array\", \"items\": {\"enum\": [\"one\", \"two\", 1.0, 2]}}",
                    true // test enum with mixed types
                ),
                new SchemaValidatesJson(
                    "[\"one\", 1.0, \"two\", 2]",
                    "{\"type\": \"object\", \"items\": {\"enum\": [\"one\", \"three\", 1.0, 2]}}",
                    false // test enum with mixed types, where a value doesn't match
                ),
                new SchemaValidatesJson(
                    "[\"one\", 1, \"two\", 2]",
                    "{\"type\": \"object\", \"items\": {\"enum\": [\"one\", \"two\", 1.0, 2]}}",
                    false // test enum with mixed types, where a value is equal but has the wrong type
                ),
                /**
                 * minItems and maxItems keywords
                 **/
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
                /**
                 * misc tests
                 **/
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
                    objectAnyofSchema,
                    true // object with anyOf key where value matches first option
                ),
                new SchemaValidatesJson(
                    "{\"a\": {\"b\": 1}}",
                    objectAnyofSchema,
                    true // object with anyOf key where value matches middle option
                ),
                new SchemaValidatesJson(
                    "{\"a\": [1, 2.55]}",
                    objectAnyofSchema,
                    true // object with anyOf key where value matches last option
                ),
                new SchemaValidatesJson(
                    "{\"a\": 1.5}",
                    objectAnyofSchema,
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
                /*********************
                 * "patternProperties" keyword
                *********************/
                new SchemaValidatesJson(
                    "{\"a1\": 1.5, \"a2\": \"NA\", \"a3\": -7.3, \"foo\": true}",
                    "{" +
                        "\"type\": \"object\", " +
                        "\"patternProperties\": {" +
                            "\"a\\\\d+\": {\"type\": [\"number\", \"string\"]}" +
                        "}, " +
                        "\"properties\": {" +
                            "\"foo\": {\"type\": \"boolean\"}" +
                        "}," +
                        "\"required\": [\"foo\"]" +
                    "}", // test patternProperties with object that has pattern
                         // keys and non-pattern keys
                    true
                ),
                new SchemaValidatesJson(
                    "{\"foo\": true}",
                    "{" +
                        "\"type\": \"object\", " +
                        "\"patternProperties\": {" +
                            "\"a\\\\d+\": {\"type\": [\"number\", \"string\"]}" +
                        "}, " +
                        "\"properties\": {" +
                            "\"foo\": {\"type\": \"boolean\"}" +
                        "}," +
                        "\"required\": [\"foo\"]" +
                    "}", // test patternProperties with object that does not have 
                         // pattern keys
                    true
                ),
                new SchemaValidatesJson(
                    "{\"a1\": null, \"a2\": \"NA\", \"a3\": -7.3, \"foo\": true}",
                    "{" +
                        "\"type\": \"object\", " +
                        "\"patternProperties\": {" +
                            "\"a\\\\d+\": {\"type\": [\"number\", \"string\"]}" +
                        "}, " +
                        "\"properties\": {" +
                            "\"foo\": {\"type\": \"boolean\"}" +
                        "}," +
                        "\"required\": [\"foo\"]" +
                    "}", // test patternProperties with object that has pattern
                         // keys and non-pattern keys, and a pattern key
                         // violates its subschema
                    false
                ),
                new SchemaValidatesJson(
                    "{\"a1\": 1.5, \"a2\": \"NA\", \"a3\": -7.3, \"b1\": null}",
                    "{" +
                        "\"type\": \"object\", " +
                        "\"patternProperties\": {" +
                            "\"a\\\\d+\": {\"type\": [\"number\", \"string\"]}, " +
                            "\"b\\\\d+\": {\"type\": [\"null\", \"boolean\"]}" +
                        "}, " +
                        "\"properties\": {" +
                            "\"foo\": {\"type\": \"boolean\"}" +
                        "}" +
                    "}", // test patternProperties with object that has
                         // multiple patterns
                    true
                ),
                new SchemaValidatesJson(
                    "{\"a1\": 1.5, \"a2\": \"NA\", \"a3\": -7.3, \"b1\": \"bad\"}",
                    "{" +
                        "\"type\": \"object\", " +
                        "\"patternProperties\": {" +
                            "\"a\\\\d+\": {\"type\": [\"number\", \"string\"]}, " +
                            "\"b\\\\d+\": {\"type\": [\"null\", \"boolean\"]}" +
                        "}, " +
                        "\"properties\": {" +
                            "\"foo\": {\"type\": \"boolean\"}" +
                        "}" +
                    "}", // test patternProperties with object that has
                         // multiple patterns, and one pattern has a
                         // nonconforming key
                    false
                ),
                /*********************
                 * "pattern" keyword
                *********************/
                new SchemaValidatesJson(
                    "[\"abc\", \"abd\", \"a\", \"aa\"]",
                    "{" +
                        "\"type\": \"array\", " +
                        "\"items\": {" +
                            "\"type\": \"string\"," +
                            "\"pattern\": \"^a\"" +
                        "}" +
                    "}",
                    true
                ),
                new SchemaValidatesJson(
                    "[\"abc\", \"abd\", \"ca\", \"aa\"]",
                    "{" +
                        "\"type\": \"array\", " +
                        "\"items\": {" +
                            "\"type\": \"string\"," +
                            "\"pattern\": \"^a\"" +
                        "}" +
                    "}",
                    false
                ),
                new SchemaValidatesJson(
                    "[\"abc\", \"abd\", \"ca\", \"aa\"]",
                    "{" +
                        "\"type\": \"array\", " +
                        "\"items\": {" +
                            "\"anyOf\": [" +
                                "{\"type\": \"string\", \"pattern\": \"^a\"}," +
                                "{\"type\": \"string\", \"pattern\": \"^c\"}" +
                            "]" +
                        "}" +
                    "}",
                    true
                ),
                new SchemaValidatesJson(
                    "[\"abc\", \"abd\", \"ca\", \"aa\", 1]",
                    "{" +
                        "\"type\": \"array\", " +
                        "\"items\": {" +
                            "\"anyOf\": [" +
                                "{\"type\": \"string\", \"pattern\": \"^a\"}," +
                                "{\"type\": \"string\", \"pattern\": \"^c\"}" +
                            "]" +
                        "}" +
                    "}",
                    false
                ),
                new SchemaValidatesJson(
                    "{\"a\": \"<h1>adfdsfkdjfdkfj</h1>\", \"b\": 2.5}",
                    "{" +
                        "\"type\": \"object\"," +
                        "\"properties\": {" +
                            "\"a\": {\"type\": \"string\", \"pattern\": \"<(h\\\\d)>.*?</\\\\1>\"}" +
                        "}" +
                    "}",
                    true
                ),
                /*********************
                 * "$defs", "definitions", and "$ref" keywords
                *********************/
                new SchemaValidatesJson(
                    "{\"foo1\": [1], \"bar1\": [\"a\"], \"foo2\": [2.5], \"bar2\": [\"b\"]}",
                    defsRefExampleSchema,
                    true
                ),
                new SchemaValidatesJson(
                    "{\"foo1\": [null], \"bar1\": [\"a\"], \"foo2\": [2.5], \"bar2\": [\"b\"]}",
                    defsRefExampleSchema,
                    false
                ),
                /***** self-referential schemas with $defs and $ref ****/
                new SchemaValidatesJson(
                    "{\"bar\": 0, \"foo\": {\"bar\": 1, \"foo\": {\"bar\": 2, \"foo\": {\"bar\": 3, \"foo\": null}}}}",
                    recursiveDefsRefSchema,
                    true
                ),
                new SchemaValidatesJson( // recursion missing required key
                    "{\"bar\": 0, \"foo\": {\"baz\": 1, \"foo\": {\"bar\": 2, \"foo\": {\"bar\": 3, \"foo\": null}}}}",
                    recursiveDefsRefSchema,
                    false
                ),
                new SchemaValidatesJson( // base case of recursion is wrong
                    "{\"bar\": 0, \"foo\": {\"bar\": 1, \"foo\": {\"bar\": 2, \"foo\": {\"bar\": 3, \"foo\": 1}}}}",
                    recursiveDefsRefSchema,
                    false
                ),
                new SchemaValidatesJson( // a type in one of the recursions is wrong
                    "{\"bar\": 0, \"foo\": {\"bar\": 1.5, \"foo\": null}}",
                    recursiveDefsRefSchema,
                    false
                ),
                /*** contains, minContains, maxContains keywords ***/
                new SchemaValidatesJson(
                    "[1, 2, 3, \"\"]",
                    containsSchemaMinAndMax,
                    false
                ),
                new SchemaValidatesJson(
                    "[1, 2, 3, \"\", \"\"]",
                    containsSchemaMinAndMax,
                    true
                ),
                new SchemaValidatesJson(
                    "[1, 2, 3, \"\", \"\", \"\"]",
                    containsSchemaMinAndMax,
                    true
                ),
                new SchemaValidatesJson(
                    "[1, 2, 3, \"\", \"\", \"\", \"\", \"\"]",
                    containsSchemaMinAndMax,
                    false
                ),
                new SchemaValidatesJson(
                    "[1, 2, 3, \"\", \"\", \"\", \"\", \"\"]",
                    containsSchemaMinAndMax,
                    false
                ),
                new SchemaValidatesJson(
                    "[1, 2, 3]",
                    "{" +
                        "\"type\": \"array\"," +
                        "\"items\": {\"type\": \"number\"}," +
                        "\"contains\": {\"type\": \"string\"}," +
                        "\"maxContains\": 4" +
                    "}",
                    false
                ),
                new SchemaValidatesJson(
                    "[1, 2, 3, \"\"]",
                    "{" +
                        "\"type\": \"array\"," +
                        "\"items\": {\"type\": \"number\"}," +
                        "\"contains\": {\"type\": \"string\"}," +
                        "\"maxContains\": 4" +
                    "}",
                    true
                ),
                new SchemaValidatesJson(
                    "[1, 2, 3, \"\"]",
                    "{" +
                        "\"type\": \"array\"," +
                        "\"items\": {\"type\": \"number\"}," +
                        "\"contains\": {\"type\": \"string\"}," +
                        "\"minContains\": 3" +
                    "}",
                    false
                ),
                new SchemaValidatesJson(
                    "[1, 2, 3, \"\", \"\", \"\"]",
                    "{" +
                        "\"type\": \"array\"," +
                        "\"items\": {\"type\": \"number\"}," +
                        "\"contains\": {\"type\": \"string\"}," +
                        "\"minContains\": 3" +
                    "}",
                    true
                ),
                /*******
                 * minLength and maxLength string keywords
                 *******/
                new SchemaValidatesJson(
                    "\"abc\"", "{\"type\": \"string\", \"minLength\": 2}", true
                ),
                new SchemaValidatesJson(
                    "\"abc\"", "{\"type\": \"string\", \"minLength\": 4}", false
                ),
                new SchemaValidatesJson(
                    "\"abc\"", "{\"type\": \"string\", \"maxLength\": 2}", false
                ),
                new SchemaValidatesJson(
                    "\"abc\"", "{\"type\": \"string\", \"maxLength\": 4}", true
                ),
                new SchemaValidatesJson(
                    "\"abc\"", "{\"type\": \"string\", \"minLength\": 2, \"maxLength\": 4}", true
                ),
                new SchemaValidatesJson(
                    "\"abc\"", "{\"type\": \"string\", \"minLength\": 4, \"maxLength\": 5}", false
                ),
                new SchemaValidatesJson( // pattern and minLength
                    "\"abc\"", "{\"type\": \"string\", \"pattern\": \"[abc]+\", \"minLength\": 3}", true
                ),
                new SchemaValidatesJson(
                    "\"abc\"", "{\"type\": \"string\", \"pattern\": \"[abc]+\", \"minLength\": 4}", false
                ),
                new SchemaValidatesJson( // pattern and maxLength
                    "\"abc\"", "{\"type\": \"string\", \"pattern\": \"[abc]+\", \"maxLength\": 4}", true
                ),
                new SchemaValidatesJson(
                    "\"abc\"", "{\"type\": \"string\", \"pattern\": \"[abc]+\", \"maxLength\": 2}", false
                ),
                new SchemaValidatesJson( // minLength and maxLength are fine but pattern isn't
                    "\"abc\"", "{\"type\": \"string\", \"pattern\": \"z\", \"minLength\": 2, \"maxLength\": 4}", false
                ),
            };
            string randomTweetText = null, tweetSchemaText = null, badRandomTweetText = null;
            string testfilesPath = Path.Combine(Npp.pluginDllDirectory, "testfiles") + "\\";
            JNode badRandomTweet = null;
            try
            {
                randomTweetText = File.ReadAllText(testfilesPath + "random_tweet.json");
                // make a copy of the tweet that violates the schema
                badRandomTweet = jsonParser.Parse(randomTweetText);
            }
            catch (Exception ex)
            {
                Npp.AddLine($"While trying to parse {testfilesPath}random_tweet.json\r\n" +
                            $"got exception {ex}");
            }
            try
            {
                var rparser = new RemesParser();
                // set a value deep in the tweet to an invalid type
                rparser.Search("@[1].entities.media[1].sizes.small.w = `THIS SHOULD BE AN INTEGER`", badRandomTweet);
                badRandomTweetText = badRandomTweet.ToString(); badRandomTweet.ToString();
            }
            catch (Exception ex)
            {
                Npp.AddLine($"While trying to mutate the JSON from {testfilesPath}randomTweet.json\r\n" +
                            $"got exception {ex}");
            }
            try
            {
                tweetSchemaText = File.ReadAllText(testfilesPath + "tweet_schema.json");
            }
            catch
            {
                Npp.AddLine($"File not found at {testfilesPath}tweet_schema.json");
            }
            if (tweetSchemaText != null && randomTweetText != null)
            {
                testcases.Add(new SchemaValidatesJson(randomTweetText, tweetSchemaText, true));
                if (badRandomTweetText != null)
                    testcases.Add(new SchemaValidatesJson(badRandomTweetText, tweetSchemaText, false));
            }
            string kitchenSinkExample = null, kitchenSinkSchema = null;
            try
            {
                kitchenSinkSchema = File.ReadAllText(testfilesPath + "small\\kitchen_sink_schema.json");
                kitchenSinkExample = File.ReadAllText(testfilesPath + "small\\kitchen_sink_example.json");
            }
            catch
            {
                Npp.AddLine("Kitchen sink schema or example not found");
            }
            if (kitchenSinkSchema != null && kitchenSinkExample != null)
            {
                testcases.Add(new SchemaValidatesJson(kitchenSinkExample, kitchenSinkSchema, true));
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
                    bool validates = JsonSchemaValidator.Validates(json, schema, int.MaxValue, true, out List<JsonLint> lints);
                    if (!validates)
                    {
                        ii++;
                        for (int jj = 0; jj < lints.Count; jj++)
                        {
                            try
                            {
                                JsonLint lint = lints[jj];
                                lint.ToString();
                            }
                            catch (Exception ex_ToString)
                            {
                                testsFailed++;
                                Npp.AddLine($"Validation of {test.json} failed under schema {test.schema}, but calling the ToString method of lint {ii} in its returned List<JsonLint> raised exception {ex_ToString}");
                            }
                        }
                    }
                    // vp should be null if and only if test.shouldValidate is true
                    if (test.shouldValidate && !validates)
                    {
                        testsFailed++;
                        Npp.AddLine($"Expected {test.json} validation under {test.schema} to return null, but instead gave validation problem(s)\r\n{JsonSchemaValidator.LintsAsJArrayString(lints)}");
                    }
                    else if (!test.shouldValidate && validates)
                    {
                        testsFailed++;
                        Npp.AddLine($"Expected {test.json} validation under schema {test.schema} to return {test.shouldValidate}, but instead gave no validation problem(s).");
                    }
                }
                catch (Exception e)
                {
                    testsFailed++;
                    Npp.AddLine($"Expected {test.json} validation under schema {test.schema} to return {test.shouldValidate}, but it raised exception {e}");
                }
            }
            Npp.AddLine($"Failed {testsFailed} tests.");
            Npp.AddLine($"Passed {ii - testsFailed} tests.");
            return testsFailed > 0;
        }
    }
}
