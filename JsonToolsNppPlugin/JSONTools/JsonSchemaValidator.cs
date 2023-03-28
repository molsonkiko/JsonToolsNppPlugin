using JSON_Tools.Utils;
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
        private const int RECURSION_LIMIT = 64;
        // the recursion limit is especially important because the "definitions" and "$ref" keywords
        // allow recursive self-references, and we need to avoid infinite recursion

        public enum ValidationProblemType
        {
            TYPE_MISMATCH,
            VALUE_NOT_IN_ENUM,
            ARRAY_TOO_LONG,
            ARRAY_TOO_SHORT,
            CONTAINS_KEYWORD_VIOLATION,
            OBJECT_MISSING_REQUIRED_KEY,
            FALSE_SCHEMA, // nothing validates
            STRING_DOESNT_MATCH_PATTERN,
            RECURSION_LIMIT_REACHED,
            NUMBER_LESS_THAN_MIN,
            NUMBER_GREATER_THAN_MAX,
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
                    case ValidationProblemType.CONTAINS_KEYWORD_VIOLATION:
                        var contains_schema = (JNode)keywords["contains"];
                        var minContains = (int)keywords["minContains"];
                        var maxContains = (int)keywords["maxContains"];
                        var quantifier = maxContains == Int32.MaxValue
                            ? $"at least {minContains}"
                            : $"between {minContains} and {maxContains}";
                        return msg + $"Array must have {quantifier} items that match \"contains\" schema {contains_schema}";
                    case ValidationProblemType.OBJECT_MISSING_REQUIRED_KEY:
                        string key_missing = (string)keywords["required"];
                        return msg + $"object missing required key {key_missing}";
                    case ValidationProblemType.FALSE_SCHEMA:
                        return msg + "the schema is `false`, so nothing will validate.";
                    case ValidationProblemType.STRING_DOESNT_MATCH_PATTERN:
                        string str = (string)keywords["string"];
                        Regex regex = (Regex)keywords["regex"];
                        return msg + $"string '{str}' does not match regex '{regex}'";
                    case ValidationProblemType.RECURSION_LIMIT_REACHED:
                        return msg + "validation has a maximum depth of 128";
                    case ValidationProblemType.NUMBER_LESS_THAN_MIN:
                        var min = (double)keywords["min"];
                        var num = (double)keywords["num"];
                        return msg + $"number {num} less than minimum {min}";
                    case ValidationProblemType.NUMBER_GREATER_THAN_MAX:
                        var max = (double)keywords["max"];
                        num = (double)keywords["num"];
                        return msg + $"number {num} greater than maximum {max}";
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

        /// <summary>
        /// Extracts all schemas with named definitions from a schema,
        /// and then compiles the schema into a validation function.
        /// </summary>
        /// <param name="schema_">a JNode representing a parsed JSON schema</param>
        /// <returns></returns>
        public static Func<JNode, ValidationProblem?> CompileValidationFunc(JNode schema_)
        {
            if (!(schema_ is JObject obj) || !(
                obj.children.TryGetValue("definitions", out JNode defs)
                || obj.children.TryGetValue("$defs", out defs)))
            {
                // boolean schemas and schemas without a "$defs" or "definitions" keyword
                return CompileValidationFuncHelper(schema_, new JObject(), 0);
            }
            return CompileValidationFuncHelper(schema_, (JObject)defs, 0);
        }

        /// <summary>
        /// Compiles a schema into a validation function using named definitions for subschemas
        /// (if any exist)
        /// </summary>
        /// <param name="schema_">a JNode representing a parsed JSON schema</param>
        /// <param name="definitions"></param>
        /// <returns></returns>
        /// <exception cref="SchemaValidationException"></exception>
        public static Func<JNode, ValidationProblem?> CompileValidationFuncHelper(JNode schema_, JObject definitions, int recursions)
        {
            if (recursions == RECURSION_LIMIT)
                return (x) => new ValidationProblem(ValidationProblemType.RECURSION_LIMIT_REACHED, new Dictionary<string, object> { }, 0);
            if (!(schema_ is JObject schema))
            {
                // the booleans are valid schemas.
                // true validates everything, and false validates nothing
                if ((bool)schema_.value) return (x) => null;
                return (x) => new ValidationProblem(ValidationProblemType.FALSE_SCHEMA, new Dictionary<string, object>(), x.line_num);
            }
            if (schema.Length == 0)
                return (x) => null; // the empty schema validates everything
            if (schema.children.TryGetValue("$ref", out JNode refnode))
            {
                // a $ref should go to a "valid schema location path"
                // which would look something like "#/$defs/foo"
                // or "#/definitions/bar".
                // there's a lot of detail in the JSON schema specification
                // at https://json-schema.org/draft/2020-12/json-schema-core.html#name-schema-references
                // for our purposes, we assume that the "$ref" string
                // is probably delimited by the '/' character, and whatever
                // comes after the last delimiter is the actual reference.
                var reference = ((string)refnode.value).Split('/').Last();
                if (!definitions.children.TryGetValue(reference, out JNode def))
                {
                    throw new SchemaValidationException($"\"$ref\" {refnode} does not point to a definition defined in the definitions:\r\n{definitions}");
                }
                // we now use the schema that was pointed to
                var func = CompileValidationFuncHelper(def, definitions, recursions);
                return func;
            }
            if (!schema.children.TryGetValue("type", out JNode type))
            {
                // an anyOf array of allowable schemas
                if (!schema.children.TryGetValue("anyOf", out JNode anyOf))
                {
                    throw new SchemaValidationException("Each schema must have one of the '$ref', 'anyOf', or 'type' keywords.");
                }
                var subValidators = ((JArray)anyOf).children
                    .Select((subschema) => CompileValidationFuncHelper(subschema, definitions, recursions))
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
                var dtypes = Dtype.TYPELESS;
                foreach (var typenode in types.children)
                {
                    var dtype_ = JsonSchemaMaker.typeNameToDtype[(string)typenode.value];
                    dtypes = JsonSchemaMaker.DtypeUnion(dtypes, dtype_);
                }
                return (JNode json) =>
                {
                    if ((json.type & dtypes) != 0 || ((json.type == Dtype.INT) && (dtypes & Dtype.FLOAT) != 0))
                        return null;
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
                    var itemsValidator = CompileValidationFuncHelper(items, definitions, recursions + 1);
                    if (schema.children.TryGetValue("contains", out JNode contains))
                    {
                        var containsValidator = CompileValidationFuncHelper(contains, definitions, recursions + 1);
                        var minContains = schema.children.TryGetValue("minContains", out JNode minContainsNode)
                            ? Convert.ToInt32(minContainsNode.value)
                            : 1;
                        var maxContains = schema.children.TryGetValue("maxContains", out JNode maxContainsNode)
                            ? Convert.ToInt32(maxContainsNode.value)
                            : Int32.MaxValue;
                        return (json) =>
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
                            var containsCount = 0;
                            foreach (JNode child in arr.children)
                            {
                                if (containsValidator(child) != null)
                                {
                                    // it doesn't match the "contains" schema. Maybe it matches the normal "items" schema?
                                    var vp = itemsValidator(child);
                                    if (vp != null) return vp;
                                }
                                else containsCount++;
                            }
                            if (containsCount >= minContains && containsCount <= maxContains)
                                return null;
                            return new ValidationProblem(ValidationProblemType.CONTAINS_KEYWORD_VIOLATION,
                                new Dictionary<string, object>
                                {
                                    { "contains", contains },
                                    { "minContains", minContains },
                                    { "maxContains", maxContains },
                                },
                                arr.line_num
                            );
                        };
                    }
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
                        propsValidators[key] = CompileValidationFuncHelper(props[key], definitions, recursions + 1);
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
                            var validator = CompileValidationFuncHelper(kv.Value, definitions, recursions + 1);
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

            if ((dtype & Dtype.FLOAT_OR_INT) != 0)
            {
                var minimum = schema.children.TryGetValue("minimum", out JNode minNode)
                    ? Convert.ToDouble(minNode.value)
                    : NanInf.neginf;
                var maximum = schema.children.TryGetValue("maximum", out JNode maxNode)
                    ? Convert.ToDouble(maxNode.value)
                    : NanInf.inf;
                if (!double.IsInfinity(minimum) || !double.IsInfinity(maximum))
                {
                    // the user specified minimum and/or maximum values for numbers, so we need to check that
                    return (JNode json) =>
                    {
                        if (json.type != Dtype.INT && json.type != Dtype.FLOAT)
                        {
                            return new ValidationProblem(ValidationProblemType.TYPE_MISMATCH,
                                new Dictionary<string, object>
                                {
                                    { "found", json.type },
                                    { "required", Dtype.FLOAT }
                                },
                                json.line_num
                            );
                        }
                        var floatedValue = Convert.ToDouble(json.value);
                        if (floatedValue < minimum)
                            return new ValidationProblem(
                                ValidationProblemType.NUMBER_LESS_THAN_MIN,
                                new Dictionary<string, object> { { "min", minimum }, { "num", floatedValue } },
                                json.line_num
                            );
                        if (floatedValue > maximum)
                            return new ValidationProblem(
                                ValidationProblemType.NUMBER_GREATER_THAN_MAX,
                                new Dictionary<string, object> { { "max", maximum }, { "num", floatedValue } },
                                json.line_num
                            );
                        return null;
                    };
                }
            }
            // we just check that the JSON has the right type
            // it's a scalar schema with no extra fancy validation keywords
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
