namespace nMO5
{
    public class Keyboard
    {
        private readonly int[] _ftable;
        private readonly Memory _mem;
        private int _shiftpressed;

        internal Keyboard(Memory mem)
        {
            _mem = mem;
            _ftable = new int[128];

            /* STOP */
            //ftable[0x6E]=0x29;
            /* 1 .. ACC */
            _ftable[0x5E] = '1';
            _ftable[0x4E] = '2';
            _ftable[0x3E] = '3';
            _ftable[0x2E] = '4';
            _ftable[0x1E] = '5';
            _ftable[0x0E] = '6';
            _ftable[0x0C] = '7';
            _ftable[0x1C] = '8';
            _ftable[0x2C] = '9';
            _ftable[0x3C] = '0';
            _ftable[0x4C] = '-';
            _ftable[0x5C] = '+';
            _ftable[0x6C] = 127;
            /* A .. --> */
            _ftable[0x5A] = 'a';
            _ftable[0x4A] = 'z';
            _ftable[0x3A] = 'e';
            _ftable[0x2A] = 'r';
            _ftable[0x1A] = 't';
            _ftable[0x0A] = 'y';
            _ftable[0x08] = 'u';
            _ftable[0x18] = 'i';
            _ftable[0x28] = 'o';
            _ftable[0x38] = 'p';
            _ftable[0x48] = '/';
            _ftable[0x58] = ')';
            /* Q .. enter */
            _ftable[0x56] = 'q';
            _ftable[0x46] = 's';
            _ftable[0x36] = 'd';
            _ftable[0x26] = 'f';
            _ftable[0x16] = 'g';
            _ftable[0x06] = 'h';
            _ftable[0x04] = 'j';
            _ftable[0x14] = 'k';
            _ftable[0x24] = 'l';
            _ftable[0x34] = 'm';
            _ftable[0x68] = '\r';
            /* W .. , */
            _ftable[0x60] = 'w';
            _ftable[0x50] = 'x';
            _ftable[0x64] = 'c';
            _ftable[0x54] = 'v';
            _ftable[0x44] = 'b';
            _ftable[0x00] = 'n';
            _ftable[0x10] = ',';

            _ftable[0x20] = '.';
            _ftable[0x30] = '@';
            //_ftable[0x6E] = 145 + Event; //STOP
            _ftable[0x58] = '*';

            ///* Specials keys */
            //_ftable[0x12] = (int)(Keys.Insert + Event);
            //_ftable[0x02] = (int)(Keys.Delete + Event);
            //_ftable[0x22] = 36 + Event; // Back to top
            _ftable[0x62] = 0xF700;
            _ftable[0x52] = 0xF702;
            _ftable[0x32] = 0xF703;
            _ftable[0x42] = 0xF701;
            /* espace */
            _ftable[0x40] = ' ';
            /* SHIFT + BASIC */
            //_ftable[0x70] = (int)(Keys.F12 + Event); //Shift
            //_ftable[0x72] = (int)(Keys.F11 + Event); //Basic

            /* CNT RAZ */
            //_ftable[0x6A] = 17 + Event; //CTRL
            _ftable[0x66] = 27; //ECHAP = raz
        }

        public void KeyPressed(char keycode)
        {
            for (var i = 0; i < 127; i++)
                _mem.RemKey(i);
            KeyTranslator(keycode, true);
        }

        public void KeyReleased(char keycode)
        {
            KeyTranslator(keycode, false);
        }

        public void Press(int tmp)
        {
            if (tmp == 'z')
            {
                _shiftpressed++;
                tmp = 16;
            }
            if (tmp == 'x')
                tmp = 50;
            if (_shiftpressed == 2)
            {
                _shiftpressed = 0;
                return;
            }

            for (var i = 0; i < 127; i++)
            {
                if (_ftable[i] != tmp) continue;

                _mem.SetKey(i);
                return;
            }
        }

        public void Release(int tmp)
        {
            if (tmp == 'z')
            {
                if (_shiftpressed == 1)
                    return;
                tmp = 16;
            }
            if (tmp == 'x')
                tmp = 50;
            for (var i = 0; i < 127; i++)
            {
                if (_ftable[i] == tmp)
                {
                    _mem.RemKey(i);
                    return;
                }
            }
        }

        private void KeyMemory(int key, bool press)
        {
            if (press)
                _mem.SetKey(key);
            else
                _mem.RemKey(key);
        }

        private void KeyTranslator(char keycode, bool press)
        {
            //System.Console.WriteLine("key " + keycode); // Debug
            switch (keycode)
            {
                case '!':
                    KeyMemory(0x70, press); //Shift
                    KeyMemory(0x5E, press); //1
                    return;
                case '\"':
                    KeyMemory(0x70, press); //Shift
                    KeyMemory(0x4E, press); //2
                    return;
                case '#':
                    KeyMemory(0x70, press); //Shift
                    KeyMemory(0x3E, press); //3
                    return;
                case '$':
                    KeyMemory(0x70, press); //Shift
                    KeyMemory(0x2E, press); //4
                    return;
                case '%':
                    KeyMemory(0x70, press); //Shift
                    KeyMemory(0x1E, press); //5
                    return;
                case '&':
                    KeyMemory(0x70, press); //Shift
                    KeyMemory(0x0E, press); //6
                    return;
                case (char)39: //'
                    KeyMemory(0x70, press); //Shift
                    KeyMemory(0x0C, press); //7
                    return;
                case '(':
                    KeyMemory(0x70, press); //Shift
                    KeyMemory(0x1C, press); //8
                    return;
                case ')':
                    KeyMemory(0x70, press); //Shift
                    KeyMemory(0x2C, press); //9
                    return;
                case '=':
                    KeyMemory(0x70, press); //Shift
                    KeyMemory(0x4C, press); //-
                    return;
                case ';':
                    KeyMemory(0x70, press); //Shift
                    KeyMemory(0x5C, press); //+
                    return;
                case '?':
                    KeyMemory(0x70, press); //Shift
                    KeyMemory(0x48, press); // /
                    return;
                case ':':
                    KeyMemory(0x70, press); //Shift
                    KeyMemory(0x58, press); //*
                    return;
                case '<':
                    KeyMemory(0x70, press); //Shift
                    KeyMemory(0x10, press); //,
                    return;
                case '>':
                    KeyMemory(0x70, press); //Shift
                    KeyMemory(0x20, press); //.
                    return;
                case '^':
                    KeyMemory(0x70, press); //Shift
                    KeyMemory(0x30, press); //@
                    return;
                case '©':
                    KeyMemory(0x6A, press); //Ctrl
                    KeyMemory(0x64, press); //C
                    return;
            }

            for (var i = 0; i < 127; i++)
            {
                if (_ftable[i] == keycode)
                {
                    // Match the lookup table
                    KeyMemory(i, press); // Press or release the key
                    return;
                }
            }
        }
    }
}