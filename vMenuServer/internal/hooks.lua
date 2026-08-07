local hookCallDebugging = GetConvar("vmenu_hook_call_debugging", "false") == "true"
local hookResultDebuggingPattern = GetConvar("vmenu_hook_result_debugging_pattern", "")

local function dumpTable(o)
    if type(o) == 'table' then
        local s = '{ '
        for k, v in pairs(o) do
            if type(k) ~= 'number' then k = '"' .. k .. '"' end
            s = s .. '[' .. k .. '] = ' .. dumpTable(v) .. ','
        end
        return s .. '} '
    else
        return tostring(o)
    end
end

--- @param hookPath string
--- @param type string "enter" or "exit"
local function debugHookCall(hookPath, type)
    if hookCallDebugging then
        print("Hook " .. hookPath .. " -> " .. type)
    end
end

--- @param hookFn fun(table?): table?
--- @param hookName string
--- @param hooksGroup string
local function addHookHandler(hookFn, hooksGroup, hookName)
    local hookPath = hooksGroup .. ":" .. hookName
    local eventName = "vMenu:Hooks:" .. hookPath

    AddEventHandler(eventName, function(requestId, argsJson)
        local args = json.decode(argsJson or "{}")

        Citizen.CreateThread(function()
            debugHookCall(hookPath, "enter")
            local start = GetGameTimer()
            local result = hookFn(args)
            if hookResultDebuggingPattern ~= "" and string.find(hookResultDebuggingPattern, hookPath) then
                print(hookPath .. ":\n" ..
                    "  Args: " .. dumpTable(args) .. "\n" ..
                    "  Result: " .. dumpTable(result))
            end
            local timeStr = string.format("%.2f", (GetGameTimer() - start) / 1000.0)
            debugHookCall(hookPath, "exit (" .. timeStr .. " ms)")
            TriggerEvent("vMenu:RequestManager:Response", requestId, json.encode(result or {}))
        end)
    end)
end

--- @param hooksGroup string
--- @param hookNames string[]
local function addHookHandlers(hooksGroup, hookNames)
    local hooksTableNameHead = string.sub(hooksGroup, 1, 2)
    local hooksTableNameTail = string.sub(hooksGroup, 2)
    local hooksFileName = string.lower(hooksTableNameHead) .. hooksTableNameTail

    local hooksTableName = hooksGroup .. "Hooks"
    local hooksTable = _G[hooksTableName]
    if type(hooksTable) ~= "table" then
        error("'" ..
            hooksTableName ..
            "' not found. Did you accidentally delete it from 'hooks/" ..
            hooksFileName ..
            ".lua'?")
    end

    for _, hookName in ipairs(hookNames) do
        local hookFn = hooksTable[hookName]
        if type(hookFn) ~= 'function' then
            error("'" ..
                hookName ..
                "' hook not found. Did you accidentally delete it from '" ..
                hooksTableName ..
                "' in 'hooks/" ..
                hooksFileName ..
                ".lua'?")
        end

        addHookHandler(hookFn, hooksGroup, hookName)
    end
end

addHookHandlers("VehicleInfo", { "fetch" })                               -- vehicleInfo.lua
addHookHandlers("Usersettings", { "getInfo", "fetchAllFor", "storeFor" }) -- usersettings.lua
addHookHandlers("PlayerPermissions", { "fetchFor" })                      -- playerPermissions.lua
