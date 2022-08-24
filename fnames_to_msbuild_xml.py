import os
import re

COMPILE_REGEX = re.compile('<Compile Include="(.+)"\s*/?>')


def get_cs_files(dirname):
    '''recursively search for C# files in directory
    '''
    return set(os.path.relpath(os.path.join(dirname, root, fname), 
                              dirname)
            for root, subdirs, files in os.walk(dirname)
            for fname in files
            if fname.endswith('.cs'))


def compiled_files_in_csproj(fname):
    with open(fname) as f:
        cs = f.read()

    return set(COMPILE_REGEX.findall(cs))


def get_xml_for_new_resources(dirname, csproj, exts = 'png|ico|bmp'):
    extlist = exts.split('|')
    print('=============\nicons and such\n=============')
    extant = set(os.path.relpath(os.path.join(dirname, root, f), 
                              dirname)
            for root, subdirs, files in os.walk(dirname)
            for f in files
            if re.search(f".(?:{exts})$", f, re.I))
    with open(csproj) as f:
        cs = f.read()
    incsproj = set(re.findall('<(?:None|Content) Include="(.+)"\s*/?>', cs))
    new_files = extant - incsproj
    for fname in sorted(new_files):
        print(f'<None Include="{fname}" />')


def get_xml_for_new_files(dirname, csproj_fname):
    already_compiled = set()# compiled_files_in_csproj(csproj_fname)
    # print(already_compiled)
    all_files = get_cs_files(dirname)
    new_files = sorted(all_files - already_compiled)
    forms = [f for f in new_files if 'Forms\\' in f]
    designers = [f for f in new_files if f.lower().endswith('.designer.cs')]
    nonforms = [f for f in new_files if 'Forms\\' not in f]
    print('=============\nNON-FORMS\n=============')
    for f in nonforms:
        print(f'<Compile Include="{f}" />')
    print('=============\nFORMS\n=============')
    for f in forms:
        print(f'''<Compile Include="{f}">
  <SubType>Form</SubType>
</Compile>''')
    print('=============\nDESIGNERS\n=============')
    for f in designers:
        corresp_form = re.sub('.designer', '', f, re.I)
        print(f'''<Compile Include="{f}">
  <DependentUpon>{corresp_form}</DependentUpon>
</Compile>''')
    print('=============\nRESX files\n=============')
    for f in forms:
        resx = re.sub('cs$', 'resx', f)
        print(f'''<EmbeddedResource Include="{resx}">
  <DependentUpon>{f}</DependentUpon>
  <SubType>Designer</SubType>
</EmbeddedResource>''')

    get_xml_for_new_resources(dirname, csproj_fname)


if __name__ == '__main__':
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument('dirname', 
        help='the parent directory of all the .cs files in the project.')
    parser.add_argument('csproj_fname', help='the absolute path of the csproj file')
    args = parser.parse_args()

    get_xml_for_new_files(args.dirname, args.csproj_fname)