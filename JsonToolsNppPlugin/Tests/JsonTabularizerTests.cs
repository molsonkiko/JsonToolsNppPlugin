using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;
using Kbg.NppPluginNET.PluginInfrastructure;

namespace JSON_Tools.Tests
{
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
				},
				new string[]
				{
				"[" +
					"{\"a\": [1], \"b\": 1.5, \"c\": [\"x\"]}, " +
                    "{\"a\": [], \"b\": 3}" +
                "]", // empty arrays in default algorithm
				"[" +
					"{\"a\": 1, \"b\": 1.5, \"c\": \"x\"}," +
                    "{\"a\": \"\", \"b\": 3}" +
                "]",
				"."
				},
				new string[]
				{
					"[[1, 3, 4], []]", // empty arrays in arrays
                    "[{\"col1\": 1, \"col2\": 3, \"col3\": 4}, {}]",
					"."
				}
			};
			JsonParser jsonParser = new JsonParser();
			JsonTabularizer tabularizer = new JsonTabularizer();
			int tests_failed = 0;
			int ii = 0;
			foreach (string[] test in testcases)
			{
				string inp = test[0];
				string desired_out = test[1];
				string key_sep = test[2];
				ii++;
				JNode jinp = jsonParser.Parse(inp);
				Dictionary<string, object> schema = JsonSchemaMaker.BuildSchema(jinp);
				//Npp.AddText($"schema for {inp}:\n{JsonSchemaMaker.SchemaToJNode(schema).ToString()}");
				JNode jdesired_out = jsonParser.Parse(desired_out);
				JNode result = new JNode();
				string base_message = $"Expected BuildTable({jinp.ToString()})\nto return\n{jdesired_out.ToString()}\n";
				try
				{
					result = tabularizer.BuildTable(jinp, schema, key_sep);
					try
					{
						if (!jdesired_out.Equals(result))
						{
							tests_failed++;
							Npp.AddLine($"{base_message}Instead returned\n{result.ToString()}");
						}
					}
					catch
					{
						tests_failed++;
						Npp.AddLine($"{base_message}Instead returned\n{result.ToString()}");
					}
				}
				catch (Exception ex)
				{
					tests_failed++;
					Npp.AddLine($"{base_message}Instead threw exception\n{ex}");
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
				"a\n1\n2\n3\n", // one column
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
				},
				new object[]
                {
				"[{\"a\": \"\\u042f\", \"b\": 1, \"c\": \"a\"}," + // backward R cyrillic char
                 "{\"a\": \"\\u25d0\", \"b\": 2, \"c\": \"b\"}," + // circle where half black and half white
                 "{\"a\": \"\\u1ed3\", \"b\": 3, \"c\": \"c\"}," + // o with hat and accent
                 "{\"a\": \"\\uff6a\", \"b\": 4, \"c\": \"d\"}, " + // HALFWIDTH KATAKANA LETTER SMALL E
                 "{\"a\": \"\\u8349\", \"b\": 5, \"c\": \"e\"}," + // Taiwanese char for "grass"
				 "{\"a\": \"Love \\u8349. It \\ud83d\\ude00\", \"b\": 5, \"c\": \"e\"}," + // ascii to non-ascii back to ascii and then to emoji
                 "{\"a\": \"\\ud83d\\ude00\", \"b\": 6, \"c\": \"f\"}]", // smily face (\U0001F600)
				"a,b,c\n" +
                "\u042f,1,a\n" +
                "\u25d0,2,b\n" +
                "\u1ed3,3,c\n" +
				"\uff6a,4,d\n" +
                "\u8349,5,e\n" +
				"Love \u8349. It \ud83d\ude00,5,e\n" +
                "\ud83d\ude00,6,f\n",
				',', '"', null, false
				},
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
				string message_without_desired = $"With default strategy, expected TableToCsv({inp}, '{delim}', '{quote_char}', {head_str})\nto return\n";
				string base_message = $"{message_without_desired}{desired_out}\n";
				int msg_len = Encoding.UTF8.GetByteCount(desired_out) + 1 + message_without_desired.Length;
				try
				{
					result = tabularizer.TableToCsv((JArray)table, delim, quote_char, header, bools_as_ints);
					int result_len = Encoding.UTF8.GetByteCount(result);
					try
					{
						if (!desired_out.Equals(result))
						{
							tests_failed++;
							Npp.editor.AppendText(msg_len + 17 + result_len + 1, $"{base_message}Instead returned\n{result}\n");
						}
					}
					catch (Exception ex)
					{
						tests_failed++;
						int ex_len = ex.ToString().Length;
						Npp.editor.AppendText(msg_len + 17 + result_len + 21 + ex_len + 1, 
							$"{base_message}Instead returned\n{result}\nand threw exception\n{ex}\n");
					}
				}
				catch (Exception ex)
				{
					tests_failed++;
					Npp.AddLine($"{base_message}Instead threw exception\n{ex}");
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
				Dictionary<string, object> schema = JsonSchemaMaker.BuildSchema(jinp);
				//Npp.AddText($"schema for {inp}:\n{JsonSchemaMaker.SchemaToJNode(schema).ToString()}");
				JNode jdesired_out = jsonParser.Parse(desired_out);
				JNode result = new JNode();
				string base_message = $"With recursive search turned off, expected BuildTable({jinp.ToString()})\nto return\n{jdesired_out.ToString()}\n";
				try
				{
					result = no_recursion_tabularizer.BuildTable(jinp, schema);
					try
					{
						if (!jdesired_out.Equals(result))
						{
							tests_failed++;
							Npp.AddLine($"{base_message}Instead returned\n{result.ToString()}");
						}
					}
					catch
					{
						tests_failed++;
						Npp.AddLine($"{base_message}Instead returned\n{result.ToString()}");
					}
				}
				catch (Exception ex)
				{
					tests_failed++;
					Npp.AddLine($"{base_message}Instead threw exception\n{ex}");
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
				Dictionary<string, object> schema = JsonSchemaMaker.BuildSchema(jinp);
				//Npp.AddText($"schema for {inp}:\n{JsonSchemaMaker.SchemaToJNode(schema).ToString()}");
				JNode jdesired_out = jsonParser.Parse(desired_out);
				JNode result = new JNode();
				string base_message = $"With full recursive strategy, expected BuildTable({jinp.ToString()})\nto return\n{jdesired_out.ToString()}\n";
				try
				{
					result = full_recursive_tabularizer.BuildTable(jinp, schema);
					try
					{
						if (!jdesired_out.Equals(result))
						{
							tests_failed++;
							Npp.AddLine($"{base_message}Instead returned\n{result.ToString()}");
						}
					}
					catch
					{
						tests_failed++;
						Npp.AddLine($"{base_message}Instead returned\n{result.ToString()}");
					}
				}
				catch (Exception ex)
				{
					tests_failed++;
					Npp.AddLine($"{base_message}Instead threw exception\n{ex}");
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
				"]", // literal quote chars in string in stringified object
				"[" +
					"{\"foo\": \"bar\", \"baz\": " +
						"\"{\\\"retweet_count\\\": 5, \\\"retweeted\\\": false, " +
						"\\\"source\\\": \\\"<a href=\\\\\\\"http://twitter.com/download/android\\\\\\\" rel=\\\\\\\"nofollow\\\\\\\">Twitter for Android</a>\\\"}\"" +
					"}" +
				"]"
				},
				new string[]{ // object with child arrays, one of which contains sub-iterables
					"{\"a\": 1.5, \"b\": [[1], [\"x\"], {\"y\": \"z\"}], \"c\": [\"a\", \"b\", \"c\"]}",
					"[" +
						"{\"a\": 1.5, \"b\": \"[1]\", \"c\": \"a\"}," +
                        "{\"a\": 1.5, \"b\": \"[\\\"x\\\"]\", \"c\": \"b\"}," +
                        "{\"a\": 1.5, \"b\": \"{\\\"y\\\": \\\"z\\\"}\", \"c\": \"c\"}" +
                    "]"
				},
				new string[]
				{ // object with child arrays, one of which is longer than the others
					"{\"a\": [\"x\", \"y\", \"z\"], \"b\": [[1], [2,3]], \"c\": [1.0,2.0,3.0,4.0]}",
					"[" +
						"{\"a\": \"x\", \"b\": \"[1]\", \"c\": 1.0}, " +
                        "{\"a\": \"y\", \"b\": \"[2, 3]\", \"c\": 2.0}, " +
                        "{\"a\": \"z\", \"b\": \"\", \"c\": 3.0}, " +
                        "{\"a\": \"\", \"b\": \"\", \"c\": 4.0} " +
                    "]"
				},
                new string[]
                { // object with child arrays, one of which is longer than the others, and a non-array
					"{\"a\": [\"x\", \"y\", \"z\"], \"b\": [[1], [2.2,3]], \"c\": NaN}",
                    "[" +
                        "{\"a\": \"x\", \"b\": \"[1]\", \"c\": NaN}, " +
                        "{\"a\": \"y\", \"b\": \"[2.2, 3]\", \"c\": NaN}, " +
                        "{\"a\": \"z\", \"b\": \"\", \"c\": NaN}" +
                    "]"
                },
                new string[]{
                "[" +
                    "[1, [2.5], \"a\"]," +
                    "[2, [3.5], \"b\"]" +
                "]", // array of deep-nested arrays
				"[" +
                    "{\"col1\": 1, \"col2\": \"[2.5]\", \"col3\": \"a\"}," +
                    "{\"col1\": 2, \"col2\": \"[3.5]\", \"col3\": \"b\"}" +
                "]"
                },
        };

			foreach (string[] test in stringify_iterables_testcases)
			{
				string inp = test[0];
				string desired_out = test[1];
				ii++;
				JNode jinp = jsonParser.Parse(inp);
				Dictionary<string, object> schema = JsonSchemaMaker.BuildSchema(jinp);
				//Npp.AddText($"schema for {inp}:\n{JsonSchemaMaker.SchemaToJNode(schema).ToString()}");
				JNode jdesired_out = jsonParser.Parse(desired_out);
				JNode result = new JNode();
				string base_message = $"With stringified iterables, expected BuildTable({jinp.ToString()})\nto return\n{jdesired_out.ToString()}\n";
				try
				{
					result = stringify_iterables_tabularizer.BuildTable(jinp, schema);
					try
					{
						if (!jdesired_out.Equals(result))
						{
							tests_failed++;
							Npp.AddLine($"{base_message}Instead returned\n{result.ToString()}");
						}
					}
					catch
					{
						tests_failed++;
						Npp.AddLine($"{base_message}Instead returned\n{result.ToString()}");
					}
				}
				catch (Exception ex)
				{
					tests_failed++;
					Npp.AddLine($"{base_message}Instead threw exception\n{ex}");
				}
			}
			
			Npp.AddLine($"Failed {tests_failed} tests.");
			Npp.AddLine($"Passed {ii - tests_failed} tests.");
		}
	}
}
