﻿module ``Custom assertions``

open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open Faqt
open AssertionHelpers
open type AssertionHelpers
open Xunit


[<Extension>]
type private Assertions =


    [<Extension>]
    static member DelegatingFail
        (
            t: Testable<'a>,
            [<CallerFilePath; Optional; DefaultParameterValue("")>] fn,
            [<CallerLineNumber; Optional; DefaultParameterValue(0)>] lno,
            ?methodNameOverride
        ) : And<'a> =
        t.Subject
            .Should()
            .Fail(fn, lno, defaultArg methodNameOverride (nameof Assertions.DelegatingFail))


    [<Extension>]
    static member NotInvade
        (
            t: Testable<string>,
            target: string,
            [<Optional; DefaultParameterValue("")>] because,
            [<CallerFilePath; Optional; DefaultParameterValue("")>] fn,
            [<CallerLineNumber; Optional; DefaultParameterValue(0)>] lno,
            ?methodNameOverride
        ) : And<string> =
        if t.Subject = "Russia" && target = "Ukraine" then
            fail
                $"\tExpected\n{sub (fn, lno, methodNameOverride)}\n\tto not invade\n{fmt target}\n\t{bcc because}but an invasion was found to be taking place by\n{fmt t.Subject}"

        And(t)


[<Fact>]
let ``DelegatingFail gives expected subject name`` () =
    fun () -> "asd".Should().DelegatingFail()
    |> assertExnMsg "\"asd\""


[<Fact>]
let ``Custom assertion gives expected message`` () =
    fun () ->
        let country = "Russia"
        country.Should().NotInvade("Ukraine")
    |> assertExnMsg
        """
    Expected
country
    to not invade
"Ukraine"
    but an invasion was found to be taking place by
"Russia"
"""
