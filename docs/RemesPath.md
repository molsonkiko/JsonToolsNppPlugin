RemesPath documentation
========================

RemesPath is a JSON query language inspired by [JMESpath](https://jmespath.org/) with such useful features as
* indexing in objects with both dot syntax and square bracket syntax
* [boolean indexing](#boolean-indexing)
* [vectorized arithmetic](#vectorized-operations)
* many built-in [functions](#functions), both [vectorized](#vectorized-functions) and not
* [regular expression](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference) functions
* [recursive search](#recursive-search) for keys
* SQL-like group_by capabilities (function in [non-vectorized functions](#non-vectorized-functions))
* reshaping and summarization of JSON using [projections](#projections)
* [editing of JSON](#editing-with-assignment-expressions)

## Indexing and selecting keys ##

1. `@` selects the entirety of an object or array.
2. [Python-style slices](https://stackoverflow.com/questions/509211/understanding-slicing) and indices can be used to select parts of an array. For example:
    * `@[1]` selects the second element
    * `@[-4]` selects the fourth-to-last element
    * `@[:3]` selects the first, second and third elements
    * `@[5::2]` selects every other element of the array starting with the fifth.
3. You can select multiple slices and indices in the same square brackets!
    * `@[1, 5:8, -1]` selects the second, sixth, seventh, eight, and last elements.
4. Dot syntax and square brackets are both valid ways to select keys of an object.
    * `@.bar` and `@[bar]` both select the value corresponding to a single key `bar`.
    * You can select multiple keys from a single object by enclosing all of them in square brackets. You *cannot* follow a dot with square brackets.
        * So `@[foo, bar, baz]` gets the values associated with keys `foo`, `bar`, and `baz`.
    * Backticks (\`) can be used to enquote strings. Thus ``@.`bar` ``and ``@[`bar`]`` are equivalent to `@.bar` and `@[bar]`.
    * The literal backtick character \` can be rendered by an escaped backtick \\\` inside a backtick-enclosed string.
    * Any string that does not *begin* with an underscore or an ASCII letter and *contain* only underscores, ASCII letters, and digits *must* be enclosed in backticks.
        * So `@.a12`, `@._`, `@.a_1`, but ``@[`1_a`]``.
5. Each time you select an index or key, the next index selected from the corresponding value(s).
    1. Consider the array `[[1, 2], [3, 4], [5, 6]]`.
        * `@[:2][1]` selects the second element of each of the first and second elements of that array. So `@[:2][1]` will return `[2, 4]`.
    2. Consider the array of objects `[{"a": 1, "b": ["_"]}, {"a": 2, "b": ["?"]}]`.
        * `@[:].b[0]` will return the first value of the array child of key `b` in each object, so `["_", "?"]`.
        * `@[0][b, a]` will return keys `{"a": 1, "b": ["_"]}`. 
            * *Note that the order of keys in the index is not preserved because objects are inherently unordered.*
6. If every indexer in a chain of indexers returns only one index/key, the query will not return an array or object containing the result; it will only return the result.
    1. Consider again the array `[[1, 2], [3, 4], [5, 6]]`.
        * Query `@[0][1]` returns `2`.
    2. Consider again the array of objects `[{"a": 1, "b": ["_"]}, {"a": 2, "b": ["?"]}]`.
        * Query `@[1].b` returns `["?"]`.
        * Query `@[0].b[0]` returns `"_"`.
7. An out-of-bounds index on an array will return an empty array; indexing an object with a key it does not have returns an empty object.
    1. Consider the array `[1, 2, 3]`
        * Queries `@[4]` and `@[-8]` will both return `[]`
        * NOTE: prior to [v5.5.0](/CHANGELOG.md#550---2023-08-13), `@[-n]` on an array with fewer than `n` elements would cause an error to be thrown instead.
    2. Consider the object `{"a": 1, "b": 2}`
        * Queries `@.z` and `@[x, j]` will both return `{}`

### Negated indexing and *excluding* keys (v5.7+ only) ###

Suppose you want to match every key in an object *except `c` and `d`*, or every element in an array *except the 3rd*. RemesPath has always offered ways to do this (often roundabout), but beginning in [v5.7](/CHANGELOG.md#570---2023-09-08), this is much easier with `!` (exclamation point) before any of the key-selecting or index-selecting indexers described above:

* To select every key *except `c` and `d`*, use the query `@![c, d]`
    * The query `@![c, d]` on JSON `{"a": 1, "b": 2, "c": 3, "d": 4}` returns `{"a": 1, "b": 2}`
* To select every key that *does not match the regex `[a-c]`*, use the query ``@!.g`[a-c]` ``
    * The query ``@!.g`[a-c]` `` on JSON `{"a": 1, "b": 2, "c": 3, "d": 4}` returns `{"d": 4}`
* To select every value of an array *except the 3rd and the last three*, use the query `@![2, -3:]`
    * The query `@![2, -3:]` on JSON `[1, 2, 3, 4, 5, 6, 7, 8]` returns `[1, 2, 4, 5]`

__Negated indexing does not work with recursive key selection.__ For example `@!..a` will raise an error.

## Vectorized operations ##

1. Many operations are *vectorized* in RemesPath. That is, they are applied to every element in an iterable.
    1. Consider the array `[1, 2, 3]`.
        * `2 * @` returns `[2, 4, 6]`.
        * `str(@)` returns `["1", "2", "3"]` because `str` is a vectorized function for converting things to their string representations.
        * @ + @ / 2 returns `[1.5, 3.0, 4.5]`.
        * `@ > @[1]` returns `[false, false, true]`.
    2. Consider the object `{"a": 1, "b": 2, "c": 3}`.
        * `@ ** @` returns `{"a": 1.0, "b": 4.0, "c": 27.0}`
        * `@ & 1` returns `{"a": 1, "b": 0, "c": 1}`.
        * `@ > @.a` returns `{"a": false, "b": true, "c": true}`.

## Binary operators, unary operators and arithmetic ##

All binary operators in RemesPath are vectorized over iterables.

The binary operators in RemesPath are as follows:

Symbol | Operator                                      | Precedence | Return type
-------|-----------------------------------------------|------------|------------
 `&`   |  bitwise/logical `AND`                        |  0         | int/bool
 `\|`   |  bitwise/logical `OR`                        |  0         | int/bool
 `^`   |  bitwise/logical `XOR`                        |  0         | int/bool
 `=~`  |  string matches regex                         |  1         | bool
 `==`, `!=`, `<`, `>`, `<=`, `>=` | the usual comparison operators | 1 | bool
 `+`   | Addition of numbers, concatenation of strings   |  2         | int/float/string
 `-`   | subtraction                                   |  2         | int/float
 `//`  | floor division                                |  3         | int
 `%`   | modulo                                        |  3         | int/float
 `*`   | multiplication                                |  3         | int/float/*string*
 `/`   | division                                      |  3         | float
 `**`  | exponentiation                                |  5         | float

All binary operators are [left-associative](https://en.wikipedia.org/wiki/Operator_associativity) (evaluated left-to-right when precedence is tied), except exponentiation (`**`), which is right-associative.

In general, binary operators *should* raise an exception when two objects of unequal type are compared. The only exception is that numbers (including booleans) may be freely compared to other numbers, and ints and floats can freely interoperate.

Starting in [v5.4.0](/CHANGELOG.md#540---2023-07-04), all arithmetic operations can accept a boolean as one or both of the arguments. For example, prior to 5.4.0, `true * 3 - (false / 2.5)` was a type error, but since then it is valid. 

Beginning in [v5.1.0](/CHANGELOG.md#510---2023-06-02), the `*` operator in supports multiplication of strings by integers (but not integers by strings). For example, `["a", "b", "c"] * [1,2,3]` will return `["a", "bb", "ccc"]`. Starting in [5.4.0](/CHANGELOG.md), multiplication of a string by a boolean or a negative integer is valid.

If you find that a binary operator can operate on a number and a non-number without raising an exception, *this is a bug in my implementation.*

### Unary operators ###

As in normal math, the unary minus operator (e.g., `-5`) has lower precedence than exponentiation and higher precedence than everything else.

Starting in [5.4.0](/CHANGELOG.md#540---2023-07-04), the unary `+` operator has the same precedence as the unary minus operator. Unary `+` is a no-op on floats and ints, but it converts `true` and `false` to `1` and `0` respectively.

The `not` operator introduced in [5.4.0](/CHANGELOG.md#540---2023-07-04) (which replaced [the older function of the same name](#vectorized-functions)) is very similar to the Python operator of the same name, in that `not x` returns `False` if x is ["truthy" (see below)](#truthiness), and `True` if x is "falsy".

### Truthiness ###

Similar to in JavaScript and Python, RemesPath has the concept of "truthiness" (and its opposite, "falsiness"), where in some cases a non-boolean is treated as a boolean.

The rules are as follows:
* `true` is "truthy", `false` is "falsy".
* `0` and `0.0` are "falsy", and any nonzero numbers are "truthy".
* `""` (the empty string) is "falsy", and any non-empty strings are "truthy".
* `[]` and `{}` (empty arrays and objects) are "falsy", and non-empty arrays and objects are "truthy"
* `null` is "falsy".
* Anything that is not covered by the above cases is "falsy". In practice this should never happen.

### Regular expressions ###

A regular expression can be created in a RemesPath expression by prefixing a \`\` string with the character "g". So ``g`\\s+` ``is the regular expression "\\s+", i.e., at least one whitespace character.

JsonTools uses [.NET regular expressions](https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference) instead of the Boost library used by the Notepad++ find/replace form.

There are numerous differences between JsonTools regular expressions and Notepad++ find/replace form regexes, but the main differences are as follows:
- Prior to [v7.0](/CHANGELOG.md#700---2024-02-09), `^` and `$` would only match at the beginning and end of the string, respectively (except as noted below for `s_sub` and `s_fa`)
    - Note that `\A` can still be used to match the start of the string, and `\z` can be used to match the end of the string.
- Even after v7.0, `^` and `$` only treat `\n` as as the end of the line. That means that `\r` is not matched at all, and `\r\n` is matched, but regexes must use `\r?$` instead of `$` to handle `\r\n` correctly.
- Matching is case-sensitive by default, whereas Notepad++ is case-insensitive by default. The `(?i)` flag can be added at the beginning of any regex to make it case-insensitive.

### Json literals ###

A JSON literal can be created inside a RemesPath expression by prefixing a \`\` string with the character "j". So ``j`[1, 2, 3]` ``creates the JSON array `[1, 2, 3]`.

### Regular expression indexing in objects ###

You can select all the keys of an object that match a regular expression by using a regular expression with the dot or square bracket syntax.

__Examples:__
* Consider the object `{"foo": 1, "bar": 2, "baz": 3}`.
* ``@.g`^b` ``returns `{"bar": 2, "baz": 3}`.
* ``@[g`r$`, foo]`` returns `{"bar": 2, "foo": 1}`.

## Boolean indexing ##

You can select all elements in an iterable that satisfy a condition by applying a boolean index.

A boolean index can be one of the following:
* A single boolean. If it's `false`, an empty array is returned (*prior to [v5.5.0](/CHANGELOG.md#550---2023-08-13), it's always an array, even for one-element boolean indices on objects*). If it's true, the whole iterable is returned.
    * Consider the array `[1, 2, 3]`
    * e.g., `@[in(2, @)]` will return `[1, 2, 3]` because `in(2, @)` is `true`.
    * `@[in(4, @)]` will return `[]` because `in(4, @)` is `false`.
    * Starting in [5.5.0](/CHANGELOG.md#550---2023-08-13), boolean indices with a single boolean can be applied to non-iterables (e.g. `@[@ > 2]` returns `[]` for `1` and `3` for `3`)
        - With the input `[1, 2, 3]`, `@[:][@ < 3]` returns `[1, 2]` in v5.5.0+, but prior to that it would just raise an error.
    * Starting in [5.5.0](/CHANGELOG.md#550---2023-08-13), a one-boolean boolean index on an object returns the original object, allowing the user to query the result of the boolean index.
        - With the input `[{"a": 1, "b": "a"}, {"a": 2, "b": "b"}]`, `@[@.a < 2].b` returns `["a"]` in v5.5.0+, but prior to that is would just raise an error.
* *If the iterable is an __array__, an array of booleans of the same length as the iterable*. An array with all the values for which the boolean index was `true` will be returned.
    * Consider the array `[1, 2, 3]`
    * `@[@ > @[0]]` will return `[2, 3]`.
    * `@[@ ** 2 < 1]` will return `[]`.
    * `@[@[:2] > 0]` will throw a VectorizedArithmeticException, because the boolean index has length 2 and the array has length 3. 
* *If the iterable is an __object__, an object of booleans with exactly the same keys as the iterable.* An object will be returned with all the pairs k: v for which the boolean index's value corresponding to k was `true`.
    * Consider the object `{"a": 1, "b": 2, "c": 3}`
    * `@[@ > @.a]` returns `{"b": 2, "c": 3}`.
    * `@[@[a,b] > 1]` will throw a VectorizedArithmeticException, because the boolean index is `{"a": false, "b": true}`, which does not have exactly the same keys as the object.

## Grouping parentheses ##

Grouping parentheses work exactly the way you expect them to with arithmetic expressions.
* `2 ** 3 / (4 - 5)` evaluates to `8/-1` and thus returns `-8.0`.

Grouping parentheses can also be used to make the query parser treat a single expression as atomic.
* Consider the object `[{"a": [1, 2, 3]}]`.
* The query `@[:].a[@ > @[0]]` returns `[[2, 3]]`. In pseudo-code, this would be:
```
make an array arr
for each object obj in this
    make a subarray subarr
    for each element in obj[a]
       if element > obj[a][0], add element to subarr
    add subarr to arr
return arr
```
* However, we *can't* select the first element of each array by just making the query `@[:].a[@ > @[0]][0]`. This will throw an error.
* That's because the query has already descended to the level of individual elements, and we can't index on the individual elements.
* Instead, we enclose the original query in grouping parentheses: `(@[:].a[@ > @[0]])`.
* Now we can select the first element of each array as follows: `(@[:].a[@ > @[0]])[:][0]`.

## Recursive search ##
* Suppose you have really deep JSON, but all you really want is a certain key in an object.
* For example, consider the JSON `[[[{"a": 1, "b": 2}], [{"a": 3, "b": 4}]]]`.
* You can recursively search for the key "a" in this JSON with *double-dot* syntax `@..a`. This will return `[1, 3]`.
* You can also recursively search for the keys "b" and "a" with the query `@..[b, a]`. This will return `[2, 1, 4, 3]`.

### Recursively find all descendants ###

*Added in v3.7.0*

`@..*` will return a single array containing all the *scalar* descendants of the current JSON, no matter their depth.

It will not return indices or parents, only the child nodes

For example, the `@..*` query on JSON
```json
{"a": [true, 2, [3]], "b": {"c": ["d", "e"], "f": null}}
```

will return

```json
[true, 2, 3, "d", "e", null]
```

## Functions ##
RemesPath supports a variety of functions, some of which are [vectorized](#vectorized-functions) and some of which are not.

We'll present the non-vectorized functions separately from the vectorized ones to avoid confusion.

Each subset will be organized in alphabetical order.

### Non-vectorized functions ###

`add_items(obj: object, k1: string, v1: anything, ...: string, anything (alternating)) -> object`

Takes 3+ arguments. As shown, every even-numbered argument must be a string (new keys).

Returns a *new object* with the key-value pair(s) k_i, v_i added.

*Does not mutate the original object.*

__EXAMPLES__
- add_items({}, "a", 1, "b", 2, "c", 3, "d", 4) -> {"a": 1, "b": 2, "c": 3, "d": 4}

-----
`all(x: array[bool]) -> bool`

Returns true if *all* of the values in `x` (which *must* contain all booleans) are `true`, else `false`.

---
`and(x: anything, y: anything, ...: anything) -> bool`

[*Added in v7.0*](/CHANGELOG.md#700---2024-02-09)

Returns `true` if and only if *all* of the arguments are ["truthy"](#truthiness).

Unlike the [`&` binary operator](#binary-operators-unary-operators-and-arithmetic) above, __this function uses conditional execution.__

This means that for example, if the input is `"abc"`, `and(is_num(@), @ < 3)` will return `false`, because *`@ < 3` will only be evaluated if `is_num(@)` evaluates to `true`.*

-----
`any(x: array[bool]) -> bool`

Returns true if *any* of the values in `x` (which *must* contain all booleans) are `true`, else `false`.

-----
`append(x: array, ...: anything) -> array`

Takes an array and any number of things (any JSON) and returns a *new array* with
the other things added to the end of the first array.

Does not mutate the original array.

The other things are added in the order that they were passed as arguments.

__EXAMPLES__
- `append([], 1, false, "a", [4]) -> [1, false, "a", [4]]`

-----
`at(x: array | object, inds: array | int | str) -> float`

If x is an array, inds must be an integer or an array of integers.
If x is an object, inds must be a string or an array of strings.
If inds is an array:
* returns `x[k]` for key/index `k` in `inds`.

__EXAMPLES__
- at([1, 2, 3], 0) -> 1<br></br>
- at(["foo", "bar", "baz"], [-1, 0]) -> ["baz", "foo"]
- at({"foo": 1, "bar": 2}, "bar") -> 2<br></br>
- at({"foo": 1, "bar": 2}, ["bar", "foo"]) -> [2, 1]

-----
`avg(x: array) -> float`

Finds the arithmetic mean of an array of numbers. `mean` is an alias for this function.

-----
`concat(x: array | object, ...: array | object) -> array | object`

Takes 2+ arguments, either all arrays or all objects.

If all args are arrays, returns an array that contains all elements of
every array passed in, in the order they were passed.

If all args are objects, returns an object that contains all key-value pairs in
all the objects passed in.

If multiple objects have the same keys, objects later in the arguments take precedence.

__EXAMPLES__
- `concat([1, 2], [3, 4], [5])` -> `[1, 2, 3, 4, 5]`
- `concat({"a": 1, "b": 2}, {"c": 3}, {"a": 4})` -> `{"b": 2, "c": 3, "a": 4}`
- `concat([1, 2], {"a": 2})` raises an exception because you can't concatenate arrays with objects.
- `concat(1, [1, 2])` raises an exception because you can't concatenate anything with non-iterables.

----
`csv_regex(nColumns: int, delim: string=",", newline: string="\r\n", quote_char: string="\"")`

Returns the regex that [`s_csv`](#vectorized-functions) uses to match a single row of a CSV file (formatted according to [RFC 4180](https://www.ietf.org/rfc/rfc4180.txt)) with delimiter `delim`, `nColumns` columns, quote character `quote_char`, and newline `newline`.

-----
`dict(x: array) -> object`

If x is an array of 2-element subarrays where the first element in each subarray is a string, return an object where each subarray is converted to a key-value pair.

__Example:__
* `dict([["a", 1], ["b", 2]])` returns `{"a": 1, "b": 2}`.

---
`enumerate(x: array) -> array`

For each index in the array, returns a subarray containing that index and the element at that index. *Added in [v5.2](/CHANGELOG.md#520---2023-06-04)*

__Example:__
* `enumerate(["a", "b", "c"])` returns `[[0, "a"], [1, "b"], [2, "c"]]`

----
`flatten(x: array, depth: int = 1]) -> array`
   
Recursively searches in `x` down to a depth of `depth`, pulling each element of every sub-array at that depth into the final array.

It's easier to understand with some __examples:__
* `flatten([[1, 2], [3, 4]])` returns `[1, 2, 3, 4]`.
* `flatten([1, 2, 3])` returns `[1, 2, 3]`.
* `flatten([1, [2, [3]]])` returns `[1, 2, [3]]`.
* `flatten([1, [2, [3, [4]]]], 3)` returns `[1, 2, 3, 4]`.

----
`group_by(x: array, k: int | str | array) -> object`

* If `k` is an array (*and the JsonTools version is [v5.7](/CHANGELOG.md#570---2023-09-08) or greater*)
   * Returns a new object where each value `v` associated with `k[n]` is mapped to all (children of itbl where `child[k[n]] == v`) recursively grouped by `[k[n + 1], k[n + 2], ...]`.
* If `x` is an array of *arrays*:
   * If `k` is *not* an int, throw an error.
   * Return an object where key `str(v)` has an array of sub-arrays `subarr` such that `subarr[k] == v` is `true`.
   * Note that `subarr[k]` might not be a string in these sub-arrays. However, keys in JSON objects must be strings, so the key is the string representation of `subarr[k]` rather than `subarr[k]` itself.
   * Prior to [v5.5.0](/CHANGELOG.md#550---2023-08-13), Python-style negative indices were not allowed for the `k` argument.
* If `x` is an array of *objects*:
   * If `k` is *not* a string, throw an error.
   * Return an object where key `str(v)` has an array of sub-objects `subobj` such that `subobj[k] == v` is `true`.
   * Note that `subobj[k]` might not be a string in these sub-objects.
* If `x` is an array of anything else, or it has a mixture of arrays an objects, throw an error.

__Examples:__
* `group_by([{"foo": 1, "bar": "a"}, {"foo": 2, "bar": "b"}, {"foo": 3, "bar": "a"}], "bar")` returns
```json
{"a": [{"foo": 1, "bar": "a"}, {"foo": 3, "bar": "a"}], "b": [{"foo": 2, "bar": "b"}]}
```
* `group_by([[1, "a"], [2, "b"], [2, "c"], [3, "d"]], 0)` returns
```json
{"1": [[1, "a"]], "2": [[2, "b"], [2, "c"]], "3": [[3, "d"]]}
```
* `group_by([{"a": 1, "b": "x", "c": -0.5}, {"a": 1, "b": "y", "c": 0.0}, {"a": 2, "b": "x", "c": 0.5}], ["a", "b"])` returns
```json
{"1": {"x": [{"a": 1, "b": "x", "c": -0.5}], "y": [{"a": 1, "b": "y", "c": 0.0}]}, "2": {"x": [{"a": 2, "b": "x", "c": 0.5}]}}
```
* `group_by([[1, "x", -0.5], [1, "y", 0.0], [2, "x", 0.5]], [1, 0])` returns
```json
{"x": {"1": [[1, "x", -0.5]], "2": [[2, "x", 0.5]]}, "y": {"1": [[1, "y", 0.0]]}}
```
* `group_by([[1, 2, 2, 0.0], [1, 2, 3, -1.0], [1, 3, 3, -2.0], [1, 3, 4, -3.0], [2, 2, 2, -4.0]], [0, 1, 2])` returns
```json
{"1": {"2": {"2": [[1, 2, 2, 0.0]], "3": [[1, 2, 3, -1.0]]}, "3": {"3": [[1, 3, 3, -2.0]], "4": [[1, 3, 4, -3.0]]}}, "2": {"2": {"2": [[2, 2, 2, -4.0]]}}}
```
----
`in(elt: anything, itbl: object | array) -> bool`

* If `itbl` is an *array*:
    * if `elt` has a type that is not comparable with an element of `itbl`, throws an error. 
    * returns `true` if `elt` is equal to any element.
* If `itbl` is an *object*:
    * if `elt` is not a string, throws an error.
    * If `elt` is one of the keys in `itbl`, returns `true`.

----
`index(x: array, elt: anything, reverse: bool = false) -> int`

* If `reverse` is `false` (*default*): Returns the index of the *first* element in `x` that is equal to `elt`.
* If `reverse` is `true`: Returns the index of the *last* element in `x` that is equal to `elt`.
* If *no elements in x are equal to elt*, throws an error.

----
`items(x: object) -> array`

Returns an array of 2-item subarrays (the key-value pairs of `x`).

Because objects are not inherently ordered, you may need to sort the key-value pairs by their key or value to get the same result every time.

---
`iterable(x: anything) -> bool`

Returns whether `x` is an iterable (object or array). *Added in [v5.2](/CHANGELOG.md#520---2023-06-04)*

Because this function is *not vectorized*, use this instead of `is_expr` if you want to a single bool returned for an entire iterable.

----
`keys(x: object) -> array`

Returns an array of the keys in `x`.

----
`len(x: object | array) -> int`

Returns the number of key-value pairs in `x` (if an object) or the number of elements in `x` (if an array).

----
`max(x: array) -> float`

Returns a floating-point number equal to the maximum value in an array.

----
`max_by(x: array, k: int | str | function) -> anything`

* *If `k` is a function:*
    * Return the child `maxchild` in `x` such that `k(maxchild) >= k(child2)` for every other child `child2` in `x`.
* If `x` is an array of *arrays*:
    * If `k` is not an int or if `k >= len(x) or k < -len(x)`, throw an error.
    * Return the subarray `maxarr` such that `maxarr[k] >= subarr[k]` for all other sub-arrays `subarr` in `x`.
    * NOTE: prior to [v5.5.0](/CHANGELOG.md#550---2023-08-13), Python-style negative indices were not allowed at all.
* If `x` is an array of *objects*:
    * If `k` is not a string, throw an error.
    * Return the subobject `maxobj` such that `maxobj[k] >= subobj[k]` for all other sub-objects `subobj` in `x`.

__Examples:__
* With `[[1, 2], [2, 0], [3, -1]]` as input, `max_by(@, 0)` returns `[3, -1]` because that is the subarray with the largest first element.
* With `[{"a": 1, "b": 3}, {"a": 2, "b": 2}, {"a": 3, "b": 1}]` as input, `max_by(@, b)` returns `{"a": 1, "b": 3}` because that is the subobject with the largest value associated with key `b`.
* With `["a", "bbb", "cc"]` as input, `max_by(@, s_len(@))` returns `"bbb"`, because that is the child with the greatest length (recall that `s_len` returns the length of a string).

----
`min(x: array) -> float`

Returns a floating-point number equal to the minimum value in an array.

----
`min_by(x: array, k: int | str) -> array | object`

See `max_by`, but minimizing instead of maximizing.

---
`or(x: anything, y: anything, ...: anything) -> bool`

[*Added in v7.0*](/CHANGELOG.md#700---2024-02-09)

Returns `true` if and only if *any* of the arguments are ["truthy"](#truthiness).

Unlike the [`|` binary operator](#binary-operators-unary-operators-and-arithmetic) above, __this function uses conditional execution.__

This means that for example, if the input is `3`, `or(is_num(@), s_len(@) < 3)` will return `true`, because *`s_len(@) < 3` will only be evaluated if `is_num(@)` evaluates to `false`.*

---
`pivot(x: array[object | array], by: str | int, val_col: str | int, ...: str | int) -> object[str, array]`

There must be at least 3 arguments to this function.

The first argument should be an array whose sub-iterables have a repeating cycle of values for one column (`by`), and the only other column that varies within a given cycle is the values column (`val_col`).

The result is an object where each distinct value of the `by` column is mapped to an array of the corresponding values in the `val_col` column. Additionally, you may include any number of other columns.

__Examples:__
- With
```json
[
    ["foo", 2, 3, true],
    ["bar", 3, 3, true],
    ["foo", 4, 4, false],
    ["bar", 5, 4, false]
]
```
as input, `pivot(@, 0, 1, 2, 3)` (use `0` as pivot, `1` as values, `2` and `3` as other columns) returns
```json
{
    "foo": [2, 4],
    "bar": [3, 5],
    "2": [3, 4],
    "3": [true, false]
}
```
- With
```json
[
    {"a": "foo", "b": 2, "c": 3},
    {"a": "bar", "b": 3, "c": 3},
    {"a": "foo", "b": 4, "c": 4},
    {"a": "bar", "b": 5, "c": 4}
]
```
as input, `pivot(@, a, b, c)` returns
```json
{
    "foo": [2, 4],
    "bar": [3, 5],
    "c": [3, 4]
}
```

----
`quantile(x: array, q: float) -> float`

x must contain only numbers.

q must be between 0 and 1, exclusive.

Returns the q^th quantile of `x`, as a floating-point number.

So `quantile(x, 0.5)` returns the median, `quantile(x, 0.75)` returns the 75th percentile, and so on.

Uses linear interpolation if the index found is not an integer.

For example, suppose that the 60th percentile is at index 6.6, and elements 6 and 7 are 8 and 10.

Then the returned value is `0.6*10 + 0.4*8`, or 9.2.

---
`rand() -> float`

Random number between 0 (inclusive) and 1 (exclusive). *Added in [v5.2](/CHANGELOG.md#520---2023-06-04)*

---
`randint(start: int, end: int=null) -> int`

*Added in [v6.0](/CHANGELOG.md#600---2023-12-13)*
Returns a random integer greater than or equal to `start` and less than `end`.
If `end` is not specified, instead return a random integer greater than or equal to 0 and less than `start`.

----
`range(start: int, end: int = null, step: int = 1) -> array[int]`

Returns an array of integers.

* If `end` and `step` are not supplied, return all the integers from 0 to start, excluding start.
   * So `range(3)` returns `[0, 1, 2]`
   * `range(-1)` returns `[]` because -1 is less than 0.
* If `step` is not supplied, return all the integers from `start` to `end`, excluding `end`.
   * `range(3, 5)` returns `[3, 4]`.
   * `range(3, 1)` returns `[]` because 1 is less than 3.
* If all arguments are supplied, return all the integers from `start` to `end`, incrementing by `step` each time.
   * `range(3, 1, -1)` returns `[3, 2]`.
   * `range(0, 6, 3)` returns `[0, 3]`.

---
`s_cat(x: anything, ...: anything) -> string`

*Added in [v6.1](/CHANGELOG.md#610---2023-12-28)*

Concatenates the string representation (or the value, for a string) of every argument. Arrays and objects are incorporated using the Python-style compact representation, with a single space after item-separating commas and key-value separating colons. 

__Example:__
* With input `[[1, 2], 3, {"a": 4}]`, ``s_cat(@[0], foo, ` bar `, @[1] * 3, @[2])`` will return `"[1, 2]foo bar 9{\"a\": 4}"`

----
`s_join(sep: string, x: array) -> string`

Every element of `x` must be a string.

Returns x string-joined with sep (i.e., returns a string that begins with `x[0]` and has `sep` between `x[i - 1]` and `x[i]` for `1 <= i <= len(x)`)

---
`set(x: array) -> object`

*Added in [v6.0](/CHANGELOG.md#600---2023-12-13)*

Returns an object mapping each unique string representation of an element in `x` to null. This may be preferable to `unique` because of the O(1) average-case lookup performance in an object.

Example: ``set(j`["a", "b", "a", 1, 2.0, null, 1, null]`)`` returns `{"a": null, "b": null, "1": null, "2.0": null, "null": null}`

One issue with this function that may make the `unique` function preferable: two different elements may have the same string representation for the purposes of this function (e.g., `null` and `"null"`, `2.0` and `"2.0"`)

----
`sort_by(x: array, k: string | int | function, descending: bool = false)`

`x` must be:
* an array of arrays (if `k` is an integer)
* an array of objects (if `k` is a string)
* any array (if `k` is a function)

Returns:
* a new array of subarrays/subobjects `subitbl` such that `subitbl[k]` is sorted (if `k` is an integer or string)
* a new array of children `child` such that `k(child)` is sorted (if `k` is a function)

Analogous to SQL `ORDER BY`.

By default, these sub-iterables are sorted ascending. If `descending` is `true`, they will instead be sorted descending.

Prior to [v5.5.0](/CHANGELOG.md#550---2023-08-13), Python-style negative indices were not allowed for the `k` argument.

__Examples:__
* With `[[1, 2], [2, 0], [3, -1]]` as input, `sort_by(@, 1)` returns `[[3,-1],[2,0],[1,2]]` because it sorts ascending by the second element.
* With `[{"a": 1, "b": 3}, {"a": 2, "b": 2}, {"a": 3, "b": 1}]` as input, `sort_by(@, a, true)` returns `[{"a":3,"b":1},{"a":2,"b":2},{"a":1,"b":3}]` because it sorts descending by key `a`.
* With `["a", "bbb", "cc"]` as input, `sort_by(@, s_len(@))` returns `["a", "cc", "bbb"]`, because the children are sorted ascending by string length.

----
`sorted(x: array, descending: bool = false)`

`x` must be an array of all strings or all numbers. Either is fine so long as all elements are comparable.

Returns a new array where the elements are sorted ascending. If `descending` is `true`, they're instead sorted descending.

See the [general notes on string sorting](/README.md#note-on-how-jsontools-sorts-strings) for notes on how strings are sorted.

----
`sum(x: array) -> float`

Returns the sum of the elements in x. 

x must contain only numbers. Booleans are fine.

---
`stringify(elt: anything, print_style: string=m, sort_keys: bool=true, indent: int | str=4) -> str`

Returns the string representation (compressed, minimal whitespace, sort keys) of x.

When called with one argument, `stringify` differs from `str` in two regards:
1. `stringify` is not vectorized.
2. If `x` is a string, `str` returns a copy of `x`, but `stringify` returns the string representation of `x`.
    * For example, `str(abc)` returns `"abc"`, but `stringify(abc)` returns `"\"abc\""`.

*Added in [v5.5.0](/CHANGELOG.md#550---2023-08-13).*

__The optional arguments did not exist before [v7.0](/CHANGELOG.md#700---2024-02-09).__ Since that version, they work as follows:

If the third argument (`sort_keys`, default true) is false, object keys are not sorted.

If the fourth argument (`indent`, default 4) is an integer, the indent for pretty-print options is that integer. If it is `` `\t` `` (the tab character), tabs are used for indentation.

* if `print_style` (the second argument) is `m` (the default), return the [minimal-whitespace compact representation](/docs/README.md#minimal_whitespace_compression).
* if `print_style` is `c`, return the Python-style compact representation (one space after ',' or ':')
* if `print_style` is `g`, return the Google-style [pretty-printed representation](/docs/README.md#pretty_print_style)
* if `print_style` is `w`, return the Whitesmith-style pretty-printed representation
* if `print_style` is `p`, return the PPrint-style pretty-printed representation

---
`to_csv(x: array, delimiter: string=",", newline: string="\r\n", quote_char: string="\"") -> string`

*Added in [v6.0](/CHANGELOG.md#600---2023-12-13)*

Returns x formatted as a CSV (RFC 4180 rules as normal), according to the following rules:
* if x is an array of non-iterables, each child is converted to a string on a separate line
* if x is an array of arrays, each subarray is converted to a row
* if x is an array of objects, the keys of the first subobject are converted to a header row, and the values of every subobject become their own row.

See [json-to-csv.md](/docs/json-to-csv.md#how-json-nodes-are-represented-in-csv) for information on how JSON values are represented in CSVs.

---
`to_records(x: iterable, [strategy: str]) -> array[object]`

Converts some iterable to an array of objects, using one of the [strategies](/docs/json-to-csv.md#strategies) used to make a CSV in the JSON-to-CSV form. The resulting JSON is just the JSON equivalent of the CSV that would be generated with `x` as input and the same strategy (each object has the same column types and same column names as the corresponding row of the CSV).

The strategy argument must be one of the following strings:
- 'd': [default](/docs/json-to-csv.md#default)
- 'r': [full recursive](/docs/json-to-csv.md#full-recursive)
- 'n': [no recursion](/docs/json-to-csv.md#no-recursion)
- 's': [stringify iterables](/docs/json-to-csv.md#stringify-iterables)

---
`type(x: anything) -> str`

Returns the [JSON Schema type name](https://json-schema.org/understanding-json-schema/reference/type.html#type-specific-keywords) for x. *Added in [v5.5.0](/CHANGELOG.md#550---2023-08-13).*

----
`unique(x: array, sorted: bool = false)`

Returns an array of all the unique elements in `x`. 

If `sorted` is true, sorts the array ascending. This will raise an error if not all of x's elements are comparable.

----
`value_counts(x: array, sort_by_count: bool = false) -> array`

Returns an array of two-element subarrays `[k: anything, count: int]` where `count` is the number of elements in `x` equal to `k`.

The order of the sub-arrays is unreliable.

As of [5.3.0](/CHANGELOG.md#530---2023-06-10), there is an second optional argument (default false). If true, the subarrays are sorted by count descending.

__Example:__
* `value_counts(["a", "b", "c", "c", "c", "b"], true)` returns `[["c", 3], ["b", 2], ["a", 1]]`

----
`zip(x1: array, ...: array) -> array`

There must be at least two arguments to this function, all arrays.

Returns a new array in which each `i^th` element is an array containing the `i^th` elements of each argument, in the order in which they were passed.

All the argument arrays *must have the same length*.

In other words, it's like the Python `zip` function, except it returns an array, not a lazy iterator.

__Example:__
* `zip(["a", "b", "c"], [1, 2, 3])` returns `[["a", 1], ["b", 2], ["c", 3]]`.

### Vectorized functions ###

__All of these functions are vectorized across their first argument,__ meaning that when one of these functions is called on an array or object, *any functions in the second and subsequent arguments reference the entire array/object, but the first argument is set to one element at a time.*

For example, consider the vectorized function `s_mul(s: string, n: int) -> string`. This function concatenates `n` instances of string `s`.
* With __array__ input `["a", "cd", "b"]`, `s_mul(@, len(@))` returns `["aaa", "cdcdcd", "bbb"]`
    * The *first argument* references each element of the array separately.
    * The *second argument `len(@)` references the entire array*, and is thus `3`, because the array has three elements.
    * Because the *first element of the first argument* is `"a"`, the *first element of the output* is `s_mul(a, 3)`, or `"aaa"`
    * Because the *second element of the first argument* is `"cd"`, the *second element of the output* is `s_mul(cd, 3)`, or `"cdcdcd"`
* With __object__ input `{"foo": "a", "bar": "cd"}`, `s_mul(@, len(@))` returns `{"foo": "aa", "bar": "cdcd"}` (__NOTE__: this example will fail on JsonTools earlier than [v7.0](/CHANGELOG.md#700---2024-02-09))
    * The *first argument* references each element of the object separately.
    * The *second argument `len(@)` references the entire object*, and is thus `2`, because the object has two children.
    * Because the *child of key `foo` of the first argument* is `"a"`, the *child of key `foo` of the output* is `s_mul(a, 2)`, or `"aa"`
    * Because the *child of key `bar` of the first argument* is `"cd"`, the *child of key `bar` of the output* is `s_mul(cd, 2)`, or `"cdcd"`


All the vectorized string functions have names beginning with `s_`.

----
`abs(x: number) -> number`

Returns the absolute value of x.

---
`bool(x: anything) -> bool`

True if [`x` is "truthy"](#truthiness).

----
`float(x: number | string) -> number`

* If x is a boolean, integer, or float: Returns a 64-bit floating-point number equal to x.
* If x is a __*decimal* string representation of a floating-point number__: returns the 64-bit floating point number that is represented.

----
`ifelse(cond: anything, if_true: anything, if_false: anything) -> anything`

Returns `if_true` if `cond` is ["truthy"](#truthiness), otherwise returns `if_false`.

__Note:__
* Beginning in [v7.0](/CHANGELOG.md#700---2024-02-09), this function's execution is conditional, meaning that *only the chosen branch is executed*. 
* For example, consider the input `["foo", 1, "a", null]`.
* Prior to v7.0, the query `@[:]->ifelse(is_str(@), s_len(@), -1)` would raise an error on that input, because it would call `s_len` on non-strings (illegal arguments).
* As of v7.0, the expected `[3, -1, 1, -1]` would be returned, because the `s_len` function would only be called when `is_str` returned true (i.e., on strings).

----
`int(x: number | string) -> int`

* If x is a boolean or integer: returns a 64-bit integer equal to x.
* If x is a float: returns the closest 64-bit integer to x.
   * Note that this is *NOT* the same as the Python `int` function, because __if x is halfway between two integers, the nearest *even integer* is returned.__
* If x is a __*decimal* string representation of an integer__: returns the integer that is represented. *This means hex numbers can't be parsed by this function, and you should use `num` below instead for that.*

----
`is_expr(x: anything) -> bool`

Returns true iff x is an array or object.

----
`is_num(x: anything) -> bool`

Returns true iff x is a number.

----
`is_str(x: anything) -> bool`

Returns true iff x is a string.

----
`isna(x: number) -> bool`

Returns true iff x is the floating-point Not-A-Number (represented in some JSON by `NaN`).

Recall that `NaN` is *NOT* in the original JSON specification.

---
`isnull(x: anything) -> bool`

Returns true iff x is null, else false.

----
`log(x: number, base: number = e) -> number`

Returns the log base `base` of `x`. If `base` is not specified, returns the [natural logarithm (base `e`)](https://en.wikipedia.org/wiki/Natural_logarithm) of `x`.

----
`log2(x: number) -> number`

Returns the log base 2 of x.

---
`num(x: anything) -> float`

*Added in [v6.0](/CHANGELOG.md#600---2023-12-13)*

As `float` above, but also handles hex integers preceded by `0x` (and optional `+` or `-` sign).

This is the only function that is guaranteed to be able to parse anything captured by the `(NUMBER)` capture group in the `s_fa` and `s_sub` functions.

__EXAMPLES:__
* With `["+0xff" "-0xa", "10", "-5e3", 1, true, false, -3e4, "0xbC"]` as input, returns `[255.0, -10.0, 10.0, -5000.0, 1.0, 1.0, 0.0, -30000.0, 188.0]`

----
`not(x: bool) -> bool`

Logical `NOT`. __Replaced with a unary operator of the same name in [5.4.0](/CHANGELOG.md#540---2023-07-04).__

---
`parse(x: str) -> anything`

Attempts to parse `x` as JSON according to the most permissive [parser setttings](/docs/README.md#parser-settings). *Added in [v5.5.0](/CHANGELOG.md#550---2023-08-13).*

* If `x` is not a string or there is a fatal error while parsing, returns
    ```
    {"error": "the exception raised as a string"}
    ```
* If `x` is parsed successfully, returns
    ```
    {"result": x parsed as JSON}
    ```

EXAMPLE:
Consider the input
```json
[
    "[1,2,3]",
    "u"
]
```
The query `parse(@)` will return
```json
[
    {"result": [1, 2, 3]},
    {"error": "No valid literal possible at position 0 (char 'u')"}
]
```

----
`round(x: number, sigfigs: int = 0) -> float | int)`

`x` must be an integer or a floating-point number, *not* a boolean.

* If sigfigs is 0: Returns the closest 64-bit integer to `x`.
* If sigfigs > 0: Returns the closest 64-bit floating-point number to `x` rounded to `sigfigs` decimal places.

----
`s_count(x: string, sub: regex | string) -> int`

Returns the number of times substring/regex `sub` occurs in `x`.

---
`s_csv(csvText: string, nColumns: int, delimiter: string=",", newline: string="\r\n", quote: string="\"", header_handling: string="n", ...: int) -> array[(array[string | number] | object[string | number])]`

__[Introduced in v6.0](/CHANGELOG.md#600---2023-12-13).__

__Arguments:__
* `csvText` (1st arg): the text of a CSV file encoded as a JSON string
* `nColumns` (2nd arg): the number of columns
* `delimiter` (3rd arg, default `,`): the column separator
* `newline` (4th arg default `\r\n`): the newline. Must be one of (``)
* `quote` (5th arg, default `"`): the character used to wrap columns that `newline`, `quote`, or `delimiter`.
* `header_handling` (6th arg, default `n`): how the header row is treated. *Must be one of `n`, `d`, or `h`.* Each of these options will be explained in the list below.
    * *`n`: skip header row (this is the default)*.                    This would parse the CSV file `"foo,bar\n1,2` as `[["1", "2"]]`
    * *`h`: include header row*.                                       This would parse the CSV file `"foo,bar\n1,2` as `[["foo", "bar"], ["1", "2"]]`
    * *`d`: return an array of objects, using the header row as keys.* This would parse the CSV file `"foo,bar\n1,2` as `[{"foo": "1", "bar": "2"}]`
* `...` (7th and subsequent args): the numbers of columns to attempt to parse as numbers. [Any valid number within the JSON5 specification](https://spec.json5.org/#numbers) can be parsed. You can pass a negative number here to get the nth-to-last column rather than the nth column.

__Return value:__
* if `nColumns` is 1, returns an array of strings
* otherwise, returns an array of arrays of strings, where each sub-array is a row that has exactly `nColumns` columns.

__Notes:__
* *Any row that does not have exactly `nColumns` columns will be ignored completely.*
* See [RFC 4180](https://www.ietf.org/rfc/rfc4180.txt) for the accepted format of CSV files. A brief synopsis is below.
* Any column that starts and ends with a quote character is assumed to be a quoted string. In a quoted string, anything is fine, but *a literal quote character in a quoted column must be escaped with itself*.
    * For example, `"""quoted"",string,in,quoted column"` is a valid column in a file with `,` delimiter and `"` quote character.
    * On the other hand, `" " "` is *not a valid column if `"` is the quote character* because it contains an unescaped `"` in a quoted column.
    * Finally, `a,b` would be treated as two columns in a CSV file with `"` quote character, but `"a,b"` is a single column because a comma is not treated as a column separator in a quoted column.
* Columns containing literal quote characters or the newline characters `\r` and `\n` must be wrapped in quotes.
* When `s_csv` parses a file, quoted values are parsed without the enclosing quotes and with any internal doubled quote characters replaced with a single instance of the quote character. Thus the valid value (for `"` quote character)`"foo""bar"` would be parsed as the JSON string `"foo\"bar"`
* You can pass in `null` for the 3rd, 4th, and 5th args. Any instance of `null` in those args will be replaced with the default value.
* To improve performance, this function and `s_fa` use a shared cache that maps (input, function argument) pairs to the return value of the function. Up to 8 return values can be cached, only documents between 100KB and (5MB if 32bit, else 10MB) use the cache, and the cache is disabled for mutating queries (to avoid mutating values in the cache).
* Prior to [v6.1](/CHANGELOG.md#610---2023-12-28), this function did not work properly if the delimiter was a regex metacharacter like `|`.

__Example:__
Suppose you have the JSON string `"nums,names,cities,date,zone,subzone,contaminated\nnan,Bluds,BUS,,1,'',TRUE\n0.5,dfsd,FUDG,12/13/2020 0:00,2,c,TRUE\n,qere,GOLAR,,3,f,\n1.2,qere,'GOL''AR',,3,h,TRUE\n'',flodt,'q,tun',,4,q,FALSE\n4.6,Kjond,,,,w,''\n4.6,'Kj\nond',YUNOB,10/17/2014 0:00,5,z,FALSE"`

which represents this CSV file (7 columns, comma delimiter, `LF` newline, `'` quote character):
```
nums,names,cities,date,zone,subzone,contaminated
nan,Bluds,BUS,,1,'',TRUE
0.5,dfsd,FUDG,12/13/2020 0:00,2,c,TRUE
,qere,GOLAR,,3,f,
1.2,qere,'GOL''AR',,3,h,TRUE
'',flodt,'q,tun',,4,q,FALSE
4.6,Kjond,,,,w,''
4.6,'Kj
ond',YUNOB,10/17/2014 0:00,5,z,FALSE
```
Notice that the 8th row of this CSV file has a newline in the middle of the second column, and this is fine, because as discussed above, this column is quoted and newlines are allowed within a quoted column.

The query ``s_csv(@, 7, `,`, `\n`, `'`)`` will correctly parse this as *an array of seven 7-string subarrays (omitting the header)*, shown below:
```json
[
    ["nan", "Bluds", "BUS", "", "1", "", "TRUE"],
    ["0.5", "dfsd", "FUDG", "12/13/2020 0:00", "2", "c", "TRUE"],
    ["", "qere", "GOLAR", "", "3", "f", ""],
    ["1.2", "qere", "GOL'AR", "", "3", "h", "TRUE"],
    ["", "flodt", "q,tun", "", "4", "q", "FALSE"],
    ["4.6", "Kjond", "", "", "", "w", ""],
    ["4.6", "Kj\nond", "YUNOB", "10/17/2014 0:00", "5", "z", "FALSE"]
]
``` 

The query ``s_csv(@, 7, `,`, `\n`, `'`, h, 0, -3)`` will correctly parse this as *an array of eight 7-item subarrays (including the heaader) with the 1st and 3rd-to-last (i.e. 5th) columns parsed as numbers where possible*, shown below:
```json
[
    ["nums", "names", "cities", "date", "zone", "subzone", "contaminated"],
    ["nan", "Bluds", "BUS", "", 1, "", "TRUE"],
    [0.5, "dfsd", "FUDG", "12/13/2020 0:00", 2, "c", "TRUE"],
    ["", "qere", "GOLAR", "", 3, "f", ""],
    [1.2, "qere", "GOL'AR", "", 3, "h", "TRUE"],
    ["", "flodt", "q,tun", "", 4, "q", "FALSE"],
    [4.6, "Kjond", "", "", "", "w", ""],
    [4.6, "Kj\nond", "YUNOB", "10/17/2014 0:00", 5, "z", "FALSE"]
]
``` 

---
`s_fa(x: string, pat: regex | string, includeFullMatchAsFirstItem: bool = false, ...: int) -> array[string | number] | array[array[string | number]]`

__Added in [v6.0](/CHANGELOG.md#600---2023-12-13).__

* If the third argument, `includeFullMatchAsFirstItem`, is set to `false` (the default):
    * If `pat` is a regex with *no capture groups or one capture group*, returns an array of the substrings of `x` that match `pat`.
    * If `pat` has *multiple capture groups*, returns an array of subarrays of substrings, where each subarray has a number of elements equal to the number of capture groups.
* otherwise:
    * If `pat` is a regex with *no capture groups*, returns an array of the substrings of `x` that match `pat`.
    * If `pat` has *at least one capture group*, returns an array of subarrays of substrings, where each subarray has a number of elements equal to the number of capture groups + 1, *and the first element of each subarray is the entire text of the match (including the uncaptured text)*.

The fourth argument and any subsequent argument must all be the number of a capture group to attempt to parse as a number (`0` matches the match value if there were no capture groups). [Any valid number within the JSON5 specification](https://spec.json5.org/#numbers) can be parsed. If a capture group cannot be parsed as a number, the capture group is returned. As with `s_csv` above, you can use a negative number to parse the nth-to-last column as a number instead of the nth column as a numer.

__SPECIAL NOTES FOR `s_fa`:__
1. *`s_fa` treats `^` as the beginning of a line and `$` as the end of a line*, but elsewhere in JsonTools (prior to [v7.0](/CHANGELOG.md#700---2024-02-09)) `^` matches only the beginning of the string and `$` matches only the end of the string.
2. Every instance of `(INT)` in `pat` will be replaced by a regex that captures a decimal number or (a hex integer preceded by `0x`), optionally preceded by a `+` or `-`. A noncapturing regex that matches the same thing is available through `(?:INT)`.
3. Every instance of `(NUMBER)` in `pat` will be replaced by a regex that captures a decimal floating point number or (a hex integer preceded by `0x`). A noncapturing regex that matches the same thing is available through `(?:NUMBER)`. *Neither `(NUMBER)` nor `(?:NUMBER)` matches `NaN` or `Infinity`, but those can be parsed if desired.*
4. *`s_fa` may be very slow if `pat` is a function of input,* because the above described regex transformations need to be applied every time the function is called instead of just once at compile time.

__Examples:__
1. ``s_fa(`1  -1 +2 -0xF +0x1a 0x2B`, `(INT)`)`` will return `["1", "-1", "+2", "-0xF", "+0x1a", "0x2B"]`
2. ``s_fa(`1  -1 +2 -0xF +0x1a 0x2B 0x10000000000000000`, `(?:INT)`,false, 0)`` will return `[1, -1, 2, -15, 26, 43, "0x10000000000000000"]` because passing `0` as the fourth arg caused all the match results to be parsed as integers, except `0x10000000000000000`, which stayed as a string because its numeric value was too big for the 64-bit integers used in JsonTools.
3. ``s_fa(`a 1.5 1\r\nb -3e4 2\r\nc -.2 6`, `^(\w+) (NUMBER) (INT)\r?$`,false, 1)`` will return `[["a",1.5,"1"],["b",-30000.0,"2"],["c",-0.2,"6"]]`. Note that the second column but not the third will be parsed as a number, because only `1` was passed in as the number of a capture group to parse as a number.
4. ``s_fa(`a 1.5 1\r\nb -3e4 2\r\nc -.2 6`, `^(\w+) (NUMBER) (INT)\r?$`,false, -2, 2)`` will return `[["a",1.5,1],["b",-30000.0,2],["c",-0.2,6]]`. This time the same input is parsed with numbers in the second-to-last and third columns because `-2` and `2` were passed as optional args.
5. ``s_fa(`a 1.5 1\r\nb -3e4 2\r\nc -.2 6`, `^(\w+) (?:NUMBER) (INT)\r?$`,false, 1)`` will return `[["a",1],["b",2],["c",6]]`. This time the same input is parsed with only two columns, because we used a noncapturing version of the number-matching regex.
6. 1. ``s_fa(`a1  b+2 c-0xF d+0x1a`, `[a-z](INT)`, true, 1)`` will return `[["a1",1],["b+2",2],["c-0xF",-15],["d+0x1a",26]]` because the third argument is `true` and there is one capture group, meaning that the matches will be represented as two-element subarrays, with the first element being the full text of the match, and the second element being the captured integer parsed as a number.
7. 1. ``s_fa(`a1  b+2 c-0xF d+0x1a`, `[a-z](?:INT)`, true)`` will return `["a1","b+2","c-0xF","d+0x1a"]` because the third argument is `true` but there are no capture groups, so an array of strings is returned instead of 1-element subarrays.

----
`s_find(x: string, sub: regex | string) -> array[string]`

Returns an array of all the substrings in `x` that match `sub`.

__As of [v6.0](/CHANGELOG.md#600---2023-12-13), *this function is DEPRECATED in favor of `s_fa`*.__ However, it can still be useful if you always want the result to be a single string rather than an array of capture groups.

---
`s_format(s: str, print_style: string=m, sort_keys: bool=true, indent: int | str=4, remember_comments: bool=false) -> str`

[*Added in v7.0*](/CHANGELOG.md#700---2024-02-09)

If `s` is not valid JSON (according to the most permissive parsing rules, same as used by the parse() function),
return a copy of `s`.

Otherwise, let `elt` be the JSON returned by parsing `s`.

If the third argument (`sort_keys`, default true) is false, object keys are not sorted.

If the fourth argument (`indent`, default 4) is an integer, the indent for pretty-print options is that integer. If it is `` `\t` `` (the tab character), tabs are used for indentation.

*If __not__ `remember_comments` (the fifth argument)*, return `elt` formatted as follows:
* if `print_style` (the second argument) is `m` (the default), return the [minimal-whitespace compact representation](/docs/README.md#minimal_whitespace_compression).
* if `print_style` is `c`, return the Python-style compact representation (one space after ',' or ':')
* if `print_style` is `g`, return the Google-style [pretty-printed representation](/docs/README.md#pretty_print_style)
* if `print_style` is `w`, return the Whitesmith-style pretty-printed representation
* if `print_style` is `p`, return the PPrint-style pretty-printed representation

*If `remember_comments`*, any comments in `s` will be remembered as described in [the `remember_comments` setting](/docs/README.md#remember_comments), and return `elt` formatted as follows:
* if `print_style` is `m` or `c` (the default), compressed.
* if `print_style` is `g` or `w`, pretty-printed Google-style.
* if `print_style` is `p`, pretty-printed PPrint-style.

----
`s_len(x: string) -> int`

The length of string x, when encoded in UTF-16. In brief, this means that most characters count for 1, but some characters like 😀 count for 2 or more.

Note that the character count in the Notepad++ status bar indicates the number of bytes in the UTF-8 representation of text, and this will be greater than the value returned by `s_len` for any text that contains non-ASCII characters.

----
`s_lower(x: string) -> string`

The lower-case form of x.

---
`s_lines(x: string) -> array[string]`

*Added in [v6.1](/CHANGELOG.md#610---2023-12-28)*

Returns an array of all the lines (including an empty string at the end if there's a trailing newline) in `x`.

This function treats `\r`, `\n`, and `\r\n` all as valid newlines. Use `s_split` below if you want to only accept one or two of those.

---
`s_lpad(x: string, padWith: string, padToLen: int) -> string`
*Added in [v6.1](/CHANGELOG.md#610---2023-12-28)*
return a string that contains `s` padded on the *left* with enough repetitions of `padWith`
to make a composite string with length at least `padToLen`
__EXAMPLES:__
* `s_lpad(foo, e, 5)` returns `"eefoo"`
* ``s_lpad(ab, `01`, 5)`` returns `"0101ab"`
* ``s_lpad(abc, `01`, 5)`` returns `"01abc"`

----
`s_mul(x: string, reps: int) -> string`

A string containing `x` repeated `reps` times. E.g., ``s_mul(`abc`, 3)`` returns `"abcabcabc"`.

Basically `x * reps` in Python, except that the binary operator `*` doesn't have that capability in RemesPath.

*Note that as of [v5.1](/CHANGELOG.md#510---2023-06-02), this function is unnecessary because `x * reps` will return the same thing as `s_mul(x, reps)`.*

---
`s_rpad(x: string, padWith: string, padToLen: int) -> string`
*Added in [v6.1](/CHANGELOG.md#610---2023-12-28)*
return a string that contains `s` padded on the *right* with enough repetitions of `padWith`
to make a composite string with length at least `padToLen`
__EXAMPLES:__
* `s_rpad(foo, e, 5)` returns `"fooee"`
* ``s_rpad(ab, `01`, 5)`` returns `"ab0101"`
* ``s_rpad(abc, `01`, 5)`` returns `"abc01"`

----
`s_slice(x: string, sli: slice | int) -> string`

`sli` can be an integer or slice with the same Python slice syntax used to index arrays (see above).

Returns the appropriate slice/index of `x`.

Prior to [v5.5.0](/CHANGELOG.md#550---2023-08-13), Python-style negative indices were not allowed for the `sli` argument.

----
``s_split(x: string, sep: regex | string=g`\s+`) -> array[string]``

If `sep` is not specified (the function is called with one argument):
* Returns `x` split by whitespace.
    * E.g., ``s_split(`a b c\n d `)`` returns `["a", "b", "c", "d", ""]` (the last empty string is because `x` ends with whitespace)
    * The 1-argument option was added in [v6.0](/CHANGELOG.md#600---2023-12-13).

If `sep` is a string (which is treated as a regex) or regex:
* Returns an array containing substrings of `x` where the parts that match `sep` are missing.
   * E.g., ``s_split(`a big bad man`, g`\\s+`)`` returns `["a", "big", "bad", "man"]`.
* However, if `sep` contains any capture groups, the capture groups are included in the array.
   * ``s_split(`a big bad man`, g`(\\s+)`)`` returns `["a", " " "big", " ", "bad", " ", "man"]`.
   * ``s_split(`bob num: 111-222-3333, carol num: 123-456-7890`, g`(\\d{3})-(\\d{3}-\\d{4})`)`` returns `["bob num: ", "111", "222-3333", ", carol num: ", "123", "456-7890", ""]`
* See [the docs for C# Regex.Split](https://learn.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.regex.split?view=netframework-4.8#system-text-regularexpressions-regex-split(system-string)) for more info.

----
`s_strip(x: string) -> string`

Strips the whitespace off both ends of x.

----
`s_sub(x: string, to_replace: regex | string, replacement: string | function) -> string`

Replaces all instances of string/regex `to_replace` in `x` with `replacement`.

* If `to_replace` is a string, replaces all instances of `to_replace` with `replacement`. *NOTE: This is a new behavior in [JsonTools 4.10.1](/CHANGELOG.md#4101---2023-03-02). Prior to that, this function treated `to_replace` as a regex no matter what.*
* If `to_replace` is a regex:
    1. if `replacement` is a string, replaces every instance of `to_replace` with the `replacement` string according to  [C# regex substitution syntax](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference#substitutions).
    2. If `replacement` is a function *(which must take an array as input and return a string)*, replaces every instance of `to_replace` with that function called on the array of strings captured by the regex. __New in [v6.0](/CHANGELOG.md#600---2023-12-13).__
        * Within the callback function, you can reference `loop()`, a no-argument function that returns 1 + the number of replacements made so far. 

__Examples:__

* ``s_sub(abbbbbcb, g`b+`, z)`` returns `azcz`.
* ``s_sub(abbbbbc, `b+`, z)`` returns `abbbbbc`, because `b+` is not being matched as a regex. *Prior to version 4.10.1, this would return the same thing as ``s_sub(abbbbbc, g`b+`, z)``.*
* ``s_sub(abbbbbc, b, z)`` returns `azzzzzc`, because every instance of `b` is replaced by `z`.

Consider as input the JSON string version of the following:
```
1. Frank Foomeister
2. Bob Barheim
3. Bill Bazenstein
```
The regex-replace ``s_sub(@, g`^(\d+)\. (\w+)`, @[2] + str(int(@[1]) * loop()))`` would return
```
Frank1 Foomeister
Bob4 Barheim
Bill9 Bazenstein
```
Let's unpack how that worked:
* the regex we're searching for, ``g`^(\d+)\. (\w+)` ``, matches an integer (`(\d+)`, the first capture group) at the start of a line, then `.`, then a space, then a word (`(\w+)`, the second capture group).
* every time the regex is matched, the callback function `@[2] + str(int(@[1]) * loop())` is invoked on an array containing `[the captured string, the first capture group, the second capture group]`.
* This concatenates the second capture group to the integer value of the first capture group multiplied by `loop()`, which is `1 + the number of replacements made so far`.
* Thus the callback function returns `Frank` + `1 * 1` when called on the line `1. Frank Foomeister` because the match array is `["1. Frank", "Frank", "1"]`.
* On the second match, `loop()` returns `2`, so we the callback function returns `Bob` + `2 * 2` when invoked on `2. Bob Barheim`.

__Notes on regular expressions in `s_sub`:__

1. *Like the function `s_fa`, `s_sub` uses `^` and `$` to match the start and end of lines*, rather than the start and end of a string. Before [v7.0](/CHANGELOG.md#700---2024-02-09), elsewhere in RemesPath, `^` and `$` would match only at the start and end of a string.
2. *`(INT)` and `(NUMBER)` match integers and floating point decimals, respectively, just as in `s_fa` above.* `(?:INT)` and `(?:NUMBER)` are non-capturing versions of the same regular expressions.

----
`s_upper(x: string) -> string`

Returns the upper-case form of x.

----
`str(x: anything) -> string`

Returns the string representation of `x`, unless `x` is a string, in which case it returns a copy of `x`.

---
`zfill(x: anything, padToLen: int) -> string`

*Added in [v6.1](/CHANGELOG.md#610---2023-12-28)*

Returns a string that contains `x` (or the string representation of `x`, if not a string)
padded on the left with enough repetitions of the `0` character to make a composite string with length `padToLen`

__EXAMPLES:__
* `zfill(10, 5)` returns `"00010"`
* `zfill(ab, 4)` returns `"00ab"`

## Projections ##

A __projection__ (a concept from JMESpath) is a subquery that is somehow based on the current JSON. Projections can be used to reshape JSON, capture summaries, and much more.

In RemesPath, a projection can be created by following a valid RemesPath query with:
* a comma-separated list of elements enclosed by `{}` (curly braces) produces an *array*
* a comma-separated list of key-value pairs enclosed by `{}` produces an *object*
* `->` followed by any valid RemesPath expression (with some restrictions; try wrapping it in parentheses if it can't be parsed) can produce *any type* (*introduced in [v5.6](/CHANGELOG.md#560---2023-08-18)*)

For example, suppose you have an array of arrays of numbers.
```json
[
    [1, 2, 3, 4],
    [5, 6],
    [7, 8, 9]
]
```
You might be interested in getting a list of the length of the array, the length of the first element of the array, and the length of the last element.

```
@{
    len(@), 
    len(@[0]), 
    len(@[-1])
}
``` 
returns 
```json
[3, 4, 3]
```

Or maybe you want to know the sum and average of each subarray.

`@[:]{sum(@), avg(@)}` returns

```json
[[10.0, 2.5],
 [11.0, 5.5],
 [24.0, 8.0]]
```

Or maybe you prefer to get that information as an object, so that the reader can more easily figure out what each row is.

`@[:]{row_sum: sum(@), row_avg: avg(@)}` returns

```json
[
    {"row_avg": 2.5, "row_sum": 10.0},
    {"row_avg": 5.5, "row_sum": 11.0}, 
    {"row_avg": 8.0, "row_sum": 24.0}
]
```

These projections can themselves be queried, allowing you to do perform some SQL-like analyses of your data.

Suppose you want to know the average and length of the two rows that have the highest average.

```
sort_by(
    @[:]
        {`len`: len(@), `avg`: avg(@)},
    `avg`,
    true
)[:2]
```
returns
```json
[
    {"avg": 8.0, "len": 3},
    {"avg": 5.5, "len": 2}
]
```

Note that in this example, we're using quotes around the key names `avg` and `len` to indicate that they're being used as strings and not function names. Otherwise the parser will get confused.

Finally, we have the aforementioned `->` projections introduced in [v5.6](/CHANGELOG.md#560---2023-08-18).

Projections with `->` are quite simple: `a -> b` returns `b(a)` if `b` is a function, and `b` otherwise.

Thus, still considering the JSON `[[1,2,3,4],[5,6],[7,8,9]]`, the query `@[:]->len(@)->(str(@)*@)` returns 

```json
["4444","22","333"]
```

## f-strings to easily glue together strings and non-strings *(added in v6.1)* ##

Beginning in [v6.1](/CHANGELOG.md#610---2023-12-28), RemesPath supports f-strings, quoted strings preceded by the `f` character that can contain complex expressions inside of curly braces.
These work similarly to f-strings in Python and `$`-strings in C#.

Because curly braces are used to wrap expressions in the f-string, __you need to use `}}` to get a single literal `}` character, and `{{` to get a single literal `{` character in an f-string.__

For example, consider the input
```json
[
    {"a": "foo", "b": -5.5},
    {"a": "bar", "b": 7},
    {"c": ["y", -1, null]}
]
```

__Examples:__
* The query `` f`first a = {@[0][a]}. Is first b less than second b? {@[0].b < @[1].b}! Show me third c: {@[2].c}` `` will return
    ```json
    "first a = foo. Is first b less than second b? true! Show me third c: [\"y\", -1, null]"
    ```
* The query `` f`sum of b's, wrapped in curlybraces = {{ {sum(@[:].b)} }}` `` will return `"sum of b's, wrapped in curlybraces = { 1.5 }"` because we needed to use double curlybraces to get literal curlybrace characters.

__Notes:__
* f-strings use the [`s_cat` function](#non-vectorized-functions) under the hood to concatenate all the parts of the f-string together. This means that it may be possible to get an error message that references the `s_cat` function in an expression that uses f-strings but does not explicitly call `s_cat`.

## Editing with assignment expressions ##

*Added in version v2.0.0*

A RemesPath query can contain at most one `=` separating two valid expressions. This is the __assignment operator__.

The LHS of the assignment expression is typically a query that selects items from a document (e.g., `@.foo[@ > 0]`).

The RHS is typically a scalar (if you want to give everything queried the same value) or a function like `@ + 1`.

If the RHS is a function and the LHS is an iterable, the RHS is applied separately to each element of the iterable.

### Limitations ###
1. Until further notice, you __cannot__ mutate an object or array, other than to change its scalar elements
    * For example, the query `@ = len(@)` on JSON `[[1, 2, 3]]` will fail, because this ends up trying to mutate the subarray `[1, 2, 3]`.
2. You also cannot mutate a non-array or non-object into an array or object. For example, the query ``@[0] = j`[1]` ``on the input `[0]` will fail because you're trying to convert a scalar (the integer `0`) to an array (`[1]`).

An assignment expression mutates the input and then returns the input.

In these examples, we'll use the input
```json
{
    "foo": [-1, 2, 3],
    "bar": "abc",
    "baz": "de"
}
```

Some examples:
* The query `@.foo[@ < 0] = @ + 1` will yield `{"foo": [0, 2, 3], "bar": "abc", "baz": "de"}`
* The query `@.bar = s_slice(@, :2)` will yield `{"foo": [-1, 2, 3], "bar": "ab", "baz": "de"}`
* The query ``@.g`b` = s_len(@)`` will yield `{"foo": [-1, 2, 3], "bar": 3, "baz": 2}`

## Assigning variables *(added in v5.7)* and executing multi-statement queries ##

Beginning in [v5.7](/CHANGELOG.md#570---2023-09-08), it is possible to run queries with multiple statements. __Each statement in the query must be terminated by a semicolon (`;`), except the final statement.__

In addition, you can assign variables using the syntax `var <name> = <statement>`

Let's see how this works in practice with the following multi-statement query.

```
var a = 1;
var b = @[0];
var c = a + 2;
b = @ * c;
@[:][1]
```

If we run this query on the JSON 
```json
[
    [-1, 1],
    [1, 2]
]
```
here's what will happen:
1. The variable `a` will be set to value __`a = 1`__.
2. The variable `b` will be set to `@[0]`, i.e., the first element of the input array. So __`b = [-1, 1]`__
3. The variable `c` will be set to value `a + 2`, which is just __`c = 3`__.
4. The variable `b` will be mutated by multiplying every element by `c`, because *a statement of the form `<LHS> = <RHS>` is an [assignment expression](#editing-with-assignment-expressions) as described above unless it is preceded by the `var` keyword.*
    * Since this is an assignment expression, __b has been changed to `b = [-3, 3]`, and the input JSON has also been changed!__
5. Finally, we run the query `@[:][1]` on the input JSON. This gets the second element of every sub-array in the input JSON.
    * Since the previous statement `b = @ * c` changed the input to `[[-3, 3], [1, 2]]`, __the final result is:__
```json
[3, 1]
```

While this toy example doesn't fully showcase the utility of variable assignment, it should be obvious that this is a significant improvement in the expressive power of RemesPath.

Some notes:
* *All variables are always passed by reference in RemesPath, not by value.*
    * For example, the statement `var x = 1; var y = x; y = @ + 1; x` will return `2` because *the statement `var y = x;` turns `y` into a reference to `x`*, and the mutation `y = @ + 1` will also change `x`.
    * However, *defining a variable as a non-identity function of another variable copies the first variable.* Thus, `var x = 1; var y = x + 0; y = @ + 1` will not affect `x` because `var y = x + 0;` creates a copy that is the result of adding 0 to x.
* Variables can have the same name as [functions](#functions), because an unquoted string is only interpreted as the name of a function if it is immediately followed by an open parenthesis.
    * For example, the query `var ifelse = blah; var s_len = s_len(ifelse); ifelse(s_len < 3, foo, bar)` is actually perfectly legal, even though it declares two variables that have the same name as functions that are also used in it.
    * Obviously this behavior will no longer be sustainable if it becomes possible to pass functions as arguments to other functions in RemesPath, but that may never happen.
* The tree view (as well as the "query result" JSON that can be converted to a CSV or pasted in a new document)
* To redefine the value of a variable named `a`, just use `var a = <whatever>` to redefine it. This is acceptable because, as noted above, the statement `a = <whatever>` is already reserved for [assignment expressions](#editing-with-assignment-expressions).
* For example, given the input `{bar: "bar", baz: "baz"}`, the query
```
var bar = @.bar;
var baz = @.baz;
var barbaz = bar + baz;
var baz = @{bar, baz, barbaz};
baz
```
will return
```json
["bar", "baz", "barbaz"]
```
because when baz is redefined, it just uses the value of baz that was previously defined, and no weird infinite loops of self-reference will happen.

## For loops/Loop variables *(added in v6.0)* ##

Beginning in [v6.0](/CHANGELOG.md#600---2023-12-13), you can loop over an array by assigning a variable to the array with the `for` keyword rather than the `var` keyword.

When you assign a variable `x` to an array with the `for` keyword, here is what happens:
```
toLoopOver = x
start of loop = statement after assignment of x
end of loop = end of query OR next instance of "end for;" statement
for each value of toLoopOver:
    x = value
    execute each statement between start of loop and end of loop
```

### Notes on loop variables ###

1. If the last statement of a query is `end for;`, or if a `for` loop is not closed, the value returned by the query (and thus the value that the tree view will be populated with) is *the array that was looped over.* For example, the return value of the query ``for x = j`[1, 2, 3]`; x = @ + 1; end for;`` is `[2, 3, 4]`, since we added 1 to every value in the array.
2. As in Python, a loop variable persists after a for loop is finished. Thus ``for x = j`[1, 2];` end for; x`` returns `2`, since that was the last value in the array `[1, 2]` that was looped through. 

Let's see an example of loop variables on this JSON:
```json
{
    "a" : [1, 2, 3],
    "b": ["a", "bb", "c"]
}
```
and this query:
```
var a = @.a;
var b = @.b;
var b_maxlen = ``;
for i = range(len(a));
    var bval = at(b, i);
    bval = @ * at(a, i);
    var b_maxlen = ifelse(s_len(bval) > s_len(b_maxlen), bval, b_maxlen);
end for;
b_maxlen;
```
The query *will return `"bbbb"`*
and *will mutate the JSON to*
```json
{
	"a": [1, 2, 3],
	"b": ["a", "bbbb", "ccc"]
}
```

Here's how the query is executed:
1. Set the variable `a` to `[3, 2, 1]` (which is `@.a`)
2. Set the variable `b` to `["a", "bb", "c"]` (statement `@.b`)
3. Set the variable `b_maxlen` to an *initial value of `""`* (statement ```var b_maxlen = ``;```)
4. For each index `i` of array `a` (statement `for i = range(len(a));`):
    1. First, mutate the current element of `b` by multiplying it by the corresponding element of `a` (statements `var bval = at(b, i);` and `bval = @ * at(a, i)`)
    2. Now check if the current element of `b` is longer than `b_maxlen`. If it is, reassign `b_maxlen` to the current element of `b` (statement `var b_maxlen = ifelse(s_len(bval) > s_len(b_maxlen), bval, b_maxlen);`)
5. Return the current value of `b_maxlen`,  which is `"bbbb"` because that's the longest string in the JSON after the transformation.

## Spreading function args to fill multiple arguments (added in v5.8) ##

Beginning in [v5.8](/CHANGELOG.md#580---2023-10-09), the `*` spread operator has been added that allows the user to pass in an array to stand for multiple arguments, which RemesPath will attempt to get from the elements of that array.


__Examples:__
* ``zip(*j`[[1, 2], ["a", "b"], [true, false]]`)`` returns `[[1, "a", true], [2, "b", false]]`
* Consider the input
```json
{
    "a": [[[1, 0], [0, 1]], 1],
    "b": [[[1, 0], [0, 1]], 0]
}
```
The query `@.*->max_by(*@)` returns `{"a": [0, 1], "b": [1, 0]}` because when working with key `a`, we max by the second element of each subarray, and when working with key `b`, we max by the first element.

__Notes:__
* *Only the final argument to a function can be spread.* For example, ``zip(*j`[1, 2]`, j`[3, 4]`)`` is not a legal query because a non-final argument was spread.

## Omitting optional function arguments before the final argument (added in [v6.0](/CHANGELOG.md#600---2023-12-13)) ##

Beginning in [v6.0](/CHANGELOG.md#600---2023-12-13), if a function has multiple optional arguments, you can leave any number of optional arguments (including the last) empty, rather than writing `null`.
For example, if the function `foo` has two optional arguments:
* `foo(1, , 2)` would be equivalent to `foo(1, null, 2)`
* `foo(1, 2, )` would be equivalent to `foo(1, 2, null)` or `foo(1, 2)`.

## Comments (added in [v7.0](/CHANGELOG.md#700---2024-02-09)) ##

Beginning in [v7.0](/CHANGELOG.md#700---2024-02-09), queries can include any number of Python-style single-line comments.

Thus the query
```
foo # comment1
+ #comment2
# comment3
bar #comment4
```
would simply be parsed as `foo + bar`