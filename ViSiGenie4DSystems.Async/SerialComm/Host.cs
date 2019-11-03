// Copyright(c) 2016 Michael Dorough
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ViSiGenie4DSystems.Async.Enumeration;
using ViSiGenie4DSystems.Async.Event;
using ViSiGenie4DSystems.Async.Message;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;

namespace ViSiGenie4DSystems.Async.SerialComm
{
    /// <summary>
    /// Enabled a client application to communicate with one or more 4D System display(s) connected via a serial device. 
    /// </summary>
    /// <remarks>
    /// <see cref="Host"/> enables the client app to send outbound messages to the display and sets up optional subscriptions to 4D Systems Visi Genie inbound Reports. 
    /// A Silicon Labs CP2102 USB to Serial UART Bridge Converter Cable is used to connected the host USB port to the display's backside 5 pins connector.
    /// </remarks>
    public sealed class Host
    {
        #region SINGLETON CONSTRUCTION
        /// <summary>
        /// Singleton initialization
        /// </summary>
        private static readonly Lazy<Host> VeryLazy = new Lazy<Host>(() => new Host());

        /// <summary>
        /// The single instance of the Host object.
        /// </summary>
        public static Host Instance => VeryLazy.Value;
        /// <summary>
        /// Initializes the one and only <see cref="Host"/>.
        /// </summary>
        private Host()
        {
            this.SerialDeviceDisplays = new Dictionary<string, SerialDeviceDisplay>(); 
            Debug.WriteLine("Created Host");
        }
        #endregion

        #region DEVICE DISCOVERY
        /// <summary>
        /// Finds 1 to N connected 4D Systems display modules. 
        /// Each display found is identified by a string identifier. 
        /// The client app must retain this string identifier in order to use the other class methods.
        /// IMPORTANT TODO: 
        /// The client application must add the DeviceCapability to their project's Package.appxmanifest file.
        /// Capability must be defined only once in your app's Package.appxmanifest as follows:
        /// <Capabilities>
        ///     <DeviceCapability Name="serialcommunication">
        ///         <Device Id = "any" >
        ///             <Function Type="name:serialPort" />
        ///         </Device>
        ///     </DeviceCapability>
        /// </Capabilities>
        /// </summary>
        /// <returns>
        /// Returns a <see cref="Task{List{string}}"/> that contains one or more device indentifiers that logically represent serial device displays.
        /// </returns>
        public async Task<List<string>> DiscoverDeviceIds()
        {
            this.SerialDeviceDisplays.Clear();

            string advancedQuerySyntax = SerialDevice.GetDeviceSelector();
            DeviceInformationCollection deviceInformationCollection = await DeviceInformation.FindAllAsync(advancedQuerySyntax);

            var ids = new List<string>();
            foreach (var di in deviceInformationCollection)
            {
                ids.Add(di.Id);

                var device = new SerialDeviceDisplay();

                //Remember each id, as this will be the token for client to use on the adjacent public methods in this class
                this.SerialDeviceDisplays.Add(di.Id, device);
            }

            return ids;
        }
        #endregion

        #region DEVICE LIFETIME
        /// <summary>
        /// Given a deviceID and client port definition, the host is connected to a particular serial device display.
        /// 
        /// PRECONDITION: DiscoverDeviceIds() was successful
        /// 
        /// </summary>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display.
        /// </param>
        /// <param name="portDef">
        /// A <see cref="PortDef"/> baud rate that matches the related Workshop 4 project's baud rate.
        /// A mismatched baud rate will result in failed communication between the host and the display.
        /// </param>
        /// <returns>Returns a <see cref="Task"/></returns>
        public async Task Connect(string deviceId, PortDef portDef)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                Debug.WriteLine($"Host.Instance.Connect was unable to connect to display using deviceId {deviceId}");
                throw new NullReferenceException();
            }
            await serialDeviceDisplay.Connect(deviceId, portDef).ConfigureAwait(false);
        }

        /// <summary>
        /// Given a deviceID, the host is disconnected from display.
        /// All pending subscriptions are implicitly unsubscribed. 
        /// The maintained reference to the SerialDevice is surrendered to the garbage collector.
        /// 
        /// PRECONDITION: DiscoverDeviceIds() was successful
        /// 
        /// </summary>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display.
        /// </param>
        public void Disconnect(string deviceId)
        {
            try
            {
                this.CancelAllTokenSources(deviceId);
                this.RemoveAllSubscriptions(deviceId);
                this.SerialDeviceDisplays.Remove(deviceId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Host.Instance.Disconnect threw: {ex.Message}");
                throw;
            }
        }
        #endregion


        #region REPORT EVENTS SUBSCRIPTIONS
        /// <summary>
        /// Optional subscription to a ReportEventMessage object that originate from the display.
        /// </summary>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display.
        /// </param>
        /// <param name="reportEventMessageHandler">
        /// Client function name to receive ReportEventMessage objects.
        /// </param>
        public void SubscribeToReportEventMessages(string deviceId, EventHandler<ReportEventArgs> reportEventMessageHandler )
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error =
                    $"Unable to subscribe to ReportEventMessages because deviceId {deviceId} could not be found";
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }
            serialDeviceDisplay.ReportEventMessageSubscriptions.Add(reportEventMessageHandler);
        }

        /// <summary>
        /// Optional subscription to a ReportObjectStatusMessage object that originate from the display.
        /// </summary>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display.
        /// </param>
        /// <param name="reportObjectStatusMessageHandler">
        /// Client function name to receive ReportObjectStatusMessage objects.
        /// </param>
        public void SubscribeToReportObjectStatusMessages(string deviceId, EventHandler<ReportEventArgs> reportObjectStatusMessageHandler)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error =
                    $"Unable to subscribe to ReportObjectStatusMessages because deviceId {deviceId} could not be found";
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }
            serialDeviceDisplay.ReportObjectStatusMessageSubscriptions.Add(reportObjectStatusMessageHandler);
        }

        /// <summary>
        /// Optional subscription to a ReportMagicBytesMessage object that originate from the display.
        /// </summary>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display.
        /// </param>
        /// <param name="reportMagicBytesMessageHandler">
        /// Client function name to receive ReportMagicBytesMessage objects.
        /// </param>
        public void SubscribeToReportMagicBytesMessages(string deviceId, EventHandler<ReportEventArgs> reportMagicBytesMessageHandler)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error =
                    $"Unable to subscribe to ReportMagicBytesMessages because deviceId {deviceId} could not be found";
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }
            serialDeviceDisplay.ReportMagicBytesMessageSubscriptions.Add(reportMagicBytesMessageHandler);
        }

        /// <summary>
        /// Optional subscription to a ReportMagicDoubleBytesMessage object that originate from the display.
        /// </summary>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display.
        /// </param>
        /// <param name="reportMagicDoubleBytesMessageHandler">
        /// Client function name to receive ReportMagicDoubleBytesMessage objects.
        /// </param>
        public void SubscribeToReportMagicDoubleBytesMessages(string deviceId, EventHandler<ReportEventArgs> reportMagicDoubleBytesMessageHandler)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error =
                    $"Unable to subscribe to ReportMagicDoubleBytesMessages because deviceId {deviceId} could not be found";
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }
            serialDeviceDisplay.ReportMagicDoubleBytesMessageSubscriptions.Add(reportMagicDoubleBytesMessageHandler);
        }

        /// <summary>
        /// Unsubscribe from ReportEventMessage objects being sent by the display.
        /// </summary>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display.
        /// </param>
        /// <param name="reportEventMessageHandler">
        /// Client function name to stop receiving ReportEventMessage objects.
        /// </param>
        public void UnsubscribeFromReportEventMessages(string deviceId, EventHandler<ReportEventArgs> reportEventMessageHandler)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error =
                    $"Unable to unsubscribe from ReportEventMessages because deviceId {deviceId} could not be found";
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }
            serialDeviceDisplay.ReportEventMessageSubscriptions.Remove(reportEventMessageHandler);
        }

        /// <summary>
        /// Unsubscribe from ReportObjectStatusMessage objects being sent by the display.
        /// </summary>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display.
        /// </param>
        /// <param name="reportObjectStatusMessageHandler">
        /// Client function name to stop receiving ReportObjectStatusMessage objects.
        /// </param>
        public void UnsubscribeFromReportObjectStatusMessages(string deviceId, EventHandler<ReportEventArgs> reportObjectStatusMessageHandler)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error =
                    $"Unable to unsubscribe from ReportObjectStatusMessages because deviceId {deviceId} could not be found";
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }
            serialDeviceDisplay.ReportObjectStatusMessageSubscriptions.Remove(reportObjectStatusMessageHandler);
        }

        /// <summary>
        /// Unsubscribe from ReportMagicBytesMessage objects being sent by the display.
        /// </summary>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display.
        /// </param>
        /// <param name="reportMagicBytesMessageHandler">
        /// Client function name to stop receiving ReportMagicBytesMessage objects.
        /// </param>
        public void UnsubscribeFromReportMagicBytesMessages(string deviceId, EventHandler<ReportEventArgs> reportMagicBytesMessageHandler)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error =
                    $"Unable to unsubscribe from ReportMagicBytesMessages because deviceId {deviceId} could not be found";
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }
            serialDeviceDisplay.ReportMagicBytesMessageSubscriptions.Remove(reportMagicBytesMessageHandler);
        }

        /// <summary>
        /// Unsubscribe from ReportMagicDoubleBytesMessage objects being sent by the display.
        /// </summary>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display.
        /// </param>
        /// <param name="reportMagicDoubleBytesMessageHandler">
        /// Client function name to stop receiving ReportMagicDoubleBytesMessage objects.
        /// </param>
        public void UnsubscribeFromReportMagicDoubleBytesMessages(string deviceId, EventHandler<ReportEventArgs> reportMagicDoubleBytesMessageHandler)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error =
                    $"Unable to unsubscribe from ReportMagicDoubleBytesMessages because deviceId {deviceId} could not be found";
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }
            serialDeviceDisplay.ReportMagicDoubleBytesMessageSubscriptions.Remove(reportMagicDoubleBytesMessageHandler);
        }
        #endregion

        #region SUBSCRIPTION LIFETIME 
        /// <summary>
        /// Given a discovered deviceID, starts the internal display listener tasks.
        /// This method needs to be called once before using the Host.Instance.Send methods.
        /// Precondition: The <see cref="Connect"/> task was successful.
        /// </summary>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display.
        /// </param>
        /// <returns>Returns a <see cref="Task"/> to be awaited across the listening lifetime.</returns>
        public Task StartListening(string deviceId)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error =
                    $"Host.Instance.StartListening was unable to start listening to display because deviceId {deviceId} could not be found";
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }

            var tasks = new List<Task>();

            Task taskDequeueReportEventMessages =
                serialDeviceDisplay.DequeueReportEventMessages(serialDeviceDisplay
                    .ReportEventMessageCancellationTokenSource.Token);
            tasks.Add(taskDequeueReportEventMessages);

            Task taskDequeueReportObjectStatusMessages =
                serialDeviceDisplay.DequeueReportObjectStatusMessages(serialDeviceDisplay
                    .ReportObjectStatusMessageCancellationTokenSource.Token);
            tasks.Add(taskDequeueReportObjectStatusMessages);

            Task taskDequeueReportMagicBytesMessages =
                serialDeviceDisplay.DequeueReportMagicBytesMessages(serialDeviceDisplay
                    .ReportObjectStatusMessageCancellationTokenSource.Token);
            tasks.Add(taskDequeueReportObjectStatusMessages);

            Task taskReportMagicDoubleBytesMessages =
                serialDeviceDisplay.DequeueReportMagicDoubleBytesMessages(serialDeviceDisplay
                    .ReportObjectStatusMessageCancellationTokenSource.Token);
            tasks.Add(taskReportMagicDoubleBytesMessages);

            //Internal entry point for all events including ACK/NAK
            Task taskMessageReceiver = serialDeviceDisplay.Receive(serialDeviceDisplay.ReceiveCancellationTokenSource.Token);
            tasks.Add(taskMessageReceiver);

            Debug.WriteLine($"Starting tasks for device {deviceId} to listen to queues...");

            Task taskWhenAll = Task.WhenAll(tasks.ToArray()) ?? throw new ArgumentNullException("Task.WhenAll(tasks.ToArray())");

            //
            //******Mark that listener tasks are running
            //
            serialDeviceDisplay.AreEventListenerTasksRunning = true;
 
            //Run until told to cancel
            return taskWhenAll;
        }

        /// <summary>Given a discovered deviceID, stops the internal display listener tasks. Precondition: The <see cref="StartSubscriptions"/> has been called.</summary>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display.
        /// </param>
        public void StopListening(string deviceId)
        {
            try
            {
                this.CancelAllTokenSources(deviceId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"StopListening threw: {ex.Message}");
                throw;
            }
        }
        #endregion

        #region SEND MESSAGE TO SERIAL DEVICE DISPLAY 
        /// <summary>
        /// Sends a WriteMessage to the display.
        /// </summary>
        /// <remarks>
        /// Caller should consider using the CancellationTokenSource Constructor (Int32),
        /// which initializes a new instance of the CancellationTokenSource class that will be 
        /// canceled after the specified delay in milliseconds.
        /// </remarks>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display.
        /// </param>
        /// <param name="writeMessage">
        /// A concrete subclass of <see cref="WriteMessage"/> per the Visi-Genie Reference Manual. 
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> that can cancel or timeout the cref="Send"/> method. 
        /// </param>
        /// <returns>
        /// A <see cref="Task{Acknowledgement}"/> to determine is the <see cref="WriteMessage"/> returned ACK, NAK or TIMEOUT.  
        /// </returns>
        public async Task<Acknowledgement> Send(string deviceId, WriteMessage writeMessage,
            CancellationToken cancellationToken)
        {
            return await this.Send(deviceId, writeMessage.ToByteArray(), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends bytes to the serial device display related to the deviceId.
        /// </summary>
        /// <remarks>
        /// Caller should consider using the CancellationTokenSource Constructor (Int32),
        /// which initializes a new instance of the CancellationTokenSource class that will be 
        /// canceled after the specified delay in milliseconds.
        /// </remarks>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display.
        /// </param>
        /// <param name="bytesToWrite">
        /// An order packing of bytes per the command data structures found in the Visi-Genie Reference Manual. 
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> that can cancel or timeout the cref="Send"/> method. 
        /// </param>
        /// <returns>A <see cref="Task{Acknowledgement}"/> to determine if <see cref="WriteMessage"/> returned ACK, NAK or TIMEOUT.</returns>
        public async Task<Acknowledgement> Send(string deviceId, byte[] bytesToWrite,
            CancellationToken cancellationToken)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error =
                    $"Host.Instance.Send cannot send message to display because deviceId {deviceId} could not be found";
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }

            if (serialDeviceDisplay.AreEventListenerTasksRunning == false)
            {
                throw new InvalidOperationException("Host.Instance.StartListening must be called once before calling Host.Instance.Send!");
            }

            return await serialDeviceDisplay.Send(bytesToWrite, cancellationToken).ConfigureAwait(false);
        }
        #endregion

        #region IMPLEMENTATION OF SERIAL DEVICE MANAGEMENT
        /// <summary>
        /// A collection of key-value pairs that maintain discovered <see cref="SerialDeviceDisplay"/> objects.
        /// </summary>
        private Dictionary<string, SerialDeviceDisplay> SerialDeviceDisplays { get; set; }

        // <summary>Given a valid deviceId key string, this method returns a particular <see cref="SerialDeviceDisplay"/> instance.</summary>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display.
        /// </param>
        /// <returns>
        /// Returns the <see cref="SerialDeviceDisplay"/> if found, otherwise returns null. 
        /// </returns>
        private SerialDeviceDisplay LookupDevice(string deviceId)
        {
            SerialDeviceDisplay serialDeviceDisplay = this.SerialDeviceDisplays[deviceId];
            if (this.SerialDeviceDisplays.ContainsKey(deviceId))
            {
                return serialDeviceDisplay;
            }
            return null;
        }

        // <summary> Given a valid deviceId key string, this method removes <see cref="SerialDeviceDisplay"/> instance.</summary>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display.
        /// </param>
        private void RemoveDevice(string deviceId)
        {
            if (this.SerialDeviceDisplays.ContainsKey(deviceId))
            {
                this.SerialDeviceDisplays.Remove(deviceId);
            }
        }
        #endregion

        #region
        // <summary>Given a valid deviceId key string, this cancels internal tasks used by the <see cref="SerialDeviceDisplay"/> instance.</summary>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display.
        /// </param>
        private void CancelAllTokenSources(string deviceId)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error = $"No task were cancelled because deviceId {deviceId} could not be found";
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }

            try
            {
                serialDeviceDisplay.ReportEventMessageCancellationTokenSource.Cancel();
                Debug.WriteLine($"Cancelled DequeueReportEvent Task identified by {deviceId}");

                serialDeviceDisplay.ReportObjectStatusMessageCancellationTokenSource.Cancel();
                Debug.WriteLine($"Cancelled DequeueReportObjectStatusMessages Task identified by {deviceId}");

                serialDeviceDisplay.ReportMagicBytesMessageCancellationTokenSource.Cancel();
                Debug.WriteLine($"Cancelled ReportMagicBytesMessage Task identified by {deviceId}");

                serialDeviceDisplay.ReportMagicDoubleBytesMessageCancellationTokenSource.Cancel();
                Debug.WriteLine($"Cancelled ReportMagicDoubleBytesMessage Task identified by {deviceId}");

                serialDeviceDisplay.ReceiveCancellationTokenSource.Cancel();
                Debug.WriteLine($"Cancelled Receive Task identified by {deviceId}");
            }
            catch (TaskCanceledException tce)
            {
                Debug.WriteLine($"CancelAllTokenSource caught {tce.Message}");
            }
        }

        /// <summary>
        /// Removes all subscriptions 
        /// </summary>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display.
        /// </param>
        private void RemoveAllSubscriptions(string deviceId)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error = $"No subscriptions were removed because deviceId {deviceId} could not be found";
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }

            serialDeviceDisplay.ReportEventMessageSubscriptions.RemoveAll();
            serialDeviceDisplay.ReportObjectStatusMessageSubscriptions.RemoveAll();
            serialDeviceDisplay.ReportMagicBytesMessageSubscriptions.RemoveAll();
            serialDeviceDisplay.ReportMagicDoubleBytesMessageSubscriptions.RemoveAll();
        }
        #endregion
    }
}
