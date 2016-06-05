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
    /// takes care of the setting up the subscription by using the client app's <see cref="EventHandler{DeferrableDisplayEventArgs}"/>
    /// Physically, a connection is required from the host USB port to display's backside 5 pins connector.
    /// </remarks>
    public sealed class Host
    {

        #region CONSTRUCTION
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
            this.ReportEventMessageSubscriptions = new Dictionary<string, DisplayableEvent>();
            this.ReportObjectStatusMessageSubscriptions = new Dictionary<string, DisplayableEvent>(); 
            Debug.WriteLine("Created Host");
        }
        #endregion

        #region SERIAL COMMUNICATION DEVICE DISCOVERY
        /// <summary>
        /// Finds 1 to N serial device displays, where each display is identified by string identifier. 
        /// In order for the client app do use subsequent class method, the client app must 
        /// hold onto the returned list of string Ids returned from this method.
        /// 
        /// TODO: Client application must add the DeviceCapability to their project's Package.appxmanifest file.
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
        /// <returns>
        /// Returns a <see cref="Task{List{string}}"/> that contains device indentifiers logically representing serial device displays.
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

        #region DEVICE CONNECTION LIFETIME
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
        /// <returns>Returns a <see cref="Task"/>.</returns>
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
        /// The maintained reference to the serial device is surrendered and all outstanding event subscriptions are cleared.
        /// 
        /// PRECONDITION: DiscoverDeviceIds() was successful
        /// 
        /// </summary>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display object.
        /// </param>
        /// <returns>Returns a <see cref="Task"/>.</returns>
        public async Task Disconnect(string deviceId)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                Debug.WriteLine(string.Format("Unable to find deviceId {0}", deviceId));
                throw new NullReferenceException();
            }

            //execute in thread pool thread
            await Task.Run(() =>
            {
                this.CancelAllTokenSources(deviceId);

                if (this.ReportEventMessageSubscriptions.ContainsKey(deviceId))
                {
                    this.ReportEventMessageSubscriptions[deviceId].RemoveAll();
                    this.ReportEventMessageSubscriptions.Remove(deviceId);
                }

                if (this.ReportObjectStatusMessageSubscriptions.ContainsKey(deviceId))
                {
                    this.ReportObjectStatusMessageSubscriptions[deviceId].RemoveAll();
                    this.ReportObjectStatusMessageSubscriptions.Remove(deviceId);
                }

                this.RemoveDevice(deviceId);
            });
        }
        #endregion

        #region RECEIVE DISPLAY REPORTS 
        /// <summary>
        /// Given a discovered deviceID, start listenening for display messages.
        /// Precondition: The <see cref="Connect"/> task was successful.
        /// 
        /// </summary>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display object.
        /// </param>
        /// <param name="reportEventMessageHandler">
        /// A <see cref="EventHandler"/> to receive a <see cref="ReportEventMessage"/>
        /// </param>
        /// <param name="reportObjectStatusMessageHandler">
        /// A <see cref="EventHandler"/> to receive a <see cref="ReportEventMessage"/>
        /// </param>
        /// <returns>Returns a <see cref="Task"/> to be awaited across the listening lifetime.</returns>
        public async Task StartListening(string deviceId, 
            EventHandler<DeferrableDisplayEventArgs> reportEventMessageHandler, 
            EventHandler<DeferrableDisplayEventArgs> reportObjectStatusMessageHandler)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                Debug.WriteLine(string.Format("Cannot start listening because device {0} was not found", deviceId));
                throw new NullReferenceException();
            }

            //Set up client subscriptions 
            this.SubscribeToReportEventMessages(deviceId, reportEventMessageHandler);
            this.SubscribeToReportObjectStatusMessages(deviceId, reportObjectStatusMessageHandler);

            //Link subscriptions to internal queues of the particular device instance
            await this.LinkQueuesToSubscriptions(deviceId).ConfigureAwait(false);
        }

        /// <summary>
        /// Given a discovered deviceID, stops listenening to display messages.
        /// Precondition: The <see cref="StartListening"/> task is running.
        /// 
        /// </summary>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display object.
        /// </param>
        /// <param name="reportEventMessageHandler">
        /// A <see cref="EventHandler"/> currently receiving <see cref="ReportEventMessage"/>
        /// </param>
        /// <param name="reportObjectStatusMessageHandler">
        /// A <see cref="EventHandler"/> currently receiving <see cref="ReportEventMessage"/>
        /// </param>
        /// <returns></returns>
        public async Task StopListening(string deviceId, 
            EventHandler<DeferrableDisplayEventArgs> reportEventMessageHandler, 
            EventHandler<DeferrableDisplayEventArgs> reportObjectMessageHandler)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                Debug.WriteLine(string.Format("Device {0} was not found.", deviceId));
                return;
            }

            try
            {
                await Task.Run(() =>
                {
                    //Cancel active token sources
                    this.CancelAllTokenSources(deviceId);

                    //End subscriptions 
                    this.UnsubscribeFromReportEventMessages(deviceId, reportEventMessageHandler);
                    this.UnsubscribeFromReportObjectStatusMessages(deviceId, reportObjectMessageHandler);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(String.Format("Cancel threw: {0}", ex.Message));
                throw ex;
            }
        }
        #endregion

        #region SEND COMMANDS TO SERIAL DEVICE DISPLAY 
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
                Debug.WriteLine(string.Format("Cannot send bytes because device {0} was not found", deviceId));
                throw new NullReferenceException();
            }

            return await serialDeviceDisplay.Send(bytesToWrite, cancellationToken).ConfigureAwait(false);
        }
        #endregion

        #region IMPLEMENTATION OF REPORT SUBSCRIPTION MANAGEMENT

        /// <summary>
        /// A collection of key-value pairs that maintain <see cref="ReportEventMessage"/> subscriptions.
        /// </summary>
        private Dictionary<string, DisplayableEvent> ReportEventMessageSubscriptions { get; set; }

        /// <summary>
        /// A collection of key-value pairs that maintain <see cref="ReportObjectStatusMessage"/> subscriptions.
        /// </summary>
        private Dictionary<string, DisplayableEvent> ReportObjectStatusMessageSubscriptions { get; set; }

        private void SubscribeToReportEventMessages(string deviceId, EventHandler<DeferrableDisplayEventArgs> handler)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                Debug.WriteLine(string.Format("SubscribeToReportEventMessages failed to find device {0}", deviceId));
                throw new NullReferenceException();
            }

            var ehc = new DisplayableEvent();
            ehc.Add(handler);

            this.ReportEventMessageSubscriptions.Add(deviceId, ehc);         
        }

        private void UnsubscribeFromReportEventMessages(string deviceId, EventHandler<DeferrableDisplayEventArgs> handler)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                Debug.WriteLine(string.Format("UnsubscribeFromReportEventMessages failed to find device {0}", deviceId));
                throw new NullReferenceException();
            }
            this.ReportEventMessageSubscriptions[deviceId].Remove(handler);
        }

        private void SubscribeToReportObjectStatusMessages(string deviceId, EventHandler<DeferrableDisplayEventArgs> handler)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                Debug.WriteLine(string.Format("SubscribeToReportObjectStatusMessages failed to find device {0}", deviceId));
                throw new NullReferenceException();
            }

            var ehc = new DisplayableEvent();
            ehc.Add(handler);

            this.ReportObjectStatusMessageSubscriptions.Add(deviceId, ehc);
        }

        private void UnsubscribeFromReportObjectStatusMessages(string deviceId, EventHandler<DeferrableDisplayEventArgs> handler)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                Debug.WriteLine(string.Format("UnsubscribeFromReportObjectStatusMessages failed to find device {0}", deviceId));
                throw new NullReferenceException();
            }

            this.ReportObjectStatusMessageSubscriptions[deviceId].Remove(handler);
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

        // <summary>
        /// Given a valid deviceId key string, this method seeds links queues to client subscriptions for a <see cref="SerialDeviceDisplay"/> instance.
        /// </summary>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display object.
        /// </param>
        private async Task LinkQueuesToSubscriptions(string deviceId)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                Debug.WriteLine(string.Format("Cannot create cancellation token sources because device {0} was not found", deviceId));
                throw new NullReferenceException();
            }

            var tasks = new List<Task>();

            var rem = this.ReportEventMessageSubscriptions[deviceId];
            Task taskDequeueReportEventMessages = serialDeviceDisplay.DequeueReportEventMessages(rem, serialDeviceDisplay.ReportEventMessageCancellationTokenSource.Token);
            tasks.Add(taskDequeueReportEventMessages);

            var rosm = this.ReportObjectStatusMessageSubscriptions[deviceId];
            Task taskDequeueReportObjectStatusMessages = serialDeviceDisplay.DequeueReportObjectStatusMessages(rosm, serialDeviceDisplay.ReportObjectStatusMessageCancellationTokenSource.Token);
            tasks.Add(taskDequeueReportObjectStatusMessages);

            Task taskMessageReceiver = serialDeviceDisplay.Receive(serialDeviceDisplay.ReceiveCancellationTokenSource.Token);
            tasks.Add(taskMessageReceiver);

            Debug.WriteLine(string.Format("Starting internal task listeners for device {0}", deviceId));

            Task taskWhenAll = Task.WhenAll(tasks.ToArray());

            await taskWhenAll;
        }

        // <summary>
        /// Given a valid deviceId key string, this cancels internal tasks used by the <see cref="SerialDeviceDisplay"/> instance.
        /// </summary>
        /// <param name="deviceId">
        /// A unique key that represents a physically connected serial device display object.
        /// </param>
        private void CancelAllTokenSources(string deviceId)
        {
            var serialDeviceDisplay = this.LookupDevice(deviceId);
            if (serialDeviceDisplay == null)
            {
                Debug.WriteLine(string.Format("Cannot cancel token sources because device {0} was not found", deviceId));
                throw new NullReferenceException();
            }

            try
            { 
                serialDeviceDisplay.ReportEventMessageCancellationTokenSource.Cancel();
                Debug.WriteLine(string.Format("Cancelled DequeueReportEvent Task indentified by {0}", deviceId));

                serialDeviceDisplay.ReportObjectStatusMessageCancellationTokenSource.Cancel();
                Debug.WriteLine(string.Format("Cancelled DequeueReportObjectStatusMessages Task indentified by {0}", deviceId));

                serialDeviceDisplay.ReceiveCancellationTokenSource.Cancel();
                Debug.WriteLine(string.Format("Cancelled Receive Task indentified by {0}", deviceId));
            }
            catch (TaskCanceledException tce)
            {
                Debug.WriteLine(String.Format("CancelAllTokenSource caught {0}", tce.Message));
            }
        }
        #endregion
    }
}
