using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;
using Kbg.NppPluginNET.PluginInfrastructure;

namespace JSON_Tools.Tests
{
    public class CsvSnifferTests
    {
        public static bool Test()
        {
            var testcases = new (string text, char delim, EndOfLine eol, char quote, int maxLinesToSniff, int maxCharsToSniff, int minLinesToDecide, int correctOutput)[]{
                ("foo\tbar\tbaz\r\n" +
                "1\t2\t3\r\n" +
                "\"4\t5\r\n\"\t6\t7", '\t', EndOfLine.CRLF, '"',16, 1000, 6, 3),
                ("foo\tbar\tbaz\r\n" +
                "1\t2\t3\r\n" +
                "\"4\t5\r\n\"\t6\t7\r\n", '\t', EndOfLine.CRLF, '"', 16, 1000, 6, 3), // make sure trailing newlines tolerated
                ("foo\tbar\tbaz\r\n" +
                "1\t2\t3\r\n" +
                "\"4\t5\r\n\"\t6\t7", '\t', EndOfLine.CRLF, '"', 16, 30, 6, -1), // stop before consuming last line
                ("foo\tbar\tbaz\tquz\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" + // the sniffer should stop at the end of this line
                "1\t2\t3\t\"1\r\"\"2\"\t3\r\n" + // there are more than 4 columns here, but that shouldn't matter because the sniffer should have stopped
                "1\t2\t3\t\"1\r\"\"2\"\t\t\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\t\t\t\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "\"4\t5\r\n\"\t6\t7\t9", '\t', EndOfLine.CRLF, '"', 16, 10_000, 6, 4),
                ("foo\tbar\tbaz\tquz\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\t3\r\n" + // there are more than 4 columns here, and the sniffer should go far enough to notice it and complain
                "1\t2\t3\t\"1\r\"\"2\"\t\t\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\t\t\t\r\n" +
                "1\t2\t3\t\"1\r\"\"2\"\r\n" +
                "\"4\t5\r\n\"\t6\t7\t9", '\t', EndOfLine.CRLF, '"', 20, 10_000, 6, -1),
                ("baz,quz\n" +
                "3,\"1\"\",2\"\n" +
                "3,\"1\"\",2\"\n" +
                "3,\"1\"\",2\"\n" +
                "3,\"1\"\",2\"\n" +
                "3,\"1\"\",2\"\n" +
                "3,\"1\"\",2\"\n" +
                "3,\"1\"\",2\"\n" +
                "3,\"1\"\",2\"\n" +
                "3,\"1\"\",2\"\n" +
                "3,\"1\"\",2\"\n" + // the sniffer should stop at the end of this line
                "3,\"1\"\",2\",3\n" + // there are more than 2 columns here, but that shouldn't matter because the sniffer should have stopped
                "3,\"1\"\",2\",,\n" +
                "\"4,5\n\",6,7,9", ',', EndOfLine.LF, '"', 11, 10_000, 6, 2),
                ("baz$quz\r" +
                "3$\"1\"\"$2\"\r" +
                "3$\"1\"\"$2\"\r" +
                "3$\"1\"\"$2\"\r" +
                "3$\"1\"\"$2\"\r" +
                "3$\"1\"\"$2\"\r" +
                "3$\"1\"\"$2\"\r" +
                "3$\"1\"\"$2\"\r" + // the sniffer should stop at the end of this line
                "3$\"1\"\"$2\"$3\r" + // there are more than 2 columns here, but that shouldn't matter because the sniffer should have stopped
                "3$\"1\"\"$2\"$$\r" +
                "\"4$5\r\"$6$7$9", '$', EndOfLine.CR, '"', 8, 10_000, 5, 2),
                ("baz$quz\r" +
                "3$\"1\"\"$2\"\r" +
                "3$\"1\"\"$2\"\r" +
                "3$\"1\"\"$2\"\r" +
                "3$\"1\"\"$2\"\r" +
                "3$\"1\"\"$2\"\r" +
                "3$\"1\"\"$2\"$3\r" + // the sniffer will stop here, and complain because there are more than 2 columns
                "3$\"1\"\"$2\"$$\r" +
                "\"4$5\r\"$6$7$9", '$', EndOfLine.CR, '"', 7, 10_000, 5, -1),
                ("baz$quz\r" +
                "3$\"1\"\"$2\"\r" +
                "3$\"1\"\"$2\"\r" +
                "3$\"1\"\"$2\"\r" + // the sniffer will stop here, and complain because it hasn't reached its minLinesToDecide of 5
                "3$\"1\"\"$2\"\r" +
                "3$\"1\"\"$2\"\r" +
                "3$\"1\"\"$2\"\r" +
                "3$\"1\"\"$2\"\r" +
                "3$\"1\"\"$2\"$3\r" +
                "3$\"1\"\"$2\"$$\r" +
                "\"4$5\r\"$6$7$9", '$', EndOfLine.CR, '"', 8, 38, 5, -1),
                ("abc\r\nd,e\r\nf,g\r\nh,i\r\nj,k\r\nl,m\r\nn,o\r\np,q", '\t', EndOfLine.CRLF, '"', 16, 10_000, 5, -1), // 1 column, must quit
                ("abc\r\nd,e\r\nf,g\r\nh,i\r\nj,k\r\nl,m\r\nn,o\r\np,q", ',', EndOfLine.CRLF, '"', 16, 10_000, 5, -1), // first row has 1 column, must quit
                ("a,bc\r\nd,\r\n,g\r\nh,\"\"\r\n\"\",k\r\n,\r\nn,o\r\np,q", ',', EndOfLine.CRLF, '"', 16, 10_000, 5, 2), // rows with empty values
                (",bc\nd,e\nf,g\nh,i\nj,\nl,m\nn,o\np,q", ',', EndOfLine.LF, '"', 16, 10_000, 5, 2),
                ("nums,names,cities,date,zone,subzone,contaminated\r\nnan,Bluds,BUS,,1,a,TRUE\r\nnan,Bluds,BUS,,1,b,FALSE\r\nnan,Bluds,BUS,,1,c,FALSE\r\n0.5,dfsd,FUDG,12/13/2020 0:00,2,c,TRUE\r\n0.5,\"df\"\"sd\",FUDG,12/13/2020 0:00,2,d,FALSE\r\n0.5,dfsd,FUDG,12/13/2020 0:00,2,e,FALSE\r\n0.5,dfsd,\"FU\r\nDG\",12/13/2020 0:00,2,g,FALSE\r\n1.2,qere,GOLAR,,3,f,TRUE\r\n1.2,qere,GOLAR,,3,h,TRUE\r\n3.4,flodt,\"q,tün\",,4,q,FALSE\r\n4.6,Kjond,YUNOB,10/17/2014 0:00,5,w,TRUE\r\n4.6,Kjond,YUNOB,10/17/2014 0:00,5,z,FALSE\r\n7,Unyir,MOKJI,5/11/2017 0:00,6,i,TRUE\r\n",
                    ',', EndOfLine.CRLF, '"', 16, 10_000, 6, 7),
                ("nums,names,cities,date,zone,subzone,contaminated\nnan,Bluds,BUS,,1,a,TRUE\nnan,Bluds,BUS,,1,b,FALSE\nnan,Bluds,BUS,,1,c,FALSE\n0.5,dfsd,FUDG,12/13/2020 0:00,2,c,TRUE\n0.5,\"df\"\"sd\",FUDG,12/13/2020 0:00,2,d,FALSE\n0.5,dfsd,FUDG,12/13/2020 0:00,2,e,FALSE\n0.5,dfsd,\"FU\nDG\",12/13/2020 0:00,2,g,FALSE\n1.2,qere,GOLAR,,3,f,TRUE\n1.2,qere,GOLAR,,3,h,TRUE\n3.4,flodt,\"q,tün\",,4,q,FALSE\n4.6,Kjond,YUNOB,10/17/2014 0:00,5,w,TRUE\n4.6,Kjond,YUNOB,10/17/2014 0:00,5,z,FALSE\n7,Unyir,MOKJI,5/11/2017 0:00,6,i,TRUE\n",
                    ',', EndOfLine.LF, '"', 16, 400, 5, 7),
                ("nums,names,cities,date,zone,subzone,contaminated\nnan,Bluds,BUS,,1,a,TRUE\nnan,Bluds,BUS,,1,b,FALSE\nnan,Bluds,BUS,,1,c,FALSE\n0.5,dfsd,FUDG,12/13/2020 0:00,2,c,TRUE\n0.5,\"df\"\"sd\",FUDG,12/13/2020 0:00,2,d,FALSE\n0.5,dfsd,FUDG,12/13/2020 0:00,2,e,FALSE\n0.5,dfsd,\"FU\nDG\",12/13/2020 0:00,2,g,FALSE\n1.2,qere,GOLAR,,3,f,TRUE\n1.2,qere,GOLAR,,3,h,TRUE\n3.4,flodt,\"q,tün\",,4,q,FALSE\n4.6,Kjond,YUNOB,10/17/2014 0:00,5,w,TRUE\n4.6,Kjond,YUNOB,10/17/2014 0:00,5,z,FALSE\n7,Unyir,MOKJI,5/11/2017 0:00,6,i,TRUE\n",
                    ',', EndOfLine.CRLF, '"', 16, 400, 5, -1),
                ("nums,names,cities,date,zone,subzone,contaminated\rnan,Bluds,BUS,,1,a,TRUE\rnan,Bluds,BUS,,1,b,FALSE\rnan,Bluds,BUS,,1,c,FALSE\r0.5,dfsd,FUDG,12/13/2020 0:00,2,c,TRUE\r0.5,\"df\"\"sd\",FUDG,12/13/2020 0:00,2,d,FALSE\r0.5,dfsd,FUDG,12/13/2020 0:00,2,e,FALSE\r0.5,dfsd,\"FU\rDG\",12/13/2020 0:00,2,g,FALSE\r1.2,qere,GOLAR,,3,f,TRUE\r1.2,qere,GOLAR,,3,h,TRUE\r3.4,flodt,\"q,tün\",,4,q,FALSE\r4.6,Kjond,YUNOB,10/17/2014 0:00,5,w,TRUE\r4.6,Kjond,YUNOB,10/17/2014 0:00,5,z,FALSE\r7,Unyir,MOKJI,5/11/2017 0:00,6,i,TRUE\r",
                    ',', EndOfLine.CR, '"', 16, 400, 7, 7),
                ("nums,names,cities,date,zone,subzone,contaminated\rnan,Bluds,BUS,,1,a,TRUE\rnan,Bluds,BUS,,1,b,FALSE\rnan,Bluds,BUS,,1,c,FALSE\r0.5,dfsd,FUDG,12/13/2020 0:00,2,c,TRUE\r0.5,\"df\"\"sd\",FUDG,12/13/2020 0:00,2,d,FALSE\r0.5,dfsd,FUDG,12/13/2020 0:00,2,e,FALSE\r0.5,dfsd,\"FU\rDG\",12/13/2020 0:00,2,g,FALSE\r1.2,qere,GOLAR,,3,f,TRUE\r1.2,qere,GOLAR,,3,h,TRUE\r3.4,flodt,\"q,tün\",,4,q,FALSE\r4.6,Kjond,YUNOB,10/17/2014 0:00,5,w,TRUE\r4.6,Kjond,YUNOB,10/17/2014 0:00,5,z,FALSE\r7,Unyir,MOKJI,5/11/2017 0:00,6,i,TRUE\r",
                    ',', EndOfLine.CR, '"', 16, 4000, 7, 7),
                ("nums,names,cities,date,zone,subzone,contaminated\rnan,Bluds,BUS,,1,a,TRUE\rnan,Bluds,BUS,,1,b,FALSE\rnan,Bluds,BUS,,1,c,FALSE\r0.5,dfsd,FUDG,12/13/2020 0:00,2,c,TRUE\r0.5,\"df\"\"sd\",FUDG,12/13/2020 0:00,2,d,FALSE\r0.5,dfsd,FUDG,12/13/2020 0:00,2,e,FALSE\r0.5,dfsd,\"FU\rDG\",12/13/2020 0:00,2,g,FALSE\r1.2,qere,GOLAR,,3,f,TRUE\r1.2,qere,GOLAR,,3,h,TRUE\r3.4,flodt,\"q,tün\",,4,q,FALSE\r4.6,Kjond,YUNOB,10/17/2014 0:00,5,w,TRUE\r4.6,Kjond,YUNOB,10/17/2014 0:00,5,z,FALSE\r7,Unyir,MOKJI,5/11/2017 0:00,6,i,TRUE",
                    ',', EndOfLine.CR, '"', 16, 4000, 7, 7), // same as previous, but w/o trailing EOL
                ("nums,names,cities,date,zone,subzone,contaminated\rnan,Bluds,BUS,,1,a,TRUE\rnan,Bluds,BUS,,1,b,FALSE\rnan,Bluds,BUS,,1,c,FALSE\r0.5,dfsd,FUDG,12/13/2020 0:00,2,c,TRUE\r0.5,\"df\"\"sd\",FUDG,12/13/2020 0:00,2,d,FALSE\r0.5,dfsd,FUDG,12/13/2020 0:00,2,e,FALSE\r0.5,dfsd,\"FU\rDG\",12/13/2020 0:00,2,g,FALSE\r1.2,qere,GOLAR,,3,f,TRUE\r1.2,qere,GOLAR,,3,h,TRUE\r3.4,flodt,\"q,tün\",,4,q,FALSE\r4.6,Kjond,YUNOB,10/17/2014 0:00,5,w,TRUE\r4.6,Kjond,YUNOB,10/17/2014 0:00,5,z,FALSE\r7,Unyir,MOKJI,5/11/2017 0:00,6,i,TRUE\r",
                    ',', EndOfLine.CR, '"', 16, 400, 15, -1), // not enough lines to meet minLinesToDecide
                ("nums,names,cities,date,zone,subzone,contaminated\rnan,Bluds,BUS,,1,a,TRUE\rnan,Bluds,BUS,,1,b,FALSE\rnan,Bluds,BUS,,1,c,FALSE\r0.5,dfsd,FUDG,12/13/2020 0:00,2,c,TRUE\r0.5,\"df\"\"sd\",FUDG,12/13/2020 0:00,2,d,FALSE\r0.5,dfsd,FUDG,12/13/2020 0:00,2,e,FALSE\r0.5,dfsd,\"FU\rDG\",12/13/2020 0:00,2,g,FALSE\r1.2,qere,GOLAR,,3,f,TRUE\r1.2,qere,GOLAR,,3,h,TRUE\r3.4,flodt,\"q,tün\",,4,q,FALSE\r4.6,Kjond,YUNOB,10/17/2014 0:00,5,w,TRUE\r4.6,Kjond,YUNOB,10/17/2014 0:00,5,z,FALSE\r7,Unyir,MOKJI,5/11/2017 0:00,6,i,TRUE\r",
                    ',', EndOfLine.CRLF, '"', 16, 400, 7, -1),
                ("a,b,c,d,e,f,g,h,i", ',', EndOfLine.CRLF, '"', 16, 10_000, 5, -1), // one line, so can't decide anything
                ("a\tb\tc\td\te\tf\r\n", ',', EndOfLine.CRLF, '"', 16, 10_000, 5, -1), // one line, so can't decide anything
            };
            int testsFailed = 0;
            int ii = 0;

            foreach ((string text, char delim, EndOfLine eol, char quote, int maxLinesToSniff, int maxCharsToSniff, int minLinesToDecide, int correctOutput) in testcases)
            {
                ii++;
                int output;
                string baseMsg = $"Expected CsvSniffer.Sniff({JNode.StrToString(text, true)}, {eol}, {quote}, '{ArgFunction.CsvCleanChar(delim)}', {maxLinesToSniff}, {maxCharsToSniff}, {minLinesToDecide}) to return {correctOutput}, but got";
                try
                {
                    output = CsvSniffer.Sniff(text, eol, delim, quote, maxLinesToSniff, maxCharsToSniff, minLinesToDecide);
                }
                catch (Exception ex)
                {
                    Npp.AddLine($"{baseMsg} exception:\r\n{RemesParser.PrettifyException(ex)}");
                    testsFailed++;
                    continue;
                }
                if (output != correctOutput)
                {
                    testsFailed++;
                    Npp.AddLine($"{baseMsg} {output}");
                }
            }
            Npp.AddLine($"Failed {testsFailed} tests.");
            Npp.AddLine($"Passed {ii - testsFailed} tests.");
            return testsFailed > 0;
        }
    }
}
