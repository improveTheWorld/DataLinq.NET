# DataLinq.NET — API Reference

This document covers the **public API surface** of DataLinq.NET: the classes and methods you use directly when building data pipelines.

> For the complete extension method matrix across all four paradigms (sync, async, parallel-sync, parallel-async), see the [Extension Methods API Reference →](Extension-Methods-API-Reference.md).

## Table of Contents

1. [Read Class](#read-class)
2. [Writers Class](#writers-class)
3. [IEnumerable Extensions](#ienumerable-extensions)
4. [String Extensions](#string-extensions)
5. [Extension Method Quick Reference](#extension-method-quick-reference)

---

## Read Class

Static class providing streaming data readers with lazy evaluation.

```csharp
public static IEnumerable<string> text(StreamReader file, bool autoClose = true)
```
**Description:** Reads lines from a StreamReader  
**Parameters:**
- `file`: StreamReader instance to read from
- `autoClose`: Whether to automatically close the stream (default: true)  
**Returns:** `IEnumerable<string>` — Lazy enumerable of lines  
**Example:**
```csharp
using var reader = new StreamReader("data.txt");
var lines = Read.text(reader);
```

```csharp
public static IEnumerable<string> text(string path, bool autoClose = true)
```
**Description:** Reads lines from a text file  
**Parameters:**
- `path`: File path to read from
- `autoClose`: Whether to automatically close the file stream (default: true)  
**Returns:** `IEnumerable<string>` — Lazy enumerable of file lines  
**Example:**
```csharp
var lines = Read.text("data.txt");
```

```csharp
public static IEnumerable<T?> csv<T>(string path, string separator = ",", bool autoClose = true, params string[] schema)
```
**Description:** Reads and parses CSV files into strongly-typed objects  
**Parameters:**
- `path`: CSV file path
- `separator`: Field separator (default: `","`)
- `autoClose`: Whether to automatically close the file (default: true)
- `schema`: Optional custom field schema (uses file header if not provided)  
**Returns:** `IEnumerable<T?>` — Lazy enumerable of parsed objects  
**Example:**
```csharp
var employees = Read.csv<Employee>("employees.csv", ",");
```

---

## Writers Class

Static class providing extension methods for writing data to various formats.

```csharp
public static void WriteText(this IEnumerable<string> lines, StreamWriter file)
```
**Description:** Writes string enumerable to a StreamWriter  
**Parameters:**
- `lines`: Enumerable of strings to write
- `file`: StreamWriter instance  
**Example:**
```csharp
lines.WriteText(writer);
```

```csharp
public static void WriteText(this IEnumerable<string> lines, string path, bool autoFlash = true)
```
**Description:** Writes string enumerable to a text file  
**Parameters:**
- `lines`: Enumerable of strings to write
- `path`: Output file path
- `autoFlash`: Whether to auto-flush writes (default: true)  
**Example:**
```csharp
processedLines.WriteText("output.txt");
```

```csharp
public static void WriteCSV<T>(this IEnumerable<T> records, StreamWriter file, bool withTitle = true, string separator = ",") where T : struct
```
**Description:** Writes strongly-typed objects to CSV format using StreamWriter  
**Type Constraints:** `where T : struct`  
**Parameters:**
- `records`: Enumerable of objects to write
- `file`: StreamWriter instance
- `withTitle`: Whether to include header row (default: true)
- `separator`: Field separator (default: `","`)  
**Example:**
```csharp
products.WriteCSV(writer, withTitle: true, separator: ",");
```

```csharp
public static void WriteCSV<T>(this IEnumerable<T> records, string path, bool withTitle = true, string separator = ",") where T : struct
```
**Description:** Writes strongly-typed objects to CSV file  
**Type Constraints:** `where T : struct`  
**Parameters:**
- `records`: Enumerable of objects to write
- `path`: Output CSV file path
- `withTitle`: Whether to include header row (default: true)
- `separator`: Field separator (default: `","`)  
**Example:**
```csharp
products.WriteCSV("products.csv", withTitle: true, separator: ",");
```

---

## IEnumerable Extensions

Comprehensive extension methods for `IEnumerable<T>` manipulation.

### Control Flow

> **Note:** All `Until` overloads use **inclusive** stop semantics — the item that triggers the condition is **included** in the output. This is the opposite of LINQ's `TakeWhile` (which is exclusive).

```csharp
public static IEnumerable<T> Until<T>(this IEnumerable<T> items, Func<bool> stopCondition)
```
**Description:** Processes items until a global condition becomes true  
**Parameters:**
- `items`: Source enumerable
- `stopCondition`: Function that returns true when processing should stop  
**Returns:** `IEnumerable<T>` — Items up to and including the one where the condition became true  

```csharp
public static IEnumerable<T> Until<T>(this IEnumerable<T> items, Func<T, bool> stopCondition)
```
**Description:** Processes items until an item-specific condition is met (inclusive)  
**Parameters:**
- `items`: Source enumerable
- `stopCondition`: Function that evaluates each item  

```csharp
public static IEnumerable<T> Until<T>(this IEnumerable<T> items, Func<T, int, bool> stopCondition)
```
**Description:** Processes items until a condition involving the item and its index is met (inclusive)  
**Parameters:**
- `items`: Source enumerable
- `stopCondition`: Function that evaluates each item and its index  

```csharp
public static IEnumerable<T> Until<T>(this IEnumerable<T> items, int lastItemIdx)
```
**Description:** Processes items up to and including a specific index  
**Parameters:**
- `items`: Source enumerable
- `lastItemIdx`: Last index to process (inclusive)  

### Actions

```csharp
public static IEnumerable<T> ForEach<T>(this IEnumerable<T> items, Action<T> action)
```
**Description:** Executes an action for each item while maintaining the enumerable chain  
**Parameters:**
- `items`: Source enumerable
- `action`: Action to execute for each item  
**Returns:** `IEnumerable<T>` — Original enumerable for chaining  

```csharp
public static IEnumerable<T> ForEach<T>(this IEnumerable<T> items, Action<T, int> action)
```
**Description:** Executes an action with access to both item and index  
**Parameters:**
- `items`: Source enumerable
- `action`: Action to execute with item and index  

```csharp
public static void Do<T>(this IEnumerable<T> items)
```
**Description:** Forces enumeration of the sequence without returning values  
**Parameters:**
- `items`: Source enumerable  

```csharp
public static void Do<T>(this IEnumerable<T> items, Action action)
```
**Description:** Executes an action for each enumeration step  
**Parameters:**
- `items`: Source enumerable
- `action`: Action to execute during enumeration  

### Cases Pattern

```csharp
public static IEnumerable<(int category, T item)> Cases<T>(this IEnumerable<T> items, params Func<T, bool>[] filters)
```
**Description:** Categorizes items based on predicates  
**Parameters:**
- `items`: Source enumerable
- `filters`: Array of predicate functions for categorization  
**Returns:** `IEnumerable<(int category, T item)>` — Items tagged with category indices  

```csharp
public static IEnumerable<(int category, T item, R newItem)> SelectCase<T, R>(this IEnumerable<(int category, T item)> items, params Func<T, R>[] selectors)
```
**Description:** Applies different transformations based on category  
**Parameters:**
- `items`: Categorized enumerable
- `selectors`: Array of transformation functions for each category  
**Returns:** `IEnumerable<(int category, T item, R newItem)>` — Items with transformations  

```csharp
public static IEnumerable<(int category, T item, R newItem)> ForEachCase<T, R>(this IEnumerable<(int category, T item, R newItem)> items, params Action<R>[] actions)
```
**Description:** Executes different actions based on category  
**Parameters:**
- `items`: Categorized enumerable with transformations
- `actions`: Array of actions for each category  
**Returns:** Same enumerable for chaining  

```csharp
public static IEnumerable<R> AllCases<T, R>(this IEnumerable<(int category, T item, R newItem)> items)
```
**Description:** Extracts transformed items from categorized enumerable  
**Parameters:**
- `items`: Categorized enumerable with transformations  
**Returns:** `IEnumerable<R>` — Transformed items only  

### Aggregation

```csharp
public static T Cumul<T>(this IEnumerable<T> sequence, Func<T?, T, T> cumulate)
```
**Description:** Performs cumulative operations on a sequence  
**Parameters:**
- `sequence`: Source enumerable
- `cumulate`: Function to combine accumulator with current item  
**Returns:** `T` — Final accumulated result  

```csharp
public static TResult Cumul<T, TResult>(this IEnumerable<T> sequence, Func<TResult?, T, TResult> cumulate, TResult? initial)
```
**Description:** Performs cumulative operations with an initial value  
**Parameters:**
- `sequence`: Source enumerable
- `cumulate`: Function to combine accumulator with current item
- `initial`: Initial accumulator value  
**Returns:** `TResult` — Final accumulated result  

```csharp
public static dynamic Sum<T>(this IEnumerable<T> items)
```
**Description:** Generic sum operation using dynamic typing  
**Parameters:**
- `items`: Source enumerable of numeric values  
**Returns:** `dynamic` — Sum of all items  

### Utilities

```csharp
public static IEnumerable<T> MergeOrdered<T>(this IEnumerable<T> first, IEnumerable<T> second, Func<T, T, bool> isFirstLessThanOrEqualToSecond)
```
**Description:** Merges two ordered enumerables into a single ordered enumerable  
**Parameters:**
- `first`: First ordered enumerable
- `second`: Second ordered enumerable
- `isFirstLessThanOrEqualToSecond`: Comparison function  
**Returns:** `IEnumerable<T>` — Merged ordered enumerable  

```csharp
public static IEnumerable<T> Take<T>(this IEnumerable<T> sequence, int start, int count)
```
**Description:** Takes a specific range of items  
**Parameters:**
- `sequence`: Source enumerable
- `start`: Starting index
- `count`: Number of items to take  
**Returns:** `IEnumerable<T>` — Specified range of items  

```csharp
public static bool IsNullOrEmpty<T>(this IEnumerable<T> sequence)
```
**Description:** Checks if enumerable is null or empty  
**Parameters:**
- `sequence`: Enumerable to check  
**Returns:** `bool` — True if null or empty  

### String Building

```csharp
public static string BuildString(this IEnumerable<string> items, StringBuilder str = null, string separator = ", ", string before = "{", string after = "}")
```
**Description:** Builds formatted strings from string enumerables  
**Parameters:**
- `items`: Source string enumerable
- `str`: Optional StringBuilder instance
- `separator`: Separator between items (default: `", "`)
- `before`: Prefix string (default: `"{"`)
- `after`: Suffix string (default: `"}"`)  
**Returns:** `string` — Formatted string  

---

## String Extensions

Extension methods for string manipulation and validation.

### Validation

```csharp
public static bool IsNullOrEmpty(this string text)
```
**Description:** Checks if string is null or empty  
**Returns:** `bool`  

```csharp
public static bool IsNullOrWhiteSpace(this string text)
```
**Description:** Checks if string is null, empty, or contains only whitespace  
**Returns:** `bool`  

```csharp
public static bool IsBetween(this string text, string start, string end)
```
**Description:** Checks if string starts with one delimiter and ends with another  
**Returns:** `bool`  

### Content Analysis

```csharp
public static bool StartsWith(this string value, IEnumerable<string> acceptedStarts)
```
**Description:** Checks if string starts with any of the provided prefixes  
**Returns:** `bool`  

```csharp
public static bool ContainsAny(this string line, IEnumerable<string> tokens)
```
**Description:** Checks if string contains any of the specified tokens  
**Returns:** `bool`  

### Manipulation

```csharp
public static string ReplaceAt(this string value, int index, int length, string toInsert)
```
**Description:** Replaces substring at specific position  
**Returns:** `string` — Modified string  

```csharp
public static int LastIdx(this string text)
```
**Description:** Gets the last valid index of the string  
**Returns:** `int` — Last valid index (Length - 1)  

---

## Extension Method Quick Reference

### IEnumerable\<T\> Extensions

| Method | Description | Returns |
|--------|-------------|---------|
| `Until(condition)` | Process until condition | `IEnumerable<T>` |
| `ForEach(action)` | Execute action for each | `IEnumerable<T>` |
| `Cases(filters)` | Categorize items | `IEnumerable<(int, T)>` |
| `SelectCase(selectors)` | Transform by category | `IEnumerable<(int, T, R)>` |
| `ForEachCase(actions)` | Execute by category | `IEnumerable<(int, T, R)>` |
| `AllCases()` | Extract transformations | `IEnumerable<R>` |
| `Cumul(function)` | Cumulative operation | `T` |
| `Sum()` | Generic sum | `dynamic` |
| `MergeOrdered(other, comparer)` | Merge ordered sequences | `IEnumerable<T>` |
| `IsNullOrEmpty()` | Check if null/empty | `bool` |
| `BuildString(options)` | Build formatted string | `string` |
| `Flat()` | Flatten nested enumerables | `IEnumerable<T>` |
| `Spy(tag, display)` | Debug enumerable | `IEnumerable<T>` |
| `Display(tag)` | Output to console | `IEnumerable<T>` |

### String Extensions

| Method | Description | Returns |
|--------|-------------|---------|
| `IsNullOrEmpty()` | Check if null/empty | `bool` |
| `IsNullOrWhiteSpace()` | Check if null/whitespace | `bool` |
| `IsBetween(start, end)` | Check delimiters | `bool` |
| `StartsWith(prefixes)` | Check multiple prefixes | `bool` |
| `ContainsAny(tokens)` | Check for any token | `bool` |
| `ReplaceAt(index, length, text)` | Replace at position | `string` |
| `LastIdx()` | Get last index | `int` |

### Data Writing Extensions

| Method | Description | Returns |
|--------|-------------|---------|
| `WriteText(path, autoFlush)` | Write text lines | `void` |
| `WriteCSV(path, title, separator)` | Write CSV data | `void` |
