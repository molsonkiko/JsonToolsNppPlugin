using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Runtime;
using JSON_Tools.Utils;

namespace JSON_Tools.JSON_Tools
{
    public class RandomJsonFromSchema
    {
        public static Random random { get; private set; } = new Random();

        public static void Reseed()
        {
            random = new Random();
        }

        #region RANDOM_SCALARS
        /// <summary>
        /// 50% chance for true or false
        /// </summary>
        private static JNode RandomBoolean(JNode schema, JObject refs, int minArrayLength, int maxArrayLength, bool extendedAsciiStrings)
        {
            if (random.NextDouble() < 0.5)
                return new JNode(true, Dtype.BOOL, 0);
            return new JNode(false, Dtype.BOOL, 0);
        }

        /// <summary>
        /// random double from -5 to 5
        /// </summary>
        private static JNode RandomFloat(JNode schema, JObject refs, int minArrayLength, int maxArrayLength, bool extendedAsciiStrings)
        {
            return new JNode(random.NextDouble() * 10 - 5, Dtype.FLOAT, 0);
        }

        /// <summary>
        /// random int from -1 million to 1 million 
        /// </summary>
        private static JNode RandomInt(JNode schema, JObject refs, int minArrayLength, int maxArrayLength, bool extendedAsciiStrings)
        {
            return new JNode((long)random.Next(-1_000_000, 1_000_001), Dtype.INT, 0);
        }
        
        private static JNode RandomNumberBetweenMinAndMax(double min, double max)
        {
            if (min == NanInf.neginf) min = -1e6;
            if (max == NanInf.inf) max = 1e6;
            if (min > max) min = max - 1e6;
            var randNumber = random.NextDouble() * (max - min) + min;
            if (random.NextDouble() < 0.5)
                return new JNode(randNumber, Dtype.FLOAT, 0);
            var rounded = Convert.ToInt64(randNumber);
            return new JNode(rounded, Dtype.INT, 0);
        }

        private static JNode RandomIntegerBetweenMinAndMax(double min, double max)
        {
            var rand = RandomNumberBetweenMinAndMax(min, max);
            if (rand.type == Dtype.FLOAT)
                return new JNode(Convert.ToInt64(rand.value), Dtype.INT, 0);
            return rand;
        }

        /// <summary>
        /// 50% chance of random int, 50% chance of random float
        /// </summary>
        private static JNode RandomNumber(JNode schema, JObject refs, int minArrayLength, int maxArrayLength, bool extendedAsciiStrings)
        {
            if (random.NextDouble() < 0.5)
                return RandomInt(schema, refs, minArrayLength, maxArrayLength, extendedAsciiStrings);
            return RandomFloat(schema, refs, minArrayLength, maxArrayLength, extendedAsciiStrings);
        }

        /// <summary>
        /// always returns the null JNode
        /// </summary>
        private static JNode RandomNull(JNode schema, JObject refs, int minArrayLength, int maxArrayLength, bool extendedAsciiStrings)
        {
            return new JNode();
        }

        private static readonly string PRINTABLE = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~ \t\r\n";

        private static readonly string EXTENDED_ASCII = "\x00\x01\x02\x03\x04\x05\x06\x07\x08\x09\x0a\x0b\x0c\x0d\x0e\x0f\x10\x11\x12\x13\x14\x15\x16\x17\x18\x19\x1a\x1b\x1c\x1d\x1e\x1f\x20\x21\x22\x23\x24\x25\x26\x27\x28\x29\x2a\x2b\x2c\x2d\x2e\x2f\x30\x31\x32\x33\x34\x35\x36\x37\x38\x39\x3a\x3b\x3c\x3d\x3e\x3f\x40\x41\x42\x43\x44\x45\x46\x47\x48\x49\x4a\x4b\x4c\x4d\x4e\x4f\x50\x51\x52\x53\x54\x55\x56\x57\x58\x59\x5a\x5b\x5c\x5d\x5e\x5f\x60\x61\x62\x63\x64\x65\x66\x67\x68\x69\x6a\x6b\x6c\x6d\x6e\x6f\x70\x71\x72\x73\x74\x75\x76\x77\x78\x79\x7a\x7b\x7c\x7d\x7e\x7f\x80\x81\x82\x83\x84\x85\x86\x87\x88\x89\x8a\x8b\x8c\x8d\x8e\x8f\x90\x91\x92\x93\x94\x95\x96\x97\x98\x99\x9a\x9b\x9c\x9d\x9e\x9f\xa0\xa1\xa2\xa3\xa4\xa5\xa6\xa7\xa8\xa9\xaa\xab\xac\xad\xae\xaf\xb0\xb1\xb2\xb3\xb4\xb5\xb6\xb7\xb8\xb9\xba\xbb\xbc\xbd\xbe\xbf\xc0\xc1\xc2\xc3\xc4\xc5\xc6\xc7\xc8\xc9\xca\xcb\xcc\xcd\xce\xcf\xd0\xd1\xd2\xd3\xd4\xd5\xd6\xd7\xd8\xd9\xda\xdb\xdc\xdd\xde\xdf\xe0\xe1\xe2\xe3\xe4\xe5\xe6\xe7\xe8\xe9\xea\xeb\xec\xed\xee\xef\xf0\xf1\xf2\xf3\xf4\xf5\xf6\xf7\xf8\xf9\xfa\xfb\xfc\xfd\xfe\xff";

        /// <summary>
        /// string of random length between 0 and 10<br></br>
        /// Can contain any printable ASCII character, possibly multiple times
        /// </summary>
        private static JNode RandomString(JNode schema, JObject refs, int minArrayLength, int maxArrayLength, bool extendedAsciiStrings)
        {
            int minLength = 0;
            int exclusiveMaxLength = 11;
            if (schema is JObject obj)
            {
                if (obj.children.TryGetValue("minLength", out JNode minLengthNode) && minLengthNode.value is long minLengthVal)
                    minLength = (int)minLengthVal;
                if (obj.children.TryGetValue("maxLength", out JNode maxLengthNode) && maxLengthNode.value is long maxLengthVal)
                    exclusiveMaxLength = (int)maxLengthVal + 1;
            }
            int length = random.Next(minLength, exclusiveMaxLength);
            StringBuilder sb = new StringBuilder(length);
            if (extendedAsciiStrings)
            {
                for (int ii = 0; ii < length; ii++)
                {
                    char randChar = EXTENDED_ASCII[random.Next(1, 256)]; // not allowing \x00 because that will terminate string early in C
                    sb.Append(randChar);
                }
            }
            else
            {
                for (int ii = 0; ii < length; ii++)
                {
                    char randChar = PRINTABLE[random.Next(PRINTABLE.Length)];
                    sb.Append(randChar);
                }
            }
            return new JNode(sb.ToString(), Dtype.STR, 0);
        }
        #endregion

        #region RANDOM_ITERABLES
        private static JNode RandomArray(JNode schema, JObject refs, int minArrayLength, int maxArrayLength, bool extendedAsciiStrings)
        {
            Dictionary<string, JNode> children = ((JObject)schema).children;
            if (!children.TryGetValue("items", out JNode items))
            {
                return new JArray();
                // no "items" keyword technically means that the array could contain
                // anything, but we will just produce empty arrays for such schemas
            }
            int minlen = minArrayLength; 
            if (children.TryGetValue("minItems", out JNode minItemsNode))
                minlen = Convert.ToInt32(minItemsNode.value);
            int maxlen = maxArrayLength;
            if (children.TryGetValue("maxItems", out JNode maxItemsNode))
                maxlen = Convert.ToInt32(maxItemsNode.value);
            int length = random.Next(minlen, maxlen + 1);
            var outItems = new List<JNode>();
            if (children.TryGetValue("contains", out JNode contains))
            {
                // this is a separate schema from "items", for items that MUST be
                // included in the array.
                int minContains = 1;
                if (children.TryGetValue("minContains", out JNode minContainsNode))
                    minContains = Convert.ToInt32(minContainsNode.value);
                int maxContains = length;
                if (children.TryGetValue("maxContains", out JNode maxContainsNode))
                    maxContains = Convert.ToInt32(maxContainsNode.value);
                int ncontains = random.Next(minContains, maxContains + 1);
                length -= ncontains;
                for (int ii = 0; ii < ncontains; ii++)
                    outItems.Add(RandomJsonHelper(contains, refs, minArrayLength, maxArrayLength, extendedAsciiStrings));
            }
            for (int ii = 0; ii < length; ii++)
                outItems.Add(RandomJsonHelper(items, refs, minArrayLength, maxArrayLength, extendedAsciiStrings));
            if (contains != null)
            {
                // shuffle the array to randomize the order in which
                // the "contains" items and the "items" items appear
                for (int ii = 0; ii < outItems.Count; ii++)
                {
                    int jj = random.Next(0, ii + 1);
                    JNode temp = outItems[jj];
                    outItems[jj] = outItems[ii];
                    outItems[ii] = temp;
                }
            }
            return new JArray(0, outItems);
        }

        private static JNode RandomObject(JNode schema, JObject refs, int minArrayLength, int maxArrayLength, bool extendedAsciiStrings)
        {
            var children = ((JObject)schema).children;
            if (!children.TryGetValue("properties", out JNode properties))
            {
                return new JObject();
                // as with an array with no "items" keyword,
                // an object with no "properties" keyword could contain anything
            }
            var propertiesObj = (JObject)properties;
            HashSet<string> optionalKeys = new HashSet<string>(propertiesObj.children.Keys);
            HashSet<string> requiredKeys = new HashSet<string>();
            if (children.TryGetValue("required", out JNode requiredNode) && requiredNode is JArray requiredArr)
            {
                foreach (JNode req in requiredArr.children)
                {
                    string reqKey = (string)req.value;
                    optionalKeys.Remove(reqKey);
                    requiredKeys.Add(reqKey);
                }
            }
            var optionalIncluded = new List<string>();
            if (optionalKeys.Count > 0)
            {
                int numOptionalIncluded = random.Next(optionalKeys.Count + 1);
                var allOptional = optionalKeys.ToArray();
                HashSet<int> indicesChosen = new HashSet<int>();
                // choose a random subset of the optional keys to include in the object
                // This may mean choosing no optional keys at all.
                for (int ii = 0; ii < numOptionalIncluded; ii++)
                {
                    int idx = random.Next(allOptional.Length);
                    while (indicesChosen.Contains(idx))
                        idx = random.Next(allOptional.Length);
                    indicesChosen.Add(idx);
                    optionalIncluded.Add(allOptional[idx]);
                }
            }
            var result = new Dictionary<string, JNode>();
            foreach (string k in requiredKeys.Concat(optionalIncluded))
            {
                JNode subschema = propertiesObj[k];
                result[k] = RandomJsonHelper(subschema, refs, minArrayLength, maxArrayLength, extendedAsciiStrings);
            }
            return new JObject(0, result);
        }
        #endregion

        private static readonly Dictionary<string, Func<JNode, JObject, int, int, bool, JNode>> GENERATORS = new Dictionary<string, Func<JNode, JObject, int, int, bool, JNode>>
        {
            { "array", RandomArray },
            { "boolean", RandomBoolean },
            { "integer", RandomInt },
            { "null", RandomNull },
            { "number", RandomNumber },
            { "object", RandomObject },
            { "string", RandomString },
        };

        /// <summary>
        /// Produces a random value of a random type.
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="minArrayLength"></param>
        /// <param name="maxArrayLength"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private static JNode RandomAnything(JNode schema, JObject refs, int minArrayLength, int maxArrayLength, bool extendedAsciiStrings)
        {
            int choice = random.Next(7);
            switch (choice)
            {
                case 0: return RandomBoolean(schema, refs, minArrayLength, maxArrayLength, extendedAsciiStrings);
                case 1: return RandomFloat(schema, refs, minArrayLength, maxArrayLength, extendedAsciiStrings);
                case 2: return RandomInt(schema, refs, minArrayLength, maxArrayLength, extendedAsciiStrings);
                case 3: return RandomNull(schema, refs, minArrayLength, maxArrayLength, extendedAsciiStrings);
                case 4: return RandomString(schema, refs, minArrayLength, maxArrayLength, extendedAsciiStrings);
                case 5: return RandomArray(schema, refs, minArrayLength, maxArrayLength, extendedAsciiStrings);
                case 6: return RandomObject(schema, refs, minArrayLength, maxArrayLength, extendedAsciiStrings);
                default: throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// choose a random schema from an anyOf list of schemas, and make random JSON based on that schema
        /// </summary>
        /// <returns></returns>
        private static JNode RandomAnyOf(JNode anyOf, JObject refs, int minArrayLength, int maxArrayLength, bool extendedAsciiStrings)
        {
            JArray anyOfArr = (JArray)anyOf;
            JNode schema = anyOfArr.children[random.Next(anyOfArr.Length)];
            return RandomJsonHelper(schema, refs, minArrayLength, maxArrayLength, extendedAsciiStrings);
        }

        public static JNode RandomJsonHelper(JNode schema, JObject refs, int minArrayLength, int maxArrayLength, bool extendedAsciiStrings)
        {
            if (!(schema is JObject obj))
            {
                if (schema is null)
                    throw new SchemaValidationException("Schema was null (in the programmatic sense, not a JSON element representing null)");
                if (schema.type != Dtype.BOOL)
                {
                    throw new SchemaValidationException("A JSON schema must be an object or a boolean");
                }
                if ((bool)schema.value)
                    return RandomAnything(new JObject(), new JObject(), minArrayLength, maxArrayLength, extendedAsciiStrings);
                    // the true schema validates everything, so we'll make a
                    // random instance of a random type
                return new JNode();
                // the false schema validates nothing,
                // and the null JNode is the closest we can get to nothing
            }
            if (obj.Length == 0) // the empty schema validates everything
                return RandomAnything(new JObject(), new JObject(), minArrayLength, maxArrayLength, extendedAsciiStrings);
            if (obj.children.TryGetValue("enum", out JNode enum_))
            {
                // any type can be an enum; it's just a list of possible values
                List<JNode> enumMembers = ((JArray)enum_).children;
                return enumMembers[random.Next(enumMembers.Count)].Copy();
            }
            if (!obj.children.TryGetValue("type", out JNode typeNode))
            {
                if (!obj.children.TryGetValue("anyOf", out JNode anyOf))
                {
                    if (!obj.children.TryGetValue("$ref", out JNode refnode))
                        throw new SchemaValidationException("A schema must have at least one of the \"type\", \"enum\", \"anyOf\" keywords.");
                    var refname = ((string)refnode.value).Split('/').Last();
                    if (!refs.children.TryGetValue(refname, out JNode reference))
                        throw new SchemaValidationException($"Reference {refname} to an undefined schema");
                    return RandomJsonHelper(reference, refs, minArrayLength, maxArrayLength, extendedAsciiStrings);
                }
                return RandomAnyOf(anyOf, refs, minArrayLength, maxArrayLength, extendedAsciiStrings);
            }
            if (typeNode is JArray typeArr)
            {
                // multiple scalar types possible
                JNode typeChoice = typeArr.children[random.Next(typeArr.Length)];
                return GENERATORS[(string)typeChoice.value](schema, refs, minArrayLength, maxArrayLength, extendedAsciiStrings);
            }
            string type = (string)typeNode.value;
            bool typeInt = type[0] == 'i';  // "integer" is the only type that starts with 'i'
            if (typeInt || type == "number")
            {
                var min = obj.children.TryGetValue("minimum", out JNode minNode)
                    ? Convert.ToDouble(minNode.value)
                    : NanInf.neginf;
                var max = obj.children.TryGetValue("maximum", out JNode maxNode)
                    ? Convert.ToDouble(maxNode.value)
                    : NanInf.inf;
                if (!double.IsInfinity(min) || !double.IsInfinity(max))
                {
                    if (typeInt) return RandomIntegerBetweenMinAndMax(min, max);
                    return RandomNumberBetweenMinAndMax(min, max);
                }
            }
            var typeGenerator = GENERATORS[type];
            return typeGenerator(schema, refs, minArrayLength, maxArrayLength, extendedAsciiStrings);
        }

        public static JNode RandomJson(JNode schema, int minArrayLength, int maxArrayLength, bool extendedAsciiStrings)
        {
            var schemobj = (JObject)schema;
            JNode refs;
            if (!(schemobj.children.TryGetValue("$defs", out refs)
                || schemobj.children.TryGetValue("definitions", out refs)))
                refs = new JObject();
            return RandomJsonHelper(schema, (JObject)refs, minArrayLength, maxArrayLength, extendedAsciiStrings);
        }
    }
}
