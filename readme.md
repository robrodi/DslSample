#HeskyScript
by robrodi

## Summary
A DSL in the DLR to route events into a couple of buckets.

## Notes
*   Not case sensitive
*   Tabs for grouping

## Goals

### Sample 1: Id equality w/ rewards (done)
    when id is 5 add cookie
    when id is 6 add 2 cookies
    when id is 7 add widget

### Sample 2: Compound Queries & GreaterThan / Less Than  (done)
    when id is 5 and count GreaterThan 4 add cookie
    when id is 6 and count is 3 add 2 cookies

### Sample 3: Input + event conditions (done)
    when mode is Alpha and count GreaterThan 4 add cookie
    when Variant is foxtrot and count is 3 add 2 cookies

### Sample 4: Everyone likes comments(done)
    when id is 5 add cookie
    # I'm a comment
    when id is 7 add widget

    # I'm a comment preceded by a newline!
    when id is 6 add 2 cookies

    // I'm a comment too.
    when id is 9 add 2 cookies    

### Sample X
    when mode is charlie and variant is foxtrot
        when id is 3 add 4 spacebucks and 100 cookies
        when id is 5 add cookie
        when id is 6 add 2 cookies
        when id is 7 add widget

## TODO:
* Make the lookup on input runtime rather than CompileTime? 
* I like the idea of tabbing, but then things get weird