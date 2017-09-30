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

`int emu.framecount()`

Returns the framecount value. The frame counter runs without a movie running so this always returns a value.

`emu.getdir()`

Returns the path of MO5-Emulator executable as a string.

`emu.loadrom(string filename)`

Loads the ROM from the directory relative to the lua script or from the absolute path. Hence, the filename parameter can be absolute or relative path.

If the ROM can't be loaded, loads the most recent one.

## Memory Library

`memory.readbyte(int address)`

`memory.readbyteunsigned(int address)`

Get an unsigned byte from the RAM at the given address. Returns a byte regardless of emulator. The byte will always be positive.

`memory.readbyterange(int address, int length)`

Get a length bytes starting at the given address and return it as a string. Convert to table to access the individual bytes.

`memory.readbytesigned(int address)`

Get a signed byte from the RAM at the given address. Returns a byte regardless of emulator. The most significant bit will serve as the sign.

`memory.readword(int address)`

`memory.readwordunsigned(int address)`

Get an unsigned word from the RAM at the given address. Returns a 16-bit value regardless of emulator. The value will always be positive.

`memory.readwordsigned(int address)`

The same as above, except the returned value is signed, i.e. its most significant bit will serve as the sign.

`memory.writebyte(int address, int value)`

Write the value to the RAM at the given address. The value is modded with 256 before writing (so writing 257 will actually write 1). Negative values allowed.

`int memory.getregister(cpuregistername)`

Returns the current value of the given hardware register.
For example, memory.getregister("pc") will return the main CPU's current Program Counter.

Valid registers are: "a", "b", "cc", "d", "dp", "s", "u", "x", "y" and "pc".

`memory.setregister(string cpuregistername, int value)`

Sets the current value of the given hardware register.
For example, memory.setregister("pc",0x200) will change the main CPU's current Program Counter to 0x200.

Valid registers are: "a", "b", "cc", "d", "dp", "s", "u", "x", "y" and "pc".

You had better know exactly what you're doing or you're probably just going to crash the game if you try to use this function. That applies to the other memory.write functions as well, but to a lesser extent.

`memory.register(int address, [int size,] function func)`

`memory.registerwrite(int address, [int size,] function func)`

Registers a function to be called immediately whenever the given memory address range is written to.

address is the address in CPU address space (0x0000 - 0xFFFF).

size is the number of bytes to "watch". For example, if size is 100 and address is 0x0200, then you will register the function across all 100 bytes from 0x0200 to 0x0263. A write to any of those bytes will trigger the function. Having callbacks on a large range of memory addresses can be expensive, so try to use the smallest range that's necessary for whatever it is you're trying to do. If you don't specify any size then it defaults to 1.

The callback function will receive two arguments, (address, size) indicating what write operation triggered the callback. If you don't care about that extra information then you can ignore it and define your callback function to not take any arguments. The value that was written is NOT passed into the callback function, but you can easily use any of the memory.read functions to retrieve it.

You may use a memory.write function from inside the callback to change the value that just got written. However, keep in mind that doing so will trigger your callback again, so you must have a "base case" such as checking to make sure that the value is not already what you want it to be before writing it. Another, more drastic option is to de-register the current callback before performing the write.

If func is nil that means to de-register any memory write callbacks that the current script has already registered on the given range of bytes.

`memory.writeword(int address, int value)`

Write the value to the RAM at the given address. The value is modded with 65536 before writing (so writing 65537 will actually write 1). Negative values allowed.

`memory.registerexec(int address, [int size,] function func)`

`memory.registerrun(int address, [int size,] function func)`

`memory.registerexecute(int address, [int size,] function func)`

Registers a function to be called immediately whenever the emulated system runs code located in the given memory address range.

Since "address" is the address in CPU address space (0x0000 - 0xFFFF), this doesn't take ROM banking into account, so the callback will be called for any bank, and in some cases you'll have to check current bank in your callback function.

The information about memory.register applies to this function as well.

## Debugger Library

`int debugger.getcyclescount()`

Returns an integer value representing the number of CPU cycles elapsed since the poweron or since the last reset of the cycles counter.

`int debugger.getinstructionscount()`

Returns an integer value representing the number of CPU instructions executed since the poweron or since the last reset of the instructions counter.

`debugger.resetcyclescount()`

Resets the cycles counter.

`debugger.resetinstructionscount()`

Resets the instructions counter.

## GUI Library

`gui.pixel(int x, int y, type color)`

`gui.drawpixel(int x, int y, type color)`

`gui.setpixel(int x, int y, type color)`

`gui.writepixel(int x, int y, type color)`

Draw one pixel of a given color at the given position on the screen. See drawing notes and color notes at the bottom of the page.

`gui.line(int x1, int y1, int x2, int y2 [, color])`

`gui.drawline(int x1, int y1, int x2, int y2 [, color])`

Draws a line between the two points. The x1,y1 coordinate specifies one end of the line segment, and the x2,y2 coordinate specifies the other end. If skipfirst is true then this function will not draw anything at the pixel x1,y1, otherwise it will. The default color for the line is solid white, but you may optionally override that using a color of your choice. See also drawing notes and color notes at the bottom of the page.

`gui.box(int x1, int y1, int x2, int y2 [, fillcolor [, outlinecolor]]))`

`gui.drawbox(int x1, int y1, int x2, int y2 [, fillcolor [, outlinecolor]]))`

`gui.rect(int x1, int y1, int x2, int y2 [, fillcolor [, outlinecolor]]))`

`gui.drawrect(int x1, int y1, int x2, int y2 [, fillcolor [, outlinecolor]]))`

Draws a rectangle between the given coordinates of the emulator screen for one frame. The x1,y1 coordinate specifies any corner of the rectangle (preferably the top-left corner), and the x2,y2 coordinate specifies the opposite corner.

The default color for the box is transparent white with a solid white outline, but you may optionally override those using colors of your choice. Also see drawing notes and color notes.

`gui.text(int x, int y, string str [, textcolor [, backcolor]])`

`gui.drawtext(int x, int y, string str [, textcolor [, backcolor]])`

Draws a given string at the given position. textcolor and backcolor are optional. See 'on colors' at the end of this page for information. Using nil as the input or not including an optional field will make it use the default.

## Input Library

`table input.get()`

`table input.read()`

Reads input from keyboard and mouse. Returns pressed keys and the position of mouse in pixels on game screen.  The function returns a table with at least two properties; table.xmouse and table.ymouse.  Additionally any of these keys will be set to true if they were held at the time of executing this function:
leftclick, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z, delete, comma, insert, dot, back, at, space, slash, minus, multiply, plus, reset, enter, left, up, right, down, numpad0, numpad1, numpad2, numpad3, numpad4, numpad5, numpad6, numpad7, numpad8, numpad9, control, backspace, stop, shift, basic.

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

`savestate.persist(object savestate)`

Set the given savestate to be persistent. It will not be deleted when you load this state but at the exit of this script instead, unless it's one of the predefined states.  If it is one of the predefined savestates it will be saved as a file on disk.

## Appendix

### On colors

Colors can be of a few types.

- Int: use the a formula to compose the color as a number (depends on color depth)
- String: Can either be a HTML colors, simple colors, or internal palette colors.
- HTML string: "#rrggbb" ("#228844") or #rrggbbaa if alpha is supported.
Simple colors: "clear", "red", "green", "blue", "white", "black", "gray", "grey", "orange", "yellow", "green", "teal", "cyan", "purple", "magenta".
- Array: Example: {255,112,48,96} means {red=255, green=112, blue=48, alpha=96}
- Table: Example: {r=255,g=112,b=48,a=96} means {red=255, green=112, blue=48, alpha=96}

For transparancy use "clear".