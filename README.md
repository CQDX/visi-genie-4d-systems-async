# ViSiGenie4DSystems.Async

# About

This is a C# async class library for Windows IoT apps where one or more 4D Systems display module(s) can be connected to the host's serial communications port. Windows IoT makers and commercial builders using this library should appreciate how nicely it maps to the ViSi-Genie Communication Protocols, Objects, Properties, and Genie Magic as specified in the ViSi-Genie Reference Manual.
An example of a touch display is the 4.3" DIABLO16 Display Module that is loaded with a Workshop4 project running off of 2-4 GB Phison MicroSD Card. The host is connected to the display using a Silicon Labs brand, CP2102, USB to Serial UART Bridge Converter Cable.

* Windows IoT makers and commercial builders will appreciate how nicely this library maps to the ViSi-Genie Communication Protocols, Objects, Properties, and Genie Magic data structures and protocols specificated in the [ViSi-Genie Reference Manual]: http://www.4dsystems.com.au/productpages/ViSi-Genie/downloads/Visi-Genie_refmanual_R_1_11.pdf

* Headed or headless Windows IoT Core applications can be accompanied by one or more serial port connected display.

* Supports host application development on the Raspberry Pi 2 and 3, Arrow DragonBoard 410c or MinnowBoard MAX app.  

* Discovers one or more connected displays. The singleton class named *Host* creates instances of serially connected device. 
  For example, the Raspberry Pi 2 has four USB ports and this means potentially four different display modules could be found. 
  
* Supports listening for ViSi Genie Report Events and Report Objects. For example, the user presses a menu button object on the touch display.  

## BRING-UP NOTES 

* Deploy your 4D Workshop4 project to your 4D Systems display's uSD card. 

* Edit your app's package manifest and add the *serialcommunication* capability. If deviceCapability is not configured, then serial device communications will fail when *Host* tries to connect to the 4D Systems display.

```XML
	<Capabilities>
		<DeviceCapability Name="serialcommunication">
			<Device Id = "any" >
				<Function Type="name:serialPort" />
			</Device>
		</DeviceCapability>
	</Capabilities>
```		
* Plug the 4D Systems Silabs USB programmers cable into the host's USB port and connect the other end of the cable to display's backside 5 pins connector.

## CODE CLIP  

Below is code clip from a Windows IoT headless app that shows how to use the *Host.Instance* singleton:

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
		
			//1. Find the connected device's identifier string and hang on to it
			Task<List<string>> discoverDeviceIdsTask = Host.Instance.DiscoverDeviceIds();
			
			await discoverDeviceIdsTask;
			 
			//2. Connect host to 4D Systems display
			//   In this case, ony one display is connected to the host
			var deviceId = discoverDeviseIdsTask.Result.First();
			
			//Baud rate of host must match 4D Workshop project baud rate
			var portDef = new PortDef(BaudRate.Bps115200);
			Task connectTask = Host.Instance.Connect(deviceId, portDef);
			await connectTask;

			//3. Host start listening for display events. 
			//   You need to write the handler method; i.e., TODO...
			await Host.Instance.StartListening(deviceId,
												/*** TODO MyReportEventClass.ReportEventMessageHandler.Handler,
												MyReportObjectClass.ReportObjectStatusMessageHandler.Handler ***/);
		
			//4. Change to form 0 on display per 4D Workshop4 project layout...
			const int displayFormId = 0;
			var writeObjectMessage = new WriteObjectValueMessage(ObjectType.Form, displayFormId);
			await Host.Instance.Send(enabledBoard.SerialDeviceId, writeObjectMessage, cts.Token);

			//5. Write string message to display per 4D Workshop4 project layout...
			const int displayStringId = 0;
			var writeStringMessage = new WriteStringASCIIMessage(displayStringId, "Hello 4D Systems via Windows IoT!");
			await Host.Instance.Send(enabledBoard.SerialDeviceId, writeStringMessage, cts.Token);
		
			//5. Host stops listening to display events
		    Host.Instance.StopListening(enabledBoard.Value.SerialDeviceId,
										/*** TODO MyReportEventClass.ReportEventMessageHandler.Handler,
										MyReportObjectClass.ReportObjectStatusMessageHandler.Handler ***/);
    
            _defferal.Complete();
        }        
    }
}
```
