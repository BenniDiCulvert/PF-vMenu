# AGENTS

## Carbon Mile specific functionality:

- If you're not sure whether a requested feature is Carbon Mile specific, ask the user first.
- vMenu should remain a menu that's usable on any server that wants its functionality.
    As such, where possible and reasonable, suggest more generic ways to implement an otherwise Carbon Mile specific feature. For example, instead of the following
    ```c#
    TriggerServerEvent("vMenu:RequestSponsorPlate"); // Request the player's sponsor plate from the server and sets it through "vMenu:SetSponsorPlate" iff the player is allowed to do so
    EventHandlers.Add("vMenu:SetSponsorPlate", ...); // Sets the player's sponsor plate (only called if player is sponsor)
    ```
    suggest
    ```c#
    TriggerServerEvent("vMenu:RequestDefaultPlate"); // Request the player's default plate from the server
    EventHandlers.Add("vMenu:SetDefaultPlate", ...); // Sets the player's default plate
    ```
    as vMenu has no concept of paid sponsors.
    In the second version it is implicitly up to the server to just filter out players without the proper permission and do nothing or just provide a plate style that indicates that no plate is set.

    vMenu now also has a more flexible permission system (see `vMenuServer/hooks/playerPermissions.lua`) that can query permissions dynamically from the server when a player logs in.
    So the above could also be integrated into the normal vMenu permissions setup by introducing a new vMenu permission that controls (a) whether a new vMenu setting (e.g. in the Spawn Options) is shown to configure and enable/disable the default plate, and (b) whether this plate is applied to newly spawned vehicles.
    The `"vMenu:SetDefaultPlate"` handler would then simply exist to be able to also change the configured default plate from other resources.
- Also make the implementation code modular.
    That is, if you introduce some new helpers when implementing a feature, try to make them generic and not related to anything Carbon Mile specific so that other or future vMenu code can still use it.
- Anything that cannot be reasonably generalized further and is Carbon Mile specific *must* be wrapped inside
    ```c#
    #if CARBON_MILE
    // ... code goes here ...
    #endif
    ```
    and the code must still compile when `CARBON_MILE` is not set.
    Functions and members inside such blocks should have descriptive, Carbon Mile specific names.
    If the name is too generic and does not involve Carbon Mile in any way but the functionality only makes sense for Carbon Mile, prefix the name with `CarbonMile`.

    But try to minimize the amount of code inside such a preprocessor block by making the implementation as modular as (reasonably) possible. For example by extracting out or extending already existing, more generic helper functions that could be used by new or are already used by existing vMenu features.
