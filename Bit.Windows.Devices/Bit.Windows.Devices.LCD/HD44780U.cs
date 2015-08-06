//
// The MIT License(MIT)
//
// Copyright(c) 2015 Christopher McLendon (http://www.bitunify.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//
// <Description>
//   Base class for communicating with Hitachi HD44780U compatible LCD displays.  This display
//   uses an 8-bit or 4-bit data interface with 3 additional control lines (RS, RW and E).
// </Description>  
//

using System;

namespace Bit.Windows.Devices.I2c.LCD
{
    public enum DATAMODE : byte
    {
        MODE_4BIT = 0x0,
        MODE_8BIT = 0x1
    };

    public enum DISPLAYDEF : byte
    {
        DISPLAY2x16 = 0x0,
        DISPLAY4x20 = 0x1,
    };

    public enum TEXT_JUSTIFICATION : byte
    {
        LEFT = 0x0,
        CENTER = 0x1,
        RIGHT = 0x2
    }

    public enum TEXT_WRAPPING : byte
    {
        NO = 0x0,
        YES = 0x1
    }

    public abstract class HD44780U
    {
        private const char emptyChar = ' ';
        private string emptyString;

        protected struct Command
        {
            public bool Rs { get; }
            public bool Rw { get; }
            public byte Data { get; set; }

            public Command(bool rs, bool rw, byte data)
            {
                Rs = rs;
                Rw = rw;
                Data = data;
            }
        }

        [Flags]
        protected enum FEATURE : byte
        {
            BACKLIGHT = 0x01
        };

        #region Commands and FLAGS
        /// <summary>
        /// LCD commands
        /// </summary>
        protected enum COMMANDS : byte
        {
            CLEARDISPLAY = 0x01,
            RETURNHOME = 0x02,
            ENTRYMODESET = 0x04,
            DISPLAYCONTROL = 0x08,
            CURSORDISPLAYSHIFT = 0x10,
            FUNCTIONSET = 0x20,
            SETCGRAMADDR = 0x40,
            SETDDRAMADDR = 0x80
        };

        /// <summary>
        /// Display entry mode set
        /// </summary>
        protected enum ENTRYMODESET : byte
        {
            CURSORLEFT = 0x0,
            CURSORDISPLAY_SHIFTLEFT = 0x1,
            CURSORRIGHT = 0x2,
            CURSORDISPLAY_SHIFTRIGHT = 0x3
        };

        /// <summary>
        /// Display on/off and cursor control
        /// </summary>
        protected enum DISPLAYCONTROL_DISPLAY : byte
        {
            ON = 0x4,
            OFF = 0x0
        };

        protected enum DISPLAYCONTROL_CURSORVISIBILITY : byte
        {
            ON = 0x2,
            OFF = 0x0
        };

        protected enum DISPLAYCONTROL_CURSORBLINK : byte
        {
            BLINK = 0x1,
            NOBLINK = 0x0
        };

        /// <summary>
        /// Cursor and display shift
        /// </summary>
        protected enum CURSORSHIFT : byte
        {
            CURSORLEFT = 0x0,
            CURSORRIGHT = 0x4
        };

        protected enum DISPLAYSHIFT : byte
        {
            DISPLAYLEFT = 0x8,
            DISPLAYRIGHT = 0xC
        };

        /// <summary>
        /// Function set mask
        /// </summary>   
        protected enum FUNCTION_DATABUSWIDTH : byte
        {
            DATABUS_4BIT = 0x00,
            DATABUS_8BIT = 0x10
        };

        /// <summary>
        /// Function set mask
        /// </summary>   
        protected enum FUNCTION_DISPLAY : byte
        {
            CHAR_5x8_1LINE = 0x0,
            CHAR_5x10_1LINE = 0x4,
            CHAR_5x8_2LINE = 0x8
        };
        #endregion

        private FUNCTION_DISPLAY displayFunction;

        private DATAMODE dataMode;
        private DISPLAYDEF displayMode;

        protected bool dataBusInitialized;

        private bool enableBacklight;
        private bool blinkingCursor;
        private bool cursorVisible;
        private bool displayEnabled;

        private byte columns;
        private byte rows;

        private byte[] rowAddressEntry;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="address">PCF8574A base read-address</param>
        public HD44780U(DATAMODE dataMode, DISPLAYDEF displayMode)
        {
            dataBusInitialized = false;

            this.dataMode = dataMode;
            this.displayMode = displayMode;

            // default feature and display property values
            enableBacklight = false;
            blinkingCursor = false;
            cursorVisible = false;
            displayEnabled = true;

            // configure display mode settings
            setDisplayMode(displayMode);
        }

        private void setDisplayMode(DISPLAYDEF displayMode)
        {
            switch (displayMode)
            {
                case DISPLAYDEF.DISPLAY2x16:
                    {
                        columns = 16;
                        rows = 2;
                        rowAddressEntry = new byte[] { 0x0, 0x40 };
                        displayFunction = FUNCTION_DISPLAY.CHAR_5x8_2LINE;
                        break;
                    }
                case DISPLAYDEF.DISPLAY4x20:
                    {
                        columns = 20;
                        rows = 4;
                        rowAddressEntry = new byte[] { 0x0, 0x40, 0x14, 0x54 };
                        displayFunction = FUNCTION_DISPLAY.CHAR_5x8_2LINE;
                        break;
                    }
                default:
                    {
                        throw new NotSupportedException("Display mode is not supported");
                    }
            }

            // initialize the empty row string
            emptyString = String.Empty;

            for (byte index = 0; index < columns; index++)
                emptyString += emptyChar;
        }

        #region Properties
        public bool EnableBacklight
        {
            get
            {
                return enableBacklight;
            }

            set
            {
                if (enableBacklight != value)
                {
                    enableBacklight = value;
                    FeatureStateChanged(FEATURE.BACKLIGHT);
                }
            }
        }

        public bool CursorVisible
        {
            get
            {
                return cursorVisible;
            }
            set
            {
                if (value != cursorVisible)
                {
                    cursorVisible = value;
                    refreshDisplayControl();
                }
            }
        }

        public bool BlinkingCursor
        {
            get
            {
                return blinkingCursor;
            }

            set
            {
                if (value!= blinkingCursor)
                {
                    blinkingCursor = value;
                    refreshDisplayControl();
                }
            }
        }

        public bool DisplayEnabled
        {
            get
            {
                return displayEnabled;
            }

            set
            {
                if (value != displayEnabled)
                {
                    displayEnabled = value;
                    refreshDisplayControl();
                }
            }
        }

        public DATAMODE BusMode
        {
            get
            {
                return dataMode;
            }
        }
        #endregion

        public virtual void Initialize()
        {
            dataBusInitialized = false;

            // SEE HITACHI data sheet page 46/figure 24 for 4-bit interface initialization specification
            I2cHelper.wait(100);

            FunctionSet(FUNCTION_DATABUSWIDTH.DATABUS_8BIT);
            I2cHelper.wait(50);

            FunctionSet(FUNCTION_DATABUSWIDTH.DATABUS_8BIT);
            I2cHelper.wait(50);

            FunctionSet(FUNCTION_DATABUSWIDTH.DATABUS_8BIT);
            I2cHelper.wait(20);

            if (BusMode == DATAMODE.MODE_4BIT)
            {
                // transition to 4-bit data bus mode
                FunctionSet(FUNCTION_DATABUSWIDTH.DATABUS_4BIT);
            }

            // done with configuring data bus width
            dataBusInitialized = true;

            // set display default and bus width for last time; display lines and character font can't be changed after this point
            FunctionSet(
                BusMode == DATAMODE.MODE_4BIT ? FUNCTION_DATABUSWIDTH.DATABUS_4BIT : FUNCTION_DATABUSWIDTH.DATABUS_8BIT, 
                displayFunction);

            // turn off display, cusor visibility and cursor blink
            DisplayControl(DISPLAYCONTROL_DISPLAY.OFF, DISPLAYCONTROL_CURSORVISIBILITY.OFF, DISPLAYCONTROL_CURSORBLINK.NOBLINK);

            // clear the display
            ClearDisplay();

            // Entry mode set
            EntryModeSet(ENTRYMODESET.CURSORRIGHT);

            // turn on the display
            DisplayControl(DISPLAYCONTROL_DISPLAY.ON, DISPLAYCONTROL_CURSORVISIBILITY.OFF, DISPLAYCONTROL_CURSORBLINK.NOBLINK);
        }

        #region Instruction Helpers
        private void SendCommand(COMMANDS command, byte flags)
        {
            byte data = (byte)((byte)command | flags);
            Command i = new Command(false, false, data);
            SendCommand(i);
        }
        protected void ClearDisplay()
            => SendCommand(COMMANDS.CLEARDISPLAY, 0x0);

        protected void ReturnHome() 
            => SendCommand(COMMANDS.RETURNHOME, 0x0);

        protected void EntryModeSet(ENTRYMODESET flags) 
            => SendCommand(COMMANDS.ENTRYMODESET, (byte)flags);

        protected void DisplayControl(DISPLAYCONTROL_DISPLAY display, 
            DISPLAYCONTROL_CURSORVISIBILITY cursorVisibility, DISPLAYCONTROL_CURSORBLINK cursorBlink)
            => SendCommand(COMMANDS.DISPLAYCONTROL, (byte)((byte)display | (byte)cursorVisibility | (byte)cursorBlink));

        protected void CursorDisplayShift(CURSORSHIFT cursor, DISPLAYSHIFT display) 
            => SendCommand(COMMANDS.CURSORDISPLAYSHIFT, (byte) ( (byte)cursor | (byte)display));

        protected void FunctionSet(FUNCTION_DATABUSWIDTH width, FUNCTION_DISPLAY displayFlags)
            => SendCommand(COMMANDS.FUNCTIONSET, (byte)((byte)width | (byte)displayFlags));


        protected void FunctionSet(FUNCTION_DATABUSWIDTH width)
            => SendCommand(COMMANDS.FUNCTIONSET, (byte)width);

        protected void FunctionSet(FUNCTION_DISPLAY displayFlags)
            => SendCommand(COMMANDS.FUNCTIONSET, (byte)displayFlags);

        protected void SetCGRAMAddress(byte address) 
            => SendCommand(COMMANDS.SETCGRAMADDR, address);

        protected void SetDDRAMAddress(byte address) 
            => SendCommand(COMMANDS.SETDDRAMADDR, address);
        #endregion

        #region Public Operators
        public void Write(char data)
        {
            Command i = new Command(true, false, (byte) data);
            SendCommand(i);
        }

        public void Write(string data)
        {
            Command i = new Command(true, false, 0x0);
            char[] chars = data.ToCharArray();

            foreach (char c in chars)
            {
                // send character in string array to LCD
                i.Data = (byte)c;
                SendCommand(i);
            }
        }

        public void Write(byte row, byte column, string data)
        {
            // write string to LCD
            SetCursorPosition(row, column);
            Write(data);
        }

        public void Write(byte row, byte column, char data)
        {
            SetCursorPosition(row, column);
            Command i = new Command(true, false, (byte)data);

            // send character to LCD
            SendCommand(i);
        }

        public void WriteLine(byte row, string data,
            TEXT_JUSTIFICATION justification = TEXT_JUSTIFICATION.LEFT,
            TEXT_WRAPPING textwrap = TEXT_WRAPPING.NO)
        {
            byte index = 0;

            if (textwrap == TEXT_WRAPPING.YES)
                throw new NotImplementedException();

            if (data.Length <= columns)
            {
                switch (justification)
                {
                    case TEXT_JUSTIFICATION.RIGHT:
                        {
                            index = (byte)(columns - data.Length);
                            break;
                        }
                    case TEXT_JUSTIFICATION.CENTER:
                        {
                            index = (byte)((columns - data.Length) / 2);
                            break;
                        }
                    case TEXT_JUSTIFICATION.LEFT:
                    default:
                        {
                            index = 0;
                            break;
                        }
                }
            }

            // clear the line first
            Write(row, 0, emptyString);

            // write the string to the LCD
            Write(row, index, data);
        }

        public void SetCursorPosition(byte row, byte column)
        {
            SetDDRAMAddress(DDRAMfromPosition(row, column));
        }

        public void Clear()
        {
            ClearDisplay();
        }
        #endregion  

        #region Helpers
        protected void refreshDisplayControl()
        {
            DisplayControl(
                DisplayEnabled == true ? DISPLAYCONTROL_DISPLAY.ON : DISPLAYCONTROL_DISPLAY.OFF,
                CursorVisible == true ? DISPLAYCONTROL_CURSORVISIBILITY.ON : DISPLAYCONTROL_CURSORVISIBILITY.OFF,
                BlinkingCursor == true ? DISPLAYCONTROL_CURSORBLINK.BLINK : DISPLAYCONTROL_CURSORBLINK.NOBLINK);
        }

        protected byte DDRAMfromPosition(byte row, byte column)
        {
            byte address = 0x0;

            if ((row >= 0 && row <= rows) &&
                (column >= 0 && column <= columns))
            {
                address = (byte) (rowAddressEntry[row] + column);
            }

            return address;
        }
        #endregion

        #region Abstract Members
        protected abstract void SendCommand(Command cmd);
        protected abstract byte Read();
        protected abstract void FeatureStateChanged(FEATURE mask);
        #endregion
    }
}