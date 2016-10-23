using System;
using System.Collections.Generic;
using System.Text;

namespace MyoSharp.Device
{
    public class MyoEventArgs : EventArgs
    {
        #region Constructors
        public MyoEventArgs(IMyo myo, DateTime timestamp)
        {
            this.Myo = myo;
            this.Timestamp = timestamp;
        }
        #endregion

        #region Properties
        public IMyo Myo { get; private set; }

        public DateTime Timestamp { get; private set; }
        #endregion
    }
}