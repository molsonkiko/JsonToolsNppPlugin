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
            STRING_DOESNT_MATCH_PATTERN,
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
                        var found_type = JsonSchemaMaker.TypeName((Dtype)keywords["found"]);
                        object required = keywords["required"];
                        if (required is JArray req_arr)
                        {
                            return msg + $"found type {found_type}, expected one of the types {req_arr.ToString()}.";
                        }
                        return msg + $"found type {found_type}, expected type {JsonSchemaMaker.TypeName((Dtype)required)}.";
                    case ValidationProblemType.VALUE_NOT_IN_ENUM:
                        var enum_ = (JArray)keywords["enum"];
                        var found_node = (JNode)keywords["found"];
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
                    case ValidationProblemType.STRING_DOESNT_MATCH_PATTERN:
                        string str = (string)keywords["string"];
                        Regex regex = (Regex)keywords["regex"];
                        return msg + $"string '{str}' does not match regex '{regex}'";
                    default: throw new ArgumentException($"Unknown validation problem type {problemType}");
                }
            }
        }

        private struct RegexAndValidator
        {
            public Regex regex;
            public Func<JNode, ValidationProblem?> validator;

            public RegexAndValidator(Regex regex, Func<JNode, ValidationProblem?> validator)
            {
                this.regex = regex;
                this.validator = validator;
            }
        }

        /// <summary>
        /// checks if the types are equal,
        /// OR if json_type is integer and schema_type is number
        /// </summary>
        public static bool TypeValidates(Dtype json_type, Dtype schema_type)
        {
            return json_type == schema_type || json_type == Dtype.INT && schema_type == Dtype.FLOAT;
        }

        public static Func<JNode, ValidationProblem?> CompileValidationFunc(JNode schema_)
        {
            if (!(schema_ is JObject schema))
            {
                // the booleans are valid schemas.
                // true validates everything, and false validates nothing
                if ((bool)schema_.value) return (x) => null;
                return (x) => new ValidationProblem(ValidationProblemType.FALSE_SCHEMA, new Dictionary<string, object>(), x.line_num);
            }
            if (schema.Length == 0)
                return (x) => null; // the empty schema validates everything
            if (!schema.children.TryGetValue("type", out JNode type))
            {
                // an anyOf array of allowable schemas
                if (!schema.children.TryGetValue("anyOf", out JNode anyOf))
                {
                    throw new SchemaValidationException("Each schema must have either an 'anyOf' or a 'type' keyword.");
                }
                var subValidators = ((JArray)anyOf).children
                    .Select((subschema) => CompileValidationFunc(subschema))
                    .ToArray();
                return (JNode json) =>
                {
                    foreach (var subValidator in subValidators)
                    {
                        if (subValidator(json) is null) return null;
                    }
                    return new ValidationProblem(ValidationProblemType.TYPE_MISMATCH,
                        new Dictionary<string, object>
                        {
                            { "found", json.type },
                            { "required", anyOf }
                        },
                        json.line_num
                    );
                };
            }
            if (type is JArray types)
            {
                // an array of scalar types
                var dtypes = types.children
                    .Select((x) => JsonSchemaMaker.typeNameToDtype[(string)x.value])
                    .ToArray();
                return (JNode json) =>
                {
                    foreach (var dtype_ in dtypes)
                    {
                        if (TypeValidates(json.type, dtype_))
                            return null;
                    }
                    return new ValidationProblem(ValidationProblemType.TYPE_MISMATCH,
                        new Dictionary<string, object>
                        {
                            { "found", json.type },
                            { "required", types }
                        },
                        json.line_num
                    );
                };
            }
            var dtype = JsonSchemaMaker.typeNameToDtype[(string)type.value];
            // now do any additional validation as needed
            if (schema.children.TryGetValue("enum", out JNode enum_) && enum_ is JArray enumarr)
            {
                // the "enum" keyword means that the JSON must have
                // one of the values in the associated array
                var enumMembers = enumarr.children;
                return (json) =>
                {
                    foreach (JNode possible in enumMembers)
                    {
                        if (possible.type != json.type) continue;
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
                };
            }
            // validation logic for arrays
            if (dtype == Dtype.ARR)
            {
                int maxItems = (schema.children.TryGetValue("maxItems", out JNode maxItemsNode))
                    ? Convert.ToInt32(maxItemsNode.value)
                    : int.MaxValue;
                int minItems = (schema.children.TryGetValue("minItems", out JNode minItemsNode))
                    ? Convert.ToInt32(minItemsNode.value)
                    : 0;
                Func<JArray, ValidationProblem?> ItemsRightLength;
                if (minItems == 0 && maxItems == int.MaxValue)
                    ItemsRightLength = (arr) => null;
                else
                {
                    ItemsRightLength = (arr) =>
                    {
                        var len_ = arr.children.Count;
                        if (len_ < minItems) // minItems sets minimum length for array
                        {
                            return new ValidationProblem(
                                ValidationProblemType.ARRAY_TOO_SHORT,
                                new Dictionary<string, object>
                                {
                                    { "found", len_ },
                                    { "minItems", minItems },
                                },
                                arr.line_num
                            );
                        }
                        if (len_ > maxItems) // maxItems sets maximum length for array
                        {
                            return new ValidationProblem(
                                ValidationProblemType.ARRAY_TOO_LONG,
                                new Dictionary<string, object>
                                {
                                    { "found", len_ },
                                    { "maxItems", maxItems },
                                },
                                arr.line_num
                            );
                        }
                        return null;
                    };
                }
                if (schema.children.TryGetValue("items", out JNode items))
                {
                    var itemsValidator = CompileValidationFunc(items);
                    return (JNode json) =>
                    {
                        if (!(json is JArray arr))
                        {
                            return new ValidationProblem(ValidationProblemType.TYPE_MISMATCH,
                                new Dictionary<string, object>
                                {
                                    { "found", json.type },
                                    { "required", Dtype.ARR }
                                },
                                json.line_num
                            );
                        }
                        var itemsRightLength = ItemsRightLength(arr);
                        if (itemsRightLength != null)
                            return itemsRightLength;
                        foreach (JNode child in arr.children)
                        {
                            var vp = itemsValidator(child);
                            if (vp != null) return vp;
                        }
                        return null;
                    };
                }
                // no "items" keyword means the array could hold anything or nothing
                else return (json) =>
                {
                    if (!(json is JArray arr))
                    {
                        return new ValidationProblem(ValidationProblemType.TYPE_MISMATCH,
                            new Dictionary<string, object>
                            {
                                { "found", json.type },
                                { "required", Dtype.ARR }
                            },
                            json.line_num
                        );
                    }
                    return ItemsRightLength(arr);
                };
            }
            // validation logic for objects
            if (dtype == Dtype.OBJ)
            {
                var propsValidators = new Dictionary<string, Func<JNode, ValidationProblem?>>();
                if (schema.children.TryGetValue("properties", out JNode properties))
                {
                    JObject props = (JObject)properties;
                    foreach (string key in props.children.Keys)
                    {
                        propsValidators[key] = CompileValidationFunc(props[key]);
                    }
                }
                string[] required = schema.children.TryGetValue("required", out JNode requiredNode)
                    ? ((JArray)requiredNode).children.Select((x) => (string)x.value).ToArray()
                    : new string[0];
                Func<JObject, ValidationProblem?> patternPropsValidate;
                if (schema.children.TryGetValue("patternProperties", out JNode patternPropsNode)
                    && patternPropsNode is JObject patternProps)
                {
                    // "patternProperties" have regular expressions as keys.
                    // If a key in an object matches one of the regexes
                    // in patternProperties, the corresponding value in the object
                    // must validate under that
                    // pattern's subschema in patternProperties.
                    var patternPropsValidators = patternProps.children
                        .Select((kv) =>
                        {
                            string pat = kv.Key.Replace("\\\\", "\\");
                            var validator = CompileValidationFunc(kv.Value);
                            return new RegexAndValidator(new Regex(pat, RegexOptions.Compiled), validator);
                        })
                        .ToArray();
                    patternPropsValidate = (obj) =>
                    {
                        foreach (var regexAndValidator in patternPropsValidators)
                        {
                            var regex = regexAndValidator.regex;
                            foreach (string key in obj.children.Keys)
                            {
                                if (regex.IsMatch(key))
                                {
                                    var vp = regexAndValidator.validator(obj[key]);
                                    if (vp != null) return vp;
                                }
                            }
                        }
                        return null;
                    };
                }
                else patternPropsValidate = (obj) => null;
                return (json) =>
                {
                    if (!(json is JObject obj))
                    {
                        return new ValidationProblem(ValidationProblemType.TYPE_MISMATCH,
                            new Dictionary<string, object>
                            {
                                { "found", json.type },
                                { "required", Dtype.OBJ }
                            },
                            json.line_num
                        );
                    }
                    // check if object has all required keys
                    foreach (string required_key in required)
                    {
                        if (!obj.children.ContainsKey(required_key))
                        {
                            return new ValidationProblem(
                                ValidationProblemType.OBJECT_MISSING_REQUIRED_KEY,
                                new Dictionary<string, object>
                                {
                                    { "required", required_key },
                                },
                                obj.line_num
                            );
                        }
                    }
                    var vp = patternPropsValidate(obj);
                    if (vp != null) return vp;
                    foreach (string key in propsValidators.Keys)
                    {
                        // for each property name in the schema, make sure that
                        // the JSON has the corresponding key and that the associated value
                        // validates with the property name's subschema.
                        if (!obj.children.TryGetValue(key, out JNode value))
                            continue;
                        vp = propsValidators[key](value);
                        if (vp != null) return vp;
                    }
                    return null;
                };
            }
            if (dtype == Dtype.STR && schema.children.TryGetValue("pattern", out JNode pattern))
            {
                Regex regex = new Regex((string)pattern.value, RegexOptions.Compiled);
                return (json) =>
                {
                    if (!(json.value is string str))
                    {
                        return new ValidationProblem(
                            ValidationProblemType.TYPE_MISMATCH,
                            new Dictionary<string, object>
                            {
                                { "found", json.type },
                                { "required", dtype },
                            },
                            json.line_num
                        );
                    }
                    if (!regex.IsMatch(str))
                    {
                        return new ValidationProblem(
                            ValidationProblemType.STRING_DOESNT_MATCH_PATTERN,
                            new Dictionary<string, object>
                            {
                                { "string", str },
                                { "regex", regex }
                            },
                            json.line_num
                        );
                    }
                    return null;
                };
            }
            // we just check that the JSON has the right type
            // it's a scalar, and at present no extra validation is done on scalars
            return (JNode json) =>
            {
                if (!TypeValidates(json.type, dtype))
                {
                    return new ValidationProblem(ValidationProblemType.TYPE_MISMATCH,
                        new Dictionary<string, object>
                        {
                            { "found", json.type },
                            { "required", dtype }
                        },
                        json.line_num
                    );
                }
                return null;
            };
        }

        public static ValidationProblem? Validates(JNode json, JNode schema_)
        {
            var validator = CompileValidationFunc(schema_);
            return validator(json);
        }
    }
}
