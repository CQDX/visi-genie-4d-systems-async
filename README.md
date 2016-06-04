# ViSiGenie4DSystems.Async

# About

This is a C# async class library that support Windows IoT apps requiring a 4D Systems display. An IoT scenario might be an embedded power measurement meter built using a resistive touch 4.3" DIABLO16 Display Module loaded with a Workshop4 user interface project executing from a 4 GB micro-SD card. Leveraging Windows IoT core, an ARM, Qualcomm or Intel SOC can be connected to the display using a Silicon Labs brand, CP2102, USB to Serial UART Bridge Converter Cable.

## Interfacing with the 7.0" TFT LCD using ViSiGenie4DSystems.Async Lib
<img src="https://github.com/CQDX/visi-genie-4d-systems-async/blob/master/TouchDisplay.png">

* Directly maps the ViSi-Genie Communication Protocols, Objects, Properties, and Genie Magic to Interfaces implemented as C# Message classes.
  See [ViSi-Genie Reference Manual]: http://www.4dsystems.com.au/productpages/ViSi-Genie/downloads/Visi-Genie_refmanual_R_1_11.pdf for data structures and command/event protocols.

* Intended for applications requiring a non-primary display for a Raspberry Pi 2 and 3, Arrow DragonBoard 410c or MinnowBoard MAX host.  

* Connects and discovers one or more displays to Headed or Headless Windows IoT applications.
   
* Queues all incomming ViSi Genie Report Events and Report Objects originating from the display. The library sends C# async event for the app to handle. For instance, the user presses a menu button object on the resistive touch display.  

## Hardware 

* [4D Programming Capable, USB to Serial UART Converter Cable](http://www.4dsystems.com.au/product/4D_Programming_Cable/)
* [uSD-4GB-Instustrial rated micro-SD card](http://www.4dsystems.com.au/product/uSD_4GB_Industrial/). Needs to be Phison. Industrial grade is optional. 
* [uLCD-35DT 3.5" TFT LCD Display Module with Resistive Touch] (http://www.4dsystems.com.au/product/uLCD_35DT_PI/) or other size module
* [Raspberry Pi 3 - Model B - ARMv8 with 1G RAM] (https://www.adafruit.com/product/3055) or equivalent P2, Broadcomm or Intel Atom type.

## Bring-Up Notes 

* Build and deploy your 4D Workshop4 project to the display's micro-SD card and then install micron-SD card into display's pannel.

* In Microsoft Visual Studio, use the NuGet Package Manager Console to install the library into your project: 

```
PM> Install-Package ViSiGenie.4DSystems.Async
```

* In Microsoft Visual Studio, edit the project's Package.appmanifest and add a capability to support *serialcommunication*. If deviceCapability is not configured, then serial device communications will fail when *Host* tries to connect to the 4D Systems display.

```XML
	<Capabilities>
		<DeviceCapability Name="serialcommunication">
			<Device Id = "any" >
				<Function Type="name:serialPort" />
			</Device>
		</DeviceCapability>
	</Capabilities>
```		
* Plug the USB programmers cable into the USB port on Pi 3 or equivalent. Connect the other end of the cable into the backside of the display's 5 pins connector.

* Review and add code clips shown below to your project. 

* Build your ARM project in Visual Studio, cycle power on Pi 3. Then deploy and debug your app :)

## Class Relationships

The singleton class named *Host* creates and manages the lifetime of serial device instances. For example, a Raspberry Pi 2 with four USB ports potentially could have four different display modules to monitor various subsystems of an embedded device and in turn offer user interactive control. 

<img src="https://github.com/CQDX/visi-genie-4d-systems-async/blob/master/ViSiGenie4DSystems.Async/ClassDiagram.png">

## Host.Instance 

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
			
			//Host baud must match 4D Workshop project baud rate, else Connect will throw
			var portDef = new PortDef(BaudRate.Bps115200);
			Task connectTask = Host.Instance.Connect(deviceId, portDef);
			await connectTask;

			//3. App start listening for display sent from the display. Developer writes the handler method...
			await Host.Instance.StartListening(deviceId,
											   ReportEventMessageHandler.Handler,
											   ReportObjectStatusMessageHandler.Handler);
		
			//4. Change to form 0 on display per a particular 4D Workshop4 project layout.  
			const int displayFormId = 0;
			var writeObjectMessage = new WriteObjectValueMessage(ObjectType.Form, displayFormId);
			await Host.Instance.Send(enabledBoard.SerialDeviceId, writeObjectMessage, cts.Token);

			//5. Write string message to display per a particular 4D Workshop4 project layout...
			const int displayStringId = 0;
			var writeStringMessage = new WriteStringASCIIMessage(displayStringId, "Hello 4D Systems via Windows IoT!");
			await Host.Instance.Send(enabledBoard.SerialDeviceId, writeStringMessage, cts.Token);
		
			//6. App stops listening to display events
		    Host.Instance.StopListening(enabledBoard.Value.SerialDeviceId,
										ReportEventMessageHandler.Handler,
										ReportObjectStatusMessageHandler.Handler);
    
            _defferal.Complete();
        }        
    }
}
```

## Report Event Message Handler 

The exemplar below shows how-to handle a received 4D System Report Event Message. Switch statements can be added to handle behavior specific to the particular Workshop 4D project.

```C#
using System.Threading.Tasks;

using ViSiGenie4DSystems.Async.Enumeration;
using ViSiGenie4DSystems.Async.Event;
using ViSiGenie4DSystems.Async.Message;

namespace DisplayIO
{
    public class ReportEventMessageHandler 
    {
        public async void Handler (object sender, DeferrableDisplayEventArgs e)
        {
            using (var deferral = e.GetDeferral())
            {
                //Run task message cracker in thread pool thread
                await Task.Run(() =>
               {
                   ReportEventMessage hotReportEventMessage = (ReportEventMessage)sender;

				   //TODO: Switch on your specific Workshop 4D project identifiers, for example,
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
                                           break; 
                                        }
                                   case 1:
                                       {
										   //TODO: user activated Form 1 on display...
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
										   // i.e., maybe shutdown headless app here
                                           break;
                                       }

                                   case 5:
                                       {
									       // i.e., maybe reboot headless app here
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
                });
            }
        }
    }
}
```

## Report Object Status Message Handler 

The exemplar below shows how-to handle a received 4D System Report Object Status Message. Switch statements can be added to handle behavior specific to the particular Workshop 4D project.

```C#
using System.Threading.Tasks;
using ViSiGenie4DSystems.Async.Event;
using ViSiGenie4DSystems.Async.Message;

namespace DisplayIO
{
    public class ReportObjectStatusMessageHandler
    {
        public async void Handler(object sender, DeferrableDisplayEventArgs e)
        {
            using (var deferral = e.GetDeferral())
            {
                await Task.Run(() =>
                {
                    ReportObjectStatusMessage hotReportObjectMessage = (ReportObjectStatusMessage)sender;
             
			        //TODO: Switch on the report object status message ...
                });
            }
        }
    }
}
```