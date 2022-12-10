using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Runtime;

namespace JSON_Tools.JSON_Tools
{
    public class RandomJsonFromSchema
    {
        private static Random random = new Random();

        public static void Reseed()
        {
            random = new Random();
        }

        #region RANDOM_SCALARS
        /// <summary>
        /// 50% chance for true or false
        /// </summary>
        private static JNode RandomBoolean(JNode schema, int minArrayLength, int maxArrayLength)
        {
            if (random.NextDouble() < 0.5)
                return new JNode(true, Dtype.BOOL, 0);
            return new JNode(false, Dtype.BOOL, 0);
        }

        /// <summary>
        /// random double from -5 to 5
        /// </summary>
        private static JNode RandomFloat(JNode schema, int minArrayLength, int maxArrayLength)
        {
            return new JNode(random.NextDouble() * 10 - 5, Dtype.FLOAT, 0);
        }

        /// <summary>
        /// random int from -1 million to 1 million 
        /// </summary>
        private static JNode RandomInt(JNode schema, int minArrayLength, int maxArrayLength)
        {
            return new JNode(random.Next(-1_000_000, 1_000_001), Dtype.INT, 0);
        }

        /// <summary>
        /// 50% chance of random int, 50% chance of random float
        /// </summary>
        private static JNode RandomNumber(JNode schema, int minArrayLength, int maxArrayLength)
        {
            if (random.NextDouble() < 0.5)
                return RandomInt(schema, minArrayLength, maxArrayLength);
            return RandomFloat(schema, minArrayLength, maxArrayLength);
        }

        /// <summary>
        /// always returns the null JNode
        /// </summary>
        private static JNode RandomNull(JNode schema, int minArrayLength, int maxArrayLength)
        {
            return new JNode();
        }

        private static readonly string PRINTABLE = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~ \t\r\n";

        /// <summary>
        /// string of random length between 0 and 10<br></br>
        /// Can contain any printable ASCII character, possibly multiple times
        /// </summary>
        private static JNode RandomString(JNode schema, int minArrayLength, int maxArrayLength)
        {
            int length = random.Next(11);
            StringBuilder sb = new StringBuilder(length);
            for (int ii = 0; ii < length; ii++)
            {
                char randChar = PRINTABLE[random.Next(PRINTABLE.Length)];
                sb.Append(randChar);
            }
            return new JNode(sb.ToString(), Dtype.STR, 0);
        }
        #endregion

        #region RANDOM_ITERABLES
        private static JNode RandomArray(JNode schema, int minArrayLength, int maxArrayLength)
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
                    outItems.Add(RandomJson(contains, minArrayLength, maxArrayLength));
            }
            for (int ii = 0; ii < length; ii++)
                outItems.Add(RandomJson(items, minArrayLength, maxArrayLength));
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

        private static JNode RandomObject(JNode schema, int minArrayLength, int maxArrayLength)
        {
            var children = ((JObject)schema).children;
            if (!children.TryGetValue("properties", out JNode properties))
            {
                return new JObject();
                // as with an array with no "items" keyword,
                // an object with no "properties" keyword could contain anything
            }
            var propertiesObj = (JObject)properties;
            HashSet<string> optionalKeys = new HashSet<string>();
            HashSet<string> requiredKeys = new HashSet<string>();
            foreach (string key in propertiesObj.children.Keys)
                optionalKeys.Add(key);
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
                int numOptionalIncluded = random.Next(optionalKeys.Count);
                var allOptional = optionalKeys.ToArray();
                HashSet<int> indicesChosen = new HashSet<int>();
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
            foreach (string k in requiredKeys.Concat(optionalKeys))
            {
                JNode subschema = propertiesObj[k];
                result[k] = RandomJson(subschema, minArrayLength, maxArrayLength);
            }
            return new JObject(0, result);
        }
        #endregion

        private static readonly Dictionary<string, Func<JNode, int, int, JNode>> GENERATORS = new Dictionary<string, Func<JNode, int, int, JNode>>
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
        private static JNode RandomAnything(JNode schema, int minArrayLength, int maxArrayLength)
        {
            int choice = random.Next(7);
            switch (choice)
            {
                case 0: return RandomBoolean(schema, minArrayLength, maxArrayLength);
                case 1: return RandomFloat(schema, minArrayLength, maxArrayLength);
                case 2: return RandomInt(schema, minArrayLength, maxArrayLength);
                case 3: return RandomNull(schema, minArrayLength, maxArrayLength);
                case 4: return RandomString(schema, minArrayLength, maxArrayLength);
                case 5: return RandomArray(schema, minArrayLength, maxArrayLength);
                case 6: return RandomObject(schema, minArrayLength, maxArrayLength);
                default: throw new ArgumentOutOfRangeException();
            }
        }

        private static JNode RandomAnyOf(JNode anyOf, int minArrayLength, int maxArrayLength)
        {
            JArray anyOfArr = (JArray)anyOf;
            JNode schema = anyOfArr.children[random.Next(anyOfArr.Length)];
            return RandomJson(schema, minArrayLength, maxArrayLength);
        }

        public static JNode RandomJson(JNode schema, int minArrayLength, int maxArrayLength)
        {
            if (!(schema is JObject obj))
            {
                if (schema.type != Dtype.BOOL)
                {
                    throw new SchemaValidationException("A JSON schema must be an object or a boolean");
                }
                if ((bool)schema.value)
                    return RandomAnything(new JObject(), minArrayLength, maxArrayLength);
                    // the true schema validates everything, so we'll make a
                    // random instance of a random type
                return new JNode();
                // the false schema validates nothing,
                // and the null JNode is the closest we can get to nothing
            }
            if (obj.Length == 0) // the empty schema validates everything
                return RandomAnything(new JObject(), minArrayLength, maxArrayLength);
            if (!obj.children.TryGetValue("type", out JNode typeNode))
            {
                if (!obj.children.TryGetValue("anyOf", out JNode anyOf))
                    throw new SchemaValidationException("A schema must have an \"anyOf\" keyword or a \"type\" keyword");
                return RandomAnyOf(anyOf, minArrayLength, maxArrayLength);
            }
            if (obj.children.TryGetValue("enum", out JNode enum_))
            {
                // any type can be an enum; it's just a list of possible values
                List<JNode> enumMembers = ((JArray)enum_).children;
                return enumMembers[random.Next(enumMembers.Count)].Copy();
            }
            if (typeNode is JArray typeArr)
            {
                // multiple scalar types possible
                JNode typeChoice = typeArr.children[random.Next(typeArr.Length)];
                return GENERATORS[(string)typeChoice.value](schema, minArrayLength, maxArrayLength);
            }
            var typeGenerator = GENERATORS[(string)typeNode.value];
            return typeGenerator(schema, minArrayLength, maxArrayLength);
        }
    }
}
