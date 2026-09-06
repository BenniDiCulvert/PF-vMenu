--- PlayerPermissions: Per-player permission overrides.
---
--- This hook allows granting or denying permissions for a specific player. Additionally supports granting custom
--- permissions for vMenu features that can work with them.

--- Player handle (as a string)
--- @alias PlayerSrc string

--- Either the name of an ACE (as specified in permissions.cfg) for a vMenu built-in permission, or a custom permission
--- key.
--- @alias PermissionString string

--- A table mapping built-in or custom permissions to whether they are allowed or not.
--- @alias PlayerPermissions table<PermissionString, boolean>

--- @param playerSrc PlayerSrc
local function getLicense(playerSrc)
    return GetPlayerIdentifierByType(playerSrc, "license")
end

---@param playerSrc PlayerSrc
---@return boolean
local function isAdmin(playerSrc)
    return IsPlayerAceAllowed(playerSrc, "vMenu.IsAdmin")
end

---@param playerSrc PlayerSrc
---@return boolean
local function isModerator(playerSrc)
    return IsPlayerAceAllowed(playerSrc, "vMenu.IsModerator")
end

PlayerPermissionsHooks = {

    --- Fetch permissions for the given player. Built-in permissions returned here will override the value specified for
    --- them ACE in permissions.cfg. Custom permissions are denied by default and can only be allowed through this hook.
    --- @param playerSrc PlayerSrc
    --- @return PlayerPermissions
    fetchFor = function(playerSrc)
        -- DO NOT CHANGE THE NAME OF THIS FUNCTION
        -- THE RETURN TYPE MUST FOLLOW THE SPECIFICATION

        -- The __dummy field exists solely so we encode the returned value as a JSON object instead of an array.
        local dummy = { __dummy = false }

        local license = getLicense(playerSrc)

        -- TODO :)

        return dummy

        --[[
        Example:

        return {
            -- vMenu built-in permissions use their full ACE permission names.
            ["vMenu.VehicleOptions.Repair"] = true,
            ["vMenu.OnlinePlayers.Kick"] = false,

            -- Non vMenu built-in permissions. Specifying 'false' permission here really does nothing, as these are
            -- disabled by default anyways and can only be explicitly allowed through this hook.
            ["this.permission"] = true,
            ["that.other.permission"] = false,
        }
        --]]
    end
}
