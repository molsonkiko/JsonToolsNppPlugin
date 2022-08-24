using System;
using System.Collections.Generic;
using System.Linq;

namespace JSON_Tools.JSON_Tools
{
    /// <summary>
    /// This class generates a minimalist <a href="https://json-schema.org/">JSON schema</a> for a JSON object.<br></br>
    /// It does <i>not</i> validate existing schemas (yet), only generate new schemas.<br></br>
    /// The only keywords supported are the following:<br></br>
    /// - <b>type</b><br></br>
    /// - <b>$schema</b><br></br>
    /// - <b>items</b><br></br>
    /// - <b>properties</b><br></br>
    /// - <b>required</b><br></br>
    /// - <b>anyOf</b><br></br>
    /// <b>Currently known bug(s)</b>:<br></br>
    /// 1. The "required" attribute for object schemas may be incorrect.<br></br>
    /// Specifically, the "required" attribute for an object that is a child of an object and the ancestor of an array
    /// will be wrong.<br></br>
    /// Consider this JSON<br></br>
    /// [<br></br>
    ///     {'a': 3},<br></br> 
    ///     {'a': 1, 'b': [{'a': {'b': 1, 'c': 2}}, {'a': {'b': 1}}]},<br></br>
    ///     {'a': 1, 'b': [{'a': {'b': 1, 'c': 2, 'd': 3}}]},<br></br>
    ///     { 'a': 2, 'c': 3}<br></br>
    /// ]<br></br>
    /// The most deeply nested object will have "required" listed as ["b", "c", "d"] when it is actually ["b"].
    /// </summary>
    public class JsonSchemaMaker
    {
        // not sure what attributes JsonSchemaMaker class should have - at present there are no knobs the user can turn to customize it
        // I'm not sure how fancy/customizable of a schema generator I'm even capable of implementing
        public JsonSchemaMaker()
        {
            // initialize attrs
        }

        public static readonly Dictionary<string, object> BASE_SCHEMA = new Dictionary<string, object>
        {
            // ["$id"] = "https://example.com/arrays.schema.json",
            // the $id should be any URL I control
            ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
            // The $schema should be the URI for the rules of the latest draft of JSON
            // schema. Currently the latest is 2020-12.
        };

        /// <summary>
        /// get the JSON schema type name associated with a JSON variable (e.g. ints -> "integer", bools -> "boolean")
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public string TypeName(Dtype x)
        {
            switch (x)
            {
                case Dtype.STR: return "string";
                case Dtype.FLOAT: return "number";
                case Dtype.INT: return "integer";
                case Dtype.ARR: return "array";
                case Dtype.OBJ: return "object";
                case Dtype.BOOL: return "boolean";
                case Dtype.NULL: return "null";
                default: return x.ToString().ToLower();
            }
        }

        /// <summary>
        /// Combine two JSON schemas to make the simplest possible hybrid schema<br></br>
        /// EXAMPLES<br></br>
        /// MergeSchemas({"type": "array", "items": {"type": "integer"}}, {"type": "array", "items": {"type": "string"}})<br></br>
        /// returns {"type": "array", "items": {"type": ["integer", "string"]}}<br></br>
        /// MergeSchemas({"type": "object", "properties": {"a": {"type": "integer"}, "b": {"type": "string"}}, "required": ["a", "b"]},
        /// {"type": "object", "properties": {"a": {"type": "number"}}, "required": ["a"]}
        /// returns {"type": "object", "properties": {"a": {"type": ["integer", "number"]}, "b": {"type": "string"}}, "required": ["a"]}
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        /// <returns></returns>
        public Dictionary<string, object> MergeSchemas(Dictionary<string, object> s1, Dictionary<string, object> s2)
        {
            List<object> anyof = new List<object>();
            bool s1_has_type = s1.TryGetValue("type", out object s1type);
            bool s2_has_type = s2.TryGetValue("type", out object s2type);
            bool s1_empty = s1type is List<object> && ((List<object>)s1type).Count == 0;
            bool s2_empty = s2type is List<object> && ((List<object>)s2type).Count == 0;
            if (!s1_has_type)
            {
                if (!s2_has_type)
                {
                    foreach (object v in (List<object>)s1["anyOf"])
                    {
                        anyof.Add(v);
                    }
                    foreach (object v in (List<object>)s2["anyOf"])
                    {
                        anyof.Add(v);
                    }
                    return new Dictionary<string, object> { ["anyOf"] = anyof };
                }
                anyof.Add(new Dictionary<string, object> { ["type"] = s2type });
                foreach (object v in (List<object>)s1["anyOf"])
                {
                    anyof.Add(v);
                }
                return new Dictionary<string, object> { ["anyOf"] = anyof };
            }
            if (!s2_has_type)
            {
                anyof.Add(new Dictionary<string, object> { ["type"] = s1type });
                foreach (object v in (List<object>)s2["anyOf"])
                {
                    anyof.Add(v);
                }
                return new Dictionary<string, object> { ["anyOf"] = anyof };
            }
            if (s1.Count == 1)
            {
                if (s1_empty)
                {
                    return s2;
                }
                if (s2.Count == 1)
                {
                    // two scalars, or a scalar and an array of scalars, or two arrays of scalars
                    var newtypes = new HashSet<object>();
                    if (s1type is List<object>)
                    {
                        newtypes.UnionWith((List<object>)s1type);
                    }
                    else
                    {
                        newtypes.Add(s1type);
                    }
                    if (!s2_empty)
                    {
                        if (s2type is List<object>)
                        {
                            newtypes.UnionWith((List<object>)s2type);
                        }
                        else
                        {
                            newtypes.Add(s2type);
                        }
                    }
                    var newtype_list = new List<object>(newtypes);
                    if (newtype_list.Count == 1)
                    {
                        return new Dictionary<string, object> { ["type"] = newtype_list[0] };
                    }
                    return new Dictionary<string, object> { ["type"] = newtype_list };
                }
                if (s1type is List<object>)
                {
                    foreach (object t in (List<object>)s1type)
                    {
                        anyof.Add(new Dictionary<string, object> { ["type"] = t });
                    }
                    anyof.Add(s2);
                    return new Dictionary<string, object> { ["anyOf"] = anyof };
                }
            }
            if (s2.Count == 1)
            {
                if (s2_empty)
                {
                    return s1;
                }
                if (s2type is List<object>)
                {
                    foreach (object t in (List<object>)s2type)
                    {
                        anyof.Add(new Dictionary<string, object> { { "type", t } });
                    }
                    anyof.Add(s1);
                    return new Dictionary<string, object> { { "anyOf", anyof } };
                }
                anyof.Add(new Dictionary<string, object> { { "type", s2type } });
                anyof.Add(s1);
                return new Dictionary<string, object> { { "anyOf", anyof } };
            }
            if ((Dtype)s1type == Dtype.ARR && (Dtype)s2type == Dtype.ARR)
            {
                var combined_items = MergeSchemas((Dictionary<string, object>)s1["items"], 
                                                  (Dictionary<string, object>)s2["items"]);
                return new Dictionary<string, object> { { "type", Dtype.ARR }, { "items", combined_items } };
            }
            if ((Dtype)s1type == Dtype.OBJ && (Dtype)s2type == Dtype.OBJ)
            {
                var props = new Dictionary<string, object>();
                var result = new Dictionary<string, object> { { "type", Dtype.OBJ }, { "properties", props } };
                var s1props = (Dictionary<string, object>)s1["properties"];
                var s2props = (Dictionary<string, object>)s2["properties"];
                var s1_keyset = new HashSet<string>(s1props.Keys);
                var s2_keyset = new HashSet<string>(s2props.Keys);
                HashSet<string> s1_req = s1.ContainsKey("required") ? (HashSet<string>)s1["required"] : s1_keyset;
                HashSet<string> s2_req = s2.ContainsKey("required") ? (HashSet<string>)s2["required"] : s2_keyset;
                var shared_keys = new HashSet<string>(s1_req.Intersect(s2_req));
                result["required"] = shared_keys;
                foreach (string k in shared_keys)
                {
                    props[k] = MergeSchemas((Dictionary<string, object>)s1props[k], (Dictionary<string, object>)s2props[k]);
                }
                foreach (string k in s1_keyset)
                {
                    if (!shared_keys.Contains(k)) { props[k] = s1props[k]; }
                }
                foreach (string k in s2_keyset)
                {
                    if (!shared_keys.Contains(k)) { props[k] = s2props[k]; }
                }
                return result;
            }
            anyof.Add(s1);
            anyof.Add(s2);
            return new Dictionary<string, object> { { "anyOf", anyof } };
        }

        /// <summary>
        /// Create a minimalist JSON schema for a JSON object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public Dictionary<string, object> BuildSchema(JNode obj)
        {
            Dtype tipe = obj.type;
            var schema = new Dictionary<string, object>{ { "type", tipe } };
            if (tipe == Dtype.ARR)
            {
                var items = new Dictionary<string, object> { { "type", new List<object>() } };
                HashSet<object> scalar_types = new HashSet<object>();
                foreach (JNode elt in ((JArray)obj).children)
                {
                    if ((elt.type & Dtype.SCALAR) != 0)
                    {
                        scalar_types.Add(elt.type);
                    }
                    else
                    {
                        items = MergeSchemas(items, BuildSchema(elt));
                    }
                }
                if (scalar_types.Count == 1)
                {
                    object scaltype = new List<object>(scalar_types)[0];
                    items = MergeSchemas(new Dictionary<string, object> { { "type", scaltype } }, items);
                }
                else if (scalar_types.Count > 1)
                {
                    List<object> scaltypes = new List<object>(scalar_types);
                    items = MergeSchemas(new Dictionary<string, object> { { "type", scaltypes } }, items);
                }
                schema["items"] = items;
            }
            else if (tipe == Dtype.OBJ)
            {
                JObject oobj = (JObject)obj;
                var props = new Dictionary<string, object>();
                foreach (string k in oobj.children.Keys)
                {
                    props[k] = BuildSchema(oobj.children[k]);
                }
                if (!schema.ContainsKey("required"))
                    schema["required"] = new HashSet<string>(oobj.children.Keys);
                schema["properties"] = props;
            }
            return schema;
        }

        private int SchemaComparer(JNode x, JNode y)
        {
            if (x.type == Dtype.OBJ)
            {
                if (y.type == Dtype.OBJ) { return 0; }
                return 1;
            }
            if (y.type == Dtype.OBJ) { return -1; }
            return x.type.ToString().CompareTo(y.type.ToString());
        }

        /// <summary>
        /// Convert the schema to a JNode so that it can be conveniently displayed in a text document
        /// via the ToString or PrettyPrint methods
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public JNode SchemaToJNode(object schema)
        {
            if (schema is Dtype)
            {
                return new JNode(TypeName((Dtype)schema), Dtype.STR, 0);
            }
            if (schema is string)
            {
                return new JNode((string)schema, Dtype.STR, 0);
            }
            if (schema is Dictionary<string, object>)
            {
                var kids = new Dictionary<string, JNode>();
                foreach (KeyValuePair<string, object> kv in (Dictionary<string, object>)schema)
                {
                    kids[kv.Key] = SchemaToJNode(kv.Value);
                }
                return new JObject(0, kids);
            }
            var children = new List<JNode>();
            if (schema is List<object>)
            {
                foreach (object v in (List<object>)schema)
                {
                    children.Add(SchemaToJNode(v));
                }
                children.Sort(SchemaComparer);
                return new JArray(0, children);
            }
            // by process of elimination, it's the "required" attribute of object schemas which is initially a HashSet
            foreach (string v in (HashSet<string>)schema)
            {
                children.Add(new JNode(v, Dtype.STR, 0));
            }
            children.Sort();
            return new JArray(0, children);
        }

        /// <summary>
        /// Creates a minimalist JSON schema for a JNode tree.
        /// This is functionally equivalent to BuildSchema, but it is prettified for printing to a text document.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public JNode GetSchema(JNode obj)
        {
            var schema = new Dictionary<string, object>(BASE_SCHEMA);
            foreach (KeyValuePair<string, object> kv in BuildSchema(obj))
                schema[kv.Key] = kv.Value;
            return SchemaToJNode(schema);
        }
    }

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
                    desired_schema.children[k] = base_schema_j.children[k];
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
                            Console.WriteLine($"Expected the schema for {inp} to be\n{desired_sch_str}\nInstead got\n{schema.ToString()}");
                        }
                    }
                    catch
                    {
                        // probably because of something like trying to compare an array to a non-array
                        tests_failed++;
                        Console.WriteLine($"Expected the schema for {inp} to be\n{desired_sch_str}\nInstead got {schema.ToString()}");
                    }
                }
                catch (Exception e)
                {
                    tests_failed++;
                    Console.WriteLine($"Expected the schema for {jinp} to be\n{desired_sch_str}\nInstead raised exception {e}");
                }
            }
            Console.WriteLine($"Failed {tests_failed} tests.");
            Console.WriteLine($"Passed {ii - tests_failed} tests.");
        }
    }
}
