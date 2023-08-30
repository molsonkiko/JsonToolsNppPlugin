/*
A query language for JSON. 
*/
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using JSON_Tools.Utils;

namespace JSON_Tools.JSON_Tools
{
    #region DATA_HOLDER_STRUCTS
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
        public Func<JNode, IEnumerable<object>> proj_func;

        public Projection(Func<JNode, IEnumerable<object>> proj_func)
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

    public class IndexerFunc
    {
        /// <summary>
        /// An enumerator that yields JNodes from a JArray or JObject
        /// </summary>
        public Func<JNode, IEnumerable<object>> idxr;
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

        public IndexerFunc(Func<JNode, IEnumerable<object>> idxr, bool has_one_option, bool is_projection, bool is_dict, bool is_recursive)
        {
            this.idxr = idxr;
            this.has_one_option = has_one_option;
            this.is_projection = is_projection;
            this.is_dict = is_dict;
            this.is_recursive = is_recursive;
        }

        /// <summary>
        /// Unlike other indexers, boolean indices may simply return the original object
        /// (for example @[@.a &gt; 3] returns <i>an entire object</i> if the value with key "a" is greater than 3)<br></br>
        /// OR return individual elements of the original object (for example @[@ &gt; 3] returns <i>the elements of an object/array</i> that are greater than 3).<br></br>
        /// This is important because we want boolean indices to be easy to chain.<br></br>
        /// Because of this, ApplyBooleanIndex must be an instance method of the IndexerFunc class,
        /// so that it can set the has_one_option and is_dict attributes of this class.
        /// </summary>
        /// <param name="inds"></param>
        /// <returns></returns>
        /// <exception cref="VectorizedArithmeticException">if the boolean index has the wrong length or contains non-booleans</exception>
        public Func<JNode, IEnumerable<object>> ApplyBooleanIndex(JNode inds)
        {
            IEnumerable<object> bool_idxr_func(JNode x)
            {
                has_one_option = false;
                JNode newinds = (inds is CurJson cj)
                    ? cj.function(x)
                    : inds;
                is_dict = x is JObject;
                if (newinds.value is bool newibool)
                {
                    // to allow for boolean indices that filter on the entire object/array, like @.bar == @.foo or sum(@) == 0
                    if (newibool)
                    {
                        // boolean indices yield each individual element of an array
                        // but treat objects and scalars as atomic
                        // because this allows chaining of boolean indices in an intuitive way
                        // e.g., @[:][@.a < 2][@.b > 3].c[:][@ < 3]
                        if (x is JArray xarr)
                        {
                            for (int ii = 0; ii < xarr.Length; ii++)
                            {
                                yield return xarr[ii];
                            }
                        }
                        else
                        {
                            has_one_option = true;
                            yield return x;
                        }
                    }
                    // if the condition is false, yield nothing
                    yield break;
                }
                else if (newinds is JObject iobj)
                {
                    JObject xobj = (JObject)x;
                    if (iobj.Length != xobj.Length)
                    {
                        throw new VectorizedArithmeticException($"bool index length {iobj.Length} does not match object/array length {xobj.Length}.");
                    }
                    foreach (KeyValuePair<string, JNode> kv in xobj.children)
                    {
                        bool i_has_key = iobj.children.TryGetValue(kv.Key, out JNode ival);
                        if (i_has_key)
                        {
                            if (!(ival.value is bool ibool))
                            {
                                throw new VectorizedArithmeticException("bool index contains non-booleans");
                            }
                            if (ibool)
                            {
                                yield return kv;
                            }
                        }
                    }
                    yield break;
                }
                else if (newinds is JArray iarr)
                {
                    JArray xarr = (JArray)x;
                    if (iarr.Length != xarr.Length)
                    {
                        throw new VectorizedArithmeticException($"bool index length {iarr.Length} does not match object/array length {xarr.Length}.");
                    }
                    for (int ii = 0; ii < xarr.Length; ii++)
                    {
                        JNode ival = iarr[ii];
                        JNode xval = xarr[ii];
                        if (!(ival.value is bool ibool))
                        {
                            throw new VectorizedArithmeticException("bool index contains non-booleans");
                        }
                        if (ibool)
                        {
                            yield return xval;
                        }
                    }
                    yield break;
                }
            }
            return bool_idxr_func;
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
        /// <summary>0-based index</summary>
        public int arg_num { get; set; }
        public ArgFunction func { get; set; }

        public RemesPathArgumentException(string description, int arg_index_0_based, ArgFunction func) : base(description)
        {
            this.arg_num = arg_index_0_based;
            this.func = func;
        }

        public override string ToString()
        {
            string fmt_dtype = JNode.FormatDtype(func.InputTypes()[arg_num]);
            return $"For argument {arg_num} of function {func.name}, expected {fmt_dtype}, instead {description}";
        }
    }

    public class InvalidMutationException : RemesPathException
    {
        public InvalidMutationException(string description) : base(description) { }

        public override string ToString() { return description; }
    }

    public class RemesPathIndexOutOfRangeException : RemesPathException
    {
        public int index;
        public JNode node;

        public RemesPathIndexOutOfRangeException(int index, JNode node) : base("")
        {
            this.index = index;
            this.node = node;
        }

        public override string ToString()
        {
            string typeName = JNode.DtypeStrings[node.type];
            return $"Index {index} out of range for {typeName} {node.ToString()}";
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

        /// <summary>
        /// A LRU cache mapping queries to compiled results that the parser can check against
        /// to save time on parsing.
        /// </summary>
        public LruCache<string, JNode> cache;

        /// <summary>
        /// The cache_capacity indicates how many queries to store in the old query cache.
        /// </summary>
        /// <param name="cache_capacity"></param>
        public RemesParser(int cache_capacity = 64)
        {
            cache = new LruCache<string, JNode>(cache_capacity);
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
        public JNode Compile(string query)
        {
            if (cache.TryGetValue(query, out JNode old_result))
                return old_result;
            List<object> toks = lexer.Tokenize(query);
            int indexOfAssignment = toks.IndexOf('=');
            if (indexOfAssignment == 0)
                throw new RemesPathException("Assignment with no LHS");
            if (indexOfAssignment == toks.Count - 1)
                throw new RemesPathException("Assignment with no RHS");
            if (indexOfAssignment > 0)
            {
                if (toks.Count(x => x is char c && c == '=') > 1)
                    throw new RemesPathException("Only one '=' assignment operator allowed in a query");
                JNode selector = (JNode)ParseExprOrScalarFunc(toks, 0, indexOfAssignment).obj;
                JNode mutator = (JNode)ParseExprOrScalarFunc(toks, indexOfAssignment + 1, toks.Count).obj;
                return new JMutator(selector, mutator);
            }
            var result = (JNode)ParseExprOrScalarFunc(toks, 0, toks.Count).obj;
            cache.SetDefault(query, result);
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
        public JNode Search(string query, JNode obj)
        {
            JNode compiled_query = Compile(query);
            if (compiled_query is CurJson cjres)
            {
                return cjres.function(obj);
            }
            else if (compiled_query is JMutator mut)
            {
                return mut.Mutate(obj);
            }
            return compiled_query;
        } 

        public const string EXPR_FUNC_ENDERS = "]:},)";
        // these tokens have high enough precedence to stop an expr_function or scalar_function
        public const string INDEXER_STARTERS = ".[{>";
        public const string PROJECTION_STARTERS = "{>";

        #region INDEXER_FUNCTIONS
        private Func<JNode, IEnumerable<object>> ApplyMultiIndex(object inds, bool is_varname_list, bool is_recursive = false)
        {
            if (inds is CurJson cj)
            {
                IEnumerable<object> multi_idx_func(JNode x)
                {
                    return ApplyMultiIndex(cj.function(x), is_varname_list, is_recursive)(x);
                }
                return multi_idx_func;
            }
            var children = (List<object>)inds;
            if (is_varname_list)
            {
                if (is_recursive)
                {
                    IEnumerable<object> multi_idx_func(JNode x, string path, HashSet<string> paths_visited)
                    {
                        if (x is JArray xarr)
                        {
                            // a varname list can only match dict keys, not array indices
                            // we'll just recursively search from each child of this array
                            for (int ii = 0; ii < xarr.Length; ii++)
                            {
                                foreach (object kv in multi_idx_func(xarr[ii], $"{path},{ii}", paths_visited))
                                {
                                    yield return kv;
                                }
                            }
                        }
                        else if (x is JObject xobj)
                        {
                            // yield each key or regex match in this dict
                            // recursively descend from each key that doesn't match
                            foreach (object v in children)
                            {
                                if (v is string strv)
                                {
                                    if (path.Length == 0) path = strv;
                                    foreach (KeyValuePair<string, JNode> kv in xobj.children)
                                    {
                                        string newpath = $"{path},{kv.Key}";
                                        if (kv.Key == strv)
                                        {
                                            if (!paths_visited.Contains(newpath))
                                                yield return kv.Value;
                                            paths_visited.Add(newpath);
                                        }
                                        else
                                        {
                                            foreach (object node in multi_idx_func(kv.Value, newpath, paths_visited))
                                                yield return node;
                                        }
                                    }
                                }
                                else // v is a regex
                                {
                                    Regex regv = (Regex)v;
                                    if (path.Length == 0) path = regv.ToString();
                                    foreach (KeyValuePair<string, JNode> kv in xobj.children)
                                    {
                                        string newpath = $"{path},{kv.Key}";
                                        if (regv.IsMatch(kv.Key))
                                        {
                                            if (!paths_visited.Contains(newpath))
                                                yield return kv.Value;
                                            paths_visited.Add(newpath);
                                        }
                                        else
                                        {
                                            foreach (object node in multi_idx_func(kv.Value, newpath, paths_visited))
                                            {
                                                yield return node;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    return x => multi_idx_func(x, "", new HashSet<string>());
                }
                else // not recursive
                {
                    IEnumerable<object> multi_idx_func(JNode x)
                    {
                        var xobj = (JObject)x;
                        foreach (object v in children)
                        {
                            if (v is string vstr)
                            {
                                if (xobj.children.TryGetValue(vstr, out JNode val))
                                {
                                    yield return new KeyValuePair<string, JNode>(vstr, val);
                                }
                            }
                            else
                            {
                                foreach (KeyValuePair<string, JNode> ono in ApplyRegexIndex(xobj, (Regex)v))
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
                IEnumerable<object> multi_idx_func(JNode x)
                {
                    JArray xarr = (JArray)x;
                    foreach (object ind in children)
                    {
                        if (ind is int?[] slicer)
                        {
                            // it's a slice, so yield all the JNodes in that slice
                            foreach (JNode subind in xarr.children.LazySlice(slicer))
                            {
                                yield return subind;
                            }
                        }
                        else
                        {
                            int ii = Convert.ToInt32(ind);
                            if (xarr.children.WrappedIndex(ii, out JNode atII))
                                yield return atII;
                        }
                    }
                }
                return multi_idx_func;
            }
        }

        private IEnumerable<KeyValuePair<string, JNode>> ApplyRegexIndex(JObject obj, Regex regex)
        {
            foreach (KeyValuePair<string, JNode> kv in obj.children)
            {
                if (regex.IsMatch(kv.Key))
                {
                    yield return kv;
                }
            }
        }

        private IEnumerable<object> ApplyStarIndexer(JNode x)
        {
            if (x is JObject xobj)
            {
                foreach (KeyValuePair<string, JNode> kv in xobj.children)
                {
                    yield return kv;
                }
                yield break;
            }
            var xarr = (JArray)x;
            for (int ii = 0; ii < xarr.Length; ii++)
            {
                yield return xarr[ii];
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
        private static IEnumerable<object> RecursivelyFlattenIterable(JNode node)
        {
            if (node is JObject obj)
            {
                foreach (JNode val in obj.children.Values)
                {
                    if ((val.type & Dtype.ITERABLE) == 0)
                    {
                        yield return val;
                    }
                    else
                    {
                        foreach (object child in RecursivelyFlattenIterable(val))
                            yield return child;
                    }
                }
            }
            else if (node is JArray arr)
            {
                foreach (JNode val in arr.children)
                {
                    if ((val.type & Dtype.ITERABLE) == 0)
                    {
                        yield return val;
                    }
                    else
                    {
                        foreach (object child in RecursivelyFlattenIterable(val))
                            yield return child;
                    }
                }
            }
        }

        private Func<JNode, JNode> ApplyIndexerList(List<IndexerFunc> indexers)
        {
            JNode idxr_list_func(JNode obj, List<IndexerFunc> idxrs, int ii)
            {
                IndexerFunc ix = idxrs[ii];
                var inds = ix.idxr(obj).GetEnumerator();
                // IEnumerator<T>.MoveNext returns a bool indicating if the enumerator has passed the end of the collection
                if (!inds.MoveNext())
                {
                    // the IndexerFunc couldn't find anything
                    if (ix.is_dict)
                    {
                        return new JObject();
                    }
                    return new JArray();
                }
                object current = inds.Current;
                bool is_dict = current is KeyValuePair<string, JNode>;
                List<JNode> arr;
                Dictionary<string, JNode> dic;
                if (ii == idxrs.Count - 1)
                {
                    if (ix.has_one_option)
                    {
                        // return a scalar rather than an iterable with one element
                        if (current is KeyValuePair<string, JNode> kv)
                            return kv.Value;
                        return (JNode)current;
                    }
                    if (is_dict)
                    {
                        var kv = (KeyValuePair<string, JNode>)current;
                        dic = new Dictionary<string, JNode>
                        {
                            [kv.Key] = kv.Value
                        };
                        while (inds.MoveNext())
                        {
                            kv = (KeyValuePair<string, JNode>)inds.Current;
                            dic[kv.Key] = kv.Value;
                        }
                        return new JObject(obj.position, dic);
                    }
                    arr = new List<JNode> { (JNode)current };
                    while (inds.MoveNext())
                    {
                        arr.Add((JNode)inds.Current);
                    }
                    return new JArray(obj.position, arr);
                }
                if (ix.is_projection)
                {
                    if (is_dict)
                    {
                        var kv = (KeyValuePair<string, JNode>)current;
                        dic = new Dictionary<string, JNode>
                        {
                            [kv.Key] = kv.Value
                        };
                        while (inds.MoveNext())
                        {
                            kv = (KeyValuePair<string, JNode>)inds.Current;
                            dic[kv.Key] = kv.Value;
                        }
                        // recursively search this projection using the remaining indexers
                        return idxr_list_func(new JObject(0, dic), idxrs, ii + 1);
                    }
                    else if (ix.has_one_option)
                    {
                        return idxr_list_func((JNode)inds.Current, idxrs, ii + 1);
                    }
                    arr = new List<JNode> { (JNode)current };
                    while (inds.MoveNext())
                    {
                        arr.Add((JNode)inds.Current);
                    }
                    return idxr_list_func(new JArray(0, arr), idxrs, ii + 1);
                }
                JNode v1_subdex;
                if (current is JNode node)
                {
                    v1_subdex = idxr_list_func(node, idxrs, ii + 1);
                }
                else
                {
                    node = ((KeyValuePair<string, JNode>)current).Value;
                    v1_subdex = idxr_list_func(node, idxrs, ii + 1);
                }
                if (ix.has_one_option)
                {
                    return v1_subdex;
                }
                int is_empty = Binop.ObjectOrArrayEmpty(v1_subdex);
                if (is_dict)
                {
                    var kv = (KeyValuePair<string, JNode>)current;
                    dic = new Dictionary<string, JNode>();
                    if (is_empty != 1)
                    {
                        dic[kv.Key] = v1_subdex;
                    }
                    while (inds.MoveNext())
                    {
                        kv = (KeyValuePair<string, JNode>)inds.Current;
                        JNode subdex = idxr_list_func(kv.Value, idxrs, ii + 1);
                        is_empty = Binop.ObjectOrArrayEmpty(subdex);
                        if (is_empty != 1)
                        {
                            dic[kv.Key] = subdex;
                        }
                    }
                    return new JObject(obj.position, dic);
                }
                // obj is a list iterator
                arr = new List<JNode>();
                if (is_empty != 1)
                {
                    arr.Add(v1_subdex);
                }
                while (inds.MoveNext())
                {
                    var v = (JNode)inds.Current;
                    JNode subdex = idxr_list_func(v, idxrs, ii + 1);
                    is_empty = Binop.ObjectOrArrayEmpty(subdex);
                    if (is_empty != 1)
                    {
                        arr.Add(subdex);
                    }
                }
                return new JArray(obj.position, arr);
            }
            return (JNode obj) => idxr_list_func(obj, indexers, 0);
        }

        #endregion
        #region APPLY_ARG_FUNCTION
        private JNode ApplyArgFunction(ArgFunctionWithArgs func)
        {
            if (func.function.maxArgs == 0)
            {
                // paramterless function like rand()
                if (!func.function.isDeterministic)
                    return new CurJson(func.function.type, blah => func.function.Call(func.args));
                return func.function.Call(func.args);
            }
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
            Dtype out_type = func.function.isVectorized && ((x.type & Dtype.ITERABLE) != 0) ? x.type : func.function.type;
            List<JNode> all_args = new List<JNode>(func.args.Count);
            foreach (var a in func.args)
                all_args.Add(null);
            if (func.function.isVectorized)
            {
                if (x is CurJson xcur)
                {
                    if (other_callables)
                    {
                        // x is a function of the current JSON, as is at least one other argument
                        JNode arg_outfunc(JNode inp)
                        {
                            var itbl = xcur.function(inp);
                            for (int ii = 0; ii < other_args.Count; ii++)
                            {
                                JNode other_arg = other_args[ii];
                                all_args[ii + 1] = other_arg is CurJson cjoa ? cjoa.function(inp) : other_arg;
                            }
                            if (itbl is JObject otbl)
                            {
                                var dic = new Dictionary<string, JNode>(otbl.Length);
                                foreach (KeyValuePair<string, JNode> okv in otbl.children)
                                {
                                    dic[okv.Key] = func.function.Call(all_args);
                                }
                                return new JObject(0, dic);
                            }
                            else if (itbl is JArray atbl)
                            {
                                var arr = new List<JNode>();
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
                            if (itbl is JObject otbl)
                            {
                                var dic = new Dictionary<string, JNode>(otbl.Length);
                                foreach (KeyValuePair<string, JNode> okv in otbl.children)
                                {
                                    all_args[0] = okv.Value;
                                    dic[okv.Key] = func.function.Call(all_args);
                                }
                                return new JObject(0, dic);
                            }
                            else if (itbl is JArray atbl)
                            {
                                var arr = new List<JNode>();
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
                            var dic = new Dictionary<string, JNode>(xobj.Length);
                            for (int ii = 0; ii < other_args.Count; ii++)
                            {
                                JNode other_arg = other_args[ii];
                                all_args[ii + 1] = other_arg is CurJson cjoa ? cjoa.function(inp) : other_arg;
                            }
                            foreach (KeyValuePair<string, JNode> xkv in xobj.children)
                            {
                                all_args[0] = xkv.Value;
                                dic[xkv.Key] = func.function.Call(all_args);
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
                                all_args[ii + 1] = other_arg is CurJson cjoa ? cjoa.function(inp) : other_arg;
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
                                all_args[ii + 1] = other_arg is CurJson cjoa ? cjoa.function(inp) : other_arg;
                            }
                            all_args[0] = x;
                            return func.function.Call(all_args);
                        }
                        return new CurJson(out_type, arg_outfunc);
                    }
                }
                else
                {
                    if (!func.function.isDeterministic)
                        return new CurJson(func.function.type, blah => CallVectorizedArgFuncWithArgs(x, other_args, all_args, func.function));
                    return CallVectorizedArgFuncWithArgs(x, other_args, all_args, func.function);
                }
            }
            else
            {
                // this is NOT a vectorized arg function (it's something like len or mean)
                if (x is CurJson xcur)
                {
                    if (other_callables)
                    {
                        JNode arg_outfunc(JNode inp)
                        {
                            for (int ii = 0; ii < other_args.Count; ii++)
                            {
                                JNode other_arg = other_args[ii];
                                all_args[ii + 1] = other_arg is CurJson cjoa ? cjoa.function(inp) : other_arg;
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
                            all_args[ii + 1] = other_arg is CurJson cjoa ? cjoa.function(inp) : other_arg;
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
                if (!func.function.isDeterministic)
                    return new CurJson(func.function.type, blah => func.function.Call(all_args));
                return func.function.Call(all_args);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="other_args"></param>
        /// <param name="all_args"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        private static JNode CallVectorizedArgFuncWithArgs(JNode x, List<JNode> other_args, List<JNode> all_args, ArgFunction func)
        {
            // none of the arguments are functions of the current JSON
            for (int ii = 0; ii < other_args.Count; ii++)
            {
                JNode other_arg = other_args[ii];
                all_args[ii + 1] = other_arg;
            }
            if (x is JObject xobj)
            {
                var dic = new Dictionary<string, JNode>(xobj.Length);
                foreach (KeyValuePair<string, JNode> xkv in xobj.children)
                {
                    all_args[0] = xobj[xkv.Key];
                    dic[xkv.Key] = func.Call(all_args);
                }
                return new JObject(0, dic);
            }
            else if (x is JArray xarr)
            {
                var arr = new List<JNode>(xarr.Length);
                foreach (JNode val in xarr.children)
                {
                    all_args[0] = val;
                    arr.Add(func.Call(all_args));
                }
                return new JArray(0, arr);
            }
            // x is not iterable, and no args are functions of the current JSON
            all_args[0] = x;
            return func.Call(all_args);
        }

        #endregion
        #region PARSER_FUNCTIONS

        private static object PeekNextToken(List<object> toks, int pos, int end)
        {
            if (pos + 1 >= end)
                return null;
            return toks[pos + 1];
        }

        private Obj_Pos ParseSlicer(List<object> toks, int pos, int? first_num, int end)
        {
            var slicer = new int?[3];
            int slots_filled = 0;
            int? last_num = first_num;
            while (pos < end)
            {
                object t = toks[pos];
                if (t is char tval)
                {
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
                    Obj_Pos npo = ParseExprOrScalarFunc(toks, pos, end);
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

        private Obj_Pos ParseIndexer(List<object> toks, int pos, int end)
        {
            object t = toks[pos];
            object nt;
            if (!(t is char d))
            {
                throw new RemesPathException("Expected delimiter at the start of indexer");
            }
            List<object> children = new List<object>();
            if (d == '.')
            {
                nt = PeekNextToken(toks, pos, end);
                if (nt != null)
                {
                    if (nt is Binop bnt && bnt.name == "*")
                    {
                        // it's a '*' indexer, which means select all keys/indices
                        return new Obj_Pos(new StarIndexer(), pos + 2);
                    }
                    JNode jnt = (nt is UnquotedString st)
                        ? new JNode(st.value, Dtype.STR, 0)
                        : (JNode)nt;
                    if ((jnt.type & Dtype.STR_OR_REGEX) == 0)
                    {
                        throw new RemesPathException("'.' syntax for indexers requires that the indexer be a string, " +
                                                    "regex, or '*'");
                    }
                    if (jnt is JRegex jnreg)
                    {
                        children.Add(jnreg.regex);
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
                return ParseProjection(toks, pos+1, end);
            }
            else if (d == '>') // "->" map operator
            {
                return ParseMap(toks, pos + 1, end);
            }
            else if (d != '[')
            {
                throw new RemesPathException("Indexer must start with '.', '[', '{', or \"->\"");
            }
            Indexer indexer = null;
            object last_tok = null;
            JNode jlast_tok;
            Dtype last_type = Dtype.UNKNOWN;
            t = toks[++pos];
            if (t is Binop b && b.name == "*")
            {
                // it was '*', indicating a star indexer
                nt = PeekNextToken(toks, pos, end);
                if (nt is char nd && nd  == ']')
                {
                    return new Obj_Pos(new StarIndexer(), pos + 2);
                }
                throw new RemesPathException("Unacceptable first token '*' for indexer list");
            }
            while (pos < end)
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
                            Obj_Pos opo = ParseSlicer(toks, pos, null, end);
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
                            Obj_Pos opo = ParseSlicer(toks, pos, Convert.ToInt32(jlast_tok.value), end);
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
                    Obj_Pos opo = ParseExprOrScalarFunc(toks, pos, end);
                    last_tok = opo.obj;
                    pos = opo.pos;
                    last_type = ((JNode)last_tok).type;
                }
            }
            throw new RemesPathException("Unterminated indexer");
        }

        private Obj_Pos ParseExprOrScalar(List<object> toks, int pos, int end)
        {
            if (toks.Count == 0)
            {
                throw new RemesPathException("Empty query");
            }
            object t = toks[pos];
            JNode last_tok = null;
            if (t is Binop b)
            {
                throw new RemesPathException($"Binop {b} without appropriate left operand");
            }
            if (t is char delim)
            {
                if (delim != '(')
                {
                    throw new RemesPathException($"Invalid token {delim} at position {pos}");
                }
                int unclosed_parens = 1;
                int subqueryStart = pos + 1;
                for (int subqueryEnd = subqueryStart; subqueryEnd < end; subqueryEnd++)
                {
                    object subtok = toks[subqueryEnd];
                    if (subtok is char subd)
                    {
                        if (subd == '(')
                        {
                            unclosed_parens++;
                        }
                        else if (subd == ')')
                        {
                            if (--unclosed_parens == 0)
                            {
                                last_tok = (JNode)ParseExprOrScalarFunc(toks, subqueryStart, subqueryEnd).obj;
                                pos = subqueryEnd + 1;
                                break;
                            }
                        }
                    }
                }
            }
            else if (t is UnquotedString st)
            {
                if (pos < end - 1
                    && toks[pos + 1] is char c && c == '(')
                {
                    // an unquoted string followed by an open paren
                    // *might* be an ArgFunction; we need to check
                    if (ArgFunction.FUNCTIONS.TryGetValue(st.value, out ArgFunction af))
                    {
                        Obj_Pos opo = ParseArgFunction(toks, pos + 1, af, end);
                        last_tok = (JNode)opo.obj;
                        pos = opo.pos;
                    }
                    else
                    {
                        throw new RemesPathException($"'{st.value}' is not the name of a RemesPath function.");
                    }
                }
                else // unquoted string just being used as a string
                {
                    last_tok = new JNode(st.value, Dtype.STR, 0);
                    pos++;
                }
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
            object nt = PeekNextToken(toks, pos - 1, end);
            if ((last_tok.type & Dtype.ITERABLE) != 0 || (nt is char && PROJECTION_STARTERS.Contains((char)nt)))
            {
                // The last token is an iterable (in which case various indexers are allowed)
                // or the next token is the start of a projection (unlike other indexers, projections can operate on scalars too)
                var idxrs = new List<IndexerFunc>();
                object nt2, nt3;
                while (nt != null && nt is char nd && INDEXER_STARTERS.Contains(nd))
                {
                    nt2 = PeekNextToken(toks, pos, end);
                    bool is_recursive = false;
                    if (nt2 is char nd2 && nd2 == '.' && nd == '.')
                    {
                        is_recursive = true;
                        nt3 = PeekNextToken(toks, pos + 1, end);
                        pos += (nt3 is char nd3 && nd3 == '[') ? 2 : 1;
                    }
                    Obj_Pos opo = ParseIndexer(toks, pos, end);
                    Indexer cur_idxr = (Indexer)opo.obj;
                    pos = opo.pos;
                    nt = PeekNextToken(toks, pos - 1, end);
                    bool is_varname_list = cur_idxr is VarnameList;
                    bool is_dict = is_varname_list & !is_recursive;
                    bool has_one_option = nd == '>'; // Some slicer/varname lists return one item, but the map item always does
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
                        Func<JNode, IEnumerable<object>> idx_func = ApplyMultiIndex(children, is_varname_list, is_recursive);
                        idxrs.Add(new IndexerFunc(idx_func, has_one_option, is_projection, is_dict, is_recursive));
                    }
                    else if (cur_idxr is BooleanIndex boodex)
                    {
                        JNode boodex_fun = (JNode)boodex.value;
                        var idxr = new IndexerFunc(null, has_one_option, is_projection, is_dict, is_recursive);
                        idxr.idxr = idxr.ApplyBooleanIndex(boodex_fun);
                        idxrs.Add(idxr);
                    }
                    else if (cur_idxr is Projection proj)
                    {
                        Func<JNode, IEnumerable<object>> proj_func = proj.proj_func;
                        idxrs.Add(new IndexerFunc(proj_func, has_one_option, true, false, false));
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
                    Func<JNode, JNode> idxrs_func = ApplyIndexerList(idxrs);
                    if (last_tok is CurJson lcur)
                    {
                        JNode idx_func(JNode inp)
                        {
                            return idxrs_func(lcur.function(inp));
                        }
                        return new Obj_Pos(new CurJson(lcur.type, idx_func), pos);
                    }
                    if (last_tok is JObject last_obj)
                    {
                        return new Obj_Pos(idxrs_func(last_obj), pos);
                    }
                    return new Obj_Pos(idxrs_func((JArray)last_tok), pos);
                }
            }
            return new Obj_Pos(last_tok, pos);
        }

        private Obj_Pos ParseExprOrScalarFunc(List<object> toks, int pos, int end)
        {
            object curtok = null;
            object nt = PeekNextToken(toks, pos, end);
            // most common case is a single JNode followed by the end of the query or an expr func ender
            // e.g., in @[0,1,2], all of 0, 1, and 2 are immediately followed by an expr func ender
            // and in @.foo.bar the bar is followed by EOF
            // MAKE THE COMMON CASE FAST!
            if (nt == null || (nt is char nd && EXPR_FUNC_ENDERS.Contains(nd)))
            {
                curtok = toks[pos];
                if (curtok is UnquotedString uqs)
                {
                    curtok = new JNode(uqs.value, Dtype.STR, 0);
                }
                if (!(curtok is JNode))
                {
                    throw new RemesPathException($"Invalid token {curtok} where JNode expected");
                }
                return new Obj_Pos((JNode)curtok, pos + 1);
            }
            object leftTok = null;
            object leftOperand = null;
            var unopStack = new List<UnaryOp>();
            UnaryOp unop;
            var bwaStack = new List<BinopWithArgs>();
            var argStack = new List<object>();
            while (pos < end)
            {
                leftTok = curtok;
                curtok = toks[pos];
                if (curtok is char curd && EXPR_FUNC_ENDERS.Contains(curd))
                {
                    if (leftTok == null)
                    {
                        throw new RemesPathException("No expression found where scalar expected");
                    }
                    break;
                }
                if (curtok is Binop bop)
                {
                    if (!(leftTok is JNode))
                    {
                        // no left operand for binop, so maybe the current binop is actually a unary operator with same name
                        if (UnaryOp.UNARY_OPS.TryGetValue(bop.name, out unop))
                            unop.AddToStack(unopStack);
                        else
                            throw new RemesPathException($"Binop {bop.name} with invalid left operand");
                    }
                    else
                    {
                        List<JNode> leftOperandTransformations = null;
                        List<Binop> binopTransformations = null;
                        if (unopStack.Count > 0)
                        {
                            if (!(leftTok is JNode leftNode))
                                throw new RemesPathException($"Binop has non-JNode operand {leftTok}");
                            leftOperandTransformations = new List<JNode>{ leftNode };
                            binopTransformations = new List<Binop> { bop };
                        }
                        while (unopStack.Count > 0)
                        {
                            unop = unopStack.Pop();
                            if (bop.PrecedesLeft(unop.precedence))
                            {
                                // unop has lower precedence than binop, so will be applied to the result of the binop's evaluation
                                var oldBop = binopTransformations.Last();
                                Func<JNode, JNode, JNode> oldBopCall = oldBop.Call;
                                Func<JNode, JNode> unopCall = unop.Call;
                                binopTransformations.Add(new Binop(
                                    (a, b) => unopCall(oldBopCall(a, b)),
                                    oldBop.precedence, oldBop.name, oldBop.is_right_associative));
                            }
                            else
                            {
                                // unop called on left operand because it precedes current binop
                                // binop will now act on unop'd left operand; pop unop from stack
                                var oldLeftOperand = leftOperandTransformations.Last();
                                leftOperandTransformations.Add(unop.Call(oldLeftOperand));
                            }
                        }
                        if (leftOperandTransformations != null)
                            argStack[argStack.Count - 1] = leftOperandTransformations.Last();
                        if (binopTransformations != null)
                            bop = binopTransformations.Last();
                        bwaStack.Add(new BinopWithArgs(bop, null, null));
                        leftOperand = BinopWithArgs.ResolveStack(bwaStack, argStack);
                    }
                    pos++;
                }
                else if (curtok is UnaryOp unop_)
                {
                    unop_.AddToStack(unopStack);
                    pos++;
                }
                else
                {
                    Obj_Pos opo = ParseExprOrScalar(toks, pos, end);
                    pos = opo.pos;
                    if (!(opo.obj is JNode onode))
                        throw new RemesPathException($"Expected JNode, got {opo.obj.GetType()}");
                    nt = PeekNextToken(toks, pos - 1, end);
                    if (nt == null || (nt is char nd_ && EXPR_FUNC_ENDERS.Contains(nd_)))
                    {
                        // no more binops for unary operators to fight with, so just apply all of them
                        while (unopStack.Count > 0)
                        {
                            JNode oldOnode = onode;
                            unop = unopStack.Pop();
                            JNode newOnode = unop.Call(oldOnode);
                            onode = newOnode;
                        }
                    }
                    argStack.Add(onode);
                    // no binop coming up, so clean up the stack
                    if (pos >= end || !(toks[pos] is Binop && bwaStack.Count > 0))
                        leftOperand = BinopWithArgs.ResolveStack(bwaStack, argStack);
                    curtok = onode;
                }
            }
            if (leftOperand == null)
            {
                throw new RemesPathException("Null return from ParseExprOrScalar");
            }
            return new Obj_Pos(leftOperand, pos);
        }

        private Obj_Pos ParseArgFunction(List<object> toks, int pos, ArgFunction fun, int end)
        {
            object t;
            pos++;
            int arg_num = 0;
            Dtype[] intypes = fun.InputTypes();
            List<JNode> args = new List<JNode>(fun.minArgs);
            if (fun.maxArgs == 0)
            {
                t = toks[pos];
                if (!(t is char d_ && d_ == ')'))
                    throw new RemesPathException($"Expected no arguments for function {fun.name} (0 args)");
                var withArgs = new ArgFunctionWithArgs(fun, args);
                return new Obj_Pos(ApplyArgFunction(withArgs), pos + 1);
            }
            JNode cur_arg = null;
            while (pos < end)
            {
                t = toks[pos];
                // the last Dtype in an ArgFunction's input_types is either the type options for the last arg
                // or the type options for every optional arg (if the function can have infinitely many args)
                Dtype type_options = arg_num >= intypes.Length 
                    ? intypes[intypes.Length - 1]
                    : intypes[arg_num];
                try
                {
                    try
                    {
                        Obj_Pos opo = ParseExprOrScalarFunc(toks, pos, end);
                        cur_arg = (JNode)opo.obj;
                        pos = opo.pos;
                    }
                    catch
                    {
                        cur_arg = null;
                    }
                    if ((Dtype.SLICE & type_options) != 0)
                    {
                        object nt = PeekNextToken(toks, pos - 1, end);
                        if (nt is char nd && nd == ':')
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
                            Obj_Pos opo = ParseSlicer(toks, pos, first_num, end);
                            cur_arg = (JNode)opo.obj;
                            pos = opo.pos;
                        }
                    }
                    if (cur_arg == null || (cur_arg.type & type_options) == 0)
                    {
                        Dtype arg_type = cur_arg == null ? Dtype.NULL : cur_arg.type;
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
                if (t is char d)
                {
                    comma = d == ',';
                    close_paren = d == ')';
                }
                else
                {
                    throw new RemesPathException($"Arguments of arg functions must be followed by ',' or ')', not {t}");
                }
                if (arg_num + 1 < fun.minArgs && !comma)
                {
                    if (fun.minArgs == fun.maxArgs)
                        throw new RemesPathException($"Expected ',' after argument {arg_num} of function {fun.name} ({fun.maxArgs} args)");
                    throw new RemesPathException($"Expected ',' after argument {arg_num} of function {fun.name} " +
                                                 $"({fun.minArgs} - {fun.maxArgs} args)");
                }
                if (arg_num + 1 == fun.maxArgs && !close_paren)
                {
                    if (fun.minArgs == fun.maxArgs)
                        throw new RemesPathException($"Expected ')' after argument {arg_num} of function {fun.name} ({fun.maxArgs} args)");
                    throw new RemesPathException($"Expected ')' after argument {arg_num} of function {fun.name} " +
                                                 $"({fun.minArgs} - {fun.maxArgs} args)");
                }
                args.Add(cur_arg);
                arg_num++;
                pos++;
                if (close_paren)
                {
                    var withargs = new ArgFunctionWithArgs(fun, args);
                    if (fun.maxArgs < int.MaxValue)
                    {
                        // for functions that have a fixed number of optional args, pad the unfilled args with null nodes
                        for (int arg2 = arg_num; arg2 < fun.maxArgs; arg2++)
                        {
                            args.Add(new JNode());
                        }
                    }
                    return new Obj_Pos(ApplyArgFunction(withargs), pos);
                }
            }
            if (fun.minArgs == fun.maxArgs)
                throw new RemesPathException($"Expected ')' after argument {arg_num} of function {fun.name} ({fun.maxArgs} args)");
            throw new RemesPathException($"Expected ')' after argument {arg_num} of function {fun.name} "
                                         + $"({fun.minArgs} - {fun.maxArgs} args)");
        }

        private Obj_Pos ParseProjection(List<object> toks, int pos, int end)
        {
            var children = new List<object>();
            bool is_object_proj = false;
            while (pos < end)
            {
                Obj_Pos opo = ParseExprOrScalarFunc(toks, pos, end);
                JNode key = (JNode)opo.obj;
                pos = opo.pos;
                object nt = PeekNextToken(toks, pos - 1, end);
                if (nt is char nd)
                {
                    if (nd == ':')
                    {
                        if (children.Count > 0 && !is_object_proj)
                        {
                            throw new RemesPathException("Mixture of values and key-value pairs in object/array projection");
                        }
                        if (key.type == Dtype.STR)
                        {
                            opo = ParseExprOrScalarFunc(toks, pos + 1, end);
                            JNode val = (JNode)opo.obj;
                            pos = opo.pos;
                            string keystr_in_quotes = key.ToString();
                            string keystr = keystr_in_quotes.Substring(1, keystr_in_quotes.Length - 2);
                            // do proper JSON string representation of characters that should not be in JSON keys
                            // (e.g., '\n', '\t', '\f')
                            // in case the user uses such a character in the projection keys in their query
                            children.Add(new KeyValuePair<string, JNode>(keystr, val));
                            is_object_proj = true;
                            nt = PeekNextToken(toks, pos - 1, end);
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
                        children.Add(key);
                    }
                    if (nd == '}')
                    {
                        if (is_object_proj)
                        {
                            IEnumerable<object> proj_func(JNode obj)
                            {
                                foreach(object child in children)
                                {
                                    var kv = (KeyValuePair<string, JNode>)child;
                                    yield return new KeyValuePair<string, JNode>(
                                        kv.Key,
                                        kv.Value is CurJson cj
                                            ? cj.function(obj)
                                            : kv.Value
                                    );
                                }
                            };
                            return new Obj_Pos(new Projection(proj_func), pos + 1);
                        }
                        else
                        {
                            IEnumerable<object> proj_func(JNode obj)
                            {
                                foreach (object child in children)
                                {
                                    var node = (JNode)child;
                                    yield return node is CurJson cj
                                        ? cj.function(obj)
                                        : node;
                                }
                            };
                            return new Obj_Pos(new Projection(proj_func), pos + 1);
                        }
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

        /// <summary>
        /// Consider the operation a -> b.<br></br>
        /// If b is a function b(x) -> y, a -> b returns b(a).<br></br>
        /// Otherwise, simply returns b<br></br>
        /// EXAMPLES:<br></br>
        /// Let x = [1, 2]<br></br>
        /// x -> str(len(@)) returns "2"<br></br>
        /// x -> 3 returns 3
        /// </summary>
        private Obj_Pos ParseMap(List<object> toks, int pos, int end)
        {
            Obj_Pos opo = ParseExprOrScalar(toks, pos, end);
            JNode val = (JNode)opo.obj;
            pos = opo.pos;
            Func<JNode, JNode> outfunc;
            if (val is CurJson cj)
                outfunc = cj.function;
            else
                outfunc = x => val;
            IEnumerable<object> iterator(JNode x) { yield return outfunc(x); }
            return new Obj_Pos(new Projection(iterator), pos + 1);
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
            if (ex is RemesPathIndexOutOfRangeException rpioore)
            {
                return rpioore.ToString();
            }
            if (ex is DsonDumpException dde)
            {
                return $"DSON dump error: {dde.Message}";
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
