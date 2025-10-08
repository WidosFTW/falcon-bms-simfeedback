//
// Copyright (c) 2019 Rausch IT
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
// THE SOFTWARE.
//
//
using SimFeedback.log;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SimFeedback.telemetry.falcon
{
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct FlightData
    {
        public float x;
        public float y;
        public float z;
        public float xDot;
        public float yDot;
        public float zDot;
        public float alpha;
        public float beta;
        public float gamma;
        public float pitch;
        public float roll;
        public float yaw;
        public float mach;
        public float kias;
        public float vt;
        public float gs;
        public float windOffset;
        public float nozzlePos;
        public float internalFuel;
        public float externalFuel;
        public float fuelFlow;
        public float rpm;
        public float ftit;
        public float gearPos;
        public float speedBrake;
        public float epuFuel;
        public float oilPressure;
        public uint lightBits;

        public float headPitch;
        public float headRoll;
        public float headYaw;

        public uint lightBits2;
        public uint lightBits3;

        public float ChaffCount;
        public float FlareCount;

        public float NoseGearPos;
        public float LeftGearPos;
        public float RightGearPos;

        public float AdiIlsHorPos;
        public float AdiIlsVerPos;

        public int courseState;
        public int headingState;
        public int totalStates;

        public float courseDeviation;
        public float desiredCourse;
        public float distanceToBeacon;
        public float bearingToBeacon;
        public float currentHeading;
        public float desiredHeading;
        public float deviationLimit;
        public float halfDeviationLimit;
        public float localizerCourse;
        public float airbaseX;
        public float airbaseY;
        public float totalValues;

        public float TrimPitch;
        public float TrimRoll;
        public float TrimYaw;

        public uint hsiBits;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5 * 26)]
        public byte[] DEDLines;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5 * 26)]
        public byte[] Invert;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5 * 26)]
        public byte[] PFLLines;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5 * 26)]
        public byte[] PFLInvert;

        public int UFCTChan;
        public int AUXTChan;

        public int RwrObjectCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public int[] RWRsymbol;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public float[] bearing;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public uint[] missileActivity;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public uint[] missileLaunch;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public uint[] selected;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public float[] lethality;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public uint[] newDetection;

        public float fwd;
        public float aft;
        public float total;

        public int VersionNum;

        public float headX;
        public float headY;
        public float headZ;

        public int MainPower;
    }

    /// <summary>

    /// </summary>
    class FalconTelemetryProvider : AbstractTelemetryProvider
    {

        private bool isStopped = true;          // flag to control the polling thread
        private Thread t;                       // the polling thread, reads telemetry data and sends TelemetryUpdated events

        public FalconTelemetryProvider() : base()
        {
            Author = "WidosFTW";
            Version = "v1.0.0";
            BannerImage = @"img\banner_falcon.png"; // Image shown on top of the profiles tab
            IconImage = @"img\falcon.png";          // Icon used in the tree view for the profile
            TelemetryUpdateFrequency = 100;     // the update frequency in samples per second
        }

        /// <summary>
        /// Name of this TelemetryProvider.
        /// Used for dynamic loading and linking to the profile configuration.
        /// </summary>
        public override string Name { get { return "Falcon"; } }

        public override void Init(ILogger logger)
        {
            base.Init(logger);
            Log("Initializing F4 BMS Telemetry Provider");
        }

        /// <summary>
        /// A list of all telemetry names of this provider.
        /// </summary>
        /// <returns>List of all telemetry names</returns>
        public override string[] GetValueList()
        {
            return GetValueListByReflection(typeof(FalconData));
        }

        /// <summary>
        /// Start the polling thread
        /// </summary>
        public override void Start()
        {
            if (isStopped)
            {
                isStopped = false;
                t = new Thread(Run);
                t.Start();
            }
        }

        /// <summary>
        /// Stop the polling thread
        /// </summary>
        public override void Stop()
        {
            isStopped = true;
            if (t != null) t.Join();
        }

        /// <summary>
        /// The thread funktion to poll the telemetry data and send TelemetryUpdated events.
        /// </summary>
        private void Run()
        {
            FalconData lastTelemetryData = new FalconData();
            Stopwatch sw = new Stopwatch();
            Session session = new Session();
            sw.Start();

            while (!isStopped)
            {
                try
                {

                    FlightData falconflightdata = (FlightData) readSharedMemory(typeof(FlightData), "FalconSharedMemoryArea");
                    // get data from game, 

                    FalconData telemetryData = new FalconData();

                    IsRunning = true;

                    telemetryData.time = (float)sw.Elapsed.TotalSeconds;
                    telemetryData.pitch = falconflightdata.pitch;
                    telemetryData.roll = falconflightdata.roll;
                    telemetryData.yaw = falconflightdata.yaw;
                    telemetryData.airspeed = (float)falconflightdata.kias * 1.852f;
                    telemetryData.heave = falconflightdata.gs;
                    telemetryData.aoa = (float)falconflightdata.alpha * (float)Math.PI / 180.0f;

                    TelemetryEventArgs args = new TelemetryEventArgs(
                        new FalconTelemetryInfo(telemetryData, lastTelemetryData, session));
                    RaiseEvent(OnTelemetryUpdate, args);
                    lastTelemetryData = telemetryData;

                    Thread.Sleep(10);
                    sw.Restart();
                }
                catch (Exception)
                {
                    IsConnected = false;
                    IsRunning = false;
                    Thread.Sleep(1000);
                }
            }
            IsConnected = false;
            IsRunning = false;

            Log("Listener thread stopped, FalconTelemetryProvider.Thread");
        }


    }
}
