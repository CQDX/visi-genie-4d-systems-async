// Copyright(c) 2016 Michael Dorough
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ViSiGenie4DSystems.Async.Enumeration;
using ViSiGenie4DSystems.Async.Event;
using ViSiGenie4DSystems.Async.Message;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace ViSiGenie4DSystems.Async.SerialComm
{
    /// <summary>.
    /// This class is carries out the essential I/O requirements needed to 
    /// communicate with one 4D Systems display.
    /// </summary>
    public sealed class SerialDeviceDisplay
    {
        #region Construction
        /// <summary>
        /// Default constructor
        /// </summary>
        public SerialDeviceDisplay()
        {
            this.AreEventListenerTasksRunning = false;

            this._ack = (int)Acknowledgement.Ack;
            this._nak = (int)Acknowledgement.Nak;

            //See Connect method
            this.SerialDevice = null;

            //The initial and maximum number of requests that can be granted concurrently when sending data to the display.
            this.SendSemaphore = new SemaphoreSlim(1, 1);

            //Queues
            this.AckNakQueue = new ConcurrentQueue<byte>();
            this.ReportEventMessageQueue = new ConcurrentQueue<ReportEventMessage>();
            this.ReportObjectStatusMessageQueue = new ConcurrentQueue<ReportObjectStatusMessage>();
            this.ReportMagicBytesMessageQueue = new ConcurrentQueue<ReportMagicBytesMessage>();
            this.ReportMagicDoubleBytesMessageQueue = new ConcurrentQueue<ReportMagicDoubleBytesMessage>();

            //Cancellation of tasks
            this.ReportEventMessageCancellationTokenSource = new CancellationTokenSource();
            this.ReportObjectStatusMessageCancellationTokenSource = new CancellationTokenSource();
            this.ReportMagicBytesMessageCancellationTokenSource = new CancellationTokenSource();
            this.ReportMagicDoubleBytesMessageCancellationTokenSource =  new CancellationTokenSource();
            this.ReceiveCancellationTokenSource = new CancellationTokenSource();

            //Report events received from display
            this.ReportEventMessageSubscriptions = new ReportSubscriptions();
            this.ReportObjectStatusMessageSubscriptions =new ReportSubscriptions();
            this.ReportMagicBytesMessageSubscriptions = new ReportSubscriptions();
            this.ReportMagicDoubleBytesMessageSubscriptions = new ReportSubscriptions();
        }
        #endregion

        #region CANCELLATION
        public CancellationTokenSource ReportEventMessageCancellationTokenSource { get; set; }
        public CancellationTokenSource ReportObjectStatusMessageCancellationTokenSource { get; set; }
        public CancellationTokenSource ReportMagicBytesMessageCancellationTokenSource { get; set; }
        public CancellationTokenSource ReportMagicDoubleBytesMessageCancellationTokenSource { get; set; }
        public CancellationTokenSource ReceiveCancellationTokenSource { get; set; }
        #endregion

        #region PROPERTIES
        /// <summary>
        /// UWP resource that provides serial device communications
        /// </summary>
        private SerialDevice SerialDevice { get; set; }

        /// <summary>
        /// Indicates if listener tasks are up and running.
        /// This prevents client from sending a message without first setting up their subscriptions
        /// </summary>
        public bool AreEventListenerTasksRunning { get; set; }

        /// <summary>
        /// The response received from display was successful
        /// </summary>
        private readonly byte _ack;

        /// <summary>
        /// The response received from the display was unsuccessful
        /// </summary>
        private readonly byte _nak;

        /// <summary>
        /// Synchronize one and only send message of N bytes to the display.
        /// </summary>
        private SemaphoreSlim SendSemaphore { get; }

        /// <summary>
        /// Concurrent FIFO representation of inbound responses received from the connected display
        /// </summary>
        private ConcurrentQueue<byte> AckNakQueue { get; }

        /// <summary>
        /// The default collection type for BlockingCollection<T> is ConcurrentQueue<T>.
        /// Concurrent FIFO representation of ReportEventMessage objects received from the connected display.
        /// </summary>
        private ConcurrentQueue<ReportEventMessage> ReportEventMessageQueue { get; set; }

        /// <summary>
        /// The default collection type for BlockingCollection<T> is ConcurrentQueue<T>
        /// Concurrent FIFO representation of ReportObjectStatusMessage objects received from the connected display.
        /// </summary>
        private ConcurrentQueue<ReportObjectStatusMessage> ReportObjectStatusMessageQueue { get; set; }

        private ConcurrentQueue<ReportMagicBytesMessage> ReportMagicBytesMessageQueue { get; set; }

        private ConcurrentQueue<ReportMagicDoubleBytesMessage> ReportMagicDoubleBytesMessageQueue { get; set; }
        #endregion

        #region EVENTS SUBSCRIPTIONS
        public ReportSubscriptions ReportEventMessageSubscriptions { get; set; }
        public ReportSubscriptions ReportObjectStatusMessageSubscriptions { get; set; }
        public ReportSubscriptions ReportMagicBytesMessageSubscriptions { get; set; }
        public ReportSubscriptions ReportMagicDoubleBytesMessageSubscriptions { get; set; }
        #endregion

        #region Connect to Serial Device
        /// <summary>
        /// Connects the serial device to the 4D Systems display per client specified
        /// DeviceInformation unique Id and the 4D Display's project PortDef, which specifies Baud rate.
        /// </summary>
        /// <param name="deviceInformationId"></param>
        /// <param name="portDef"></param>
        /// <returns></returns>
        public async Task Connect(string deviceInformationId, PortDef portDef)
        {
            try
            {
                //IMPORTANT: For accessing the serial port, you must add the DeviceCapability to the Package.appxmanifest file in your project.
                //This applies to Headless and Headed Apps contained in this project solution set.
                //https://ms-iot.github.io/content/en-US/win10/samples/SerialSample.htm

                //https://social.msdn.microsoft.com/Forums/en-US/b9633593-377e-4d6f-b3a9-838de0555371/serialdevicefromidasync-always-returns-null-unless-the-serial-adapter-is-plugged-in-after-boot?forum=WindowsIoT
                //If SerialDevice is null then follow steps below to resolve:
                //Connect to the Windows 10 IoT Core device through PowerShell
                //Run the command  iotstartup remove headless ZWave
                //Reboot the device  shutdown /r /t 0
                this.SerialDevice = await SerialDevice.FromIdAsync(deviceInformationId);

                if (this.SerialDevice == null)
                {
                    Debug.WriteLine("SerialDevice is null. Check DeviceCapability in app.manifest.");
                    //will throw!
                }

                // Configure serial settings
                this.SerialDevice.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                this.SerialDevice.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                this.SerialDevice.BaudRate = (uint)portDef.BaudRate;
                this.SerialDevice.Parity = portDef.SerialParity;
                this.SerialDevice.StopBits = portDef.SerialStopBitCount;
                this.SerialDevice.DataBits = portDef.DataBits;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Connect threw: ");
                Debug.WriteLine(ex.StackTrace);

                throw;
            }
        }
        #endregion

        #region SEND MESSAGE FROM HOST TO DISPLAY 
        /// <summary>
        /// A task dedicated to sending 1 to N byte(s) from the host to 4D Systems display via this serial device implementation.
        /// </summary>
        /// <param name="sendMessage"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Acknowledgement> Send(byte[] sendMessage, CancellationToken cancellationToken)
        {
            Acknowledgement acknowledgement = Acknowledgement.Nak;
            try
            {
                //Wait to enter semaphore, which is an in-process type semaphore
                await this.SendSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false); ;
               
                Task<uint> sendBytesTask = this.SendBytes(sendMessage, cancellationToken);
                Task<Acknowledgement> dequeueResponseTask = this.DequeueResponse(cancellationToken);
                Task.WaitAll(sendBytesTask, dequeueResponseTask);

                //Good debug point here...
                //Debug.WriteLine(string.Format("WROTE {0} BYTES", sendBytesTask.Result));  

                acknowledgement = await dequeueResponseTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException oce)
            {
                Debug.WriteLine($"Send caught {oce.Message}");
                throw;
            }
            catch (ObjectDisposedException ode)
            {
                Debug.WriteLine($"Send caught {ode.Message}");
                Debug.WriteLine(ode.StackTrace);
                throw;
            }
            catch (SemaphoreFullException sfe)
            {
                Debug.WriteLine($"SendSemaphore threw {sfe.Message}");
                Debug.WriteLine(sfe.StackTrace);
                throw;
            }
            finally
            {
                /**** ALWAYS RELEASE TO LET NEXT SENDER IN ****/
                this.SendSemaphore.Release();
            }

            return acknowledgement;
        }

        /// <summary>
        /// An implementation task that is dedicated to sending byte packages out to the connected 4D Display.
        /// </summary>
        /// <param name="sendMessage"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<uint> SendBytes(byte[] sendMessage, CancellationToken cancellationToken)
        {
            DataWriter dataWriter = null;
            uint bytesWrote = 0;
            try
            {
                // If send task cancellation was requested, then comply
                cancellationToken.ThrowIfCancellationRequested();

                dataWriter = new DataWriter(this.SerialDevice.OutputStream);
                dataWriter.WriteBytes(sendMessage);
                Task<UInt32> storeAsyncTask = dataWriter.StoreAsync().AsTask(cancellationToken);
                bytesWrote = await storeAsyncTask.ConfigureAwait(false);
            }
            finally
            {
                dataWriter?.DetachStream();
            }
            return bytesWrote;
        }
        #endregion

        #region RECEIVE MESSAGE FROM DISPLAY TO HOST
        /// <summary>
        /// A task for receiving messages originating from the 4D Display. 
        /// For example, the user touches an object on the display. 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task Receive(CancellationToken cancellationToken)
        {       
            Debug.WriteLine("Entering Receive Task");
            await Task.Delay(5, cancellationToken).ConfigureAwait(false);
            try
            {
                while (cancellationToken.IsCancellationRequested == false)
                {
                    await this.ParseReceivedMessage(cancellationToken).ConfigureAwait(false);                  
                }
            }
            catch (OperationCanceledException oce)
            {
                Debug.WriteLine($"Receive caught {oce.Message}");
            }
            catch (ObjectDisposedException ode)
            {
                Debug.WriteLine($"Receive caught {ode.Message}");
                Debug.WriteLine(ode.StackTrace);
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Receive caught {ex.Message}");
            }
            finally
            {
                if (this.SerialDevice != null)
                {
                    this.SerialDevice.Dispose();
                    this.SerialDevice = null;
                }
            }
            Debug.WriteLine("Existing Receive Task");
        }

        /// <summary>
        /// A task that works on decoding bytes received from display and
        /// then enqueue to appropriate type of queue.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task ParseReceivedMessage(CancellationToken cancellationToken)
        {
            const uint readBufferLength = 1;
            byte[] peakByte = await ReadBytes(readBufferLength, cancellationToken).ConfigureAwait(false);
            if (peakByte != null)
            {
                if (peakByte[0] == _ack)
                {
                    //Debug.WriteLine(String.Format("RX ACK 0x{0}: ", peakByte[0].ToString("X2")));

                    this.AckNakQueue.Enqueue(peakByte[0]);
                }
                else if (peakByte[0] == _nak)
                {
                    //Debug.WriteLine(String.Format("RX NACK 0x{0}: ", peakByte[0].ToString("X2")));

                    this.AckNakQueue.Enqueue(peakByte[0]);
                }
                else if (peakByte[0] == ((byte)Command.ReportEvent))
                {
                    const uint readRestOfReportEvent = 5;
                    byte[] moreBytes = await this.ReadBytes(readRestOfReportEvent, cancellationToken).ConfigureAwait(false);
                    if (moreBytes != null)
                    {
                        byte[] rawReportEvent = new byte[6];
                        rawReportEvent[0] = peakByte[0];

                        rawReportEvent[1] = moreBytes[0];
                        rawReportEvent[2] = moreBytes[1];
                        rawReportEvent[3] = moreBytes[2];
                        rawReportEvent[4] = moreBytes[3];
                        rawReportEvent[5] = moreBytes[4];

                        //Debug.Write("RX RE ");
                        //foreach (var rre in rawReportEvent)
                        //{
                        //    Debug.Write(String.Format("0x{0} ", rre.ToString("X2")));
                        //}
                        //Debug.WriteLine("");

                        this.ReportEventMessageQueue.Enqueue(new ReportEventMessage(rawReportEvent));
                    }
                }
                else if (peakByte[0] == ((byte)Command.ReportObj))
                {
                    const uint readRestOfReportObjectStatusMessage = 5;
                    byte[] moreBytes = await this.ReadBytes(readRestOfReportObjectStatusMessage, cancellationToken).ConfigureAwait(false);
                    if (moreBytes != null)
                    {
                        byte[] rawReportObjectStatusMessage = new byte[6];
                        rawReportObjectStatusMessage[0] = peakByte[0];

                        rawReportObjectStatusMessage[1] = moreBytes[0];
                        rawReportObjectStatusMessage[2] = moreBytes[1];
                        rawReportObjectStatusMessage[3] = moreBytes[2];
                        rawReportObjectStatusMessage[4] = moreBytes[3];
                        rawReportObjectStatusMessage[5] = moreBytes[4];

                        //Debug.Write("RX RE ");
                        //foreach (var rrosm. in rawReportObjectStatusMessage)
                        //{
                        //    Debug.Write(String.Format("0x{0} ", rrosm.ToString("X2")));
                        //}
                        //Debug.WriteLine("");

                        this.ReportObjectStatusMessageQueue.Enqueue(new ReportObjectStatusMessage(rawReportObjectStatusMessage));
                    }
                }
                else if (peakByte[0] == ((byte)Command.WriteMagicEventBytes)) //CMD
                {
                    const uint readRestOfReportMagicMessage = 2; 
                    //GET OBJECT-INDEX + LENGTH
                    byte[] descriptorBytes = await this.ReadBytes(readRestOfReportMagicMessage, cancellationToken).ConfigureAwait(false);
                    if (descriptorBytes != null)
                    {
                        int objectIndex = descriptorBytes[0];
                        uint magicByteLength = descriptorBytes[1];
                        //GET BYTES
                        byte[] magicBytes = await this.ReadBytes(magicByteLength, cancellationToken).ConfigureAwait(false);
                        if (magicBytes != null)
                        {
                            //GET CHECKSUM
                            byte[] checkSumByte = await this.ReadBytes(1, cancellationToken).ConfigureAwait(false);
                            if (checkSumByte != null)
                            {
                                var bytesReport = new ReportMagicBytesMessage(objectIndex, (int)magicByteLength, magicBytes, checkSumByte[0]);
                                this.ReportMagicBytesMessageQueue.Enqueue(bytesReport);
                            }
                        }
                    }
                }
                else if (peakByte[0] == ((byte)Command.WriteMagicEventDbytes)) //CMD
                {
                    const uint readRestOfReportMagicMessage = 2;
                    //GET OBJECT-INDEX + LENGTH
                    byte[] descriptorBytes = await this.ReadBytes(readRestOfReportMagicMessage, cancellationToken).ConfigureAwait(false);
                    if (descriptorBytes != null)
                    {
                        int objectIndex = descriptorBytes[0];
                        uint magicByteLength = descriptorBytes[1];
                        //GET BYTES
                        byte[] doubleMagicBytes = await this.ReadBytes(magicByteLength, cancellationToken).ConfigureAwait(false);
                        if (doubleMagicBytes != null)
                        {
                            //GET CHECKSUM
                            byte[] checkSumByte = await this.ReadBytes(1, cancellationToken).ConfigureAwait(false);
                            if (checkSumByte != null)
                            {
                                var doubleBytesReport = new ReportMagicDoubleBytesMessage(objectIndex, (int)magicByteLength, doubleMagicBytes, checkSumByte[0]);
                                this.ReportMagicDoubleBytesMessageQueue.Enqueue(doubleBytesReport);
                            }
                        }
                    }
                }
                else
                {
                    Debug.WriteLine($"RX ? 0x{peakByte[0]:X2}");
                }
            }
        }

        /// <summary>
        /// A supporting task that reads N bytes async from the 4D Display. 
        /// This task will throw cancellation since this is the lowest level read tasking.
        /// </summary>
        /// <param name="readBufferLength"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<byte[]> ReadBytes(uint readBufferLength, CancellationToken cancellationToken)
        {
            byte[] bytes = null;
            DataReader dataReaderObject = null;
            try
            {
                // If read task cancellation was requested, then comply
                cancellationToken.ThrowIfCancellationRequested();

                dataReaderObject = new DataReader(this.SerialDevice.InputStream)
                {
                    InputStreamOptions = InputStreamOptions.Partial
                };

                // Specify an asynchronous read operation when one or more bytes is available

                // Create a task object to wait for data on the serialPort.InputStream
                Task<UInt32> loadAsyncTask = dataReaderObject.LoadAsync(readBufferLength).AsTask(cancellationToken);

                UInt32 bytesRead = await loadAsyncTask.ConfigureAwait(false);

                if (bytesRead == readBufferLength)
                {
                    bytes = new byte[readBufferLength];
                    dataReaderObject.ReadBytes(bytes);
                }
            }
            finally
            {
                // Cleanup once complete
                dataReaderObject?.DetachStream();
            }
            return bytes;
        }
        #endregion

        #region DEQUEUE DISPLAY EVENT AND FIRE SUBSCRIPTIONS
        /// <summary>
        /// Takes first and send to subscriber of ReportEventMessage objects.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task DequeueReportEventMessages(CancellationToken cancellationToken)
        {
            Debug.WriteLine("Entering DequeueReportEventMessages Task");
            await Task.Delay(5, cancellationToken).ConfigureAwait(false); //Otherwise consumes thread

            while (cancellationToken.IsCancellationRequested == false)
            {
                //TRY DEQUEUE
                bool status = this.ReportEventMessageQueue.TryDequeue(out var dequeuedReportEventMessage);
                if (status)
                {
                    await this.ReportEventMessageSubscriptions.Raise(dequeuedReportEventMessage).ConfigureAwait(false);

                    //WAITING FOR RX-MAIN UWP RELEASE. CURRENTLY 2.3.0-beta2 
                    //this.reportEventMessageReceived.OnNext(dequeuedReportEventMessage);
                }
            }

            Debug.WriteLine("Exiting DequeueReportEventMessages Task");
        }

        /// <summary>
        /// Takes first and send to subscriber of ReportObjectStatusMessage objects.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task DequeueReportObjectStatusMessages(CancellationToken cancellationToken)
        {
            Debug.WriteLine("Entering DequeueReportObjectStatusMessages Task");
            await Task.Delay(5, cancellationToken).ConfigureAwait(false); //Otherwise consumes thread

            while (cancellationToken.IsCancellationRequested == false)
            { 
                //TRY DEQUEUE
                bool status = this.ReportObjectStatusMessageQueue.TryDequeue(out var dequeuedReportObjectStatusMessage);
                if (status)
                {
                    await this.ReportObjectStatusMessageSubscriptions.Raise(dequeuedReportObjectStatusMessage).ConfigureAwait(false);

                    //WAITING FOR RX-MAIN UWP RELEASE. CURRENTLY 2.3.0-beta2 
                    //this.reportObjectStatusMessageReceived.OnNext(dequeuedReportObjectStatusMessage);
                }
            }

            Debug.WriteLine("Exiting DequeueReportObjectStatusMessages Task");
        }

        public async Task DequeueReportMagicBytesMessages(CancellationToken cancellationToken)
        {
            Debug.WriteLine("Entering DequeueReportMagicBytesMessages Task");
            await Task.Delay(5, cancellationToken).ConfigureAwait(false); //Otherwise consumes thread

            while (cancellationToken.IsCancellationRequested == false)
            {
                //TRY DEQUEUE
                bool status = this.ReportMagicBytesMessageQueue.TryDequeue(out var dequeuedReportMagicBytesMessage);
                if (status)
                {
                    await this.ReportMagicBytesMessageSubscriptions.Raise(dequeuedReportMagicBytesMessage).ConfigureAwait(false);

                    //WAITING FOR RX-MAIN UWP RELEASE. CURRENTLY 2.3.0-beta2 
                    //this.reportMagicBytesMessageReceived.OnNext(dequeuedReportMagicBytesMessage);
                }
            }

            Debug.WriteLine("Exiting DequeueReportMagicBytesMessages Task");
        }

        public async Task DequeueReportMagicDoubleBytesMessages(CancellationToken cancellationToken)
        {
            Debug.WriteLine("Entering ReportMagicDoubleBytesMessages Task");
            await Task.Delay(5, cancellationToken).ConfigureAwait(false); //Otherwise consumes thread

            while (cancellationToken.IsCancellationRequested == false)
            {
                //TRY DEQUEUE
                bool status = this.ReportMagicDoubleBytesMessageQueue.TryDequeue(out var dequeuedReportDoubleMagicBytesMessage);
                if (status)
                {
                    await this.ReportMagicDoubleBytesMessageSubscriptions.Raise(dequeuedReportDoubleMagicBytesMessage).ConfigureAwait(false);

                    //WAITING FOR RX-MAIN UWP RELEASE. CURRENTLY 2.3.0-beta2 	
                    //this.reportMagicDoubleBytesMessageReceived.OnNext(dequeuedReportDoubleMagicBytesMessage);
                }
            }

            Debug.WriteLine("Exiting DequeueReportMagicDoubleBytesMessagesTask");
        }
        #endregion

        #region DEQUEUE ACK OR NAK		
        /// <summary>
        /// Takes first and returns Acknowledgement.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<Acknowledgement> DequeueResponse(CancellationToken cancellationToken)
        {
            Acknowledgement acknowledgement = Acknowledgement.Nak; 
            await Task.Run(() =>
            {
                //Block until ACK or NAK until told to cancel this task
                if ( this.AckNakQueue.TryDequeue(out var response) )
                {
                    if (response == (int)Acknowledgement.Ack)
                    {
                        Debug.WriteLine($"RX ACK 0x{response.ToString($"X2")}");

                        acknowledgement = Acknowledgement.Ack;
                    }
                    else if (response == (int)Acknowledgement.Nak)
                    {
                        //SOME ISSUE WITH 4D DISPLAY BUT PROCEED... 
                        Debug.WriteLine($"RX NAK 0x{response.ToString($"X2")} AFTER 500 mS WAIT");

                        acknowledgement = Acknowledgement.Nak;
                    }
                    else
                    {
                        Debug.WriteLine("RX TIMEOUT");
                        acknowledgement = Acknowledgement.Timeout;
                    }
                }                       
            }, cancellationToken).ConfigureAwait(false);

            return acknowledgement;
        }
        #endregion
    }
}
