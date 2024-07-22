using System;
using JSON_Tools.Utils;

namespace JSON_Tools.Tests
{
    public class GlobTester
    {
        public static bool Test()
        {
            int ii = 0;
            int failed = 0;
            var testcases = new (string query, (string input, bool desiredResult)[])[]
            {
                ("foo bar", new[]
                {
                    ("foobar.txt", true),
                    ("bar.foo", true),
                    ("bar.txt", false),
                    ("foo.txt", false),
                }),
                ("foo bar txt", new[]
                {
                    ("foobar.txt", true),
                    ("bar.foo", false),
                    ("bar.txt", false),
                    ("foo.txt", false),
                }),
                ("foo;bar;*.txt", new[]
                {
                    ("foobar.txt", true),
                    ("bar.foo", false),
                    ("bar.txt", false),
                    ("foo.txt", false),
                }),
                ("foo | bar", new[]
                {
                    ("foobar.txt", true),
                    ("bar.txt", true),
                    ("foo.txt", true),
                    ("ghi.txt", false),
                }),
                ("!bar", new[]
                {
                    ("foo.txt", true),
                    ("bar.txt", false),
                }),
                ("foo !bar", new[]
                {
                    ("foobar.txt", false),
                    ("foo.txt", true),
                    ("ghi.txt", false),
                }),
                ("foo | < !bar baz", new[]
                {
                    ("foo.bar", true),
                    ("bar.baz", false),
                    ("baz.txt", true),
                    ("foo.baz", true),
                }),
                ("foo**[!c-p]u\\u0434ck*.{tx?,cpp} !fgh | <ba\\x72 baz", new[]
                {
                    ("foo\\boo\\quдcked.txt", true),
                    ("foo\\boo\\quдcked.cpp", true),
                    ("foozuдck.txb", true),
                    ("bar.baz", true),
                    ("baz.bar", true),
                    ("fgh\\bar.baz", true),
                    ("foo\\boo\\duдcked.txt", false),
                    ("foo\\boo\\uдcked.txt", false),
                    ("foozuдck.xml", false),
                    ("foo.baz", false),
                    ("foo\\boo\\quдcked.txto", false),
                    ("foo\\fgh\\quдcked.txt", false),
                    ("foo\\fgh\\quдcked.cpp", false),
                    ("foo\\boo\\quдcked\\bad.txt", false),
                }),
                ("foo[ !]*.*", new[]
                {
                    ("foo!man.txt", true),
                    ("foo man.txt", true),
                    ("footman.txt", false),
                    ("C:\\reoreno\\84303\\foo .eorn", true),
                    ("foo!.zzy", true),
                }),
                ("foo[! ].*", new[]
                {
                    ("foot.txt", true),
                    ("foo .txt", false),
                    ("boot.txt", false),
                    ("foo\\.txt", false),
                }),
                ("**!bar", new[]
                {
                    ("", true),
                    ("reore", true),
                    ("$#@.~~~", true),
                    ("bar.txt", false),
                }),
                ("foo|bar!baz", new[]
                {
                    ("bar.txt", true),
                    ("foo.txt", true),
                    ("foo.baz", false),
                    ("bar.baz", false),
                }),
                ("^.()[[ ]]$.*", new[]
                {
                    ("^.()[]$.xml", true),
                    ("^.() ]$.xml", true),
                    ("^.()]]$.xml", false),
                    ("foo.xml", false),
                }),
                ("*[*].{h,c[px][px]}", new[]
                {
                    ("foo*.h", true),
                    ("bar*.cpp", true),
                    ("baz*.cpx", true),
                    ("foo\\qux*.cxp", true),
                    ("roeu\\843*.cxx", true),
                    ("roeu\\843*.c\\x", false),
                    ("foo\\qux.cxp", false),
                    ("foo.h", false),
                    ("roeu\\843*.cxxe", false),
                    ("roeu\\843*.xxx", false),
                    ("843.cxx", false),
                }),
                ("{foo,bar}.{txt,md}", new[]
                {
                    ("foo.txt", true),
                    ("foo.md", true),
                    ("bar.txt", true),
                    ("bar.md", true),
                    ("bar.baz", false),
                }),
                ("foo{ baz bar[", new[] // test ignoring of globs with syntactically invalid globs
                {
                    ("baz.txt", true),
                    ("foo.txt", false),
                }),
                ("baz *.foo{1,2} bar{", new[]
                {
                    ("baz.foo1", true),
                    ("c:\\bazel.foo2", true),
                    ("baz\\fjie.foo1", true),
                    ("baz\\fjie\\foo3", false),
                    ("baz\\fjie\\foo1", false),
                    ("bazfjie.foo3", false),
                    ("baz\\fjie\\foo12", false),
                }),
                ("\\x20\\x21a\\u0434.txt", new[] // " !aд", check support for \xNN, \uNNNN
                {
                    (" !aд.txt", true),
                    ("foo\\ !aд.txt", true),
                    ("!aд.txt", false),
                    ("aд.txt", false),
                    ("д.md", false),
                }),
                ("**.txt | *.json", new[]
                {
                    ("Z:\\foo.json", true),
                    ("c:\\goon\\guk.txt", true),
                    ("foo.jsonl", false),
                    ("c:\\bar.txto", false),
                }),
                ("*.p* !< __ | \\j", new[]
                {
                    ("bep.po", true),
                    ("joo.po", true),
                    ("boo\\joo.po", false),
                    ("bbq.go", false),
                    ("__bzz.po", false),
                    ("boo\\j__.po", false),
                    ("boo__\\eorpwn\\reiren.po", false),
                    ("Z:\\jnq\\eorpwn\\reiren.po", false),
                }),
            };
            var glob = new Glob();
            foreach ((string query, var examples) in testcases)
            {
                Func<string, bool> resultFunc;
                try
                {
                    resultFunc = glob.Parse(query);
                }
                catch (Exception ex)
                {
                    ii++;
                    Npp.AddLine($"While parsing query \"{query}\", got exception\r\n{ex}");
                    failed++;
                    continue;
                }
                string failMessage = $"While executing query \"{query}\" on filename";
                foreach ((string filename, bool desiredResult) in examples)
                {
                    ii++;
                    failed += TestGlobFuncOnFilename(resultFunc, failMessage, filename, desiredResult);
                }
            }
            Npp.AddLine($"Ran {ii} tests and failed {failed}");
            return failed > 0;
        }

        public static bool TestParseLinesSimple()
        {
            int ii = 0;
            int failed = 0;
            var testcases = new (string query, (string input, bool desiredResult)[])[]
            {
                ("foo*\nbar*", new[]
                {
                    ("foobar.txt", true),
                    ("txt.foo", true),
                    ("txt.bar", true),
                    ("quz.txt", false),
                }),
                ("f)o*\rb(r\r\n*$xt", new[]
                {
                    ("b(r.rjek", false),
                    ("rke.b(r", true),
                    ("kkb(r", true),
                    ("b(r.baz", false),
                    ("f)o.baz", true),
                    ("f)o.$xt", true),
                    ("f)o.b(r", true),
                    ("zkd.txb", false),
                    ("rj.$xt", true),
                }),
                ("foo\\bar*\nn*/jj.txt", new[]
                {
                    ("foo\\bar.ri", true),
                    ("foo\\bar.foo", true),
                    ("bar.txt", false),
                    ("jn\\jj.txt", true),
                    ("foo\\barfooj.txt", true),
                    ("foo\\barn\\jj.txt", true),
                    ("nkfkfk\\jj.txt", true),
                    ("kfkfk\\jj.txt", false),
                    ("foo\\bartxb.", true),
                }),
                ("foo/*bar", new[]
                {
                    ("foo\\j.bar", true),
                    ("foo\\i.bar", true),
                    ("foo\\bar\\jj.txt.mm", false),
                    ("foo\\barjj.txt.mm", false),
                    ("foo\\barjj.txt.bar", true),
                    ("kkrkrk\\foo\\bazjj.txt.bar", true),
                    ("kkr\\krk\\foo\\bazjj.txt.bar", true),
                    ("kkr\\krk\\foo\\bar", true),
                    ("kkr\\krk\\foo\\baz\\jj.txt.bar", false),
                    ("foo\\barjj.txt.bar", true),
                    ("foo\\bartxb.", false),
                }),
                ("g?**tx??", new[]
                {
                    ("go:\\foo.txtb", true),
                    ("g:\\foo.txtb", true),
                    ("guk.txto", true),
                    ("guk.txt", false),
                    ("juk.txto", false),
                    ("guk.tato", false),
                    ("guk.ta\\o", false),
                    ("rojo.gtxxx", false),
                    ("rojo.gbtx88", true),
                    ("rojo.txxx", false),
                    ("g\\bar.txto", false),
                    ("g\\bar.txt", false),
                    ("gg\\bar.txjj", true),
                }),
                ("g?**\ntx??", new[]
                {
                    ("go:\\foo.txtb", true),
                    ("kfjekrjekr\\jrjrj\\rjrj.txtb", true),
                    ("guk.jjrjr", true),
                    ("guk.txt", true),
                    ("gu\\kjj\\.txt", true),
                    ("g\\uk.txt", false),
                    ("juk.txto", true),
                    ("guk.tato", true),
                    ("rojo.gtxxx", true),
                    ("rojo.tbxx", false),
                    ("g\\bar.txto", true),
                    ("g\\bar.txt", false),
                    ("gg\\bar.tjj", true),
                }),
            };
            var glob = new Glob();
            foreach ((string query, var examples) in testcases)
            {
                Func<string, bool> resultFunc;
                try
                {
                    resultFunc = glob.ParseLinesSimple(query);
                }
                catch (Exception ex)
                {
                    ii++;
                    Npp.AddLine($"While parsing query \"{query}\", got exception\r\n{ex}");
                    failed++;
                    continue;
                }
                string failMessage = $"While executing query \"{query}\" on filename";
                foreach ((string filename, bool desiredResult) in examples)
                {
                    ii++;
                    failed += TestGlobFuncOnFilename(resultFunc, failMessage, filename, desiredResult);
                }
            }
            Npp.AddLine($"Ran {ii} tests and failed {failed}");
            return failed > 0;
        }

        /// <summary>
        /// returns 0 and outputs nothing if test passes<br></br>
        /// returns 1 and outputs something beginning with failMessage if test fails or an exception is raised.
        /// </summary>
        private static int TestGlobFuncOnFilename(Func<string, bool> resultFunc, string failMessage, string filename, bool desiredResult)
        {
            bool result;
            try
            {
                result = resultFunc(filename);
            }
            catch (Exception ex)
            {
                Npp.AddLine($"{failMessage} \"{filename}\", got exception\r\n{ex}");
                return 1;
            }
            if (result != desiredResult)
            {
                Npp.AddLine($"{failMessage} \"{filename}\", EXPECTED {desiredResult}, GOT {result}");
                return 1;
            }
            return 0;
        }

        public static void TestCachedTopDirectory()
        {
            int ii = 0;
            int failed = 0;
            var testcases = new (string query, string topDir, (string input, bool desiredResult)[])[]
            {
                ("foo;bar;*.txt", "Z:\\foo", new[]
                {
                    ("Z:\\foo\\bar.txt", true),
                    ("Z:\\foo\\gozo\\barono.txt", true),
                    ("Z:\\foo\\hund\\goog.txt", false),
                    ("Z:\\foo\\hund\\gut\\foo.txt", false),
                    ("Z:\\foo\\hund\\gut\\txt.bar", false),
                    ("Z:\\foo\\hund\\gut\\bar.txt", true),
                }),
                ("foo;bar;*.txt", "Cj\\bar", new[]
                {
                    ("Cj\\bar\\foo.txt", true),
                    ("Cj\\bar\\gozo\\fooono.txt", true),
                    ("Cj\\bar\\hund\\goog.txt", false),
                    ("Cj\\bar\\hund\\gut\\bar.txt", false),
                    ("Cj\\bar\\hund\\gut\\txt.foo", false),
                    ("Cj\\bar\\hund\\gut\\foo.txt", true),
                }),
                ("foo | bar*", "YOLO\\bar", new[]
                {
                    ("YOLO\\bar\\bar.txt", true),
                    ("YOLO\\bar\\foo.md", true),
                    ("YOLO\\bar\\vingo.cs", false),
                    ("YOLO\\bar\\foo\\ghi.igh", true),
                }),
                ("!bar", "bar", new[]
                {
                    ("bar\\foo.txt", false),
                    ("bar\\bar.txt", false),
                }),
                ("!bar", "foo", new[]
                {
                    ("foo\\foo.txt", true),
                    ("foo\\bar.txt", false),
                }),
                ("foo\r!bar", "bar", new[]
                {
                    ("bar\\foobar.txt", false),
                    ("bar\\foo.txt", false),
                    ("bar\\ghi.txt", false),
                }),
                ("foo\n!bar", "foo", new[]
                {
                    ("bar\\foobar.txt", false),
                    ("bar\\foo.txt", false),
                    ("bar\\ghi.txt", false),
                }),
                ("foo\r\t | \n< !bar baz", "bazel", new[]
                {
                    ("bazel\\foo.c", true),
                    ("bazel\\bar.z", false),
                    ("bazel\\bar\\e.txt", false),
                    ("bazel\\bar\\foo.txt", true),
                }),
                ("foo[ !]*.*", "G:\\foo jou.m", new[]
                {
                    ("G:\\foo jou.m\\foo!man.txt", true),
                    ("G:\\foo jou.m\\foo man.txt", true),
                    ("G:\\foo jou.m\\footman.txt", false),
                    ("G:\\foo jou.m\\C\\reoreno\\84303\\foo .eorn", true),
                    ("G:\\foo jou.m\\foo!.zzy", true),
                }),
                ("!**bar", "baz\\bar", new[]
                {
                    ("baz\\bar\\o.bar", false),
                    ("baz\\bar\\g\\j.k", true),
                }),
                ("**bar txt", "baz\\bar", new[]
                {
                    ("baz\\bar\\o.bar", false),
                    ("baz\\bar\\txt\\o.quz", false),
                    ("baz\\bar\\g\\txt.bar", true),
                }),
                ("{foo,bar}{txt,md}*", "footxt", new[]
                {
                    ("footxt\\foo.txt", false),
                    ("footxt\\foo.md", false),
                    ("footxt\\bartxt.c", true),
                    ("footxt\\barmd\\z.y", false),
                    ("footxt\\barmd.go", true),
                }),
                ("foo bar 2", "foo\\bar", new[]
                {
                    ("foo\\bar\\2.txt", true),
                    ("foo\\bar\\1.c", false),
                    ("foo\\bar\\blah2\\1.h", true),
                }),
                ("2 foo *c", "foo\\bar", new[]
                {
                    ("foo\\bar\\2.txt", false),
                    ("foo\\bar\\2.c", true),
                    ("foo\\bar\\1.c", false),
                    ("foo\\bar\\blah2\\1.pyc", true),
                }),
            };
            var glob = new Glob();
            foreach ((string query, string topDir, var examples) in testcases)
            {
                Func<string, bool> resultFunc;
                try
                {
                    resultFunc = glob.Parse(query);
                }
                catch (Exception ex)
                {
                    ii++;
                    Npp.AddLine($"While parsing query \"{query}\", got exception\r\n{ex}");
                    failed++;
                    continue;
                }
                Func<string, bool> resultFuncWithCachedTopDir;
                try
                {
                    resultFuncWithCachedTopDir = Glob.CacheGlobFuncResultsForTopDirectory(topDir, glob.globFunctions);
                }
                catch (Exception ex)
                {
                    ii++;
                    Npp.AddLine($"While parsing query \"{query}\" (with cached top directory \"{topDir}\"), got exception\r\n{ex}");
                    failed++;
                    continue;
                }
                foreach ((string filename, bool desiredResult) in examples)
                {
                    ii += 2;
                    string failMessage = $"While executing query \"{query}\" on filename";
                    failed += TestGlobFuncOnFilename(resultFunc, failMessage, filename, desiredResult);
                    failMessage = $"Running query \"{query}\" (with cached top directory \"{topDir}\") on filename";
                    failed += TestGlobFuncOnFilename(resultFuncWithCachedTopDir, failMessage, filename, desiredResult);
                }
            }

            // now we test that the caching will cause a result different from the result without caching
            // if the cached top directory is different from the true top directory
            var testcasesWithWrongTopDir = new (string query, string topDir, (string input, bool desiredResult, bool desiredResultWithCaching)[])[]
            {
                ("2 bar *c", "bar", new[]
                {
                    ("foo\\2.txt", false, false),
                    ("foo\\2.c", false, true),
                    ("foo\\1.c", false, false),
                    ("foo\\blah2\\1.pyc", false, true),
                    ("foo\\2bar\\1.pyc", true, true),
                }),
                ("2 !bar", "bar", new[]
                {
                    ("foo\\2.c", true, false),
                    ("foo\\1.c", false, false),
                    ("foo\\blah2\\1.pyc", true, false),
                    ("foo\\2bar\\1.pyc", false, false),
                    ("foo\\2\\1.pyc", true, false),
                }),
            };
            foreach ((string query, string topDir, var examples) in testcasesWithWrongTopDir)
            {
                Func<string, bool> resultFunc;
                try
                {
                    resultFunc = glob.Parse(query);
                }
                catch (Exception ex)
                {
                    ii++;
                    Npp.AddLine($"While parsing query \"{query}\", got exception\r\n{ex}");
                    failed++;
                    continue;
                }
                Func<string, bool> resultFuncWithCachedTopDir;
                try
                {
                    resultFuncWithCachedTopDir = Glob.CacheGlobFuncResultsForTopDirectory(topDir, glob.globFunctions);
                }
                catch (Exception ex)
                {
                    ii++;
                    Npp.AddLine($"While parsing query \"{query}\" (with cached top directory \"{topDir}\"), got exception\r\n{ex}");
                    failed++;
                    continue;
                }
                foreach ((string filename, bool desiredResult, bool desiredResultWithCaching) in examples)
                {
                    ii += 2;
                    string failMessage = $"While executing query \"{query}\" on filename";
                    failed += TestGlobFuncOnFilename(resultFunc, failMessage, filename, desiredResult);
                    failMessage = $"Running query \"{query}\" (with cached top directory \"{topDir}\") on filename";
                    failed += TestGlobFuncOnFilename(resultFuncWithCachedTopDir, failMessage, filename, desiredResultWithCaching);
                }
            }

            Npp.AddLine($"Ran {ii} tests and failed {failed}");
        }
    }
}