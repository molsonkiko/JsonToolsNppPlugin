/*
A query language for JSON. 
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
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
    [Flags]
    public enum IndexerStart
    {
        NOT_AN_INDEXER = 1,
        /// <summary>[</summary>
        SQUAREBRACE = NOT_AN_INDEXER * 2,
        /// <summary>{</summary>
        CURLYBRACE = SQUAREBRACE * 2,
        /// <summary>.</summary>
        DOT = CURLYBRACE * 2,
        /// <summary>..</summary>
        DOUBLEDOT = DOT * 2,
        /// <summary>..[</summary>
        DOUBLEDOT_SQUAREBRACE = DOUBLEDOT * 2,
        /// <summary>-&gt;</summary>
        FORWARD_ARROW = DOUBLEDOT_SQUAREBRACE * 2,
        /// <summary>![</summary>
        BANG_SQUAREBRACE = FORWARD_ARROW * 2,
        /// <summary>!.</summary>
        BANG_DOT = BANG_SQUAREBRACE * 2,
        // currently recursive negated indices are not supported because (a) annoying to implement and (b) potentially very counterintuitive
        ///// <summary>!..</summary>
        //BANG_DOUBLEDOT = BANG_DOT * 2,
        ///// <summary>!..[</summary>
        //BANG_DOUBLEDOT_SQUAREBRACE = BANG_DOUBLEDOT * 2,

        ANY_DOT_TYPE = DOT | DOUBLEDOT | BANG_DOT /*| BANG_DOUBLEDOT*/,

        PROJECTION = CURLYBRACE | FORWARD_ARROW,

        ANY_SQUAREBRACE_TYPE = BANG_SQUAREBRACE | SQUAREBRACE | DOUBLEDOT_SQUAREBRACE,

        ANY_BANG_TYPE = BANG_DOT | BANG_SQUAREBRACE /*| BANG_DOUBLEDOT | BANG_DOUBLEDOT_SQUAREBRACE*/,

        ANY_DOUBLEDOT_TYPE = DOUBLEDOT | DOUBLEDOT_SQUAREBRACE /*| BANG_DOUBLEDOT | BANG_DOUBLEDOT_SQUAREBRACE*/,
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
        public Func<JNode, IEnumerable<object>> projFunc;

        public Projection(Func<JNode, IEnumerable<object>> projFunc)
        {
            this.projFunc = projFunc;
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
        public bool hasOneOption;
        /// <summary>
        /// is an array or object projection made by the {foo: @[0], bar: @[1]} type syntax.
        /// </summary>
        public bool isProjection;
        /// <summary>
        /// is an object
        /// </summary>
        public bool isDict;
        /// <summary>
        /// involves recursive search
        /// </summary>
        public bool isRecursive;

        public IndexerFunc(Func<JNode, IEnumerable<object>> idxr, bool hasOneOption, bool isProjection, bool isDict, bool isRecursive)
        {
            this.idxr = idxr;
            this.hasOneOption = hasOneOption;
            this.isProjection = isProjection;
            this.isDict = isDict;
            this.isRecursive = isRecursive;
        }

        /// <summary>
        /// Unlike other indexers, boolean indices may simply return the original object
        /// (for example @[@.a &gt; 3] returns <i>an entire object</i> if the value with key "a" is greater than 3)<br></br>
        /// OR return individual elements of the original object (for example @[@ &gt; 3] returns <i>the elements of an object/array</i> that are greater than 3).<br></br>
        /// This is important because we want boolean indices to be easy to chain.<br></br>
        /// Because of this, ApplyBooleanIndex must be an instance method of the IndexerFunc class,
        /// so that it can set the hasOneOption and isDict attributes of this class.
        /// </summary>
        /// <param name="inds"></param>
        /// <returns></returns>
        /// <exception cref="VectorizedArithmeticException">if the boolean index has the wrong length or contains non-booleans</exception>
        public Func<JNode, IEnumerable<object>> ApplyBooleanIndex(JNode inds)
        {
            IEnumerable<object> boolIdxrFunc(JNode x)
            {
                hasOneOption = false;
                JNode newinds = (inds is CurJson cj)
                    ? cj.function(x)
                    : inds;
                isDict = x is JObject;
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
                            hasOneOption = true;
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
                        bool iHasKey = iobj.children.TryGetValue(kv.Key, out JNode ival);
                        if (iHasKey)
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
            return boolIdxrFunc;
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
        /// <summary>0-based index of the bad argument</summary>
        public int ArgNum { get; set; }
        /// <summary>
        /// type that was passed in for that argument
        /// </summary>
        public Dtype GotType;
        public ArgFunction func { get; set; }

        /// <summary></summary>
        /// <param name="description">a description of the error. Can be null, if gotType is specified.</param>
        /// <param name="argIndex0Based">the argument where an incorrect type was passed in</param>
        /// <param name="func">the function that raised this error (choose one from ArgFunction.FUNCTIONS)</param>
        /// <param name="gotType">the type that was passed in for argument argIndex0Based</param>
        public RemesPathArgumentException(string description, int argIndex0Based, ArgFunction func, Dtype gotType = Dtype.NULL) : base(description)
        {
            GotType = gotType;
            ArgNum = argIndex0Based;
            this.func = func;
        }

        public override string ToString()
        {
            string validTypes = JNode.FormatDtype(func.TypeOptions(ArgNum));
            if (description is null)
                return $"For argument {ArgNum} of function {func.name}, expected {validTypes}, instead got type {JNode.FormatDtype(GotType)}";
            return $"For argument {ArgNum} of function {func.name}, expected {validTypes}, instead {description}";
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

    /// <summary>
    /// thrown by JQueryContext.TryGetValue when a variable name is referenced,
    /// but the variable isn't declared until a later statement.
    /// </summary>
    public class RemesPathNameException : RemesPathException
    {
        public string varname;
        public int indexInStatements;
        public int indexOfDeclaration;

        public RemesPathNameException(string varname, int indexInStatements, int indexOfDeclaration) : base("")
        {
            this.varname = varname;
            this.indexInStatements = indexInStatements;
            this.indexOfDeclaration = indexOfDeclaration;
        }

        public override string ToString()
        {
            return $"Variable named \"{varname}\" was referenced in statement {indexInStatements + 1}, " +
                   $"but was not declared until statement {indexOfDeclaration + 1}";
        }
    }

    public class RemesPathLoopVarNotAnArrayException : RemesPathException
    {
        public VarAssign Va;
        public Dtype Dtype_;

        public RemesPathLoopVarNotAnArrayException(VarAssign va, Dtype dtype) : base("")
        {
            this.Va = va;
            this.Dtype_ = dtype;
        }

        public override string ToString()
        {
            return $"Loop variable named {Va.Name} must be an array, but got type {JNode.DtypeStrings[Dtype_]}";
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
        /// The cacheCapacity indicates how many queries to store in the old query cache.
        /// </summary>
        /// <param name="cacheCapacity"></param>
        public RemesParser(int cacheCapacity = 64)
        {
            cache = new LruCache<string, JNode>(cacheCapacity);
            lexer = new RemesPathLexer();
        }

        /// <summary>
        /// Parse a query and compile it into a RemesPath function that operates on JSON.<br></br>
        /// If the query is not a function of input, it will instead just output fixed JSON.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public JNode Compile(string query)
        {
            if (cache.TryGetValue(query, out JNode oldResult))
                return oldResult;
            List<object> toks = lexer.Tokenize(query);
            JNode result = ParseQuery(toks);
            cache.SetDefault(query, result);
            return result;
        }

        /// <summary>
        /// Perform a RemesPath query on JSON and return the result.<br></br>
        /// If isAssignmentExpr is true, this means that the query is an assignment expression<br></br>
        /// (i.e., a query that mutates the underlying JSON)
        /// </summary>
        /// <param name="query"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public JNode Search(string query, JNode obj)
        {
            JNode compiledQuery = Compile(query);
            if (compiledQuery.CanOperate)
                return compiledQuery.Operate(obj);
            return compiledQuery;
        }

        /// <summary>
        /// these tokens have high enough precedence to stop an exprFunc (parsed by ParseExprFunc)
        /// </summary>
        public const string EXPR_FUNC_ENDERS = "]:},);";
        public const string INDEXER_STARTERS = ".[{>!";
        public const string PROJECTION_STARTERS = "{>";

        #region INDEXER_FUNCTIONS
        private Func<JNode, IEnumerable<object>> ApplyMultiIndex(object inds, bool isVarnameList, bool isRecursive = false)
        {
            if (inds is CurJson cj)
            {
                IEnumerable<object> multiIdxFunc(JNode x)
                {
                    return ApplyMultiIndex(cj.function(x), isVarnameList, isRecursive)(x);
                }
                return multiIdxFunc;
            }
            var children = (List<object>)inds;
            if (isVarnameList)
            {
                if (isRecursive)
                {
                    IEnumerable<object> multiIdxFunc(JNode x, string path, HashSet<string> pathsVisited)
                    {
                        if (x is JArray xarr)
                        {
                            // a varname list can only match dict keys, not array indices
                            // we'll just recursively search from each child of this array
                            for (int ii = 0; ii < xarr.Length; ii++)
                            {
                                foreach (object kv in multiIdxFunc(xarr[ii], $"{path},{ii}", pathsVisited))
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
                                            if (!pathsVisited.Contains(newpath))
                                                yield return kv.Value;
                                            pathsVisited.Add(newpath);
                                        }
                                        else
                                        {
                                            foreach (object node in multiIdxFunc(kv.Value, newpath, pathsVisited))
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
                                            if (!pathsVisited.Contains(newpath))
                                                yield return kv.Value;
                                            pathsVisited.Add(newpath);
                                        }
                                        else
                                        {
                                            foreach (object node in multiIdxFunc(kv.Value, newpath, pathsVisited))
                                            {
                                                yield return node;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    return x => multiIdxFunc(x, "", new HashSet<string>());
                }
                else // not recursive
                {
                    IEnumerable<object> multiIdxFunc(JNode x)
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
                    return multiIdxFunc;
                }
            }
            else
            {
                // it's a list of ints or slices
                if (isRecursive)
                {
                    // TODO: decide whether to implement recursive search for slices and indices
                    throw new NotImplementedException("Recursive search for array indices and slices is not implemented");
                }
                IEnumerable<object> multiIdxFunc(JNode x)
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
                return multiIdxFunc;
            }
        }

        /// <summary>
        /// returns a predicate that evaluates whether a key s matches a varname list<br></br>
        /// (e.g., ".bar" matches the key "bar",<br></br>
        /// "[bar, g`baz`]" matches the key "bar" or the regex "baz" 
        /// </summary>
        /// <param name="inds"></param>
        /// <returns></returns>
        /// <exception cref="RemesPathException"></exception>
        private Predicate<string> VarnameListMatcher(List<object> inds)
        {
            var keys = new HashSet<string>();
            var regexes = new List<Regex>();
            foreach (object obj in inds)
            {
                if (obj is string s)
                    keys.Add(s);
                else
                    regexes.Add((Regex)obj);
            }
            int keyCount = keys.Count;
            int rexCount = regexes.Count;
            string firstKey = keyCount == 0 ? null : keys.ToArray()[0];
            Regex firstRegex = rexCount == 0 ? null : regexes[0];
            if (keyCount > 0)
            {
                if (rexCount > 0)
                {
                    if (keyCount == 1)
                    {
                        if (rexCount == 1)
                            return s => (s == firstKey) || firstRegex.IsMatch(s);
                        else 
                            return s => (s == firstKey) || regexes.Any(rex => rex.IsMatch(s));
                    }
                    else if (rexCount == 1)
                        return s => keys.Contains(s) || firstRegex.IsMatch(s);
                    else // multiple keys, multiple regexes
                        return s => keys.Contains(s) || regexes.Any(rex => rex.IsMatch(s));
                }
                else // multiple keys, no regexes
                    return keys.Contains;
            }
            else if (rexCount > 0)
            {
                if (rexCount == 1) // one regex, no keys
                    return firstRegex.IsMatch;
                return s => regexes.Any(rex => rex.IsMatch(s));
            }
            else
                throw new RemesPathException("Negated indexer with no strings or regexes"); // should never happen due to earlier checks
        }

        private Func<JNode, IEnumerable<object>> ApplyNegatedVarnameList(List<object> inds)
        {
            //if (isRecursive)
            //{
            //    IEnumerable<JNode> negatedVarnameListFunc(JNode node)
            //    {
            //        if (node is JArray arr)
            //        {
            //            foreach (JNode child in arr.children)
            //            {
            //                foreach (JNode subchild in negatedVarnameListFunc(child))
            //                    yield return subchild;
            //            }
            //        }
            //        else if (node is JObject obj)
            //        {
            //            foreach (KeyValuePair<string, JNode> kv in obj.children)
            //            {
            //                if (!matcher(kv.Key))
            //                    yield return kv.Value;
            //            }
            //        }
            //    }
            //    return negatedVarnameListFunc;
            //}
            //else
            //{
            Predicate<string> matcher = VarnameListMatcher(inds);
            IEnumerable<object> negatedVarnameListFunc(JNode node)
            {
                var obj = (JObject)node;
                foreach (KeyValuePair<string, JNode> kv in obj.children)
                {
                    if (!matcher(kv.Key))
                        yield return kv;
                }
            };
            return negatedVarnameListFunc;
            //}
        }

        private int[] IndicesMatchingNegatedSlicerList(List<object> inds, int length)
        {
            if (length == 0)
                return new int[0];
            var allIndices = new int[length];
            for (int ii = 0; ii < length; ii++)
                allIndices[ii] = ii;
            var potentialIndices = new HashSet<int>(allIndices);
            foreach (object ind in inds)
            {
                if (ind is int ii)
                    potentialIndices.Remove(ii < 0 ? ii + length : ii);
                else if (ind is int?[] slicer)
                    potentialIndices.ExceptWith(allIndices.LazySlice(slicer));
            }
            var matchedIndices = potentialIndices.ToArray();
            Array.Sort(matchedIndices);
            return matchedIndices;
        }

        private Func<JNode, IEnumerable<JNode>> ApplyNegatedSlicerList(List<object> inds)
        {
            IEnumerable<JNode> negatedSlicerListFunc(JNode node)
            {
                JArray arr = (JArray)node;
                foreach (int ii in IndicesMatchingNegatedSlicerList(inds, arr.Length))
                    yield return arr.children[ii];
            }
            return negatedSlicerListFunc;
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
            JNode idxrListFunc(JNode obj, List<IndexerFunc> idxrs, int ii)
            {
                IndexerFunc ix = idxrs[ii];
                var inds = ix.idxr(obj).GetEnumerator();
                // IEnumerator<T>.MoveNext returns a bool indicating if the enumerator has passed the end of the collection
                if (!inds.MoveNext())
                {
                    // the IndexerFunc couldn't find anything
                    if (ix.isDict)
                    {
                        return new JObject();
                    }
                    return new JArray();
                }
                object current = inds.Current;
                bool isDict = current is KeyValuePair<string, JNode>;
                List<JNode> arr;
                Dictionary<string, JNode> dic;
                if (ii == idxrs.Count - 1)
                {
                    if (ix.hasOneOption)
                    {
                        // return a scalar rather than an iterable with one element
                        if (current is KeyValuePair<string, JNode> kv)
                            return kv.Value;
                        return (JNode)current;
                    }
                    if (isDict)
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
                if (ix.isProjection)
                {
                    if (isDict)
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
                        return idxrListFunc(new JObject(0, dic), idxrs, ii + 1);
                    }
                    else if (ix.hasOneOption)
                    {
                        return idxrListFunc((JNode)inds.Current, idxrs, ii + 1);
                    }
                    arr = new List<JNode> { (JNode)current };
                    while (inds.MoveNext())
                    {
                        arr.Add((JNode)inds.Current);
                    }
                    return idxrListFunc(new JArray(0, arr), idxrs, ii + 1);
                }
                JNode v1_subdex;
                if (current is JNode node)
                {
                    v1_subdex = idxrListFunc(node, idxrs, ii + 1);
                }
                else
                {
                    node = ((KeyValuePair<string, JNode>)current).Value;
                    v1_subdex = idxrListFunc(node, idxrs, ii + 1);
                }
                if (ix.hasOneOption)
                {
                    return v1_subdex;
                }
                int isEmpty = Binop.ObjectOrArrayEmpty(v1_subdex);
                if (isDict)
                {
                    var kv = (KeyValuePair<string, JNode>)current;
                    dic = new Dictionary<string, JNode>();
                    if (isEmpty != 1)
                    {
                        dic[kv.Key] = v1_subdex;
                    }
                    while (inds.MoveNext())
                    {
                        kv = (KeyValuePair<string, JNode>)inds.Current;
                        JNode subdex = idxrListFunc(kv.Value, idxrs, ii + 1);
                        isEmpty = Binop.ObjectOrArrayEmpty(subdex);
                        if (isEmpty != 1)
                        {
                            dic[kv.Key] = subdex;
                        }
                    }
                    return new JObject(obj.position, dic);
                }
                // obj is a list iterator
                arr = new List<JNode>();
                if (isEmpty != 1)
                {
                    arr.Add(v1_subdex);
                }
                while (inds.MoveNext())
                {
                    var v = (JNode)inds.Current;
                    JNode subdex = idxrListFunc(v, idxrs, ii + 1);
                    isEmpty = Binop.ObjectOrArrayEmpty(subdex);
                    if (isEmpty != 1)
                    {
                        arr.Add(subdex);
                    }
                }
                return new JArray(obj.position, arr);
            }
            return (JNode obj) => idxrListFunc(obj, indexers, 0);
        }

        #endregion
        #region APPLY_ARG_FUNCTION
        private JNode ApplyArgFunction(ArgFunctionWithArgs func)
        {
            if (func.function.conditionalExecution)
            {
                // need to add a CurJson after all the normal args
                // so that the function will have a pointer to the current json,
                // and all instances of the @ symbol can be resolved
                func.args.Add(new CurJson());
            }
            if (func.function.maxArgs == 0)
            {
                // paramterless function like rand()
                if (!func.function.isDeterministic)
                    return new CurJson(func.function.type, blah => func.function.Call(func.args));
                return func.function.Call(func.args);
            }
            func.function.argsTransform?.Transform(func.args);
            JNode x = func.args[0];
            bool otherCallables = false;
            List<JNode> otherArgs = new List<JNode>(func.args.Count - 1);
            bool[] argsCanBeFunctions = new bool[func.args.Count - 1]; // for each otherArgs index, whether that arg can be a function
            bool firstArgCanBeFunction = (func.function.TypeOptions(0) & Dtype.FUNCTION) != 0;
            for (int ii = 0; ii < func.args.Count - 1; ii++)
            {
                JNode arg = func.args[ii + 1];
                if (arg is CurJson) { otherCallables = true; }
                argsCanBeFunctions[ii] = (func.function.TypeOptions(ii + 1) & Dtype.FUNCTION) != 0;
                otherArgs.Add(arg);
            }
            if (func.function.conditionalExecution)
                argsCanBeFunctions[argsCanBeFunctions.Length - 1] = false; // if conditional execution, last arg is CurJson
                                                                           // that must be called before argfunction is evaluated
            Dtype outType = func.function.OutputType(x);
            List<JNode> allArgs = new List<JNode>(func.args.Count);
            foreach (var a in func.args)
                allArgs.Add(null);
            if (func.function.isVectorized)
            {
                if (x is CurJson xcur)
                {
                    if (otherCallables)
                    {
                        // x is a function of the current JSON, as is at least one other argument
                        JNode argOutfunc(JNode inp)
                        {
                            var itbl = firstArgCanBeFunction ? xcur : xcur.function(inp);
                            for (int ii = 0; ii < otherArgs.Count; ii++)
                            {
                                JNode otherArg = otherArgs[ii];
                                allArgs[ii + 1] = (otherArg is CurJson cjoa && !argsCanBeFunctions[ii]) ? cjoa.function(inp) : otherArg;
                            }
                            if (itbl is JObject otbl)
                            {
                                var dic = new Dictionary<string, JNode>(otbl.Length);
                                foreach (KeyValuePair<string, JNode> okv in otbl.children)
                                {
                                    allArgs[0] = okv.Value;
                                    dic[okv.Key] = func.function.Call(allArgs);
                                }
                                return new JObject(0, dic);
                            }
                            else if (itbl is JArray atbl)
                            {
                                var arr = new List<JNode>();
                                foreach (JNode val in atbl.children)
                                {
                                    allArgs[0] = val;
                                    arr.Add(func.function.Call(allArgs));
                                }
                                return new JArray(0, arr);
                            }
                            // x is a scalar function of the current JSON, so we just call the function on that scalar
                            // and the other args
                            allArgs[0] = itbl;
                            return func.function.Call(allArgs);
                        }
                        return new CurJson(outType, argOutfunc);
                    }
                    else
                    {
                        // there are no other functions of the current JSON; the first argument is the only one
                        // this means that all the other args are fixed and can be used as is
                        for (int ii = 0; ii < otherArgs.Count; ii++)
                        {
                            JNode otherArg = otherArgs[ii];
                            allArgs[ii + 1] = otherArg;
                        }
                        JNode argOutfunc(JNode inp)
                        {
                            
                            var itbl = firstArgCanBeFunction ? xcur : xcur.function(inp);
                            if (itbl is JObject otbl)
                            {
                                var dic = new Dictionary<string, JNode>(otbl.Length);
                                foreach (KeyValuePair<string, JNode> okv in otbl.children)
                                {
                                    allArgs[0] = okv.Value;
                                    dic[okv.Key] = func.function.Call(allArgs);
                                }
                                return new JObject(0, dic);
                            }
                            else if (itbl is JArray atbl)
                            {
                                var arr = new List<JNode>();
                                foreach (JNode val in atbl.children)
                                {
                                    allArgs[0] = val;
                                    arr.Add(func.function.Call(allArgs));
                                }
                                return new JArray(0, arr);
                            }
                            // x is a scalar function of the input
                            allArgs[0] = itbl;
                            return func.function.Call(allArgs);
                        }
                        return new CurJson(outType, argOutfunc);
                    }
                }
                if (otherCallables)
                {
                    // at least one other argument is a function of the current JSON, but not the first argument
                    if (x.type == Dtype.OBJ)
                    {
                        JObject xobj = (JObject)x;
                        JNode argOutfunc(JNode inp)
                        {
                            var dic = new Dictionary<string, JNode>(xobj.Length);
                            for (int ii = 0; ii < otherArgs.Count; ii++)
                            {
                                JNode otherArg = otherArgs[ii];
                                allArgs[ii + 1] = (otherArg is CurJson cjoa && !argsCanBeFunctions[ii]) ? cjoa.function(inp) : otherArg;
                            }
                            foreach (KeyValuePair<string, JNode> xkv in xobj.children)
                            {
                                allArgs[0] = xkv.Value;
                                dic[xkv.Key] = func.function.Call(allArgs);
                            }
                            return new JObject(0, dic);
                        }
                        return new CurJson(outType, argOutfunc);
                    }
                    else if (x.type == Dtype.ARR)
                    {
                        // x is an array and at least one other argument is a function of the current JSON
                        var xarr = (JArray)x;
                        JNode argOutfunc(JNode inp)
                        {
                            var arr = new List<JNode>();
                            for (int ii = 0; ii < otherArgs.Count; ii++)
                            {
                                JNode otherArg = otherArgs[ii];
                                allArgs[ii + 1] = (otherArg is CurJson cjoa && !argsCanBeFunctions[ii]) ? cjoa.function(inp) : otherArg;
                            }
                            foreach (JNode val in xarr.children)
                            {
                                allArgs[0] = val;
                                arr.Add(func.function.Call(allArgs));
                            }
                            return new JArray(0, arr);
                        }
                        return new CurJson(outType, argOutfunc);
                    }
                    else
                    {
                        // x is not iterable, and at least one other arg is a function of the current JSON
                        JNode argOutfunc(JNode inp)
                        {
                            for (int ii = 0; ii < otherArgs.Count; ii++)
                            {
                                JNode otherArg = otherArgs[ii];
                                allArgs[ii + 1] = (otherArg is CurJson cjoa && !argsCanBeFunctions[ii]) ? cjoa.function(inp) : otherArg;
                            }
                            allArgs[0] = x;
                            return func.function.Call(allArgs);
                        }
                        return new CurJson(outType, argOutfunc);
                    }
                }
                else
                {
                    if (!func.function.isDeterministic)
                        return new CurJson(func.function.type, blah => CallVectorizedArgFuncWithArgs(x, otherArgs, allArgs, func.function));
                    return CallVectorizedArgFuncWithArgs(x, otherArgs, allArgs, func.function);
                }
            }
            else
            {
                // this is NOT a vectorized arg function (it's something like len or mean)
                if (x is CurJson xcur)
                {
                    if (otherCallables)
                    {
                        JNode argOutfunc(JNode inp)
                        {
                            for (int ii = 0; ii < otherArgs.Count; ii++)
                            {
                                JNode otherArg = otherArgs[ii];
                                allArgs[ii + 1] = (otherArg is CurJson cjoa && !argsCanBeFunctions[ii]) ? cjoa.function(inp) : otherArg;
                            }
                            allArgs[0] = firstArgCanBeFunction ? xcur : xcur.function(inp);
                            return func.function.Call(allArgs);
                        }
                        return new CurJson(outType, argOutfunc);
                    }
                    else
                    {
                        for (int ii = 0; ii < otherArgs.Count; ii++)
                        {
                            JNode otherArg = otherArgs[ii];
                            allArgs[ii + 1] = otherArg;
                        }
                        JNode argOutfunc(JNode inp)
                        {
                            allArgs[0] = firstArgCanBeFunction ? xcur : xcur.function(inp);
                            return func.function.Call(allArgs);
                        }
                        return new CurJson(outType, argOutfunc);
                    }
                }
                else if (otherCallables)
                {
                    // it's a non-vectorized function where the first arg is not a current json func but at least
                    // one other is
                    JNode argOutfunc(JNode inp)
                    {
                        for (int ii = 0; ii < otherArgs.Count; ii++)
                        {
                            JNode otherArg = otherArgs[ii];
                            allArgs[ii + 1] = (otherArg is CurJson cjoa && !argsCanBeFunctions[ii]) ? cjoa.function(inp) : otherArg;
                        }
                        allArgs[0] = x;
                        return func.function.Call(allArgs);
                    }
                    return new CurJson(outType, argOutfunc);
                }
                // it is a non-vectorized function where none of the args are functions of the current
                // json (e.g., s_mul(`a`, 14))
                for (int ii = 0; ii < otherArgs.Count; ii++)
                {
                    JNode otherArg = otherArgs[ii];
                    allArgs[ii + 1] = otherArg;
                }
                allArgs[0] = x;
                if (!func.function.isDeterministic)
                    return new CurJson(func.function.type, blah => func.function.Call(allArgs));
                return func.function.Call(allArgs);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="otherArgs"></param>
        /// <param name="allArgs"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        private static JNode CallVectorizedArgFuncWithArgs(JNode x, List<JNode> otherArgs, List<JNode> allArgs, ArgFunction func)
        {
            // none of the arguments are functions of the current JSON
            for (int ii = 0; ii < otherArgs.Count; ii++)
            {
                JNode otherArg = otherArgs[ii];
                allArgs[ii + 1] = otherArg;
            }
            if (x is JObject xobj)
            {
                var dic = new Dictionary<string, JNode>(xobj.Length);
                foreach (KeyValuePair<string, JNode> xkv in xobj.children)
                {
                    allArgs[0] = xobj[xkv.Key];
                    dic[xkv.Key] = func.Call(allArgs);
                }
                return new JObject(0, dic);
            }
            else if (x is JArray xarr)
            {
                var arr = new List<JNode>(xarr.Length);
                foreach (JNode val in xarr.children)
                {
                    allArgs[0] = val;
                    arr.Add(func.Call(allArgs));
                }
                return new JArray(0, arr);
            }
            // x is not iterable, and no args are functions of the current JSON
            allArgs[0] = x;
            return func.Call(allArgs);
        }

        #endregion
        #region PARSER_FUNCTIONS

        private static object PeekNextToken(List<object> toks, int pos, int end)
        {
            if (pos + 1 >= end)
                return null;
            return toks[pos + 1];
        }

        /// <summary>
        /// Parses toks as a series of statements terminated by semicolons (a semicolon after the last statement is optional).<br></br>
        /// See ParseStatement for details on how those statements are parsed
        /// </summary>
        /// <param name="toks"></param>
        /// <returns></returns>
        private JNode ParseQuery(List<object> toks)
        {
            int pos = 0;
            int end = toks.Count;
            var context = new JQueryContext();
            Dictionary<string, bool> isVarnameFunctionOfInput = DetermineWhichVariablesAreFunctionsOfInput(toks);
            while (pos < end)
            {
                int nextSemicolon = toks.IndexOf(';', pos, end - pos);
                int endOfStatement = nextSemicolon < 0 ? end : nextSemicolon;
                JNode statement = ParseStatement(toks, pos, endOfStatement, context, isVarnameFunctionOfInput);
                context.AddStatement(statement);
                pos = endOfStatement + 1;
            }
            return context.GetQuery();
        }

        /// <summary>
        /// If a variable is not a function of the input, RemesPath can evaluate all functions and binops that reference that variable at compile time.<br></br>
        /// However, sometimes a variable could be assigned to a compile-time constant value, referenced in a function,<br></br>
        /// and then later reassigned to a value that is a function of input.<br></br>
        /// The only way to know for sure that a variable is a function of input (and thus ineligible for constant propagation in functions)<br></br>
        /// is to scan through the entire input before actually evaluating functions and flag whether each variable is a function of input.<br></br>
        /// Returns a map from variable names to the predicate (the variable with that name is a function of input)
        /// </summary>
        /// <param name="toks"></param>
        /// <returns></returns>
        private Dictionary<string, bool> DetermineWhichVariablesAreFunctionsOfInput(List<object> toks)
        {
            var isVarnameFunctionOfInput = new Dictionary<string, bool>();
            int pos = 0;
            int end = toks.Count;
            bool containsMutation = false;
            while (pos < end)
            {
                int nextSemicolon = toks.IndexOf(';', pos, end - pos);
                int endOfStatement = nextSemicolon < 0 ? end : nextSemicolon;
                int nextEqualsSign = toks.IndexOf('=', pos, end - pos);
                if (nextEqualsSign >= 0 && nextEqualsSign < endOfStatement)
                {
                    // it's an assignment expression or a variable assignment.
                    // either way, we will assume that any variable referenced on the LHS *could* be a function of the input
                    // if the RHS references the input.
                    (pos, containsMutation) = CheckStatementForInputReferences(toks, pos, nextEqualsSign, endOfStatement, isVarnameFunctionOfInput, containsMutation);
                }
                else
                    pos = endOfStatement + 1;
            }
            ArgFunction.InitializeGlobals(containsMutation);
            if (containsMutation)
            {
                // no variable is safe from the mutation, not even ones that were declared before the mutation expression
                foreach (string varname in isVarnameFunctionOfInput.Keys.ToArray())
                    isVarnameFunctionOfInput[varname] = true;
            }
            return isVarnameFunctionOfInput;
        }

        /// <summary>
        /// always returns endOfStatement + 1.<br></br>
        /// If the RHS of the current statement (tokens in toks between equalsPosition and endOfStatement) contains any references to input (@)
        /// or variables that themselves reference the current input,<br></br>
        /// also sets isVarnameFunctionOfInput[varname] = true for all variable names on the LHS of the current statement (tokens in toks between pos and equalsPosition)<br></br>
        /// If RHS is NOT function of input, sets isVarnameFunctionOfInput[varname] = false for every varname on LHS *if isVanameFunctionOfInput[varname] is not already true*
        /// </summary>
        /// <param name="toks">list of tokens</param>
        /// <param name="pos">position to start iterating</param>
        /// <param name="equalsPosition">the position of the next '=' sign</param>
        /// <param name="endOfStatement">the position of the next semicolon or the end of the query</param>
        /// <param name="isVarnameFunctionOfInput">map from variable name to bool (this variable is a function of input)</param>
        /// <returns></returns>
        private (int end, bool containsMutation) CheckStatementForInputReferences(List<object> toks, int pos, int equalsPosition, int endOfStatement, Dictionary<string, bool> isVarnameFunctionOfInput, bool containsMutation)
        {
            if (StatementIsVarAssign(toks, pos, endOfStatement, out string varname, out VariableAssignmentType assignmentType))
            {
                // variable assignment; obviously the varname is on the LHS
                if (containsMutation || assignmentType == VariableAssignmentType.LOOP)
                {
                    // loop variables are re-cached for every iteration of the loop
                    // so even if you're looping over a compile-time known constant, it's effectively a function of input
                    isVarnameFunctionOfInput[varname] = true;
                    return (endOfStatement + 1, containsMutation);
                }
            }
            else
            {
                // it's a mutator expression (aka assignment expression)
                // we can't assume that ANY variable is safe from this mutation without doing some really complex analysis of the reference graph
                // so we'll just quit and assume that all variables are functions of input
                return (endOfStatement + 1, true);
                
            }
            // LHS is variable assignment and there are no mutation expressions in the query; check RHS for functions of input
            for (pos = equalsPosition + 1; pos < endOfStatement; pos++)
            {
                object tok = toks[pos];
                if (tok is CurJson // RHS references input directly
                    || (tok is UnquotedString otherVarUqs && isVarnameFunctionOfInput.TryGetValue(otherVarUqs.value, out bool otherVarReferencesInput)
                        && otherVarReferencesInput)) // varname in RHS references input
                {
                    isVarnameFunctionOfInput[varname] = true;
                    return (endOfStatement + 1, false);
                }
            }
            // rhs doesn't ref input, so all variables on LHS are assumed not to ref input
            // unless they were previously defined in a way that referenced input
            if (!(isVarnameFunctionOfInput.TryGetValue(varname, out bool wasAlreadyFunctionOfInput) && wasAlreadyFunctionOfInput))
                isVarnameFunctionOfInput[varname] = false;
            return (endOfStatement + 1, false);
        }

        private JNode ParseStatement(List<object> toks, int start, int end, JQueryContext context, Dictionary<string, bool> isVarnameFunctionOfInput)
        {
            int indexOfAssignment = toks.IndexOf('=', start, end - start);
            if (indexOfAssignment == start)
                throw new RemesPathException("Assignment operator '=' with no left-hand side");
            if (indexOfAssignment == end - 1)
                throw new RemesPathException("Assignment operator '=' with no right-hand side");
            if (indexOfAssignment >= 0)
            {
                if (toks.IndexOf('=', indexOfAssignment + 1, end - indexOfAssignment - 1) >= 0)
                    throw new RemesPathException("Only one '=' assignment operator allowed in a statement");
                if (StatementIsVarAssign(toks, start, end, out string varname, out VariableAssignmentType assignmentType))
                {
                    // variable assignment
                    // TODO: need to figure out what to do if variable name collides with function names
                    bool isFunctionOfInput = isVarnameFunctionOfInput[varname];
                    return ParseVariableAssignment(toks, start + 3, end, varname, context, assignmentType, isFunctionOfInput);
                }
                return ParseAssignmentExpr(toks, start, end, indexOfAssignment, context);
            }
            else if (StatementIsLoopEnd(toks, start, end))
            {
                return new LoopEnd();
            }
            return (JNode)ParseExprFunc(toks, start, end, context).obj;
        }

        private JMutator ParseAssignmentExpr(List<object> toks, int start, int end, int indexOfAssignment, JQueryContext context)
        {
            JNode selector = (JNode)ParseExprFunc(toks, start, indexOfAssignment, context).obj;
            JNode mutator = (JNode)ParseExprFunc(toks, indexOfAssignment + 1, end, context).obj;
            return new JMutator(selector, mutator);
        }
        
        private JNode ParseVariableAssignment(List<object> toks, int start, int end, string name, JQueryContext context, VariableAssignmentType assignmentType, bool isFunctionOfInput)
        {
            JNode value = (JNode)ParseExprFunc(toks, start, end, context).obj;
            return new VarAssign(name, value, context.indexInStatements, assignmentType, isFunctionOfInput);
        }

        /// <summary>
        /// VAR_ASSIGN := VAR_KEYWORD VARNAME "=" ExprFunc<br></br>
        /// returns true, assignmentType = RemesPathLexer.VAR_ASSIGN_KEYWORDS_TO_TYPES[VAR_KEYWORD], and VARNAME = (an unquoted string between varKeyword and "=")<br></br>
        /// At present VAR_KEYWORD can only be "var" or "for".<br></br>
        /// if the statement is not of that form, return false, varname = null, and assignmentType = VariableAssignmentType.INVALID.
        /// </summary>
        private bool StatementIsVarAssign(List<object> toks, int start, int end, out string varname, out VariableAssignmentType assignmentType)
        {
            varname = null;
            assignmentType = VariableAssignmentType.INVALID;
            if (end <= toks.Count && start <= end - 4 // [var, name, =, end] - there are 4 tokens in a minimal assignment expression 
                && toks[start] is UnquotedString uqs && RemesPathLexer.VAR_ASSIGN_KEYWORDS_TO_TYPES.TryGetValue(uqs.value, out assignmentType)
                && toks[start + 1] is UnquotedString varnameUqs
                && toks[start + 2] is char c && c == '=')
            {
                varname = varnameUqs.value;
                return true;
            }
            return false;
        }

        /// <summary>
        /// LOOP_END := LOOP_END_KEYWORD LOOP_KEYWORD<br></br>
        /// returns true if the statement is of this form.
        /// LOOP_END_KEYWORD is currently "end" and LOOP_KEYWORD is currently "for"<br></br>
        /// </summary>
        private bool StatementIsLoopEnd(List<object> toks, int start, int end)
        {
            return (end <= toks.Count && start == end - 2
                && toks[start] is UnquotedString keywordUqs && keywordUqs.value == RemesPathLexer.LOOP_END_KEYWORD
                && toks[start + 1] is UnquotedString uqs && RemesPathLexer.LOOP_VAR_KEYWORDS.Contains(uqs.value));
        }

        private Obj_Pos ParseSlicer(List<object> toks, int pos, int? firstNum, int end, JQueryContext context)
        {
            var slicer = new int?[3];
            int slotsFilled = 0;
            int? lastNum = firstNum;
            while (pos < end)
            {
                object t = toks[pos];
                if (t is char tval)
                {
                    if (tval == ':')
                    {
                        slicer[slotsFilled++] = lastNum;
                        lastNum = null;
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
                    Obj_Pos npo = ParseExprFunc(toks, pos, end, context);
                    JNode numtok = (JNode)npo.obj;
                    pos = npo.pos;
                    if (numtok.type != Dtype.INT)
                    {
                        throw new ArgumentException();
                    }
                    lastNum = Convert.ToInt32(numtok.value);
                }
                catch (Exception)
                {
                    throw new RemesPathException("Found non-integer while parsing a slicer");
                }
                if (slotsFilled == 2)
                {
                    break;
                }
            }
            slicer[slotsFilled++] = lastNum;
            slicer = slicer.Take(slotsFilled).ToArray();
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

        private static (IndexerStart indStart, int pos) DetermineIndexerStart(List<object> toks, int pos, int end)
        {
            object t = PeekNextToken(toks, pos - 1, end);
            if (!(t is char d) || !INDEXER_STARTERS.Contains(d))
            {
                return (IndexerStart.NOT_AN_INDEXER, pos);
            }
            object nt = PeekNextToken(toks, pos, end);
            if (nt is char nd)
            {
                if (d == '.' && nd == '.')
                {
                    object nnt = PeekNextToken(toks, pos + 1, end);
                    if (nnt is char nnd && nnd == '[')
                        return (IndexerStart.DOUBLEDOT_SQUAREBRACE, pos + 3);
                    return (IndexerStart.DOUBLEDOT, pos + 2);
                }
                if (d == '!')
                {
                    if (nd == '.')
                    {
                        //object nnt = PeekNextToken(toks, pos + 1, end);
                        //if (nnt is char nnd && nnd == '.') // uncomment if bringing back support for negated recursive indexers
                        //{
                        //    object nnnt = PeekNextToken(toks, pos + 2, end);
                        //    if (nnnt is char nnnd && nnnd == '[')
                        //        return (IndexerStart.BANG_DOUBLEDOT_SQUAREBRACE, pos + 4);
                        //    return (IndexerStart.BANG_DOUBLEDOT, pos + 3);
                        //}
                        return (IndexerStart.BANG_DOT, pos + 2);
                    }
                    else if (nd == '[')
                        return (IndexerStart.BANG_SQUAREBRACE, pos + 2);
                    return (IndexerStart.NOT_AN_INDEXER, pos);
                }
            }
            switch (d)
            {
            case '[': return (IndexerStart.SQUAREBRACE, pos + 1);
            case '{': return (IndexerStart.CURLYBRACE, pos + 1);
            case '.': return (IndexerStart.DOT, pos + 1);
            case '>': return (IndexerStart.FORWARD_ARROW, pos + 1);
            default: return (IndexerStart.NOT_AN_INDEXER, pos);
            }
        }

        /// <summary>
        /// Parses all indexers, including projections.
        /// </summary>
        private Obj_Pos ParseIndexer(List<object> toks, int pos, int end, IndexerStart indStart, JQueryContext context)
        {
            object t = toks[pos];
            object nt;
            List<object> children = new List<object>();
            if (IndexerStart.ANY_DOT_TYPE.HasFlag(indStart))
            {
                if (t != null)
                {
                    if (t is Binop bt && bt.name == "*")
                    {
                        // it's a '*' indexer, which means select all keys/indices
                        return new Obj_Pos(new StarIndexer(), pos + 1);
                    }
                    JNode jt = (t is UnquotedString st)
                        ? new JNode(st.value, Dtype.STR, 0)
                        : (JNode)t;
                    if ((jt.type & Dtype.STR_OR_REGEX) == 0)
                    {
                        throw new RemesPathException("'.' syntax for indexers requires that the indexer be a string, " +
                                                    "regex, or '*'");
                    }
                    if (jt is JRegex jnreg)
                    {
                        children.Add(jnreg.regex);
                    }
                    else
                    {
                        children.Add((string)jt.value);
                    }
                    return new Obj_Pos(new VarnameList(children), pos + 1);
                }
            }
            else if (indStart == IndexerStart.CURLYBRACE)
            {
                return ParseProjection(toks, pos, end, context);
            }
            else if (indStart == IndexerStart.FORWARD_ARROW)
            {
                return ParseMap(toks, pos, end, context);
            }
            else if (!IndexerStart.ANY_SQUAREBRACE_TYPE.HasFlag(indStart))
            {
                throw new RemesPathException("Indexer must start with '.', '[', '{', '!', or \"->\"");
            }
            Indexer indexer = null;
            object lastTok = null;
            JNode jlastTok;
            Dtype lastType = Dtype.UNKNOWN;
            if (t is Binop b && b.name == "*")
            {
                // it was '*', indicating star indexer in squarebraces ("[*]")
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
                if (t is char d)
                {
                    if (d == ']')
                    {
                        // it's a ']' that terminates the indexer
                        if (lastTok == null)
                        {
                            throw new RemesPathException("Empty indexer");
                        }
                        if (indexer == null)
                        {
                            if ((lastType & Dtype.STR_OR_REGEX) != 0)
                            {
                                indexer = new VarnameList(children);
                            }
                            else if ((lastType & Dtype.INT_OR_SLICE) != 0)
                            {
                                indexer = new SlicerList(children);
                            }
                            else
                            {
                                // it's a boolean index of some sort, e.g. [@ > 0]
                                indexer = new BooleanIndex(lastTok);
                            }
                        }
                        if (indexer is VarnameList || indexer is SlicerList)
                        {
                            children.Add(GetSingleIndexerListValue((JNode)lastTok));
                        }
                        else if ((indexer is VarnameList && (lastType & Dtype.STR_OR_REGEX) == 0) // a non-string, non-regex in a varname list
                                || (indexer is SlicerList && (lastType & Dtype.INT_OR_SLICE) == 0))// a non-int, non-slice in a slicer list
                        {
                            throw new RemesPathException("Cannot have indexers with a mix of ints/slicers and " +
                                                         "strings/regexes");
                        }
                        return new Obj_Pos(indexer, pos + 1);
                    }
                    if (d == ',')
                    {
                        if (lastTok == null)
                        {
                            throw new RemesPathException("Comma before first token in indexer");
                        }
                        if (indexer == null)
                        {
                            if ((lastType & Dtype.STR_OR_REGEX) != 0)
                            {
                                indexer = new VarnameList(children);
                            }
                            else if ((lastType & Dtype.INT_OR_SLICE) != 0)
                            {
                                indexer = new SlicerList(children);
                            }
                        }
                        children.Add(GetSingleIndexerListValue((JNode)lastTok));
                        lastTok = null;
                        lastType = Dtype.UNKNOWN;
                        pos++;
                    }
                    else if (d == ':')
                    {
                        if (lastTok == null)
                        {
                            Obj_Pos opo = ParseSlicer(toks, pos, null, end, context);
                            lastTok = opo.obj;
                            pos = opo.pos;
                        }
                        else if (lastTok is JNode)
                        {
                            jlastTok = (JNode)lastTok;
                            if (jlastTok.type != Dtype.INT)
                            {
                                throw new RemesPathException($"Expected token other than ':' after {jlastTok} " +
                                                             $"in an indexer");
                            }
                            Obj_Pos opo = ParseSlicer(toks, pos, Convert.ToInt32(jlastTok.value), end, context);
                            lastTok = opo.obj;
                            pos = opo.pos;
                        }
                        else
                        {
                            throw new RemesPathException($"Expected token other than ':' after {lastTok} in an indexer");
                        }
                        lastType = ((JNode)lastTok).type;
                    }
                    else
                    {
                        throw new RemesPathException($"Expected token other than {t} after {lastTok} in an indexer");
                    }
                }
                else if (lastTok != null)
                {
                    throw new RemesPathException($"Consecutive indexers {lastTok} and {t} must be separated by commas");
                }
                else
                {
                    // it's a new token of some sort
                    Obj_Pos opo = ParseExprFunc(toks, pos, end, context);
                    lastTok = opo.obj;
                    pos = opo.pos;
                    lastType = ((JNode)lastTok).type;
                }
            }
            throw new RemesPathException("Unterminated indexer");
        }

        /// <summary>
        /// handles the following:<br></br>
        /// * grouping parentheses<br></br>
        /// * determining whether an unquoted string is an arg function or just a string<br></br>
        /// * parsing and applying chained indexers
        /// </summary>
        private Obj_Pos ParseExpr(List<object> toks, int pos, int end, JQueryContext context)
        {
            if (toks.Count == 0)
            {
                throw new RemesPathException("Empty query");
            }
            object t = toks[pos];
            JNode lastTok = null;
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
                int unclosedParens = 1;
                int subqueryStart = pos + 1;
                for (int subqueryEnd = subqueryStart; subqueryEnd < end; subqueryEnd++)
                {
                    object subtok = toks[subqueryEnd];
                    if (subtok is char subd)
                    {
                        if (subd == '(')
                        {
                            unclosedParens++;
                        }
                        else if (subd == ')')
                        {
                            if (--unclosedParens == 0)
                            {
                                lastTok = (JNode)ParseExprFunc(toks, subqueryStart, subqueryEnd, context).obj;
                                pos = subqueryEnd + 1;
                                break;
                            }
                        }
                    }
                }
            }
            else if (t is UnquotedString st)
            {
                string s = st.value;
                if (pos < end - 1
                    && toks[pos + 1] is char c && c == '(')
                {
                    // an unquoted string followed by an open paren
                    // *might* be an ArgFunction; we need to check
                    if (ArgFunction.FUNCTIONS.TryGetValue(s, out ArgFunction af))
                    {
                        Obj_Pos opo = ParseArgFunction(toks, pos + 1, af, end, context);
                        lastTok = (JNode)opo.obj;
                        pos = opo.pos;
                    }
                    else
                    {
                        throw new RemesPathException($"'{s}' is not the name of a RemesPath function.");
                    }
                }
                else
                {
                    lastTok = ParseNonFunctionUnquotedStr(pos, st, context);
                    pos++;
                }
            }
            else
            {
                lastTok = (JNode)t;
                pos++;
            }
            if (lastTok == null)
            {
                throw new RemesPathException("Found null where JNode expected");
            }
            (IndexerStart indStart, int indStartEndPos) = DetermineIndexerStart(toks, pos, end);
            if ((lastTok.type & Dtype.ITERABLE) != 0 || IndexerStart.PROJECTION.HasFlag(indStart))
            {
                // The last token is an iterable (in which case various indexers are allowed)
                // or the next token is the start of a projection (unlike other indexers, projections can operate on scalars too)
                var idxrs = new List<IndexerFunc>();
                pos = indStartEndPos;
                while (indStart != IndexerStart.NOT_AN_INDEXER)
                {
                    bool isRecursive = IndexerStart.ANY_DOUBLEDOT_TYPE.HasFlag(indStart);
                    bool isNegated = IndexerStart.ANY_BANG_TYPE.HasFlag(indStart);
                    if (isRecursive && isNegated)
                        throw new RemesPathException("Recursive negated indexers (of the form \"!..a\" or \"!..[g`a`]\") are not currently supported.");
                    Obj_Pos opo = ParseIndexer(toks, pos, end, indStart, context);
                    Indexer curIdxr = (Indexer)opo.obj;
                    bool isVarnameList = curIdxr is VarnameList;
                    bool isDict = isVarnameList & !isRecursive;
                    bool hasOneOption = indStart == IndexerStart.FORWARD_ARROW; // Some slicer/varname lists return one item, but the map item always does
                    bool isProjection = false;
                    if (isVarnameList || curIdxr is SlicerList)
                    {
                        List<object> children = null;
                        if (isVarnameList)
                        {
                            children = ((VarnameList)curIdxr).children;
                            // recursive search means that even selecting a single key/index could select from multiple arrays/dicts and thus get multiple results
                            if (!isRecursive && !isNegated && children.Count == 1 && children[0] is string)
                            {
                                // the indexer only selects a single key from a dict
                                // Since the key is defined implicitly by this choice, this indexer will only return the value
                                hasOneOption = true;
                            }
                        }
                        else
                        {
                            children = ((SlicerList)curIdxr).children;
                            if (!isRecursive && !isNegated && children.Count == 1 && children[0] is int)
                            {
                                // the indexer only selects a single index from an array
                                // Since the index is defined implicitly by this choice, this indexer will only return the value
                                hasOneOption = true;
                            }
                        }
                        Func<JNode, IEnumerable<object>> idxFunc;
                        if (isNegated)
                        {
                            if (isVarnameList)
                                idxFunc = ApplyNegatedVarnameList(children);
                            else
                                idxFunc = ApplyNegatedSlicerList(children);
                        }
                        else
                            idxFunc = ApplyMultiIndex(children, isVarnameList, isRecursive);
                        idxrs.Add(new IndexerFunc(idxFunc, hasOneOption, isProjection, isDict, isRecursive));
                    }
                    else if (curIdxr is BooleanIndex boodex)
                    {
                        if (isNegated)
                            throw new RemesPathException("Negated boolean indices are not supported; just invert the logic to get the same effect.");
                        JNode boodexFun = (JNode)boodex.value;
                        var idxr = new IndexerFunc(null, hasOneOption, isProjection, isDict, isRecursive);
                        idxr.idxr = idxr.ApplyBooleanIndex(boodexFun);
                        idxrs.Add(idxr);
                    }
                    else if (curIdxr is Projection proj)
                    {
                        if (isNegated)
                            throw new RemesPathException("Negated projections are not supported.");
                        Func<JNode, IEnumerable<object>> projFunc = proj.projFunc;
                        idxrs.Add(new IndexerFunc(projFunc, hasOneOption, true, false, false));
                    }
                    else
                    {
                        if (isNegated)
                            throw new RemesPathException("Negated star indexers are not supported.");
                        // it's a star indexer
                        if (isRecursive)
                            idxrs.Add(new IndexerFunc(RecursivelyFlattenIterable, false, false, false, true));
                        else
                            idxrs.Add(new IndexerFunc(ApplyStarIndexer, hasOneOption, isProjection, isDict, false));
                    }
                    (indStart, pos) = DetermineIndexerStart(toks, opo.pos, end);
                }
                if (idxrs.Count > 0)
                {
                    Func<JNode, JNode> idxrsFunc = ApplyIndexerList(idxrs);
                    // if we're indexing on a function of input, we can't evaluate the indexers at compile time
                    if (lastTok is CurJson lcur)
                    {
                        JNode idxFunc(JNode inp)
                        {
                            return idxrsFunc(lcur.function(inp));
                        }
                        return new Obj_Pos(new CurJson(lcur.type, idxFunc), pos);
                    }
                    // if a variable is referenced in the indexers (e.g., "var x = @; range(10)[:]->at(x, @ % len(x))",
                    // we also need to wait until runtime to evaluate the indexers
                    if (context.AnyVariableReferencedInRange(indStartEndPos, pos))
                    {
                        JNode idxFuncVarRef(JNode _)
                        {
                            return idxrsFunc(lastTok);
                        }
                        return new Obj_Pos(new CurJson(lastTok.type, idxFuncVarRef), pos);
                    }
                    if (lastTok is JObject lastObj)
                    {
                        return new Obj_Pos(idxrsFunc(lastObj), pos);
                    }
                    return new Obj_Pos(idxrsFunc(lastTok), pos);
                }
            }
            return new Obj_Pos(lastTok, pos);
        }

        private JNode ParseNonFunctionUnquotedStr(int tokenIndex, UnquotedString uqs, JQueryContext context)
        {
            string s = uqs.value;
            if (context.TryGetValue(tokenIndex, s, out JNode varNamedS))
                return varNamedS; // it's a variable reference
            // not a variable, just a string
            return new JNode(s);
        }

        private Obj_Pos ParseExprFunc(List<object> toks, int pos, int end, JQueryContext context)
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
                if (curtok is UnquotedString st)
                {
                    curtok = ParseNonFunctionUnquotedStr(pos, st, context);
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
                                    oldBop.precedence, oldBop.name, oldBop.isRightAssociative));
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
                    Obj_Pos opo = ParseExpr(toks, pos, end, context);
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

        private Obj_Pos ParseArgFunction(List<object> toks, int pos, ArgFunction fun, int end, JQueryContext context)
        {
            object t;
            pos++;
            int argNum = 0;
            List<JNode> args = new List<JNode>(fun.minArgs);
            if (fun.maxArgs == 0)
            {
                t = toks[pos];
                if (!(t is char d_ && d_ == ')'))
                    throw new RemesPathException($"Expected no arguments for function {fun.name} (0 args)");
                var withArgs = new ArgFunctionWithArgs(fun, args);
                return new Obj_Pos(ApplyArgFunction(withArgs), pos + 1);
            }
            JNode curArg = null;
            while (pos < end)
            {
                t = toks[pos];
                if (t is char d_ && (d_ == ',' || d_ == ')'))
                {
                    if (!(fun.maxArgs > fun.minArgs && argNum >= fun.minArgs
                    && ((d_ == ',' && argNum < fun.maxArgs - 1) // ignore an optional arg that's not the last arg. e.g., "foo(a,,1)", where the second and third args are optional.
                        || d_ == ')'))) // ignore the last arg if it is optional. e.g., "foo(a,)", where all args after the first are optional.
                        throw new RemesPathArgumentException("Omitting a required argument for a function is not allowed", argNum, fun);
                    // set defaults to optional args JavaScript-style, by simply omitting a token where the argument would normally go.
                    args.Add(new JNode());
                    argNum++;
                    pos++;
                    if (d_ == ')') // last arg was omitted and optional
                    {
                        var withargs = new ArgFunctionWithArgs(fun, args);
                        fun.PadToMaxArgs(args);
                        return new Obj_Pos(ApplyArgFunction(withargs), pos);
                    }
                    continue;
                }
                // the last Dtype in an ArgFunction's inputTypes is either the type options for the last arg
                // or the type options for every optional arg (if the function can have infinitely many args)
                Dtype typeOptions = fun.TypeOptions(argNum); 
                // Python style *args syntax; e.g. zip(*@) is equivalent to zip(@[0], @[1], @[2], ..., @[-1])
                bool spreadCurArg = t is Binop b && b.name == "*";
                if (spreadCurArg)
                    pos++;
                try
                {
                    try
                    {
                        Obj_Pos opo = ParseExprFunc(toks, pos, end, context);
                        curArg = (JNode)opo.obj;
                        pos = opo.pos;
                    }
                    catch
                    {
                        curArg = null;
                    }
                    if ((Dtype.SLICE & typeOptions) != 0)
                    {
                        object nt = PeekNextToken(toks, pos - 1, end);
                        if (nt is char nd && nd == ':')
                        {
                            int? firstNum;
                            if (curArg == null)
                            {
                                firstNum = null;
                            }
                            else
                            {
                                firstNum = Convert.ToInt32(curArg.value);
                            }
                            Obj_Pos opo = ParseSlicer(toks, pos, firstNum, end, context);
                            curArg = (JNode)opo.obj;
                            pos = opo.pos;
                        }
                    }
                    if (!spreadCurArg) // if spreading, we'll check the type of each element of curArg separately
                        fun.CheckType(curArg, argNum);
                }
                catch (Exception ex)
                {
                    if (ex is RemesPathArgumentException) throw;
                    throw new RemesPathArgumentException($"threw exception {ex}.", argNum, fun);
                }
                t = toks[pos];
                pos++;
                bool comma = false;
                bool closeParen = false;
                if (t is char d)
                {
                    comma = d == ',';
                    closeParen = d == ')';
                }
                else
                {
                    throw new RemesPathException($"Arguments of arg functions must be followed by ',' or ')', not {t}");
                }
                if (spreadCurArg)
                {
                    if (!closeParen)
                        throw new RemesPathException("There can be no arguments to a function after an array that was spread to multiple args using the '*' operator");
                    // current argument is an array being "spread", meaning that each element is being used as a separate argument
                    var spreadResult = SpreadArrayToArgFunctionArgs(args, curArg, fun);
                    return new Obj_Pos(spreadResult, pos);
                }
                else
                {
                    args.Add(curArg);
                    argNum++;
                }
                if ((argNum < fun.minArgs && !comma)
                    || (argNum == fun.maxArgs && !closeParen))
                    fun.ThrowWrongArgCount(argNum);
                if (closeParen)
                {
                    var withargs = new ArgFunctionWithArgs(fun, args);
                    fun.PadToMaxArgs(args);
                    return new Obj_Pos(ApplyArgFunction(withargs), pos);
                }
            }
            fun.ThrowWrongArgCount(argNum);
            throw new Exception("unreachable");
        }

        private JNode SpreadArrayToArgFunctionArgs(List<JNode> args, JNode curArg, ArgFunction fun)
        {
            if (curArg is CurJson cj)
            {
                Func<JNode, JNode> spreadFun = (JNode inp) =>
                {
                    var inpArr = cj.function(inp);
                    var argsCalledOnInp = args.Select(x => x is CurJson xcj ? xcj.function(inp) : x).ToList();
                    return SpreadArrayToArgFunctionArgs(argsCalledOnInp, inpArr, fun);
                };
                var spreadCj = new CurJson(fun.OutputType(curArg), spreadFun);
                return spreadCj;
            }
            var argsCpy = args.ToList();
            if (!(curArg is JArray curArr))
            {
                throw new RemesPathException($"Any function argument preceded by '*' must be an array, got type {JNode.FormatDtype(curArg.type)}");
            }
            int argNum = argsCpy.Count;
            curArr.children.ForEach(child =>
            {
                fun.CheckType(child, argNum);
                argsCpy.Add(child);
                argNum++;
            });
            if (argNum < fun.minArgs || argNum > fun.maxArgs)
                fun.ThrowWrongArgCount(argNum);
            fun.PadToMaxArgs(argsCpy);
            var withargs = new ArgFunctionWithArgs(fun, argsCpy);
            return ApplyArgFunction(withargs);
        }
        private Obj_Pos ParseProjection(List<object> toks, int pos, int end, JQueryContext context)
        {
            var children = new List<object>();
            bool isObjectProj = false;
            while (pos < end)
            {
                Obj_Pos opo = ParseExprFunc(toks, pos, end, context);
                JNode key = (JNode)opo.obj;
                pos = opo.pos;
                object nt = PeekNextToken(toks, pos - 1, end);
                if (nt is char nd)
                {
                    if (nd == ':')
                    {
                        if (children.Count > 0 && !isObjectProj)
                        {
                            throw new RemesPathException("Mixture of values and key-value pairs in object/array projection");
                        }
                        if (key.value is string keystr)
                        {
                            opo = ParseExprFunc(toks, pos + 1, end, context);
                            JNode val = (JNode)opo.obj;
                            pos = opo.pos;
                            children.Add(new KeyValuePair<string, JNode>(keystr, val));
                            isObjectProj = true;
                            nt = PeekNextToken(toks, pos - 1, end);
                            if (!(nt is char))
                            {
                                throw new RemesPathException("Key-value pairs in projection must be delimited by ',' and projections must end with '}'.");
                            }
                            nd = (char)nt;
                        }
                        else
                        {
                            throw new RemesPathException($"Object projection keys must be string, not type {JNode.FormatDtype(key.type)} (value {key.ToString()})");
                        }
                    }
                    else
                    {
                        // it's an array projection
                        children.Add(key);
                    }
                    if (nd == '}')
                    {
                        if (isObjectProj)
                        {
                            IEnumerable<object> projFunc(JNode obj)
                            {
                                foreach(object child in children)
                                {
                                    var kv = (KeyValuePair<string, JNode>)child;
                                    yield return new KeyValuePair<string, JNode>(
                                        kv.Key,
                                        kv.Value is CurJson cj
                                            ? cj.function(obj)
                                            : kv.Value.Copy()
                                    );
                                }
                            };
                            return new Obj_Pos(new Projection(projFunc), pos + 1);
                        }
                        else
                        {
                            IEnumerable<object> projFunc(JNode obj)
                            {
                                foreach (object child in children)
                                {
                                    var node = (JNode)child;
                                    yield return node is CurJson cj
                                        ? cj.function(obj)
                                        : node.Copy();
                                }
                            };
                            return new Obj_Pos(new Projection(projFunc), pos + 1);
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
        private Obj_Pos ParseMap(List<object> toks, int pos, int end, JQueryContext context)
        {
            Obj_Pos opo = ParseExprFunc(toks, pos, end, context);
            JNode val = (JNode)opo.obj;
            pos = opo.pos;
            Func<JNode, JNode> outfunc;
            if (val is CurJson cj)
                outfunc = cj.function;
            else
                outfunc = x => val.Copy();
            IEnumerable<object> iterator(JNode x) { yield return outfunc(x); }
            return new Obj_Pos(new Projection(iterator), pos);
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
            if (ex is SchemaValidationException sve)
            {
                return sve.ToString();
            }
            if (ex is DsonDumpException dde)
            {
                return $"DSON dump error: {dde.Message}";
            }
            string exstr = ex.ToString();
            Match isCast = CAST_REGEX.Match(exstr);
            if (isCast.Success)
            {
                string ogtype = "";
                string target = "";
                switch (isCast.Groups[1].Value)
                {
                    case "Object": ogtype = "JSON object"; break;
                    case "Array": ogtype = "JSON array"; break;
                    case "Node": ogtype = "JSON scalar"; break;
                    case "Char": ogtype = "character"; break;
                }
                switch (isCast.Groups[2].Value)
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
