using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Xml.Serialization;

namespace JSON_Tools.JSON_Tools
{
    /// <summary>
    /// This class generates a minimalist <a href="https://json-schema.org/">JSON schema</a> for a JSON object.<br></br>
    /// It does <i>not</i> validate existing schemas, only generate new schemas.<br></br>
    /// The only keywords supported are the following:<br></br>
    /// - <b>type</b><br></br>
    /// - <b>$schema</b><br></br>
    /// - <b>items</b><br></br>
    /// - <b>properties</b><br></br>
    /// - <b>required</b><br></br>
    /// - <b>anyOf</b><br></br>
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
        public static string TypeName(Dtype x)
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

        public static Dictionary<string, Dtype> typeNameToDtype = new Dictionary<string, Dtype>
        {
            { "string", Dtype.STR },
            { "number", Dtype.FLOAT },
            { "integer", Dtype.INT },
            { "array", Dtype.ARR },
            { "object", Dtype.OBJ },
            { "boolean", Dtype.BOOL },
            { "null", Dtype.NULL }
        };

        public static bool IsSingleDtype(Dtype x)
        {
            return x == Dtype.ARR
                || x == Dtype.STR
                || x == Dtype.FLOAT
                || x == Dtype.INT
                || x == Dtype.OBJ
                || x == Dtype.BOOL
                || x == Dtype.NULL
                || x == Dtype.REGEX
                || x == Dtype.DATE
                || x == Dtype.DATETIME
                || x == Dtype.SLICE;
        }

        public static Dtype DtypeUnion(Dtype a, Dtype b)
        {
            var together = a | b;
            if ((together & Dtype.FLOAT_OR_INT) == Dtype.FLOAT_OR_INT)
            {
                // in JSON Schema, "number" is a supertype of "integer"
                // so if both are present, we get rid of "integer"
                together ^= Dtype.INT;
            }
            return together;
        }

        /// <summary>
        /// An anyOf list of schemas should in principle be a list of
        /// unrelated schemas that happen to be in the same array
        /// or assigned to the same key in an object.<br></br>
        /// For example, an array might contain integers, null, arrays, and objects.<br></br>
        /// If all the objects in the array are similar to the other objects,<br></br>
        /// and all the arrays are similar to the other arrays
        /// we would prefer the resulting anyOf list to look something like<br></br>
        /// [<br></br>
        ///    {"type": ["integer", "null"]}, // covers all scalars<br></br>
        ///    {"type": "array", "items": {...}} // covers all array subschemas<br></br>
        ///    {"type": "object", "properties": {...}} // covers all object subschemas<br></br>
        /// ]<br></br>
        /// However, the algorithm in MergeSchemas will typically produce an anyOf list more like:<br></br>
        /// [<br></br>
        ///    {"type": ["integer", null"]},<br></br>
        ///    {"type": "array", ...}, // first array schema found<br></br>
        ///    {"type": "array", ...}, // second array schema, which may be identical to the first<br></br>
        ///    {"type": "array", ...}, // yet another similar or identical array schema<br></br>
        ///    {"type": "object", ...}, // first object schema found<br></br>
        ///    {"type": "object", ...} // you get the idea
        /// ]<br></br>
        /// This function merges all array schemas together and all object schemas together to get a compact anyOf list.
        /// </summary>
        /// <param name="anyOf"></param>
        /// <returns></returns>
        private static List<object> CompactAnyOf(List<object> anyOf)
        {
            var object_anyof = new Dictionary<string, object>();
            var array_anyof = new Dictionary<string, object>();
            var scaltypes = Dtype.TYPELESS;
            foreach (object subschema_obj in anyOf)
            {
                var subschema = (Dictionary<string, object>)subschema_obj;
                if (!subschema.TryGetValue("type", out object subtipe_obj))
                    continue;
                Dtype subtipe = (Dtype)subtipe_obj;
                if ((subtipe & Dtype.SCALAR) != 0)
                    scaltypes = DtypeUnion(scaltypes, subtipe);
                else if (subtipe == Dtype.ARR)
                    array_anyof = MergeSchemas(array_anyof, subschema);
                else if (subtipe == Dtype.OBJ)
                    object_anyof = MergeSchemas(object_anyof, subschema);
            }
            List<object> result = new List<object>();
            if (scaltypes != Dtype.TYPELESS)
                result.Add(new Dictionary<string, object> { { "type", scaltypes } });
            if (array_anyof.Count > 0)
                result.Add(array_anyof);
            if (object_anyof.Count > 0)
                result.Add(object_anyof);
            return result;
        }

        /// <summary>
        /// Combine two JSON schemas to make the simplest possible hybrid schema<br></br>
        /// EXAMPLES<br></br>
        /// MergeSchemas({"type": "array", "items": {"type": "integer"}}, {"type": "array", "items": {"type": "string"}})<br></br>
        /// returns {"type": "array", "items": {"type": ["integer", "string"]}}<br></br>
        /// MergeSchemas({"type": "object", "properties": {"a": {"type": "integer"}, "b": {"type": "string"}}, "required": ["a", "b"]},
        /// {"type": "object", "properties": {"a": {"type": "number"}}, "required": ["a"]}
        /// returns {"type": "object", "properties": {"a": {"type": "number"}, "b": {"type": "string"}}, "required": ["a"]}
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        /// <returns></returns>
        private static Dictionary<string, object> MergeSchemas(Dictionary<string, object> s1, Dictionary<string, object> s2)
        {
            if (s1.Count == 0) return s2;
            if (s2.Count == 0) return s1;
            List<object> anyof = new List<object>();
            var scaltypes = Dtype.TYPELESS;
            // A schema must have either the "anyOf" or the "type" keyword
            bool s1_anyOf = !s1.TryGetValue("type", out object s1type_obj);
            bool s2_anyOf = !s2.TryGetValue("type", out object s2type_obj);
            Dtype s1type = s1_anyOf ? Dtype.TYPELESS : (Dtype)s1type_obj;
            Dtype s2type = s2_anyOf ? Dtype.TYPELESS : (Dtype)s2type_obj;
            bool s1_typeless = s1type == Dtype.TYPELESS;
            bool s2_typeless = s2type == Dtype.TYPELESS;
            if (s1_anyOf)
            {
                foreach (object v in (List<object>)s1["anyOf"])
                {
                    // get all scalar types from s1 and combine them into a list
                    if (v is Dictionary<string, object> vobj
                        && vobj.TryGetValue("type", out object v_type)
                        && v_type is Dtype v_dtype
                        && (v_dtype & Dtype.SCALAR) != 0)
                    {
                        scaltypes = DtypeUnion(scaltypes, v_dtype);
                    }
                    else anyof.Add(v);
                }
                if (s2_anyOf)
                {
                    foreach (object v in (List<object>)s2["anyOf"])
                    {
                        if (v is Dictionary<string, object> vobj
                            && vobj.TryGetValue("type", out object v_type)
                            && v_type is Dtype v_dtype
                            && (v_dtype & Dtype.SCALAR) != 0)
                        {
                            scaltypes = DtypeUnion(scaltypes, v_dtype);
                        }
                        else anyof.Add(v);
                    }
                    // at this point scaltypes should be the bitwise OR of any types
                    // shared by s1 and s2, e.g., Dtype.STR | Dtype.BOOL
                }
                // s2 is a scalar or array of scalars (has only the type keyword)
                else if ((s2type & Dtype.ARR_OR_OBJ) == 0)
                {
                    scaltypes = DtypeUnion(scaltypes, s2type);
                }
                // s2 is an array or object
                else
                {
                    anyof.Add(s2);
                }
                if (scaltypes != Dtype.TYPELESS)
                    anyof.Add(new Dictionary<string, object> {{ "type", scaltypes }});
                return new Dictionary<string, object> { ["anyOf"] = CompactAnyOf(anyof) };
            }
            // s1 is not anyOf; it could be an array, object, or scalar type(s)
            if (s2_anyOf)
            {
                foreach (object v in (List<object>)s2["anyOf"])
                {
                    // get all scalar types from s2 and combine them into a list
                    if (v is Dictionary<string, object> vobj
                        && vobj.TryGetValue("type", out object v_type)
                        && v_type is Dtype v_dtype
                        && (v_dtype & Dtype.SCALAR) != 0)
                    {
                        scaltypes = DtypeUnion(scaltypes, v_dtype);
                    }
                    else anyof.Add(v);
                }
                if ((s1type & Dtype.ARR_OR_OBJ) == 0)
                {
                    scaltypes = DtypeUnion(scaltypes, s1type);
                }
                else
                {
                    anyof.Add(s1);
                }
                if (scaltypes != Dtype.TYPELESS)
                    anyof.Add(new Dictionary<string, object> {{ "type", scaltypes }});
                return new Dictionary<string, object> { ["anyOf"] = CompactAnyOf(anyof) };
            }
            // s1 is a scalar type or array of scalar types
            if ((s1type & Dtype.ARR_OR_OBJ) == 0)
            {
                if (s1_typeless)
                {
                    return s2;
                }
                // s2 is also one or more scalar type(s)
                if ((s2type & Dtype.ARR_OR_OBJ) == 0)
                {
                    var newtypes = DtypeUnion(s1type, s2type);
                    return new Dictionary<string, object> { ["type"] = newtypes };
                }
                // s2 is an array or object
                anyof.Add(new Dictionary<string, object> { { "type", s1type } });
                anyof.Add(s2);
                return new Dictionary<string, object> { { "anyOf", anyof } };
            }
            // s1 is an array or object
            if ((s2type & Dtype.ARR_OR_OBJ) == 0)
            {
                if (s2_typeless)
                {
                    return s1;
                }
                // s2 is one or more scalar type(s), and since s1 is not scalar,
                // we get anyOf
                anyof.Add(new Dictionary<string, object> { { "type", s2type } });
                anyof.Add(s1);
                return new Dictionary<string, object> { { "anyOf", anyof } };
            }
            // both are arrays, so create a new array schema
            // by merging their "items" attributes
            if (s1type == Dtype.ARR && s2type == Dtype.ARR)
            {
                Dictionary<string, object> combined_items;
                bool s1_has_items = s1.TryGetValue("items", out object s1_items);
                bool s2_has_items = s2.TryGetValue("items", out object s2_items);
                if (!s1_has_items)
                {
                    if (!s2_has_items)
                    {
                        // both are arrays with no items, so create schema
                        // with no items
                        return new Dictionary<string, object> { { "type", Dtype.ARR } };
                    }
                    combined_items = (Dictionary<string, object>)s2_items;
                }
                else if (!s2_has_items)
                {
                    combined_items = (Dictionary<string, object>)s1_items;
                }
                else
                {
                    combined_items = MergeSchemas(
                        (Dictionary<string, object>)s1_items,
                        (Dictionary<string, object>)s2_items);
                }
                return new Dictionary<string, object> { { "type", Dtype.ARR }, { "items", combined_items } };
            }
            // both are objects, so create a new object schema by merging
            // their "properties" attributes and reducing their "required"
            // keys if necessary
            if (s1type == Dtype.OBJ && s2type == Dtype.OBJ)
            {
                object s2_props_obj;
                Dictionary<string, object> s1props;
                Dictionary<string, object> s2props;
                if (!s1.TryGetValue("properties", out object s1_props_obj))
                {
                    if (!s2.TryGetValue("properties", out s2_props_obj))
                    {
                        // both have no properties, so return empty object schema
                        return new Dictionary<string, object>
                        {
                            { "type", Dtype.OBJ }
                        };
                    }
                    // s2 is not empty, but s1 is
                    // we return a new schema identical to s2, except with
                    // no required keys
                    return new Dictionary<string, object>
                    {
                        { "type", Dtype.OBJ },
                        { "properties", s2_props_obj },
                        { "required", new List<string>() }
                    };
                }
                if (!s2.TryGetValue("properties", out s2_props_obj))
                {
                    // s1 has properties, but s2 doesn't
                    return new Dictionary<string, object>
                    {
                        { "type", Dtype.OBJ },
                        { "properties", s1_props_obj },
                        { "required", new List<string>() }
                    };
                }
                var props = new Dictionary<string, object>();
                var result = new Dictionary<string, object> { { "type", Dtype.OBJ }, { "properties", props } };
                s1props = (Dictionary<string, object>)s1_props_obj;
                s2props = (Dictionary<string, object>)s2_props_obj;
                var s1_keyset = new HashSet<string>(s1props.Keys);
                var s2_keyset = new HashSet<string>(s2props.Keys);
                // get all keys that are shared, but not necessarily required
                // we want to merge those schemas
                var shared_keys = new HashSet<string>(s1_keyset.Intersect(s2_keyset));
                s1_keyset.ExceptWith(shared_keys);
                s2_keyset.ExceptWith(shared_keys);
                // the existing "required" attribute should be used if possible
                // but make sure to remove keys that aren't in the new dict
                if (!(s1.TryGetValue("required", out object s1_req_obj)
                    && s1_req_obj is HashSet<string> s1_req))
                {
                    s1_req = s1_keyset;
                }
                if (!(s2.TryGetValue("required", out object s2_req_obj)
                    && s2_req_obj is HashSet<string> s2_req))
                {
                    s2_req = s2_keyset;
                }
                result["required"] = new HashSet<string>(s1_req.Intersect(s2_req));
                // merge schemas for any keys that both have (even if not required)
                foreach (string k in shared_keys)
                {
                    props[k] = MergeSchemas((Dictionary<string, object>)s1props[k], (Dictionary<string, object>)s2props[k]);
                }
                // now add the schemas of keys found in only one of the two
                foreach (string k in s1_keyset)
                {
                    props[k] = s1props[k];
                }
                foreach (string k in s2_keyset)
                {
                    props[k] = s2props[k];
                }
                return result;
            }
            // s1 is an array and s2 is an object, or vice versa
            anyof.Add(s1);
            anyof.Add(s2);
            return new Dictionary<string, object> { { "anyOf", anyof } };
        }

        /// <summary>
        /// Create a minimalist JSON schema for some JSON.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static Dictionary<string, object> BuildSchema(JNode json)
        {
            Dtype tipe = json.type;
            var schema = new Dictionary<string, object> { { "type", tipe } };
            if (tipe == Dtype.ARR)
            {
                var items = new Dictionary<string, object> { };
                Dtype scalar_types = Dtype.TYPELESS;
                foreach (JNode elt in ((JArray)json).children)
                {
                    if ((elt.type & Dtype.SCALAR) != 0)
                    {
                        scalar_types = DtypeUnion(scalar_types, elt.type);
                    }
                    else
                    {
                        items = MergeSchemas(items, BuildSchema(elt));
                    }
                }
                if (scalar_types != 0)
                    items = MergeSchemas(new Dictionary<string, object> { { "type", scalar_types } }, items);
                if (items.Count > 0)
                    schema["items"] = items;
            }
            else if (json is JObject obj)
            {
                var props = new Dictionary<string, object>();
                foreach (string k in obj.children.Keys)
                {
                    props[k] = BuildSchema(obj[k]);
                }
                if (props.Count > 0)
                {
                    if (!schema.ContainsKey("required"))
                        schema["required"] = new HashSet<string>(obj.children.Keys);
                    schema["properties"] = props;
                }
            }
            // it's a scalar
            return schema;
        }

        /// <summary>
        /// A list of multiple schemas should be ordered as follows:<br></br>
        /// 1. Scalars (in alphabetical order)<br></br>
        /// 2. Arrays<br></br>
        /// 3. Objects
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private static int SchemaComparer(JNode x, JNode y)
        {
            JObject xobj = (JObject)x;
            JObject yobj = (JObject)y;
            if (!xobj.children.TryGetValue("type", out JNode xtype)
                || !yobj.children.TryGetValue("type", out JNode ytype))
            {
                return 0;
            }
            // x type is array of scalars
            if (xtype is JArray)
            {
                // y type is also array of scalars
                if (ytype is JArray)
                {
                    return 0;
                }
                // y is an iterable, and those come after scalars
                return -1;
            }
            string xtype_str = (string)xtype.value;
            string ytype_str = (string)ytype.value;
            if (xtype_str == "array")
            {
                if (ytype_str == "array")
                    return 0;
                if (ytype_str == "object")
                    return -1;
                return 1;
            }
            if (xtype_str == "object")
            {
                if (ytype_str == "object") { return 0; }
                return 1;
            }
            if (ytype_str == "object") { return -1; }
            return x.ToString().CompareTo(y.ToString());
        }

        /// <summary>
        /// Convert the schema to a JNode so that it can be conveniently displayed in a text document
        /// via the ToString or PrettyPrint methods
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public static JNode SchemaToJNode(object schema)
        {
            if (schema is Dtype type)
            {
                if (IsSingleDtype(type))
                    return new JNode(TypeName(type), Dtype.STR, 0);
                List<JNode> typelist = new List<JNode>();
                if ((type & Dtype.BOOL) != 0)
                    typelist.Add(new JNode("boolean", Dtype.STR, 0));
                if ((type & Dtype.INT) != 0)
                    typelist.Add(new JNode("integer", Dtype.STR, 0));
                if ((type & Dtype.NULL) != 0)
                    typelist.Add(new JNode("null", Dtype.STR, 0));
                if ((type & Dtype.FLOAT) != 0)
                    typelist.Add(new JNode("number", Dtype.STR, 0));
                if ((type & Dtype.STR) != 0)
                    typelist.Add(new JNode("string", Dtype.STR, 0));
                return new JArray(0, typelist);
            }
            if (schema is string str)
            {
                // this should only happen in the base schema
                return new JNode(str, Dtype.STR, 0);
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
            // anyOf list of schemas
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
        public static JNode GetSchema(JNode obj)
        {
            var schema = new Dictionary<string, object>(BASE_SCHEMA);
            foreach (KeyValuePair<string, object> kv in BuildSchema(obj))
                schema[kv.Key] = kv.Value;
            return SchemaToJNode(schema);
        }
    }
}