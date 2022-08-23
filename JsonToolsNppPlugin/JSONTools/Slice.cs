using System;
using System.Collections.Generic;

namespace JSON_Viewer.JSONViewer
{
    public static class SliceExtensions
    {
        // TODO: Replace these slices with code copy-pasted from
        // https://github.com/henon/SliceAndDice/blob/master/src/SliceAndDice/Shape.cs
        // Those slices are better because (a) they are more versatile and (b) THEY ARE VIEWS RATHER THAN MEM-COPIERS

        /// <summary>
        /// Allows the use of Python-style slices, where start, stop, and stride must be declared as individual paramters.<br></br>
        /// Thus e.g. arr.Slice(2, null, -1) is just like arr[slice(2, None, -1)] in Python.<br></br>
        /// This just yields the elements one by one.<br></br>
        /// If you want a function that yields a shallow copy of the sliced region of the iterable, use Slice&lt;T&gt; instead.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <param name="stride"></param>
        /// <returns></returns>
        public static IEnumerable<T> LazySlice<T>(this IList<T> source, int? start, int? stop = null, int? stride = null)
        {
            int len = (source is T[]) ? ((T[])source).Length : ((List<T>)source).Count;
            int ind;
            if (stride == 0)
            {
                throw new ArgumentException("The stride parameter of a slice must be a non-zero integer");
            }
            if (len <= 0)
            {
                yield break;
            }
            int istart, istop, istride;
            if (start != null && stop == null && stride == null)
            {
                int temp = (int)start;
                stop = temp;
                start = 0;
            }
            else if (stride == null || stride > 0)
            {
                if (start == null) { start = 0; }
                if (stop == null) { stop = len; }
            }
            else
            {
                if (start == null)
                {
                    start = len - 1;
                }
                if (stop == null)
                {
                    istride = (int)stride;
                    istart = (int)start;
                    // we want "start::-x" or "::-x" to go from start 
                    // to first element by x
                    istop = -1;
                    if (istart < 0)
                    {
                        istart += len;
                    }
                    len = (istop - istart) / istride + 1;
                    if (len % istride == 0)
                    {
                        // this is tricky! If len is divisible by stride,
                        // we would overshoot into index -1 and get
                        // an index error.
                        len -= 1;
                    }
                    for (int ii = 0; ii < len; ii++)
                    {
                        ind = istart + ii * istride;
                        yield return source[ind];
                    }
                }
            }
            istop = (stop < 0) ? len + (int)stop : (stop > len ? len : (int)stop);
            istart = (start < 0) ? len + (int)start : (int)start;
            istride = (stride == null) ? 1 : (int)stride;
            if (istart >= len && istop >= len)
            {
                yield break;
            }
            if ((istop - istart) % Math.Abs(istride) == 0)
            {
                len = (istop - istart) / istride;
            }
            else
            {
                // if the final stride would carry you out of the array, the output array is going to be
                // one larger than (stop - start)/stride, because the first element is always in the output array.
                len = (istop - istart) / istride + 1;
            }
            if (len <= 0)
            {
                yield break;
            }
            for (int ii = 0; ii < len; ii++)
            {
                ind = istart + ii * istride;
                yield return source[ind];
            }
        }

        ///<summary>
        /// Allows the use of Python-style slices, passed as strings (e.g., ":", "1::-2").<br></br>
        /// Because LazySlice is an extension method, all arrays in this namespace can use this method.<br></br>
        /// See https://stackoverflow.com/questions/509211/understanding-slicing
        /// This just yields the elements one by one.<br></br>
        /// If you want a function that yields a shallow copy of the sliced region of the iterable, use Slice&lt;T&gt; instead.
        ///</summary>
        public static IEnumerable<T> LazySlice<T>(this IList<T> source, string slicer)
        // note the "this" before the type and type.
        // that designates Slice as a new method that can be used by T[] objects
        // (i.e., arrays of any object type) using the "." syntax.
        // That's why later on we see a.Slice(":" + a.Length) even though a doesn't
        // come with the Slice method built in.
        {
            string[] parts = slicer.Split(':');
            int? start = 0, stop = 0, stride = 1;
            switch (parts.Length)
            {
                case 1: start = 0; stop = int.Parse(parts[0]); break;
                case 2:
                    if (parts[0] == "") start = null; else start = int.Parse(parts[0]);
                    if (parts[1] == "") stop = null; else stop = int.Parse(parts[1]);
                    break;
                case 3:
                    if (parts[0] == "") start = null; else start = int.Parse(parts[0]);
                    if (parts[1] == "") stop = null; else stop = int.Parse(parts[1]);
                    if (parts[2] == "") stride = null; else stride = int.Parse(parts[2]);
                    break;
            }
            foreach (T item in source.LazySlice(start, stop, stride)) { yield return item; }
        }

        /// <summary>
        /// This just yields the elements one by one.<br></br>
        /// If you want a function that yields a shallow copy of the sliced region of the iterable, use Slice&lt;T&gt; instead.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="slicer"></param>
        /// <returns></returns>
        public static IEnumerable<T> LazySlice<T>(this IList<T> source, int?[] slicer)
        {
            int? start = 0, stop = 0, stride = 1;
            switch (slicer.Length)
            {
                case 1: start = 0; stop = slicer[0]; break;
                case 2:
                start = slicer[0];
                stop = slicer[1];
                break;
                case 3:
                start = slicer[0];
                stop = slicer[1];
                stride = slicer[2];
                break;
            }
            foreach (T item in source.LazySlice(start, stop, stride)) { yield return item; }
        }

        /// <summary>
        /// Allows use of Python-style slices, except that these create a copy of the sliced object rather than a view.<br></br>
        /// For higher performance at the cost of only producing an iterator and not a new iterable, use LazySlice.<br></br>
        /// See the documentation for LazySlice with three int? arguments.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <param name="stride"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static IList<T> Slice<T>(this IList<T> source, int? start, int? stop = null, int? stride = null)
        {
            bool isarr = source is T[];
            bool islist = source is List<T>;
            int len = isarr ? ((T[])source).Length : (islist ? ((List<T>)source).Count : throw new NotImplementedException("Slice is only implemented for arrays and Lists. For slicing of other indexable iterables, use LazySlice."));
            IList<T> res;
            int source_len = len;
            int ind;
            if (stride == 0)
            {
                throw new ArgumentException("The stride parameter of a slice must be a non-zero integer");
            }
            if (len <= 0)
            {
                if (isarr) return new T[0]; else return new List<T>();
            }
            int istart, istop, istride;
            if (start != null && stop == null && stride == null)
            {
                int temp = (int)start;
                stop = temp;
                start = 0;
            }
            else if (stride == null || stride > 0)
            {
                if (start == null) { start = 0; }
                if (stop == null) { stop = len; }
            }
            else
            {
                if (start == null)
                {
                    start = len - 1;
                }
                if (stop == null)
                {
                    istride = (int)stride;
                    istart = (int)start;
                    // we want "start::-x" or "::-x" to go from start 
                    // to first element by x
                    istop = -1;
                    if (istart < 0)
                    {
                        istart += len;
                    }
                    else if (istart >= len)
                    {
                        istart = len - 1;
                    }
                    len = (istop - istart) / istride + 1;
                    if (len % istride == 0)
                    {
                        // this is tricky! If len is divisible by stride,
                        // we would overshoot into index -1 and get
                        // an index error.
                        len -= 1;
                    }
                    if (isarr)
                    {
                        res = new T[len];
                        for (int ii = 0; ii < len; ii++)
                        {
                            ind = istart + ii * istride;
                            res[ii] = source[ind];
                        }
                        return res;
                    }
                    res = new List<T>();
                    for (int ii = 0; ii < len; ii++)
                    {
                        ind = istart + ii * istride;
                        res.Add(source[ind]);
                    }
                    return res;
                }
            }
            // make sure the start isn't higher than
            istop = (stop < 0) ? len + (int)stop : (stop > len ? len : (int)stop);
            istart = (start < 0) ? len + (int)start : (int)start;
            istride = (stride == null) ? 1 : (int)stride;
            if (istart >= len && istop >= len)
            {
                if (isarr) return new T[0]; else return new List<T>();
            }
            if ((istop - istart) % Math.Abs(istride) == 0)
            {
                len = (istop - istart) / istride;
            }
            else
            {
                // if the final stride would carry you out of the array, the output array is going to be
                // one larger than (stop - start)/stride, because the first element is always in the output array.
                len = (istop - istart) / istride + 1;
            }
            if (len <= 0)
            {
                if (isarr) return new T[0]; else return new List<T>();
            }
            if (isarr)
            {
                res = new T[len];
                for (int ii = 0; ii < len; ii++)
                {
                    ind = istart + ii * istride;
                    res[ii] = source[ind];
                }
                return res;
            }
            res = new List<T>();
            for (int ii = 0; ii < len; ii++)
            {
                ind = istart + ii * istride;
                res.Add(source[ind]);
            }
            return res;
        }

        /// <summary>
        /// Allows use of Python-style slices, except that these create a copy of the sliced object rather than a view.<br></br>
        /// For higher performance at the cost of only producing an iterator and not a new iterable, use LazySlice.<br></br>
        /// See the documentation for LazySlice with a string argument
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <param name="stride"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static IList<T> Slice<T>(this IList<T> source, string slicer)
        {
            string[] parts = slicer.Split(':');
            int? start = 0, stop = 0, stride = 1;
            switch (parts.Length)
            {
                case 1: start = 0; stop = int.Parse(parts[0]); break;
                case 2:
                    if (parts[0] == "") start = null; else start = int.Parse(parts[0]);
                    if (parts[1] == "") stop = null; else stop = int.Parse(parts[1]);
                    break;
                case 3:
                    if (parts[0] == "") start = null; else start = int.Parse(parts[0]);
                    if (parts[1] == "") stop = null; else stop = int.Parse(parts[1]);
                    if (parts[2] == "") stride = null; else stride = int.Parse(parts[2]);
                    break;
            }
            return source.Slice(start, stop, stride);
        }

        /// <summary>
        /// Allows use of Python-style slices, except that these create a copy of the sliced object rather than a view.<br></br>
        /// For higher performance at the cost of only producing an iterator and not a new iterable, use LazySlice.<br></br>
        /// See the documentation for LazySlice with three int? arguments.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <param name="stride"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static IList<T> Slice<T>(this IList<T> source, int?[] slicer)
        {
            int? start = 0, stop = 0, stride = 1;
            switch (slicer.Length)
            {
                case 1: start = 0; stop = slicer[0]; break;
                case 2:
                start = slicer[0];
                stop = slicer[1];
                break;
                case 3:
                start = slicer[0];
                stop = slicer[1];
                stride = slicer[2];
                break;
            }
            return source.Slice(start, stop, stride);
        }

        public static string Slice(this string source, string slicer)
        {
            return new string((char[])source.ToCharArray().Slice(slicer));
        }

        public static string Slice(this string source, int? start, int? stop, int? stride)
        {
            return new string((char[])source.ToCharArray().Slice(start, stop, stride));
        }

        public static string Slice(this string source, int?[] slicer)
        {
            return new string((char[])source.ToCharArray().Slice(slicer));
        }
    }
}
