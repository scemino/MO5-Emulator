﻿namespace nMO5
{
    public class Machine
    {
        private readonly Memory _mem;
        private readonly M6809 _micro;
        private readonly Screen _screen;
        private bool _irq;

        public Machine(Screen screen, ISound sound)
        {
            _mem = new Memory();
            _screen = screen;
            _micro = new M6809(_mem, sound);
            Keyboard = new Keyboard(_mem);
            _screen.Init(_mem);
        }

		public Keyboard Keyboard { get; }

        public void Step()
        {
            FullSpeed();
            //Synchronize();
        }

        public void SetK7File(string k7)
        {
            _mem.SetK7File(k7);
        }

        // soft reset method ("reinit prog" button on original MO5) 
        public void ResetSoft()
        {
            _micro.Reset();
        }

        // hard reset (switch off and on)
        public void ResetHard()
        {
            for (var i = 0x2000; i < 0x3000; i++) {
                _mem.Set(i, 0);
            }
            _micro.Reset();
        }

        // Debug Methods
        public string DumpRegisters()
        {
            return _micro.PrintState();
        }

        public string DisassembleFromPc(int nblines)
        {
            return _micro.Disassemble(_micro.Pc, nblines);
        }

        // the emulator main loop
        private void FullSpeed()
        {
            //screen.repaint(); // Mise a jour de l'affichage

            // Mise a jour du crayon optique a partir des donnée de la souris souris
            if (_screen != null)
            {
                _mem.LightPenClick = _screen.MouseClick;
                _mem.LightPenX = _screen.MouseX;
                _mem.LightPenY = _screen.MouseY;
            }

            _mem.Set(0xA7E7, 0x00);
            /* 3.9 ms haut �cran (+0.3 irq)*/
            if (_irq)
            {
                _irq = false;
                _micro.FetchUntil(3800);
            }
            else
            {
                _micro.FetchUntil(4100);
            }

            /* 13ms fenetre */
            _mem.Set(0xA7E7, 0x80);
            _micro.FetchUntil(13100);

            _mem.Set(0xA7E7, 0x00);
            _micro.FetchUntil(2800);

            if ((_mem.Crb & 0x01) == 0x01)
            {
                _irq = true;
                /* Positionne le bit 7 de CRB */
                _mem.Crb |= 0x80;
                _mem.Set(0xA7C3, _mem.Crb);
                var cc = _micro.ReadCc();
                if ((cc & 0x10) == 0)
                    _micro.Irq();
                /* 300 cycles sous interrupt */
                _micro.FetchUntil(300);
                _mem.Crb &= 0x7F;
                _mem.Set(0xA7C3, _mem.Crb);
            }
        }

/*      private void Synchronize()
        {
            var realTimeMillis = Environment.TickCount - _lastTime;

            var sleepMillis = 20 - realTimeMillis - 1;
            if (sleepMillis < 0)
            {
                _lastTime = Environment.TickCount;
                return;
            }
            try
            {
                Thread.Sleep(sleepMillis);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
            _lastTime = Environment.TickCount;
        }*/
    } // of class
}