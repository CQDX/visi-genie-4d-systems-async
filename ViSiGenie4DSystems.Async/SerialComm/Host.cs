// Copyright(c) 2016 Michael Dorough
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;

using ViSiGenie4DSystems.Async.Enumeration;
using ViSiGenie4DSystems.Async.Message;
using ViSiGenie4DSystems.Async.Event;

namespace ViSiGenie4DSystems.Async.SerialComm
{
    /// <summary>
    /// Enabled a client application to communicate with one or more 4D System display(s) connected via a serial device. 
    /// </summary>
    /// <remarks>
    /// <see cref="Host"/> is a sealed singleton class that creates compatible ViSi Genie compatible read events. This class
    /// takes care of the setting up the subscription to 4D Systems Visi Genie Reports by using Reactive Extensions - Main Library 2.3.0-beta2.
    /// A Silicon Labs CP2102 USB to Serial UART Bridge Converter Cable is used to connected the host USB port to display's backside 5 pins connector.
    /// </remarks>
    public sealed class Host
    {
        #region SINGLETON CONSTRUCTION
        /// <summary>
        /// Singleton initialization
        /// </summary>
        private static readonly Lazy<Host> veryLazy = new Lazy<Host>(() => new Host());

        /// <summary>
        /// The single instance of the Host object.
        /// </summary>
        public static Host Instance
        {
            get
            {
                return veryLazy.Value;
            }
        }

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
        /// Each display found connected to the USB port on the Host is identified by string identifier. 
        /// The client app must retain the return string identifier in order to use subsequent Host class methods.
        /// A list of string indentifiers is returned, where 1 or more display can be connected via the USB ports.
        /// 
        /// ************************************************************************************************
        /// 
        /// IMPORTANT TODO: 
        /// The client application must add the DeviceCapability to their project's Package.appxmanifest file.
        /// Capability must be defined only once in your app's Package.appxmanifest as follows:
        /// 
        /// <Capabilities>
        ///     <DeviceCapability Name="serialcommunication">
        ///         <Device Id = "any" >
        ///             <Function Type="name:serialPort" />
        ///         </Device>
        ///     </DeviceCapability>
        /// </Capabilities>
        /// 
        /// *************************************************************************************************
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
        /// A unique key that represents a physically connected serial device display object.
        /// </param>
        /// <param name="portDef">
        /// A <see cref="PortDef"/> baud rate that matches the related Workshop 4 project baud rate.
        /// </param>
        /// <returns>Returns a <see cref="Task"/></returns>
        public async Task Connect(string deviceId, PortDef portDef)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                Debug.WriteLine(string.Format("Unable to connect to deviceId {0}", deviceId));
                throw new NullReferenceException();
            }
            await serialDeviceDisplay.Connect(deviceId, portDef).ConfigureAwait(false);
        }

        /// <summary>
        /// Given a deviceID, the host is disconnected from a particular serial device display.
        /// All pending subscriptions are implicitly unsubscribed. 
        /// The maintained reference to the serial device is surrendered.
        /// 
        /// PRECONDITION: DiscoverDeviceIds() was successful
        /// 
        /// </summary>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display object.
        /// </param>
        /// <returns>Returns a <see cref="Task"/></returns>
        public async Task Disconnect(string deviceId)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error = string.Format("Unable disconnect because deviceId {0} could not be found", deviceId);
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }

            await this.CancelAllTokenSources(deviceId);

            serialDeviceDisplay.ReportEventMessageSubscriptions.RemoveAll();
            serialDeviceDisplay.ReportObjectStatusMessageSubscriptions.RemoveAll();
            serialDeviceDisplay.ReportMagicBytesMessageSubscriptions.RemoveAll();
            serialDeviceDisplay.ReportMagicDoubleBytesMessageSubscriptions.RemoveAll();

            this.SerialDeviceDisplays.Clear();
        }
        #endregion


        #region REPORT EVENTS SUBSCRIPTIONS
        public void SubscribeToReportEventMessages(string deviceId, EventHandler<ReportEventArgs> reportEventMessageHandler )
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error = string.Format("Unable to subscribe to ReportEventMessages because deviceId {0} could not be found", deviceId);
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }
            serialDeviceDisplay.ReportEventMessageSubscriptions.Add(reportEventMessageHandler);
        }

        public void SubscribeToReportObjectStatusMessages(string deviceId, EventHandler<ReportEventArgs> reportObjectStatusMessageHandler)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error = string.Format("Unable to subscribe to ReportObjectStatusMessages because deviceId {0} could not be found", deviceId);
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }
            serialDeviceDisplay.ReportObjectStatusMessageSubscriptions.Add(reportObjectStatusMessageHandler);
        }

        public void SubscribeToReportMagicBytesMessages(string deviceId, EventHandler<ReportEventArgs> reportMagicBytesMessageHandler)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error = string.Format("Unable to subscribe to ReportMagicBytesMessages because deviceId {0} could not be found", deviceId);
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }
            serialDeviceDisplay.ReportMagicBytesMessageSubscriptions.Add(reportMagicBytesMessageHandler);
        }

        public void SubscribeToReportMagicDoubleBytesMessages(string deviceId, EventHandler<ReportEventArgs> reportMagicDoubleBytesMessageHandler)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error = string.Format("Unable to subscribe to ReportMagicDoubleBytesMessages because deviceId {0} could not be found", deviceId);
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }
            serialDeviceDisplay.ReportMagicDoubleBytesMessageSubscriptions.Add(reportMagicDoubleBytesMessageHandler);
        }

        public void UnsubscribeFromReportEventMessages(string deviceId, EventHandler<ReportEventArgs> reportEventMessageHandler)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error = string.Format("Unable to unsubscribe from ReportEventMessages because deviceId {0} could not be found", deviceId);
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }
            serialDeviceDisplay.ReportEventMessageSubscriptions.Remove(reportEventMessageHandler);
        }

        public void UnsubscribeFromReportObjectStatusMessages(string deviceId, EventHandler<ReportEventArgs> reportObjectStatusMessageHandler)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error = string.Format("Unable to unsubscribe from ReportObjectStatusMessages because deviceId {0} could not be found", deviceId);
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }
            serialDeviceDisplay.ReportObjectStatusMessageSubscriptions.Remove(reportObjectStatusMessageHandler);
        }

        public void UnsubscribeFromReportMagicBytesMessages(string deviceId, EventHandler<ReportEventArgs> reportMagicBytesMessageHandler)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error = string.Format("Unable to unsubscribe from ReportMagicBytesMessages because deviceId {0} could not be found", deviceId);
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }
            serialDeviceDisplay.ReportMagicBytesMessageSubscriptions.Remove(reportMagicBytesMessageHandler);
        }

        public void UnsubscribeFromReportMagicDoubleBytesMessages(string deviceId, EventHandler<ReportEventArgs> reportMagicDoubleBytesMessageHandler)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error = string.Format("Unable to unsubscribe from ReportMagicDoubleBytesMessages because deviceId {0} could not be found", deviceId);
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }
            serialDeviceDisplay.ReportMagicDoubleBytesMessageSubscriptions.Remove(reportMagicDoubleBytesMessageHandler);
        }
        #endregion

        #region FUTURE SUPPORT FOR REACTIVE EXTENSION SUBSCRIPTIONS 

        /*WAITING FOR RX-MAIN UWP RELEASE. CURRENTLY 2.3.0-beta2 
        public IObservable<ReportEventMessage> SubscribeToReportEventMessages(string deviceId)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error = string.Format("Unable to subscribe to ReportEventMessage events because deviceId {0} could not be found", deviceId);
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }

            var observableREMR = serialDeviceDisplay.ReportEventMessageReceived;
            return observableREMR;
        }

        public IObservable<ReportObjectStatusMessage> SubscribeToReportObjectStatusMessage(string deviceId)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error = string.Format("Unable to subscribe to ReportObjectStatusMessage events because deviceId {0} could not be found", deviceId);
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }

            var observableROSMR = serialDeviceDisplay.ReportObjectStatusMessageReceived;
            return observableROSMR;
        }

        public IObservable<ReportMagicBytesMessage> SubscribeToReportMagicBytesMessage(string deviceId)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error = string.Format("Unable to subscribe to ReportMagicBytesMessage events because deviceId {0} could not be found", deviceId);
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }

            var observableRMBMR = serialDeviceDisplay.ReportMagicBytesMessageReceived;
            return observableRMBMR;
        }

        public IObservable<ReportMagicDoubleBytesMessage> SubscribeToReportMagicDoubleBytesMessage(string deviceId)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error = string.Format("Unable to subscribe to ReportMagicDoubleBytesMessage events because deviceId {0} could not be found", deviceId);
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }

            var observableRMDBMR = serialDeviceDisplay.ReportMagicDoubleBytesMessageReceived;
            return observableRMDBMR;
        }
        */
        #endregion

        #region SUBSCRIPTION LIFETIME 
        /// <summary>
        /// Given a discovered deviceID, starts the internal display listener tasks.
        /// This method needs to be called once before using the Host.Instance.Send methods.
        /// Precondition: The <see cref="Connect"/> task was successful.
        ///  
        /// </summary>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display object.
        /// </param>
        /// <returns>Returns a <see cref="Task"/> to be awaited across the listening lifetime.</returns>
        public async Task StartListening(string deviceId)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error = string.Format("Unable to start subscriptions because deviceId {0} could not be found", deviceId);
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }

            var tasks = new List<Task>();

            Task taskDequeueReportEventMessages = serialDeviceDisplay.DequeueReportEventMessages(serialDeviceDisplay.ReportEventMessageCancellationTokenSource.Token);
            tasks.Add(taskDequeueReportEventMessages);

            Task taskDequeueReportObjectStatusMessages = serialDeviceDisplay.DequeueReportObjectStatusMessages(serialDeviceDisplay.ReportObjectStatusMessageCancellationTokenSource.Token);
            tasks.Add(taskDequeueReportObjectStatusMessages);

            Task taskDequeueReportMagicBytesMessages = serialDeviceDisplay.DequeueReportMagicBytesMessages(serialDeviceDisplay.ReportObjectStatusMessageCancellationTokenSource.Token);
            tasks.Add(taskDequeueReportObjectStatusMessages);

            Task taskReportMagicDoubleBytesMessages = serialDeviceDisplay.DequeueReportMagicDoubleBytesMessages(serialDeviceDisplay.ReportObjectStatusMessageCancellationTokenSource.Token);
            tasks.Add(taskReportMagicDoubleBytesMessages);

            //Internal entry point for all events including ACK/NAK
            Task taskMessageReceiver = serialDeviceDisplay.Receive(serialDeviceDisplay.ReceiveCancellationTokenSource.Token);
            tasks.Add(taskMessageReceiver);

            Debug.WriteLine(string.Format("Starting subscription tasks for device {0}", deviceId));

            Task taskWhenAll = Task.WhenAll(tasks.ToArray());

            //
            //******Mark that listener tasks are running
            //
            serialDeviceDisplay.AreEventListenerTasksRunning = true;
 
            //Run until told to cancel
            await taskWhenAll;
        }

        /// <summary>
        /// Given a discovered deviceID, stops the internal display listener tasks.
        /// Precondition: The <see cref="StartSubscriptions"/> has been called.
        /// </summary>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display object.
        /// </param>
        /// <returns>Returns a <see cref="Task"/></returns>
        public async Task StopListening(string deviceId)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error = string.Format("No subscriptions stopped because deviceId {0} could not be found", deviceId);
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }

            try
            {
                await this.CancelAllTokenSources(deviceId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(String.Format("Cancel threw: {0}", ex.Message));
                throw ex;
            }
        }
        #endregion

        #region SEND MESSAGE TO SERIAL DEVICE DISPLAY 
        /// <summary>
        /// Polymorphically sends a WriteMessage subclass to the display.
        /// </summary>
        /// <remarks>
        /// Caller should consider using the CancellationTokenSource Constructor (Int32),
        /// which initializes a new instance of the CancellationTokenSource class that will be 
        /// canceled after the specified delay in milliseconds.
        /// </remarks>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display object.
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
        public async Task<Acknowledgement> Send(string deviceId, WriteMessage writeMessage, CancellationToken cancellationToken)
        {
            return await this.Send(deviceId, writeMessage.ToByteArray(), cancellationToken);
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
        /// A unique key that represents a physically connected serial device display object.
        /// </param>
        /// <param name="bytesToWrite">
        /// An order packing of bytes per the command data structures found in the Visi-Genie Reference Manual. 
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> that can cancel or timeout the cref="Send"/> method. 
        /// </param>
        /// <returns>
        /// A <see cref="Task{Acknowledgement}"/> to determine if <see cref="WriteMessage"/> returned ACK, NAK or TIMEOUT.  
        /// </returns>
        /// <returns>
        public async Task<Acknowledgement> Send(string deviceId, byte[] bytesToWrite, CancellationToken cancellationToken)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error = string.Format("Cannot sent message to display because deviceId {0} could not be found", deviceId);
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }

            if (serialDeviceDisplay.AreEventListenerTasksRunning == false)
            {
                throw new InvalidOperationException("Host.Instance.StartListening must be called once before calling Send!");
            }

            return await serialDeviceDisplay.Send(bytesToWrite, cancellationToken).ConfigureAwait(false);
        }
        #endregion

        #region IMPLEMENTATION OF SERIAL DEVICE MANAGEMENT
        /// <summary>
        /// A collection of key-value pairs that maintain discovered <see cref="SerialDeviceDisplay"/>.
        /// </summary>
        private Dictionary<string, SerialDeviceDisplay> SerialDeviceDisplays { get; set; }

        // <summary>
        /// Given a valid deviceId key string, this method returns a particular <see cref="SerialDeviceDisplay"/> instance.
        /// </summary>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display object.
        /// </param>
        /// <returns>
        /// Returns the <see cref="SerialDeviceDisplay"/> if found, otherwise returns null. 
        /// </returns>
        private SerialDeviceDisplay LookupDevice(string deviceId)
        {
            if (this.SerialDeviceDisplays.ContainsKey(deviceId))
            {
                SerialDeviceDisplay serialDeviceDisplay = this.SerialDeviceDisplays[deviceId];
                return serialDeviceDisplay;
            }
            return null;
        }

        // <summary>
        /// Given a valid deviceId key string, this method removes <see cref="SerialDeviceDisplay"/> instance.
        /// </summary>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display object.
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
        // <summary>
        /// Given a valid deviceId key string, this cancels internal tasks used by the <see cref="SerialDeviceDisplay"/> instance.
        /// </summary>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display object.
        /// </param>
        private async Task CancelAllTokenSources(string deviceId)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                var error = string.Format("No task were cancelled because deviceId {0} could not be found", deviceId);
                Debug.WriteLine(error);
                throw new NullReferenceException(error);
            }

            try
            {
                await Task.Run( () =>
                {
                    serialDeviceDisplay.ReportEventMessageCancellationTokenSource.Cancel();
                    Debug.WriteLine(string.Format("Cancelled DequeueReportEvent Task indentified by {0}", deviceId));

                    serialDeviceDisplay.ReportObjectStatusMessageCancellationTokenSource.Cancel();
                    Debug.WriteLine(string.Format("Cancelled DequeueReportObjectStatusMessages Task indentified by {0}", deviceId));

                    serialDeviceDisplay.ReportMagicBytesMessageCancellationTokenSource.Cancel();
                    Debug.WriteLine(string.Format("Cancelled ReportMagicBytesMessage Task indentified by {0}", deviceId));

                    serialDeviceDisplay.ReportMagicDoubleBytesMessageCancellationTokenSource.Cancel();
                    Debug.WriteLine(string.Format("Cancelled ReportMagicDoubleBytesMessage Task indentified by {0}", deviceId));

                    serialDeviceDisplay.ReceiveCancellationTokenSource.Cancel();
                    Debug.WriteLine(string.Format("Cancelled Receive Task indentified by {0}", deviceId));
                });
            }
            catch (TaskCanceledException tce)
            {
                Debug.WriteLine(String.Format("CancelAllTokenSource caught {0}", tce.Message));
            }
        }
        #endregion
    }
}
