#HeskyScript

## Summary
A DSL in the DLR to route events into a couple of buckets.

## Notes
*   Not case sensitive
*   Tabs for grouping

## Goals

### Sample 1 (done)
    when id is 5 add cookie
    when id is 6 add 2 cookies
    when id is 7 add widget

### Sample 2 (done)
    when id is 5 and count GreaterThan 4 add cookie
    when id is 6 and count is 3 add 2 cookies

### Sample 3 (done)
    when mode is Alpha and count GreaterThan 4 add cookie
    when Variant is foxtrot and count is 3 add 2 cookies
    
### Sample 4
    when mode is charlie and variant is foxtrot
        when id is 3 add 4 spacebucks and 100 cookies
        when id is 5 add cookie
        when id is 6 add 2 cookies
        when id is 7 add widget
