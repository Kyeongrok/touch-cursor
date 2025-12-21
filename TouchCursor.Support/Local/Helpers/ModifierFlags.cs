// Copyright © 2025. Ported to C# from original C++ TouchCursor by Martin Stone.
// Original project licensed under GNU GPL v3.

namespace TouchCursor.Support.Local.Helpers;

[Flags]
public enum ModifierFlags
{
    None = 0,
    Shift = 0x00010000,
    Ctrl = 0x00020000,
    Alt = 0x00040000,
    Win = 0x00080000
}
