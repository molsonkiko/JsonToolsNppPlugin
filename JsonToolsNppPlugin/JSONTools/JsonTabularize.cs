/*
Utilities for converting JSON with a "tabular" layout 
(broadly speaking, arrays of arrays, objects with keys mapping to parallel arrays, and arrays of same-format objects)
into arrays of same-format objects.
Uses an algorithm to flatten nested JSON.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JSON_Tools.Utils;

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
		private JsonFormat _output_format = JsonFormat.REC;
		public JsonFormat output_format
		{
			get { return _output_format; }
			set
			{
				if ((value & JsonFormat.ANY_TABLE) != 0) _output_format = value;
				else throw new ArgumentException("The output format of the JsonTabularizer must be one of REC, SQR, or TAB");
			}
		}

		public JsonTabularizer(JsonTabularizerStrategy strategy = JsonTabularizerStrategy.DEFAULT) // ,
							// JsonFormat output_format = JsonFormat.REC)
							// don't worry about output_format, because other output formats aren't supported
		{
			this.strategy = strategy;
			//if ((output_format & JsonFormat.ANY_TABLE) != 0) _output_format = output_format;
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
				if (!schema.TryGetValue("items", out object items_obj))
					return JsonFormat.ROW; // an empty array is a row
				var items = (Dictionary<string, object>)items_obj;
				bool item_has_type = items.TryGetValue("type", out object item_types);
				if (!item_has_type)
				{
					// this is because it's an "anyOf" type, where it can have some non-scalars and some scalars as values.
					// we can't tabularize it so we call it "bad"
					return JsonFormat.BAD;
				}
				Dtype item_dtype = (Dtype)item_types;
				if ((item_dtype & Dtype.SCALAR) != 0)
				{
					// it's an array where all elements have the same (scalar) type
					return JsonFormat.ROW;
				}
				if (item_dtype == Dtype.ARR)
				{
					// it's an array of arrays
					if (!items.TryGetValue("items", out object subitems_obj))
					{
						// an array containing only empty arrays would be a table
						// with no columns, so this is BAD
						return JsonFormat.BAD;
					}
					Dictionary<string, object> subitems = (Dictionary<string, object>)subitems_obj;
					bool subarr_has_type = subitems.TryGetValue("type", out object subarr_types);
					if (!subarr_has_type)
					{
						// this is an array containing a mix of scalars and
						// non-scalars; BAD!
						return JsonFormat.BAD;
					}
					if (((Dtype)subarr_types & Dtype.ITERABLE) == 0)
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
					bool has_subtipe = dprop.TryGetValue("type", out object subtipe);
					if (!has_subtipe)
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
				bool has_subtipe = dprop.TryGetValue("type", out object subtipe_obj);
				var subtipe = (Dtype)subtipe_obj;
				if (!has_subtipe)
				{
					return JsonFormat.BAD; // it's a dict with a mixture of non-scalar types - bad! 
				}
				if (subtipe == Dtype.OBJ)
				{
					Dictionary<string, object> subprops = (Dictionary<string, object>)dprop["properties"];
					foreach (object subprop in subprops.Values)
					{
						Dictionary<string, object> dsubprop = (Dictionary<string, object>)subprop;
						bool subprop_has_type = dsubprop.TryGetValue("type", out object subproptipe);
						if (!subprop_has_type)
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
				bool subarr_has_type = ((Dictionary<string, object>)subitems).TryGetValue("type", out object subarr_tipe);
				if (!subarr_has_type)
					return JsonFormat.BAD;
				if (((Dtype)subarr_tipe & Dtype.ARR_OR_OBJ) != 0)
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


		private void FindTabsInSchemaHelper(Dictionary<string, object> schema, List<object> path, Dictionary<string, JsonFormat> tab_paths)
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
				tab_paths[patharr.ToString()] = cls;
				return;
			}
			// by process of elimination, it's bad, but maybe it contains tables
			Dtype tipe = (Dtype)schema["type"];
			if (tipe == Dtype.ARR)
			{
				// it's an array; we'll search recursively at each index for tables
				if (!schema.TryGetValue("items", out object items_obj))
					return; // no "items" keyword only happens for empty arrays
				var items = (Dictionary<string, object>)items_obj;
				List<object> newpath = new List<object>(path);
				// use 0 as a placeholder for all indices in arrays
				// the important thing is that it's not a string
				newpath.Add(0);
				if (items.TryGetValue("anyOf", out object anyof))
				{
					List<object> danyof = (List<object>)anyof;
					foreach (object subschema in danyof)
					{
						FindTabsInSchemaHelper((Dictionary<string, object>)subschema, newpath, tab_paths);
					}
				}
				else
				{
					FindTabsInSchemaHelper(items, newpath, tab_paths);
				}
				return;
			}
			// it's a dict; we'll search recursively beneath each key for tables
			var props = (Dictionary<string, object>)schema["properties"];
			foreach (string k in props.Keys)
			{
				List<object> newpath = new List<object>(path);
				newpath.Add(k);
				FindTabsInSchemaHelper((Dictionary<string, object>)props[k], newpath, tab_paths);
			}
		}

		public Dictionary<string, JsonFormat> FindTabsInSchema(Dictionary<string, object> schema)
		{
			var tab_paths = new Dictionary<string, JsonFormat>();
			FindTabsInSchemaHelper(schema, new List<object>(), tab_paths);
			return tab_paths;
		}

		/// <summary>
		/// hanging_row: a dict where some values are scalars and others are
		/// dicts with only scalar values.
		/// Returns: a new dict where the off-hanging dict at key k has been merged into
		/// the original by adding &lt;k&gt;&lt;key_sep&gt;&lt;key in hanger&gt; = &lt;val in hanger&gt;
		/// for each key, val in hanger.
		/// </summary>
		/// <param name="hanging_row"></param>
		/// <param name="key_sep"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		private Dictionary<string, JNode> ResolveHang(Dictionary<string, JNode> hanging_row, string key_sep = ".")
		{
			var result = new Dictionary<string, JNode>();
			foreach (string k in hanging_row.Keys)
			{
				JNode v = hanging_row[k];
				if (v is JObject)
				{
					var dv = (JObject)v;
					foreach (string subk in dv.children.Keys)
					{
						JNode subv = dv[subk];
						string new_key = $"{k}{key_sep}{subk}";
						if (result.ContainsKey(new_key))
						{
							throw new ArgumentException($"Attempted to create hanging row with synthetic key {new_key}, but {new_key} was already a key in {hanging_row}");
						}
						result[new_key] = subv;
					}
				}
				else
				{
					result[k] = v;
				}
			}
			return result;
		}

		private string FormatKey(string key, string super_key, string key_sep = ".")
		{
			return super_key.Length == 0 ? key : $"{super_key}{key_sep}{key}";
		}

		private void AnyTableToRecord(JNode obj,
									   JsonFormat cls,
									   Dictionary<string, JNode> rest_of_row,
									   List<JNode> result,
									   List<string> super_keylist,
									   string key_sep)
		{
			string super_key = string.Join(key_sep, super_keylist);
			Func<string, string> keyformat = (string x) => FormatKey(x, super_key, key_sep);
			if (cls == JsonFormat.REC)
			{
				JArray aobj = (JArray)obj;
				// arrays of flat objects - this is what we're aiming for
				foreach (object rec in aobj.children)
				{
					Dictionary<string, JNode> newrec = new Dictionary<string, JNode>();
					JObject orec = (JObject)rec;
					foreach (string k in orec.children.Keys)
					{
						newrec[keyformat(k)] = orec[k];
					}
					foreach (string k in rest_of_row.Keys)
					{
						newrec[k] = rest_of_row[k];
					}
					result.Add(new JObject(0, newrec));
				}
			}
			else if (cls == JsonFormat.TAB)
			{
				// a dict mapping some number of keys to scalars and the rest to arrays
				// some keys may be mapped to hanging all-scalar dicts and not scalars
				JObject oobj = (JObject)obj;
				Dictionary<string, JArray> arr_dict = new Dictionary<string, JArray>();
				Dictionary<string, JNode> not_arr_dict = new Dictionary<string, JNode>();
				int len_arr = 0;
				foreach (string k in oobj.children.Keys)
				{
					JNode v = oobj[k];
					if (v is JArray)
					{
						JArray varr = (JArray)v;
						arr_dict[k] = varr;
						if (varr.Length > len_arr) { len_arr = varr.Length; }
					}
					else
					{
						not_arr_dict[keyformat(k)] = v;
					}
				}
				not_arr_dict = ResolveHang(not_arr_dict, key_sep);
				if (len_arr == 0)
                {
					// A 0-length array should be dealt with by adding a single row with an empty value
					// for the array's parent key
					foreach (string k in arr_dict.Keys)
						not_arr_dict[k] = new JNode("", Dtype.STR, 0);
					result.Add(new JObject(0, not_arr_dict));
					return;
                }
				for (int ii = 0; ii < len_arr; ii++)
				{
					Dictionary<string, JNode> newrec = new Dictionary<string, JNode>();
					foreach (string k in arr_dict.Keys)
					{
						JArray v = arr_dict[k];
						// if one array is longer than others, the missing values from the short rows are filled by
						// empty strings
						newrec[keyformat(k)] = ii >= v.Length ? new JNode("", Dtype.STR, 0) : v[ii];
					}
					foreach (string k in not_arr_dict.Keys) { JNode v = not_arr_dict[k]; newrec[k] = v; }
					foreach (string k in rest_of_row.Keys) { JNode v = rest_of_row[k]; newrec[k] = v; }
					result.Add(new JObject(0, newrec));
				}
			}
			else if (cls == JsonFormat.SQR)
			{
				// an array of arrays of scalars
				JArray aobj = (JArray)obj;
				foreach (JNode arr in aobj.children)
				{
					Dictionary<string, JNode> newrec = new Dictionary<string, JNode>();
					foreach (string k in rest_of_row.Keys)
					{
						newrec[k] = rest_of_row[k];
					}
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
		/// <param name="obj"></param>
		/// <param name="cls"></param>
		/// <param name="depth"></param>
		/// <param name="tab_path"></param>
		/// <param name="result"></param>
		/// <param name="rest_of_row"></param>
		/// <param name="super_keylist"></param>
		/// <param name="key_sep"></param>
		private void BuildTableHelper_DEFAULT(JNode obj,
									JsonFormat cls,
									int depth,
									JArray tab_path,
									List<JNode> result,
									Dictionary<string, JNode> rest_of_row,
									List<string> super_keylist,
									string key_sep)
		{
			List<string> new_super_keylist = new List<string>(super_keylist);
			Dictionary<string, JNode> new_rest_of_row = new Dictionary<string, JNode>(rest_of_row);
			if (depth == tab_path.Length)
			{
				AnyTableToRecord(obj, cls, new_rest_of_row, result, new_super_keylist, key_sep);
				return;
			}
			else
			{
				if (obj is JArray)
				{
					foreach (JNode subobj in ((JArray)obj).children)
					{
						BuildTableHelper_DEFAULT(subobj, cls, depth + 1, tab_path, result, new_rest_of_row, new_super_keylist, key_sep);
					}
				}
				else
				{
					string new_key = (string)tab_path[depth].value;
					string super_key = string.Join(key_sep, new_super_keylist);
					Dictionary<string, JNode> children = ((JObject)obj).children;
					foreach (string k in children.Keys)
					{
						JNode v = children[k];
						if (k == new_key) continue;
						string newk = FormatKey(k, super_key, key_sep);
						if (v.type == Dtype.OBJ)
						{
							JObject ov = (JObject)v;
							foreach (string subk in ov.children.Keys)
							{
								JNode subv = ov[subk];
								// add hanging scalar dicts to rest_of_row
								if ((subv.type & Dtype.ARR_OR_OBJ) == 0)
								{
									new_rest_of_row[$"{newk}{key_sep}{subk}"] = subv;
								}
							}
						}
						else if (v.type != Dtype.ARR)
						{
							new_rest_of_row[newk] = v;
						}
					}
					new_super_keylist.Add(new_key);
					BuildTableHelper_DEFAULT(children[new_key], cls, depth + 1, tab_path, result, new_rest_of_row, new_super_keylist, key_sep);
				}
			}
		}

		/// <summary>
		/// Recursively searches a JSON object/array and all subobjects/subarrays<br></br>
		/// and makes a flattened long object (which is passed in as rest_of_row).<br></br>
		/// For example, this would take {"a": [1, 2, 3], "b": {"c": 1, "d": [1, 2]}, "e": "scal"}<br></br>
		/// and make the flattened row<br></br>
		/// {"a.col1": 1, "a.col2": 2, "a.col3": 3, "b.c": 1, "b.d.col1": 1, "b.d.col2": 2, "e": "scal"}
		/// </summary>
		/// <param name="row"></param>
		/// <param name="key_sep"></param>
		/// <param name="rest_of_row"></param>
		/// <param name="super_keylist"></param>
		private void FlattenSingleRow_FULL_RECURSIVE(JNode row, string key_sep, Dictionary<string, JNode> rest_of_row, List<string> super_keylist)
		{
			string super_key = string.Join(key_sep, super_keylist);
			Func<string, string> keyformat = (string x) => FormatKey(x, super_key, key_sep);
			if (row is JArray)
			{
				JArray arow = (JArray)row;
				for (int ii = 0; ii < arow.Length; ii++)
				{
					JNode child = arow[ii];
					if (child is JArray || child is JObject)
					{
						List<string> new_super_keylist = new List<string>(super_keylist);
						new_super_keylist.Add($"col{ii + 1}");
						FlattenSingleRow_FULL_RECURSIVE(child, key_sep, rest_of_row, new_super_keylist);
					}
					else
					{
						string key = keyformat($"col{ii + 1}");
						rest_of_row[key] = child;
					}
				}
			}
			else if (row is JObject)
			{
				JObject orow = (JObject)row;
				foreach (string key in orow.children.Keys)
				{
					JNode child = orow[key];
					if (child is JArray || child is JObject)
					{
						List<string> new_super_keylist = new List<string>(super_keylist);
						new_super_keylist.Add(key);
						FlattenSingleRow_FULL_RECURSIVE(child, key_sep, rest_of_row, new_super_keylist);
					}
					else
					{
						rest_of_row[keyformat(key)] = child;
					}
				}
			}
			else
			{
				rest_of_row[super_key] = row;
			}
		}

		/// <summary>
		/// Given that obj is an array of arrays or an array of objects,<br></br>
		/// creates an array of objects,<br></br>
		/// where each subobject "row" is a flattened version of a subarray/subobject in obj.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="result"></param>
		/// <param name="key_sep"></param>
		private void BuildTableHelper_FULL_RECURSIVE(JNode obj,
												List<JNode> result,
												string key_sep = ".")
		{
			foreach (JNode child in ((JArray)obj).children)
			{
				Dictionary<string, JNode> row = new Dictionary<string, JNode>();
				FlattenSingleRow_FULL_RECURSIVE(child, key_sep, row, new List<string>());
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
		/// <param name="key_sep"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		private void BuildTableHelper_STRINGIFY_ITERABLES(JNode obj, List<JNode> result, string key_sep = ".")
		{
			if (obj is JArray)
            {
				foreach (JNode child in ((JArray)obj).children)
				{
					Dictionary<string, JNode> row = new Dictionary<string, JNode>();
					if (child is JArray)
					{
						JArray achild = (JArray)child;
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
					else if (child is JObject)
					{
						JObject ochild = (JObject)child;
						foreach (string key in ochild.children.Keys)
						{
							JNode subchild = ochild[key];
							if (subchild is JArray || subchild is JObject)
							{
								row[key] = new JNode(subchild.ToString(), Dtype.STR, 0); //.Replace("\"", "\\\""), Dtype.STR, 0);
							}
							else
							{
								row[key] = subchild;
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
			Dictionary<string, JArray> arr_dict = new Dictionary<string, JArray>();
			Dictionary<string, JNode> not_arr_dict = new Dictionary<string, JNode>();
			int len_arr = 0;
			foreach (string k in oobj.children.Keys)
			{
				JNode v = oobj[k];
				if (v is JArray varr)
				{
					arr_dict[k] = varr;
					if (varr.Length > len_arr) { len_arr = varr.Length; }
				}
				else
				{
					not_arr_dict[k] = v;
				}
			}
			not_arr_dict = ResolveHang(not_arr_dict, key_sep);
			// for each array child, iterate through the arrays in parallel,
			// and make the i^th object in each record contain the following:
			// * all the scalar values of the non-array children in the object
			// * if the child is an array:
			//     - if the array is shorter than i, an empty string
			//     - if the i^th element is an iterable, the stringified form of that iterable
			//     - if the i^th element is a scalar, that scalar
			for (int ii = 0; ii < len_arr; ii++)
			{
				Dictionary<string, JNode> newrec = new Dictionary<string, JNode>();
				foreach (string k in arr_dict.Keys)
				{
					JArray v = arr_dict[k];
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
				foreach (string k in not_arr_dict.Keys)
					newrec[k] = not_arr_dict[k];
				result.Add(new JObject(0, newrec));
			}
		}

		public JArray BuildTable(JNode obj, Dictionary<string, object> schema, string key_sep = ".")
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
				BuildTableHelper_DEFAULT(obj, cls, 0, path, result, new Dictionary<string, JNode>(), new List<string>(), key_sep);
				return new JArray(0, result);
			}
			else if (strategy == JsonTabularizerStrategy.FULL_RECURSIVE)
			{
				if (!(obj is JArray))
				{
					throw new ArgumentException($"Full recursive strategy does not work on non-array JSON.");
				}
				BuildTableHelper_FULL_RECURSIVE(obj, result, key_sep);
			}
			else if (strategy == JsonTabularizerStrategy.STRINGIFY_ITERABLES)
			{
				BuildTableHelper_STRINGIFY_ITERABLES(obj, result);
			}
			else
			{
				// default strategy - find tables in the schema, and recursively add everything that's on the path
				// to the tables.
				Dictionary<string, JsonFormat> tab_paths = FindTabsInSchema(schema);
				if (tab_paths.Count == 0)
				{
					return new JArray();
				}
				if (tab_paths.Count > 1)
				{
					throw new ArgumentException($"This JSON contains {tab_paths.Count} possible tables, and BuildTable doesn't know how to proceed.");
				}
				foreach (string pathstr in tab_paths.Keys)
				{
					JsonFormat clsname = tab_paths[pathstr];
					jsonParser.ii = 0;
					jsonParser.line_num = 0;
					path = jsonParser.ParseArray(pathstr, 0);
					cls = clsname;
				}
				BuildTableHelper_DEFAULT(obj, cls, 0, path, result, new Dictionary<string, JNode>(), new List<string>(), key_sep);
			}
			return new JArray(0, result);
		}

		/// <summary>
		/// If a string contains the delimiter, wrap that string in quotes.<br></br>
		/// Also replace any newlines with "\\n".<br></br>
		/// Otherwise, leave it alone.
		/// </summary>
		/// <param name="s"></param>
		/// <param name="delim"></param>
		/// <param name="quote_char"></param>
		/// <returns></returns>
		private string ApplyQuotesIfNeeded(string s, char delim, char quote_char)
		{
			string squote_char = new string(quote_char, 1);
			StringBuilder sb = new StringBuilder();
			if (s.Contains(delim))
			{
				// if the string contains the delimiter, we need to wrap it in quotes
				// we also need to escape all literal quote characters in the string
				// regardless of what happens, we need to replace internal newlines with "\\n"
				// so we don't get fake rows
				sb.Append(quote_char);
				foreach (char c in s)
                {
					if (c == quote_char)
						sb.Append("\\\"");
					else if (c == '\n')
						sb.Append("\\n");
					else
						sb.Append(c);
                }
				sb.Append(quote_char);
				return sb.ToString();
			}
			// just replace newlines
			foreach (char c in s)
            {
				if (c == '\n')
					sb.Append("\\n");
				else
					sb.Append(c);
            }
			return sb.ToString();
		}

		public string TableToCsv(JArray table, char delim = ',', char quote_char = '"', string[] header = null, bool bools_as_ints = false)
		{
			// allow the user to supply their own column order. If they don't, just alphabetically sort colnames
			if (header == null)
			{
				HashSet<string> all_keys = new HashSet<string>();
				foreach (JNode child in table.children)
				{
					foreach (string key in ((JObject)child).children.Keys)
					{
						all_keys.Add(key);
					}
				}
				header = new string[all_keys.Count];
				int ii = 0;
				foreach (string key in all_keys)
				{
					header[ii++] = key;
				}
				Array.Sort(header);
			}
			StringBuilder sb = new StringBuilder();
			for (int ii = 0; ii < header.Length; ii++)
			{
				string col = header[ii];
				sb.Append(ApplyQuotesIfNeeded(col, delim, quote_char));
				if (ii < header.Length - 1) sb.Append(delim);
			}
			sb.Append('\n');
			foreach (JNode row in table.children)
			{
				JObject orow = (JObject)row;
				for (int ii = 0; ii < header.Length; ii++)
				{
					string col = header[ii];
					if (!orow.children.TryGetValue(col, out JNode val))
					{
						// sometimes the row might have a missing key.
						// in that case, just leave an empty entry, and add a delimiter if needed.
						if (ii < header.Length - 1) sb.Append(delim);
						continue;
					}
					switch (val.type)
					{
						case Dtype.STR:
							sb.Append(ApplyQuotesIfNeeded((string)val.value, delim, quote_char));
							break; // only apply quotes if internal delim
						case Dtype.DATE:
							sb.Append(((DateTime)val.value).ToString("yyyy-MM-dd"));
							break;
						case Dtype.DATETIME:
							sb.Append(((DateTime)val.value).ToString("yyyy-MM-dd hh:mm:ss"));
							break;
						case Dtype.NULL:
							break; // nulls should be empty entries
						case Dtype.BOOL:
							if (bools_as_ints)
							{
								sb.Append((bool)val.value ? "1" : "0");
							}
							else
							{
								sb.Append((bool)val.value ? "true" : "false");
							}
							break;
						default:
							sb.Append(val.ToString()); // everything else gets quote chars
							break;
					}
					if (ii < header.Length - 1) sb.Append(delim);
				}
				sb.Append('\n');
			}
			return sb.ToString();
		}
	}
}