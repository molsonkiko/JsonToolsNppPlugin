using JSON_Tools.JSON_Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JSON_Tools.Utils
{
    public static class ArrayExtensions
    {
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
            int len = source.Count;
            if (len <= 0)
            {
                yield break;
            }
            int istop, istart, istride;
            if (stride is int istride_)
            {
                istride = istride_;
                if (istride_ < 0)
                {
                    if (start is int istart_)
                    {
                        if (istart_ < -len)
                            yield break;
                        istart = ClampWithinLen(len, istart_, true);
                    }
                    else
                        istart = len - 1;

                    if (stop is int istop_)
                        istop = ClampWithinLen(len, istop_, false);
                    else
                        istop = -1;
                }
                else if (istride_ == 0)
                {
                    throw new ArgumentException("The stride parameter of a slice must be a non-zero integer, or must be left empty");
                }
                else // positive stride
                {
                    if (start is int istart_)
                    {
                        if (istart_ >= len)
                            yield break;
                        istart = ClampWithinLen(len, istart_, true);
                    }
                    else
                        istart = 0;

                    if (stop is int istop_)
                        istop = ClampWithinLen(len, istop_, false);
                    else
                        istop = len;
                }
            }
            else // stride unspecified, assumed to be 1
            {
                istride = 1;

                if (start is int istart_)
                    istart = ClampWithinLen(len, istart_, true);
                else
                    istart = 0;

                if (stop is int istop_)
                    istop = ClampWithinLen(len, istop_, false);
                else
                    istop = len;
            }
            
            if (istride < 0)
            {
                for (int ii = istart; ii > istop; ii += istride)
                {
                    yield return source[ii];
                }
            }
            else
            {
                for (int ii = istart; ii < istop; ii += istride)
                {
                    yield return source[ii];
                }
            }
        }

        /// <summary>
        /// If num is negative, use Python-style negative indices (e.g., -1 is the last element, -len is the first elememnt)
        /// Otherwise, restrict num to between 0 and len (inclusive unless isStartIdx)
        /// </summary>
        /// <param name="len"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        public static int ClampWithinLen(int len, int num, bool isStartIdx)
        {
            if (num >= len)
                return isStartIdx ? len - 1: len;
            if (num < 0)
            {
                num += len;
                if (num < 0)
                    return isStartIdx ? 0 : -1;
                return num;
            }
            return num;
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
        /// See the documentation for LazySlice with three int arguments.
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
            if (source is T[])
                return source.LazySlice(start, stop, stride).ToArray();
            if (source is List<T>)
                return source.LazySlice(start, stop, stride).ToList();
            throw new NotImplementedException("Slice extension is only implemented for strings, Lists, and arrays");
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
        /// See the documentation for LazySlice with three int arguments.
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
            return new string(source.ToCharArray().LazySlice(slicer).ToArray());
        }

        public static string Slice(this string source, int start, int stop, int stride)
        {
            return new string(source.ToCharArray().LazySlice(start, stop, stride).ToArray());
        }

        /// <summary>
        /// s_slice(x: string, sli: integer | slicer) -> string<br></br>
        /// uses Python slicing syntax.<br></br>
        /// EXAMPLES:<br></br>
        /// * s_slice(abcde, 1:-2) returns "bc"<br></br>
        /// * s_slice(abcde, :2) returns "ab"<br></br>
        /// * s_slice(abcde, -2) returns "d"<br></br>
        /// </summary>
        public static string Slice(this string source, int?[] slicer)
        {
            return new string(source.ToCharArray().LazySlice(slicer).ToArray());
        }

        /// <summary>
        /// randomize the order of the elements in arr
        /// </summary>
        public static void Shuffle<T>(this IList<T> arr)
        {
            for (int ii = 1; ii < arr.Count; ii++)
            {
                int swapWith = RandomJsonFromSchema.random.Next(0, ii + 1);
                if (swapWith < ii)
                {
                    T temp = arr[ii];
                    arr[ii] = arr[swapWith];
                    arr[swapWith] = temp;
                }
            }
        }

        /// <summary>
        /// Removes and returns the last element of a list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static T Pop<T>(this List<T> list)
        {
            int lastIdx = list.Count - 1;
            T last = list[lastIdx];
            list.RemoveAt(lastIdx);
            return last;
        }

        /// <summary>
        /// e.g., int[]{1,2,3,4,5} ->
        /// "[1, 2, 3, 4, 5]"
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static string ArrayToString<T>(this IList<T> list)
        {
            var sb = new StringBuilder();
            sb.Append('[');
            for (int ii = 0; ii < list.Count; ii++)
            {
                T x = list[ii];
                sb.Append(x.ToString());
                if (ii < list.Count - 1)
                    sb.Append(", ");
            }
            sb.Append(']');
            return sb.ToString();
        }

        /// <summary>
        /// If idx is between [0, source length) exclusive, atIdx = source[idx] and return true.<br></br>
        /// If idx is between [-(source length), -1] inclusive, atIdx = source[idx + source length] and return true.<br></br>
        /// else atIdx =  default(T) and return false
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="idx"></param>
        /// <param name="inBounds"></param>
        /// <returns></returns>
        public static bool WrappedIndex<T>(this IList<T> source, int idx, out T atIdx)
        {
            int count = source.Count;
            if (idx >= count || idx < -count)
            {
                atIdx = default(T);
                return false;
            }
            idx = idx >= 0 ? idx : idx + count;
            atIdx = source[idx];
            return true;
        }

        /// <summary>
        /// see WrappedIndex docs for lists and arrays.<br></br>
        /// Returns null if idx is out of bounds.<br></br>
        /// EXAMPLES<br></br>
        /// 1. WrappedIndex("abc", 1) -> "b"<br></br>
        /// 2. WrappedIndex("abc", -3) -> "a"<br></br>
        /// 3. WrappedIndex("abc", 4) -> null<br></br>
        /// 4. WrappedIndex("abc", -5) -> null<br></br>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="idx"></param>
        /// <param name="atIdx"></param>
        /// <returns></returns>
        public static bool WrappedIndex(this string source, int idx, out string atIdx)
        {
            int count = source.Length;
            if (idx >= count || idx < -count)
            {
                atIdx = null;
                return false;
            }
            idx = idx >= 0 ? idx : idx + count;
            atIdx = source.Substring(idx, 1);
            return true;
        }

        /// <summary>
        /// Return the first element of list if idx is not a valid index for this IList.<br></br>
        /// Otherwise return list[idx]
        /// </summary>
        public static T FirstIfOutOfBounds<T>(this IList<T> list, int idx)
        {
            return list[idx >= list.Count || idx < 0 ? 0 : idx];
        }
    }
}
