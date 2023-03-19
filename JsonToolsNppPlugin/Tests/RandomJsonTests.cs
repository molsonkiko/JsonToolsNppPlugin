using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JSON_Tools.Tests
{
    internal class RandomJsonTests
    {
        public static void TestRandomJson()
        {
            int ii = 0;
            int tests_failed = 0;
            JsonParser parser = new JsonParser();
            string[] tests = new string[]
            {
                "[1, \"a\"]",
                "{\"a\": 1, \"b\": 2.5, \"c\": [1.5, 2.3, 7.8]}",
                "[{\"a\": 1, \"b\": \"a\"}, {\"a\": \"foo\"}]"
            };

            foreach (string test in tests)
            {
                JNode node;
                try
                {
                    node = parser.Parse(test);
                }
                catch (Exception ex)
                {
                    Npp.AddLine($"Couldn't parse JSON, got error {ex}");
                    continue;
                }
                JObject schema;
                try
                {
                    schema = (JObject)JsonSchemaMaker.GetSchema(node);
                }
                catch (Exception ex2)
                {
                    Npp.AddLine($"Couldn't make schema from {test}, got error:\n{ex2}");
                    continue;
                }
                HashSet<string> required = new HashSet<string>();
                HashSet<string> allKeys = new HashSet<string>();
                string allRequiredKeyString = "";
                string allKeyString = "";
                if (node is JObject)
                {
                    // three extra tests:
                    // 1. all required keys must be present
                    // 2. No keys not in schema can be present
                    // 3. make sure that random objects have a random number of keys (if required is a proper subset of all possible keys)
                    // 4. Make sure that random objects sometimes have every possible key
                    ii += 3;
                    try
                    {
                        JArray required_arr = (JArray)schema["required"];
                        required_arr.children.RemoveAt(0);
                        schema["required"] = required_arr;
                        foreach (string possibleKey in ((JObject)schema["properties"]).children.Keys)
                        {
                            allKeys.Add((string)possibleKey);
                        }
                        foreach (JNode keynode in required_arr.children)
                        {
                            required.Add((string)keynode.value);
                        }
                    }
                    catch
                    {
                        Npp.AddLine("'required' keyword or 'properties' keyword missing from object schema");
                        continue;
                    }
                    allRequiredKeyString = string.Join(", ", required);
                    allKeyString = string.Join(", ", allKeys);
                    //Npp.AddLine($"~~~~~~~~~~~~~~~~~~~~~~~~~~~\nOBJECT SCHEMA\n{schema.ToString()}\n~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
                }
                var rands = new List<JNode>();
                ii++;
                // make some random JSON without extended ASCII
                for (int rand_num = 0; rand_num < 50; rand_num++)
                {
                    try
                    {
                        rands.Add(RandomJsonFromSchema.RandomJson(schema, 1, 4, false));
                    }
                    catch (Exception ex3)
                    {
                        tests_failed++;
                        Npp.AddLine($"Couldn't generate random JSON from schema of {test}, got error:\n{ex3}");
                        break;
                    }
                }
                ii++;
                // also test with extended ASCII allowed
                for (int rand_num = 0; rand_num < 50; rand_num++)
                {
                    try
                    {
                        rands.Add(RandomJsonFromSchema.RandomJson(schema, 1, 4, false));
                    }
                    catch (Exception ex3)
                    {
                        tests_failed++;
                        Npp.AddLine($"Couldn't generate random JSON from schema of {test} with extended ASCII allowed, got error:\n{ex3}");
                        break;
                    }
                }
                ii += 2;
                List<int> keycounts = new List<int>();
                foreach (JNode rand in rands)
                {
                    // make sure all random JSON validates under the schema
                    var vp = JsonSchemaValidator.Validates(rand, schema);
                    if (vp != null)
                    {
                        tests_failed++;
                        Npp.AddLine($"Random JSON\n{rand.ToString()}\ncould not be validated by schema\n{schema}\nGot validation problem:\n{vp.ToString()}");
                        break;
                    }
                    // test that the minArrayLength and maxArrayLength args to RandomJson do what they're supposed to
                    if (rand is JArray arr && (arr.Length > 4 || arr.Length < 1))
                    {
                        tests_failed++;
                        Npp.AddLine($"Random JSON\n{rand.ToString()}\nshould have had length betweeen 1 and 4, had length {arr.Length}");
                        break;
                    }
                    else if (rand is JObject obj)
                    {
                        //Npp.AddLine(rand.ToString());
                        keycounts.Add(obj.Length);
                        HashSet<string> randKeys = new HashSet<string>();
                        bool failure = false;
                        foreach (string key in obj.children.Keys)
                        {
                            randKeys.Add(key);
                            if (!allKeys.Contains(key))
                            {
                                tests_failed++;
                                Npp.AddLine($"Random JSON\n{rand.ToString()}\ncontained key \"{key}\" not in schema:\n{schema.ToString()}");
                                failure = true;
                                break;
                            }
                        }
                        if (failure)
                            break;
                        if (!randKeys.IsSupersetOf(required))
                        {
                            tests_failed++;
                            Npp.AddLine($"Random JSON\n{rand.ToString()}\ndid not contain all required keys in {allRequiredKeyString}");
                            break;
                        }
                    }
                }
                if (node is JObject)
                {
                    // make sure that random objects have a random number of keys
                    // (if required is a proper subset of all possible keys)
                    if (keycounts.All((count) => count > required.Count) & allKeys.Count > required.Count)
                    {
                        Npp.AddLine($"For schema\n{schema.ToString()}\nall random objects had more than the required number of keys ({required.Count}), " +
                                    "but statistically at least some should have had only the required keys\n" +
                                    $"Required keys: {allRequiredKeyString}\nAll keys: {allKeyString}");
                        tests_failed++;
                    }
                    if (!keycounts.Any((count) => count < allKeys.Count))
                    {
                        Npp.AddLine($"For schema\n{schema.ToString()}\nall random objects had fewer than the maximum number of keys ({allKeys.Count}), " +
                                    "but statistically at least some should have had all of the possible keys\n" +
                                    $"All keys: {allKeyString}");
                        tests_failed++;
                    }
                }
            }
            Npp.AddLine($"Failed {tests_failed} tests.");
            Npp.AddLine($"Passed {ii - tests_failed} tests.");
        }
    }
}
