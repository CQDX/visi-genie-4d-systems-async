// Copyright(c) 2016 Michael Dorough
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

using ViSiGenie4DSystems.Async.Enumeration;
using ViSiGenie4DSystems.Async.Message;
using ViSiGenie4DSystems.Async.Specification;
using ViSiGenie4DSystems.Async.Event;

namespace ViSiGenie4DSystems.Async.SerialComm
{
    /// <summary>
    /// A plurality of async tasks that deal with a serial device instance.
    /// This class is carries out the essential I/O requirements needed to 
    /// communicate with one 4D Systems Display.
    /// </summary>
    public sealed class SerialDeviceDisplay
    {
        #region Construction
        /// <summary>
        /// Default constructor
        /// </summary>
        public SerialDeviceDisplay()
        {
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

            //Cancelation of tasks...
            this.ReportEventMessageCancellationTokenSource = new CancellationTokenSource();
            this.ReportObjectStatusMessageCancellationTokenSource = new CancellationTokenSource();
            this.ReceiveCancellationTokenSource = new CancellationTokenSource();
        }
        #endregion

        #region Cancellation 
        /// <summary>
        /// Manage life of task named ReportEventMessageCancellationTokenSource
        /// </summary>
        public CancellationTokenSource ReportEventMessageCancellationTokenSource { get; set; }

        /// <summary>
        /// Manage life of task named ReportObjectStatusMessageCancellationTokenSource
        /// </summary>
        public CancellationTokenSource ReportObjectStatusMessageCancellationTokenSource { get; set; }

        /// <summary>
        /// Manage life of task named ReceiveCancellationTokenSource
        /// </summary>
        public CancellationTokenSource ReceiveCancellationTokenSource { get; set; }
        #endregion

        #region Implementation Properties
        /// <summary>
        /// UWP resource that provides serial device communications
        /// </summary>
        private SerialDevice SerialDevice { get; set; }

        /// <summary>
        /// The response received from display was successfull
        /// </summary>
        private readonly byte ACK;

        /// <summary>
        /// The response received from the display was unsuccessfull
        /// </summary>
        private readonly byte NAK;

        /// <summary>
        /// Synchronize one and only SEND of N bytes to the display.
        /// </summary>
        private SemaphoreSlim SendSemaphore { get; set; }

        /// <summary>
        /// Concurrent FIFO of responses received from display
        /// </summary>
        private ConcurrentQueue<byte> AckNakQueue { get; set; }

        /// <summary>
        /// The default collection type for BlockingCollection<T> is ConcurrentQueue<T>.
        /// Concurrent FIFO of ReportEventMessage objects received from display.
        /// </summary>
        private ConcurrentQueue<ReportEventMessage> ReportEventMessageQueue { get; set; }

        /// <summary>
        /// The default collection type for BlockingCollection<T> is ConcurrentQueue<T>
        /// Concurrent FIFO of ReportObjectStatusMessage objects received from display
        /// </summary>
        private ConcurrentQueue<ReportObjectStatusMessage> ReportObjectStatusMessageQueue { get; set; }

        #endregion

        #region Connect to Serial Device
        /// <summary>
        /// Connects the serial device to the 4D Systems display per client specified
        /// DeviceInformation unique Id and the 4D Display's project PortDef (Baud ect...)
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
        /// A task sending 1 to N byte(s) to the 4D Systems display via this serial device implementation.
        /// </summary>
        /// <param name="sendMessage"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Acknowledgement> Send(byte[] sendMessage, CancellationToken cancellationToken)
        {
            Acknowledgement acknowledgement = Acknowledgement.NAK;
            try
            {
                //Wait to enter semaphore (in process)
                await this.SendSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false); ;
               
                Task<uint> sendBytesTask = this.SendBytes(sendMessage, cancellationToken);
                Task<Acknowledgement> dequeueResponseTask = this.DequeueResponse(cancellationToken);
                Task.WaitAll(sendBytesTask, dequeueResponseTask);

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
        /// An implementation task that works on sending bytes out to the 4D Display.
        /// See IChecksum interface, which enforces that all Message object know how 
        /// express their physical byte[] representation.
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
        /// A task for receiving messages from the 4D Display 
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
        /// then enqueues to approapriate ACK, NACK, REPORT_EVENT or REPORT_OBJ queue.
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


        #region EVENT DEQUEUE
        public async Task DequeueReportEventMessages(DisplayableEvent eventHandlerContainer, CancellationToken cancellationToken)
        {
            Debug.WriteLine("Entering DequeueReportEventMessages Task");
            await Task.Delay(5);

            //Listen for Report Event Messages until told to cancel this task
            while (cancellationToken.IsCancellationRequested == false)
            {
                //TRY DEQUEUE NEXT ReportEventMessage
                //Take blocks until item is available to be removed or the token is canceled
                ReportEventMessage dequeuedReportEventMessage;
                if (this.ReportEventMessageQueue.TryDequeue(out dequeuedReportEventMessage))
                {
                    await eventHandlerContainer.Raise(dequeuedReportEventMessage);
                }
            }

            Debug.WriteLine("Exiting DequeueReportEventMessages Task");
        }

        public async Task DequeueReportObjectStatusMessages(DisplayableEvent eventHandlerContainer, CancellationToken cancellationToken)
        {
            Debug.WriteLine("Entering DequeueReportObjectStatusMessages Task");
            await Task.Delay(5); //Otherwise consumes thread

            //Listen for Report Event Messages until told to cancel this task
            while (cancellationToken.IsCancellationRequested == false)
            { 
                //TRY DEQUEUE NEXT ReportEventMessage
                ReportObjectStatusMessage dequeuedReportObjectStatusMessage;
                if (this.ReportObjectStatusMessageQueue.TryDequeue(out dequeuedReportObjectStatusMessage))
                { 
                    await eventHandlerContainer.Raise(dequeuedReportObjectStatusMessage);
                }
            }

            Debug.WriteLine("Exiting DequeueReportObjectStatusMessages Task");
        }
        #endregion

        #region DEQUEUE ACK OR NAK		
        /// <summary>
        /// A task to dequeue a previously received ACK or NAK from the 4D Display.
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
