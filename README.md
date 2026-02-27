# IronBrew 2 (macOS setup)

VM-based Lua 5.1 obfuscation with a macOS-friendly workflow.

## What changed in this version

- Added `run-obfuscate.sh` to run obfuscation in one command.
- Added automatic local Lua 5.1 toolchain bootstrap (`.local-tools/lua-5.1.5`).
- Updated obfuscator runtime tool resolution in `IronBrew2/Program.cs`:
  - Uses `IB2_LUAC` and `IB2_LUA` env vars when set.
  - Falls back to executables on `PATH` (`luac`, then `luajit`/`lua`).
- Added custom watermark injection via `IB2_WATERMARK`.
- Added script tags support in `run-obfuscate.sh`:
  - `--tag "text"` can be repeated.
- Hardened generated VM loader against `table.concat` hook dumping:
  - Removed dependency on global `table.concat`.
  - Uses internal `Concat` implementation in generated VM.

## Quick start (macOS)

1. Go to repo root:
# CHANGE URUSERHERE TO YOUR COMPUTER NAME
```bash
cd /Users/URUSER HERE/Desktop/ironbrew-2-master
```

2. Make script executable once:

```bash
chmod +x run-obfuscate.sh
```

3. Create test input:

```bash
printf 'print("hello world")\n' > hello.lua
```

4. Obfuscate:

```bash
./run-obfuscate.sh hello.lua hello.obf.lua
```

5. Run output with local Lua:

```bash
./.local-tools/lua-5.1.5/src/lua hello.obf.lua
```

## Usage

Basic:

```bash
./run-obfuscate.sh <input.lua> [output.lua]
```

With custom watermark tags (this goes in the comment at the top):

```bash
./run-obfuscate.sh \
  --tag "ironbrew on mac" \
  --tag "whatever u want here" \
  input.lua output.obf.lua
```

## How `run-obfuscate.sh` works

1. Ensures local Lua 5.1 tools exist (`lua` and `luac`).
2. Builds `IronBrew2 CLI` in Debug.
3. Runs CLI with:
   - `DOTNET_ROLL_FORWARD=Major`
   - `IB2_LUAC=<local luac>`
   - `IB2_LUA=<local lua>`
   - `IB2_WATERMARK=<joined tags>`
4. Copies generated `out.lua` to your requested output path.

## Luau compatibility (important)

IronBrew2 compiles with Lua 5.1 (`luac`), so Luau-only syntax will fail.

Common breaking syntax:

- `continue`
- compound assignment (`+=`, `-=`, etc.)
- Luau type annotations (`local x: number`)
- other Luau-only parser features

Example failure:

```text
ERR: ...luac: ...:12510: '=' expected near 'end'
```

In this case, `continue` caused the error.

### Current fix strategy

Use Lua 5.1-compatible source before obfuscation.

Example conversion:

```lua
-- Luau
for _, group in ipairs(groups) do
    if #group == 1 then continue end
    -- work
end
```

```lua
-- Lua 5.1-compatible
for _, group in ipairs(groups) do
    if #group ~= 1 then
        -- work
    end
end
```

## Troubleshooting

`luac not found` or `luajit not found`

- Use `run-obfuscate.sh`; it bootstraps local Lua tools automatically.

`.NET 3.1 missing` when running CLI directly

- Use the script, or set:
  - `DOTNET_ROLL_FORWARD=Major`

`Permission denied: ./run-obfuscate.sh`

- Run:

```bash
chmod +x run-obfuscate.sh
```

`Updates were rejected (fetch first)` when pushing

- Remote has commits you do not have locally.
- Fetch and merge/rebase remote `main` before push.

## Security notes

The `table.concat` dump vector was mitigated, but this is not full anti-tamper.

A hostile runtime can still hook other globals before loader startup, such as:

- `string.byte`
- `string.char`
- `string.sub`

If needed, add loader integrity checks and early abort behavior for patched primitives.

* Ps yes ai wrote this description I have other things to do...
## Known Constant Dump Methods
`Please note these still take a few braincells to use so slightly better than before. With concat dump at top.`
Thanks to Norb for finding these 
Method #1 

Targeted string reconstruction dump.

```
local dump = "";
    for e = 1, #n do
        l[e] = h(o(f(t(n, e, e)), 39));
         dump = dump .. l[e]
    end;
    warn(dump)
```
Method #2 Deep table traversal

```
return function(...)
        local t = l;
        local B = n;
        for i, v in n do 
            for i2, v2 in v do 
                if typeof(v2) == "table" then
                    for i3, v3 in v2 do 
                        for i4, v4 in v3 do 
                            warn(v4)
                        end
                    end
                end
                print(v2)
                
            end
        end
        local o = e;
```
