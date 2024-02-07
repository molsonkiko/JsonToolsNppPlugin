using System;
using System.Collections.Generic;
using System.Text;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;

namespace JSON_Tools.Tests
{
	public class JsonTabularizerTester
	{
		public static bool Test()
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
			JsonParser jsonParser = new JsonParser(LoggerLevel.JSON5);
			JsonTabularizer tabularizer = new JsonTabularizer();
			int testsFailed = 0;
			int ii = 0;
			foreach (string[] test in testcases)
			{
				string inp = test[0];
				string desiredOut = test[1];
				string keySep = test[2];
				JNode jinp;
				ii++;
				try
				{
					jinp = jsonParser.Parse(inp);
				}
				catch (Exception ex)
				{
					testsFailed++;
					Npp.AddLine($"While trying to parse {inp}, got exception {RemesParser.PrettifyException(ex)}");
					continue;
				}
				Dictionary<string, object> schema = JsonSchemaMaker.BuildSchema(jinp);
				//Npp.AddText($"schema for {inp}:\n{JsonSchemaMaker.SchemaToJNode(schema).ToString()}");
				JNode jdesiredOut = jsonParser.Parse(desiredOut);
				JNode result = new JNode();
				string baseMessage = $"Expected BuildTable({jinp.ToString()})\nto return\n{jdesiredOut.ToString()}\n";
				try
				{
					result = tabularizer.BuildTable(jinp, schema, keySep);
					if (!jdesiredOut.TryEquals(result, out _))
					{
						testsFailed++;
						Npp.AddLine($"{baseMessage}Instead returned\n{result.ToString()}");
					}
				}
				catch (Exception ex)
				{
					testsFailed++;
					Npp.AddLine($"{baseMessage}Instead threw exception\n{ex}");
				}
			}

			// TEST CSV CREATION
			JsonParser fancyParser = new JsonParser(LoggerLevel.JSON5);

			var csvTestcases = new (string inp, string desiredOut, char delim, char quoteChar, string[] header, bool boolsAsInts, string eol)[]
			{
				( "[{\"a\": 1, \"b\": \"a\"}, {\"a\": 2, \"b\": \"b\"}]", "a,b\r\n1,a\r\n2,b\r\n", ',', '"', null, false, "\r\n" ),
				( "[{\"a\": 1, \"b\": \"a\"}, {\"a\": 2, \"b\": \"b\"}]", "a,b\r1,a\r2,b\r", ',', '"', null, false, "\r" ),
				( "[{\"a\": 1, \"b\": \"a\"}, {\"a\": 2, \"b\": \"b\"}]", "a,b\n1,a\n2,b\n", ',', '"', null, false, "\n" ),
				(
				"[{\"a\": 1, \"b\": 2, \"c\": 3}, {\"a\": 2, \"b\": 3, \"d\": 4}, {\"a\": 1, \"b\": 2}]",
				"a,b,c,d\r\n" + // test missing entries in some rows
				"1,2,3,\r\n" +
				"2,3,,4\r\n" +
				"1,2,,\r\n",
				',', '"', null, false, "\r\n"
				),
				(
				"[" +
                    "{\"a\": 1, \"b\": \"[1, 2, 3]\", \"c\": \"{\\\"d\\\": \\\"y\\\"}\"}," +
                    "{\"a\": 2, \"b\": \"[4, 5, 6]\", \"c\": \"{\\\"d\\\": \\\"z\\\"}\"}" +
                "]", // test stringified iterables
				"a\tb\tc\r\n" +
				"1\t[1, 2, 3]\t\"{\"\"d\"\": \"\"y\"\"}\"\r\n" +
				"2\t[4, 5, 6]\t\"{\"\"d\"\": \"\"z\"\"}\"\r\n",
				'\t', '"', null, false, "\r\n"
				),
				( "[{\"a\": null, \"b\": 1.0}, {\"a\": \"blah\", \"b\": NaN}]", // nulls and NaNs
				"a,b\r\n,1.0\r\nblah,NaN\r\n",
				',', '"', null, false, "\r\n"
				),
				( "[{\"a\": 1, \"b\": \"a\"}, {\"a\": 2, \"b\": \"b\"}]", "a\tb\r\n1\ta\r\n2\tb\r\n", '\t', '"', null, false, "\r\n" ),
				( "[{\"a\": 1, \"b\": \"a\"}, {\"a\": 2, \"b\": \"b\"}]", "a,b\r\n1,a\r\n2,b\r\n", ',', '\'', null, false, "\r\n" ),
				( "[{\"a\": 1, \"b\": \"a\"}, {\"a\": 2, \"b\": \"b\"}]", "a\tb\r\n1\ta\r\n2\tb\r\n", '\t', '\'', null, false, "\r\n" ),
				( "[{\"a\": 1, \"b\": \"a\"}, {\"a\": 2, \"b\": \"b\"}]", "b,a\r\na,1\r\nb,2\r\n", ',', '"', new string[]{"b", "a"}, false, "\r\n" ),
				( "[{\"a\": 1, \"b\": \"a\"}, {\"a\": 2, \"b\": \"b\"}]", "b\ta\r\na\t1\r\nb\t2\r\n", '\t', '"', new string[]{"b", "a"}, false, "\r\n" ),
				( "[{\"a\": 1, \"b\": \"a,b\"}, {\"a\": 2, \"b\": \"c\"}]", "a,b\r\n1,\"a,b\"\r\n2,c\r\n", ',', '"', null, false, "\r\n" ),
				( "[{\"a\": 1, \"b\": \"a,b\"}, {\"a\": 2, \"b\": \"c\"}]", "a,b\r\n1,'a,b'\r\n2,c\r\n", ',', '\'', null, false, "\r\n" ), // delims in values
				( "[{\"a\": 1, \"b\": \"a,b\"}, {\"a\": 2, \"b\": \"c\"}]", "a,b\r1,'a,b'\r2,c\r", ',', '\'', null, false, "\r" ), // delims in values
				( "[{\"a\": 1, \"b\": \"a,b\"}, {\"a\": 2, \"b\": \"c\"}]", "a,b\n1,'a,b'\n2,c\n", ',', '\'', null, false, "\n" ), // delims in values
				( "[{\"a,b\": 1, \"b\": \"a\"}, {\"a,b\": 2, \"b\": \"b\"}]", "\"a,b\",b\r\n1,a\r\n2,b\r\n", ',', '"', null, false, "\r\n" ),
				( "[{\"a,b\": 1, \"b\": \"a\"}, {\"a,b\": 2, \"b\": \"b\"}]", "'a,b',b\r\n1,a\r\n2,b\r\n", ',', '\'', null, false, "\r\n" ),
				( "[{\"a,b\": 1, \"b\": \"a\"}, {\"a,b\": 2, \"b\": \"b\"}]", // internal delims in column header
				"b,\"a,b\"\r\na,1\r\nb,2\r\n",
				',', '"', new string[]{"b", "a,b"}, false, "\r\n"
				),
				( "[{\"a\\tb\": 1, \"b\": \"a\"}, {\"a\\tb\": 2, \"b\": \"b\"}]", // \t in column header when \t is delim
				"a\\tb\tb\r\n1\ta\r\n2\tb\r\n",
				'\t', '"', null, false, "\r\n"
				),
				( "[{\"a\": 1, \"b\": \"a\\tb\"}, {\"a\": 2, \"b\": \"c\"}]",
				"a\tb\r\n1\t\"a\tb\"\r\n2\tc\r\n",
				'\t', '"', null, false, "\r\n"
				),
				("[{\"a\": 1}, {\"a\": 2}, {\"a\": 3}]",
				"a\r\n1\r\n2\r\n3\r\n", // one column
				',', '"', null, false, "\r\n"
				),
				("[{\"a\": 1, \"b\": 2, \"c\": 3}, {\"a\": 2, \"b\": 3, \"c\": 4}, {\"a\": 3, \"b\": 4, \"c\": 5}]",
				"a|b|c\r\n1|2|3\r\n2|3|4\r\n3|4|5\r\n",
				'|', '"', null, false, "\r\n"
				),
				("[{\"date\": \"1999-01-03\", \"cost\": 100.5, \"num\": 13}, {\"date\": \"2000-03-15\", \"cost\": 157.0, \"num\": 17}]",
				"cost,date,num\r\n100.5,1999-01-03,13\r\n157.0,2000-03-15,17\r\n", // dates
				',', '"', null, false, "\r\n"
				),
				("[{\"date\": \"1999-01-03 07:03:29\", \"cost\": 100.5, \"num\": 13}]",
				"cost,date,num\r\n100.5,1999-01-03 07:03:29,13\r\n", // datetimes
				',', '"', null, false, "\r\n"
				),
				("[{\"name\": \"The Exalted \\\"Samuel Blutentharst\\\" of Doom\", \"phone number\": \"420-997-1043\"}," +
				"{\"name\": \"\\\"Fjordlak the Deranged\\\"\", \"phone number\": \"blo-od4-blud\"}]", // internal quote chars
				"name,phone number\r\n\"The Exalted \"\"Samuel Blutentharst\"\" of Doom\",420-997-1043\r\n\"\"\"Fjordlak the Deranged\"\"\",blo-od4-blud\r\n",
				',', '"', null, false, "\r\n"
				),
				("[{\"a\": \"new\\r\\nline\", \"b\": 1}]", // internal newlines
				"a,b\r\n\"new\r\nline\",1\r\n",
				',', '"', null, false, "\r\n"
				),
                ("[{\"a\": \"new\\rline\", \"b\": 1}]", // internal newlines
				"a,b\r\"new\rline\",1\r",
                ',', '"', null, false, "\r"
                ),
                ("[{\"a\": \"n,ew\\nl'i\\rne\", \"b\": 1, \"c\": \"a\\r\\nbc\"}]", // internal newlines and quote chars (use '\'') and delims
				"a,b,c\r\n'n,ew\nl''i\rne',1,'a\r\nbc'\r\n",
                ',', '\'', null, false, "\r\n"
                ),
                (
				"[{\"a\": true, \"b\": \"foo\"}, {\"a\": false, \"b\": \"bar\"}]", // boolean values with boolsAsInts false
				"a,b\r\ntrue,foo\r\nfalse,bar\r\n",
				',', '"', null, false, "\r\n"
				),
				(
				"[{\"a\": true, \"b\": \"foo\"}, {\"a\": false, \"b\": \"bar\"}]", // boolean values with boolsAsInts true
				"a,b\r\n1,foo\r\n0,bar\r\n",
				',', '"', null, true, "\r\n"
				),
				(
				"[" +
					"{\"a\": 1, \"b\": 1, \"c.d\": \"y\"}," +
					"{\"a\": true, \"b\": 7}," +
					"{\"a\": true, \"b\": 8}," +
					"{\"a\": false, \"b\": 9}" +
				"]", // missing keys
				"a,b,c.d\r\n1,1,y\r\ntrue,7,\r\ntrue,8,\r\nfalse,9,\r\n",
				',', '"', null, false, "\r\n"
				),
				("[{\"a\": \"\\u042f\", \"b\": 1, \"c\": \"a\"}," + // backward R cyrillic char
                 "{\"a\": \"\\u25d0\", \"b\": 2, \"c\": \"b\"}," + // circle where half black and half white
                 "{\"a\": \"\\u1ed3\", \"b\": 3, \"c\": \"c\"}," + // o with hat and accent
                 "{\"a\": \"\\uff6a\", \"b\": 4, \"c\": \"d\"}, " + // HALFWIDTH KATAKANA LETTER SMALL E
                 "{\"a\": \"\\u8349\", \"b\": 5, \"c\": \"e\"}," + // Taiwanese char for "grass"
				 "{\"a\": \"Love \\u8349. It \\ud83d\\ude00\", \"b\": 5, \"c\": \"e\"}," + // ascii to non-ascii back to ascii and then to emoji
                 "{\"a\": \"\\ud83d\\ude00\", \"b\": 6, \"c\": \"f\"}]", // smily face (\U0001F600)
				"a,b,c\r\n" +
                "\u042f,1,a\r\n" +
                "\u25d0,2,b\r\n" +
                "\u1ed3,3,c\r\n" +
				"\uff6a,4,d\r\n" +
                "\u8349,5,e\r\n" +
				"Love \u8349. It \ud83d\ude00,5,e\r\n" +
                "\ud83d\ude00,6,f\r\n",
				',', '"', null, false, "\r\n"
				),
			};
			foreach ((string inp, string desiredOut, char delim, char quote, string[] header, bool boolsAsInts, string eol) in csvTestcases)
			{
				ii++;
				JNode table = fancyParser.Parse(inp);
				string result = "";
				string headStr = header == null ? "null" : '[' + string.Join(", ", header) + ']';
				string escapedEol = JNode.StrToString(eol, true);
				string messageWithoutDesired = $"With default strategy, expected TableToCsv({inp}, '{delim}', '{quote}', {headStr}, {escapedEol})\nto return\n";
				string baseMessage = $"{messageWithoutDesired}{desiredOut}\n";
				int msgLen = Encoding.UTF8.GetByteCount(desiredOut) + 1 + messageWithoutDesired.Length;
				try
				{
					result = tabularizer.TableToCsv((JArray)table, delim, quote, eol, header, boolsAsInts);
					int resultLen = Encoding.UTF8.GetByteCount(result);
					try
					{
						if (!desiredOut.Equals(result))
						{
							testsFailed++;
							Npp.editor.AppendText(msgLen + 17 + resultLen + 1, $"{baseMessage}Instead returned\n{result}\n");
						}
					}
					catch (Exception ex)
					{
						testsFailed++;
						int exLen = ex.ToString().Length;
						Npp.editor.AppendText(msgLen + 17 + resultLen + 21 + exLen + 1, 
							$"{baseMessage}Instead returned\n{result}\nand threw exception\n{ex}\n");
					}
				}
				catch (Exception ex)
				{
					testsFailed++;
					Npp.AddLine($"{baseMessage}Instead threw exception\n{ex}");
				}
			}
			// TEST NO_RECURSION setting
			var noRecursionTabularizer = new JsonTabularizer(JsonTabularizerStrategy.NO_RECURSION);
			var noRecursionTestcases = new string[][]
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

			foreach (string[] test in noRecursionTestcases)
			{
				string inp = test[0];
				string desiredOut = test[1];
				ii++;
				JNode jinp = jsonParser.Parse(inp);
				Dictionary<string, object> schema = JsonSchemaMaker.BuildSchema(jinp);
				//Npp.AddText($"schema for {inp}:\n{JsonSchemaMaker.SchemaToJNode(schema).ToString()}");
				JNode jdesiredOut = jsonParser.Parse(desiredOut);
				JNode result = new JNode();
				string baseMessage = $"With recursive search turned off, expected BuildTable({jinp.ToString()})\nto return\n{jdesiredOut.ToString()}\n";
				try
				{
					result = noRecursionTabularizer.BuildTable(jinp, schema);
					if (!jdesiredOut.TryEquals(result, out _))
					{
						testsFailed++;
						Npp.AddLine($"{baseMessage}Instead returned\n{result.ToString()}");
					}
				}
				catch (Exception ex)
				{
					testsFailed++;
					Npp.AddLine($"{baseMessage}Instead threw exception\n{ex}");
				}
			}

			// TEST FULL_RECURSIVE setting
			var fullRecursiveTabularizer = new JsonTabularizer(JsonTabularizerStrategy.FULL_RECURSIVE);
			var fullRecursiveTestcases = new string[][]
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
			foreach (string[] test in fullRecursiveTestcases)
			{
				string inp = test[0];
				string desiredOut = test[1];
				ii++;
				JNode jinp = jsonParser.Parse(inp);
				Dictionary<string, object> schema = JsonSchemaMaker.BuildSchema(jinp);
				//Npp.AddText($"schema for {inp}:\n{JsonSchemaMaker.SchemaToJNode(schema).ToString()}");
				JNode jdesiredOut = jsonParser.Parse(desiredOut);
				JNode result = new JNode();
				string baseMessage = $"With full recursive strategy, expected BuildTable({jinp.ToString()})\nto return\n{jdesiredOut.ToString()}\n";
				try
				{
					result = fullRecursiveTabularizer.BuildTable(jinp, schema);
					if (!jdesiredOut.TryEquals(result, out _))
					{
						testsFailed++;
						Npp.AddLine($"{baseMessage}Instead returned\n{result.ToString()}");
					}
				}
				catch (Exception ex)
				{
					testsFailed++;
					Npp.AddLine($"{baseMessage}Instead threw exception\n{ex}");
				}
			}

			// TEST STRINGIFY_ITERABLES setting
			var stringifyIterablesTabularizer = new JsonTabularizer(JsonTabularizerStrategy.STRINGIFY_ITERABLES);
			var stringifyIterablesTestcases = new string[][]
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

			foreach (string[] test in stringifyIterablesTestcases)
			{
				string inp = test[0];
				string desiredOut = test[1];
				ii++;
				JNode jinp = jsonParser.Parse(inp);
				Dictionary<string, object> schema = JsonSchemaMaker.BuildSchema(jinp);
				//Npp.AddText($"schema for {inp}:\n{JsonSchemaMaker.SchemaToJNode(schema).ToString()}");
				JNode jdesiredOut = jsonParser.Parse(desiredOut);
				JNode result = new JNode();
				string baseMessage = $"With stringified iterables, expected BuildTable({jinp.ToString()})\nto return\n{jdesiredOut.ToString()}\n";
				try
				{
					result = stringifyIterablesTabularizer.BuildTable(jinp, schema);
					if (!jdesiredOut.TryEquals(result, out _))
					{
						testsFailed++;
						Npp.AddLine($"{baseMessage}Instead returned\n{result.ToString()}");
					}
				}
				catch (Exception ex)
				{
					testsFailed++;
					Npp.AddLine($"{baseMessage}Instead threw exception\n{ex}");
				}
			}
			
			Npp.AddLine($"Failed {testsFailed} tests.");
			Npp.AddLine($"Passed {ii - testsFailed} tests.");
			return testsFailed > 0;
		}
	}
}
