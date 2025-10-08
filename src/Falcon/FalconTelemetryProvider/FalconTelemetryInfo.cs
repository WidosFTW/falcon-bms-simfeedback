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
using System;
using System.Reflection;

namespace SimFeedback.telemetry.falcon
{
    internal class FalconTelemetryInfo : TelemetryInfo
    {
        private FalconData telemetryData;
        private FalconData lastTelemetryData;
        private Session session;

        public FalconTelemetryInfo(FalconData telemetryData, FalconData lastTelemetryData, Session session)
        {
            this.telemetryData = telemetryData;
            this.lastTelemetryData = lastTelemetryData;
            this.session = session;
        }


        private float PitchRate
        {
            get
            {
                telemetryData.pitchrate = (telemetryData.pitch - lastTelemetryData.pitch) / (telemetryData.time);
                return telemetryData.pitchrate;
            }
        }


        private float RollRate
        {
            get
            {
                telemetryData.rollrate= (telemetryData.roll - lastTelemetryData.roll) / (telemetryData.time);
                return (telemetryData.rollrate);
            }
        }


        private float YawRate
        {
            get
            {
                telemetryData.yawrate = (telemetryData.yaw - lastTelemetryData.yaw) / (telemetryData.time);
                return (telemetryData.yawrate);
            }
        }

        private float Roll
        {
            get
            {
                return (float)(Math.Sin(telemetryData.roll)*Math.Cos(telemetryData.pitch));
            }
        }

        private float Pitch
        {
            get
            {
                return (float)(Math.Sin(telemetryData.pitch));
            }
        }

        private float Yaw
        {
            get
            {
                return (float)(Math.Sin(telemetryData.yaw));
            }
        }


        public TelemetryValue TelemetryValueByName(string name)
        {
            TelemetryValue tv;
            switch (name)
            {
                case "roll":
                    tv = new FalconTelemetryValue("roll", Roll);
                    break;

                case "pitch":
                    tv = new FalconTelemetryValue("pitch", Pitch);
                    break;

                case "yaw":
                    tv = new FalconTelemetryValue("yaw", Yaw);
                    break;

                case "rollrate":
                    tv = new FalconTelemetryValue("rollrate", RollRate);
                    break;

                case "pitchrate":
                    tv = new FalconTelemetryValue("pitchrate", PitchRate);
                    break;

                case "yawrate":
                    tv = new FalconTelemetryValue("yawrate", YawRate);
                    break;


                default:
                    object data;
                    Type eleDataType = typeof(FalconData);
                    PropertyInfo propertyInfo;
                    FieldInfo fieldInfo = eleDataType.GetField(name);
                    if (fieldInfo != null)
                    {
                        data = fieldInfo.GetValue(telemetryData);
                    }
                    else if ((propertyInfo = eleDataType.GetProperty(name)) != null)
                    {
                        data = propertyInfo.GetValue(telemetryData, null);
                    }
                    else
                    {
                        throw new UnknownTelemetryValueException(name);
                    }
                    tv = new FalconTelemetryValue(name, data);
                    object value = tv.Value;
                    if (value == null)
                    {
                        throw new UnknownTelemetryValueException(name);
                    }

                    break;
            }

            return tv;
        }

    }
}