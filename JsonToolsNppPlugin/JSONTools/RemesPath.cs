/*
A query language for JSON. 
*/
using System;
using System.Collections.Generic;
using System.Diagnostics; // for Stopwatch class
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JSON_Viewer.JSONViewer
{
    /// <summary>
    /// an exception thrown when trying to use a boolean index of unequal length<br></br>
    /// or when trying to apply a binary operator to two objects with different sets of keys<br></br>
    /// or arrays with different lengths.
    /// </summary>
    public class VectorizedArithmeticException : RemesPathException
    {
        public VectorizedArithmeticException(string description) : base(description) { }

        public override string ToString() { return description; }
    }

    public class QueryCache
    {
        public int capacity;
        public Dictionary<string, JNode> cache;
        public LinkedList<string> use_order;

        public QueryCache(int capacity = 64)
        {
            cache = new Dictionary<string, JNode>();
            this.capacity = capacity;
            this.use_order = new LinkedList<string>();
        }

        /// <summary>
        /// Checks the cache to see if the string query has already been stored.
        /// If so, return the value associated with it.
        /// If not, return null.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public JNode? Check(string query)
        {
            if (cache.TryGetValue(query, out JNode existing_result))
            {
                return existing_result;
            }
            return null;
        }

        /// <summary>
        /// Check if the query is already in the cache. If it is, and capacity is full, purge the oldest query.<br></br>
        /// Then add the query-result pair.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="result"></param>
        public void Add(string query, JNode result)
        {
            if (cache.ContainsKey(query)) { return; }
            if (use_order.Count == capacity)
            {
                string oldest_query = use_order.First();
                use_order.RemoveFirst();
                cache.Remove(oldest_query);
            }
            use_order.AddLast(query);
            cache[query] = result;
        }
    }

    /// <summary>
    /// anything that filters the keys of an object or the indices of an array
    /// </summary>
    public class Indexer { }

    /// <summary>
    /// a list of strings or regexes, for selecting keys from objects
    /// </summary>
    public class VarnameList : Indexer
    {
        public List<object> children;

        public VarnameList(List<object> children)
        {
            this.children = children;
        }
    }

    /// <summary>
    /// A list of ints or slicers, for selecting indices from arrays
    /// </summary>
    public class SlicerList : Indexer
    {
        public List<object> children;

        public SlicerList(List<object> children)
        {
            this.children = children;
        }
    }

    /// <summary>
    /// An indexer that always selects all the keys of an object or all the indices of an array
    /// </summary>
    public class StarIndexer : Indexer { }

    /// <summary>
    /// An array or object with values that are usually functions of some parent JSON.<br></br>
    /// For example, @{@.foo, @.bar} returns an array projection
    /// where the first element is the value associated with the foo key of current JSON<br></br>
    /// and the second element is the value associated with the bar key of current JSON.
    /// </summary>
    public class Projection : Indexer
    {
        public Func<JNode, IEnumerable<(object, JNode)>> proj_func;

        public Projection(Func<JNode, IEnumerable<(object, JNode)>> proj_func)
        {
            this.proj_func = proj_func;
        }
    }

    /// <summary>
    /// An array or object or bool (or more commonly a function of the current JSON that returns an array/object/bool)<br></br>
    /// that is used to determine whether to select one or more indices/keys from an array/object.
    /// </summary>
    public class BooleanIndex : Indexer
    {
        public object value;

        public BooleanIndex(object value)
        {
            this.value = value;
        }
    }

    public struct IndexerFunc
    {
        /// <summary>
        /// An enumerator that yields JNodes from a JArray or JObject
        /// </summary>
        public Func<JNode, IEnumerable<(object, JNode)>> idxr;
        /// <summary>
        /// rather than making a JObject or JArray to contain a single selection from a parent<br></br>
        /// (e.g., when selecting a single key or a single index), we will just return that one element as a scalar.<br></br>
        /// As a result, the query @.foo[0] on {"foo": [1,2]} returns 1 rather than {"foo": [1]}
        /// </summary>
        public bool has_one_option;
        /// <summary>
        /// is an array or object projection made by the {foo: @[0], bar: @[1]} type syntax.
        /// </summary>
        public bool is_projection;
        public bool is_dict;
        /// <summary>
        /// involves recursive search
        /// </summary>
        public bool is_recursive;

        public IndexerFunc(Func<JNode, IEnumerable<(object, JNode)>> idxr, bool has_one_option, bool is_projection, bool is_dict, bool is_recursive)
        {
            this.idxr = idxr;
            this.has_one_option = has_one_option;
            this.is_projection = is_projection;
            this.is_dict = is_dict;
            this.is_recursive = is_recursive;
        }
    }

    /// <summary>
    /// Exception thrown while parsing or executing RemesPath queries.
    /// </summary>
    public class RemesPathException : Exception
    {
        public string description;

        public RemesPathException(string description) { this.description = description; }

        public override string ToString() { return description + "\nDetails:\n" + Message; }
    }

    /// <summary>
    /// RemesPath is similar to JMESPath, but more fully featured.<br></br>
    /// The RemesParser parses queries.
    /// </summary>
    public class RemesParser
    {
        public QueryCache cache;

        /// <summary>
        /// The cache_capacity indicates how many queries to store in the old query cache.
        /// </summary>
        /// <param name="cache_capacity"></param>
        public RemesParser(int cache_capacity = 64)
        {
            cache = new QueryCache();
        }

        /// <summary>
        /// Parse a query and compile it into a RemesPath function that operates on JSON.
        /// If the query is not a function of input, it will instead just output fixed JSON.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public JNode Compile(string query)
        {
            //// turns out compiling queries is very fast (tens of microseconds for a simple query),
            //// so caching old queries doesn't save much time
            // JNode? old_result = cache.Check(query);
            // if (old_result != null) { return old_result; }
            List<object> toks = RemesPathLexer.Tokenize(query);
            (JNode result, _) = ParseExprOrScalarFunc(toks, 0);
            //cache.Add(query, result);
            return result;
        }

        /// <summary>
        /// Perform a RemesPath query on JSON and return the result.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public JNode Search(string query, JNode obj)
        {
            JNode result = Compile(query);
            if (result is CurJson)
            {
                return ((CurJson)result).function(obj);
            }
            return result;
        }

        public static string EXPR_FUNC_ENDERS = "]:},)";
        // these tokens have high enough precedence to stop an expr_function or scalar_function
        public static string INDEXER_STARTERS = ".[{";

        #region INDEXER_FUNCTIONS
        private Func<JNode, IEnumerable<(object key, JNode node)>> ApplyMultiIndex(object inds, bool is_varname_list, bool is_recursive = false)
        {
            if (inds is CurJson)
            {
                IEnumerable<(object, JNode)> multi_idx_func(JNode x)
                {
                    return ApplyMultiIndex(((CurJson)inds).function(x), is_varname_list, is_recursive)(x);
                }
            }
            var children = (List<object>)inds;
            if (is_varname_list)
            {
                if (is_recursive)
                {
                    IEnumerable<(object, JNode)> multi_idx_func(JNode x, string path, HashSet<string> paths_visited)
                    {
                        if (x is JArray)
                        {
                            // a varname list can only match dict keys, not array indices
                            // we'll just recursively search from each child of this array
                            JArray xarr = (JArray)x;
                            for (int ii = 0; ii < xarr.Length; ii++)
                            {
                                foreach ((object k, JNode v) in multi_idx_func(xarr.children[ii], path + ',' + ii.ToString(), paths_visited))
                                {
                                    yield return (k, v);
                                }
                            }
                        }
                        else if (x is JObject)
                        {
                            JObject xobj = (JObject)x;
                            // yield each key or regex match in this dict
                            // recursively descend from each key that doesn't match
                            foreach (object v in children)
                            {
                                if (v is string)
                                {
                                    string strv = (string)v;
                                    if (path == "") path = strv;
                                    foreach ((string k, JNode val) in xobj.children)
                                    {
                                        string newpath = path + ',' + new JNode(k, Dtype.STR, 0).ToString();
                                        if (k == strv)
                                        {
                                            if (!paths_visited.Contains(newpath)) yield return (0, val);
                                            paths_visited.Add(newpath);
                                        }
                                        else
                                        {
                                            foreach ((_, JNode subval) in multi_idx_func(val, newpath, paths_visited))
                                            {
                                                yield return (0, subval);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Regex regv = (Regex)v;
                                    if (path == "") path = regv.ToString();
                                    foreach ((string k, JNode val) in xobj.children)
                                    {
                                        string newpath = path + ',' + new JNode(k, Dtype.STR, 0).ToString();
                                        if (regv.IsMatch(k))
                                        {
                                            if (!paths_visited.Contains(newpath)) yield return (0, val);
                                            paths_visited.Add(newpath);
                                        }
                                        else
                                        {
                                            foreach ((_, JNode subval) in multi_idx_func(val, newpath, paths_visited))
                                            {
                                                yield return (0, subval);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    return x => multi_idx_func(x, "", new HashSet<string>());
                }
                else
                {
                    IEnumerable<(object, JNode)> multi_idx_func(JNode x)
                    {
                        var xobj = (JObject)x;
                        foreach (object v in children)
                        {
                            if (v is string)
                            {
                                string vstr = (string)v;
                                if (xobj.children.TryGetValue(vstr, out JNode val))
                                {
                                    yield return (vstr, val);
                                }
                            }
                            else
                            {
                                foreach ((string vstr, JNode val) in ApplyRegexIndex(xobj, (Regex)v))
                                {
                                    yield return (vstr, val);
                                }
                            }
                        }
                    }
                    return multi_idx_func;
                }
            }
            else
            {
                // it's a list of ints or slices
                if (is_recursive)
                {
                    // decide whether to implement recursive search for slices and indices
                    throw new NotImplementedException("Recursive search for array indices and slices is not implemented");
                }
                IEnumerable<(object, JNode)> multi_idx_func(JNode x)
                {
                    JArray xarr = (JArray)x;
                    foreach (object ind in children)
                    {
                        if (ind is int?[])
                        {
                            // it's a slice, so yield all the JNodes in that slice
                            foreach (JNode subind in xarr.children.LazySlice((int?[])ind))
                            {
                                yield return (0, subind);
                            }
                        }
                        else
                        {
                            int ii = Convert.ToInt32(ind);
                            if (ii >= xarr.Length) { continue; }
                            // allow negative indices for consistency with how slicers work
                            yield return (0, xarr.children[ii >= 0 ? ii : ii + xarr.Length]);
                        }
                    }
                }
                return multi_idx_func;
            }
        }

        private IEnumerable<(string key, JNode val)> ApplyRegexIndex(JObject obj, Regex regex)
        {
            foreach ((string ok, JNode val) in obj.children)
            {
                if (regex.IsMatch(ok))
                {
                    yield return (ok, val);
                }
            }
        }

        private Func<JNode, IEnumerable<(object key, JNode node)>> ApplyBooleanIndex(JNode inds)
        {
            IEnumerable<(object key, JNode node)> bool_idxr_func(JNode x)
            {
                JNode newinds = inds;
                if (inds is CurJson)
                {
                    CurJson jinds = (CurJson)inds;
                    Func<JNode, JNode> indfunc = jinds.function;
                    newinds = indfunc(x);
                }
                if (newinds.type == Dtype.BOOL)
                {
                    // to allow for boolean indices that filter on the entire object/array, like @.bar == @.foo or sum(@) == 0
                    if ((bool)newinds.value)
                    {
                        if (x.type == Dtype.OBJ)
                        {
                            foreach ((string key, JNode val) in ((JObject)x).children)
                            {
                                yield return (key, val);
                            }
                        }
                        else if (x.type == Dtype.ARR)
                        {
                            JArray xarr = (JArray)x;
                            for (int ii = 0; ii < xarr.Length; ii++)
                            {
                                yield return (ii, xarr.children[ii]);
                            }
                        }
                    }
                    // if the condition is false, yield nothing
                    yield break;
                }
                else if (newinds.type == Dtype.OBJ)
                {
                    JObject iobj = (JObject)newinds;
                    JObject xobj= (JObject)x;
                    if (iobj.Length != xobj.Length)
                    {
                        throw new VectorizedArithmeticException($"bool index length {iobj.Length} does not match object/array length {xobj.Length}.");
                    }
                    foreach ((string key, JNode xval) in xobj.children)
                    {
                        bool i_has_key = iobj.children.TryGetValue(key, out JNode ival);
                        if (i_has_key)
                        {
                            if (ival.type != Dtype.BOOL)
                            {
                                throw new VectorizedArithmeticException("bool index contains non-booleans");
                            }
                            if ((bool)ival.value)
                            {
                                yield return (key, xval);
                            }
                        }
                    }
                    yield break;
                }
                else if (newinds.type == Dtype.ARR)
                {
                    JArray iarr = (JArray)newinds;
                    JArray xarr = (JArray)x;
                    if (iarr.Length != xarr.Length)
                    {
                        throw new VectorizedArithmeticException($"bool index length {iarr.Length} does not match object/array length {xarr.Length}.");
                    }
                    for (int ii = 0; ii < xarr.Length; ii++)
                    {
                        JNode ival = iarr.children[ii];
                        JNode xval = xarr.children[ii];
                        if (ival.type != Dtype.BOOL)
                        {
                            throw new VectorizedArithmeticException("bool index contains non-booleans");
                        }
                        if ((bool)ival.value)
                        {
                            yield return (ii, xval);
                        }
                    }
                    yield break;
                }
            }
            return bool_idxr_func;
        }

        private IEnumerable<(object key, JNode val)> ApplyStarIndexer(JNode x)
        {
            if (x.type == Dtype.OBJ)
            {
                var xobj = (JObject)x;
                foreach ((string key, JNode val) in xobj.children)
                {
                    yield return (key, val);
                }
                yield break;
            }
            var xarr = (JArray)x;
            for (int ii = 0; ii < xarr.Length; ii++)
            {
                yield return (ii, xarr.children[ii]);
            }
        }

        /// <summary>
        /// return null if x is not an object or array<br></br>
        /// If it is an object or array, return true if its length is 0. 
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private static bool? ObjectOrArrayEmpty(JNode x)
        {
            if (x.type == Dtype.OBJ) { return ((JObject)x).Length == 0; }
            if (x.type == Dtype.ARR) { return ((JArray)x).Length == 0; }
            return null;
        }

        private Func<JNode, JNode> ApplyIndexerList(List<IndexerFunc> indexers)
        {
            JNode idxr_list_func(JNode obj, List<IndexerFunc> indexers)
            {
                IndexerFunc ix = indexers[0];
                var inds = ix.idxr(obj).GetEnumerator();
                // IEnumerator<T>.MoveNext returns a bool indicating if the enumerator has passed the end of the collection
                if (!inds.MoveNext())
                {
                    // the IndexerFunc couldn't find
                    if (ix.is_dict)
                    {
                        return new JObject(0, new Dictionary<string, JNode>());
                    }
                    return new JArray(0, new List<JNode>());
                }
                (object k1, JNode v1) = inds.Current;
                bool is_dict = (ix.is_dict || k1 is string) && !ix.is_recursive;
                var arr = new List<JNode>();
                var dic = new Dictionary<string, JNode>();
                if (indexers.Count == 1)
                {
                    if (ix.has_one_option)
                    {
                        // return a scalar rather than an iterable with one element
                        return v1;
                    }
                    if (is_dict)
                    {
                        dic = new Dictionary<string, JNode>();
                        dic[(string)k1] = v1;
                        while (inds.MoveNext())
                        {
                            (object k, JNode v) = inds.Current;
                            dic[(string)k] = v;
                        }
                        return new JObject(0, dic);
                    }
                    arr = new List<JNode>();
                    arr.Add(v1);
                    while (inds.MoveNext())
                    {
                        (_, JNode v) = inds.Current;
                        arr.Add(v);
                    }
                    return new JArray(0, arr);
                }
                var remaining_idxrs = indexers.TakeLast(indexers.Count - 1).ToList();
                if (ix.is_projection)
                {
                    if (is_dict)
                    {
                        dic = new Dictionary<string, JNode>();
                        dic[(string)k1] = v1;
                        while (inds.MoveNext())
                        {
                            (object k, JNode v) = inds.Current;
                            dic[(string)k] = v;
                        }
                        // recursively search this projection using the remaining indexers
                        return idxr_list_func(new JObject(0, dic), remaining_idxrs);
                    }
                    arr = new List<JNode>();
                    arr.Add(v1);
                    while (inds.MoveNext())
                    {
                        (_, JNode v) = inds.Current;
                        arr.Add(v);
                    }
                    return idxr_list_func(new JArray(0, arr), remaining_idxrs);
                }
                JNode v1_subdex = idxr_list_func(v1, remaining_idxrs);
                if (ix.has_one_option)
                {
                    return v1_subdex;
                }
                bool? is_empty = ObjectOrArrayEmpty(v1_subdex);
                if (is_dict)
                {
                    dic = new Dictionary<string, JNode>();
                    if (!is_empty.HasValue || !is_empty.Value)
                    {
                        dic[(string)k1] = v1_subdex;
                    }
                    while (inds.MoveNext())
                    {
                        (object k, JNode v) = inds.Current;
                        JNode subdex = idxr_list_func(v, remaining_idxrs);
                        is_empty = ObjectOrArrayEmpty(subdex);
                        if (!is_empty.HasValue || !is_empty.Value)
                        {
                            dic[(string)k] = subdex;
                        }
                    }
                    return new JObject(0, dic);
                }
                // obj is a list iterator
                arr = new List<JNode>();
                if (!is_empty.HasValue || !is_empty.Value)
                {
                    arr.Add(v1_subdex);
                }
                while (inds.MoveNext())
                {
                    (_, JNode v) = inds.Current;
                    JNode subdex = idxr_list_func(v, remaining_idxrs);
                    is_empty = ObjectOrArrayEmpty(subdex);
                    if (!is_empty.HasValue || !is_empty.Value)
                    {
                        arr.Add(subdex);
                    }
                }
                return new JArray(0, arr);
            }
            return (JNode obj) => idxr_list_func(obj, indexers);
        }

        #endregion
        #region BINOP_FUNCTIONS
        private JNode BinopTwoJsons(Binop b, JNode left, JNode right)
        {
            if (ObjectOrArrayEmpty(right) == null)
            {
                if (ObjectOrArrayEmpty(left) == null)
                {
                    return b.Call(left, right);
                }
                return BinopJsonScalar(b, left, right);
            }
            if (ObjectOrArrayEmpty(left) == null)
            {
                return BinopScalarJson(b, left, right);
            }
            if (right.type == Dtype.OBJ)
            {
                var dic = new Dictionary<string, JNode>();
                var robj = (JObject)right;
                var lobj = (JObject)left;
                if (robj.Length != lobj.Length)
                {
                    throw new VectorizedArithmeticException("Tried to apply a binop to two dicts with different sets of keys");
                }
                foreach ((string key, JNode right_val) in robj.children)
                {
                    bool left_has_key = lobj.children.TryGetValue(key, out JNode left_val);
                    if (!left_has_key)
                    {
                        throw new VectorizedArithmeticException("Tried to apply a binop to two dicts with different sets of keys");
                    }
                    dic[key] = b.Call(left_val, right_val);
                }
                return new JObject(0, dic);
            }
            var arr = new List<JNode>();
            var rarr = (JArray)right;
            var larr = (JArray)left;
            if (larr.Length != rarr.Length)
            {
                throw new VectorizedArithmeticException("Tried to perform vectorized arithmetic on two arrays of unequal length");
            }
            for (int ii = 0; ii < rarr.Length; ii++)
            {
                arr.Add(b.Call(larr.children[ii], rarr.children[ii]));
            }
            return new JArray(0, arr);
        }

        private JNode BinopJsonScalar(Binop b, JNode left, JNode right)
        {
            if (left.type == Dtype.OBJ)
            {
                var dic = new Dictionary<string, JNode>();
                var lobj = (JObject)left;
                foreach ((string key, JNode left_val) in lobj.children)
                {
                    dic[key] = b.Call(left_val, right);
                }
                return new JObject(0, dic);
            }
            var arr = new List<JNode>();
            var larr = (JArray)left;
            for (int ii = 0; ii < larr.Length; ii++)
            {
                arr.Add(b.Call(larr.children[ii], right));
            }
            return new JArray(0, arr);
        }

        private JNode BinopScalarJson(Binop b, JNode left, JNode right)
        {
            if (right.type == Dtype.OBJ)
            {
                var dic = new Dictionary<string, JNode>();
                var robj = (JObject)right;
                foreach ((string key, JNode right_val) in robj.children)
                {
                    dic[key] = b.Call(left, right_val);
                }
                return new JObject(0, dic);
            }
            var arr = new List<JNode>();
            var rarr = (JArray)right;
            for (int ii = 0; ii < rarr.Length; ii++)
            {
                arr.Add(b.Call(left, rarr.children[ii]));
            }
            return new JArray(0, arr);
        }

        /// <summary>
        /// For a given binop and the types of two JNodes, determines the output's type.<br></br>
        /// Raises a RemesPathException if the types are inappropriate for that Binop.<br></br>
        /// EXAMPLES<br></br>
        /// BinopOutType(Binop.BINOPS["+"], Dtype.STR, Dtype.STR) -> Dtype.STR<br></br>
        /// BinopOutType(Binop.BINOPS["**"], Dtype.STR, Dtype.INT) -> throws RemesPathException<br></br>
        /// BinopOutType(Binop.BINOPS["*"], Dtype.INT, Dtype.FLOAT) -> Dtype.FLOAT
        /// </summary>
        /// <param name="b"></param>
        /// <param name="ltype"></param>
        /// <param name="rtype"></param>
        /// <returns></returns>
        /// <exception cref="RemesPathException"></exception>
        private Dtype BinopOutType(Binop b, Dtype ltype, Dtype rtype)
        {
            if (ltype == Dtype.UNKNOWN || rtype == Dtype.UNKNOWN) { return Dtype.UNKNOWN; }
            if (ltype == Dtype.OBJ)
            {
                if (rtype == Dtype.ARR)
                {
                    throw new RemesPathException("Cannot have a function of an array and an object");
                }
                return Dtype.OBJ;
            }
            if (ltype == Dtype.ARR)
            {
                if (rtype == Dtype.OBJ)
                {
                    throw new RemesPathException("Cannot have a function of an array and an object");
                }
                return Dtype.ARR;
            }
            string name = b.name;
            if (Binop.BOOLEAN_BINOPS.Contains(name)) { return Dtype.BOOL; }
            if ((ltype & Dtype.NUM) == 0 && (rtype & Dtype.NUM) == 0)
            {
                if (name == "+")
                {
                    if (ltype == Dtype.STR)
                    {
                        if (rtype != Dtype.STR && (rtype & Dtype.ITERABLE) == 0)
                        {
                            throw new RemesPathException("Cannot add non-string to string");
                        }
                        return Dtype.STR;
                    }
                }
                throw new RemesPathException($"Invalid argument types {ltype} and {rtype} for binop {name}");
            }
            if (Binop.BITWISE_BINOPS.Contains(name)) // ^, & , |
            {
                if (ltype == Dtype.INT)
                {
                    if (rtype == Dtype.FLOAT)
                    {
                        throw new RemesPathException($"Incompatible types {ltype} and {rtype} for bitwise binop {name}");
                    }
                    return Dtype.INT;
                }
                if (rtype == Dtype.INT)
                {
                    if (ltype == Dtype.FLOAT)
                    {
                        throw new RemesPathException($"Incompatible types {ltype} and {rtype} for bitwise binop {name}");
                    }
                    return Dtype.INT;
                }
                return Dtype.BOOL;
            }
            // it's a polymorphic binop - one of -, +, *, %
            if (rtype == Dtype.BOOL && ltype == Dtype.BOOL)
            {
                throw new RemesPathException($"Can't do arithmetic operation {name} on two bools");
            }
            if (name == "//") { return Dtype.INT; }
            if (Binop.FLOAT_RETURNING_BINOPS.Contains(name)) { return Dtype.FLOAT; } 
            // division and exponentiation always give doubles
            if (rtype == Dtype.INT && ltype == Dtype.INT)
            {
                return Dtype.INT;
            }
            return Dtype.FLOAT;
        }

        /// <summary>
        /// Handles all possible argument combinations for a Binop being called on two JNodes:<br></br>
        /// iterable and iterable, iterable and scalar, iterable that's a function of the current JSON and scalar 
        /// that's not, etc.<br></br>
        /// Throws a RemesPathException if an invalid combination of types is chosen.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        private JNode ResolveBinop(Binop b, JNode left, JNode right)
        {
            bool left_itbl = (left.type & Dtype.ITERABLE) != 0;
            bool right_itbl = (right.type & Dtype.ITERABLE) != 0;
            Dtype out_type = BinopOutType(b, left.type, right.type);
            if (left is CurJson)
            {
                CurJson lcur = (CurJson)left;
                if (right is CurJson)
                {
                    // both are functions of the current JSON
                    CurJson rcur = (CurJson)right;
                    if (left_itbl)
                    {
                        if (right_itbl)
                        {
                            // they're both iterables or unknown type
                            return new CurJson(out_type, (JNode x) => BinopTwoJsons(b, lcur.function(x), rcur.function(x)));
                        }
                        // only left is an iterable and unknown type
                        return new CurJson(out_type, (JNode x) => BinopTwoJsons(b, lcur.function(x), rcur.function(x)));
                    }
                    if (right_itbl)
                    {
                        // right is iterable or unknown, but left is not iterable
                        return new CurJson(out_type, (JNode x) => BinopTwoJsons(b, lcur.function(x), rcur.function(x)));
                    }
                    // they're both scalars
                    return new CurJson(out_type, (JNode x) => b.Call(lcur.function(x), rcur.function(x)));
                }
                // right is not a function of the current JSON, but left is
                if (left_itbl)
                {
                    if (right_itbl)
                    {
                        return new CurJson(out_type, (JNode x) => BinopTwoJsons(b, lcur.function(x), right));
                    }
                    return new CurJson(out_type, (JNode x) => BinopTwoJsons(b, lcur.function(x), right));
                }
                if (right_itbl)
                {
                    return new CurJson(out_type, (JNode x) => BinopTwoJsons(b, lcur.function(x), right));
                }
                return new CurJson(out_type, (JNode x) => b.Call(lcur.function(x), right));
            }
            if (right is CurJson)
            {
                // left is not a function of the current JSON, but right is
                CurJson rcur = (CurJson)right;
                if (left_itbl)
                {
                    if (right_itbl)
                    {
                        return new CurJson(out_type, (JNode x) => BinopTwoJsons(b, left, rcur.function(x)));
                    }
                    return new CurJson(out_type, (JNode x) => BinopTwoJsons(b, left, rcur.function(x)));
                }
                if (right_itbl)
                {
                    return new CurJson(out_type, (JNode x) => BinopTwoJsons(b, left, rcur.function(x)));
                }
                return new CurJson(out_type, (JNode x) => b.Call(left, rcur.function(x)));
            }
            // neither is a function of the current JSON
            if (left_itbl)
            {
                if (right_itbl)
                {
                    return BinopTwoJsons(b, left, right);
                }
                return BinopJsonScalar(b, left, right);
            }
            if (right_itbl)
            {
                return BinopScalarJson(b, left, right);
            }
            return b.Call(left, right);
        }

        /// <summary>
        /// Resolves a binop where left and right may also be binops, by recursively descending to left and right<br></br>
        /// and resolving the leaf binops.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        private JNode ResolveBinopTree(BinopWithArgs b)
        {
            object left = b.left;
            object right = b.right;
            if (left is BinopWithArgs)
            {
                left = ResolveBinopTree((BinopWithArgs)left);
            }
            if (right is BinopWithArgs)
            {
                right = ResolveBinopTree((BinopWithArgs)right);
            }
            return ResolveBinop(b.binop, (JNode)left, (JNode)right);
        }

        #endregion
        #region APPLY_ARG_FUNCTION
        private JNode ApplyArgFunction(ArgFunctionWithArgs func)
        {
            JNode x = func.args[0];
            bool other_callables = false;
            JNode[] other_args = new JNode[func.args.Length - 1];
            for (int ii = 0; ii < func.args.Length - 1; ii++)
            {
                JNode arg = func.args[ii + 1];
                if (arg is CurJson) { other_callables = true; }
                other_args[ii] = arg;
            }
            // vectorized functions take on the type of the iterable they're vectorized across, but they have a set type
            // when operating on scalars (e.g. s_len returns an array when acting on an array and a dict
            // when operating on a dict, but s_len always returns an int when acting on a single string)
            // non-vectorized functions always return the same type
            Dtype out_type = func.function.is_vectorized && ((x.type & Dtype.ITERABLE) != 0) ? x.type : func.function.type;
            JNode[] all_args = new JNode[func.args.Length];
            if (func.function.is_vectorized)
            {
                if (x is CurJson)
                {
                    CurJson xcur = (CurJson)x;
                    if (other_callables)
                    {
                        // x is a function of the current JSON, as is at least one other argument
                        JNode arg_outfunc(JNode inp)
                        {
                            var itbl = xcur.function(inp);
                            for (int ii = 0; ii < other_args.Length; ii++)
                            {
                                JNode other_arg = other_args[ii];
                                all_args[ii + 1] = other_arg is CurJson ? ((CurJson)other_arg).function(inp) : other_arg;
                            }
                            if (itbl.type == Dtype.OBJ)
                            {
                                var dic = new Dictionary<string, JNode>();
                                var otbl = (JObject)itbl;
                                foreach ((string key, JNode val) in otbl.children)
                                {
                                    all_args[0] = val;
                                    dic[key] = func.function.Call(all_args);
                                }
                                return new JObject(0, dic);
                            }
                            else if (itbl.type == Dtype.ARR)
                            {
                                var arr = new List<JNode>();
                                var atbl = (JArray)itbl;
                                foreach (JNode val in atbl.children)
                                {
                                    all_args[0] = val;
                                    arr.Add(func.function.Call(all_args));
                                }
                                return new JArray(0, arr);
                            }
                            // x is a scalar function of the current JSON, so we just call the function on that scalar
                            // and the other args
                            all_args[0] = itbl;
                            return func.function.Call(all_args);
                        }
                        return new CurJson(out_type, arg_outfunc);
                    }
                    else
                    {
                        // there are no other functions of the current JSON; the first argument is the only one
                        // this means that all the other args are fixed and can be used as is
                        for (int ii = 0; ii < other_args.Length; ii++)
                        {
                            JNode other_arg = other_args[ii];
                            all_args[ii + 1] = other_arg;
                        }
                        JNode arg_outfunc(JNode inp)
                        {
                            
                            var itbl = xcur.function(inp);
                            if (itbl.type == Dtype.OBJ)
                            {
                                var dic = new Dictionary<string, JNode>();
                                var otbl = (JObject)itbl;
                                foreach ((string key, JNode val) in otbl.children)
                                {
                                    all_args[0] = val;
                                    dic[key] = func.function.Call(all_args);
                                }
                                return new JObject(0, dic);
                            }
                            else if (itbl.type == Dtype.ARR)
                            {
                                var arr = new List<JNode>();
                                var atbl = (JArray)itbl;
                                foreach (JNode val in atbl.children)
                                {
                                    all_args[0] = val;
                                    arr.Add(func.function.Call(all_args));
                                }
                                return new JArray(0, arr);
                            }
                            // x is a scalar function of the input
                            all_args[0] = itbl;
                            return func.function.Call(all_args);
                        }
                        return new CurJson(out_type, arg_outfunc);
                    }
                }
                if (other_callables)
                {
                    // at least one other argument is a function of the current JSON, but not the first argument
                    if (x.type == Dtype.OBJ)
                    {
                        JObject xobj = (JObject)x;
                        JNode arg_outfunc(JNode inp)
                        {
                            var dic = new Dictionary<string, JNode>();
                            for (int ii = 0; ii < other_args.Length; ii++)
                            {
                                JNode other_arg = other_args[ii];
                                all_args[ii + 1] = other_arg is CurJson ? ((CurJson)other_arg).function(inp) : other_arg;
                            }
                            foreach ((string key, JNode val) in xobj.children)
                            {
                                all_args[0] = val;
                                dic[key] = func.function.Call(all_args);
                            }
                            return new JObject(0, dic);
                        }
                        return new CurJson(out_type, arg_outfunc);
                    }
                    else if (x.type == Dtype.ARR)
                    {
                        // x is an array and at least one other argument is a function of the current JSON
                        var xarr = (JArray)x;
                        JNode arg_outfunc(JNode inp)
                        {
                            var arr = new List<JNode>();
                            for (int ii = 0; ii < other_args.Length; ii++)
                            {
                                JNode other_arg = other_args[ii];
                                all_args[ii + 1] = other_arg is CurJson ? ((CurJson)other_arg).function(inp) : other_arg;
                            }
                            foreach (JNode val in xarr.children)
                            {
                                all_args[0] = val;
                                arr.Add(func.function.Call(all_args));
                            }
                            return new JArray(0, arr);
                        }
                        return new CurJson(out_type, arg_outfunc);
                    }
                    else
                    {
                        // x is not iterable, and at least one other arg is a function of the current JSON
                        JNode arg_outfunc(JNode inp)
                        {
                            for (int ii = 0; ii < other_args.Length; ii++)
                            {
                                JNode other_arg = other_args[ii];
                                all_args[ii + 1] = other_arg is CurJson ? ((CurJson)other_arg).function(inp) : other_arg;
                            }
                            all_args[0] = x;
                            return func.function.Call(all_args);
                        }
                        return new CurJson(out_type, arg_outfunc);
                    }
                }
                else
                {
                    // none of the arguments are functions of the current JSON
                    for (int ii = 0; ii < other_args.Length; ii++)
                    {
                        JNode other_arg = other_args[ii];
                        all_args[ii + 1] = other_arg;
                    }
                    if (x.type == Dtype.OBJ)
                    {
                        var xobj = (JObject)x;
                        var dic = new Dictionary<string, JNode>();
                        foreach ((string key, JNode val) in xobj.children)
                        {
                            all_args[0] = val;
                            dic[key] = func.function.Call(all_args);
                        }
                        return new JObject(0, dic);
                    }
                    else if (x.type == Dtype.ARR)
                    {
                        var xarr = (JArray)x;
                        var arr = new List<JNode>();
                        foreach (JNode val in xarr.children)
                        {
                            all_args[0] = val;
                            arr.Add(func.function.Call(all_args));
                        }
                        return new JArray(0, arr);
                    }
                    // x is not iterable, and no args are functions of the current JSON
                    all_args[0] = x;
                    return func.function.Call(all_args);
                }
            }
            else
            {
                // this is NOT a vectorized arg function (it's something like len or mean)
                if (x is CurJson)
                {
                    CurJson xcur = (CurJson)x;
                    if (other_callables)
                    {
                        JNode arg_outfunc(JNode inp)
                        {
                            for (int ii = 0; ii < other_args.Length; ii++)
                            {
                                JNode other_arg = other_args[ii];
                                all_args[ii + 1] = other_arg is CurJson ? ((CurJson)other_arg).function(inp) : other_arg;
                            }
                            all_args[0] = xcur.function(inp);
                            return func.function.Call(all_args);
                        }
                        return new CurJson(out_type, arg_outfunc);
                    }
                    else
                    {
                        for (int ii = 0; ii < other_args.Length; ii++)
                        {
                            JNode other_arg = other_args[ii];
                            all_args[ii + 1] = other_arg;
                        }
                        JNode arg_outfunc(JNode inp)
                        {
                            all_args[0] = xcur.function(inp);
                            return func.function.Call(all_args);
                        }
                        return new CurJson(out_type, arg_outfunc);
                    }
                }
                else if (other_callables)
                {
                    // it's a non-vectorized function where the first arg is not a current json func but at least
                    // one other is
                    JNode arg_outfunc(JNode inp)
                    {
                        for (int ii = 0; ii < other_args.Length; ii++)
                        {
                            JNode other_arg = other_args[ii];
                            all_args[ii + 1] = other_arg is CurJson ? ((CurJson)other_arg).function(inp) : other_arg;
                        }
                        all_args[0] = x;
                        return func.function.Call(all_args);
                    }
                    return new CurJson(out_type, arg_outfunc);
                }
                // it is a non-vectorized function where none of the args are functions of the current
                // json (e.g., s_mul(`a`, 14))
                for (int ii = 0; ii < other_args.Length; ii++)
                {
                    JNode other_arg = other_args[ii];
                    all_args[ii + 1] = other_arg;
                }
                all_args[0] = x;
                return func.function.Call(all_args);
            }
        }
        #endregion
        #region PARSER_FUNCTIONS

        private static object? PeekNextToken(List<object> toks, int ii)
        {
            if (ii + 1 >= toks.Count) { return null; }
            return toks[ii + 1];
        }

        private (JSlicer, int ii) ParseSlicer(List<object> toks, int ii, int? first_num)
        {
            var slicer = new int?[3];
            int slots_filled = 0;
            int? last_num = first_num;
            while (ii < toks.Count)
            {
                object t = toks[ii];
                if (t is char)
                {
                    char tval = (char)t;
                    if (tval == ':')
                    {
                        slicer[slots_filled++] = last_num;
                        last_num = null;
                        ii++;
                        continue;
                    }
                    else if (EXPR_FUNC_ENDERS.Contains(tval))
                    {
                        break;
                    }
                }
                try
                {
                    JNode numtok;
                    (numtok, ii) = ParseExprOrScalarFunc(toks, ii);
                    if (numtok.type != Dtype.INT)
                    {
                        throw new ArgumentException();
                    }
                    last_num = Convert.ToInt32(numtok.value);
                }
                catch (Exception e)
                {
                    throw new RemesPathException("Found non-integer while parsing a slicer");
                }
                if (slots_filled == 2)
                {
                    break;
                }
            }
            slicer[slots_filled++] = last_num;
            slicer = slicer.Take(slots_filled).ToArray();
            return (new JSlicer(slicer), ii);
        }

        private static object GetSingleIndexerListValue(JNode ind)
        {
            switch (ind.type)
            {
                case Dtype.STR: return (string)ind.value;
                case Dtype.INT: return Convert.ToInt32(ind.value);
                case Dtype.SLICE: return ((JSlicer)ind).slicer;
                case Dtype.REGEX: return ((JRegex)ind).regex;
                default: throw new RemesPathException("Entries in an indexer list must be string, regex, int, or slice.");
            }
        }

        private (Indexer indexer, int ii) ParseIndexer(List<object> toks, int ii)
        {
            object t = toks[ii];
            object? nt;
            if (!(t is char))
            {
                throw new RemesPathException("Expected delimiter at the start of indexer");
            }
            char d = (char)t;
            List<object> children = new List<object>();
            if (d == '.')
            {
                nt = PeekNextToken(toks, ii);
                if (nt != null)
                {
                    if (nt is Binop && ((Binop)nt).name == "*")
                    {
                        // it's a '*' indexer, which means select all keys/indices
                        return (new StarIndexer(), ii + 2);
                    }
                    JNode jnt = (JNode)nt;
                    if ((jnt.type & Dtype.STR_OR_REGEX) == 0)
                    {
                        throw new RemesPathException("'.' syntax for indexers requires that the indexer be a string, " +
                                                    "regex, or '*'");
                    }
                    if (jnt is JRegex)
                    {
                        children.Add(((JRegex)jnt).regex);
                    }
                    else
                    {
                        children.Add(jnt.value);
                    }
                    return (new VarnameList(children), ii + 2);
                }
            }
            else if (d == '{')
            {
                return ParseProjection(toks, ii+1);
            }
            else if (d != '[')
            {
                throw new RemesPathException("Indexer must start with '.' or '[' or '{'");
            }
            Indexer? indexer = null;
            object? last_tok = null;
            JNode jlast_tok;
            Dtype last_type = Dtype.UNKNOWN;
            t = toks[++ii];
            if (t is Binop && ((Binop)t).name == "*")
            {
                // it was '*', indicating a star indexer
                nt = PeekNextToken(toks, ii);
                if (nt is char && (char)nt  == ']')
                {
                    return (new StarIndexer(), ii + 2);
                }
                throw new RemesPathException("Unacceptable first token '*' for indexer list");
            }
            while (ii < toks.Count)
            {
                t = toks[ii];
                if (t is char)
                {
                    d = (char)t;
                    if (d == ']')
                    {
                        // it's a ']' that terminates the indexer
                        if (last_tok == null)
                        {
                            throw new RemesPathException("Empty indexer");
                        }
                        if (indexer == null)
                        {
                            if ((last_type & Dtype.STR_OR_REGEX) != 0)
                            {
                                indexer = new VarnameList(children);
                            }
                            else if ((last_type & Dtype.INT_OR_SLICE) != 0)
                            {
                                indexer = new SlicerList(children);
                            }
                            else
                            {
                                // it's a boolean index of some sort, e.g. [@ > 0]
                                indexer = new BooleanIndex(last_tok);
                            }
                        }
                        if (indexer is VarnameList || indexer is SlicerList)
                        {
                            children.Add(GetSingleIndexerListValue((JNode)last_tok));
                        }
                        else if ((indexer is VarnameList && (last_type & Dtype.STR_OR_REGEX) == 0) // a non-string, non-regex in a varname list
                                || (indexer is SlicerList && (last_type & Dtype.INT_OR_SLICE) == 0))// a non-int, non-slice in a slicer list
                        {
                            throw new RemesPathException("Cannot have indexers with a mix of ints/slicers and " +
                                                         "strings/regexes");
                        }
                        return (indexer, ii + 1);
                    }
                    if (d == ',')
                    {
                        if (last_tok == null)
                        {
                            throw new RemesPathException("Comma before first token in indexer");
                        }
                        if (indexer == null)
                        {
                            if ((last_type & Dtype.STR_OR_REGEX) != 0)
                            {
                                indexer = new VarnameList(children);
                            }
                            else if ((last_type & Dtype.INT_OR_SLICE) != 0)
                            {
                                indexer = new SlicerList(children);
                            }
                        }
                        children.Add(GetSingleIndexerListValue((JNode)last_tok));
                        last_tok = null;
                        last_type = Dtype.UNKNOWN;
                        ii++;
                    }
                    else if (d == ':')
                    {
                        if (last_tok == null)
                        {
                            (last_tok, ii) = ParseSlicer(toks, ii, null);
                        }
                        else if (last_tok is JNode)
                        {
                            jlast_tok = (JNode)last_tok;
                            if (jlast_tok.type != Dtype.INT)
                            {
                                throw new RemesPathException($"Expected token other than ':' after {jlast_tok} " +
                                                             $"in an indexer");
                            }
                            (last_tok, ii) = ParseSlicer(toks, ii, Convert.ToInt32(jlast_tok.value));
                        }
                        else
                        {
                            throw new RemesPathException($"Expected token other than ':' after {last_tok} in an indexer");
                        }
                        last_type = ((JNode)last_tok).type;
                    }
                    else
                    {
                        throw new RemesPathException($"Expected token other than {t} after {last_tok} in an indexer");
                    }
                }
                else if (last_tok != null)
                {
                    throw new RemesPathException($"Consecutive indexers {last_tok} and {t} must be separated by commas");
                }
                else
                {
                    // it's a new token of some sort
                    (last_tok, ii) = ParseExprOrScalarFunc(toks, ii);
                    last_type = ((JNode)last_tok).type;
                }
            }
            throw new RemesPathException("Unterminated indexer");
        }

        private (JNode node, int ii) ParseExprOrScalar(List<object> toks, int ii)
        {
            if (toks.Count == 0)
            {
                throw new RemesPathException("Empty query");
            }
            object t = toks[ii];
            JNode? last_tok = null;
            if (t is Binop)
            {
                throw new RemesPathException($"Binop {(Binop)t} without appropriate left operand");
            }
            if (t is char)
            {
                char d = (char)t;
                if (d != '(')
                {
                    throw new RemesPathException($"Invalid token {d} at position {ii}");
                }
                int unclosed_parens = 1;
                List<object> subquery = new List<object>();
                for (int end = ii + 1; end < toks.Count; end++)
                {
                    object subtok = toks[end];
                    if (subtok is char)
                    {
                        char subd = (char)subtok;
                        if (subd == '(')
                        {
                            unclosed_parens++;
                        }
                        else if (subd == ')')
                        {
                            if (--unclosed_parens == 0)
                            {
                                (last_tok, _) = ParseExprOrScalarFunc(subquery, 0);
                                ii = end + 1;
                                break;
                            }
                        }
                    }
                    subquery.Add(subtok);
                }
            }
            else if (t is ArgFunction)
            {
                (last_tok, ii) = ParseArgFunction(toks, ii+1, (ArgFunction)t);
            }
            else
            {
                last_tok = (JNode)t;
                ii++;
            }
            if (last_tok == null)
            {
                throw new RemesPathException("Found null where JNode expected");
            }
            if ((last_tok.type & Dtype.ITERABLE) != 0)
            {
                // the last token is an iterable, so now we look for indexers that slice it
                var idxrs = new List<IndexerFunc>();
                object? nt = PeekNextToken(toks, ii - 1);
                object? nt2, nt3;
                while (nt != null && nt is char && INDEXER_STARTERS.Contains((char)nt))
                {
                    nt2 = PeekNextToken(toks, ii);
                    bool is_recursive = false;
                    if (nt2 is char && (char)nt2 == '.' && (char)nt == '.')
                    {
                        is_recursive = true;
                        nt3 = PeekNextToken(toks, ii + 1);
                        ii += (nt3 is char && (char)nt3 == '[') ? 2 : 1;
                    }
                    Indexer cur_idxr;
                    (cur_idxr, ii) = ParseIndexer(toks, ii);
                    nt = PeekNextToken(toks, ii - 1);
                    bool is_varname_list = cur_idxr is VarnameList;
                    bool has_one_option = false;
                    bool is_projection = false;
                    if (is_varname_list || cur_idxr is SlicerList)
                    {
                        List<object>? children = null;
                        if (is_varname_list)
                        {
                            children = ((VarnameList)cur_idxr).children;
                            // recursive search means that even selecting a single key/index could select from multiple arrays/dicts and thus get multiple results
                            if (!is_recursive && children.Count == 1 && children[0] is string)
                            {
                                // the indexer only selects a single key from a dict
                                // Since the key is defined implicitly by this choice, this indexer will only return the value
                                has_one_option = true;
                            }
                        }
                        else
                        {
                            children = ((SlicerList)cur_idxr).children;
                            if (!is_recursive && children.Count == 1 && children[0] is int)
                            {
                                // the indexer only selects a single index from an array
                                // Since the index is defined implicitly by this choice, this indexer will only return the value
                                has_one_option = true;
                            }
                        }
                        Func<JNode, IEnumerable<(object, JNode)>> idx_func = ApplyMultiIndex(children, is_varname_list, is_recursive);
                        idxrs.Add(new IndexerFunc(idx_func, has_one_option, is_projection, is_varname_list, is_recursive));
                    }
                    else if (cur_idxr is BooleanIndex)
                    {
                        object boodex_fun = ((BooleanIndex)cur_idxr).value;
                        Func<JNode, IEnumerable<(object, JNode)>> idx_func = ApplyBooleanIndex((JNode)boodex_fun);
                        idxrs.Add(new IndexerFunc(idx_func, has_one_option, is_projection, is_varname_list, is_recursive));
                    }
                    else if (cur_idxr is Projection)
                    {
                        Func<JNode, IEnumerable<(object, JNode)>> proj_func = ((Projection)cur_idxr).proj_func;
                        idxrs.Add(new IndexerFunc(proj_func, false, true, false, false));
                    }
                    else
                    {
                        // it's a star indexer
                        idxrs.Add(new IndexerFunc(ApplyStarIndexer, has_one_option, is_projection, is_varname_list, false));
                    }
                }
                if (idxrs.Count > 0)
                {
                    if (last_tok is CurJson)
                    {
                        CurJson lcur = (CurJson)last_tok;
                        JNode idx_func(JNode inp)
                        {
                            return ApplyIndexerList(idxrs)(lcur.function(inp));
                        }
                        return (new CurJson(lcur.type, idx_func), ii);
                    }
                    if (last_tok is JObject)
                    {
                        return (ApplyIndexerList(idxrs)((JObject)last_tok), ii);
                    }
                    return (ApplyIndexerList(idxrs)((JArray)last_tok), ii);
                }
            }
            return (last_tok, ii);
        }

        private (JNode node, int ii) ParseExprOrScalarFunc(List<object> toks, int ii)
        {
            object? curtok = null;
            object? nt = PeekNextToken(toks, ii);
            // most common case is a single JNode followed by the end of the query or an expr func ender
            // e.g., in @[0,1,2], all of 0, 1, and 2 are immediately followed by an expr func ender
            // and in @.foo.bar the bar is followed by EOF
            // MAKE THE COMMON CASE FAST!
            if (nt == null || (nt is char && EXPR_FUNC_ENDERS.Contains((char)nt)))
            {
                curtok = toks[ii];
                if (!(curtok is JNode))
                {
                    throw new RemesPathException($"Invalid token {curtok} where JNode expected");
                }
                return ((JNode)curtok, ii + 1);
            }
            bool uminus = false;
            object? left_tok = null;
            object? left_operand = null;
            float left_precedence = float.MinValue;
            BinopWithArgs root = null;
            BinopWithArgs leaf = null;
            object[] children = new object[2];
            Binop func;
            while (ii < toks.Count)
            {
                left_tok = curtok;
                curtok = toks[ii];
                if (curtok is char && EXPR_FUNC_ENDERS.Contains((char)curtok))
                {
                    if (left_tok == null)
                    {
                        throw new RemesPathException("No expression found where scalar expected");
                    }
                    curtok = left_tok;
                    break;
                }
                if (curtok is Binop)
                {
                    func = (Binop)curtok;
                    if (left_tok == null || left_tok is Binop)
                    {
                        if (func.name != "-")
                        {
                            throw new RemesPathException($"Binop {func.name} with invalid left operand");
                        }
                        uminus = !uminus;
                    }
                    else
                    {
                        float show_precedence = func.precedence;
                        if (func.name == "**")
                        {
                            show_precedence += (float)0.1;
                            // to account for right associativity or exponentiation
                            if (uminus)
                            {
                                // to account for exponentiation binding more tightly than unary minus
                                curtok = func = new Binop(Binop.NegPow, show_precedence, "negpow");
                                uminus = false;
                            }
                        }
                        else
                        {
                            show_precedence = func.precedence;
                        }
                        if (left_precedence >= show_precedence)
                        {
                            // the left binop wins, so it takes the last operand as its right.
                            // this binop becomes the root, and the next binop competes with it.
                            leaf.right = left_operand;
                            var newroot = new BinopWithArgs(func, root, null);
                            leaf = root = newroot;
                        }
                        else
                        {
                            // the current binop wins, so it takes the left operand as its left.
                            // the root stays the same, and the next binop competes with the current binop
                            if (root == null)
                            {
                                leaf = root = new BinopWithArgs(func, left_operand, null);
                            }
                            else
                            {
                                var newleaf = new BinopWithArgs(func, left_operand, null);
                                leaf.right = newleaf;
                                leaf = newleaf;
                            }
                        }
                        left_precedence = func.precedence;
                    }
                    ii++;
                }
                else
                {
                    if (left_tok != null && !(left_tok is Binop))
                    {
                        throw new RemesPathException("Can't have two iterables or scalars unseparated by a binop");
                    }
                    (left_operand, ii) = ParseExprOrScalar(toks, ii);
                    if (uminus)
                    {
                        nt = PeekNextToken(toks, ii - 1);
                        if (!(nt != null && nt is Binop && ((Binop)nt).name == "**"))
                        {
                            // applying unary minus to this expr/scalar has higher precedence than everything except
                            // exponentiation.
                            JNode[] args = new JNode[] { (JNode)left_operand };
                            var uminus_func = new ArgFunctionWithArgs(ArgFunction.FUNCTIONS["__UMINUS__"], args);
                            left_operand = ApplyArgFunction(uminus_func);
                            uminus = false;
                        }
                    }
                    curtok = left_operand;
                }
            }
            if (root != null)
            {
                leaf.right = curtok;
                left_operand = ResolveBinopTree(root);
            }
            if (left_operand == null)
            {
                throw new RemesPathException("Null return from ParseExprOrScalar");
            }
            return ((JNode)left_operand, ii);
        }

        private (JNode node, int ii) ParseArgFunction(List<object> toks, int ii, ArgFunction fun)
        {
            object t = toks[ii];
            if (!(t is char && (char)t == '('))
            {
                throw new RemesPathException($"Function {fun.name} must have parens surrounding arguments");
            }
            ii++;
            int arg_num = 0;
            Dtype[] intypes = fun.input_types();
            JNode[] args = new JNode[fun.max_args];
            JNode? cur_arg = null;
            while (ii < toks.Count)
            {
                t = toks[ii];
                Dtype type_options = intypes[arg_num];
                try
                {
                    try
                    {
                        (cur_arg, ii) = ParseExprOrScalarFunc(toks, ii);
                    }
                    catch
                    {
                        cur_arg = null;
                    }
                    if ((Dtype.SLICE & type_options) != 0)
                    {
                        object? nt = PeekNextToken(toks, ii - 1);
                        if (nt is char && (char)nt == ':')
                        {
                            int? first_num;
                            if (cur_arg == null)
                            {
                                first_num = null;
                            }
                            else
                            {
                                first_num = Convert.ToInt32(cur_arg.value);
                            }
                            (cur_arg, ii) = ParseSlicer(toks, ii, first_num);
                        }
                    }
                    if (cur_arg == null || (cur_arg.type & type_options) == 0)
                    {
                        Dtype arg_type = (cur_arg) == null ? Dtype.NULL : cur_arg.type;
                        throw new RemesPathException($"For arg {arg_num} of function {fun.name}, expected argument of type "
                                                     + $"in {type_options}, instead got type {arg_type}");
                    }
                }
                catch (Exception ex)
                {
                    throw new RemesPathException($"For arg {arg_num} of function {fun.name}, expected argument of type "
                                                 + $"in {type_options}, instead threw exception {ex}.");
                }
                t = toks[ii];
                bool comma = false;
                bool close_paren = false;
                if (t is char)
                {
                    char d = (char)t;
                    comma = d == ',';
                    close_paren = d == ')';
                }
                else
                {
                    throw new RemesPathException($"Arguments of arg functions must be followed by ',' or ')', not {t}");
                }
                if (arg_num + 1 < fun.min_args && !comma)
                {
                    throw new RemesPathException($"Expected ',' after argument {arg_num} of function {fun.name} " +
                                                 $"({fun.min_args} - {fun.max_args} args)");
                }
                if (arg_num + 1 == fun.max_args && !close_paren)
                {
                    throw new RemesPathException($"Expected ')' after argument {arg_num} of function {fun.name} " +
                                                 $"({fun.min_args} - {fun.max_args} args)");
                }
                args[arg_num++] = cur_arg;
                ii++;
                if (close_paren)
                {
                    var withargs = new ArgFunctionWithArgs(fun, args);
                    for (int arg2 = arg_num; arg2 < fun.max_args; arg2++)
                    {
                        // fill the remaining args with null nodes; alternatively we could have ArgFunctions use JNode?[] instead of JNode[]
                        args[arg2] = new JNode(null, Dtype.NULL, 0);
                    }
                    return (ApplyArgFunction(withargs), ii);
                }
            }
            throw new RemesPathException($"Expected ')' after argument {arg_num} of function {fun.name} "
                                         + $"({fun.min_args} - {fun.max_args} args)");
        }

        private (Indexer proj, int ii) ParseProjection(List<object> toks, int ii)
        {
            var children = new List<(object, JNode)>();
            bool is_object_proj = false;
            while (ii < toks.Count)
            {
                JNode key;
                (key, ii) = ParseExprOrScalarFunc(toks, ii);
                object? nt = PeekNextToken(toks, ii - 1);
                if (nt is char)
                {
                    char nd = (char)nt;
                    if (nd == ':')
                    {
                        if (children.Count > 0 && !is_object_proj)
                        {
                            throw new RemesPathException("Mixture of values and key-value pairs in object/array projection");
                        }
                        if (key.type == Dtype.STR)
                        {
                            JNode val;
                            (val, ii) = ParseExprOrScalarFunc(toks, ii + 1);
                            children.Add(((string)key.value, val));
                            is_object_proj = true;
                            nt = PeekNextToken(toks, ii - 1);
                            if (!(nt is char))
                            {
                                throw new RemesPathException("Key-value pairs in projection must be delimited by ',' and projections must end with '}'.");
                            }
                            nd = (char)nt;
                        }
                        else
                        {
                            throw new RemesPathException($"Object projection keys must be string, not {key.type}");
                        }
                    }
                    else
                    {
                        // it's an array projection
                        children.Add((0, key));
                    }
                    if (nd == '}')
                    {
                        IEnumerable<(object, JNode)> proj_func(JNode obj)
                        {
                            foreach((object k, JNode v) in children)
                            {
                                yield return (k, (v is CurJson) ? ((CurJson)v).function(obj) : v); 
                            }
                        }
                        return (new Projection(proj_func), ii + 1);
                    }
                    if (nd != ',')
                    {
                        throw new RemesPathException("Values or key-value pairs in a projection must be comma-delimited");
                    }
                }
                else
                {
                    throw new RemesPathException("Values or key-value pairs in a projection must be comma-delimited");
                }
                ii++;
            }
            throw new RemesPathException("Unterminated projection");
        }
        #endregion

        #region EXCEPTION_PRETTIFIER
        // extracts the origin and target of the cast from an InvalidCastException
        private static Regex CAST_REGEX = new Regex("Unable to cast.+(Node|Object|Array|Char).+to type.+(Node|Object|Array|Char)", RegexOptions.Compiled);

        /// <summary>
        /// Try to take exceptions commonly thrown by this package and display them in a useful way.
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static string PrettifyException(Exception ex)
        {
            if (ex is RemesLexerException)
            {
                return ((RemesLexerException)ex).ToString();
            }
            if (ex is RemesPathException)
            {
                return ((RemesPathException)ex).ToString();
            }
            if (ex is JsonParserException)
            {
                return ((JsonParserException)ex).ToString();
            }
            string exstr = ex.ToString();
            Match is_cast = CAST_REGEX.Match(exstr);
            if (is_cast.Success)
            {
                string ogtype = "";
                string target = "";
                switch (is_cast.Groups[1].Value)
                {
                    case "Object": ogtype = "JSON object"; break;
                    case "Array": ogtype = "JSON array"; break;
                    case "Node": ogtype = "JSON scalar"; break;
                    case "Char": ogtype = "character"; break;
                }
                switch (is_cast.Groups[2].Value)
                {
                    case "Object": target = "JSON object"; break;
                    case "Array": target = "JSON array"; break;
                    case "Node": target = "JSON scalar"; break;
                    case "Char": target = "character"; break;
                }
                return $"When a {target} was expected, instead got a {ogtype}.";
            }
            return exstr;
        }
        #endregion
    }

    #region TEST_CLASSES
    public class SliceTester
    {
        public static void Test()
        {
            int[] onetofive = new int[] { 1, 2, 3, 4, 5 };
            var testcases = new (int[] input, int? start, int? stop, int? stride, int[] desired)[]
            {
                (onetofive, 2, null, null, new int[]{1, 2}),
                (onetofive, null, null, null, onetofive),
                (onetofive, null, 1, null, new int[]{1}),
                (onetofive, 1, 3, null, new int[]{2, 3}),
                (onetofive, 1, 4, 2, new int[]{2, 4}),
                (onetofive, 2, null, -1, new int[]{3, 2, 1}),
                (onetofive, 4, 1, -2, new int[]{5, 3}),
                (onetofive, 1, null, 3, new int[]{2, 5}),
                (onetofive, 4, 2, -1, new int[]{5, 4}),
                (onetofive, -3, null, null, new int[]{1,2}),
                (onetofive, -4, -1, null, new int[]{2,3,4}),
                (onetofive, -4, null, 2, new int[]{2, 4}),
                (onetofive, null, -3, null, new int[]{1,2}),
                (onetofive, -3, null, 1, new int[]{3,4,5}),
                (onetofive, -3, null, -1, new int[]{3,2,1}),
                (onetofive, -1, 1, -2, new int[]{5, 3}),
                (onetofive, 1, -1, null, new int[]{2,3,4}),
                (onetofive, -4, 4, null, new int[]{2,3,4}),
                (onetofive, -4, 4, 2, new int[]{2, 4}),
                (onetofive, 2, -2, 2, new int[]{3}),
                (onetofive, -4, null, -2, new int[]{2}),
                (onetofive, 2, 1, null, new int[]{ })
            };
            int tests_failed = 0;
            int ii = 0;
            foreach ((int[] input, int? start, int? stop, int? stride, int[] desired) in testcases)
            {
                int[] output = (int[])input.Slice(start, stop, stride);
                // verify that it works for both arrays and Lists, because both implement IList
                List<int> list_output = (List<int>)(new List<int>(input)).Slice(start, stop, stride);
                var sb_desired = new StringBuilder();
                sb_desired.Append('{');
                foreach (int desired_value in desired)
                {
                    sb_desired.Append(desired_value.ToString());
                    sb_desired.Append(", ");
                }
                sb_desired.Append('}');
                string str_desired = sb_desired.ToString();
                var sb_output = new StringBuilder();
                sb_output.Append('{');
                foreach (int value in output)
                {
                    sb_output.Append(value.ToString());
                    sb_output.Append(", ");
                }
                sb_output.Append('}');
                string str_output = sb_output.ToString();
                if (str_output != str_desired)
                {

                    tests_failed++;
                    Console.WriteLine(String.Format("Test {0} ({1}.Slice({2}, {3}, {4})) failed:\n" +
                                                    "Expected\n{5}\nGot\n{6}",
                                                    ii+1, input, start, stop, stride, str_desired, str_output));
                }
                ii++;
                var sb_list_output = new StringBuilder();
                sb_list_output.Append('{');
                foreach (int value in list_output)
                {
                    sb_list_output.Append(value.ToString());
                    sb_list_output.Append(", ");
                }
                sb_list_output.Append('}');
                string str_list_output = sb_list_output.ToString();
                if (str_list_output != str_desired)
                {

                    tests_failed++;
                    Console.WriteLine(String.Format("Test {0} ({1}.Slice({2}, {3}, {4})) failed:\n" +
                                                    "Expected\n{5}\nGot\n{6}",
                                                    ii+1, input, start, stop, stride, str_desired, str_list_output));
                }
                ii++;
            }
            var str_testcases = new (int[] input, string slicer, int[] desired)[]
            {
                (onetofive, "2", new int[]{1, 2}),
                (onetofive, ":", onetofive),
                (onetofive, ":1", new int[]{1}),
                (onetofive, "1:3", new int[]{2, 3}),
                (onetofive, "1::3", new int[]{2, 5}),
                (onetofive, "1:4:2", new int[]{2, 4}),
                (onetofive, "2::-1", new int[]{3, 2, 1}),
                (onetofive, "4:1:-2", new int[]{5, 3}),
                (onetofive, "1::3", new int[]{2, 5}),
                (onetofive, "4:2:-1", new int[]{5, 4}),
                (onetofive, "-3", new int[]{1,2}),
                (onetofive, "-4:-1", new int[]{2,3,4}),
                (onetofive, "-4::2", new int[]{2, 4}),
                (onetofive, ":-3", new int[]{1,2}),
                (onetofive, "-3::1", new int[]{3,4,5}),
                (onetofive, "-3:", new int[]{3,4,5}),
                (onetofive, "-3::-1", new int[]{3,2,1}),
                (onetofive, "-1:1:-2", new int[]{5, 3}),
                (onetofive, "1:-1", new int[]{2,3,4}),
                (onetofive, "-4:4", new int[]{2,3,4}),
                (onetofive, "-4:4:2", new int[]{2, 4}),
                (onetofive, "2:-2:2", new int[]{3}),
                (onetofive, "3::5", new int[]{4}),
                (onetofive, "5:", new int[]{ }),
                (onetofive, "3:8", new int[]{4, 5}),
                (onetofive, "-2:15", new int[]{4,5})
            };
            // test string slicer
            foreach ((int[] input, string slicer, int[] desired) in str_testcases)
            {
                int[] output = (int[])input.Slice(slicer);
                var sb_desired = new StringBuilder();
                sb_desired.Append('{');
                foreach (int desired_value in desired)
                {
                    sb_desired.Append(desired_value.ToString());
                    sb_desired.Append(", ");
                }
                sb_desired.Append('}');
                string str_desired = sb_desired.ToString();
                var sb_output = new StringBuilder();
                sb_output.Append('{');
                foreach (int value in output)
                {
                    sb_output.Append(value.ToString());
                    sb_output.Append(", ");
                }
                sb_output.Append('}');
                string str_output = sb_output.ToString();
                if (str_output != str_desired)
                {

                    tests_failed++;
                    Console.WriteLine(String.Format("Test {0} ({1}.Slice(\"{2}\")) failed:\n" +
                                                    "Expected\n{3}\nGot\n{4}",
                                                    ii+1, input, slicer, str_desired, str_output));
                }
                ii++;
            }

            Console.WriteLine($"Failed {tests_failed} tests.");
            Console.WriteLine($"Passed {ii - tests_failed} tests.");
        }
    }

    public class RemesParserTester
    {
        public static void Test()
        {
            JsonParser jsonParser = new JsonParser();
            JNode foo = jsonParser.Parse("{\"foo\": [[0, 1, 2], [3.0, 4.0, 5.0], [6.0, 7.0, 8.0]], " +
                                           "\"bar\": {\"a\": false, \"b\": [\"a`g\", \"bah\"]}, \"baz\": \"z\", " +
                                           "\"quz\": {}, \"jub\": [], \"guzo\": [[[1]], [[2], [3]]], \"7\": [{\"foo\": 2}, 1], \"_\": {\"0\": 0}}");
            RemesParser remesparser = new RemesParser();
            Console.WriteLine($"The queried JSON in the RemesParser tests is:{foo.ToString()}");
            var testcases = new (string query, string desired_result)[]
            {
                // binop precedence tests
                ("2 - 4 * 3.5", "-12.0"),
                ("2 / 3 - 4 * 5 ** 1", "-58/3"),
                ("5 ** (6 - 2)", "625.0"),
                // binop two jsons, binop json scalar, binop scalar json tests
                ("@.foo[0] + @.foo[1]", "[3.0, 5.0, 7.0]"),
                ("@.foo[0] + j`[3.0, 4.0, 5.0]`", "[3.0, 5.0, 7.0]"),
                ("j`[0, 1, 2]` + @.foo[1]", "[3.0, 5.0, 7.0]"),
                ("1 + @.foo[0]", "[1, 2, 3]"),
                ("@.foo[0] + 1", "[1, 2, 3]"),
                ("1 + j`[0, 1, 2]`", "[1, 2, 3]"),
                ("j`[0, 1, 2]` + 1", "[1, 2, 3]"),
                ("`a` + str(range(3))", "[\"a0\", \"a1\", \"a2\"]"),
                ("str(range(3)) + `a`", "[\"0a\", \"1a\", \"2a\"]"),
                ("str(@.foo[0]) + `a`", "[\"0a\", \"1a\", \"2a\"]"),
                ("`a` + str(@.foo[0])", "[\"a0\", \"a1\", \"a2\"]"),
                // uminus tests
                ("-j`[1]`", "[-1]"),
                ("-j`[1,2]`**-3", "[-1.0, -1/8]"),
                ("-@.foo[2]", "[-6.0, -7.0, -8.0]"),
                ("2/--3", "2/3"),
                // indexing tests
                ("@.baz", "\"z\""),
                ("@.foo[0]", "[0, 1, 2]"),
                ("@[g`^b`]", "{\"bar\": {\"a\": false, \"b\": [\"a`g\", \"bah\"]}, \"baz\": \"z\"}"),
                ("@.foo[1][@ > 3.5]", "[4.0, 5.0]"),
                ("@.foo[-2:]", "[[3.0, 4.0, 5.0], [6.0, 7.0, 8.0]]"),
                ("@.foo[:3:2]", "[[0, 1, 2], [6.0, 7.0, 8.0]]"),
                ("@[foo, jub]", "{\"foo\": [[0, 1, 2], [3.0, 4.0, 5.0], [6.0, 7.0, 8.0]], \"jub\": []}"),
                ("@[foo, jub][2]", "{\"foo\": [6.0, 7.0, 8.0]}"),
                ("@[foo][0][0,2]", "[0, 2]"),
                ("@[foo][0][0, 2:]", "[0, 2]"),
                ("@[foo][0][2:, 0]", "[2, 0]"),
                ("@[foo][0][0, 2:, 1] ", "[0, 2, 1]"),
                ("@[foo][0][:1, 2:]", "[0, 2]"),
                ("@[foo][0][0, 2:4]", "[0, 2]"),
                ("@[foo][0][3:, 0]", "[0]"),
                ("@.*", foo.ToString()),
                ("@.foo[*]", "[[0, 1, 2], [3.0, 4.0, 5.0], [6.0, 7.0, 8.0]]"),
                ("@.foo[:2][2*@[0] >= @[1]]", "[[3.0, 4.0, 5.0]]"),
                ("@.foo[-1]", "[6.0, 7.0, 8.0]"),
                ("@.g`[a-z]oo`", "{\"foo\": [[0, 1, 2], [3.0, 4.0, 5.0], [6.0, 7.0, 8.0]]}"),
                // ufunction tests
                ("len(@)", ((JObject)foo).Length.ToString()),
                ("s_mul(@.bar.b, 2)", "[\"a`ga`g\", \"bahbah\"]"),
                ("in(1, @.foo[0])", "true"),
                ("in(4.0, @.foo[0])", "false"),
                ("in(`foo`, @)", "true"),
                ("in(`fjdkfjdkuren`, @)", "false"),
                ("range(2, len(@)) * 3", "[6, 9, 12, 15, 18, 21]"),
                ("sort_by(@.foo, 0, true)[:2]", "[[6.0, 7.0, 8.0], [3.0, 4.0, 5.0]]"),
                ("mean(flatten(@.foo[0]))", "1.0"),
                ("flatten(@.foo)[:4]", "[0, 1, 2, 3.0]"),
                ("flatten(@.guzo, 2)", "[1, 2, 3]"),
                ("min_by(@.foo, 1)", "[0, 1, 2]"),
                ("s_sub(@.bar.b, g`a(\\`?)`, `$1z`)", "[\"`zg\", \"bzh\"]"),
                ("isna(@.foo[0])", "[false, false, false]"),
                ("s_slice(@.bar.b, 2)", "[\"g\", \"h\"]"),
                ("s_slice(@.bar.b, ::2)", "[\"ag\", \"bh\"]"),
                ("str(@.foo[2])", "[\"6.0\", \"7.0\", \"8.0\"]"),
                ("int(@.foo[1])", "[3, 4, 5]"),
                ("s_slice(str(@.foo[2]), 2:)", "[\"0\", \"0\", \"0\"]"),
                ("sorted(flatten(@.guzo, 2))", "[1, 2, 3]"),
                ("keys(@)", "[\"foo\", \"bar\", \"baz\", \"quz\", \"jub\", \"guzo\", \"7\", \"_\"]"),
                ("values(@.bar)[:]", "[false, [\"a`g\", \"bah\"]]"),
                ("s_join(`\t`, @.bar.b)", "\"a`g\tbah\""),
                ("sorted(unique(@.foo[1]), true)", "[5.0, 4.0, 3.0]"), // have to sort because this function involves a HashSet so order is random
                ("unique(@.foo[0], true)", "[0, 1, 2]"),
                ("sort_by(value_counts(@.foo[0]), 1)", "[[0, 1], [1, 1], [2, 1]]"), // function involves a Dictionary so order is inherently random
                ("sort_by(value_counts(j`[1, 2, 1, 3, 1]`), 0)", "[[1, 3], [2, 1], [3, 1]]"),
                ("quantile(flatten(@.foo[1:]), 0.5)", "5.5"),
                ("float(@.foo[0])[:1]", "[0.0]"),
                ("not(is_expr(values(@.bar)))", "[true, false]"),
				("round(@.foo[0] * 1.66)", "[0, 2, 3]"),
				("round(@.foo[0] * 1.66, 1)", "[0.0, 1.7, 3.3]"),
                ("round(@.foo[0] * 1.66, 2)", "[0.0, 1.66, 3.32]"),
                ("s_find(@.bar.b, g`[a-z]+`)", "[[\"a\", \"g\"], [\"bah\"]]"),
                ("s_count(@.bar.b, `a`)", "[1, 1]"),
                ("s_count(@.bar.b, g`[a-z]`)", "[2, 3]"),
                ("ifelse(@.foo[0] > quantile(@.foo[0], 0.5), `big`, `small`)", "[\"small\", \"small\", \"big\"]"),
                ("ifelse(is_num(j`[1, \"a\", 2.0]`), isnum, notnum)", "[\"isnum\", \"notnum\", \"isnum\"]"),
                ("s_upper(j`[\"hello\", \"world\"]`)", "[\"HELLO\", \"WORLD\"]"),
                ("s_strip(` a dog!\t`)", "\"a dog!\""),
                ("log(@.foo[0] + 1)", $"[0.0, {Math.Log(2)}, {Math.Log(3)}]"),
                ("log2(@.foo[1])", $"[{Math.Log2(3)}, 2.0, {Math.Log2(5)}]"),
                ("abs(j`[-1, 0, 1]`)", "[1, 0, 1]"),
                ("is_str(@.bar.b)", "[true, true]"),
                ("s_split(@.bar.b[0], g`[^a-z]+`)", "[\"a\", \"g\"]"),
                ("s_split(@.bar.b, `a`)", "[[\"\", \"`g\"], [\"b\", \"h\"]]"),
                ("group_by(@.foo, 0)", "{\"0\": [[0, 1, 2]], \"3.0\": [[3.0, 4.0, 5.0]], \"6.0\": [[6.0, 7.0, 8.0]]}"),
                ("group_by(j`[{\"foo\": 1, \"bar\": \"a\"}, {\"foo\": 2, \"bar\": \"b\"}, {\"foo\": 3, \"bar\": \"b\"}]`, bar).*{`sum`: sum(@[:].foo), `count`: len(@)}", "{\"a\": {\"sum\": 1.0, \"count\": 1}, \"b\": {\"sum\": 5.0, \"count\": 2}}"),
                //("agg_by(@.foo, 0, sum(flatten(@)))", "{\"0\": 3.0, \"3.0\": 11.0, \"6.0\": 21.0}"),
                ("index(j`[1,3,2,3,1]`, max(j`[1,3,2,3,1]`), true)", "3"),
                ("index(@.foo[0], min(@.foo[0]))", "0"),
                ("zip(j`[1,2,3]`, j`[\"a\", \"b\", \"c\"]`)", "[[1, \"a\"], [2, \"b\"], [3, \"c\"]]"),
                ("zip(@.foo[0], @.foo[1], @.foo[2], j`[-20, -30, -40]`)", "[[0, 3.0, 6.0, -20], [1, 4.0, 7.0, -30], [2, 5.0, 8.0, -40]]"),
                ("dict(zip(keys(@.bar), j`[1, 2]`))", "{\"a\": 1, \"b\": 2}"),
                ("dict(items(@))", foo.ToString()),
                ("dict(j`[[\"a\", 1], [\"b\", 2], [\"c\", 3]]`)", "{\"a\": 1, \"b\": 2, \"c\": 3}"),
                ("items(j`{\"a\": 1, \"b\": 2, \"c\": 3}`)", "[[\"a\", 1], [\"b\", 2], [\"c\", 3]]"),
                ("isnull(@.foo)", "[false, false, false]"),
                ("int(isnull(j`[1, 1.5, [], \"a\", \"2000-07-19\", \"1975-07-14 01:48:21\", null, false, {}]`))",
                    "[0, 0, 0, 0, 0, 0, 1, 0, 0]"),
                ("range(-10)", "[]"),
                ("range(-3, -5, -1)", "[-3, -4]"),
                ("range(2, 19, -5)", "[]"),
                ("range(2, 19, 5)", "[2, 7, 12, 17]"),
                ("range(3)", "[0, 1, 2]"),
                ("range(3, 5)", "[3, 4]"),
                ("range(-len(@))", "[]"),
                ("range(0, -len(@))", "[]"),
                ("range(0, len(@) - len(@))", "[]"),
                ("range(0, -len(@) + len(@))", "[]"), 
                // uminus'd CurJson appears to be causing problems with other arithmetic binops as the second arg to the range function
                ("range(0, -len(@) - len(@))", "[]"),
                ("range(0, -len(@) * len(@))", "[]"),
                ("range(0, 5, -len(@))", "[]"),
                ("-len(@) + len(@)", "0"), // see if binops of uminus'd CurJson are also causing problems when they're not the second arg to the range function
                ("-len(@) * len(@)", (-(((JObject)foo).Length * ((JObject)foo).Length)).ToString()),
                ("abs(-len(@) + len(@))", "0"), // see if other functions (not just range) of binops of uminus'd CurJson cause problems
                ("range(0, abs(-len(@) + len(@)))", "[]"),
                ("range(0, -abs(-len(@) + len(@)))", "[]"),
                // parens tests
                ("(@.foo[:2])", "[[0, 1, 2], [3.0, 4.0, 5.0]]"),
                ("(@.foo)[0]", "[0, 1, 2]"),
                // projection tests
                ("@{@.jub, @.quz}", "[[], {}]"),
                ("@.foo{foo: @[0], bar: @[1][:2]}", "{\"foo\": [0, 1, 2], \"bar\": [3.0, 4.0]}"),
                ("sorted(flatten(@.guzo, 2)){`min`: @[0], `max`: @[-1], `tot`: sum(@)}", "{\"min\": 1, \"max\": 3, \"tot\": 6}"),
                ("(@.foo[:]{`max`: max(@), `min`: min(@)})[0]", "{\"max\": 2.0, \"min\": 0.0}"),
                ("len(@.foo[:]{blah: 1})", "3"),
                ("str(@.foo[0]{a: @[0], b: @[1]})", "{\"a\": \"0\", \"b\": \"1\"}"),
                ("max_by(@.foo[:]{mx: max(@), first: @[0]}, mx)", "{\"mx\": 8.0, \"first\": 6.0}"),
                // recursive search
                ("@..g`\\\\d`", "[[{\"foo\": 2}, 1], 0]"),
                ("@..[foo,`0`]", "[[[0, 1, 2], [3.0, 4.0, 5.0], [6.0, 7.0, 8.0]], 2, 0]"),
                ("@..`7`[0].foo", "[2]"),
                ("@._..`0`", "[0]"),
                ("@.bar..[a, b]", "[false, [\"a`g\", \"bah\"]]"),
                ("@.bar..c", "{}"),
                ("@.bar..[a, c]", "[false]"),
                ("@.`7`..foo", "[2]"),
            };
            int ii = 0;
            int tests_failed = 0;
            JNode result;
            foreach ((string query, string desired_result_str) in testcases)
            {
                ii++;
                JNode desired_result = jsonParser.Parse(desired_result_str);
                try
                {
                    result = remesparser.Search(query, foo);
                }
                catch (Exception ex)
                {
                    tests_failed++;
                    Console.WriteLine($"Expected remesparser.Search({query}, foo) to return {desired_result.ToString()}, but instead threw" +
                                      $" an exception:\n{ex}");
                    continue;
                }
                if (result.type != desired_result.type || !result.Equals(desired_result))
                {
                    tests_failed++;
                    Console.WriteLine($"Expected remesparser.Search({query}, foo) to return {desired_result.ToString()}, " +
                                      $"but instead got {result.ToString()}.");
                }
            }

            Console.WriteLine($"Failed {tests_failed} tests.");
            Console.WriteLine($"Passed {ii - tests_failed} tests.");
        }
    }

    public class RemesPathBenchmarker
    {
        public static (JNode json, int len, long[] times) LoadJsonAndTime(string fname, int num_trials = 8)
        {
            JsonParser jsonParser = new JsonParser();
            Stopwatch watch = new Stopwatch();
            string jsonstr = File.ReadAllText(fname);
            int len = jsonstr.Length;
            long[] times = new long[num_trials];
            JNode json = new JNode(null, Dtype.NULL, 0);
            for (int ii = 0; ii < num_trials; ii++)
            {
                watch.Reset();
                watch.Start();
                json = jsonParser.Parse(jsonstr);
                watch.Stop();
                long t = watch.Elapsed.Ticks;
                times[ii] = t;
            }
            return (json, len, times);
        }

        public static (JNode json, long[] times, long compile_time) TimeRemesPathQuery(JNode json, string query, int num_trials = 8)
        {
            Stopwatch watch = new Stopwatch();
            long[] times = new long[num_trials];
            RemesParser parser = new RemesParser();
            JNode result = new JNode(null, Dtype.NULL, 0);
            watch.Start();
            Func<JNode, JNode> query_func = ((CurJson)parser.Compile(query)).function;
            watch.Stop();
            long compile_time = watch.Elapsed.Ticks;
            for (int ii = 0; ii < num_trials; ii++)
            {
                watch.Reset();
                watch.Start();
                result = query_func(json);
                watch.Stop();
                long t = watch.Elapsed.Ticks;
                times[ii] = t;
            }
            return (result, times, compile_time);
        }

        public static (double mu, double sd) GetMeanAndSd(long[] times)
        {
            double mu = 0;
            foreach (int t in times) { mu += t; }
            mu /= times.Length;
            double sd = 0;
            foreach (int t in times)
            {
                double diff = t - mu;
                sd += diff * diff;
            }
            sd = Math.Sqrt(sd / times.Length);
            return (mu, sd);
        }

        public static double ConvertTicks(double ticks, string new_unit = "ms", int sigfigs = 3)
        {
            switch (new_unit)
            {
                case "ms": return Math.Round(ticks / 1e4, 3);
                case "s": return Math.Round(ticks / 1e7, 3);
                case "ns": return Math.Round(ticks * 100, 3);
                case "mus": return Math.Round(ticks / 10, 3);
                default: throw new ArgumentException("Time unit must be s, mus, ms, or ns");
            }
        }

        /// <summary>
        /// Repeatedly parse the JSON of a large file (big_random.json, about 1MB, containing nested arrays, dicts,
        /// with ints, floats and strings as scalars)<br></br>
        /// Also repeatedly run a Remespath query on the JSON.<br></br>
        /// MOST RECENT RESULTS:<br></br>
        /// To convert JSON string of size 975068 into JNode took 185.589 +/- 53.713 ms over 14 trials
        /// Load times(ms): 214,175,222,181,267,248,229,171,175,248,139,121,114,87
        /// Compiling query "@[@[:].z =~ `(?i)[a-z]{5}`]" took 0.056 ms(one-time cost b/c caching)
        /// To run query "@[@[:].z =~ `(?i)[a-z]{5}`]" on JNode from JSON of size 975068 into took 1.854 +/- 3.915 ms over 14 trials
        /// Query times(ms) : 1.718,1.709,1.024,0.92,0.836,0.756,15.882,0.666,0.438,0.385,0.386,0.364,0.41,0.454<br></br>
        /// For reference, the Python standard library JSON parser is about 10x FASTER than JsonParser.Parse,<br></br>
        /// and my Python remespath implementation is 10-30x SLOWER than this remespath implementation.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="num_trials"></param>
        public static void BenchmarkBigFile(string query, int num_trials = 8)
        {
            (JNode json, int len, long[] load_times) = LoadJsonAndTime(@"C:\Users\mjols\Documents\csharp\JSON_Viewer_cmd\testfiles\big_random.json", num_trials);
            string json_preview = json.ToString().Slice(":300") + "\n...";
            Console.WriteLine($"Preview of json: {json_preview}");
            (double mu_load, double sd_load) = GetMeanAndSd(load_times);
            Console.WriteLine($"To convert JSON string of size {len} into JNode took {ConvertTicks(mu_load)} +/- {ConvertTicks(sd_load)} ms over {load_times.Length} trials");
            var load_times_str = new string[load_times.Length];
            for (int ii = 0; ii < load_times.Length; ii++)
            {
                load_times_str[ii] = (load_times[ii] / 10000).ToString();
            }
            Console.WriteLine($"Load times (ms): {String.Join(',', load_times_str)}");
            (JNode result, long[] times, long compile_time) = TimeRemesPathQuery(json, query, num_trials);
            (double mu, double sd) = GetMeanAndSd(times);
            Console.WriteLine($"Compiling query \"{query}\" took {ConvertTicks(compile_time)} ms (one-time cost b/c caching)");
            Console.WriteLine($"To run query \"{query}\" on JNode from JSON of size {len} into took {ConvertTicks(mu)} +/- {ConvertTicks(sd)} ms over {load_times.Length} trials");
            var query_times_str = new string[times.Length];
            for (int ii = 0; ii < times.Length; ii++)
            {
                query_times_str[ii] = Math.Round(times[ii] / 1e4, 3).ToString();
            }
            Console.WriteLine($"Query times (ms): {String.Join(',', query_times_str)}");
            string result_preview = result.ToString().Slice(":300") + "\n...";
            Console.WriteLine($"Preview of result: {result_preview}");
        }
    }
    #endregion
}
