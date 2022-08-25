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
				var items = (Dictionary<string, object>)schema["items"];
				bool item_has_type = items.TryGetValue("type", out object item_types);
				if (!item_has_type)
                {
					// this is because it's an "anyOf" type, where it can have some non-scalars and some scalars as values.
					// we can't tabularize it so we call it "bad"
					return JsonFormat.BAD;
                }
				if (item_types is List<object>)
                {
					// it's an array with a mixture of element types
					foreach (object t in (List<object>)item_types)
					{
						if (((Dtype)t & Dtype.SCALAR) == 0)
						{
							// disallow arrays of arrays of non-scalars
							return JsonFormat.BAD;
						}
					}
					// a list of scalars is not a tabular schema, but a "row".
					// rows have desirable properties, though,
					// because we can flatten them into keys of the parent object
					return JsonFormat.ROW;
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
					Dictionary<string, object> subitems = (Dictionary<string, object>)items["items"];
					bool subarr_has_type = subitems.TryGetValue("type", out object subarr_types);
					if (!subarr_has_type)
                    {
						// this is an array containing mixed arrays - bad!
						return JsonFormat.BAD;
                    }
					if (subarr_types is List<object>)
                    {
						foreach (object v in (List<object>)subarr_types)
                        {
							if (((Dtype)v & Dtype.SCALAR) == 0)
                            {
								// this is an array of mixed scalar/non-scalar arrays, no good!
								return JsonFormat.BAD;
                            }
                        }
						// an array of arrays of mixed-type, e.g., [[1, "2"], [3, "4"]]
						return JsonFormat.SQR;
                    }
					if (subarr_types is Dtype && ((Dtype)subarr_types & Dtype.SCALAR) != 0)
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
					if (!(subtipe is List<object> || (subtipe is Dtype && ((Dtype)subtipe & Dtype.SCALAR) != 0)))
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
				bool has_subtipe = dprop.TryGetValue("type", out object subtipe);
				if (!has_subtipe)
				{
					return JsonFormat.BAD; // it's a dict with a mixture of non-scalar types - bad! 
				}
				if (subtipe is Dtype && (Dtype)subtipe == Dtype.OBJ)
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
						if (!(subproptipe is List<object> || (subtipe is Dtype && ((Dtype)subproptipe & Dtype.SCALAR) != 0)))
                        {
							// this key of the dictionary has one nonscalar type
							return JsonFormat.BAD;
                        }
						continue;
					}
                }
				if (subtipe is Dtype && (Dtype)subtipe != Dtype.ARR)
                {
					// it's OK for a table to have some scalar values and some list values.
					// the scalars values are just copy-pasted into each row for that table
					continue;
                }
				if (subtipe is List<object>)
                {
					// it's also OK for a table to have a mixture of scalar values for one key
					continue;
                }
				bool subarr_has_type = ((Dictionary<string, object>)dprop["items"]).TryGetValue("type", out object subarr_tipe);
				if (!subarr_has_type || (subarr_tipe is Dtype && ((Dtype)subarr_tipe & Dtype.SCALAR) == 0))
                {
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
				JArray patharr = new JArray(0, new List<JNode>());
				foreach (object node in path)
                {
					if (node is int && (int)node == 0)
                    {
						patharr.children.Add(new JNode(0, Dtype.FLOAT, 0));
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
			object tipe = schema["type"];
			if (tipe is Dtype && (Dtype)tipe == Dtype.ARR)
            {
				// it's an array; we'll search recursively at each index for tables
				var items = (Dictionary<string, object>)schema["items"];
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
						JNode subv = dv.children[subk];
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
						newrec[keyformat(k)] = orec.children[k];
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
					JNode v = oobj.children[k];
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
				for (int ii = 0; ii < len_arr; ii++)
                {
					Dictionary<string, JNode> newrec = new Dictionary<string, JNode>();
					foreach (string k in arr_dict.Keys)
                    {
						JArray v = arr_dict[k];
						// if one array is longer than others, the missing values from the short rows are filled by
						// empty strings
						newrec[keyformat(k)] = ii >= v.Length ? new JNode("", Dtype.STR, 0) : v.children[ii];
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
						newrec[keyformat($"col{ii+1}")] = children[ii];
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
					string new_key = (string)tab_path.children[depth].value;
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
								JNode subv = ov.children[subk];
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
					JNode child = arow.children[ii];
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
					JNode child = orow.children[key];
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
		private void BuildTableHelper_STRINGIFY_ITERABLES(JNode obj, List<JNode> result)
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
						JNode subchild = achild.children[ii];
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
						JNode subchild = ochild.children[key];
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
		}

		public JArray BuildTable(JNode obj, Dictionary<string, object> schema, string key_sep = ".")
        {
			JsonParser jsonParser = new JsonParser();
			List<JNode> result = new List<JNode>();
			JArray path = new JArray(0, new List<JNode>());
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
				if (!(obj is JArray))
				{
					throw new ArgumentException($"Stringify iterables strategy does not work on non-array JSON.");
				}
				BuildTableHelper_STRINGIFY_ITERABLES(obj, result);
            }
            else
            {
				// default strategy - find tables in the schema, and recursively add everything that's on the path
				// to the tables.
				Dictionary<string, JsonFormat> tab_paths = FindTabsInSchema(schema);
				if (tab_paths.Count == 0)
				{
					return new JArray(0, new List<JNode>());
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
					path = jsonParser.ParseArray(pathstr);
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
			if (s.Contains(delim))
            {
				// if the string contains the delimiter, we need to wrap it in quotes
				// we also need to escape all literal quote characters in the string
				// regardless of what happens, we need to replace internal newlines with "\\n"
				// so we don't get fake rows
				return squote_char + s.Replace(squote_char, "\\" + squote_char).Replace("\n", "\\n") + squote_char; 
            }
			// just replace newlines
			string outval = s.Replace("\n", "\\n");
			// check for quotes at beginning and end of string
			// we would prefer to avoid 
			//if (outval[0] == quote_char || outval[outval.Length - 1] == quote_char)
			//{
			//	return outval.Replace(squote_char, "\\" + squote_char);
			//}
			return outval;
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


	public class JsonTabularizerTester
    {
		public static void Test()
		{
			var testcases = new string[][]
			{
				new string[] {
				"[" +
					"{" +
						"\"a\": 1," +
						"\"b\": \"foo\"," +
						"\"c\": [" +
							"{" +
								"\"d\": 1," +
								"\"e\": \"a\"" +
							"}," +
							"{" +
								"\"d\": 2," +
								"\"e\": \"b\"" +
							"}" +
						"]" +
					"}," +
					"{" +
						"\"a\": 2," +
						"\"b\": \"bar\"," +
						"\"c\": [" +
							"{" +
								"\"d\": 3," +
								"\"e\": \"c\"" +
							"}," +
							"{" +
								"\"d\": 4," +
								"\"e\": \"d\"" +
							"}" +
						"]" +
					"}" +
				"]", // a list of dicts mapping to records (each record is a list of dicts)
				"[" +
					"{\"a\": 1, \"b\": \"foo\", \"c.d\": 1, \"c.e\": \"a\"}," +
					"{\"a\": 1, \"b\": \"foo\", \"c.d\": 2, \"c.e\": \"b\"}," +
					"{\"a\": 2, \"b\": \"bar\", \"c.d\": 3, \"c.e\": \"c\"}," +
					"{\"a\": 2, \"b\": \"bar\", \"c.d\": 4, \"c.e\": \"d\"}" +
				"]",
				"."
				},
				new string[] {
				"{\"6\": 7," +
 "\"9\": \"ball\"," +
 "\"9a\": 2," +
 "\"a\": false," +
 "\"b\": \"3\"," +
 "\"blutentharst\": [\"DOOM BOOM\", true]," +
 "\"jub\": {\"status\": \"jubar\"," +
		 "\"uy\": [1, 2, NaN]," +
		 "\"yu\": [[6, {\"m8\": 9, \"y\": \"b\"}], null]}}", // bad irregular JSON that can't be tabularized
				"[]", // no table possible
				"."
				},
				new string[]{
				"[" +
					"{" +
						"\"name\": \"foo\"," +
						"\"players\": [" +
							"{\"name\": \"alice\", \"hits\": [1, 2], \"at-bats\": [3, 4]}," +
							"{\"name\": \"bob\", \"hits\": [2], \"at-bats\": [3]}" +
						"]" +
					"}," +
					"{" +
						"\"name\": \"bar\"," +
						"\"players\": [" +
							"{\"name\": \"carol\", \"hits\": [1], \"at-bats\": [2, 3]}" +
						"]" +
					"}" +
				"]", // list of tabs with some missing values
				"[" +
					"{" +
						"\"name\": \"foo\", " +
						"\"players.name\": \"alice\"," +
						"\"players.hits\": 1," +
						"\"players.at-bats\": 3" +
					"}," +
					"{" +
						"\"name\": \"foo\"," +
						"\"players.name\": \"alice\"," +
						"\"players.hits\": 2," +
						"\"players.at-bats\": 4" +
					"}," +
					"{" +
						"\"name\": \"foo\"," +
						"\"players.name\": \"bob\"," +
						"\"players.hits\": 2," +
						"\"players.at-bats\": 3" +
					"}," +
					"{" +
						"\"name\": \"bar\"," +
						"\"players.name\": \"carol\"," +
						"\"players.hits\": 1," +
						"\"players.at-bats\": 2" +
					"}," +
					"{" +
						"\"name\": \"bar\"," +
						"\"players.name\": \"carol\"," +
						"\"players.hits\": \"\"," +
						"\"players.at-bats\": 3" +
					"}" +
				"]",
				"."
				},
				new string[] {
				"{\"leagues\": [" +
					"{" +
					"\"league\": \"American\"," +
					"\"teams\": [" +
							"{" +
								"\"name\": \"foo\"," +
								"\"players\": [" +
									"{\"name\": \"alice\", \"hits\": [1], \"at-bats\": [3]}" +
								"]" +
							"}," +
							"{" +
								"\"name\": \"bar\"," +
								"\"players\": [" +
									"{\"name\": \"carol\", \"hits\": [1], \"at-bats\": [2]}" +
								"]" +
							"}" +
						"]" +
					"}," +
					"{" +
					"\"league\": \"National\"," +
					"\"teams\": [" +
							"{" +
								"\"name\": \"baz\"," +
								"\"players\": [" +
									"{\"name\": \"bob\", \"hits\": [2], \"at-bats\": [3]}" +
								"]" +
							"}" +
						"]" +
					"}" +
				"]}", // deeply nested tab
				"[" +
					"{" +
						"\"leagues.league\": \"American\"," +
						"\"leagues.teams.name\": \"foo\"," +
						"\"leagues.teams.players.name\": \"alice\"," +
						"\"leagues.teams.players.hits\": 1," +
						"\"leagues.teams.players.at-bats\": 3" +
					"}," +
					"{" +
						"\"leagues.league\": \"American\"," +
						"\"leagues.teams.name\": \"bar\"," +
						"\"leagues.teams.players.name\": \"carol\"," +
						"\"leagues.teams.players.hits\": 1," +
						"\"leagues.teams.players.at-bats\": 2" +
					"}," +
					"{" +
						"\"leagues.league\": \"National\"," +
						"\"leagues.teams.name\": \"baz\"," +
						"\"leagues.teams.players.name\": \"bob\"," +
						"\"leagues.teams.players.hits\": 2," +
						"\"leagues.teams.players.at-bats\": 3" +
					"}" +
				"]",
				"."
				},
				new string[] {
				"[" +
					"[1, 2, \"a\"]," +
					"[2, 3, \"b\"]" +
				"]", // list of lists with mixed scalar types
				"[" +
					"{\"col1\": 1, \"col2\": 2, \"col3\": \"a\"}," +
					"{\"col1\": 2, \"col2\": 3, \"col3\": \"b\"}" +
				"]",
				"."
				},
				new string[] {
				"{\"a\": [" +
						"{" +
							"\"name\": \"blah\"," +
							"\"rows\": [" +
								"[1, 2, \"a\"]," +
								"[1, 2, \"a\"]" +
							"]" +
						"}" +
					"]" +
				"}", // deeply nested list of lists with mixed scalar types
				"[" +
					"{" +
						"\"a.name\": \"blah\", " +
						"\"a.rows.col1\": 1, " +
						"\"a.rows.col2\": 2, " +
						"\"a.rows.col3\": \"a\"" +
					"}," +
					"{" +
						"\"a.name\": \"blah\", " +
						"\"a.rows.col1\": 1, " +
						"\"a.rows.col2\": 2, " +
						"\"a.rows.col3\": \"a\"" +
					"}" +
				"]",
				"."
				},
				new string[] {
				"[" +
					"{\"a\": 1, \"b\": \"a\"}," +
					"{\"a\": \"2\", \"b\": \"b\"}," +
					"{\"a\": 3., \"b\": \"c\"}" +
				"]", // record with mixed scalar types
				"[" +
					"{\"a\": 1, \"b\": \"a\"}," +
					"{\"a\": \"2\", \"b\": \"b\"}," +
					"{\"a\": 3., \"b\": \"c\"}" +
				"]",
				"."
				},
				new string[] {
				"{" +
					"\"f\": {\"g\": 1}," +
					"\"g\": 2.0," +
					"\"a\": {" +
						"\"a\": [1, 2]," +
						"\"b\": [\"a\", \"b\"]," +
						"\"c\": {\"d\": 2, \"e\": \"a\"}," +
						"\"d\": \"b\"" +
					"}" +
				"}", // deep nested tab with hanging dict
				"[" +
					"{" +
						"\"a.a\": 1," +
						"\"a.b\": \"a\"," +
						"\"a.c.d\": 2," +
						"\"a.c.e\": \"a\"," +
						"\"a.d\": \"b\"," +
						"\"f.g\": 1," +
						"\"g\": 2.0" +
					"}," +
					"{" +
						"\"a.a\": 2," +
						"\"a.b\": \"b\"," +
						"\"a.c.d\": 2," +
						"\"a.c.e\": \"a\"," +
						"\"a.d\": \"b\"," +
						"\"f.g\": 1," +
						"\"g\": 2.0" +
					"}" +
				"]",
				"."
				},
				new string[] {
				"{" +
					"\"a\": {\"b\": 2, \"c\": \"b\"}," +
					"\"d\": [" +
						"[1, 2]," +
						"[3, 4]" +
					"]" +
				"}", // deep nested all-same-type sqr with hanging dict
				"[" +
					"{" +
					"\"a.b\": 2," +
					"\"a.c\": \"b\"," +
					"\"d.col1\": 1," +
					"\"d.col2\": 2" +
					"}," +
					"{" +
					"\"a.b\": 2," +
					"\"a.c\": \"b\"," +
					"\"d.col1\": 3," +
					"\"d.col2\": 4" +
					"}" +
				"]",
				"."
				},
				new string[] {
				"[" +
					"{\"a\": 1, \"b\": 2}," +
					"{\"a\": 3, \"b\": 4}" +
				"]", // all-same-type tab
				"[" +
					"{\"a\": 1, \"b\": 2}," +
					"{\"a\": 3, \"b\": 4}" +
				"]",
				"."
				},
				new string[] {
				"{\"stuff\": [" +
					"{" +
						"\"name\": \"blah\"," +
						"\"substuff\": [" +
							"{" +
								"\"a\": 1," +
								"\"b\": \"foo\"," +
								"\"c\": [" +
									"{\"d\": 1, \"e\": \"a\"}" +
								"]" +
							"}," +
							"{" +
								"\"a\": 2," +
								"\"b\": \"bar\"," +
								"\"c\": [" +
									"{\"d\": 3, \"e\": \"c\"}" +
								"]" +
							"}" +
						"]" +
					"}," +
					"{" +
						"\"name\": \"wuz\"," +
						"\"substuff\": [" +
							"{" +
								"\"a\": 3, " +
								"\"b\": \"baz\", " +
								"\"c\": [" +
									"{\"d\": 4, \"e\": \"f\"}" +
								"]" +
							"}" +
						"]" +
					"}" +
				"]}",
				"[" +
					"{" +
						"\"stuff_name\": \"blah\"," +
						"\"stuff_substuff_a\": 1," +
						"\"stuff_substuff_b\": \"foo\"," +
						"\"stuff_substuff_c_d\": 1," +
						"\"stuff_substuff_c_e\": \"a\"" +
					"}," +
					"{" +
						"\"stuff_name\": \"blah\"," +
						"\"stuff_substuff_a\": 2," +
						"\"stuff_substuff_b\": \"bar\"," +
						"\"stuff_substuff_c_d\": 3," +
						"\"stuff_substuff_c_e\": \"c\"" +
					"}," +
					"{" +
						"\"stuff_name\": \"wuz\"," +
						"\"stuff_substuff_a\": 3," +
						"\"stuff_substuff_b\": \"baz\"," +
						"\"stuff_substuff_c_d\": 4," +
						"\"stuff_substuff_c_e\": \"f\"" +
					"}" +
				"]",
				"_"
				},
				new string[] {
				"[" +
					"{" +
						"\"a\": 1," +
						"\"b\": \"foo\"," +
						"\"c\": [" +
							"{\"d\": 1, \"e\": \"a\"}" +
						"]" +
					"}," +
					"{" +
						"\"a\": 2," +
						"\"b\": \"bar\"," +
						"\"c\": [" +
							"{\"d\": 3, \"e\": \"c\"}" +
						"]" +
					"}" +
				"]",
				"[" +
					"{\"a\": 1, \"b\": \"foo\", \"c/d\": 1, \"c/e\": \"a\"}," +
					"{\"a\": 2, \"b\": \"bar\", \"c/d\": 3, \"c/e\": \"c\"}" +
				"]",
				"/"
				},
				new string[] {
				"{\"a\": [[1, 2], [3, 4]], \"b\": \"a\"}",
				"[" +
					"{" +
						"\"a+col1\": 1," +
						"\"a+col2\": 2," +
						"\"b\": \"a\"" +
					"}," +
					"{" +
						"\"a+col1\": 3," +
						"\"a+col2\": 4," +
						"\"b\": \"a\"" +
					"}" +
				"]",
				"+"
				},
				new string[] {
				"[" +
					"{\"a\": 1, \"b\": [1], \"c\": {\"d\": \"y\"}}," +
					"{\"a\": true, \"b\": [7, 8]}," +
					"{\"a\": false, \"b\": [9]}" +
				"]", // missing keys and mixed types for the same key in an object
				"[" +
					"{\"a\": 1, \"b\": 1, \"c.d\": \"y\"}," +
					"{\"a\": true, \"b\": 7}," +
					"{\"a\": true, \"b\": 8}," +
					"{\"a\": false, \"b\": 9}" +
				"]",
				"."
				}
			};
			JsonParser jsonParser = new JsonParser();
			JsonTabularizer tabularizer = new JsonTabularizer();
			JsonSchemaMaker schema_maker = new JsonSchemaMaker();
			int tests_failed = 0;
			int ii = 0;
			foreach (string[] test in testcases)
			{
				string inp = test[0];
				string desired_out = test[1];
				string key_sep = test[2];
				ii++;
				JNode jinp = jsonParser.Parse(inp);
				Dictionary<string, object> schema = schema_maker.BuildSchema(jinp);
				//Console.WriteLine($"schema for {inp}:\n{schema_maker.SchemaToJNode(schema).ToString()}");
				JNode jdesired_out = jsonParser.Parse(desired_out);
				JNode result = new JNode(null, Dtype.NULL, 0);
				string base_message = $"Expected BuildTable({jinp.ToString()})\nto return\n{jdesired_out.ToString()}\n";
				try
				{
					result = tabularizer.BuildTable(jinp, schema, key_sep);
					try
					{
						if (!jdesired_out.Equals(result))
						{
							tests_failed++;
							Console.WriteLine($"{base_message}Instead returned\n{result.ToString()}");
						}
					}
					catch
					{
						tests_failed++;
						Console.WriteLine($"{base_message}Instead returned\n{result.ToString()}");
					}
				}
				catch (Exception ex)
				{
					tests_failed++;
					Console.WriteLine($"{base_message}Instead threw exception\n{ex}");
				}
			}

			// TEST CSV CREATION
			JsonParser fancyParser = new JsonParser(true);

			var csv_testcases = new object[][]
			{
				new object[] { "[{\"a\": 1, \"b\": \"a\"}, {\"a\": 2, \"b\": \"b\"}]", "a,b\n1,a\n2,b\n", ',', '"', null, false },
				new object[] {
				"[{\"a\": 1, \"b\": 2, \"c\": 3}, {\"a\": 2, \"b\": 3, \"d\": 4}, {\"a\": 1, \"b\": 2}]",
				"a,b,c,d\n" + // test missing entries in some rows
				"1,2,3,\n" +
				"2,3,,4\n" +
				"1,2,,\n",
				',', '"', null, false
				},
				new object[] {
				"[" +
					"{\"a\": 1, \"b\": \"[1, 2, 3]\", \"c\": \"{\\\"d\\\": \\\"y\\\"}\"}," +
					"{\"a\": 2, \"b\": \"[4, 5, 6]\", \"c\": \"{\\\"d\\\": \\\"z\\\"}\"}" +
				"]", // test stringified iterables
				"a\tb\tc\n" +
				"1\t[1, 2, 3]\t{\"d\": \"y\"}\n" +
				"2\t[4, 5, 6]\t{\"d\": \"z\"}\n",
				'\t', '"', null, false
				},
				new object[] { "[{\"a\": null, \"b\": 1.0}, {\"a\": \"blah\", \"b\": NaN}]", // nulls and NaNs
				"a,b\n,1.0\nblah,NaN\n",
				',', '"', null, false
				},
				new object[] { "[{\"a\": 1, \"b\": \"a\"}, {\"a\": 2, \"b\": \"b\"}]", "a\tb\n1\ta\n2\tb\n", '\t', '"', null, false },
				new object[] { "[{\"a\": 1, \"b\": \"a\"}, {\"a\": 2, \"b\": \"b\"}]", "a,b\n1,a\n2,b\n", ',', '\'', null, false },
				new object[] { "[{\"a\": 1, \"b\": \"a\"}, {\"a\": 2, \"b\": \"b\"}]", "a\tb\n1\ta\n2\tb\n", '\t', '\'', null, false },
				new object[] { "[{\"a\": 1, \"b\": \"a\"}, {\"a\": 2, \"b\": \"b\"}]", "b,a\na,1\nb,2\n", ',', '"', new string[]{"b", "a"}, false },
				new object[] { "[{\"a\": 1, \"b\": \"a\"}, {\"a\": 2, \"b\": \"b\"}]", "b\ta\na\t1\nb\t2\n", '\t', '"', new string[]{"b", "a"}, false },
				new object[] { "[{\"a\": 1, \"b\": \"a,b\"}, {\"a\": 2, \"b\": \"c\"}]", "a,b\n1,\"a,b\"\n2,c\n", ',', '"', null, false },
				new object[] { "[{\"a\": 1, \"b\": \"a,b\"}, {\"a\": 2, \"b\": \"c\"}]", "a,b\n1,'a,b'\n2,c\n", ',', '\'', null, false }, // delims in values
				new object[] { "[{\"a,b\": 1, \"b\": \"a\"}, {\"a,b\": 2, \"b\": \"b\"}]", "\"a,b\",b\n1,a\n2,b\n", ',', '"', null, false },
				new object[] { "[{\"a,b\": 1, \"b\": \"a\"}, {\"a,b\": 2, \"b\": \"b\"}]", "'a,b',b\n1,a\n2,b\n", ',', '\'', null, false },
				new object[] { "[{\"a,b\": 1, \"b\": \"a\"}, {\"a,b\": 2, \"b\": \"b\"}]", // internal delims in column header
				"b,\"a,b\"\na,1\nb,2\n",
				',', '"', new string[]{"b", "a,b"}, false
				},
				new object[] { "[{\"a\tb\": 1, \"b\": \"a\"}, {\"a\tb\": 2, \"b\": \"b\"}]", // \t in column header when \t is delim
				"a\\tb\tb\n1\ta\n2\tb\n",
				'\t', '"', null, false
				},
				new object[] { "[{\"a\": 1, \"b\": \"a\tb\"}, {\"a\": 2, \"b\": \"c\"}]",
				"a\tb\n1\t\"a\tb\"\n2\tc\n",
				'\t', '"', null, false
				},
				new object[]{"[{\"a\": 1}, {\"a\": 2}, {\"a\": 3}]",
				"a\n1\n2\n3\n",
				',', '"', null, false},
				new object[]{"[{\"a\": 1, \"b\": 2, \"c\": 3}, {\"a\": 2, \"b\": 3, \"c\": 4}, {\"a\": 3, \"b\": 4, \"c\": 5}]",
				"a|b|c\n1|2|3\n2|3|4\n3|4|5\n",
				'|', '"', null, false},
				new object[]{"[{\"date\": \"1999-01-03\", \"cost\": 100.5, \"num\": 13}, {\"date\": \"2000-03-15\", \"cost\": 157.0, \"num\": 17}]",
				"cost,date,num\n100.5,1999-01-03,13\n157.0,2000-03-15,17\n", // dates
				',', '"', null, false},
				new object[]{"[{\"date\": \"1999-01-03 07:03:29\", \"cost\": 100.5, \"num\": 13}]",
				"cost,date,num\n100.5,1999-01-03 07:03:29,13\n", // datetimes
				',', '"', null, false },
				new object[]{"[{\"name\": \"\\\"Dr. Blutentharst\\\"\", \"phone number\": \"420-997-1043\"}," +
				"{\"name\": \"\\\"Fjordlak the Deranged\\\"\", \"phone number\": \"blo-od4-blud\"}]", // internal quote chars
				"name,phone number\n\"Dr. Blutentharst\",420-997-1043\n\"Fjordlak the Deranged\",blo-od4-blud\n",
				',', '"', null, false},
				new object[]{"[{\"a\": \"new\\nline\", \"b\": 1}]", // internal newlines
				"a,b\nnew\\nline,1\n",
				',', '"', null, false
				},
				new object[]{
				"[{\"a\": true, \"b\": \"foo\"}, {\"a\": false, \"b\": \"bar\"}]", // boolean values with bools_as_ints false
				"a,b\ntrue,foo\nfalse,bar\n",
				',', '"', null, false
				},
				new object[]{
				"[{\"a\": true, \"b\": \"foo\"}, {\"a\": false, \"b\": \"bar\"}]", // boolean values with bools_as_ints true
				"a,b\n1,foo\n0,bar\n",
				',', '"', null, true
				},
				new object[]{
				"[" +
					"{\"a\": 1, \"b\": 1, \"c.d\": \"y\"}," +
					"{\"a\": true, \"b\": 7}," +
					"{\"a\": true, \"b\": 8}," +
					"{\"a\": false, \"b\": 9}" +
				"]", // missing keys
				"a,b,c.d\n1,1,y\ntrue,7,\ntrue,8,\nfalse,9,\n",
				',', '"', null, false
				}
			};
			foreach (object[] test in csv_testcases)
			{
				string inp = (string)test[0];
				string desired_out = (string)test[1];
				char delim = (char)test[2];
				char quote_char = (char)test[3];
				string[] header = (string[])test[4];
				bool bools_as_ints = (bool)test[5];
				ii++;
				JNode table = fancyParser.Parse(inp);
				string result = "";
				string head_str = header == null ? "null" : '[' + string.Join(", ", header) + ']';
				string base_message = $"With default strategy, expected TableToCsv({inp}, '{delim}', '{quote_char}', {head_str})\nto return\n{desired_out}\n";
				try
				{
					result = tabularizer.TableToCsv((JArray)table, delim, quote_char, header, bools_as_ints);
					//Console.WriteLine(table);
					//Console.WriteLine(result);
					try
					{
						if (!desired_out.Equals(result))
						{
							tests_failed++;
							Console.WriteLine($"{base_message}Instead returned\n{result}");
						}
					}
					catch (Exception ex)
					{
						tests_failed++;
						Console.WriteLine($"{base_message}Instead returned\n{result}\nand threw exception\n{ex}");
					}
				}
				catch (Exception ex)
				{
					tests_failed++;
					Console.WriteLine($"{base_message}Instead threw exception\n{ex}");
				}
			}
			// TEST NO_RECURSION setting
			var no_recursion_tabularizer = new JsonTabularizer(JsonTabularizerStrategy.NO_RECURSION);
			var no_recursion_testcases = new string[][]
			{
				new string[]{
				"[" +
					"{\"a\": 1, \"b\": 2}," +
					"{\"a\": 3, \"b\": 4}" +
				"]", // all-same-type tab
				"[" +
					"{\"a\": 1, \"b\": 2}," +
					"{\"a\": 3, \"b\": 4}" +
				"]"
				},
				new string[]{
				"[" +
					"[1, 2.5, \"a\"]," +
					"[2, 3.5, \"b\"]" +
				"]", // array of arrays
				"[" +
					"{\"col1\": 1, \"col2\": 2.5, \"col3\": \"a\"}," +
					"{\"col1\": 2, \"col2\": 3.5, \"col3\": \"b\"}" +
				"]"
				},
				new string[]{
				"{\"a\": [1,2,3], " +
					"\"b\": [0.0, 0.5, 1.0], " +
					"\"c\": [\"a\", \"b\", \"c\"]" +
				"}", // dict mapping to lists
				"[" +
					"{\"a\": 1, \"b\": 0.0, \"c\": \"a\"}," +
					"{\"a\": 2, \"b\": 0.5, \"c\": \"b\"}," +
					"{\"a\": 3, \"b\": 1.0, \"c\": \"c\"}" +
				"]"
				},
				new string[]{
				"{\"a\": [1,2,3], " +
					"\"b\": [0.0, 0.5, 1.0], " +
					"\"c\": [\"a\", \"b\", \"c\"]," +
					"\"d\": \"hang\"" +
				"}", // dict with hanging scalars and lists
				"[" +
					"{\"a\": 1, \"b\": 0.0, \"c\": \"a\", \"d\": \"hang\"}," +
					"{\"a\": 2, \"b\": 0.5, \"c\": \"b\", \"d\": \"hang\"}," +
					"{\"a\": 3, \"b\": 1.0, \"c\": \"c\", \"d\": \"hang\"}" +
				"]"
				},
			};

			foreach (string[] test in no_recursion_testcases)
			{
				string inp = test[0];
				string desired_out = test[1];
				ii++;
				JNode jinp = jsonParser.Parse(inp);
				Dictionary<string, object> schema = schema_maker.BuildSchema(jinp);
				//Console.WriteLine($"schema for {inp}:\n{schema_maker.SchemaToJNode(schema).ToString()}");
				JNode jdesired_out = jsonParser.Parse(desired_out);
				JNode result = new JNode(null, Dtype.NULL, 0);
				string base_message = $"With recursive search turned off, expected BuildTable({jinp.ToString()})\nto return\n{jdesired_out.ToString()}\n";
				try
				{
					result = no_recursion_tabularizer.BuildTable(jinp, schema);
					try
					{
						if (!jdesired_out.Equals(result))
						{
							tests_failed++;
							Console.WriteLine($"{base_message}Instead returned\n{result.ToString()}");
						}
					}
					catch
					{
						tests_failed++;
						Console.WriteLine($"{base_message}Instead returned\n{result.ToString()}");
					}
				}
				catch (Exception ex)
				{
					tests_failed++;
					Console.WriteLine($"{base_message}Instead threw exception\n{ex}");
				}
			}

			// TEST FULL_RECURSIVE setting
			var full_recursive_tabularizer = new JsonTabularizer(JsonTabularizerStrategy.FULL_RECURSIVE);
			var full_recursive_testcases = new string[][]
			{
				new string[]{
				"[" +
					"[1, 2.5, \"a\"]," +
					"[2, 3.5, \"b\"]" +
				"]", // array of arrays
				"[" +
					"{\"col1\": 1, \"col2\": 2.5, \"col3\": \"a\"}," +
					"{\"col1\": 2, \"col2\": 3.5, \"col3\": \"b\"}" +
				"]"
				},
				new string[]{
				"[" +
					"{\"a\": 1, \"b\": [1, 2, 3], \"c\": {\"d\": [\"y\"]}, \"e\": \"scal\"}, " +
					"{\"a\": 2, \"b\": [4, 5, 6], \"c\": {\"d\": [\"z\"]}, \"e\": \"scal2\"}" +
				"]", // array of deep-nested objects
				"[" +
					"{\"a\": 1, \"b.col1\": 1, \"b.col2\": 2, \"b.col3\": 3, \"c.d.col1\": \"y\", \"e\": \"scal\"}," +
					"{\"a\": 2, \"b.col1\": 4, \"b.col2\": 5, \"b.col3\": 6, \"c.d.col1\": \"z\", \"e\": \"scal2\"}" +
				"]"
				},
			};
			foreach (string[] test in full_recursive_testcases)
			{
				string inp = test[0];
				string desired_out = test[1];
				ii++;
				JNode jinp = jsonParser.Parse(inp);
				Dictionary<string, object> schema = schema_maker.BuildSchema(jinp);
				//Console.WriteLine($"schema for {inp}:\n{schema_maker.SchemaToJNode(schema).ToString()}");
				JNode jdesired_out = jsonParser.Parse(desired_out);
				JNode result = new JNode(null, Dtype.NULL, 0);
				string base_message = $"With full recursive strategy, expected BuildTable({jinp.ToString()})\nto return\n{jdesired_out.ToString()}\n";
				try
				{
					result = full_recursive_tabularizer.BuildTable(jinp, schema);
					try
					{
						if (!jdesired_out.Equals(result))
						{
							tests_failed++;
							Console.WriteLine($"{base_message}Instead returned\n{result.ToString()}");
						}
					}
					catch
					{
						tests_failed++;
						Console.WriteLine($"{base_message}Instead returned\n{result.ToString()}");
					}
				}
				catch (Exception ex)
				{
					tests_failed++;
					Console.WriteLine($"{base_message}Instead threw exception\n{ex}");
				}
			}

			// TEST STRINGIFY_ITERABLES setting
			var stringify_iterables_tabularizer = new JsonTabularizer(JsonTabularizerStrategy.STRINGIFY_ITERABLES);
			var stringify_iterables_testcases = new string[][]
			{
				new string[]{
				"[" +
					"[1, 2.5, \"a\"]," +
					"[2, 3.5, \"b\"]" +
				"]", // array of arrays
				"[" +
					"{\"col1\": 1, \"col2\": 2.5, \"col3\": \"a\"}," +
					"{\"col1\": 2, \"col2\": 3.5, \"col3\": \"b\"}" +
				"]"
				},
				new string[]{
				"[" +
					"{\"a\": 1, \"b\": [1, 2, 3], \"c\": {\"d\": \"y\"}}, " +
					"{\"a\": 2, \"b\": [4, 5, 6], \"c\": {\"d\": \"z\"}}" +
				"]", // array of deep-nested objects
				"[" +
					"{\"a\": 1, \"b\": \"[1, 2, 3]\", \"c\": \"{\\\"d\\\": \\\"y\\\"}\"}," +
					"{\"a\": 2, \"b\": \"[4, 5, 6]\", \"c\": \"{\\\"d\\\": \\\"z\\\"}\"}" +
				"]"
				},
				new string[]{
				"[" +
					"{\"foo\": \"bar\", \"baz\": " +
						"{\"retweet_count\": 5, \"retweeted\": false, " +
						"\"source\": \"<a href=\\\"http://twitter.com/download/android\\\" rel=\\\"nofollow\\\">Twitter for Android</a>\"}" +
					"}" +
				"]", // TODO: find best way to handle stringify_iterables with literal quote chars in string in stringified object
				"[" +
					"{\"foo\": \"bar\", \"baz\": " +
						"\"{\\\"retweet_count\\\": 5, \\\"retweeted\\\": false, " +
						"\\\"source\\\": \\\"<a href=\\\\\\\"http://twitter.com/download/android\\\\\\\" rel=\\\\\\\"nofollow\\\\\\\">Twitter for Android</a>\\\"}\"" +
					"}" +
				"]"
				},
		};

			foreach (string[] test in stringify_iterables_testcases)
			{
				string inp = test[0];
				string desired_out = test[1];
				ii++;
				JNode jinp = jsonParser.Parse(inp);
				Dictionary<string, object> schema = schema_maker.BuildSchema(jinp);
				//Console.WriteLine($"schema for {inp}:\n{schema_maker.SchemaToJNode(schema).ToString()}");
				JNode jdesired_out = jsonParser.Parse(desired_out);
				JNode result = new JNode(null, Dtype.NULL, 0);
				string base_message = $"With stringified iterables, expected BuildTable({jinp.ToString()})\nto return\n{jdesired_out.ToString()}\n";
				try
				{
					result = stringify_iterables_tabularizer.BuildTable(jinp, schema);
					try
					{
						if (!jdesired_out.Equals(result))
						{
							tests_failed++;
							Console.WriteLine($"{base_message}Instead returned\n{result.ToString()}");
						}
					}
					catch
					{
						tests_failed++;
						Console.WriteLine($"{base_message}Instead returned\n{result.ToString()}");
					}
				}
				catch (Exception ex)
				{
					tests_failed++;
					Console.WriteLine($"{base_message}Instead threw exception\n{ex}");
				}
			}

			Console.WriteLine($"Failed {tests_failed} tests.");
			Console.WriteLine($"Passed {ii - tests_failed} tests.");
		}
    }
}