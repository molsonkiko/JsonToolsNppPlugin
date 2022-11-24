'''Use pyautogui to run some simple UI tests on JsonTools.
'''
import os
import time
import unittest
import subprocess
import sys
# third party
import pyautogui as pyag
import pyperclip

APPDATA_PATH = os.getenv('appdata')
JSONTOOLS_SETTINGS_PATH = os.path.join(APPDATA_PATH, 'notepad++', 'plugins', 'config', 'JsonTools.ini')

def npp_path(nppversion: str = '', x64 = True, is_latest_version = False):
    base_path = 'C:\\Program Files\\' if x64 else 'C:\\Program Files (x86)\\'
    if is_latest_version:
        return base_path + 'Notepad++\\notepad++.exe'
    return base_path + f'Notepad++ {nppversion}\\notepad++.exe'

def open_npp(nppversion: str = '', x64 = True, is_latest_version = True, force_close = False):
    '''Run the desired version of Notepad++, then wait for it to start.
    If Notepad++ is already open:
        if force_close, close the current instance and reopen it.
        otherwise, maximize and focus the current instance.
    '''
    path = npp_path(nppversion, x64, is_latest_version)
    if force_close:
        empty_file_and_close()
    try:
        focus_npp()
    except:
        pass
    os.startfile(path)
    wnd = None
    while not wnd:
        time.sleep(0.3)
        wnd = get_npp_window()

def get_npp_window():
    wnds = pyag.getAllWindows()
    npp_windows = [wnd for wnd in wnds 
        if wnd.title.endswith(' - Notepad++')
    ]
    if not npp_windows:
        return
    return npp_windows[0]

def focus_npp():
    wnd = get_npp_window()
    wnd.maximize()
    wnd.activate()

def execute_keystrokes(*keys: str, 
        one_at_a_time = True, 
        focus_editor = True
    ):
    if focus_editor:
        focus_npp()
    if one_at_a_time:
        pyag.press(keys)
    else:
        for key in keys:
            pyag.keyDown(key)
        for key in keys:
            pyag.keyUp(key)
    time.sleep(0.15)

def close_npp():
    execute_keystrokes('alt', 'f4', one_at_a_time=False)

def open_new_file():
    execute_keystrokes('ctrl', 'n', one_at_a_time=False)

def empty_file_and_close():
    execute_keystrokes('ctrl', 'a', one_at_a_time=False)
    execute_keystrokes('backspace')
    execute_keystrokes('ctrl', 'w', one_at_a_time=False)

def get_current_file_contents():
    execute_keystrokes('ctrl', 'a', one_at_a_time=False)
    execute_keystrokes('ctrl', 'c', one_at_a_time=False)
    return pyperclip.paste()

def open_tree():
    execute_keystrokes('ctrl', 'alt', 'shift', 'j', one_at_a_time=False)
    time.sleep(0.5)

def get_query_result(query):
    open_tree()
    pyag.write(query)
    # submit query
    pyag.press(['tab', 'enter'])
    time.sleep(0.25)
    # open query result in new tab
    pyag.press(['tab', 'tab', 'enter'])
    time.sleep(0.25)
    # focus editor and copy text
    pyag.press('escape')
    text = get_current_file_contents()
    empty_file_and_close()
    return text

def compress_json():
    execute_keystrokes('ctrl', 'alt', 'shift', 'c', one_at_a_time=False)
    time.sleep(0.2)

def pretty_print():
    execute_keystrokes('ctrl', 'alt', 'shift', 'p', one_at_a_time=False)
    time.sleep(0.2)

def get_settings():
    with open(JSONTOOLS_SETTINGS_PATH, encoding='utf-8') as f:
        lines = [x.strip() for x in f.readlines()]
    attr_lines = [line for line in lines if line and line[0] != '[' and line[0] != ';']
    print(attr_lines)
    return dict([line.split('=') for line in attr_lines])

def change_settings(setting, value):
    '''Return True if the settings were changed, False, if the setting was not in the file.
    Raise a FileNotFoundError if %AppData%/Roaming/Notepad++/plugins/config/JsonTools.ini does not exist.'''
    with open(JSONTOOLS_SETTINGS_PATH) as f:
        settings_lines = [x.strip() for x in f.readlines()]
    the_setting_line = list(filter(lambda x: x[1].startswith(setting), enumerate(settings_lines)))
    if not the_setting_line:
        return False
    the_setting_ii = the_setting_line[0][0]
    settings_lines[the_setting_ii] = f'{setting}={value}'
    with open(JSONTOOLS_SETTINGS_PATH, 'w', encoding='utf-8') as f:
        f.write('\n'.join(settings_lines))
    return True

def powershell(script):
    '''Run a script with Powershell
    '''
    subprocess.run('powershell ' + script)

def set_culture(culture: str):
    '''Change the culture, which controls things like
    whether '.' or ',' is used to delineate the fractional
    part of floating point numbers.
    '''
    powershell('Set-Culture ' + culture)

def set_culture_german():
    '''in germany they use , instead of . to delineate 
    the fractional part of floating point numbers.
    '''
    set_culture('de-DE')

def set_culture_usa():
    set_culture('en-US')

def open_file_with_name(name):
    '''open a file that is already among the open tabs
    in Notepad++.
    NOTE: Requires the NavigateTo plugin.
    https://github.com/young-developer/nppNavigateTo/tree/master
    '''
    execute_keystrokes('ctrl', ',', one_at_a_time=False)
    time.sleep(0.25)
    pyag.write(name)
    time.sleep(0.15)
    pyag.press('enter')
    time.sleep(0.25)

def open_find_replace_form():
    open_tree()
    time.sleep(0.25)
    with pyag.hold('shift'):
        pyag.press('tab')
    pyag.press('enter')
    

class UITester(unittest.TestCase):
    nppversion: str
    x64: bool
    is_latest_version: bool
    noinput: bool
    
    def setUp(self):
        open_npp(self.nppversion, self.x64, self.is_latest_version)

    def tearDown(self):
        empty_file_and_close()
        close_npp()
        if not self.noinput:
            user_happy = input('Do you want to continue the tests (Y/N)?').lower() == 'y'
            if not user_happy:
                sys.exit(1)

    def test_compress_works(self):
        open_new_file()
        execute_keystrokes(*'[1, 2, "a", {"b": 3.5}]')
        compress_json()
        text = get_current_file_contents()
        self.assertEqual(text, '[1,2,"a",{"b":3.5}]')

    def test_german_culture_doesnt_make_floats_bad(self):
        close_npp()
        set_culture_german()
        open_npp()
        open_new_file()
        execute_keystrokes(*'1.5')
        compress_json()
        self.assertEqual(get_current_file_contents(), '1.5')
        set_culture_usa()

    def test_pretty_print_works(self):
        open_new_file()
        execute_keystrokes(*'[1, 2, "a", {"b": 3.5}]')
        pretty_print()
        text = get_current_file_contents()
        correct_text = '''[
    1,
    2,
    "a",
    {
        "b": 3.5
    }
]'''.replace('\n', '\r\n')
        self.assertEqual(text, correct_text)

    def test_tree_works(self):
        open_new_file()
        execute_keystrokes(*'[1, 2, "a", {"b": 3.5}]')
        result = get_query_result('s_mul(z, int(@[3].b * 3))')
        self.assertEqual(result, '"zzzzzzzzzz"')

    def test_big_ints_parsed_as_floats(self):
        open_new_file()
        execute_keystrokes(*('1'*40))
        compress_json()
        self.assertEqual(get_current_file_contents(), '1.11111111111111E+39')
    
    def test_linter(self):
        og_linting = get_settings()['linting']
        time.sleep(0.5)
        change_settings('linting', 'True')
        time.sleep(0.5)
        open_new_file()
        execute_keystrokes(*"{'a': [1 2")
        time.sleep(0.2)
        compress_json()
        time.sleep(0.5)
        if 'Error while trying to parse JSON' in pyag.getAllTitles():
            print('Linting must be turned on before starting these tests')
            print('Exiting the tests to prevent weird stuff from happening')
            sys.exit(1)
        pyag.press('enter')
        open_file_with_name('new 2')
        lint_info = get_current_file_contents()
        for error in [
            'Strings must be quoted with " rather than \'',
            'No comma between array members',
            'Unterminated array',
            'Unterminated object'
        ]:
            self.assertIn(error, lint_info)
        empty_file_and_close()
        if og_linting != 'True':
            change_settings('linting', 'False')

    def test_json_to_csv(self):
        open_new_file()
        pyag.write('[{"a": 1, "b": "y"}, {"a": 3, "b": "z"}]')
        open_tree()
        time.sleep(0.5)
        # open json->csv form
        pyag.press(['tab', 'tab', 'enter'])
        time.sleep(1)
        # make csv
        pyag.press(['tab', 'tab', 'tab', 'tab', 'enter'])
        time.sleep(1)
        # the csv form will now open again, so close it
        pyag.press('escape')
        time.sleep(1)
        # close tree before getting file contents
        open_tree()
        time.sleep(0.25)
        text = get_current_file_contents()
        time.sleep(0.5)
        empty_file_and_close()
        self.assertEqual(
            text,
            'a,b\n1,y\n3,z\n'
        )

    


if __name__ == '__main__':
    # raise NotImplementedError("These tests are too risky and should probably never be used")
    import sys
    UITester.nppversion = 'latest' if len(sys.argv) == 1 else sys.argv.pop(1)
    if UITester.nppversion == 'help':
        print('Usage: python -m ui_tests <optional notepad++ version> <optional x86> <optional noinput>')
        print('If x86 supplied, use 32-bit notepad++')
        print('If noinput supplied, do not prompt the user for input')
        print('If notepad++ version not supplied or notepad++ version == latest, use latest version of Notepad++')
        sys.exit(0)
    if UITester.nppversion == 'latest':
        UITester.is_latest_version = True
    else:
        UITester.is_latest_version = False
    UITester.x64 = True if len(sys.argv) == 1 else sys.argv.pop(1) != 'x86'
    UITester.noinput = False
    if UITester.x64 == 'noinput':
        UITester.noinput = True
    elif len(sys.argv) > 1:
        UITester.noinput = sys.argv.pop(1) == 'noinput'
    user_understands = True
    if not UITester.noinput:
        user_understands = input('''Before running these tests, you MUST verify the following:
1. Compressing JSON is bound to Ctrl+Alt+Shift+C.
2. Pretty-printing JSON is bound to Ctrl+Alt+Shift+P.
3. Opening the JSON tree is bound to Ctrl+Alt+Shift+J.
4. Copying the selected text is bound to Ctrl+C.
5. Selecting the entire document is bound to Ctrl+A.
6. Closing the currently open file is bound to Ctrl+W.
7. Closing Notepad++ is bound to Alt+F4.
8. Opening a new file is bound to Ctrl+N.
9. You have the NavigateTo plugin installed (https://github.com/young-developer/nppNavigateTo/tree/master)
11. No unsaved files ('e.g., "new 1", "new 2") are currently open.

If ANY of these things is not true, these tests will fail horribly and do lots of bad things,
because they use GUI automation (https://pyautogui.readthedocs.io/en/latest/keyboard.html).
DO NOT TRY TO INTERACT WITH THE KEYBOARD OR MOUSE DURING THE TESTS!

If you are unhappy with what the tests are doing, MOVE YOUR MOUSE TO THE UPPER-RIGHT CORNER OF THE SCREEN!
This will make the tests stop immediately.

After running these tests, your computer's culture settings will be en-US. This means that 
the default decimal separator will be '.' instead of ','.
If you don't like that, you can reset it by running the command 
"Set-Culture <whatever your culture is>" in Powershell.

If you understand all of this and still want to run the tests, type 'I understand' and press enter.
''')
    if user_understands:
        unittest.main()