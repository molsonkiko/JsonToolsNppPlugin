# requires the notepad++ project is cloned in a folder next to this plugin pack 

import re
from FileGenerator import Regenerate

class Line: 
    def __init__(self, content, comment):
         self.content = content
         self.comment = comment
         self.parsedContent = None

    def isEmptyLine(self):
        return self.parsedContent == None and self.comment == ""

    def isComment(self):
        return self.comment != ""

    def isDefinition(self):
        return self.parsedContent != None

def splitAndStripLine(line):
    if line[-1:] == '\n': line = line[:-1]
    pos = line.find("//")
    if pos == -1:
        return Line(line.strip(), "")
    else:
        return Line((line[:pos]).strip(), (line[pos:]).strip())

def parseLine(line):
    parsed = splitAndStripLine(line)
    match = re.match("^#define\\s+(?P<name>\\w+)\\s+\\((?P<value>[^,\")]+)\\)$", parsed.content)
    if match == None:
        match = re.match("^#define\\s+(?P<name>\\w+)\\s+(?P<value>[^,\"]+)$", parsed.content)
    parsed.parsedContent = match
    return parsed

def parseFile(name):
    file = open(name)
    allLines = []
    for line in file.readlines():
        allLines.append(parseLine(line))
    return allLines


def getComments(j, lines):
    out = []
    anyComment = False
    while j < len(lines) and lines[j].isComment():
        if len(out) == 0: 
            out.append("        /// <summary>")
        out.append("        /%s" %(lines[j].comment))
        j = j + 1
    if len(out) > 0:
        out.append("        /// </summary>")
    return out


def printFile(name, addComments):
    out = []
    lines = parseFile(name)

    for i in range(0, len(lines)):
        # blank lines
        if not lines[i].isDefinition():
            if i+1 >= len(lines) or lines[i+1].isDefinition():
                out.append("")
        # definitions
        elif lines[i].isDefinition():
            if addComments == True:
                out.extend(getComments(i+1, lines))
            value = lines[i].parsedContent.group("value")
            value = value.replace("WM_USER","Constants.WM_USER")
            value = value.replace("NPPMSG","Constants.NPPMSG")
            name = lines[i].parsedContent.group("name")
            out.append ("        %s = %s," %(name, value))
    return out

if __name__ == "__main__":
    preffile = printFile("../../../notepad-plus-plus/PowerEditor/src/WinControls/Preference/preference_rc.h", False)
    Regenerate("../../Visual Studio Project Template C#/PluginInfrastructure/Preference_h.cs", "/* ", preffile)

    resourcefile = printFile("../../../notepad-plus-plus/PowerEditor/src/resource.h", False)
    Regenerate("../../Visual Studio Project Template C#/PluginInfrastructure/Resource_h.cs", "/* ", resourcefile)

    msgsfile = printFile("../../../notepad-plus-plus/PowerEditor/src/MISC/PluginsManager/Notepad_plus_msgs.h", True)
    Regenerate("../../Visual Studio Project Template C#/PluginInfrastructure/Msgs_h.cs", "/* ", msgsfile)
