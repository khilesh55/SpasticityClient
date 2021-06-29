using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO.Ports;

namespace SpasticityClient
{
    public class XBeeData : IDisposable
    {
        #region public properties
        public bool IsCancelled { get; set; }
        #endregion

        // Initialize a serial port
        private SerialPort serialPort = null;

        #region construct

        public XBeeData(string portName)
        {
            serialPort = new SerialPort(portName, 57600, Parity.None, 8, StopBits.One);
        }

        #endregion

        // Dispose of the serial port
        public void Dispose()
        {
            Stop();
        }

        // Read from serial port
        public void Read(int keepRecords, ChartModel chartModel)
        {
            try
            {
                serialPort.Open();
                var nowstart = DateTime.Now;
                var remainHex = string.Empty;
                var packetRemainData = new List<string>();
                
                //
                var counter = 1;
                List<float> angVelArray = new List<float>();

                //infinite loop will keep running and adding to EMGInfo until IsCancelled is set to true
                while (IsCancelled == false)
                {
                    //Check if any bytes of data received in serial buffer
                    var totalbytes = serialPort.BytesToRead;
                    Thread.Sleep(30);

                    if (totalbytes > 0)
                    {
                        //Load all the serial data to buffer
                        var buffer = new byte[totalbytes];
                        serialPort.Read(buffer, 0, buffer.Length);

                        //convert bytes to hex to better visualize and parse. 
                        //TODO: it can be updated in the future to parse with byte to increase preformance if needed
                        var hexFull = BitConverter.ToString(buffer);

                        //remainhex is empty string
                        hexFull = remainHex + hexFull;

                        var packets = new List<XBeePacket>();

                        //remainHex is all that is left when legitimate packets have been added to packets
                        remainHex = XBeeFunctions.ParsePacketHex(hexFull.Split('-').ToList(), packets);

                        foreach (var packet in packets)
                        {
                            //Total transmitted data is [] byte long. 1 more byte should be checksum. prefixchar is the extra header due to API Mode
                            int prefixCharLength = 8;
                            int byteArrayLength = 16;
                            int checkSumLength = 1;
                            int totalExpectedCharLength = prefixCharLength + byteArrayLength + checkSumLength;
                            

                            //Based on above variables to parse data coming from SerialPort. Next fun is performed sequentially to all packets
                            var packetDatas = XBeeFunctions.ParseRFDataHex(packet.Data, packetRemainData, totalExpectedCharLength);

                            foreach (var packetData in packetDatas)
                            {
                                //Make sure it's 25 charactors long. It's same as the arduino receiver code for checking the length. This was previously compared to totalExpectedCharLength but looks like packetDatas - packetData only contains the data part anyway therefore compare to byteArrayLength
                                //Also modify data defn to be packetData itself
                                if (packetData.Count == (byteArrayLength))
                                {
                                    var data = packetData;

                                    #region Convert string to byte for later MSB and LSB combination- 16 bit to 8 bit

                                    #region Time
                                    //convert timestamp
                                    var TIME2MSB = Convert.ToByte(data[8], 16);
                                    var TIME2LSB = Convert.ToByte(data[9], 16);
                                    var TIME1MSB = Convert.ToByte(data[10], 16);
                                    var TIME1LSB = Convert.ToByte(data[11], 16);
                                    #endregion

                                    #region Force
                                    //convert force
                                    var FORMSB = Convert.ToByte(data[12], 16);
                                    var FORLSB = Convert.ToByte(data[13], 16);
                                    #endregion

                                    #region MSB LSB combination
                                    float elapsedTime = (long)((TIME2MSB & 0xFF) << 24 | (TIME2LSB & 0xFF) << 16 | (TIME1MSB & 0xFF) << 8 | (TIME1LSB & 0xFF));
                                    float force = (int)((FORMSB & 0xFF) << 8 | (FORLSB & 0xFF));
                                    #endregion

                                    #region Send data to chart model
                                    if (chartModel.ForceValues.Count > keepRecords)
                                    {
                                        chartModel.ForceValues.RemoveAt(0);
                                    }

                                    var nowticks = DateTime.Now;
                                    chartModel.SetAxisLimits(nowticks);
                                    chartModel.ForceValues.Add(new MeasureModel { DateTime = nowticks, Value = force });
                                    #endregion

                                    #region Send data to Excel collection
                                    chartModel.SessionDatas.Add(new SessionData
                                    {
                                        TimeStamp = (nowticks - nowstart).Ticks / 10000, //time since read start in ms
                                        Force = force
                                    }); ;
                                    #endregion

                                    counter++;
                                    Thread.Sleep(30);
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                Stop();
            }
        }

        // Stop reading
        public void Stop()
        {
            if (serialPort != null)
            {
                try
                {
                    serialPort.Close();
                    serialPort.Dispose();
                }
                catch
                {
                }
            }
        }
    }
}
#endregion