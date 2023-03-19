using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace JSON_Tools.JSON_Tools
{
    public class SchemaValidationException : Exception
    {
        public new string Message;

        public SchemaValidationException(string message)
        {
            Message = message;
        }
    }

    public class JsonSchemaValidator
    {
        public enum ValidationProblemType
        {
            TYPE_MISMATCH,
            VALUE_NOT_IN_ENUM,
            ARRAY_TOO_LONG,
            ARRAY_TOO_SHORT,
            OBJECT_MISSING_REQUIRED_KEY,
            FALSE_SCHEMA, // nothing validates
        }

        public struct ValidationProblem
        {
            public ValidationProblemType problemType;
            public Dictionary<string, object> keywords;
            public int line_num;

            public ValidationProblem(ValidationProblemType problemType, 
                Dictionary<string, object> keywords,
                int line_num)
            {
                this.problemType = problemType;
                this.keywords = keywords;
                this.line_num = line_num;
            }

            public override string ToString()
            {
                string msg = $"At line {line_num + 1}, ";
                switch (problemType)
                {
                    case ValidationProblemType.TYPE_MISMATCH:
                        string found_type = (string)keywords["found"];
                        object required = keywords["required"];
                        if (required is JArray req_arr)
                        {
                            return msg + $"found type {found_type}, expected one of the types {req_arr.ToString()}.";
                        }
                        return msg + $"found type {found_type}, expected type {(string)required}.";
                    case ValidationProblemType.VALUE_NOT_IN_ENUM:
                        JArray enum_ = (JArray)keywords["enum"];
                        JNode found_node = (JNode)keywords["found"];
                        return msg + $"found value {found_node.ToString()}, but the only allowed values are {enum_.ToString()}.";
                    case ValidationProblemType.ARRAY_TOO_SHORT:
                        int found_length = (int)keywords["found"];
                        keywords.TryGetValue("minItems", out object minItemsObj);
                        int minItems = 0;
                        if (minItemsObj != null) minItems = (int)minItemsObj;
                        return msg + $"array required to have at least {minItems} items, but it has {found_length} items.";
                    case ValidationProblemType.ARRAY_TOO_LONG:
                        found_length = (int)keywords["found"];
                        keywords.TryGetValue("maxItems", out object maxItemsObj);
                        int maxItems = 0;
                        if (maxItemsObj != null) maxItems = (int)maxItemsObj;
                        return msg + $"array required to have no more than {maxItems} items, but it has {found_length} items.";
                    case ValidationProblemType.OBJECT_MISSING_REQUIRED_KEY:
                        string key_missing = (string)keywords["required"];
                        return msg + $"object missing required key {key_missing}";
                    case ValidationProblemType.FALSE_SCHEMA:
                        return msg + "the schema is `false`, so nothing will validate.";
                    default: throw new ArgumentException($"Unknown validation problem type {problemType}");
                }
            }
        }

        /// <summary>
        /// checks if the types are equal,
        /// OR if json_type is integer and schema_type is number
        /// </summary>
        public static bool TypeValidates(string json_type, string schema_type)
        {
            return json_type == schema_type || json_type == "integer" && schema_type == "number";
        }

        private static Func<JNode, ValidationProblem?> CompileValidationFunc()
        {
            throw new NotImplementedException();
        }

        public static ValidationProblem? Validates(JNode json, JNode schema_)
        {
            if (!(schema_ is JObject schema))
            {
                // the booleans are valid schemas.
                // true validates everything, and false validates nothing
                if ((bool)schema_.value) return null;
                return new ValidationProblem(ValidationProblemType.FALSE_SCHEMA, new Dictionary<string, object>(), json.line_num);
            }
            if (schema.Length == 0)
                return null; // the empty schema validates everything
            schema.children.TryGetValue("type", out JNode type);
            string json_type_name = JsonSchemaMaker.TypeName(json.type);
            if (type == null)
            {
                schema.children.TryGetValue("anyOf", out JNode anyOf);
                // an anyOf array of allowable schemas
                if (anyOf == null)
                {
                    throw new SchemaValidationException("Each schema must have either an 'anyOf' or a 'type' keyword.");
                }
                foreach (JNode subschema in ((JArray)anyOf).children)
                {
                    if (Validates(json, (JObject)subschema) is null) return null;
                }
                return new ValidationProblem(ValidationProblemType.TYPE_MISMATCH,
                    new Dictionary<string, object>
                    {
                        { "found", json_type_name },
                        { "required", anyOf }
                    },
                    json.line_num
                );
            }
            if (type is JArray types)
            {
                // an array of scalar types
                foreach (JNode type_name in types.children)
                {
                    if (TypeValidates(json_type_name, (string)type_name.value))
                        return null;
                }
                return new ValidationProblem(ValidationProblemType.TYPE_MISMATCH,
                    new Dictionary<string, object>
                    {
                        { "found", json_type_name },
                        { "required", types }
                    }, 
                    json.line_num
                );
            }
            string typestr = (string)type.value;
            if (!TypeValidates(json_type_name, typestr))
            {
                return new ValidationProblem(ValidationProblemType.TYPE_MISMATCH,
                    new Dictionary<string, object>
                    {
                        { "found", json_type_name },
                        { "required", typestr }
                    },
                    json.line_num
                );
            }
            // we've established that the JSON has the right type
            // now do any additional validation as needed
            schema.children.TryGetValue("enum", out JNode enum_);
            if (enum_ != null && enum_ is JArray enumarr)
            {
                // the "enum" keyword means that the JSON must have
                // one of the values in the associated array
                foreach (JNode possible in enumarr.children)
                {
                    if (possible.Equals(json)) return null;
                }
                return new ValidationProblem(
                    ValidationProblemType.VALUE_NOT_IN_ENUM,
                    new Dictionary<string, object>
                    {
                        { "found", json },
                        { "enum", enumarr },
                    },
                    json.line_num
                );
            }
            if (json is JArray arr)
            {
                if (!schema.children.TryGetValue("items", out JNode items))
                    return null;
                    // no "items" keyword means the array could hold anything or nothing
                var len_ = arr.children.Count;
                if (schema.children.TryGetValue("minItems", out JNode minItemsNode))
                {
                    var minItems = Convert.ToInt32(minItemsNode.value);
                    if (len_ < minItems) // minItems sets minimum length for array
                    {
                        return new ValidationProblem(
                            ValidationProblemType.ARRAY_TOO_SHORT,
                            new Dictionary<string, object>
                            {
                                { "found", len_ },
                                { "minItems", minItems }, 
                            },
                            json.line_num
                        );
                    }
                }
                if (schema.children.TryGetValue("maxItems", out JNode maxItemsNode))
                {
                    var maxItems= Convert.ToInt32(maxItemsNode.value);
                    if (len_ > maxItems)
                    {
                        return new ValidationProblem(
                            ValidationProblemType.ARRAY_TOO_LONG,
                            new Dictionary<string, object>
                            {
                                { "found", len_ },
                                { "maxItems", maxItems },
                            },
                            json.line_num
                        );
                    }
                }
                foreach (JNode child in arr.children)
                {
                    var vp = Validates(child, items);
                    if (vp != null) return vp;
                }
                return null;
            }
            else if (json is JObject obj)
            {
                if (!schema.children.TryGetValue("properties", out JNode properties))
                    return null; // no properties keyword means anything goes
                if (!schema.children.TryGetValue("required", out JNode required))
                    required = new JArray();
                foreach (JNode required_key in ((JArray)required).children)
                {
                    if (!obj.children.ContainsKey((string)required_key.value))
                    {
                        return new ValidationProblem(
                            ValidationProblemType.OBJECT_MISSING_REQUIRED_KEY,
                            new Dictionary<string, object>
                            {
                                { "required", required_key.value },
                            },
                            json.line_num
                        );
                    }
                }
                JObject props = (JObject)properties;
                if (schema.children.TryGetValue("patternProperties", out JNode pattern_props_node)
                    && pattern_props_node is JObject pattern_props)
                {
                    // "patternProperties" have regular expressions as keys.
                    // If a key in an object matches one of the regexes
                    // in patternProperties, the corresponding value in the object
                    // must validate under that
                    // pattern's subschema in patternProperties.
                    foreach (string key in obj.children.Keys)
                    {
                        foreach (string pattern in pattern_props.children.Keys)
                        {
                            if (!new Regex(pattern.Replace("\\\\", "\\")).IsMatch(key))
                                continue;
                            JNode pattern_subschema = pattern_props[pattern];
                            var vp = Validates(obj[key], pattern_subschema);
                            if (vp != null) return vp;
                        }
                    }
                }
                foreach (string key in props.children.Keys)
                {
                    // for each property name in the schema, make sure that
                    // the JSON has the corresponding key and that the associated value
                    // validates with the property name's subschema.
                    JObject prop_schema = (JObject)props.children[key];
                    if (!obj.children.TryGetValue(key, out JNode value))
                        continue;
                    var vp = Validates(value, prop_schema);
                    if (vp != null) return vp;
                }
                return null;
            }
            // it's a scalar, and at present no extra validation is done on scalars
            return null;
        }
    }
}
