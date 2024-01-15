/*
Utilities for converting JSON with a "tabular" layout 
(broadly speaking, arrays of arrays, objects with keys mapping to parallel arrays, and arrays of same-format objects)
into arrays of same-format objects.
Uses an algorithm to flatten nested JSON.
*/
using System;
using System.Collections.Generic;
using System.Text;

namespace JSON_Tools.JSON_Tools
{
	public enum JsonTabularizerStrategy
    {
		/// <summary>
		/// Drills down into nested JSON until it finds one of the following:<br></br>
		/// * an array of arrays of scalars<br></br>
		/// * an array of same-format objects with scalar values<br></br>
		/// * an object with at least one key that maps to arrays of scalars<br></br>
		/// The emitted table will then have all the scalar values along the path to the table, as well as the table itself.<br></br>
		/// ========<br></br>
		/// For example, something that's already tabular, like<br></br>
		/// {"a": 1, "b": [1, 2, 3], "c": ["a", "b", "c"]}<br></br>
		/// will be tabularized to<br></br>
		/// a,b,c<br></br>
		/// 1,1,a<br></br>
		/// 1,2,b<br></br>
		/// 1,3,c<br></br>
		/// ========<br></br>
		/// but it can drill down to find the tabular part of this JSON:<br></br>
		/// [{"a": 1, "b": [1, 2, 3], "c": {"d": "y"}}, {"a": 2, "b": [4, 5, 6], "c": {"d": "z"}}]<br></br>
		/// will be tabularized to<br></br>
		/// a,b,c.d<br></br>
		/// 1,1,y<br></br>
		/// 1,2,y<br></br>
		/// 1,3,y<br></br>
		/// 2,4,z<br></br>
		/// 2,5,z<br></br>
		/// 2,6,z
		/// </summary>
		DEFAULT,
		/// <summary>
		/// If the JSON is an array of objects or an array of arrays,<br></br>
		/// recursively search each sub-object or sub-array, adding new columns to the emitted table for each path to a terminal node.<br></br>
		/// ========<br></br>
		/// This can make tables with a lot of columns, as shown in this example:<br></br>
		/// [{"a": 1, "b": [1, 2, 3], "c": {"d": "y"}}, {"a": 2, "b": [4, 5, 6], "c": {"d": "z"}}]<br></br>
		/// will be tabularized to<br></br>
		/// a,b.col1,b.col2,b.col3,c.d<br></br>
		/// 1,1,2,3,y<br></br>
		/// 2,4,5,6,z<br></br>
		/// </summary>
		FULL_RECURSIVE,
		/// <summary>
		/// This does not do recursive search.<br></br>
		/// If the JSON is:<br></br>
		/// * an array of arrays of scalars<br></br>
		/// * an array of same-format objects with scalar values<br></br>
		/// * an object with at least one key that maps to arrays of scalars<br></br>
		/// it will convert the JSON into a chosen tabular format.<br></br>
		/// ========<br></br>
		/// For example, {"a": 1, "b": [1, 2, 3], "c": ["a", "b", "c"]}<br></br>
		/// will be tabularized to<br></br>
		/// a,b,c<br></br>
		/// 1,1,a<br></br>
		/// 1,2,b<br></br>
		/// 1,3,c<br></br>
		/// ========<br></br>
		/// but [{"a": 1, "b": [1, 2, 3]}, {"a": 2, "b": [4, 5, 6]}]<br></br>
		/// will NOT be tabularized because it is an array of objects that don't contain all scalars.
		/// </summary>
		NO_RECURSION,
		/// <summary>
		/// This approach is similar to NO_RECURSION, but instead of refusing to run if it encounters<br></br>
		/// non-scalars in the table, it stringifies them.<br></br>
		/// =========<br></br>
		/// For example, consider<br></br>
		/// [{"a": 1, "b": [1, 2, 3]}, {"a": 2, "b": [4, 5, 6]}]<br></br>
		/// This will be tabularized to<br></br>
		/// a,b<br></br>
		/// 1,"[1, 2, 3]"<br></br>
		/// 2,"[4, 5, 6]"<br></br>
		/// =========<br></br>
		/// Meanwhile, {"a": 1, "b": [1, 2, 3], "c": ["a", "b", "c"]}<br></br>
		/// will be tabularized to<br></br>
		/// a,b,c<br></br>
		/// 1,1,a<br></br>
		/// 1,2,b<br></br>
		/// 1,3,c<br></br>
		/// which is the same behavior as NO_RECURSION and DEFAULT.
		/// </summary>
		STRINGIFY_ITERABLES,
    }

	public enum JsonFormat : byte
    {
		/// <summary>
		/// Not a tabular JSON format, an array or object or scalars, or a row.
		/// </summary>
		BAD = 1,
		/// <summary>
		/// an array containing only objects with scalar values
		/// </summary>
		REC = 2,
		/// <summary>
		/// an object or array with only scalar values
		/// </summary>
		ROW = 4,
		/// <summary>
		/// a scalar (bool, float, int, date, or string)
		/// </summary>
		SCAL = 8,
		/// <summary>
		/// An array of arrays of scalars
		/// </summary>
		SQR = 16,
		/// <summary>
		/// an object where every value is either a scalar or an array of scalars
		/// </summary>
		TAB = 32,
		// COMPOSITE TYPES
		ANY_TABLE = SQR | REC | TAB
    }
	public class JsonTabularizer
	{
		public JsonTabularizerStrategy strategy { get; set; }
		private JsonFormat _outputFormat = JsonFormat.REC;
		public JsonFormat outputFormat
		{
			get { return _outputFormat; }
			set
			{
				if ((value & JsonFormat.ANY_TABLE) != 0) _outputFormat = value;
				else throw new ArgumentException("The output format of the JsonTabularizer must be one of REC, SQR, or TAB");
			}
		}

		public JsonTabularizer(JsonTabularizerStrategy strategy = JsonTabularizerStrategy.DEFAULT) // ,
							// JsonFormat outputFormat = JsonFormat.REC)
							// don't worry about outputFormat, because other output formats aren't supported
		{
			this.strategy = strategy;
			//if ((outputFormat & JsonFormat.ANY_TABLE) != 0) _outputFormat = outputFormat;
			//else throw new ArgumentException("The output format of the JsonTabularizer must be one of REC, SQR, or TAB");
		}

		/// <summary>
		/// Determines whether a JSON schema represents a scalar (<b>"scal"</b>),<br></br>
		/// an array of arrays of scalars (<b>"sqr"</b>, e.g. [[1, 2], [3, 4]])<br></br>
		/// an array containing only objects with scalar values (<b>"rec"</b>, e.g. [{"a": 1, "b": "a"}, {"a": 2, "b": "b"}])<br></br>
		/// an object or array with only scalar values (<b>"row"</b>, e.g. [1, 2] or {"a": 1})<br></br>
		/// or something else (<b>"bad"</b>, e.g. [{"a": [1]}, [1, 2]]) 
		/// </summary>
		/// <param name="schema"></param>
		/// <returns></returns>
		public JsonFormat ClassifySchema(Dictionary<string, object> schema)
		{
			Dtype tipe = (Dtype)schema["type"];
			if ((tipe & Dtype.SCALAR) != 0)
			{
				return JsonFormat.SCAL;
			}
			if (tipe == Dtype.ARR)
			{
				// it's tabular if it contains only arrays of scalars
				// or only objects where all values are scalar
				if (!schema.TryGetValue("items", out object itemsObj))
					return JsonFormat.ROW; // an empty array is a row
				var items = (Dictionary<string, object>)itemsObj;
				bool itemHasType = items.TryGetValue("type", out object itemTypes);
				if (!itemHasType)
				{
					// this is because it's an "anyOf" type, where it can have some non-scalars and some scalars as values.
					// we can't tabularize it so we call it "bad"
					return JsonFormat.BAD;
				}
				Dtype itemDtype = (Dtype)itemTypes;
				if ((itemDtype & Dtype.SCALAR) != 0)
				{
					// it's an array where all elements have the same (scalar) type
					return JsonFormat.ROW;
				}
				if (itemDtype == Dtype.ARR)
				{
					// it's an array of arrays
					if (!items.TryGetValue("items", out object subitemsObj))
					{
						// an array containing only empty arrays would be a table
						// with no columns, so this is BAD
						return JsonFormat.BAD;
					}
					Dictionary<string, object> subitems = (Dictionary<string, object>)subitemsObj;
					bool subarrHasType = subitems.TryGetValue("type", out object subarrTypes);
					if (!subarrHasType)
					{
						// this is an array containing a mix of scalars and
						// non-scalars; BAD!
						return JsonFormat.BAD;
					}
					if (((Dtype)subarrTypes & Dtype.ITERABLE) == 0)
					{
						// an array of arrays of homogeneous scalars (e.g., [[1, 2], [3, 4]])
						return JsonFormat.SQR;
					}
					// an array of arrays of non-scalars
					return JsonFormat.BAD;
				}
				var subprops = (Dictionary<string, object>)items["properties"];
				foreach (object prop in subprops.Values)
				{
					var dprop = (Dictionary<string, object>)prop;
					bool hasSubtipe = dprop.TryGetValue("type", out object subtipe);
					if (!hasSubtipe)
					{
						return JsonFormat.BAD; // it's a dict with a mixture of non-scalar types - bad! 
					}
					if (((Dtype)subtipe & Dtype.ITERABLE) != 0)
					{
						// it's a dict containing non-scalar values
						return JsonFormat.BAD;
					}
				}
				// an array of dicts with only scalar values
				return JsonFormat.REC;
			}
			if (!schema.ContainsKey("properties"))
			{
				return JsonFormat.BAD;
			}
			// it's an object
			JsonFormat cls = JsonFormat.ROW;
			var props = (Dictionary<string, object>)schema["properties"];
			foreach (object prop in props.Values)
			{
				var dprop = (Dictionary<string, object>)prop;
				bool hasSubtipe = dprop.TryGetValue("type", out object subtipeObj);
				var subtipe = (Dtype)subtipeObj;
				if (!hasSubtipe)
				{
					return JsonFormat.BAD; // it's a dict with a mixture of non-scalar types - bad! 
				}
				if (subtipe == Dtype.OBJ)
				{
					Dictionary<string, object> subprops = (Dictionary<string, object>)dprop["properties"];
					foreach (object subprop in subprops.Values)
					{
						Dictionary<string, object> dsubprop = (Dictionary<string, object>)subprop;
						bool subpropHasType = dsubprop.TryGetValue("type", out object subproptipe);
						if (!subpropHasType)
						{
							// this key of the dictionary has a mixture of nonscalar types
							return JsonFormat.BAD;
						}
						if (((Dtype)subproptipe & Dtype.ARR_OR_OBJ) != 0)
						{
							// this key of the dictionary has nonscalar type(s)
							return JsonFormat.BAD;
						}
						continue;
					}
				}
				if (subtipe != Dtype.ARR)
				{
					// it's OK for a table to have some scalar values and some list values.
					// the scalar values are just copy-pasted into each row for that table
					continue;
				}
				if (!dprop.TryGetValue("items", out object subitems))
				{
					// The array has no "items" keyword, which can happen
					// when there are empty arrays.
					// This is fine.
					continue;
				}
				bool subarrHasType = ((Dictionary<string, object>)subitems).TryGetValue("type", out object subarrTipe);
				if (!subarrHasType)
					return JsonFormat.BAD;
				if (((Dtype)subarrTipe & Dtype.ARR_OR_OBJ) != 0)
				{
					// it's an array containing some non-scalars
					return JsonFormat.BAD;
				}
				// it must be a subarray filled with scalars, so now the presumptive type is "tab"
				// the "tab" type represents a dict where some keys map to scalars and others map to lists.
				// e.g. {"a": [1, 2, 3], "b": ["a", "b", "c"], "c": 3.0} is a tab.
				cls = JsonFormat.TAB;
			}
			// by now we've determined that the dict either contains all scalars (or dicts with all scalars)
			// in which case it is a "row"
			// OR it contains a mixture of scalars and all-scalar arrays, in which case it is a "tab".
			return cls;
		}


		private void FindTabsInSchemaHelper(Dictionary<string, object> schema, List<object> path, Dictionary<string, JsonFormat> tabPaths)
		{
			JsonFormat cls = ClassifySchema(schema);
			if (cls == JsonFormat.SCAL || cls == JsonFormat.ROW)
			{
				// scalars and rows can't have tables as children
				return;
			}
			if ((cls & JsonFormat.ANY_TABLE) != 0)
			{
				JArray patharr = new JArray();
				foreach (object node in path)
				{
					if (node is int && (int)node == 0)
					{
						patharr.children.Add(new JNode(0L, Dtype.INT, 0));
					}
					else
					{
						patharr.children.Add(new JNode((string)node, Dtype.STR, 0));
					}
				}
				tabPaths[patharr.ToString()] = cls;
				return;
			}
			// by process of elimination, it's bad, but maybe it contains tables
			Dtype tipe = (Dtype)schema["type"];
			if (tipe == Dtype.ARR)
			{
				// it's an array; we'll search recursively at each index for tables
				if (!schema.TryGetValue("items", out object itemsObj))
					return; // no "items" keyword only happens for empty arrays
				var items = (Dictionary<string, object>)itemsObj;
				List<object> newpath = new List<object>(path);
				// use 0 as a placeholder for all indices in arrays
				// the important thing is that it's not a string
				newpath.Add(0);
				if (items.TryGetValue("anyOf", out object anyof))
				{
					List<object> danyof = (List<object>)anyof;
					foreach (object subschema in danyof)
					{
						FindTabsInSchemaHelper((Dictionary<string, object>)subschema, newpath, tabPaths);
					}
				}
				else
				{
					FindTabsInSchemaHelper(items, newpath, tabPaths);
				}
				return;
			}
			// it's a dict; we'll search recursively beneath each key for tables
			var props = (Dictionary<string, object>)schema["properties"];
			foreach (string k in props.Keys)
			{
				List<object> newpath = new List<object>(path) { k };
				FindTabsInSchemaHelper((Dictionary<string, object>)props[k], newpath, tabPaths);
			}
		}

		public Dictionary<string, JsonFormat> FindTabsInSchema(Dictionary<string, object> schema)
		{
			var tabPaths = new Dictionary<string, JsonFormat>();
			FindTabsInSchemaHelper(schema, new List<object>(), tabPaths);
			return tabPaths;
		}

		/// <summary>
		/// hangingRow: a dict where some values are scalars and others are
		/// dicts with only scalar values.
		/// Returns: a new dict where the off-hanging dict at key k has been merged into
		/// the original by adding &lt;k&gt;&lt;keySep&gt;&lt;key in hanger&gt; = &lt;val in hanger&gt;
		/// for each key, val in hanger.
		/// </summary>
		/// <param name="hangingRow"></param>
		/// <param name="keySep"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		private Dictionary<string, JNode> ResolveHang(Dictionary<string, JNode> hangingRow, string keySep = ".")
		{
			var result = new Dictionary<string, JNode>();
			foreach (KeyValuePair<string, JNode> kv in hangingRow)
			{
				if (kv.Value is JObject dv)
				{
					foreach (KeyValuePair<string, JNode> subkv in dv.children)
					{
						string newKey = $"{kv.Key}{keySep}{subkv.Key}";
						if (result.ContainsKey(newKey))
						{
							throw new ArgumentException($"Attempted to create hanging row with synthetic key {newKey}, but {newKey} was already a key in {hangingRow}");
						}
						result[newKey] = subkv.Value;
					}
				}
				else
				{
					result[kv.Key] = kv.Value;
				}
			}
			return result;
		}

		private string FormatKey(string key, string superKey, string keySep = ".")
		{
			return superKey.Length == 0 ? key : $"{superKey}{keySep}{key}";
		}

		private void AnyTableToRecord(JNode obj,
									   JsonFormat cls,
									   Dictionary<string, JNode> restOfRow,
									   List<JNode> result,
									   List<string> superKeylist,
									   string keySep)
		{
			string superKey = string.Join(keySep, superKeylist);
			Func<string, string> keyformat = (string x) => FormatKey(x, superKey, keySep);
			if (cls == JsonFormat.REC)
			{
				JArray aobj = (JArray)obj;
				// arrays of flat objects - this is what we're aiming for
				foreach (object rec in aobj.children)
				{
					var newrec = new Dictionary<string, JNode>(restOfRow);
					JObject orec = (JObject)rec;
					foreach (KeyValuePair<string, JNode> kv in orec.children)
					{
						newrec[keyformat(kv.Key)] = kv.Value;
					}
					result.Add(new JObject(0, newrec));
				}
			}
			else if (cls == JsonFormat.TAB)
			{
				// a dict mapping some number of keys to scalars and the rest to arrays
				// some keys may be mapped to hanging all-scalar dicts and not scalars
				JObject oobj = (JObject)obj;
				var arrDict = new Dictionary<string, JArray>();
				var notArrDict = new Dictionary<string, JNode>();
				int lenArr = 0;
				foreach (KeyValuePair<string, JNode> kv in oobj.children)
				{
					if (kv.Value is JArray varr)
					{
						arrDict[kv.Key] = varr;
						if (varr.Length > lenArr) { lenArr = varr.Length; }
					}
					else
					{
						notArrDict[keyformat(kv.Key)] = kv.Value;
					}
				}
				notArrDict = ResolveHang(notArrDict, keySep);
				if (lenArr == 0)
                {
					// A 0-length array should be dealt with by adding a single row with an empty value
					// for the array's parent key
					foreach (string k in arrDict.Keys)
						notArrDict[k] = new JNode("", Dtype.STR, 0);
					result.Add(new JObject(0, notArrDict));
					return;
                }
				for (int ii = 0; ii < lenArr; ii++)
				{
					Dictionary<string, JNode> newrec = new Dictionary<string, JNode>(restOfRow);
					foreach (string k in arrDict.Keys)
					{
						JArray v = arrDict[k];
						// if one array is longer than others, the missing values from the short rows are filled by
						// empty strings
						newrec[keyformat(k)] = ii >= v.Length ? new JNode("", Dtype.STR, 0) : v[ii];
					}
					foreach (KeyValuePair<string, JNode> kv in notArrDict)
					{
						newrec[kv.Key] = kv.Value;
					}
					result.Add(new JObject(0, newrec));
				}
			}
			else if (cls == JsonFormat.SQR)
			{
				// an array of arrays of scalars
				JArray aobj = (JArray)obj;
				foreach (JNode arr in aobj.children)
				{
					var newrec = new Dictionary<string, JNode>(restOfRow);
					List<JNode> children = ((JArray)arr).children;
					for (int ii = 0; ii < children.Count; ii++)
					{
						newrec[keyformat($"col{ii + 1}")] = children[ii];
					}
					result.Add(new JObject(0, newrec));
				}
			}
		}

		/// <summary>
		/// Uses recursive search within each element of an array to find a table.<br></br>
		/// So if there's a 3-element array within an object within a 2-element array,<br></br>
		/// the resulting table will have 6 rows.<br></br>
		/// This function is used by the DEFAULT and NO_RECURSION JsonTabularizerStrategy's<br></br>
		/// but the NO_RECURSION strategy does not try to do recursive search.
		/// </summary>
		/// <param name="json"></param>
		/// <param name="cls"></param>
		/// <param name="depth"></param>
		/// <param name="tabPath"></param>
		/// <param name="result"></param>
		/// <param name="restOfRow"></param>
		/// <param name="superKeylist"></param>
		/// <param name="keySep"></param>
		private void BuildTableHelper_DEFAULT(JNode json,
									JsonFormat cls,
									int depth,
									JArray tabPath,
									List<JNode> result,
									Dictionary<string, JNode> restOfRow,
									List<string> superKeylist,
									string keySep)
		{
			var newRestOfRow = new Dictionary<string, JNode>(restOfRow);
			if (depth == tabPath.Length)
			{
				AnyTableToRecord(json, cls, newRestOfRow, result, superKeylist, keySep);
				return;
			}
			else
			{
				if (json is JArray arr)
				{
					foreach (JNode subobj in arr.children)
					{
						BuildTableHelper_DEFAULT(subobj, cls, depth + 1, tabPath, result, newRestOfRow, superKeylist, keySep);
					}
				}
				else
				{
					string newKey = (string)tabPath[depth].value;
					string superKey = string.Join(keySep, superKeylist);
					Dictionary<string, JNode> children = ((JObject)json).children;
					foreach (KeyValuePair<string, JNode> kv in children)
					{
						if (kv.Key == newKey) continue;
						string newk = FormatKey(kv.Key, superKey, keySep);
						if (kv.Value is JObject ov)
						{
							foreach (KeyValuePair<string, JNode> subkv in ov.children)
							{
								// add hanging scalar dicts to restOfRow
								if (!(subkv.Value is JArray || subkv.Value is JObject))
								{
									newRestOfRow[$"{newk}{keySep}{subkv.Key}"] = subkv.Value;
								}
							}
						}
						else if (kv.Value.type != Dtype.ARR)
						{
							newRestOfRow[newk] = kv.Value;
						}
					}
					superKeylist.Add(newKey);
					BuildTableHelper_DEFAULT(children[newKey], cls, depth + 1, tabPath, result, newRestOfRow, superKeylist, keySep);
					superKeylist.RemoveAt(superKeylist.Count - 1);
				}
			}
		}

		/// <summary>
		/// Recursively searches a JSON object/array and all subobjects/subarrays<br></br>
		/// and makes a flattened long object (which is passed in as restOfRow).<br></br>
		/// For example, this would take {"a": [1, 2, 3], "b": {"c": 1, "d": [1, 2]}, "e": "scal"}<br></br>
		/// and make the flattened row<br></br>
		/// {"a.col1": 1, "a.col2": 2, "a.col3": 3, "b.c": 1, "b.d.col1": 1, "b.d.col2": 2, "e": "scal"}
		/// </summary>
		/// <param name="row"></param>
		/// <param name="keySep"></param>
		/// <param name="restOfRow"></param>
		/// <param name="superKeylist"></param>
		private void FlattenSingleRow_FULL_RECURSIVE(JNode row, string keySep, Dictionary<string, JNode> restOfRow, List<string> superKeylist)
		{
			string superKey = string.Join(keySep, superKeylist);
			Func<string, string> keyformat = (string x) => FormatKey(x, superKey, keySep);
			if (row is JArray arow)
			{
				for (int ii = 0; ii < arow.Length; ii++)
				{
					JNode child = arow[ii];
					string colname = $"col{ii + 1}";
					if (child is JArray || child is JObject)
					{
						superKeylist.Add(colname);
						FlattenSingleRow_FULL_RECURSIVE(child, keySep, restOfRow, superKeylist);
						superKeylist.RemoveAt(superKeylist.Count - 1);
					}
					else
					{
						string key = keyformat(colname);
						restOfRow[key] = child;
					}
				}
			}
			else if (row is JObject orow)
			{
				foreach (KeyValuePair<string, JNode> rowkv in orow.children)
				{
					if (rowkv.Value is JArray || rowkv.Value is JObject)
					{
						superKeylist.Add(rowkv.Key);
						FlattenSingleRow_FULL_RECURSIVE(rowkv.Value, keySep, restOfRow, superKeylist);
						superKeylist.RemoveAt(superKeylist.Count - 1);
					}
					else
					{
						restOfRow[keyformat(rowkv.Key)] = rowkv.Value;
					}
				}
			}
			else
			{
				restOfRow[superKey] = row;
			}
		}

		/// <summary>
		/// Given that obj is an array of arrays or an array of objects,<br></br>
		/// creates an array of objects,<br></br>
		/// where each subobject "row" is a flattened version of a subarray/subobject in obj.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="result"></param>
		/// <param name="keySep"></param>
		private void BuildTableHelper_FULL_RECURSIVE(JNode obj,
												List<JNode> result,
												string keySep = ".")
		{
			foreach (JNode child in ((JArray)obj).children)
			{
				Dictionary<string, JNode> row = new Dictionary<string, JNode>();
				FlattenSingleRow_FULL_RECURSIVE(child, keySep, row, new List<string>());
				result.Add(new JObject(0, row));
			}
		}

		/// <summary>
		/// Given that obj is an array of arrays or an array of objects<br></br>
		/// creates an array of objects,<br></br>
		/// where each subobject row is:<br></br>
		/// * A scalar, if the corresponding entry in the source subarray/subobject was a scalar<br></br>
		/// * the stringified version of the corresponding entry in the source subarray/subobject, if it was an iterable.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="schema"></param>
		/// <param name="keySep"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		private void BuildTableHelper_STRINGIFY_ITERABLES(JNode obj, List<JNode> result, string keySep = ".")
		{
			if (obj is JArray)
            {
				foreach (JNode child in ((JArray)obj).children)
				{
					Dictionary<string, JNode> row = new Dictionary<string, JNode>();
					if (child is JArray achild)
					{
						for (int ii = 0; ii < achild.Length; ii++)
						{
							string key = $"col{ii + 1}";
							JNode subchild = achild[ii];
							if (subchild is JArray || subchild is JObject)
							{
								row[key] = new JNode(subchild.ToString(), Dtype.STR, 0); // .Replace("\"", "\\\""), Dtype.STR, 0);
							}
							else
							{
								row[key] = subchild;
							}
						}
					}
					else if (child is JObject ochild)
					{
						foreach (KeyValuePair<string, JNode> subkv in ochild.children)
						{
							if (subkv.Value is JArray || subkv.Value is JObject)
							{
								row[subkv.Key] = new JNode(subkv.Value.ToString(), Dtype.STR, 0); //.Replace("\"", "\\\""), Dtype.STR, 0);
							}
							else
							{
								row[subkv.Key] = subkv.Value;
							}
						}
					}
					result.Add(new JObject(0, row));
				}
				return;
			}
			JObject oobj = (JObject)obj;
			// obj is a dict mapping some number of keys to scalars and the rest to arrays
			// some keys may be mapped to hanging all-scalar dicts and not scalars
			var arrDict = new Dictionary<string, JArray>();
			var notArrDict = new Dictionary<string, JNode>();
			int lenArr = 0;
			foreach (KeyValuePair<string, JNode> kv in oobj.children)
			{
				if (kv.Value is JArray varr)
				{
					arrDict[kv.Key] = varr;
					if (varr.Length > lenArr) { lenArr = varr.Length; }
				}
				else
				{
					notArrDict[kv.Key] = kv.Value;
				}
			}
			notArrDict = ResolveHang(notArrDict, keySep);
			// for each array child, iterate through the arrays in parallel,
			// and make the i^th object in each record contain the following:
			// * all the scalar values of the non-array children in the object
			// * if the child is an array:
			//     - if the array is shorter than i, an empty string
			//     - if the i^th element is an iterable, the stringified form of that iterable
			//     - if the i^th element is a scalar, that scalar
			for (int ii = 0; ii < lenArr; ii++)
			{
				Dictionary<string, JNode> newrec = new Dictionary<string, JNode>();
				foreach (string k in arrDict.Keys)
				{
					JArray v = arrDict[k];
					// if one array is longer than others, the missing values from the short rows
					// are filled by empty strings
					if (ii >= v.Length)
						newrec[k] = new JNode("", Dtype.STR, 0);
                    else
                    {
						JNode subchild = v[ii];
						if (subchild is JArray subarr)
							newrec[k] = new JNode(subarr.ToString(), Dtype.STR, 0);
						else if (subchild is JObject subobj)
							newrec[k] = new JNode(subobj.ToString(), Dtype.STR, 0);
						else
							newrec[k] = subchild;
                    }
				}
				// now add in all the non-array values
				foreach (string k in notArrDict.Keys)
					newrec[k] = notArrDict[k];
				result.Add(new JObject(0, newrec));
			}
		}

		public JArray BuildTable(JNode obj, Dictionary<string, object> schema, string keySep = ".")
		{
			JsonParser jsonParser = new JsonParser();
			List<JNode> result = new List<JNode>();
			JArray path = new JArray();
			JsonFormat cls = JsonFormat.BAD;
			if (strategy == JsonTabularizerStrategy.NO_RECURSION)
			{
				cls = ClassifySchema(schema);
				if ((cls & JsonFormat.ANY_TABLE) == 0)
				{
					throw new ArgumentException($"This JSON does not have a tabular format, and BuildTable doesn't know how to proceed.");
				}
				BuildTableHelper_DEFAULT(obj, cls, 0, path, result, new Dictionary<string, JNode>(), new List<string>(), keySep);
				return new JArray(0, result);
			}
			else if (strategy == JsonTabularizerStrategy.FULL_RECURSIVE)
			{
				if (!(obj is JArray))
				{
					throw new ArgumentException($"Full recursive strategy does not work on non-array JSON.");
				}
				BuildTableHelper_FULL_RECURSIVE(obj, result, keySep);
			}
			else if (strategy == JsonTabularizerStrategy.STRINGIFY_ITERABLES)
			{
				BuildTableHelper_STRINGIFY_ITERABLES(obj, result);
			}
			else
			{
				// default strategy - find tables in the schema, and recursively add everything that's on the path
				// to the tables.
				Dictionary<string, JsonFormat> tabPaths = FindTabsInSchema(schema);
				if (tabPaths.Count == 0)
				{
					return new JArray();
				}
				if (tabPaths.Count > 1)
				{
					throw new ArgumentException($"This JSON contains {tabPaths.Count} possible tables, and BuildTable doesn't know how to proceed.");
				}
				foreach (string pathstr in tabPaths.Keys)
				{
					JsonFormat clsname = tabPaths[pathstr];
					jsonParser.Reset();
					path = jsonParser.ParseArray(pathstr, 0);
					cls = clsname;
				}
				BuildTableHelper_DEFAULT(obj, cls, 0, path, result, new Dictionary<string, JNode>(), new List<string>(), keySep);
			}
			return new JArray(0, result);
		}

        /// <summary>
        /// If string s contains the delimiter, '\r', '\n', or a literal quote character, append (the string wrapped in quotes) to sb.<br></br>
        /// If s contains literal quote character, it is escaped by doubling it up according to the CSV RFC 4180 (https://www.ietf.org/rfc/rfc4180.txt)<br></br>
        /// Otherwise, append s to sb unchanged
        /// </summary>
        /// <param name="delim">CSV delimiter ('\x00' if not specified)</param>
        /// <param name="quote">CSV quote char ('\x00' if not specified)</param>
        /// <returns></returns>
        public static void ApplyQuotesIfNeeded(StringBuilder sb, string s, char delim, char quote)
		{
			if (s.IndexOfAny(new char[] {delim, '\r', '\n', quote}) >= 0)
			{
				sb.Append(quote);
				for (int ii = 0; ii < s.Length; ii++)
				{
					char c = s[ii];
					sb.Append(c);
					if (c == quote)
                        sb.Append(quote);
				}
				sb.Append(quote);
			}
			else sb.Append(s);
		}

        /// <summary>
        /// append the CSV (RFC 4180 adherent, using quote character = quote, delimiter = delim) representation of jnode's value to sb.
        /// </summary>
        /// <param name="delim">CSV delimiter ('\x00' if not specified)</param>
        /// <param name="quote">CSV quote char ('\x00' if not specified)</param>
        /// <param name="boolsAsInts">if true, represent true as "1" and false as "0". If false, represent them as "true" and "false" respectively</param>
        public static void CsvStringToSb(StringBuilder sb, JNode jnode, char delim, char quote, bool boolsAsInts)
		{
			string val;
            switch (jnode.type)
            {
            case Dtype.STR:
                val = (string)jnode.value;
                break; // only apply quotes if internal delims, quotes, or newlines
            case Dtype.DATE:
                val = ((DateTime)jnode.value).ToString("yyyy-MM-dd");
                break;
            case Dtype.DATETIME:
                val = ((DateTime)jnode.value).ToString("yyyy-MM-dd hh:mm:ss");
                break;
            case Dtype.NULL:
                return; // nulls should be empty entries
            case Dtype.BOOL:
				sb.Append((bool)jnode.value
					? (boolsAsInts ? "1" : "true")
					: (boolsAsInts ? "0" : "false"));
                return;
            default:
                val = jnode.ToString();
                break;
            }
			ApplyQuotesIfNeeded(sb, val, delim, quote);
        }

		public string TableToCsv(JArray table, char delim = ',', char quote = '"', string newline = "\n", string[] header = null, bool boolsAsInts = false)
		{
			// allow the user to supply their own column order. If they don't, just alphabetically sort colnames
			if (header == null)
			{
				HashSet<string> allKeys = new HashSet<string>();
				foreach (JNode child in table.children)
				{
					foreach (string key in ((JObject)child).children.Keys)
					{
						allKeys.Add(key);
					}
				}
				header = new string[allKeys.Count];
				int ii = 0;
				foreach (string key in allKeys)
				{
					header[ii++] = key;
				}
				Array.Sort(header);
			}
			StringBuilder sb = new StringBuilder();
			for (int ii = 0; ii < header.Length; ii++)
			{
				string col = header[ii];
				ApplyQuotesIfNeeded(sb, JNode.StrToString(col, false), delim, quote);
				if (ii < header.Length - 1) sb.Append(delim);
			}
			sb.Append(newline);
			foreach (JNode row in table.children)
			{
				JObject orow = (JObject)row;
				for (int ii = 0; ii < header.Length; ii++)
				{
					string col = header[ii];
					if (!orow.children.TryGetValue(col, out JNode jnode))
					{
						// sometimes the row might have a missing key.
						// in that case, just leave an empty entry, and add a delimiter if needed.
						if (ii < header.Length - 1) sb.Append(delim);
						continue;
					}
					CsvStringToSb(sb, jnode, delim, quote, boolsAsInts);
					if (ii < header.Length - 1) sb.Append(delim);
				}
				sb.Append(newline);
			}
			return sb.ToString();
		}
	}
}