## ViSiGenie4DSystems.Async

### About

This is a C# async style class library that implements the ViSi Genie Serial Communication Protocol thereby giving Windows IoT apps the ability to incorporate a 4D Systems display in their solution-space. Version 1.2.0 now uses Reactive Extensions - Main Library 2.3.0-beta2, which contains packages compatible with UAP. RX is needed to support Report Events that originate on the display and subsequently needed to be handled by the headless or headed app.

An IoT scenario could be an app that handles user events originating from a resistive touch 4.3" DIABLO16 Display Module. It is assumed the developer has designed a user experience in the 4D Systems Workshop4 IDE that is deployed to the display's micro-SD card. The Windows IoT host running on a Raspberry Pi 2, 3, Dragonboard 410c or Minnowboard Max is connected to the 4D Systems display module via a Silicon Labs CP2102 USB to Serial UART Bridge Converter cable. 
To get started, use the singleton class named Host, which found in the *ViSiGenie4DSystems.Async.SerialComm* namespace. The *Host* class takes care of all the display I/O.

<img src="https://github.com/CQDX/visi-genie-4d-systems-async/blob/master/ViSiGenie4DSystems.Async/TouchDisplay.jpg">

* Provides an API supports the ViSi-Genie Communication Protocols, Objects, Properties, and Genie Magic. 
  See [ViSi-Genie Reference Manual](http://www.4dsystems.com.au/productpages/ViSi-Genie/downloads/Visi-Genie_refmanual_R_1_11.pdf) 
  The reference manual defines language independent message data structures and command/event protocols.

* Intended for applications requiring a non-primary display running on a Raspberry Pi 2 and 3, Arrow DragonBoard 410c or MinnowBoard MAX.  

* Connects and discovers one or more displays.
   
* Queues all incomming ViSi Genie Report Events and Report Objects that originate from the display. 
  The library forwards display events as C# async event for the app to handle. 
  For instance, the user presses a menu button object on the resistive touch display.  

* Uses Reactive Extensions - Main Library 2.3.0-beta2, which contains packages compatible with UAP.   

* For more details on RX, please reference [Reactive Extensions (Rx) – Part 1 – Replacing C# Events](http://rehansaeed.com/reactive-extensions-part1-replacing-events/)

### Hardware 

* [4D Programming Capable, USB to Serial UART Converter Cable](http://www.4dsystems.com.au/product/4D_Programming_Cable/)
* [uSD-4GB-Instustrial rated micro-SD card](http://www.4dsystems.com.au/product/uSD_4GB_Industrial/). The Phison brand is what 4D Systems display modules use. Industrial grade is optional. 
* [uLCD-35DT 3.5" TFT LCD Display Module with Resistive Touch] (http://www.4dsystems.com.au/product/uLCD_35DT_PI/) or other size module.
* [Raspberry Pi 3 - Model B - ARMv8 with 1G RAM] (https://www.adafruit.com/product/3055) or equivalent Raspberry P2, Broadcomm or Intel Atom SOC.

### Bring-Up Notes 

* From the 4D Workshop4 IDE, build and deploy your project to the display's micro-SD card. With ESD protection, safely remove micro-SD card from PC and install micron-SD card into display's pannel.

* In Microsoft Visual Studio, use the NuGet Package Manager Console to install the ViSiGenie.4DSystems.Async Library version 1.2.0: 

```
PM> Install-Package ViSiGenie.4DSystems.Async -Version 1.2.0
```

* If your 4D Workshop4 project sends events to the host, then also install the Reactive Extensions Main Library 2.3.0-beta2: 

```
PM> Install-Package Rx-Main -Pre
```

* In Microsoft Visual Studio, edit the project Package.appmanifest file. 
  Add a capability to support *serialcommunication*. 
  If *DeviceCapability* is not configured, then the *Host* will throw an exception when *Connect* gets called.

```XML
	<Capabilities>
		<DeviceCapability Name="serialcommunication">
			<Device Id = "any" >
				<Function Type="name:serialPort" />
			</Device>
		</DeviceCapability>
	</Capabilities>
```		
* Plug the USB programmers cable into the USB port on Pi 3 or equivalent IoT device. 
  Connect the other end of the cable into the backside of the display's 5 pins connector.

* Review and add code clips shown below to your project. 

* Build your ARM project in Visual Studio, cycle power on Pi 3. Finally deploy and debug your app :)

### Class Relationships

The singleton class named *Host* creates and manages the lifetime of serial device instances. For example, a Raspberry Pi 2 could have four different display modules, where each monitor is dedicated to specific subsystem monitoring and related interactive control. 

<img src="https://github.com/CQDX/visi-genie-4d-systems-async/blob/master/ViSiGenie4DSystems.Async/ClassDiagram.png">

### Host.Instance 

The exemplar below shows how-to use the *Host.Instance* singleton:

```C#
using System;
using System.Linq;
using Windows.ApplicationModel.Background;
using System.Diagnostics;
using Windows.System;
using System.Threading.Tasks;
using System.Threading;

using ViSiGenie4DSystems.Async.Enumeration;
using ViSiGenie4DSystems.Async.Message;
using ViSiGenie4DSystems.Async.SerialComm;

namespace DisplayHeadless
{
    public sealed class StartupTask : IBackgroundTask
    {
        BackgroundTaskDeferral _defferal;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {		
			var cts = new CancellationTokenSource();
		
			//1. App find the connected device identifier string and hang on to it
			Task<List<string>> discoverDeviceIdsTask = Host.Instance.DiscoverDeviceIds();
			
			await discoverDeviceIdsTask;
			 
			//2. App connects host -to- the 4D Systems display. In this case, only one display is connected to the host
			var deviceId = discoverDeviseIdsTask.Result.First();
			
			//3. Host baud must match 4D Workshop project baud rate, else Connect will throw
			var portDef = new PortDef(BaudRate.Bps115200);
			Task connectTask = Host.Instance.Connect(deviceId, portDef);
			await connectTask;

			//5. Set up a subscription to ReportEventMessage objects. Uses RX 2.3.0-beta2 for UWP support.
			var report = new ReportEventMessageSubscription()
			report.Subscribe()

			//6. App start listening for display sent from the display. 
			await StartListening(deviceId);
		
			//7. Change to form 0 on display per a particular 4D Workshop4 project layout.  
			const int displayFormId = 0;
			var writeObjectMessage = new WriteObjectValueMessage(ObjectType.Form, displayFormId);
			await Host.Instance.Send(enabledBoard.SerialDeviceId, writeObjectMessage, cts.Token);

			//8. Write string message to display per a particular 4D Workshop4 project layout...
			const int displayStringId = 0;
			var writeStringMessage = new WriteStringASCIIMessage(displayStringId, "Hello 4D Systems via Windows IoT!");
			await Host.Instance.Send(enabledBoard.SerialDeviceId, writeStringMessage, cts.Token);
		
			//9. App stops listening to display events
		    await Host.Instance.StopListening(enabledBoard.Value.SerialDeviceId);
    
			//10. Disconect from display by giving up the serial device to garbarge collection
			await Host.Instance.Disconnect(deviceId);

            _defferal.Complete();
        }        
    }
}
```

### Report Event Message Subscription

When designing the Genie display application in Workshop, each Object can be
configured to report its status change without the host having to poll it (see ReadObject
StatusMessage class). If the object’s ‘Event Handler’ is set to ‘Report Event’ in the ‘Event’ tab,
the display will transmit the object’s status upon any change. For example, Slider 3 object
was set from 0 to 50 by the user. The exemplar below shows how-to recieve Report Event Messages 
that occur when user touches an object on the display. 

The switch statement shown below is for demo purposes only! 
Instead, you need to customize the *OnReportEventMessageReceived* method per the project's Workshop 4 requirements.

```C#
using System.Threading.Tasks;

using ViSiGenie4DSystems.Async.Enumeration;
using ViSiGenie4DSystems.Async.Event;
using ViSiGenie4DSystems.Async.Message;

namespace HeadlessDemoApp //Contrived example
{
	public class ReportEventSubscription : IDisposable
    {
        private IDisposable subscription;

        public ReportEventSubscription(){ }

        public void Subscribe()
        {
            var observable = Host.Instance.SubscribeToReportEventMessages(this.EnabledBoard.SerialDeviceId);
            this.subscription = observable.Subscribe(OnReportEventMessageReceived);
        }

        public void Dispose()
        {
            this.subscription.Dispose();
        }

        public ADCBoard EnabledBoard { get; set; }

        public void OnReportEventMessageReceived(ReportEventMessage hotReportEventMessage) 
        {
            ReportEventMessage hotReportEventMessage = (ReportEventMessage)sender;
			//
			//TODO: Switch on specific Workshop 4D project identifiers
			//      EXAMPLE BELOW  SHOWS HANDLING VARIOUS 4D BUTTONS
			//
            switch (hotReportEventMessage.ObjectType)
            {
                case ObjectType.Button4D:
                    {
                        switch (hotReportEventMessage.ObjectIndex)
                        {
                            case 0:
                                {
									//TODO: Application specific for button id 0 handling goes here...
                                    break;
                                }
                            case 1:
                                {
									//TODO: Application specific for button id 1 handling goes here...
                                    break;
                                }
                        }
                        break;
                    }
                case ObjectType.Form:
                    {
                        switch (hotReportEventMessage.ObjectIndex)
                        {
                            case 0:
                                {
                                    //TODO: user activated Form 0 on display
									//WARNING: DON'T BLOCK
                                    break; 
                                }
                            case 1:
                                {
									//TODO: user activated Form 1 on display
									//WARNING: DON'T BLOCK
                                    break; 
                                }
                        }//END OF SWITCH

                        break;
                    }//END OF FORM ACTIVATE
                case ObjectType.Winbutton:
                    {
                        //Winbutton event was recevied from host
                        switch (hotReportEventMessage.ObjectIndex)
                        {
                            case 0:
                                {
									//EXAMPLE:  shutdown headless app 
									ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, new TimeSpan(0));
                                    break;
                                }

                            case 5:
                                {
									//EXAMPLE: reboot headless app 
									ShutdownManager.BeginShutdown(ShutdownKind.Restart, new TimeSpan(0));
                                    break;
                                }
                        }
                        break; 
                    }//END OF WIN BUTTON

                default:
                    {
                        break;
                    }
            }
        }
    }
}
```

### Report Object Status Message Handler 

The Host Sends a ReadObjectStatusMessage when it wants to determine the current value of a
specific object instance. Upon receipt of this message the display will reply with either a NAK
(in the case of an error) or the ReportObjectStatusMessage message. 
Not all Workshop 4 projects use this features.

```C#
using System.Threading.Tasks;
using ViSiGenie4DSystems.Async.Event;
using ViSiGenie4DSystems.Async.Message;

namespace HeadlessDemoApp //Contrived example
{
public class ReportObjectStatusMessageHandler : IDisposable
    {
        private IDisposable subscription;

        public ReportObjectStatusMessageHandler()
        {
        }

        public void Subscribe()
        {
            IObservable<ReportObjectStatusMessage> observable = Host.Instance.SubscribeToReportObjectStatusMessage(this.EnabledBoard.SerialDeviceId);
            this.subscription = observable.Subscribe(OnReportEventMessageReceived);
        }

        public ADCBoard EnabledBoard { get; set; }

        public void Dispose()
        {
            this.subscription.Dispose();
        }

        public void OnReportEventMessageReceived(ReportObjectStatusMessage ReportObjectStatusMessage)
        {      
            //TODO: ADD YOUR HANDLER CASES HERE PER THE APPLICATION SPECIFIC 4D WORKSHOP PROJECT
        }
    }
}
```