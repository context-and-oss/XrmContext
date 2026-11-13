# Migration

Migration from XrmContext main to XrmContext rewrite will require the following manual updates due to lack of backwards compatibility.

1. Local Option Sets
2. Primary Identifier Column
3. Option Set Name Collisions

## 1. Local Option Sets

Local Option Sets are generated using the option set name instead of the column name.
This is especially relevant for statecode and statuscode, which will change from e.g. "AccountState"/"Account_StatusCode" to "account_statecode"/"account_statuscode".

This change aligns the context of local options sets with global option sets.

## 2 Primary Identifier Column

The Primary Identifier Column generally generates context in two formats. Account Example:

``` CSharp
public Guid Id { ... }
public Guid? AccountId { ...}
```

Although `Id` uses the same underlying column data as `AccountId` the generated type has differed.
Going forward the typing will therefore become:

``` CSharp
public Guid Id { ... }
public Guid AccountId { ...}
```

## 3. Option Set Name Collisions

As in XrmContext main, an option set enum whose name collides with a table gets the suffix `_Enum`.
The comparison is now case-insensitive, so e.g. a table `prefix_Ticket` and an option set `prefix_ticket` now also generate `prefix_ticket_Enum`.
If the suffixed name is also taken, a number is added (`_Enum2`, `_Enum3`, ...).
