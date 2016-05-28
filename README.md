# ViSiGenie4DSystems.Async

# About

This is a C# async class library for Windows IoT apps where a 4D Systems display module can be connected to the host's serial communication port. Windows IoT makers and commercial builders using this library should appreciate how nicely it maps to the ViSi-Genie Communication Protocols, Objects, Properties, and Genie Magic as specified in the ViSi-Genie Reference Manual.
An example of a resistive touch display is the 4.3" DIABLO16 Display Module that is loaded with a Workshop4 project running from 2-4 GB micro-SD card. The host is connected to the display using a Silicon Labs brand, CP2102, USB to Serial UART Bridge Converter Cable.

* Windows IoT makers and commercial builders will appreciate how nicely this library maps to the ViSi-Genie Communication Protocols, Objects, Properties, and Genie Magic data structures and protocols specificated in the [ViSi-Genie Reference Manual]: http://www.4dsystems.com.au/productpages/ViSi-Genie/downloads/Visi-Genie_refmanual_R_1_11.pdf

* Applications requiring a non-primary display can use a Raspberry Pi 2 and 3, Arrow DragonBoard 410c or MinnowBoard MAX as their host.  

* Headed or headless Windows IoT Core applications can be accompanied by one or more serial port connected display(s).

* Discovers one or more connected 4D Systems displays. The singleton class named *Host* creates and manages serial device instances. 
  For example, the Raspberry Pi 2 has four USB ports and this means potentially four different display modules could be found. 
  
* Queues ViSi Genie Report Events and Report Objects which originate from the display and then sends a custom C# async event. For example, the user presses a menu button object on the resistive touch display.  

## Hardware 

* [4D Programming Capable, USB to Serial UART Converter Cable](http://www.4dsystems.com.au/product/4D_Programming_Cable/)
* [uSD-4GB-Instustrial rated micro-SD card](http://www.4dsystems.com.au/product/uSD_4GB_Industrial/). Needs to be Phison. Industrial grade is optional. 
* [uLCD-35DT 3.5" TFT LCD Display Module with Resistive Touch] (http://www.4dsystems.com.au/product/uLCD_35DT_PI/) or other size module
* [Raspberry Pi 3 - Model B - ARMv8 with 1G RAM] (https://www.adafruit.com/product/3055) or equivalent P2, Broadcomm or Intel Atom type.

## Bring-Up Notes 

* Build and deploy your 4D Workshop4 project to the display's micro-SD card and then install micron-SD card into display's pannel.

* In Microsoft Visual Studio, use the NuGet Package Manager Console to install the library into your project: 

```
PM> Install-Package ViSiGenie4DSystems.Async.dll
```

* In Microsoft Visual Studio, edit the project's Package.appmanifest and add the capability to do *serialcommunication*. If deviceCapability is not configured, then serial device communications will fail when *Host* tries to connect to the 4D Systems display.

```XML
	<Capabilities>
		<DeviceCapability Name="serialcommunication">
			<Device Id = "any" >
				<Function Type="name:serialPort" />
			</Device>
		</DeviceCapability>
	</Capabilities>
```		
* Plug the USB programmers cable into the Pi 3 or equivalent USB port and connect the other end of the cable into the backside of the display's 5 pins connector.

* Review and add code clips shown below to your project. 

* Build your project in Visual Studio, cycle power on Pi 3 (ARM Solution Platform) or equivalent, deploy, and debug your app :)

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