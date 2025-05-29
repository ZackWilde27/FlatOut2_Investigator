# FlatOut2 Investigator Mod

This mod allows you to read and write arbitrary data in the game while it's running, like a specialized cheat engine.
It's intended to help with disassembly/decompilation

<br>

## Building
It requires the FlatOut2 SDK, specifically my fork of it (until the pull request goes through)

<br>

## Using the mod
First, create a ```that.txt``` file in the game's directory, where ```FlatOut2.exe``` is.

### Reading
To read data, the syntax is like this

```
name type offset
```

### Name
The name can be any of the following:
- player[n], like player1 or player5
- car[n], like car1 or car5
- race (RaceInfo)
- garage
- profile (PlayerProfile)
- host (PlayerHost)
- menu (MenuInterface)
- raw (offset is used as is, to read or write to a specific address)

### Type
It supports most of the basic C# types:
- float
- double
- long
- int
- uint
- short
- ushort
- byte
- sbyte
- ansi (ANSI string)
- uni (Unicode string)

Those will display the number as base-10, but I added some other types to make certain stuff easier
- hex[n] (Such as hex8, or hex32. Formats the string as hex)
- mask[n] (Such as mask8 or mask32. Formats the string as binary)

### Offset
The offset is read as hex

<br>

So if I wanted to read a float from player 1 at offset 0x0, I'd write it like this
```
player1 float 0
```
With that you should see the vftable being printed as a float on each frame during a race

<br>

You can have as many entries as you want, each on their own line

So if I also wanted to read a bitmask from player 3 at offset 0xD0, it would look like this
```
player1 float 0
player3 mask32 d0
```

### Writing
To write data, add = followed by the new value
```
player1 float 0 = 1.0
player3 mask32 d0 = FFFF
```

hex and mask types will be read as hex, the others will interpret it as a normal integer
