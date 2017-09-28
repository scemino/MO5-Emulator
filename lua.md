# Supported LUA functions
The following functions are available in MO5-Emulator, in addition to standard LUA capabilities:

## Emu library

`emu.poweron()`

Executes a power cycle.

`emu.softreset()`

Executes a (soft) reset.

`emu.frameadvance()`

Advance the emulator by one frame. It's like pressing the frame advance button once.

Most scripts use this function in their main game loop to advance frames. Note that you can also register functions by various methods that run "dead", returning control to the emulator and letting the emulator advance the frame.  For most people, using frame advance in an endless while loop is easier to comprehend so I suggest  starting with that.  This makes more sense when creating bots. Once you move to creating auxillary libraries, try the register() methods.

`emu.loadrom(string filename)`

Loads the ROM from the directory relative to the lua script or from the absolute path. Hence, the filename parameter can be absolute or relative path.

If the ROM can't be loaded, loads the most recent one.

## Memory Library

`memory.readbyte(int address)`

Get an unsigned byte from the RAM at the given address. Returns a byte regardless of emulator. The byte will always be positive.

`memory.readword(int address)`

Get an unsigned word from the RAM at the given address. Returns a 16-bit value regardless of emulator. The value will always be positive.

`memory.writebyte(int address, int value)`

Write the value to the RAM at the given address. The value is modded with 256 before writing (so writing 257 will actually write 1). Negative values allowed.

`memory.writeword(int address, int value)`

Write the value to the RAM at the given address. The value is modded with 65536 before writing (so writing 65537 will actually write 1). Negative values allowed.

## GUI Library

`gui.pixel(int x, int y, type color)`

Draw one pixel of a given color at the given position on the screen. See drawing notes and color notes at the bottom of the page.

`gui.line(int x1, int y1, int x2, int y2 [, color])`

Draws a line between the two points. The x1,y1 coordinate specifies one end of the line segment, and the x2,y2 coordinate specifies the other end. If skipfirst is true then this function will not draw anything at the pixel x1,y1, otherwise it will. The default color for the line is solid white, but you may optionally override that using a color of your choice. See also drawing notes and color notes at the bottom of the page.

`gui.box(int x1, int y1, int x2, int y2 [, fillcolor [, outlinecolor]]))`

Draws a rectangle between the given coordinates of the emulator screen for one frame. The x1,y1 coordinate specifies any corner of the rectangle (preferably the top-left corner), and the x2,y2 coordinate specifies the opposite corner.

The default color for the box is transparent white with a solid white outline, but you may optionally override those using colors of your choice. Also see drawing notes and color notes.

`gui.text(int x, int y, string str [, textcolor [, backcolor]])`

Draws a given string at the given position. textcolor and backcolor are optional. See 'on colors' at the end of this page for information. Using nil as the input or not including an optional field will make it use the default.

## Savestate Library

`object savestate.object(int slot = nil)`

Create a new savestate object. Optionally you can save the current state to one of the predefined slots(1-10) using the range 1-9 for slots 1-9, and 10 for 0, QWERTY style. Using no number will create an "anonymous" savestate.
Note that this does not actually save the current state! You need to create this value and pass it on to the load and save functions in order to save it.

Anonymous savestates are temporary, memory only states. You can make them persistent by calling memory.persistent(state). Persistent anonymous states are deleted from disk once the script exits.

`savestate.save(object savestate)`

Save the current state object to the given savestate. The argument is the result of savestate.create(). You can load this state back up by calling savestate.load(savestate) on the same object.

`savestate.load(object savestate)`

Load the the given state. The argument is the result of savestate.create() and has been passed to savestate.save() at least once.

If this savestate is not persistent and not one of the predefined states, the state will be deleted after loading.

## Appendix

### On colors

Colors can be of a few types.
Int: use the a formula to compose the color as a number (depends on color depth)
String: Can either be a HTML colors, simple colors, or internal palette colors.
HTML string: "#rrggbb" ("#228844") or #rrggbbaa if alpha is supported.
Simple colors: "clear", "red", "green", "blue", "white", "black", "gray", "grey", "orange", "yellow", "green", "teal", "cyan", "purple", "magenta".
Array: Example: {255,112,48,96} means {red=255, green=112, blue=48, alpha=96}
Table: Example: {r=255,g=112,b=48,a=96} means {red=255, green=112, blue=48, alpha=96}

For transparancy use "clear".