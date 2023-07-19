Faqt
====

**Fantastic fluent assertions your F# tests and domain pre-/post-conditions/invariants.**

<img src="https://raw.githubusercontent.com/cmeeren/Faqt/main/logo/faqt-logo-docs.png" width="300" align="right" />

Faqt improves on the best of [FluentAssertions](https://github.com/fluentassertions/fluentassertions)
and [Shouldly](https://github.com/shouldly/shouldly) and serves it steaming hot on a silver platter
to the discerning F# developer. **It aims to the best assertion library for F#.** If you don't agree, I consider that a
bug - please raise an issue.

### Work in progress, 1.0 to be released late August 2023

Faqt is currently a work in progress. All "infrastructure" and supporting features are in place; most of the actual
assertions remain to be implemented. A feature complete 1.0 version will hopefully be released during August 2023.

## A motivating example

Here is an example of what you can do with Faqt. Simply use `Should()` to start asserting, whether in a unit test or for
validating preconditions etc. in domain code (demonstrated below). Like FluentAssertions, all assertions support an
optional "because" parameter that will be used in the output.

```f#
type Customer =
    | Internal of {| ContactInfo: {| Name: {| LastName: string |} |} option |}
    | External of {| Id: int |}

let calculateFreeShipping customer =
    customer
        .Should()
        .BeOfCase(Internal, "this function should only be called with internal customers")
        .Whose.ContactInfo.Should()
        .BeSome()
        .Whose.Name.LastName.Should()
        .Be("Armstrong", "only customers named Armstrong get free shipping")
```

(The example is formatted using [Fantomas](https://fsprojects.github.io/fantomas/), which line-breaks fluent chains at
method calls. While the readability of Faqt assertion chains could be slightly improved by manual formatting, entirely
foregoing automatic formatting is not worth the slight benefit to readability.)

Depending on the input, a `Faqt.AssertionFailedException` may be raised with one of these messages:

If customer is `External`:

```
customer
    should be of case
Internal
    because this function should only be called with internal customers, but was
External { Id = 1 }
```

If `ContactInfo` is `None`:

```
customer...ContactInfo
    should be of case
Some
    but was
None
```

If `LastName` is not `Armstrong`:

```
customer...ContactInfo...Name.LastName
    should be
"Armstrong"
    because only customers named Armstrong get free shipping, but was
"Aldrin"
```

As you can see, the first line tells you which part of the code fails (and `...` is used when using derived state from
an assertion).

**Yes, this works even in production, on CI with `DeterministicSourcePaths`, and otherwise when your source files are
not available, as long as you use `<DebugType>embedded</DebugType>` and `<EmbedAllSources>true</EmbedAllSources>`. It's
magic!**

## Faqt in a nutshell

As expected by the discerning F# developer, Faqt is:

- **Readable:** Assertions read like natural language.
- **Concise:** Verbosity in the syntax is kept to an absolute minimum.
- **Usable:** Faqt comes with batteries included, and contains many useful assertions, including aliases
  (like `BeTrue()` for `Be(true)` on booleans, and `BeSome` for `BeOfCase(Some)` on `option` values).
- **Safe:** Assertions are as type-safe as F# allows.
- **Extendable:** No assertion? No problem! Writing your own assertions is very simple (details below).
- **Informative:** The assertion failure messages are designed to give you all the information you need in an
  easy-to-read format.
- **Discoverable:** The fluent syntax means you can just type a dot to discover all possible assertions and actinos on
  the current value.
- **Composable:** You can chain assertions with `And`, `Whose`, `Which`, and `Subject`, assert on derived values like
  with `BeSome()`, split out assertion chains with `Satisfy`, and require one of several sub-assertions
  with `SatisfyAny`.
- **Configurable:** You can configure how values are formatted in the assertion message on a type-by-type basis, and
  specify a default formatter (e.g. for serializing objects to JSON for display).
- **Production-ready:** Faqt is very well tested and will not break your code, whether test or production.

## Writing your own assertions

Writing your own assertions is easy! They are implemented exactly like Faqt’s built-in assertions, so you can always
look at those for inspiration.

Let’s look at the implementation for Faqt’s simplest assertion, `Be`:

```f#
open Faqt
open AssertionHelpers
open Formatting

[<Extension>]
type Assertions =

    [<Extension>]
    static member Be(t: Testable<'a>, expected: 'a, ?because) : And<'a> =
        use _ = t.Assert()

        if t.Subject <> expected then
            Fail(t, because)
                .Throw("{subject}\n\tshould be\n{0}\n\t{because}but was\n{actual}", format expected)

        And(t)
```

Here are the important points. Don’t be discouraged by how detailed the explanation below is; it’s better to explain it
thoroughly once than piecewise here and there.

* Implement the assertion as
  an [extension method](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/type-extensions#extension-methods)
  for `Testable` (the first argument), with whatever constraints you need. The constraints could be implicitly imposed
  by F#, as above where it requires `equality` on `a` due to the use of `<>`, or they could be explicitly specified, for
  example by specifying more concrete types (such as `Testable<'a option>` in order to have your extension only work
  for `option`-wrapped types).

* Accept whichever arguments you need for your assertion, and end with `?because`.

* First in your method, call `use _ = t.Assert()`. This is needed to track important state necessary for subject
  names to work. If your assertion calls user code that is expected to call their own assertions (like is the case
  with `Satisfy`), call `t.Assert(true)` instead.

* If your condition is not met, call

   ```f#
   Fail(t, because).Throw("<message template>", param1, param2, ...)
   ```

* The message template is up you, but for consistency it should ideally adhere to the following conventions:

  * The general structure should be something like “{subject} should … {because}, but …”.
  * Use `{subject}`, `{because}`, and `{actual}` as placeholders for the subject name, the user-supplied reason, and the
    current value being tested (`t.Subject`), respectively. (Not all assertions need `{actual}`.)
  * Use `{0}`, `{1}`, etc. as needed for any values passed as parameters after the template. These parameters must
    be of type `string`; use the `format` function (in the opened `Formatting` module) to format values for display.
  * Don’t use string interpolation to insert values you don’t have control over (for example, values that could contain
    the placeholders mentioned above).
  * Place `{subject}`, `{actual}`, and all other important values on separate lines. All other text should be indented
    using `\t`.
  * Ensure that your message is rendered correctly if `{because}` is replaced with an empty string. Faqt will insert a
    space before `{because}` and/or a comma + space after `{because}` if needed.

* If your assertion extracts derived state that can be used for further assertions,
  return `AndDerived(t, derivedState)`. Otherwise return `And(t)`.

* If your assertion calls `Should` at any point, make sure you pass the original `Testable` as an argument, since it
  contains important state relating to the end user’s original assertion call. For example, the above `BeSome`
  implementation could (somewhat artificially) be implemented like this:

  ```f#
  [<Extension>]
  static member BeSome(t: Testable<'a option>, ?because, ?methodNameOverride) =
      t.Subject.Should(t).BeOfCase((* same as previous example *))
  ```

FAQ
---

### Why is the subject name not correct?

The automatic subject name (the first part of the assertion message) is correct in most situations, but there are edge
cases where it may produce unexpected results:

* The name is incorrect if the assertion chain does not start on a new line or at the start of a lambda (`fun ... ->`).
* Multi-line strings literals will be concatenated.
* Lines starting with `//` in multi-line string literals will be removed.
* Nested `Satisfy` or other assertions that support "child assertions" may give incorrect names.

If you have encountered a case not listed above, please raise an issue. If I can't or won't fix it, I can at the very
least document it as a known limitation.

These limitations are due to the implementation of automatic subject names. It is based on clever use of caller info
attributes, parsing source code from either local files or embedded resources, thread-local state, and simple
regex-based processing/replacement of the call chain based on which assertions have been encountered so far.

If you would like to help make the automatic subject name functionality more robust, please raise an issue.
