// Copyright(c) 2016 Michael Dorough
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

//using System.Reactive.Subjects; //FUTURE MIGRATION PLAN FOR THIS CODE

using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

using ViSiGenie4DSystems.Async.Enumeration;
using ViSiGenie4DSystems.Async.Message;
using ViSiGenie4DSystems.Async.Specification;

using ViSiGenie4DSystems.Async.Event;

namespace ViSiGenie4DSystems.Async.SerialComm
{
    /// <summary>.
    /// This class is carries out the essential I/O requirements needed to 
    /// communicate with one 4D Systems brand display.
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

            this.ACK = (int)Acknowledgement.ACK;
            this.NAK = (int)Acknowledgement.NAK;

            //See Connect method
            this.SerialDevice = null;

            //The initial and maximum number of requests that can be granted concurrently when sending data to the display.
            this.SendSemaphore = new SemaphoreSlim(1, 1);

            //Queues...
            this.AckNakQueue = new ConcurrentQueue<byte>();
            this.ReportEventMessageQueue = new ConcurrentQueue<ReportEventMessage>();
            this.ReportObjectStatusMessageQueue = new ConcurrentQueue<ReportObjectStatusMessage>();
            this.ReportMagicBytesMessageQueue = new ConcurrentQueue<ReportMagicBytesMessage>();
            this.ReportMagicDoubleBytesMessageQueue = new ConcurrentQueue<ReportMagicDoubleBytesMessage>();

            //Cancelation of tasks...
            this.ReportEventMessageCancellationTokenSource = new CancellationTokenSource();
            this.ReportObjectStatusMessageCancellationTokenSource = new CancellationTokenSource();
            this.ReportMagicBytesMessageCancellationTokenSource = new CancellationTokenSource();
            this.ReportMagicDoubleBytesMessageCancellationTokenSource =  new CancellationTokenSource();
            this.ReceiveCancellationTokenSource = new CancellationTokenSource();

            //Report events received from display...
            this.ReportEventMessageSubscriptions = new ReportSubscriptions();
            this.ReportObjectStatusMessageSubscriptions =new ReportSubscriptions();
            this.ReportMagicBytesMessageSubscriptions = new ReportSubscriptions();
            this.ReportMagicDoubleBytesMessageSubscriptions = new ReportSubscriptions();

            //Reactive Extenstions to support client subscriptions

            /* WAITING FOR RX-MAIN UWP RELEASE. CURRENTLY 2.3.0-beta2 	
            this.reportEventMessageReceived = new Subject<ReportEventMessage>();
            this.reportObjectStatusMessageReceived = new Subject<ReportObjectStatusMessage>();
            this.reportMagicBytesMessageReceived = new Subject<ReportMagicBytesMessage>();
            this.reportMagicDoubleBytesMessageReceived = new Subject<ReportMagicDoubleBytesMessage>();
            */
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
        /// The response received from display was successfull
        /// </summary>
        private readonly byte ACK;

        /// <summary>
        /// The response received from the display was unsuccessfull
        /// </summary>
        private readonly byte NAK;

        /// <summary>
        /// Synchronize one and only send message of N bytes to the display.
        /// </summary>
        private SemaphoreSlim SendSemaphore { get; set; }

        /// <summary>
        /// Concurrent FIFO representation of inbound responses received from the connected display
        /// </summary>
        private ConcurrentQueue<byte> AckNakQueue { get; set; }

        /// <summary>
        /// The default collection type for BlockingCollection<T> is ConcurrentQueue<T>.
        /// Concurrent FIFO representation of ReportEventMessage objects received from the connected display.
        /// </summary>
        private ConcurrentQueue<ReportEventMessage> ReportEventMessageQueue { get; set; }

        /// <summary>
        /// The default collection type for BlockingCollection<T> is ConcurrentQueue<T>
        /// Concurrent FIFO representatioin of ReportObjectStatusMessage objects received from the connected display.
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

        #region REACTIVE EXTENSION FOR REPORT EVENTS

        /* WAITING FOR RX-MAIN UWP RELEASE. CURRENTLY 2.3.0-beta2 	

        private Subject<ReportEventMessage> reportEventMessageReceived;
        public IObservable<ReportEventMessage> ReportEventMessageReceived
        {
            get
            {
                return this.reportEventMessageReceived;
            }
        }

        private Subject<ReportObjectStatusMessage> reportObjectStatusMessageReceived;
        public IObservable<ReportObjectStatusMessage> ReportObjectStatusMessageReceived
        {
            get
            {
                return this.reportObjectStatusMessageReceived;
            }
        }

        private Subject<ReportMagicBytesMessage> reportMagicBytesMessageReceived;
        public IObservable<ReportMagicBytesMessage> ReportMagicBytesMessageReceived
        {
            get
            {
                return this.reportMagicBytesMessageReceived;
            }
        }

        /// <summary>
        /// Implementation to support Rx for ReportMagicDoubleBytesMessage
        /// </summary>
        private Subject<ReportMagicDoubleBytesMessage> reportMagicDoubleBytesMessageReceived;

        /// <summary>
        /// Reactive extention (Rx) is used to ensures firing order and strong typing of ReportMagicBytesMessage that can be observed through subscription.
        /// </summary>
        /// <returns>Returns a <see cref="IObservable<ReportMagicDoubleBytesMessage>"/></returns>
        public IObservable<ReportMagicDoubleBytesMessage> ReportMagicDoubleBytesMessageReceived
        {
            get
            {
                return this.reportMagicDoubleBytesMessageReceived;
            }
        }

        */
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
                Debug.WriteLine(String.Format("Connect threw: ", ex.Message));
                Debug.WriteLine(ex.StackTrace);

                throw ex;
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
            Acknowledgement acknowledgement = Acknowledgement.NAK;
            try
            {
                //Wait to enter semaphore, which is an in-process type semaphore
                await this.SendSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false); ;
               
                Task<uint> sendBytesTask = this.SendBytes(sendMessage, cancellationToken);
                Task<Acknowledgement> dequeueResponseTask = this.DequeueResponse(cancellationToken);
                Task.WaitAll(sendBytesTask, dequeueResponseTask);

                //Good debug point here...
                //Debug.WriteLine(string.Format("WROTE {0} BYTES", sendBytesTask.Result));  

                acknowledgement = dequeueResponseTask.Result;
            }
            catch (OperationCanceledException oce)
            {
                Debug.WriteLine(String.Format("Send caught {0}", oce.Message));
                throw oce;
            }
            catch (ObjectDisposedException ode)
            {
                Debug.WriteLine(String.Format("Send caught {0}", ode.Message));
                Debug.WriteLine(ode.StackTrace);
                throw ode;
            }
            catch (SemaphoreFullException sfe)
            {
                Debug.WriteLine(String.Format("SendSemaphore threw {0}", sfe.Message));
                Debug.WriteLine(sfe.StackTrace);
                throw sfe;
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
                Task<UInt32> storeAsyncTask = dataWriter.StoreAsync().AsTask();
                bytesWrote = await storeAsyncTask;
            }
            finally
            {
                if (dataWriter != null)
                {
                    dataWriter.DetachStream();
                }
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
            await Task.Delay(5);
            try
            {
                while (cancellationToken.IsCancellationRequested == false)
                {
                    await this.ParseReceivedMessage(cancellationToken);                  
                }
            }
            catch (OperationCanceledException oce)
            {
                Debug.WriteLine(String.Format("Receive caught {0}", oce.Message));
            }
            catch (ObjectDisposedException ode)
            {
                Debug.WriteLine(String.Format("Receive caught {0}", ode.Message));
                Debug.WriteLine(ode.StackTrace);
                throw ode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(String.Format("Receive caught {0}", ex.Message));
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
        /// then enqueues to approapriate type of queue.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task ParseReceivedMessage(CancellationToken cancellationToken)
        {
            const uint readBufferLength = 1;
            byte[] peakByte = await ReadBytes(readBufferLength, cancellationToken);
            if (peakByte != null)
            {
                if (peakByte[0] == ACK)
                {
                    //Debug.WriteLine(String.Format("RX ACK 0x{0}: ", peakByte[0].ToString("X2")));

                    this.AckNakQueue.Enqueue(peakByte[0]);
                }
                else if (peakByte[0] == NAK)
                {
                    //Debug.WriteLine(String.Format("RX NACK 0x{0}: ", peakByte[0].ToString("X2")));

                    this.AckNakQueue.Enqueue(peakByte[0]);
                }
                else if (peakByte[0] == ((byte)Command.REPORT_EVENT))
                {
                    const uint readRestOfReportEvent = 5;
                    byte[] moreBytes = await this.ReadBytes(readRestOfReportEvent, cancellationToken);
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
                else if (peakByte[0] == ((byte)Command.REPORT_OBJ))
                {
                    const uint readRestOfReportObjectStatusMessage = 5;
                    byte[] moreBytes = await this.ReadBytes(readRestOfReportObjectStatusMessage, cancellationToken);
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
                else if (peakByte[0] == ((byte)Command.WRITE_MAGIC_EVENT_BYTES)) //CMD
                {
                    const uint readRestOfReportMagicMessage = 2; 
                    //GET OBJECT-INDEX + LENGTH
                    byte[] descriptorBytes = await this.ReadBytes(readRestOfReportMagicMessage, cancellationToken);
                    if (descriptorBytes != null)
                    {
                        int objectIndex = descriptorBytes[0];
                        uint magicByteLength = descriptorBytes[1];
                        //GET BYTES
                        byte[] magicBytes = await this.ReadBytes(magicByteLength, cancellationToken);
                        if (magicBytes != null)
                        {
                            //GET CHECKSUM
                            byte[] checkSumByte = await this.ReadBytes(1, cancellationToken);
                            if (checkSumByte != null)
                            {
                                var bytesReport = new ReportMagicBytesMessage(objectIndex, (int)magicByteLength, magicBytes, checkSumByte[0]);
                                this.ReportMagicBytesMessageQueue.Enqueue(bytesReport);
                            }
                        }
                    }
                }
                else if (peakByte[0] == ((byte)Command.WRITE_MAGIC_EVENT_DBYTES)) //CMD
                {
                    const uint readRestOfReportMagicMessage = 2;
                    //GET OBJECT-INDEX + LENGTH
                    byte[] descriptorBytes = await this.ReadBytes(readRestOfReportMagicMessage, cancellationToken);
                    if (descriptorBytes != null)
                    {
                        int objectIndex = descriptorBytes[0];
                        uint magicByteLength = descriptorBytes[1];
                        //GET BYTES
                        byte[] doubleMagicBytes = await this.ReadBytes(magicByteLength, cancellationToken);
                        if (doubleMagicBytes != null)
                        {
                            //GET CHECKSUM
                            byte[] checkSumByte = await this.ReadBytes(1, cancellationToken);
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
                    Debug.WriteLine(String.Format("RX ? 0x{0}", peakByte[0].ToString("X2")));
                }
            }
        }

        /// <summary>
        /// A supporting task that reads N bytes async from the 4D Display. 
        /// This task will throw cancelation since this is the lowest level read tasking.
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

                dataReaderObject = new DataReader(this.SerialDevice.InputStream);

                // Specify an asynchronous read operation when one or more bytes is available
                dataReaderObject.InputStreamOptions = InputStreamOptions.Partial;

                // Create a task object to wait for data on the serialPort.InputStream
                Task<UInt32> loadAsyncTask = dataReaderObject.LoadAsync(readBufferLength).AsTask(cancellationToken);

                UInt32 bytesRead = await loadAsyncTask;

                if (bytesRead == readBufferLength)
                {
                    bytes = new byte[readBufferLength];
                    dataReaderObject.ReadBytes(bytes);
                }
            }
            finally
            {
                // Cleanup once complete
                if (dataReaderObject != null)
                {
                    dataReaderObject.DetachStream();
                }
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
            await Task.Delay(5); //Otherwise consumes thread

            //await Task.Run( () =>
            //{
            while (cancellationToken.IsCancellationRequested == false)
            {
                //TRY DEQUEUE
                ReportEventMessage dequeuedReportEventMessage;
                bool status = this.ReportEventMessageQueue.TryDequeue(out dequeuedReportEventMessage);
                if (status)
                {
                    await this.ReportEventMessageSubscriptions.Raise(dequeuedReportEventMessage);

                    //WAITING FOR RX-MAIN UWP RELEASE. CURRENTLY 2.3.0-beta2 
                    //this.reportEventMessageReceived.OnNext(dequeuedReportEventMessage);
                }
            }
            //}
            //});

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
            await Task.Delay(5); //Otherwise consumes thread

            //await Task.Run( () =>
            //{
            while (cancellationToken.IsCancellationRequested == false)
            { 
                //TRY DEQUEUE
                ReportObjectStatusMessage dequeuedReportObjectStatusMessage;
                bool status = this.ReportObjectStatusMessageQueue.TryDequeue(out dequeuedReportObjectStatusMessage);
                if (status)
                {
                    await this.ReportObjectStatusMessageSubscriptions.Raise(dequeuedReportObjectStatusMessage);

                    //WAITING FOR RX-MAIN UWP RELEASE. CURRENTLY 2.3.0-beta2 
                    //this.reportObjectStatusMessageReceived.OnNext(dequeuedReportObjectStatusMessage);
                }
            }
            //});

            Debug.WriteLine("Exiting DequeueReportObjectStatusMessages Task");
        }

        public async Task DequeueReportMagicBytesMessages(CancellationToken cancellationToken)
        {
            Debug.WriteLine("Entering DequeueReportMagicBytesMessages Task");
            await Task.Delay(5); //Otherwise consumes thread

            //await Task.Run( () =>
            //{
            while (cancellationToken.IsCancellationRequested == false)
            {
                //TRY DEQUEUE
                ReportMagicBytesMessage dequeuedReportMagicBytesMessage;
                bool status = this.ReportMagicBytesMessageQueue.TryDequeue(out dequeuedReportMagicBytesMessage);
                if (status)
                {
                    await this.ReportMagicBytesMessageSubscriptions.Raise(dequeuedReportMagicBytesMessage);

                    //WAITING FOR RX-MAIN UWP RELEASE. CURRENTLY 2.3.0-beta2 
                    //this.reportMagicBytesMessageReceived.OnNext(dequeuedReportMagicBytesMessage);
                }
            }
           // });

            Debug.WriteLine("Exiting DequeueReportMagicBytesMessages Task");
        }

        public async Task DequeueReportMagicDoubleBytesMessages(CancellationToken cancellationToken)
        {
            Debug.WriteLine("Entering ReportMagicDoubleBytesMessages Task");
            await Task.Delay(5); //Otherwise consumes thread

            //await Task.Run( () =>
            //{
            while (cancellationToken.IsCancellationRequested == false)
            {
                //TRY DEQUEUE
                ReportMagicDoubleBytesMessage dequeuedReportDoubleMagicBytesMessage;
                bool status = this.ReportMagicDoubleBytesMessageQueue.TryDequeue(out dequeuedReportDoubleMagicBytesMessage);
                if (status)
                {
                    await this.ReportMagicDoubleBytesMessageSubscriptions.Raise(dequeuedReportDoubleMagicBytesMessage);

                    //WAITING FOR RX-MAIN UWP RELEASE. CURRENTLY 2.3.0-beta2 	
                    //this.reportMagicDoubleBytesMessageReceived.OnNext(dequeuedReportDoubleMagicBytesMessage);
                }
            }
            //});

            Debug.WriteLine("Exiting DequeueReportMagicDoubleBytesMessagesTask");
        }
        #endregion

        #region DEQUEUE ACK OR NAK		
        /// <summary>
        /// Takes first and returns acknowledgent.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<Acknowledgement> DequeueResponse(CancellationToken cancellationToken)
        {
            Acknowledgement acknowledgement = Acknowledgement.NAK; 
            await Task.Run(() =>
            {
                byte response;
                //Block until ACK or NAK until told to cancel this task
                if ( this.AckNakQueue.TryDequeue(out response) )
                {
                    if (response == (int)Acknowledgement.ACK)
                    {
                        Debug.WriteLine(String.Format("RX ACK 0x{0}", response.ToString("X2")));

                        acknowledgement = Acknowledgement.ACK;
                    }
                    else if (response == (int)Acknowledgement.NAK)
                    {
                        //SOME ISSUE WITH 4D DISPLAY BUT PROCEED... 
                        Debug.WriteLine(String.Format("RX NAK 0x{0} AFTER 500 mS WAIT", response.ToString("X2")));

                        acknowledgement = Acknowledgement.NAK;
                    }
                    else
                    {
                        Debug.WriteLine("RX TIMEOUT");
                        acknowledgement = Acknowledgement.TIMEOUT;
                    }
                }                       
            });

            return acknowledgement;
        }
        #endregion
    }
}
