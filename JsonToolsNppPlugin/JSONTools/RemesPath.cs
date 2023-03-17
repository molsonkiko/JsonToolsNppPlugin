/*
A query language for JSON. 
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JSON_Tools.Utils;

namespace JSON_Tools.JSON_Tools
{
    #region DATA_HOLDER_STRUCTS
    public struct Key_Node
    {
        public object obj;
        public JNode node;

        public Key_Node(object obj, JNode node)
        {
            this.obj = obj;
            this.node = node;
        }
    }

    public struct Obj_Pos
    {
        public object obj;
        public int pos;

        public Obj_Pos(object obj, int pos) { this.obj = obj; this.pos = pos; }
    }
    #endregion DATA_HOLDER_STRUCTS
    
    #region OTHER_HELPER_CLASSES
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
        public Func<JNode, IEnumerable<Key_Node>> proj_func;

        public Projection(Func<JNode, IEnumerable<Key_Node>> proj_func)
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
        public Func<JNode, IEnumerable<Key_Node>> idxr;
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
        /// <summary>
        /// is an object
        /// </summary>
        public bool is_dict;
        /// <summary>
        /// involves recursive search
        /// </summary>
        public bool is_recursive;

        public IndexerFunc(Func<JNode, IEnumerable<Key_Node>> idxr, bool has_one_option, bool is_projection, bool is_dict, bool is_recursive)
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
    /// an exception thrown when trying to use a boolean index of unequal length<br></br>
    /// or when trying to apply a binary operator to two objects with different sets of keys<br></br>
    /// or arrays with different lengths.
    /// </summary>
    public class VectorizedArithmeticException : RemesPathException
    {
        public VectorizedArithmeticException(string description) : base(description) { }

        public override string ToString() { return description; }
    }

    public class RemesPathArgumentException : RemesPathException
    {
        public int arg_num { get; set; }
        public ArgFunction func { get; set; }

        public RemesPathArgumentException(string description, int arg_num, ArgFunction func) : base(description)
        {
            this.arg_num = arg_num;
            this.func = func;
        }

        public override string ToString()
        {
            string fmt_dtype = JNode.FormatDtype(func.input_types()[arg_num]);
            return $"For argument {arg_num} of function {func.name}, expected {fmt_dtype}, instead {description}";
        }
    }
    #endregion
    /// <summary>
    /// RemesPath is similar to JMESPath, but more fully featured.<br></br>
    /// The RemesParser parses queries.
    /// </summary>
    public class RemesParser
    {
        public RemesPathLexer lexer;
        // /// <summary>
        // /// A LRU cache mapping queries to compiled results that the parser can check against
        // /// to save time on parsing.<br></br>
        // /// Not used, because parsing is really fast and so caching is unnecessary 
        // /// </summary>
        //public LruCache<string, JNode> cache;

        ///// <summary>
        ///// The cache_capacity indicates how many queries to store in the old query cache.
        ///// </summary>
        ///// <param name="cache_capacity"></param>
        public RemesParser()//(int cache_capacity = 64)
        {
            //cache = new LruCache<string, JNode>();
            lexer = new RemesPathLexer();
        }

        /// <summary>
        /// Parse a query and compile it into a RemesPath function that operates on JSON.<br></br>
        /// If the query is not a function of input, it will instead just output fixed JSON.<br></br>
        /// If is_assignment_expr is true, this means that the query is an assignment expression<br></br>
        /// (i.e., a query that mutates the underlying JSON)
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public JNode Compile(List<object> toks)
        {
            JNode result = (JNode)ParseExprOrScalarFunc(toks, 0).obj;
            return result;
        }

        /// <summary>
        /// Perform a RemesPath query on JSON and return the result.<br></br>
        /// If is_assignment_expr is true, this means that the query is an assignment expression<br></br>
        /// (i.e., a query that mutates the underlying JSON)
        /// </summary>
        /// <param name="query"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public JNode Search(string query, JNode obj, out bool is_assignment_expr)
        {
            List<object> toks = lexer.Tokenize(query, out is_assignment_expr);
            foreach (object tok in toks)
            {
                if (tok is char c && c == '=')
                {
                    is_assignment_expr = true;
                    break;
                }
            }
            if (is_assignment_expr)
            {
                JNode mutation_func = CompileAssignmentExpr(toks);
                // cache.Add(query, mutation_func);
                if (mutation_func is CurJson cjmut)
                {
                    return cjmut.function(obj);
                }
                return mutation_func;
            }
            JNode result = Compile(toks);
            if (result is CurJson cjres)
            {
                return ((CurJson)result).function(obj);
            }
            return result;
        }

        public JNode CompileAssignmentExpr(List<object> toks)
        {
            bool before_assignment_operator = true;
            List<object> lhs_toks = new List<object>();
            List<object> rhs_toks = new List<object>();
            foreach (object tok in toks)
            {
                if (tok is char c && c == '=')
                {
                    before_assignment_operator = false;
                    continue;
                }
                if (before_assignment_operator)
                    lhs_toks.Add(tok);
                else rhs_toks.Add(tok);
            }
            JNode lhs = Compile(lhs_toks);
            JNode rhs = Compile(rhs_toks);
            return ApplyAssignmentExpression(lhs, rhs);
        } 

        public static string EXPR_FUNC_ENDERS = "]:},)";
        // these tokens have high enough precedence to stop an expr_function or scalar_function
        public static string INDEXER_STARTERS = ".[{";

        #region INDEXER_FUNCTIONS
        private Func<JNode, IEnumerable<Key_Node>> ApplyMultiIndex(object inds, bool is_varname_list, bool is_recursive = false)
        {
            if (inds is CurJson)
            {
                IEnumerable<Key_Node> multi_idx_func(JNode x)
                {
                    return ApplyMultiIndex(((CurJson)inds).function(x), is_varname_list, is_recursive)(x);
                }
                return multi_idx_func;
            }
            var children = (List<object>)inds;
            if (is_varname_list)
            {
                if (is_recursive)
                {
                    IEnumerable<Key_Node> multi_idx_func(JNode x, string path, HashSet<string> paths_visited)
                    {
                        if (x is JArray)
                        {
                            // a varname list can only match dict keys, not array indices
                            // we'll just recursively search from each child of this array
                            JArray xarr = (JArray)x;
                            for (int ii = 0; ii < xarr.Length; ii++)
                            {
                                foreach (Key_Node kv in multi_idx_func(xarr[ii], path + ',' + ii.ToString(), paths_visited))
                                {
                                    yield return kv;
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
                                    foreach (string k in xobj.children.Keys)
                                    {
                                        JNode val = xobj[k];
                                        string newpath = path + ',' + new JNode(k, Dtype.STR, 0).ToString();
                                        if (k == strv)
                                        {
                                            if (!paths_visited.Contains(newpath))
                                                yield return new Key_Node(0, val);
                                            paths_visited.Add(newpath);
                                        }
                                        else
                                        {
                                            foreach (Key_Node ono in multi_idx_func(val, newpath, paths_visited))
                                                yield return new Key_Node(0, ono.node);
                                        }
                                    }
                                }
                                else
                                {
                                    Regex regv = (Regex)v;
                                    if (path == "") path = regv.ToString();
                                    foreach (string k in xobj.children.Keys)
                                    {
                                        JNode val = xobj[k];
                                        string newpath = path + ',' + new JNode(k, Dtype.STR, 0).ToString();
                                        if (regv.IsMatch(k))
                                        {
                                            if (!paths_visited.Contains(newpath))
                                                yield return new Key_Node(0, val);
                                            paths_visited.Add(newpath);
                                        }
                                        else
                                        {
                                            foreach (Key_Node ono in multi_idx_func(val, newpath, paths_visited))
                                            {
                                                yield return new Key_Node(0, ono.node);
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
                    IEnumerable<Key_Node> multi_idx_func(JNode x)
                    {
                        var xobj = (JObject)x;
                        foreach (object v in children)
                        {
                            if (v is string)
                            {
                                string vstr = (string)v;
                                if (xobj.children.TryGetValue(vstr, out JNode val))
                                {
                                    yield return new Key_Node(vstr, val);
                                }
                            }
                            else
                            {
                                foreach (Key_Node ono in ApplyRegexIndex(xobj, (Regex)v))
                                {
                                    yield return ono;
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
                    // TODO: decide whether to implement recursive search for slices and indices
                    throw new NotImplementedException("Recursive search for array indices and slices is not implemented");
                }
                IEnumerable<Key_Node> multi_idx_func(JNode x)
                {
                    JArray xarr = (JArray)x;
                    foreach (object ind in children)
                    {
                        if (ind is int?[])
                        {
                            // it's a slice, so yield all the JNodes in that slice
                            foreach (JNode subind in xarr.children.LazySlice((int?[])ind))
                            {
                                yield return new Key_Node(0, subind);
                            }
                        }
                        else
                        {
                            int ii = Convert.ToInt32(ind);
                            if (ii >= xarr.Length) { continue; }
                            // allow negative indices for consistency with how slicers work
                            yield return new Key_Node(0, xarr[ii >= 0 ? ii : ii + xarr.Length]);
                        }
                    }
                }
                return multi_idx_func;
            }
        }

        private IEnumerable<Key_Node> ApplyRegexIndex(JObject obj, Regex regex)
        {
            foreach (string ok in obj.children.Keys)
            {
                JNode val = obj[ok];
                if (regex.IsMatch(ok))
                {
                    yield return new Key_Node(ok, val);
                }
            }
        }

        private Func<JNode, IEnumerable<Key_Node>> ApplyBooleanIndex(JNode inds)
        {
            IEnumerable<Key_Node> bool_idxr_func(JNode x)
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
                            JObject xobj = (JObject)x;
                            foreach (string key in xobj.children.Keys)
                            {
                                yield return new Key_Node(key, xobj[key]);
                            }
                        }
                        else if (x.type == Dtype.ARR)
                        {
                            JArray xarr = (JArray)x;
                            for (int ii = 0; ii < xarr.Length; ii++)
                            {
                                yield return new Key_Node(ii, xarr[ii]);
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
                    foreach (string key in xobj.children.Keys)
                    {
                        JNode xval = xobj[key];
                        bool i_has_key = iobj.children.TryGetValue(key, out JNode ival);
                        if (i_has_key)
                        {
                            if (ival.type != Dtype.BOOL)
                            {
                                throw new VectorizedArithmeticException("bool index contains non-booleans");
                            }
                            if ((bool)ival.value)
                            {
                                yield return new Key_Node(key, xval);
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
                        JNode ival = iarr[ii];
                        JNode xval = xarr[ii];
                        if (ival.type != Dtype.BOOL)
                        {
                            throw new VectorizedArithmeticException("bool index contains non-booleans");
                        }
                        if ((bool)ival.value)
                        {
                            yield return new Key_Node(ii, xval);
                        }
                    }
                    yield break;
                }
            }
            return bool_idxr_func;
        }

        private IEnumerable<Key_Node> ApplyStarIndexer(JNode x)
        {
            if (x.type == Dtype.OBJ)
            {
                var xobj = (JObject)x;
                foreach (string key in xobj.children.Keys)
                {
                    yield return new Key_Node(key, xobj[key]);
                }
                yield break;
            }
            var xarr = (JArray)x;
            for (int ii = 0; ii < xarr.Length; ii++)
            {
                yield return new Key_Node(ii, xarr[ii]);
            }
        }

        /// <summary>
        /// Yield all *scalars* that are descendents of node, no matter their depth<br></br>
        /// Does not yield keys or indices, only the nodes themselves.
        /// EXAMPLE<br></br>
        /// RecursivelyFlattenIterable({"a": [true, 2, [3]], "b": {"c": ["d", "e"], "f": null}}) yields<br></br>
        /// true, 2, 3, "d", "e", null
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static IEnumerable<Key_Node> RecursivelyFlattenIterable(JNode node)
        {
            if (node is JObject obj)
            {
                foreach (JNode val in obj.children.Values)
                {
                    if ((val.type & Dtype.ITERABLE) == 0)
                    {
                        yield return new Key_Node(0, val);
                    }
                    else
                    {
                        foreach (Key_Node sub_kn in RecursivelyFlattenIterable(val))
                            yield return sub_kn;
                    }
                }
            }
            else if (node is JArray arr)
            {
                foreach (JNode val in arr.children)
                {
                    if ((val.type & Dtype.ITERABLE) == 0)
                    {
                        yield return new Key_Node(0, val);
                    }
                    else
                    {
                        foreach (Key_Node sub_kn in RecursivelyFlattenIterable(val))
                            yield return sub_kn;
                    }
                }
            }
        }

        /// <summary>
        /// return 2 if x is not an object or array<br></br>
        /// If it is an object or array:<br></br> 
        /// return 1 if its length is 0.<br></br>
        /// else return 0.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private static int ObjectOrArrayEmpty(JNode x)
        {
            if (x.type == Dtype.OBJ) { return (((JObject)x).Length == 0) ? 1 : 0; }
            if (x.type == Dtype.ARR) { return (((JArray)x).Length == 0) ? 1 : 0; }
            return 2;
        }

        private Func<JNode, JNode> ApplyIndexerList(List<IndexerFunc> indexers)
        {
            JNode idxr_list_func(JNode obj, List<IndexerFunc> idxrs)
            {
                IndexerFunc ix = idxrs[0];
                var inds = ix.idxr(obj).GetEnumerator();
                // IEnumerator<T>.MoveNext returns a bool indicating if the enumerator has passed the end of the collection
                if (!inds.MoveNext())
                {
                    // the IndexerFunc couldn't find
                    if (ix.is_dict)
                    {
                        return new JObject();
                    }
                    return new JArray();
                }
                Key_Node k1v1 = inds.Current;
                object k1 = k1v1.obj;
                JNode v1 = k1v1.node;
                Key_Node kv;
                object k;
                JNode v;
                bool is_dict = (ix.is_dict || k1 is string) && !ix.is_recursive;
                var arr = new List<JNode>();
                var dic = new Dictionary<string, JNode>();
                if (idxrs.Count == 1)
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
                            kv = inds.Current;
                            dic[(string)kv.obj] = kv.node;
                        }
                        return new JObject(0, dic);
                    }
                    arr = new List<JNode>();
                    arr.Add(v1);
                    while (inds.MoveNext())
                    {
                        arr.Add(inds.Current.node);
                    }
                    return new JArray(0, arr);
                }
                var remaining_idxrs = new List<IndexerFunc>();
                for (int ii = 1; ii < idxrs.Count; ii++)
                    remaining_idxrs.Add(idxrs[ii]);
                if (ix.is_projection)
                {
                    if (is_dict)
                    {
                        dic = new Dictionary<string, JNode>();
                        dic[(string)k1] = v1;
                        while (inds.MoveNext())
                        {
                            kv = inds.Current;
                            dic[(string)kv.obj] = kv.node;
                        }
                        // recursively search this projection using the remaining indexers
                        return idxr_list_func(new JObject(0, dic), remaining_idxrs);
                    }
                    arr = new List<JNode>();
                    arr.Add(v1);
                    while (inds.MoveNext())
                    {
                        arr.Add(inds.Current.node);
                    }
                    return idxr_list_func(new JArray(0, arr), remaining_idxrs);
                }
                JNode v1_subdex = idxr_list_func(v1, remaining_idxrs);
                if (ix.has_one_option)
                {
                    return v1_subdex;
                }
                int is_empty = ObjectOrArrayEmpty(v1_subdex);
                if (is_dict)
                {
                    dic = new Dictionary<string, JNode>();
                    if (is_empty != 1)
                    {
                        dic[(string)k1] = v1_subdex;
                    }
                    while (inds.MoveNext())
                    {
                        k = inds.Current.obj;
                        v = inds.Current.node;
                        JNode subdex = idxr_list_func(v, remaining_idxrs);
                        is_empty = ObjectOrArrayEmpty(subdex);
                        if (is_empty != 1)
                        {
                            dic[(string)k] = subdex;
                        }
                    }
                    return new JObject(0, dic);
                }
                // obj is a list iterator
                arr = new List<JNode>();
                if (is_empty != 1)
                {
                    arr.Add(v1_subdex);
                }
                while (inds.MoveNext())
                {
                    v = inds.Current.node;
                    JNode subdex = idxr_list_func(v, remaining_idxrs);
                    is_empty = ObjectOrArrayEmpty(subdex);
                    if (is_empty != 1)
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
            if (ObjectOrArrayEmpty(right) == 2)
            {
                if (ObjectOrArrayEmpty(left) == 2)
                {
                    return b.Call(left, right);
                }
                return BinopJsonScalar(b, left, right);
            }
            if (ObjectOrArrayEmpty(left) == 2)
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
                foreach (string key in robj.children.Keys)
                {
                    JNode right_val = robj[key];
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
                arr.Add(b.Call(larr[ii], rarr[ii]));
            }
            return new JArray(0, arr);
        }

        private JNode BinopJsonScalar(Binop b, JNode left, JNode right)
        {
            if (left.type == Dtype.OBJ)
            {
                var dic = new Dictionary<string, JNode>();
                var lobj = (JObject)left;
                foreach (string key in lobj.children.Keys)
                {
                    dic[key] = b.Call(lobj[key], right);
                }
                return new JObject(0, dic);
            }
            var arr = new List<JNode>();
            var larr = (JArray)left;
            for (int ii = 0; ii < larr.Length; ii++)
            {
                arr.Add(b.Call(larr[ii], right));
            }
            return new JArray(0, arr);
        }

        private JNode BinopScalarJson(Binop b, JNode left, JNode right)
        {
            if (right.type == Dtype.OBJ)
            {
                var dic = new Dictionary<string, JNode>();
                var robj = (JObject)right;
                foreach (string key in robj.children.Keys)
                {
                    dic[key] = b.Call(left, robj[key]);
                }
                return new JObject(0, dic);
            }
            var arr = new List<JNode>();
            var rarr = (JArray)right;
            for (int ii = 0; ii < rarr.Length; ii++)
            {
                arr.Add(b.Call(left, rarr[ii]));
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
            if (ltype == Dtype.OBJ || rtype == Dtype.OBJ)
            {
                if (ltype == Dtype.ARR || rtype == Dtype.ARR)
                {
                    throw new RemesPathException("Cannot have a function of an array and an object");
                }
                return Dtype.OBJ;
            }
            if (ltype == Dtype.ARR || rtype == Dtype.ARR)
            {
                if (ltype == Dtype.OBJ || rtype == Dtype.OBJ)
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
                    if (ltype == Dtype.STR || rtype == Dtype.STR)
                    {
                        if (rtype != Dtype.STR || ltype != Dtype.STR)
                        {
                            throw new RemesPathException("Cannot add non-string to string");
                        }
                        return Dtype.STR;
                    }
                }
                throw new RemesPathException($"Invalid argument types {JNode.FormatDtype(ltype)}" +
                                            $" and {JNode.FormatDtype(rtype)} for binop {name}");
            }
            if (Binop.BITWISE_BINOPS.Contains(name)) // ^, & , |
            {
                // return ints if acting on two ints, bools when acting on two bools
                if (ltype == Dtype.INT && rtype == Dtype.INT)
                {
                    return Dtype.INT;
                }
                if (ltype == Dtype.BOOL && rtype == Dtype.BOOL)
                    return Dtype.BOOL;
                throw new RemesPathException($"Incompatible types {JNode.FormatDtype(ltype)}" +
                                            $" and {JNode.FormatDtype(rtype)} for bitwise binop {name}");
            }
            // it's a polymorphic binop - one of -, +, *, %
            if (rtype == Dtype.BOOL && ltype == Dtype.BOOL)
            {
                throw new RemesPathException($"Can't do arithmetic operation {name} on two bools");
            }
            if (name == "//") { return Dtype.INT; }
            if (Binop.FLOAT_RETURNING_BINOPS.Contains(name)) { return Dtype.FLOAT; } 
            // division and exponentiation always give doubles
            if (((rtype & Dtype.INT) != 0) && ((ltype & Dtype.INT) != 0))
            {
                return rtype & ltype;
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
            List<JNode> other_args = new List<JNode>(func.args.Count - 1);
            for (int ii = 0; ii < func.args.Count - 1; ii++)
            {
                JNode arg = func.args[ii + 1];
                if (arg is CurJson) { other_callables = true; }
                other_args.Add(arg);
            }
            // vectorized functions take on the type of the iterable they're vectorized across, but they have a set type
            // when operating on scalars (e.g. s_len returns an array when acting on an array and a dict
            // when operating on a dict, but s_len always returns an int when acting on a single string)
            // non-vectorized functions always return the same type
            Dtype out_type = func.function.is_vectorized && ((x.type & Dtype.ITERABLE) != 0) ? x.type : func.function.type;
            List<JNode> all_args = new List<JNode>(func.args.Count);
            foreach (var a in func.args)
                all_args.Add(null);
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
                            for (int ii = 0; ii < other_args.Count; ii++)
                            {
                                JNode other_arg = other_args[ii];
                                all_args[ii + 1] = other_arg is CurJson ? ((CurJson)other_arg).function(inp) : other_arg;
                            }
                            if (itbl.type == Dtype.OBJ)
                            {
                                var dic = new Dictionary<string, JNode>();
                                var otbl = (JObject)itbl;
                                foreach (string key in otbl.children.Keys)
                                {
                                    all_args[0] = otbl[key];
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
                        for (int ii = 0; ii < other_args.Count; ii++)
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
                                foreach (string key in otbl.children.Keys)
                                {
                                    all_args[0] = otbl[key];
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
                            for (int ii = 0; ii < other_args.Count; ii++)
                            {
                                JNode other_arg = other_args[ii];
                                all_args[ii + 1] = other_arg is CurJson ? ((CurJson)other_arg).function(inp) : other_arg;
                            }
                            foreach (string key in xobj.children.Keys)
                            {
                                all_args[0] = xobj[key];
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
                            for (int ii = 0; ii < other_args.Count; ii++)
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
                            for (int ii = 0; ii < other_args.Count; ii++)
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
                    for (int ii = 0; ii < other_args.Count; ii++)
                    {
                        JNode other_arg = other_args[ii];
                        all_args[ii + 1] = other_arg;
                    }
                    if (x.type == Dtype.OBJ)
                    {
                        var xobj = (JObject)x;
                        var dic = new Dictionary<string, JNode>();
                        foreach (string key in xobj.children.Keys)
                        {
                            all_args[0] = xobj[key];
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
                            for (int ii = 0; ii < other_args.Count; ii++)
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
                        for (int ii = 0; ii < other_args.Count; ii++)
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
                        for (int ii = 0; ii < other_args.Count; ii++)
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
                for (int ii = 0; ii < other_args.Count; ii++)
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

        private static object PeekNextToken(List<object> toks, int pos)
        {
            if (pos + 1 >= toks.Count) { return null; }
            return toks[pos + 1];
        }

        private Obj_Pos ParseSlicer(List<object> toks, int pos, int? first_num)
        {
            var slicer = new int?[3];
            int slots_filled = 0;
            int? last_num = first_num;
            while (pos < toks.Count)
            {
                object t = toks[pos];
                if (t is char)
                {
                    char tval = (char)t;
                    if (tval == ':')
                    {
                        slicer[slots_filled++] = last_num;
                        last_num = null;
                        pos++;
                        continue;
                    }
                    else if (EXPR_FUNC_ENDERS.Contains(tval))
                    {
                        break;
                    }
                }
                try
                {
                    Obj_Pos npo = ParseExprOrScalarFunc(toks, pos);
                    JNode numtok = (JNode)npo.obj;
                    pos = npo.pos;
                    if (numtok.type != Dtype.INT)
                    {
                        throw new ArgumentException();
                    }
                    last_num = Convert.ToInt32(numtok.value);
                }
                catch (Exception)
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
            return new Obj_Pos(new JSlicer(slicer), pos);
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

        private Obj_Pos ParseIndexer(List<object> toks, int pos)
        {
            object t = toks[pos];
            object nt;
            if (!(t is char))
            {
                throw new RemesPathException("Expected delimiter at the start of indexer");
            }
            char d = (char)t;
            List<object> children = new List<object>();
            if (d == '.')
            {
                nt = PeekNextToken(toks, pos);
                if (nt != null)
                {
                    if (nt is Binop && ((Binop)nt).name == "*")
                    {
                        // it's a '*' indexer, which means select all keys/indices
                        return new Obj_Pos(new StarIndexer(), pos + 2);
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
                    return new Obj_Pos(new VarnameList(children), pos + 2);
                }
            }
            else if (d == '{')
            {
                return ParseProjection(toks, pos+1);
            }
            else if (d != '[')
            {
                throw new RemesPathException("Indexer must start with '.' or '[' or '{'");
            }
            Indexer indexer = null;
            object last_tok = null;
            JNode jlast_tok;
            Dtype last_type = Dtype.UNKNOWN;
            t = toks[++pos];
            if (t is Binop && ((Binop)t).name == "*")
            {
                // it was '*', indicating a star indexer
                nt = PeekNextToken(toks, pos);
                if (nt is char && (char)nt  == ']')
                {
                    return new Obj_Pos(new StarIndexer(), pos + 2);
                }
                throw new RemesPathException("Unacceptable first token '*' for indexer list");
            }
            while (pos < toks.Count)
            {
                t = toks[pos];
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
                        return new Obj_Pos(indexer, pos + 1);
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
                        pos++;
                    }
                    else if (d == ':')
                    {
                        if (last_tok == null)
                        {
                            Obj_Pos opo = ParseSlicer(toks, pos, null);
                            last_tok = opo.obj;
                            pos = opo.pos;
                        }
                        else if (last_tok is JNode)
                        {
                            jlast_tok = (JNode)last_tok;
                            if (jlast_tok.type != Dtype.INT)
                            {
                                throw new RemesPathException($"Expected token other than ':' after {jlast_tok} " +
                                                             $"in an indexer");
                            }
                            Obj_Pos opo = ParseSlicer(toks, pos, Convert.ToInt32(jlast_tok.value));
                            last_tok = opo.obj;
                            pos = opo.pos;
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
                    Obj_Pos opo = ParseExprOrScalarFunc(toks, pos);
                    last_tok = opo.obj;
                    pos = opo.pos;
                    last_type = ((JNode)last_tok).type;
                }
            }
            throw new RemesPathException("Unterminated indexer");
        }

        private Obj_Pos ParseExprOrScalar(List<object> toks, int pos)
        {
            if (toks.Count == 0)
            {
                throw new RemesPathException("Empty query");
            }
            object t = toks[pos];
            JNode last_tok = null;
            if (t is Binop)
            {
                throw new RemesPathException($"Binop {(Binop)t} without appropriate left operand");
            }
            if (t is char)
            {
                char d = (char)t;
                if (d != '(')
                {
                    throw new RemesPathException($"Invalid token {d} at position {pos}");
                }
                int unclosed_parens = 1;
                List<object> subquery = new List<object>();
                for (int end = pos + 1; end < toks.Count; end++)
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
                                last_tok = (JNode)ParseExprOrScalarFunc(subquery, 0).obj;
                                pos = end + 1;
                                break;
                            }
                        }
                    }
                    subquery.Add(subtok);
                }
            }
            else if (t is ArgFunction)
            {
                Obj_Pos opo = ParseArgFunction(toks, pos+1, (ArgFunction)t);
                last_tok = (JNode)opo.obj;
                pos = opo.pos;
            }
            else
            {
                last_tok = (JNode)t;
                pos++;
            }
            if (last_tok == null)
            {
                throw new RemesPathException("Found null where JNode expected");
            }
            if ((last_tok.type & Dtype.ITERABLE) != 0)
            {
                // the last token is an iterable, so now we look for indexers that slice it
                var idxrs = new List<IndexerFunc>();
                object nt = PeekNextToken(toks, pos - 1);
                object nt2, nt3;
                while (nt != null && nt is char && INDEXER_STARTERS.Contains((char)nt))
                {
                    nt2 = PeekNextToken(toks, pos);
                    bool is_recursive = false;
                    if (nt2 is char && (char)nt2 == '.' && (char)nt == '.')
                    {
                        is_recursive = true;
                        nt3 = PeekNextToken(toks, pos + 1);
                        pos += (nt3 is char && (char)nt3 == '[') ? 2 : 1;
                    }
                    Obj_Pos opo= ParseIndexer(toks, pos);
                    Indexer cur_idxr = (Indexer)opo.obj;
                    pos = opo.pos;
                    nt = PeekNextToken(toks, pos - 1);
                    bool is_varname_list = cur_idxr is VarnameList;
                    bool is_dict = is_varname_list & !is_recursive;
                    bool has_one_option = false;
                    bool is_projection = false;
                    if (is_varname_list || cur_idxr is SlicerList)
                    {
                        List<object> children = null;
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
                        Func<JNode, IEnumerable<Key_Node>> idx_func = ApplyMultiIndex(children, is_varname_list, is_recursive);
                        idxrs.Add(new IndexerFunc(idx_func, has_one_option, is_projection, is_dict, is_recursive));
                    }
                    else if (cur_idxr is BooleanIndex)
                    {
                        object boodex_fun = ((BooleanIndex)cur_idxr).value;
                        Func<JNode, IEnumerable<Key_Node>> idx_func = ApplyBooleanIndex((JNode)boodex_fun);
                        idxrs.Add(new IndexerFunc(idx_func, has_one_option, is_projection, is_dict, is_recursive));
                    }
                    else if (cur_idxr is Projection)
                    {
                        Func<JNode, IEnumerable<Key_Node>> proj_func = ((Projection)cur_idxr).proj_func;
                        idxrs.Add(new IndexerFunc(proj_func, false, true, false, false));
                    }
                    else
                    {
                        // it's a star indexer
                        if (is_recursive)
                            idxrs.Add(new IndexerFunc(RecursivelyFlattenIterable, false, false, false, true));
                        else
                            idxrs.Add(new IndexerFunc(ApplyStarIndexer, has_one_option, is_projection, is_dict, false));
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
                        return new Obj_Pos(new CurJson(lcur.type, idx_func), pos);
                    }
                    if (last_tok is JObject)
                    {
                        return new Obj_Pos(ApplyIndexerList(idxrs)((JObject)last_tok), pos);
                    }
                    return new Obj_Pos(ApplyIndexerList(idxrs)((JArray)last_tok), pos);
                }
            }
            return new Obj_Pos(last_tok, pos);
        }

        private Obj_Pos ParseExprOrScalarFunc(List<object> toks, int pos)
        {
            object curtok = null;
            object nt = PeekNextToken(toks, pos);
            // most common case is a single JNode followed by the end of the query or an expr func ender
            // e.g., in @[0,1,2], all of 0, 1, and 2 are immediately followed by an expr func ender
            // and in @.foo.bar the bar is followed by EOF
            // MAKE THE COMMON CASE FAST!
            if (nt == null || (nt is char && EXPR_FUNC_ENDERS.Contains((char)nt)))
            {
                curtok = toks[pos];
                if (!(curtok is JNode))
                {
                    throw new RemesPathException($"Invalid token {curtok} where JNode expected");
                }
                return new Obj_Pos((JNode)curtok, pos + 1);
            }
            bool uminus = false;
            object left_tok = null;
            object left_operand = null;
            float left_precedence = float.MinValue;
            BinopWithArgs root = null;
            BinopWithArgs leaf = null;
            object[] children = new object[2];
            Binop func;
            while (pos < toks.Count)
            {
                left_tok = curtok;
                curtok = toks[pos];
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
                    pos++;
                }
                else
                {
                    if (left_tok != null && !(left_tok is Binop))
                    {
                        throw new RemesPathException("Can't have two iterables or scalars unseparated by a binop");
                    }
                    Obj_Pos opo = ParseExprOrScalar(toks, pos);
                    left_operand = opo.obj;
                    pos = opo.pos;
                    if (uminus)
                    {
                        nt = PeekNextToken(toks, pos - 1);
                        if (!(nt != null && nt is Binop && ((Binop)nt).name == "**"))
                        {
                            // applying unary minus to this expr/scalar has higher precedence than everything except
                            // exponentiation.
                            List<JNode> args = new List<JNode> { (JNode)left_operand };
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
            return new Obj_Pos((JNode)left_operand, pos);
        }

        private Obj_Pos ParseArgFunction(List<object> toks, int pos, ArgFunction fun)
        {
            object t = toks[pos];
            if (!(t is char && (char)t == '('))
            {
                throw new RemesPathException($"Function {fun.name} must have parens surrounding arguments");
            }
            pos++;
            int arg_num = 0;
            Dtype[] intypes = fun.input_types();
            List<JNode> args = new List<JNode>(fun.min_args);
            JNode cur_arg = null;
            while (pos < toks.Count)
            {
                t = toks[pos];
                // the last Dtype in an ArgFunction's input_types is either the type options for the last arg
                // or the type options for every optional arg (if the function can have infinitely many args)
                Dtype type_options = arg_num >= intypes.Length ? intypes[intypes.Length - 1] : intypes[arg_num];
                try
                {
                    try
                    {
                        Obj_Pos opo = ParseExprOrScalarFunc(toks, pos);
                        cur_arg = (JNode)opo.obj;
                        pos = opo.pos;
                    }
                    catch
                    {
                        cur_arg = null;
                    }
                    if ((Dtype.SLICE & type_options) != 0)
                    {
                        object nt = PeekNextToken(toks, pos - 1);
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
                            Obj_Pos opo = ParseSlicer(toks, pos, first_num);
                            cur_arg = (JNode)opo.obj;
                            pos = opo.pos;
                        }
                    }
                    if (cur_arg == null || (cur_arg.type & type_options) == 0)
                    {
                        Dtype arg_type = (cur_arg) == null ? Dtype.NULL : cur_arg.type;
                        throw new RemesPathArgumentException($"got type {JNode.FormatDtype(arg_type)}", arg_num, fun);
                    }
                }
                catch (Exception ex)
                {
                    if (ex is RemesPathArgumentException) throw;
                    throw new RemesPathArgumentException($"threw exception {ex}.", arg_num, fun);
                }
                t = toks[pos];
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
                    if (fun.min_args == fun.max_args)
                        throw new RemesPathException($"Expected ',' after argument {arg_num} of function {fun.name} ({fun.max_args} args)");
                    throw new RemesPathException($"Expected ',' after argument {arg_num} of function {fun.name} " +
                                                 $"({fun.min_args} - {fun.max_args} args)");
                }
                if (arg_num + 1 == fun.max_args && !close_paren)
                {
                    if (fun.min_args == fun.max_args)
                        throw new RemesPathException($"Expected ')' after argument {arg_num} of function {fun.name} ({fun.max_args} args)");
                    throw new RemesPathException($"Expected ')' after argument {arg_num} of function {fun.name} " +
                                                 $"({fun.min_args} - {fun.max_args} args)");
                }
                args.Add(cur_arg);
                arg_num++;
                pos++;
                if (close_paren)
                {
                    var withargs = new ArgFunctionWithArgs(fun, args);
                    if (fun.max_args < Int32.MaxValue)
                    {
                        // for functions that have a fixed number of optional args, pad the unfilled args with null nodes
                        for (int arg2 = arg_num; arg2 < fun.max_args; arg2++)
                        {
                            args.Add(new JNode());
                        }
                    }
                    return new Obj_Pos(ApplyArgFunction(withargs), pos);
                }
            }
            if (fun.min_args == fun.max_args)
                throw new RemesPathException($"Expected ')' after argument {arg_num} of function {fun.name} ({fun.max_args} args)");
            throw new RemesPathException($"Expected ')' after argument {arg_num} of function {fun.name} "
                                         + $"({fun.min_args} - {fun.max_args} args)");
        }

        private Obj_Pos ParseProjection(List<object> toks, int pos)
        {
            var children = new List<Key_Node>();
            bool is_object_proj = false;
            while (pos < toks.Count)
            {
                Obj_Pos opo = ParseExprOrScalarFunc(toks, pos);
                JNode key = (JNode)opo.obj;
                pos = opo.pos;
                object nt = PeekNextToken(toks, pos - 1);
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
                            opo = ParseExprOrScalarFunc(toks, pos + 1);
                            JNode val = (JNode)opo.obj;
                            pos = opo.pos;
                            children.Add(new Key_Node((string)key.value, val));
                            is_object_proj = true;
                            nt = PeekNextToken(toks, pos - 1);
                            if (!(nt is char))
                            {
                                throw new RemesPathException("Key-value pairs in projection must be delimited by ',' and projections must end with '}'.");
                            }
                            nd = (char)nt;
                        }
                        else
                        {
                            throw new RemesPathException($"Object projection keys must be string, not {JNode.FormatDtype(key.type)}");
                        }
                    }
                    else
                    {
                        // it's an array projection
                        children.Add(new Key_Node(0, key));
                    }
                    if (nd == '}')
                    {
                        IEnumerable<Key_Node> proj_func(JNode obj)
                        {
                            foreach(Key_Node kv in children)
                            {
                                object k = kv.obj;
                                JNode v = kv.node;
                                yield return new Key_Node(k, (v is CurJson) ? ((CurJson)v).function(obj) : v); 
                            }
                        }
                        return new Obj_Pos(new Projection(proj_func), pos + 1);
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
                pos++;
            }
            throw new RemesPathException("Unterminated projection");
        }
        #endregion
        #region ASSIGNMENT_EXPRESSIONS

        /// <summary>
        /// Changes the type and value of v to the type and value of vnew.<br></br>
        /// Cannot mutate an object or array.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="vnew"></param>
        /// <exception cref="RemesPathException"></exception>
        private static void TransferJNodeProperties(JNode v, JNode vnew)
        {
            if (v is JArray)
            {
                throw new RemesPathException("Can't mutate an array.");
            }
            if (v is JObject)
            {
                throw new RemesPathException("Can't mutate an object.");
            }
            // v is a scalar
            if (vnew is JArray || vnew is JObject)
                throw new RemesPathException("Can't convert a scalar to an array or object.");
            v.type = vnew.type;
            v.value = vnew.value;
        }

        /// <summary>
        /// An assignment expression is a valid RemesPath expression that contains the token '='.<br></br>
        /// Assignment expressions are vectorized,<br></br>
        /// so @ = @ * 2 will:<br></br>
        /// * change [1, 2, 3] to [2, 4, 6]<br></br>
        /// * change {"a": 1.5, "b": -4.6} to {"a": 3.0, "b": -9.2}<br></br>
        /// * change 2 to 4
        /// </summary>
        /// <param name="x"></param>
        /// <param name="op"></param>
        private JNode ApplyAssignmentExpression(JNode x, JNode op)
        {
            if (x is CurJson)
            {
                CurJson cjx = (CurJson)x;
                Func<JNode, JNode> outfunc = (JNode inp) =>
                {
                    ApplyAssignmentExpression(cjx.function(inp), op);
                    return inp;
                };
                return new CurJson(Dtype.UNKNOWN, outfunc);
            }
            if (op is CurJson cjop)
            {
                var func = cjop.function;
                if (x is JObject)
                {
                    foreach (JNode v in ((JObject)x).children.Values)
                    {
                        TransferJNodeProperties(v, func(v));
                    }
                }
                else if (x is JArray)
                {
                    foreach (JNode v in ((JArray)x).children)
                    {
                        TransferJNodeProperties(v, func(v));
                    }
                }
                else // x is a scalar
                {
                    JNode xnew = func(x);
                    if (xnew is JArray || xnew is JObject)
                        throw new RemesPathException("Can't convert a scalar to an array or object");
                    x.type = xnew.type;
                    x.value = xnew.value;
                }
                return x;
            }
            // op is an unchanging value, so just perform the same change x or all children of x
            if (x is JObject xobj)
            {
                foreach (JNode v in xobj.children.Values)
                {
                    TransferJNodeProperties(v, op);
                }
            }
            else if (x is JArray xarr)
            {
                foreach (JNode v in xarr.children)
                {
                    TransferJNodeProperties(v, op);
                }
            }
            else
            {
                // x is a scalar
                if (op is JArray || op is JObject)
                    throw new RemesPathException("Can't convert a scalar to an array or object");
                x.type = op.type;
                x.value = op.value;
            }
            return x;
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
            if (ex is RemesLexerException rle)
            {
                return rle.ToString();
            }
            if (ex is JsonParserException jpe)
            {
                return jpe.ToString();
            }
            if (ex is RemesPathArgumentException rpae)
            {
                return rpae.ToString();
            }
            if (ex is RemesPathException rpe)
            {
                return rpe.ToString();
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
}
