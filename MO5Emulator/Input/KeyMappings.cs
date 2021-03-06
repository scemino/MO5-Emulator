﻿using System.Collections.Generic;
using nMO5;

namespace MO5Emulator.Input
{
	static class KeyMappings
    {
        public static readonly Dictionary<char, VirtualKey> Keys = new Dictionary<char, VirtualKey>
		{
			{'a', new VirtualKey(Mo5Key.A) },
			{'z', new VirtualKey(Mo5Key.Z) },
			{'e', new VirtualKey(Mo5Key.E) },
			{'r', new VirtualKey(Mo5Key.R) },
			{'t', new VirtualKey(Mo5Key.T) },
			{'y', new VirtualKey(Mo5Key.Y) },
			{'u', new VirtualKey(Mo5Key.U) },
			{'i', new VirtualKey(Mo5Key.I) },
			{'o', new VirtualKey(Mo5Key.O) },
			{'p', new VirtualKey(Mo5Key.P) },
			{'q', new VirtualKey(Mo5Key.Q) },
			{'s', new VirtualKey(Mo5Key.S) },
			{'d', new VirtualKey(Mo5Key.D) },
			{'f', new VirtualKey(Mo5Key.F) },
			{'g', new VirtualKey(Mo5Key.G) },
			{'h', new VirtualKey(Mo5Key.H) },
			{'j', new VirtualKey(Mo5Key.J) },
			{'k', new VirtualKey(Mo5Key.K) },
			{'l', new VirtualKey(Mo5Key.L) },
			{'m', new VirtualKey(Mo5Key.M) },
			{'w', new VirtualKey(Mo5Key.W) },
			{'x', new VirtualKey(Mo5Key.X) },
			{'c', new VirtualKey(Mo5Key.C) },
			{'v', new VirtualKey(Mo5Key.V) },
			{'b', new VirtualKey(Mo5Key.B) },
			{'n', new VirtualKey(Mo5Key.N) },
			{'0', new VirtualKey(Mo5Key.D0) },
			{'1', new VirtualKey(Mo5Key.D1) },
			{'2', new VirtualKey(Mo5Key.D2) },
			{'3', new VirtualKey(Mo5Key.D3) },
			{'4', new VirtualKey(Mo5Key.D4) },
			{'5', new VirtualKey(Mo5Key.D5) },
			{'6', new VirtualKey(Mo5Key.D6) },
			{'7', new VirtualKey(Mo5Key.D7) },
			{'8', new VirtualKey(Mo5Key.D8) },
			{'9', new VirtualKey(Mo5Key.D9) },
			{',', new VirtualKey(Mo5Key.Comma) },
			{'.', new VirtualKey(Mo5Key.Dot) },
			{'/', new VirtualKey(Mo5Key.Slash) },
			{'@', new VirtualKey(Mo5Key.At) },
			{' ', new VirtualKey(Mo5Key.Space) },
			{'*', new VirtualKey(Mo5Key.Multiply) },
			{'\r', new VirtualKey(Mo5Key.Enter) },
			{'!', new VirtualKey(Mo5Key.D1, true) },
			{'\"', new VirtualKey(Mo5Key.D2, true) },
			{'#', new VirtualKey(Mo5Key.D3, true) },
			{'$', new VirtualKey(Mo5Key.D4, true) },
			{'%', new VirtualKey(Mo5Key.D5, true) },
			{'&', new VirtualKey(Mo5Key.D6, true) },
			{'\'', new VirtualKey(Mo5Key.D7, true) },
			{'(', new VirtualKey(Mo5Key.D8, true) },
			{')', new VirtualKey(Mo5Key.D9, true) },
			{'`', new VirtualKey(Mo5Key.D0, true) },
            {'=', new VirtualKey(Mo5Key.Minus, true) },
			{';', new VirtualKey(Mo5Key.Plus, true) },
			{'?', new VirtualKey(Mo5Key.Slash, true) },
			{':', new VirtualKey(Mo5Key.Multiply, true) },
			{'<', new VirtualKey(Mo5Key.Comma, true) },
			{'>', new VirtualKey(Mo5Key.Dot, true) },
			{'^', new VirtualKey(Mo5Key.At, true) },
            {'-', new VirtualKey(Mo5Key.Minus) },
            {'+', new VirtualKey(Mo5Key.Plus) },
			{(char)0xF700, new VirtualKey(Mo5Key.Up) },
			{(char)0xF701, new VirtualKey(Mo5Key.Down) },
			{(char)0xF702, new VirtualKey(Mo5Key.Left) },
			{(char)0xF703, new VirtualKey(Mo5Key.Right) }
		};

        public static readonly Dictionary<char, JoystickOrientation> Joystick1Orientations = new Dictionary<char, JoystickOrientation>
        {
            {'z', JoystickOrientation.North },
            {'s', JoystickOrientation.South },
            {'q', JoystickOrientation.West },
            {'d', JoystickOrientation.East }
        };

        public static readonly char Joystick1Button = 'v';

        public static readonly Dictionary<char, JoystickOrientation> Joystick2Orientations = new Dictionary<char, JoystickOrientation>
        {
            {'o', JoystickOrientation.North },
            {'l', JoystickOrientation.South },
            {'k', JoystickOrientation.West },
            {'m', JoystickOrientation.East }
        };

        public static readonly char Joystick2Button = 'n';
    }
}
