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

        public override string ToString() => Message;
    }

    public class JsonSchemaValidator
    {
        private const int RECURSION_LIMIT = 64;
        // the recursion limit is especially important because the "definitions" and "$ref" keywords
        // allow recursive self-references, and we need to avoid infinite recursion

        private static JsonLint ValidationProblemToLint(JsonLintType problemType, Dictionary<string, object> keywords, int position, bool translated)
        {
            string msg;
            switch (problemType)
            {
            case JsonLintType.SCHEMA_TYPE_MISMATCH:
                var foundType = JsonSchemaMaker.TypeName((Dtype)keywords["found"]);
                object required = keywords["required"];
                msg = JsonLint.TryTranslateWithTwoParams(translated, problemType, "found type {0}, expected type {1}.", foundType, JsonSchemaMaker.TypeName((Dtype)required));
                break;
            case JsonLintType.SCHEMA_TYPE_ARRAY_MISMATCH:
                foundType = JsonSchemaMaker.TypeName((Dtype)keywords["found"]);
                var reqArr = (JArray)keywords["required"];
                msg = JsonLint.TryTranslateWithTwoParams(translated, problemType, "found type {0}, expected one of the types {1}.", foundType, reqArr.ToString());
                break;
            case JsonLintType.SCHEMA_VALUE_NOT_IN_ENUM:
                var enum_ = (JArray)keywords["enum"];
                var foundNode = (JNode)keywords["found"];
                msg = JsonLint.TryTranslateWithTwoParams(translated, problemType, "found value {0}, but the only allowed values are {1}.", foundNode.ToString(), enum_.ToString());
                break;
            case JsonLintType.SCHEMA_ARRAY_TOO_SHORT:
                int foundLength = (int)keywords["found"];
                keywords.TryGetValue("minItems", out object minItemsObj);
                int minItems = 0;
                if (minItemsObj != null) minItems = (int)minItemsObj;
                msg = JsonLint.TryTranslateWithTwoParams(translated, problemType, "array required to have at least {0} items, but it has {1} items.", minItems, foundLength);
                break;
            case JsonLintType.SCHEMA_ARRAY_TOO_LONG:
                foundLength = (int)keywords["found"];
                keywords.TryGetValue("maxItems", out object maxItemsObj);
                int maxItems = 0;
                if (maxItemsObj != null) maxItems = (int)maxItemsObj;
                msg = JsonLint.TryTranslateWithTwoParams(translated, problemType, "array required to have no more than {0} items, but it has {1} items.", maxItems, foundLength);
                break;
            case JsonLintType.SCHEMA_CONTAINS_VIOLATION:
                var containsSchema = (JNode)keywords["contains"];
                var minContains = (int)keywords["minContains"];
                var maxContains = (int)keywords["maxContains"];
                try
                {
                    msg = string.Format(Translator.TranslateLintMessage(translated, problemType, "Array must have between {0} and {1} items that match \"contains\" schema {2}"), minContains, maxContains, containsSchema.ToString());
                }
                catch
                {
                    msg = string.Format("Array must have between {0} and {1} items that match \"contains\" schema {2}", minContains, maxContains, containsSchema.ToString());
                }
                break;
            case JsonLintType.SCHEMA_MINCONTAINS_VIOLATION:
                containsSchema = (JNode)keywords["contains"];
                minContains = (int)keywords["minContains"];
                msg = JsonLint.TryTranslateWithTwoParams(translated, problemType, "Array must have at least {0} items that match \"contains\" schema {1}", minContains, containsSchema.ToString());
                break;
            case JsonLintType.SCHEMA_OBJECT_MISSING_REQUIRED_KEY:
                string keyMissing = (string)keywords["required"];
                msg = JsonLint.TryTranslateWithOneParam(translated, problemType, "object missing \"required\" key {0}", keyMissing);
                break;
            case JsonLintType.SCHEMA_FALSE_SCHEMA:
                msg = Translator.TranslateLintMessage(translated, problemType, "the schema is false, so nothing will validate.");
                break;
            case JsonLintType.SCHEMA_STRING_DOESNT_MATCH_PATTERN:
                string str = (string)keywords["string"];
                Regex regex = (Regex)keywords["regex"];
                msg = JsonLint.TryTranslateWithTwoParams(translated, problemType, "string '{0}' does not match regex '{1}'", str, regex);
                break;
            case JsonLintType.SCHEMA_RECURSION_LIMIT_REACHED:
                msg = Translator.TranslateLintMessage(translated, problemType, "validation has a maximum depth of 128");
                break;
            case JsonLintType.SCHEMA_NUMBER_LESS_THAN_MIN:
                var min = (double)keywords["min"];
                var num = (double)keywords["num"];
                msg = JsonLint.TryTranslateWithTwoParams(translated, problemType, "number {0} less than minimum {1}", num, min);
                break;
            case JsonLintType.SCHEMA_NUMBER_GREATER_THAN_MAX:
                var max = (double)keywords["max"];
                num = (double)keywords["num"];
                msg = JsonLint.TryTranslateWithTwoParams(translated, problemType, "number {0} greater than maximum {1}", num, max);
                break;
            case JsonLintType.SCHEMA_STRING_TOO_LONG:
                var maxLength = (int)keywords["maxLength"];
                str = (string)keywords["string"];
                msg = JsonLint.TryTranslateWithTwoParams(translated, problemType, "string {0} had greater than maxLength {1}", str, maxLength);
                break;
            case JsonLintType.SCHEMA_STRING_TOO_SHORT:
                var minLength = (int)keywords["minLength"];
                str = (string)keywords["string"];
                msg = JsonLint.TryTranslateWithTwoParams(translated, problemType, "string {0} had less than minLength {1}", str, minLength);
                break;
            case JsonLintType.SCHEMA_NUMBER_LESSEQ_EXCLUSIVE_MIN:
                var exMin = (double)keywords["exMin"];
                num = (double)keywords["num"];
                msg = JsonLint.TryTranslateWithTwoParams(translated, problemType, "number {0} less than or equal to exclusive minimum {1}", num, exMin);
                break;
            case JsonLintType.SCHEMA_NUMBER_GREATEREQ_EXCLUSIVE_MAX:
                var exMax = (double)keywords["exMax"];
                num = (double)keywords["num"];
                msg = JsonLint.TryTranslateWithTwoParams(translated, problemType, "number {0} greater than or equal to exclusive maximum {1}", num, exMax);
                break;
            default: throw new ArgumentException($"Unknown validation problem type {problemType}");
            }
            return new JsonLint(JsonLintType.SCHEMA_TYPE_MISMATCH, position, '\x00', msg);
        }

        /// <summary>
        /// appends to lints a JsonLint with severity SCHEMA, curChar '\x00', position position, and some appropriate message<br></br>
        /// return lints.Count &lt;= maxLintCount (after appending the lint to lints)
        /// </summary>
        /// <param name="problemType"></param>
        /// <param name="position"></param>
        /// <param name="keywords"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static bool AppendValidationProblem(JsonLintType problemType, Dictionary<string, object> keywords, int position, List<JsonLint> lints, int maxLintCount, bool translated)
        {
            lints.Add(ValidationProblemToLint(problemType, keywords, position, translated));
            return lints.Count <= maxLintCount;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lints"></param>
        /// <param name="translated">currently does nothing, as schema validation errors are not yet translated</param>
        /// <returns></returns>
        public static string LintsAsJArrayString(List<JsonLint> lints, bool translated = false)
        {
            return new JArray(0, lints.Select(x => x.ToJson(false)).ToList()).ToString();
        }

        public delegate bool ValidationFunc(JNode x, out List<JsonLint> lints);

        public delegate void ValidationHelperFunc(JNode x, List<JsonLint> lints);

        private struct RegexAndValidator
        {
            public Regex regex;
            public ValidationHelperFunc validator;

            public RegexAndValidator(Regex regex, ValidationHelperFunc validator)
            {
                this.regex = regex;
                this.validator = validator;
            }
        }

        /// <summary>
        /// checks if the types are equal,
        /// OR if jsonType is integer and schemaType is number
        /// </summary>
        public static bool TypeValidates(Dtype jsonType, Dtype schemaType)
        {
            return jsonType == schemaType || jsonType == Dtype.INT && schemaType == Dtype.FLOAT;
        }

        /// <summary>
        /// Extracts all schemas with named definitions from a schema,
        /// and then compiles the schema into a validation function.
        /// </summary>
        /// <param name="schema_">a JNode representing a parsed JSON schema</param>
        /// <returns></returns>
        public static ValidationFunc CompileValidationFunc(JNode schema_, int maxLintCount, bool translated)
        {
            ValidationHelperFunc helper = (!(schema_ is JObject obj) || !(
                obj.children.TryGetValue("definitions", out JNode defs)
                || obj.children.TryGetValue("$defs", out defs)))
                ? // boolean schemas and schemas without a "$defs" or "definitions" keyword
                  CompileValidationHelperFunc(schema_, new JObject(), 0, maxLintCount, translated)
                : CompileValidationHelperFunc(schema_, (JObject)defs, 0, maxLintCount, translated);
            bool outfunc(JNode x, out List<JsonLint> lints)
            {
                lints = new List<JsonLint>();
                helper(x, lints);
                return lints.Count == 0;
            }
            return outfunc;
        }

        /// <summary>
        /// Compiles a schema into a validation function using named definitions for subschemas
        /// (if any exist)
        /// </summary>
        /// <param name="schema_">a JNode representing a parsed JSON schema</param>
        /// <param name="definitions"></param>
        /// <returns></returns>
        /// <exception cref="SchemaValidationException"></exception>
        public static ValidationHelperFunc CompileValidationHelperFunc(JNode schema_, JObject definitions, int recursions, int maxLintCount, bool translated)
        {
            if (recursions == RECURSION_LIMIT)
                return (x, lints) => AppendValidationProblem(JsonLintType.SCHEMA_RECURSION_LIMIT_REACHED, null, 0, lints, maxLintCount, translated);
            if (!(schema_ is JObject schema))
            {
                // the booleans are valid schemas.
                // true validates everything, and false validates nothing
                if (schema_ is null)
                    throw new SchemaValidationException("Schema was null (in the programmatic sense, not a JSON element representing null)");
                if (schema_.value is bool b)
                {
                    if (b)
                        return (_, __) => { };
                    return (x, lints) => AppendValidationProblem(JsonLintType.SCHEMA_FALSE_SCHEMA, null, x.position, lints, maxLintCount, translated);
                }
                throw new SchemaValidationException($"JSON schema must be object or boolean, got type {JNode.FormatDtype(schema_.type)}");
            }
            if (schema.Length == 0)
                return (_, __) => { }; // the empty schema validates everything
            if (schema.children.TryGetValue("$ref", out JNode refnode))
            {
                // a $ref should go to a "valid schema location path
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
                var func = CompileValidationHelperFunc(def, definitions, recursions, maxLintCount, translated);
                return func;
            }
            if (schema.children.TryGetValue("enum", out JNode enum_) && enum_ is JArray enumarr)
            {
                // the "enum" keyword means that the JSON must have
                // one of the values in the associated array.
                // since the enumeration array implicitly defines the allowed types, we don't need to specify a "type" keyword
                var enumMembers = enumarr.children;
                return (json, lints) =>
                {
                    foreach (JNode possible in enumMembers)
                    {
                        if (possible.type != json.type) continue;
                        if (possible.Equals(json)) return;
                    }
                    AppendValidationProblem(
                        JsonLintType.SCHEMA_VALUE_NOT_IN_ENUM,
                        new Dictionary<string, object>
                        {
                            { "found", json },
                            { "enum", enumarr },
                        },
                        json.position,
                        lints,
                        maxLintCount,
                        translated
                    );
                };
            }
            if (!schema.children.TryGetValue("type", out JNode type))
            {
                // an anyOf array of allowable schemas
                if (!schema.children.TryGetValue("anyOf", out JNode anyOf))
                {
                    throw new SchemaValidationException("Each schema must have one of the '$ref', 'anyOf', 'type', or 'enum' keywords.");
                }
                var subValidators = ((JArray)anyOf).children
                    .Select((subschema) => CompileValidationHelperFunc(subschema, definitions, recursions, maxLintCount, translated))
                    .ToArray();
                return (JNode json, List<JsonLint> lints) =>
                {
                    int initialLintCount = lints.Count;
                    foreach (var subValidator in subValidators)
                    {
                        int lintCountBefore = lints.Count;
                        subValidator(json, lints);
                        int newLintCount = lints.Count;
                        if (newLintCount == lintCountBefore)
                        {
                            // no new errors from this schema option, so anyOf is satisfied
                            // if any other schema options added errors, trim those off
                            lints.RemoveRange(initialLintCount, newLintCount - initialLintCount);
                            return;
                        }
                    }
                    // all schema options failed
                    AppendValidationProblem(JsonLintType.SCHEMA_TYPE_ARRAY_MISMATCH,
                        new Dictionary<string, object>
                        {
                            { "found", json.type },
                            { "required", anyOf }
                        },
                        json.position,
                        lints,
                        maxLintCount,
                        translated
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
                return (JNode json, List<JsonLint> lints) =>
                {
                    if ((json.type & dtypes) != 0 || ((json.type == Dtype.INT) && (dtypes & Dtype.FLOAT) != 0))
                        return;
                    AppendValidationProblem(types is JArray ? JsonLintType.SCHEMA_TYPE_ARRAY_MISMATCH : JsonLintType.SCHEMA_TYPE_MISMATCH,
                        new Dictionary<string, object>
                        {
                            { "found", json.type },
                            { "required", types }
                        },
                        json.position,
                        lints,
                        maxLintCount,
                        translated
                    );
                };
            }
            // now do any additional validation as needed
            var dtype = JsonSchemaMaker.typeNameToDtype[(string)type.value];
            // validation logic for arrays
            if (dtype == Dtype.ARR)
            {
                int maxItems = (schema.children.TryGetValue("maxItems", out JNode maxItemsNode))
                    ? Convert.ToInt32(maxItemsNode.value)
                    : int.MaxValue;
                int minItems = (schema.children.TryGetValue("minItems", out JNode minItemsNode))
                    ? Convert.ToInt32(minItemsNode.value)
                    : 0;
                Func<JArray, JsonLint?> ItemsRightLength;
                if (minItems == 0 && maxItems == int.MaxValue)
                    ItemsRightLength = (arr) => null;
                else
                {
                    ItemsRightLength = (arr) =>
                    {
                        var len_ = arr.children.Count;
                        if (len_ < minItems) // minItems sets minimum length for array
                        {
                            return ValidationProblemToLint(
                                JsonLintType.SCHEMA_ARRAY_TOO_SHORT,
                                new Dictionary<string, object>
                                {
                                    { "found", len_ },
                                    { "minItems", minItems },
                                },
                                arr.position,
                                translated
                            );
                        }
                        if (len_ > maxItems) // maxItems sets maximum length for array
                        {
                            return ValidationProblemToLint(
                                JsonLintType.SCHEMA_ARRAY_TOO_LONG,
                                new Dictionary<string, object>
                                {
                                    { "found", len_ },
                                    { "maxItems", maxItems },
                                },
                                arr.position,
                                translated
                            );
                        }
                        return null;
                    };
                }
                if (schema.children.TryGetValue("items", out JNode items))
                {
                    var itemsValidator = CompileValidationHelperFunc(items, definitions, recursions + 1, maxLintCount, translated);
                    if (schema.children.TryGetValue("contains", out JNode contains))
                    {
                        // "contains" keyword adds another schema that some members
                        // must match instead of the "items" schema
                        var containsValidator = CompileValidationHelperFunc(contains, definitions, recursions + 1, maxLintCount, translated);
                        var minContains = schema.children.TryGetValue("minContains", out JNode minContainsNode)
                            ? Convert.ToInt32(minContainsNode.value)
                            : 1;
                        var maxContains = schema.children.TryGetValue("maxContains", out JNode maxContainsNode)
                            ? Convert.ToInt32(maxContainsNode.value)
                            : int.MaxValue;
                        return (json, lints) =>
                        {
                            if (!(json is JArray arr))
                            {
                                lints.Add(ValidationProblemToLint(JsonLintType.SCHEMA_TYPE_MISMATCH,
                                    new Dictionary<string, object>
                                    {
                                    { "found", json.type },
                                    { "required", Dtype.ARR }
                                    },
                                    json.position,
                                    translated
                                ));
                                return;
                            }
                            var itemsRightLength = ItemsRightLength(arr);
                            if (itemsRightLength.HasValue)
                            {
                                lints.Add(itemsRightLength.Value);
                                if (lints.Count > maxLintCount)
                                    return;
                            }
                            var containsCount = 0;
                            foreach (JNode child in arr.children)
                            {
                                int lintCountBeforeContains = lints.Count;
                                containsValidator(child, lints);
                                int lintsCountAfterContains = lints.Count;
                                if (lintsCountAfterContains > lintCountBeforeContains)
                                {
                                    // it doesn't match the "contains" schema. Maybe it matches the normal "items" schema?
                                    itemsValidator(child, lints);
                                    int lintsCountAfterItems = lints.Count;
                                    if (lintsCountAfterItems == lintsCountAfterContains)
                                    {
                                        // it matches the "items" schema, so the failure to match the "contains" schema is fine
                                        lints.RemoveRange(lintCountBeforeContains, lintsCountAfterItems - lintCountBeforeContains);
                                    }
                                    else if (lints.Count > maxLintCount)
                                        return;
                                }
                                else containsCount++;
                            }
                            if (containsCount < minContains || containsCount > maxContains)
                            {
                                JsonLintType lintType = maxContains == int.MaxValue ? JsonLintType.SCHEMA_MINCONTAINS_VIOLATION : JsonLintType.SCHEMA_CONTAINS_VIOLATION;
                                lints.Add(ValidationProblemToLint(lintType,
                                    new Dictionary<string, object>
                                    {
                                        { "contains", contains },
                                        { "minContains", minContains },
                                        { "maxContains", maxContains },
                                    },
                                    arr.position,
                                    translated
                                ));
                            }
                        };
                    }
                    // no "contains" keyword, just validate all items with "items" keyword
                    return (JNode json, List<JsonLint> lints) =>
                    {
                        if (!(json is JArray arr))
                        {
                            lints.Add(ValidationProblemToLint(JsonLintType.SCHEMA_TYPE_MISMATCH,
                                new Dictionary<string, object>
                                {
                                    { "found", json.type },
                                    { "required", Dtype.ARR }
                                },
                                json.position,
                                translated
                            ));
                            return;
                        }
                        var itemsRightLength = ItemsRightLength(arr);
                        if (itemsRightLength.HasValue)
                        {
                            lints.Add(itemsRightLength.Value);
                            if (lints.Count > maxLintCount)
                                return;
                        }
                        foreach (JNode child in arr.children)
                        {
                            itemsValidator(child, lints);
                            if (lints.Count > maxLintCount)
                                return;
                        }
                    };
                }
                // no "items" keyword means the array could hold anything or nothing
                else return (json, lints) =>
                {
                    if (json is JArray arr)
                    {
                        var lint = ItemsRightLength(arr);
                        if (lint.HasValue)
                            lints.Add(lint.Value);
                        return;
                    }
                    AppendValidationProblem(JsonLintType.SCHEMA_TYPE_MISMATCH,
                        new Dictionary<string, object>
                        {
                            { "found", json.type },
                            { "required", Dtype.ARR }
                        },
                        json.position,
                        lints,
                        maxLintCount,
                        translated
                    );

                };
            }
            // validation logic for objects
            if (dtype == Dtype.OBJ)
            {
                var propsValidators = new Dictionary<string, ValidationHelperFunc>();
                if (schema.children.TryGetValue("properties", out JNode properties))
                {
                    // standard validation: one schema per key in "properties"
                    JObject props = (JObject)properties;
                    foreach (KeyValuePair<string, JNode> kv in props.children)
                    {
                        propsValidators[kv.Key] = CompileValidationHelperFunc(kv.Value, definitions, recursions + 1, maxLintCount, translated);
                    }
                }
                string[] required = schema.children.TryGetValue("required", out JNode requiredNode)
                    ? ((JArray)requiredNode).children.Select((x) => (string)x.value).ToArray()
                    : new string[0];
                // return true iff lints.Count <= maxLintCount after validating under patternProperties
                Func<JObject, List<JsonLint>, bool> patternPropsValidate;
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
                            var validator = CompileValidationHelperFunc(kv.Value, definitions, recursions + 1, maxLintCount, translated);
                            return new RegexAndValidator(new Regex(pat, RegexOptions.Compiled), validator);
                        })
                        .ToArray();
                    patternPropsValidate = (JObject obj, List<JsonLint> lints) =>
                    {
                        foreach (var regexAndValidator in patternPropsValidators)
                        {
                            var regex = regexAndValidator.regex;
                            foreach (KeyValuePair<string, JNode> kv in obj.children)
                            {
                                if (regex.IsMatch(kv.Key))
                                {
                                    regexAndValidator.validator(kv.Value, lints);
                                    if (lints.Count > maxLintCount)
                                        return false;
                                }
                            }
                        }
                        return true;
                    };
                }
                else patternPropsValidate = (obj, lints) => true;
                return (json, lints) =>
                {
                    if (!(json is JObject obj))
                    {
                        lints.Add(ValidationProblemToLint(JsonLintType.SCHEMA_TYPE_MISMATCH,
                            new Dictionary<string, object>
                            {
                                { "found", json.type },
                                { "required", Dtype.OBJ }
                            },
                            json.position,
                            translated
                        ));
                        return;
                    }
                    // check if object has all required keys
                    foreach (string requiredKey in required)
                    {
                        if (!obj.children.ContainsKey(requiredKey))
                        {
                            if (!AppendValidationProblem(
                                JsonLintType.SCHEMA_OBJECT_MISSING_REQUIRED_KEY,
                                new Dictionary<string, object>
                                {
                                    { "required", requiredKey },
                                },
                                obj.position,
                                lints,
                                maxLintCount,
                                translated
                            ))
                                return;
                        }
                    }
                    patternPropsValidate(obj, lints);
                    if (lints.Count > maxLintCount)
                        return;
                    foreach (string key in propsValidators.Keys)
                    {
                        // for each property name in the schema, make sure that
                        // the JSON has the corresponding key and that the associated value
                        // validates with the property name's subschema.
                        if (!obj.children.TryGetValue(key, out JNode value))
                            continue;
                        propsValidators[key](value, lints);
                        if (lints.Count > maxLintCount)
                            return;
                    }
                };
            }
            if (dtype == Dtype.STR)
            {
                bool hasSpecialStringKeywords = false;
                // validation to test if string matches regex
                Func<string, int, List<JsonLint>, bool> regexValidator;
                if (schema.children.TryGetValue("pattern", out JNode pattern))
                {
                    hasSpecialStringKeywords = true;
                    Regex regex = new Regex((string)pattern.value, RegexOptions.Compiled);
                    regexValidator = (str, lineNum, lints) =>
                    {
                        if (!regex.IsMatch(str))
                        {
                            return AppendValidationProblem(
                                JsonLintType.SCHEMA_STRING_DOESNT_MATCH_PATTERN,
                                new Dictionary<string, object>
                                {
                                    { "string", str },
                                    { "regex", regex }
                                },
                                lineNum,
                                lints,
                                maxLintCount,
                                translated
                            );
                        }
                        return true;
                    };
                }
                else regexValidator = (_, __, ___) => true;
                // validation of string length
                var minLength = schema.children.TryGetValue("minLength", out JNode minLengthNode)
                    ? Convert.ToInt32(minLengthNode.value)
                    : 0;
                var maxLength = schema.children.TryGetValue("maxLength", out JNode maxLengthNode)
                    ? Convert.ToInt32(maxLengthNode.value)
                    : int.MaxValue;
                Func<string, int, List<JsonLint>, bool> lengthValidator;
                if (minLength == 0 && maxLength == int.MaxValue)
                {
                    lengthValidator = (_, __, ___) => true;
                }
                else
                {
                    hasSpecialStringKeywords = true;
                    lengthValidator = (string s, int position, List<JsonLint> lints) =>
                    {
                        if (s.Length < minLength)
                        {
                            lints.Add(ValidationProblemToLint(
                                JsonLintType.SCHEMA_STRING_TOO_SHORT,
                                new Dictionary<string, object>
                                {
                                    { "string", s },
                                    { "minLength", minLength }
                                },
                                position,
                                translated
                            ));
                        }
                        else if (s.Length > maxLength)
                        {
                            lints.Add(ValidationProblemToLint(
                                JsonLintType.SCHEMA_STRING_TOO_LONG,
                                new Dictionary<string, object>
                                {
                                    { "string", s },
                                    { "maxLength", maxLength }
                                },
                                position,
                                translated
                            ));
                        }
                        return true;
                    };
                }
                if (hasSpecialStringKeywords)
                {
                    return (json, lints) =>
                    {
                        if (!(json.value is string str))
                        {
                            lints.Add(ValidationProblemToLint(
                                JsonLintType.SCHEMA_TYPE_MISMATCH,
                                new Dictionary<string, object>
                                {
                                    { "found", json.type },
                                    { "required", Dtype.STR },
                                },
                                json.position,
                                translated
                            ));
                            return;
                        }
                        regexValidator(str, json.position, lints);
                        lengthValidator(str, json.position, lints);
                    };
                }
                // otherwise no special validation, just check if it's a string
            }

            if ((dtype & Dtype.FLOAT_OR_INT) != 0)
            {
                var minimum = schema.children.TryGetValue("minimum", out JNode minNode)
                    ? Convert.ToDouble(minNode.value)
                    : NanInf.neginf;
                var maximum = schema.children.TryGetValue("maximum", out JNode maxNode)
                    ? Convert.ToDouble(maxNode.value)
                    : NanInf.inf;
                var exclusiveMin = schema.children.TryGetValue("exclusiveMinimum", out JNode exclMinNode)
                    ? Convert.ToDouble(exclMinNode.value)
                    : NanInf.neginf;
                var exclusiveMax = schema.children.TryGetValue("exclusiveMaximum", out JNode exclMaxNode)
                    ? Convert.ToDouble(exclMaxNode.value)
                    : NanInf.inf;
                if (!double.IsInfinity(minimum) || !double.IsInfinity(maximum) ||
                    !double.IsInfinity(exclusiveMin) || !double.IsInfinity(exclusiveMax))
                {
                    // the user specified minimum and/or maximum values for numbers, so we need to check that
                    return (JNode json, List<JsonLint> lints) =>
                    {
                        if (!TypeValidates(json.type, dtype))
                        {
                            lints.Add(ValidationProblemToLint(JsonLintType.SCHEMA_TYPE_MISMATCH,
                                new Dictionary<string, object>
                                {
                                    { "found", json.type },
                                    { "required", dtype }
                                },
                                json.position,
                                translated
                            ));
                            return;
                        }
                        var floatedValue = Convert.ToDouble(json.value);
                        if (floatedValue < minimum)
                            lints.Add(ValidationProblemToLint(
                                JsonLintType.SCHEMA_NUMBER_LESS_THAN_MIN,
                                new Dictionary<string, object> { { "min", minimum }, { "num", floatedValue } },
                                json.position,
                                translated
                            ));
                        else if (floatedValue > maximum)
                            lints.Add(ValidationProblemToLint(
                                JsonLintType.SCHEMA_NUMBER_GREATER_THAN_MAX,
                                new Dictionary<string, object> { { "max", maximum }, { "num", floatedValue } },
                                json.position,
                                translated
                            ));
                        if (floatedValue <= exclusiveMin)
                            lints.Add(ValidationProblemToLint(
                                JsonLintType.SCHEMA_NUMBER_LESSEQ_EXCLUSIVE_MIN,
                                new Dictionary<string, object> { { "exMin", exclusiveMin }, { "num", floatedValue } },
                                json.position,
                                translated
                            ));
                        else if (floatedValue >= exclusiveMax)
                            lints.Add(ValidationProblemToLint(
                                JsonLintType.SCHEMA_NUMBER_GREATEREQ_EXCLUSIVE_MAX,
                                new Dictionary<string, object> { { "exMax", exclusiveMax }, { "num", floatedValue } },
                                json.position,
                                translated
                            ));
                    };
                }
            }
            // we just check that the JSON has the right type
            // it's a scalar schema with no extra fancy validation keywords
            return (json, lints) =>
            {
                if (!TypeValidates(json.type, dtype))
                {
                    lints.Add(ValidationProblemToLint(JsonLintType.SCHEMA_TYPE_MISMATCH,
                        new Dictionary<string, object>
                        {
                            { "found", json.type },
                            { "required", dtype }
                        },
                        json.position,
                        translated
                    ));
                }
            };
        }

        public static bool Validates(JNode json, JNode schema_, int maxLintCount, bool translated, out List<JsonLint> lints)
        {
            var validator = CompileValidationFunc(schema_, maxLintCount, translated);
            return validator(json, out lints);
        }
    }
}
