'''
script for generating toy recursive JSON to be validated with recursive_foo_schema.json.
Example usage:
cmd> python make_recursive_foo.py 3 -p
{
    "bar": 0,
    "foo": {
        "bar": 1,
        "foo": {
            "bar": 2,
            "foo": {
                "bar": 3,
                "foo": null
            }
        }
    }
}
cmd> python make_recursive_foo.py 7
{"bar": 0, "foo": {"bar": 1, "foo": {"bar": 2, "foo": {"bar": 3, "foo": {"bar": 4, "foo": {"bar": 5, "foo": {"bar": 6, "foo": {"bar": 7, "foo": null}}}}}}}}
'''
import json

def make_recursive_foo(max_depth):
    depth = 0
    recursive_foo = {}
    recursion = recursive_foo
    while depth < max_depth:
        recursion['bar'] = depth
        recursion['foo'] = {}
        depth += 1
        recursion =  recursion['foo']
    recursion['bar'] = depth
    recursion['foo'] = None
    return recursive_foo
    
if __name__ == '__main__':
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument('max_depth', type=int,
        help='the depth of recursion in the output. For example, with max_depth=3, we get {"bar": 0, "foo": {"bar": 1, "foo": {"bar": 2, "foo": {"bar": 3, "foo": null}}}}')
    parser.add_argument('-p', '--pretty', action='store_true', help='pretty-print output')
    args = parser.parse_args()
    foo = make_recursive_foo(args.max_depth)
    if args.pretty:
        print(json.dumps(foo, indent=4))
    else:
        print(json.dumps(foo))